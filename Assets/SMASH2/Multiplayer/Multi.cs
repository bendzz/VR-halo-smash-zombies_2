using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.VisualScripting.FullSerializer;
//using UnityEditor.UIElements;
//using UnityEditor.UIElements;
//using UnityEditor.PackageManager;
using UnityEngine;



/// <summary>
/// Generic code for multiplayer and recording/playback of gameplay. This is the only script that should be networked (hopefully, we'll see)
/// Use for all networked variables and functions.
/// Note: Can only sync public variables and methods
/// </summary>
public class Multi : NetworkBehaviour
{
    [Tooltip("If enabled, prepare for a bajillion old print statements")]
    public bool debug = false;

    public static Multi instance;

    /// <summary>
    /// Unique strings for every prefab in the project (when it was last run in editor anyway), for spawning them across the network
    /// </summary>
    public static Dictionary<string, GameObject> prefabIds_GOs;
    /// <summary>
    /// Opposite direction dictionary, for finding the string ID of a given prefab
    /// </summary>
    public static Dictionary<GameObject, string> prefabIds_Strings;

    /// <summary>
    /// All prefab instances that have server IDs (ie the same on all clients). 
    /// (Note, prefab here means 'spawned instance'; if u want the original prefab ref, look in its entities)
    /// </summary>
    public Dictionary<int,GameObject> syncedPrefabs { get; private set; }

    /// <summary>
    /// Prefab instances that have been spawned locally but are still waiting on a global ID from the server
    /// </summary>
    public Dictionary<int, GameObject> localPrefabs { get; private set; }

    // TODO:
    // -Make it split off and save new clips with button presses, for making it easier to save highlights for later editing
    // -Make Record.cs save clips gradually as you go so it doesn't take forever at the end. (And load clips gradually)
    // --and make it async
    // --and an option to spawn a progress bar UI on a given camera/gameobject, for VR
    // -Will need a cycling clip that holds like 1024 frames, for server authoritative games. (This one won't be saved)
    /// <summary>
    /// A recording of the game, and tracks all multiplayer synced properties (ie script variables and methods)
    /// </summary>
    public Record.Clip clip;






    private void Awake()
    {
        //NetworkManager.Singleton.OnServerStarted += HandleServerStarted;  // doesn't work
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
    }

    //private void HandleServerStarted()
    //{
    //    print("HandleServerStarted");
    //    // This will be called when the host starts the game (server-side logic)
    //    if (NetworkManager.Singleton.IsHost)
    //    {
    //        //SpawnPlayer();
    //        print("Host started game, spawning player");
    //    }
    //}

    private void OnClientConnectedCallback(ulong clientId)
    {
        // https://docs-multiplayer.unity3d.com/netcode/current/components/networkmanager/
        Debug.Log($"Player with ClientId {clientId} connected!");

        // If you want to spawn a player object only on the server when a client connects:
        if (NetworkManager.Singleton.IsServer)
        {
            // Your player spawning logic here
        }

        if (IsServer)
        {
            // send all the current scene prefabs to the new client
            print("New client joined! Syncing " + syncedPrefabs.Count + " prefabs");

            foreach(KeyValuePair<int, GameObject> prefabInstance in syncedPrefabs)
            {
                if (debug)
                    print("Sending prefab " + prefabInstance.Value.name + ", ID " + prefabInstance.Key);
                //if (prefabInstance.Value.GetComponentInChildren<SmashCharacter>())
                //    print("is smashCharacter for " + prefabInstance.Value.GetComponentInChildren<SmashCharacter>().playerName); // debug
                
                if (prefabInstance.Value != null)
                    sendExistingPrefab(clientId, prefabInstance.Value, prefabInstance.Key);
                // TODO sync up properties to initial values (they might not be updating every frame)
            }

        }
    }

    /// <summary>
    /// When a new client joins, use this to spawn existing prefab instances into their scene
    /// </summary>
    static void sendExistingPrefab(ulong newClientId, GameObject Go, int GoKey)
    {
        //ulong clientId = pars.Receive.SenderClientId;

        //List<Multi.Entity> prefabEntities = (List<Multi.Entity>)data.GetData();

        List<Multi.Entity> prefabEntities = getAllEntitysInPrefab(Go);
        if (prefabEntities[0].ownerClientID == newClientId)
        {
            if (instance.debug)
                Debug.Log(Go.name + ", key "+ GoKey + " has ownerClientID#" + prefabEntities[0].ownerClientID + ", same as newClientId " + newClientId + ". Prefab " +
                "probably already exists over there, aborting send");
            return;
        }
        if (prefabEntities.Count == 0)
        {
            Debug.LogWarning("0 entities found for Go " + Go.name + " with ID " + GoKey + ", not sending to client, aborting");
            return;
        }

        NetData data = new NetData(prefabEntities);


        //print("Server: " + clientId + " pinged the server with " + prefabEntities.Count + " entities for syncing.");


        //int uniquePrefabID = getSyncedPrefabID();
        int uniquePrefabID = GoKey;

        string prefabString = "";
        foreach (var entity in prefabEntities)
        {
            //print("Entity found, has " + entity.propertiesCount + " properties, local_entityID " + entity.local_entityID);
            //entity.syncedPrefabId = uniquePrefabID;

            //entity.serverEntityId = Entity.getUniqueServerIdentifier();
            //Entity.serverEntities.Add(entity.serverEntityId, entity);

            entity.serverSendToClients = true;

            // give properties IDs
            entity.PropertyIDs = new List<int>();
            for (int p = 0; p < entity.properties.Count; p++)
            {
                //entity.PropertyIDs.Add(SyncedProperty.getUniqueIdentifier());   // (The whole point of all this entity bullshit tbh)
                entity.PropertyIDs.Add(entity.properties[p].identifier);   // (The whole point of all this entity bullshit tbh)
                if (instance.debug)
                    print(p + " " + entity.PropertyIDs[p]);

            }
            // assign entity back to list?
            prefabString = prefabIds_Strings[entity.originalPrefabRef];

            //entity.propertiesCount = entity.properties.Count;
        }

         data.setData(prefabEntities);


        // send to only the new client
        // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/clientrpc/
        // NOTE! In case you know a list of ClientId's ahead of time, that does not need change,
        // Then please consider caching this (as a member variable), to avoid Allocating Memory every time you run this function
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { newClientId }
            }
        };





        //netSpawnPrefab_ClientRpc(data, clientId);  // send to all clients
        //instance.netSpawnPrefab_ClientRpc(prefabString, NetworkManager.Singleton.OwnerClientId, data);  // send to all clients
        instance.netSpawnPrefab_ClientRpc(prefabString, NetworkManager.Singleton.LocalClientId, data, clientRpcParams); // send to specific client
    }


    //private void OnPlayerDisconnected(ulong clientId)
    private void OnClientDisconnectCallback(ulong clientId)
    {
        Debug.Log($"Player with ClientId {clientId} disconnected!");
    }


    /// <summary>
    /// Temp test
    /// </summary>
    Rec testRec;


    public override void OnNetworkSpawn() 
    {
        if (instance != null)
            Debug.LogError("More than one Multi singleton in scene");
        instance = this;

        clip = new Record.Clip("SmashClip " + UnityEngine.Random.value * 1000);

        syncedPrefabs = new Dictionary<int, GameObject>();

        SyncedProperty.SyncedProperties = new Dictionary<int, SyncedProperty>();    // static; needs to be manually reset if domain refreshing is off

        //prefabIds_GOs = PrefabDictionaryBuild.createDictionary_S_to_GO();
        //prefabIds_Strings = PrefabDictionaryBuild.createDictionary_GO_to_S();

        prefabIds_GOs = PrefabStrings.CreateDictionary_S_to_GO();
        prefabIds_Strings = PrefabStrings.CreateDictionary_GO_to_S();
        print("PrefabStrings.CreateDictionary_S_to_GO " + prefabIds_GOs.Count);

        foreach (KeyValuePair<string, GameObject> kvp in prefabIds_GOs)
        {
            print("prefab string: " + kvp.Key + ", GameObject: " + kvp.Value);
        }


        testRec = new Rec();
    }



    // Update is called once per frame
    void Update()
    {
        //NetworkManager manager = NetworkManager.Singleton;
        //manager.

        //UnityTransport unityTransport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
        ////unityTransport.OnTransportEvent += OnTransportEvent;
        //unityTransport.
    }

    float countdown = 0;
    private void LateUpdate()
    {
        //countdown++;
        countdown += Time.deltaTime;


        if (countdown >= (1 / 30)) // attempting to fix the *minutes* of latency in our 5 person test run
        {
            countdown = 0;
            if (SyncedProperty.SyncedProperties == null)
                return;
            // sync everything
            // TODO Add sync calls scripts can use. (syncProperty(ref variable or method) etc)
            //   OR, it doesn't matter? RPCs always sent at end of frame? https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc/
            foreach (SyncedProperty prop in SyncedProperty.SyncedProperties.Values)
            {
                if (debug)
                    print("prop " + prop.identifier + " isOwner " + prop.IsOwner + " value " + prop.getCurrentValue());
                if (prop.IsOwner)
                    prop.sync();
            }
            SyncedProperty.deleteDeadProperties();
        }
    }



    

    // SPAWNING:

    // netspawnprefab
    // - finds all entities and tags then with a global prefab code
    // - tells the server to tell all clients to spawn the prefab
    //   -server comes up with unique SyncedProperty IDs for each property in the prefab, and sends them to all clients (including the original spawner)
    //   -The clients assign those IDs using their entity's property lists, to sync all properties of this prefab across the network
    // (Also add the SyncedProperties to their dictionary once they have an ID)

    // first spawns a local copy on client, then tells server and gets IDs back

    public static GameObject netSpawnPrefab_ToServer(GameObject prefab, bool IsOwner, ulong ownerClientID)
    {
        GameObject c = null;
        if (prefab)
            c = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        else
            Debug.LogError("Prefab is not assigned!");

        string prefabString = prefabIds_Strings[prefab];
        //print("prefab string: " + prefabString);

        //if (instance.IsServer)
        //{
        //    instance.syncedPrefabs.Add(0, c);
        //}

        // setup netbehaviours
        //List<NetBehaviour> netBehaviours = GetAllNetBehavioursInPrefab(c);
        //foreach (var netBehaviour in netBehaviours)
        //{
        //    netBehaviour.OnNetworkSpawn();
        //    netBehaviour.IsOwner = IsOwner;
        //    netBehaviour.IsServer = instance.IsServer;
        //}
        setupNetBehaviours(c, IsOwner, ownerClientID);

        // give entities temp local-client codes, before the server gives them real ones
        List<Multi.Entity> prefabEntities = getAllEntitysInPrefab(c);
        if (instance.debug)
            print("netSpawnPrefab_ToServer: prefabEntities.Count " + prefabEntities.Count);
        foreach (var entity in prefabEntities)
        {
            //entity.syncedPrefabId = (int)(UnityEngine.Random.value * int.MinValue);
            //instance.syncedPrefabs.Add(entity.syncedPrefabId, prefab);

            entity.ownerClientID = ownerClientID;
            entity.localPrefabId = getLocalPrefabID();
            //instance.localPrefabs.Add(entity.localPrefabId, prefab);

            if (instance.debug)
            {
                print("entity property count " + entity.properties.Count + " local entity ID " + entity.local_entityID);
            }
        }

        instance.localPrefabs.Add(prefabEntities[0].localPrefabId, c);


        var clientId = NetworkManager.Singleton.LocalClientId;

        if (instance.debug)
            print("Spawning Client (ID: " + clientId + "): netSpawnPrefab_ToServer complete for: " + prefab.name + 
            ", sent " + prefabEntities.Count + " entity to the server for global IDs");


        //instance.netSpawnPrefab_ServerRpc(serverPrefabEntities);
        // send to server
        NetData data = new NetData(prefabEntities);
        instance.netSpawnPrefab_ServerRpc(prefabString, data);    // one call per prefab, not per entity

        return c;
    }

    /// <summary>
    /// run on just-spawned prefab instances, to trigger stuff like OnNetworkSpawn().
    /// (Host is assumed to own gameobjects by default, unless IsOwner is true and LocalClientId is not 0)
    /// </summary>
    /// <param name="c"></param>
    /// <param name="IsOwner"></param>
    static void setupNetBehaviours(GameObject c, bool IsOwner, ulong LocalClientId)
    {
        if (IsOwner && LocalClientId == 0 && !instance.IsServer)
            Debug.LogError("IsOwner is true but LocalClientId is 0! If a gameobject is owned by a client (like say a player model), you have to say which client!");
        List<NetBehaviour> netBehaviours = GetAllNetBehavioursInPrefab(c);
        foreach (var netBehaviour in netBehaviours)
        {
            netBehaviour.IsOwner = IsOwner;
            //print("netBehaviour.IsOwner " + netBehaviour.IsOwner);
            netBehaviour.IsServer = instance.IsServer;
            netBehaviour.OwnerClientId = LocalClientId;
            netBehaviour.OnNetworkSpawn();
        }
    }





    /// <summary>
    /// Called by the spawning client, runs on the server to generate global IDs and sync them to all clients.
    /// (Doesn't actually do any spawning on the server, leaves that for the client call)
    /// </summary>
    /// <param name="data"></param>
    /// <param name="pars"></param>
    [ServerRpc(RequireOwnership = false)]   // Note: This singleton class always runs as !IsOwner on non server clients, for some reason.
    void netSpawnPrefab_ServerRpc(string prefabString, NetData data, ServerRpcParams pars = default)
    {
        ulong clientId = pars.Receive.SenderClientId;

        List<Multi.Entity> prefabEntities = (List<Multi.Entity>)data.GetData();

        if (debug)
            print("Server: Client " + clientId + " pinged the server with " + prefabEntities.Count + " entities for syncing.");


        int uniquePrefabID = getSyncedPrefabID();

        // TODO
        // -need to get the entity's info/lists to the server, via serializing ig..?

        // -give each entity its new global prefab ID (but need to save the old one so the spawning client can match them back up)
        // -give each SyncedProperty a global ID
        // -send the info back to all clients
        foreach (var entity in prefabEntities)
        {
            if (debug)
                print("Entity found, has " + entity.propertiesCount + " properties, local_entityID " + entity.local_entityID);
            entity.syncedPrefabId = uniquePrefabID;
            
            entity.serverEntityId = Entity.getUniqueServerIdentifier();
            Entity.serverEntities.Add(entity.serverEntityId, entity);

            entity.serverSendToClients = true;

            // give properties IDs
            entity.PropertyIDs = new List<int>();
            for (int p = 0; p < entity.propertiesCount; p++)
            {
                entity.PropertyIDs.Add(SyncedProperty.getUniqueIdentifier());   // (The whole point of all this entity bullshit tbh)
                if (debug)
                    print(p + " property: " + entity.PropertyIDs[p]);
            }
            // assign entity back to list?
        }

        data.setData(prefabEntities);

        //netSpawnPrefab_ClientRpc(data, clientId);  // send to all clients
        netSpawnPrefab_ClientRpc(prefabString, clientId, data);  // send to all clients
    }

    /// <summary>
    /// Only server instance should call this. (Doesn't automatically register the ID to the dictionary)
    /// </summary>
    /// <returns></returns>
    static int getSyncedPrefabID()
    {
        if (instance.syncedPrefabs == null)
            instance.syncedPrefabs = new Dictionary<int, GameObject>();
        int id;
        do
        {
            id = (int)(UnityEngine.Random.value * int.MaxValue);  // idk if the safety minuses are neccessary
        } while (instance.syncedPrefabs.ContainsKey(id));
        return id;
    }

    /// <summary>
    /// get local-client-only ID for a prefab instance that the server hasn't given a global ID yet
    /// </summary>
    /// <returns></returns>
    static int getLocalPrefabID()
    {
        if (instance.localPrefabs == null)
            instance.localPrefabs = new Dictionary<int, GameObject>();
        int id;
        do
        {
            id = (int)(UnityEngine.Random.value * int.MaxValue);  // idk if the safety minuses are neccessary
        } while (instance.localPrefabs.ContainsKey(id));
        return id;
    }


    /// <summary>
    /// The server just sent back unique IDs for this prefab, its entities and their properties. Assign them. 
    /// (And spawn the prefab if you're not the original spawning client!)
    /// </summary>
    /// <param name="data"></param>
    /// <param name="originalSender"></param>
    /// <param name="pars"></param>
    [ClientRpc]
    public void netSpawnPrefab_ClientRpc(string prefabString, ulong originalSender, NetData data, ClientRpcParams pars = default)
    {
        
        // WARNING the datastream seems to be getting corrupted after the NetData?? TODO
        var thisClientId = NetworkManager.Singleton.LocalClientId;
        // TODO originalSender is broken! 0!

        List<Multi.Entity> serverPrefabEntities = (List<Multi.Entity>)data.GetData();
        if (debug)
            print("entities received " + serverPrefabEntities.Count);

        bool IsOwner = false;

        int syncedPrefabId = -1;
        GameObject c;
        if (thisClientId != originalSender) // spawn prefab on other clients
        {
            IsOwner = false;
            GameObject prefab = prefabIds_GOs[prefabString];
            c = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            setupNetBehaviours(c, IsOwner, serverPrefabEntities[0].ownerClientID);
            print("prefab " + c.name + " SPAWNED on this client, SyncedPrefabID " + serverPrefabEntities[0].syncedPrefabId);

            // give entities their global IDs
            List<Multi.Entity> localPrefabEntities = getAllEntitysInPrefab(c);
            if (debug)
                print("localPrefabEntities.Count " + localPrefabEntities.Count);
            if (localPrefabEntities.Count != serverPrefabEntities.Count)
                Debug.LogError("localPrefabEntities.Count != prefabEntities.Count. (Should be impossible? Hacking?). Values: localPrefabEntities.Count "
                    + localPrefabEntities.Count + " serverPrefabEntities.Count " + serverPrefabEntities.Count);

            for (int i = 0; i < localPrefabEntities.Count; i++) // just counting on this getting the entities in the same order every time tbh
            {
                Entity localEnt = localPrefabEntities[i];
                Entity serverEnt = serverPrefabEntities[i];

                // TODO pull all this out into a function, it's duplicated with below
                localEnt.serverEntityId = serverEnt.serverEntityId;
                localEnt.syncedPrefabId = serverEnt.syncedPrefabId;
                localEnt.ownerClientID = serverEnt.ownerClientID;
                syncedPrefabId = serverEnt.syncedPrefabId;
                localEnt.originalPrefabRef = prefab;

                if (localEnt.properties.Count != serverEnt.PropertyIDs.Count)
                    Debug.LogError("Different property counts for entity " + i + "! localEnt.propertiesCount" + localEnt.properties.Count + " serverEnt.PropertyIDs.Count " + serverEnt.PropertyIDs.Count);
                
                for (int p = 0; p < localEnt.properties.Count; p++)
                {
                    //SyncedProperty localProp = SyncedProperty.SyncedProperties[i];
                    SyncedProperty localProp = localEnt.properties[p];
                    localProp.identifier = serverEnt.PropertyIDs[p];  // all that work for this ONE LITTLE value...
                    localProp.IsOwner = IsOwner;
                    if (debug)
                        print("localProp.identifier " + localProp.identifier);
                    SyncedProperty.SyncedProperties.Add(localProp.identifier, localProp);   // they're not actually in the list until they have synced IDs
                }
            }

        } else
        {
            IsOwner = true;
            int localPrefabID = serverPrefabEntities[0].localPrefabId;    // find original spawned prefab
            c = localPrefabs[localPrefabID];
            
            print("prefab " + c.name + " FOUND on original client, SyncedPrefabID " + serverPrefabEntities[0].syncedPrefabId);

            // connect to original entities, transfer server IDs
            foreach (Multi.Entity serverEnt in serverPrefabEntities)
            {
                Entity localEnt = Entity.localEntities[serverEnt.local_entityID];
                localEnt.serverEntityId = serverEnt.serverEntityId;
                localEnt.syncedPrefabId = serverEnt.syncedPrefabId;
                localEnt.ownerClientID = serverEnt.ownerClientID;   // unneccessary here, just added to keep code consistent
                syncedPrefabId = serverEnt.syncedPrefabId;
                localEnt.originalPrefabRef = prefabIds_GOs[prefabString];

                if (localEnt.properties.Count != serverEnt.PropertyIDs.Count)
                    Debug.LogError("Different property counts for entity (unknown)! localEnt.propertiesCount" + localEnt.properties.Count + " serverEnt.PropertyIDs.Count " + serverEnt.PropertyIDs.Count);
                if (debug)
                    print("entity " + localEnt.local_entityID + " has " + localEnt.properties.Count + ", server ID " + localEnt.serverEntityId + " in prefab " + localEnt.syncedPrefabId);

                for (int i = 0; i < serverEnt.propertiesCount; i++)
                {
                    //SyncedProperty localProp = SyncedProperty.SyncedProperties[i];
                    SyncedProperty localProp = localEnt.properties[i];
                    localProp.identifier = serverEnt.PropertyIDs[i];  // all that work for this ONE LITTLE value...
                    localProp.IsOwner = IsOwner;
                    if (debug)
                        print("localProp.identifier " + localProp.identifier);
                    SyncedProperty.SyncedProperties.Add(localProp.identifier, localProp);
                }
            }
        }

        //if (IsServer)
        //{
            syncedPrefabs.Add(syncedPrefabId, c);
        if (debug)
            print("added syncedPrefab to list: " + syncedPrefabId + ", " + c.name);
        //}



        //getAllEntitysInPrefab


        //foreach (Multi.Entity entity in serverPrefabEntities) // debug
        //{
        //    print("Entity found, syncedPrefabId ID " + entity.syncedPrefabId + " localPrefabId " + entity.localPrefabId + ", has " +
        //        entity.propertiesCount + " properties, local_entityID " + entity.local_entityID + " serverEntityId " + entity.serverEntityId +
        //        "");
        //    foreach(int propertyID in entity.PropertyIDs)
        //    {
        //        print("propertyID " + propertyID);
        //    }
        //}

        //asdasda
        if (debug)
            print("Server pinged client " + thisClientId + " (originally from client " + originalSender + ") with " + data.GetData().ToString());
    }



    /// <summary>
    /// Helper function used to find all scripts inheriting from NetworkBehaviour in a given prefab.
    /// </summary>
    /// <param name="prefabToSearch"></param>
    /// <returns>List of NetworkBehaviour scripts</returns>
    public static List<NetBehaviour> GetAllNetBehavioursInPrefab(GameObject prefabToSearch) // GPT 4 script
    {
        if (prefabToSearch == null) return null;

        // Get all NetworkBehaviour scripts in the prefab and its children
        NetBehaviour[] allNetworkBehaviours = prefabToSearch.GetComponentsInChildren<NetBehaviour>();
        List<NetBehaviour> foundNetworkBehaviours = new List<NetBehaviour>(allNetworkBehaviours);

        if (instance.debug)
            Debug.Log($"Found {foundNetworkBehaviours.Count} scripts inheriting from NetBehaviour in the prefab.");
        return foundNetworkBehaviours;
    }

    /// <summary>
    ///  helper used for spawning prefabs and activating their entity instances
    /// </summary>
    /// <param name="prefabToSearch"></param>
    public static List<Multi.Entity> getAllEntitysInPrefab(GameObject prefabToSearch)    // GPT 4 script
    {
        if (prefabToSearch == null) return null;

        // Get all MonoBehaviour scripts in the prefab and its children
        MonoBehaviour[] allScripts = prefabToSearch.GetComponentsInChildren<MonoBehaviour>();

        List<Multi.Entity> foundEntities = new List<Multi.Entity>();

        foreach (var script in allScripts)
        {
            // Use reflection to search through each field in the script
            System.Reflection.FieldInfo[] fields = script.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(Multi.Entity))
                {
                    Multi.Entity entityInstance = field.GetValue(script) as Multi.Entity;
                    if (entityInstance != null)
                    {
                        foundEntities.Add(entityInstance);
                    }
                }
            }
        }

        if (instance.debug)
            Debug.Log($"Found {foundEntities.Count} instances of Multi.Entity in the prefab: " + prefabToSearch.name);

        return foundEntities;
    }








    /// <summary>
    /// All synced/recorded variables and functions for a particular script (as SyncedProperties), and any components/transforms it's tightly coupled to.
    /// Meant to keep scripts separate and modular, easy to drop into future projects.
    /// Lets the server tell other clients to spawn the script's stuff and sync its properties across the network (using their list order to match them up).
    /// Is invoked by netSpawnPrefab_ToServer.
    /// (Entity is sometimes analogous to the AnimatedComponent variables of Record.cs AnimatedProperties; ie representing a script or transform etc)
    /// </summary>
    public class Entity : INetworkSerializable
    {
        // prefab IDs

        /// <summary>
        /// The NetworkManager.Singleton.OwnerClientId that owns this one (a number between 0 and (number of players in lobby)). Usually 0 for host
        /// </summary>
        public ulong ownerClientID;


        /// <summary>
        /// A unique, server genertated ID for an instance of the prefab holding this entity, across all client machines.
        /// </summary>
        public int syncedPrefabId;

        /// <summary>
        /// A temp code the spawning client comes up with, so it can tell which of its spawned prefabs the server is sending it real IDs for in a sec
        /// </summary>
        public int localPrefabId;

        /// <summary>
        /// Used for spawning new, fresh instances of this prefab on other clients
        /// </summary>
        public GameObject originalPrefabRef;


        // local ID

        /// <summary>
        /// A local-only list of local IDs for entities, for the original spawning client
        /// </summary>
        public static Dictionary<int, Entity> localEntities;

        /// <summary>
        /// A local-only ID that the original spawning client makes up so the entity can receive its info from the server
        /// </summary>
        public int local_entityID;


        ///// <summary>
        ///// if a prefab has 5 entities, this number will be between 0 and 4.
        ///// This is how the entities are matched up client side
        ///// </summary>
        //public int entityIdInPrefab;

        // server ID

        /// <summary>
        /// A server authoritative list of global Ids for entities, across all clients
        /// </summary>
        public static Dictionary<int, Entity> serverEntities;
        /// <summary>
        /// This entity's global ID, across all clients
        /// </summary>
        public int serverEntityId;


        // property IDs

        /// <summary>
        /// Must be the same across all clients; properties are synced by their list order
        /// </summary>
        public List<SyncedProperty> properties;
        //public object animatedComponent;
        //public GameObject gameObject;

        /// <summary>
        /// Server generates 1 for every property and sends it to all clients, so they can match up the properties across all clients
        /// </summary>
        public List<int> PropertyIDs;
        /// <summary>
        /// only for serialization, not neccessarily accurate
        /// </summary>
        public int propertiesCount;    


        // helpers

        /// <summary>
        /// To save typing, you can set this then use addSyncedProperty a bunch with this id
        /// </summary>
        public object current_AnimatedComponent;
        /// <summary>
        /// To save typing, you can set this then use addSyncedProperty a bunch with this id
        /// </summary>
        public GameObject current_gameObject;
        /// <summary>
        /// To save typing, you can set this then use addSyncedProperty a bunch with this id
        /// </summary>
        public bool current_IsOwner;

        /// <summary>
        /// need to send the number of properties from spawning client to server, then the IDs list from server to all clients
        /// </summary>
        public bool serverSendToClients = false;

        ///// <summary>
        ///// For destroying the entity when the script is destroyed
        ///// </summary>
        //public NetBehaviour parentScript;


        /// <summary>
        /// see class summary.
        /// (When _parentScript onDestroy() is called, this entity will be destroyed too)
        /// </summary>
        /// <param name="script_Or_AnimatedObject"></param>
        //public Entity(NetBehaviour _parentScript)
        public Entity()
        {
            properties = new List<SyncedProperty>();
            local_entityID = getUniqueLocalIdentifier();
            //parentScript = _parentScript;

            addToLocalEntities();   // idk why I had this separated before. Just do it automatically for now
        }

        /// <summary>
        /// Register on the local list so when the server sends back an entity copy with global property IDs, we can actually link it to the original.
        /// (idk why this isn't done automatically every time tbh)
        /// </summary>
        public void addToLocalEntities()
        {
            if (localEntities == null)
                localEntities = new Dictionary<int, Entity>();
            localEntities.Add(local_entityID, this);
        }

        public static int getUniqueLocalIdentifier()
        {
            if (localEntities == null)
                localEntities = new Dictionary<int, Entity>();
            int id;
            do
            {
                id = (int)(UnityEngine.Random.value * int.MaxValue);  // idk if the safety minuses are neccessary
            } while (localEntities.ContainsKey(id));
            return id;
        }

        public static int getUniqueServerIdentifier()
        {
            if (serverEntities == null)
                serverEntities = new Dictionary<int, Entity>();
            int id;
            do
            {
                id = (int)(UnityEngine.Random.value * int.MaxValue);  // idk if the safety minuses are neccessary
            } while (serverEntities.ContainsKey(id));
            return id;
        }

        public void setCurrents(object _animatedComponent, GameObject _gameObject, bool _IsOwner)
        {
            current_AnimatedComponent = _animatedComponent;
            current_gameObject = _gameObject;
            current_IsOwner = _IsOwner;
        }

        // todo localPrefabId
        // todo syncedPrefabId

        public void server_generatePropertyIDs()
        {
            // todo
        }

        //public void Update()
        //{
        //    if (parentScript == null)
        //    {
        //        Debug.Log("Entity parentScript is null! Destroying entity");
        //        for (int i = 0; i < properties.Count; i++)
        //        {
        //            //Destroy(properties[i]);
        //            SyncedProperty.SyncedProperties.Remove(properties[i].identifier);
        //        }
        //        //Destroy(this);
        //        serverEntities.Remove(serverEntityId);
        //    }
        //}

        /// <summary>
        /// Add all the properties your script wants to sync to this entity; script variables/methods, attached gameobject transforms and components, etc.
        /// The order of this list will be used to sync properties across all networked instances of this entity.
        /// Uses current_ etc etc values; MAKE SURE THEY'RE CORRECT!
        /// </summary>
        /// <param name="propertyOrField"></param>
        public SyncedProperty addSyncedProperty(object propertyOrField)
        {
            if (instance.debug)
                print(propertyOrField.ToString());
            SyncedProperty prop = new SyncedProperty(SyncedProperty.invalidIdentifier, current_AnimatedComponent,
                propertyOrField, current_gameObject, Multi.instance.clip, current_IsOwner);
            properties.Add(prop);
            //properties.Add(new SyncedProperty(SyncedProperty.invalidIdentifier, current_AnimatedComponent,
            //    propertyOrField, current_gameObject, Multi.instance.clip, current_IsOwner));
            // TODO some sort of check to see if the current component/GO values match up? Or just use those derived values entirely?
            return prop;
        }

        /// <summary>
        /// Returns a SyncedProperty which is how you call the method over the network now!
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public SyncedProperty addSyncedMethodCall(string methodName, object[] parameters)
        {
            SyncedProperty prop = new SyncedProperty(SyncedProperty.invalidIdentifier, (Component)current_AnimatedComponent,
            methodName, parameters, Multi.instance.clip, current_IsOwner);
            properties.Add(prop);

            return prop;
        }




        // 
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            // these calls handle both sending/serializing data and receiving/deserializing data

            //if (serializer.IsWriter)
            //serializer.SerializeValue(ref local_entityID);   

            serializer.SerializeValue(ref ownerClientID);

            serializer.SerializeValue(ref localPrefabId);

            serializer.SerializeValue(ref local_entityID);   

            serializer.SerializeValue(ref serverSendToClients);

            if (serializer.IsWriter)
            {
                if (!serverSendToClients)   // first, the spawning client sends the number of properties to the server
                    propertiesCount = properties.Count;
                //if (serverSendToClients && serializer.IsWriter)
                if (serverSendToClients)    // then the server sends the IDs list back
                    propertiesCount = PropertyIDs.Count;
            }

            serializer.SerializeValue(ref propertiesCount);


            if (serverSendToClients)    // second, the server sends the full list of property IDs (and syncedPrefabId) to all clients
            {
                //if (serializer.IsWriter)
                serializer.SerializeValue(ref syncedPrefabId);

                serializer.SerializeValue(ref serverEntityId);


                if (serializer.IsReader)
                    PropertyIDs = new List<int>();

                for (int i = 0; i < propertiesCount; i++)
                {
                    int id = 0;
                    if (serializer.IsWriter)
                        id = PropertyIDs[i];

                    serializer.SerializeValue(ref id);
                    
                    if (serializer.IsReader)
                        PropertyIDs.Add(id);
                }
                // TODO will have to spawn this prefab on other clients when it's received (only the spawning client currently has it)
            }




            //if (serializer.IsWriter)
            //    if (properties != null && properties.Count > 0) // will be null on server side
            //    propertiesCount = properties.Count;
            //serializer.SerializeValue(ref propertiesCount);

            ////for PropertyIDs
            //int count = 0;
            //if (serializer.IsReader)
            //{
            //    // Deserialize
            //    serializer.SerializeValue(ref count);
            //    PropertyIDs = new List<int>(count);
            //    for (int i = 0; i < count; i++)
            //    {
            //        int value = 0;
            //        serializer.SerializeValue(ref value);
            //        PropertyIDs.Add(value);
            //    }
            //}
            //else
            //{
            //    // hope one of these is set lol
            //    if (properties != null)
            //        count = properties.Count;
            //    if (PropertyIDs != null)
            //    {
            //        count = PropertyIDs.Count;

            //        // Serialize
            //        serializer.SerializeValue(ref count);
            //        for (int i = 0; i < count; i++)
            //        {
            //            int value = PropertyIDs[i];
            //            serializer.SerializeValue(ref value);
            //        }
            //    }
            //}

        }
    }






    // SYNCING GAMEPLAY:



    /// <summary>
    /// Because you can't do network RPCs in classes that don't fucking inherit from NetworkBehaviour, and those are hard/expensive to spawn
    /// </summary>
    /// <param name="value"></param>
    public static void syncSyncedProperty(int identifier, NetData value)
    {
        instance.syncParam_ServerRpc(identifier, value);
    }
    //[ServerRpc]
    //[ServerRpc(RequireOwnership = false)]   // Note: This singleton class always runs as !IsOwner on non server clients, for some reason.
    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Unreliable)]   // Note: This singleton class always runs as !IsOwner on non server clients, for some reason.
    public void syncParam_ServerRpc(int identifier, NetData data, ServerRpcParams pars = default)
    //public void syncParam_ServerRpc(SyncedProperty data, ServerRpcParams pars = default)
    {
        //data.setDataType(42.GetType());
        var clientId = pars.Receive.SenderClientId;
        //print(clientId + " pinged the server with " + data.ToString());
        //print(clientId + " pinged the server with " + data.ToString() + " data " + data.GetData().ToString());
        //print(clientId + " pinged the server with " + data.getCurrentValue().ToString());

        if (debug)
            print(clientId + " pinged the server with " + data.GetData().ToString());


        syncParam_ClientRpc(identifier, data, clientId);  // send to all clients
    }


    //[ClientRpc]
    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void syncParam_ClientRpc(int identifier, NetData data, ulong originalSender, ClientRpcParams pars = default)
    //public void syncParam_ClientRpc(SyncedProperty data, ulong originalSender, ClientRpcParams pars = default)
    {
        var thisClientId = NetworkManager.Singleton.LocalClientId;

        if (originalSender == thisClientId)
        {
            if (debug)
                print("syncParam received from self; ignoring");
            return;
        }

        // won't be able to sync if the host hasn't sent back a property ID yet
        SyncedProperty localProp;
        if (SyncedProperty.SyncedProperties.ContainsKey(identifier))    // TODO should make keep some counter of the number of misses each frame
        {
            localProp = SyncedProperty.SyncedProperties[identifier];
        }
        else
            return;


        if (!(localProp.obj is MethodInfo)) {
            localProp.netData = data;   // idk if this helps
            localProp.setCurrentValue(data.GetData());
        } else { 
            print("Calling method replicated over the network, with "+ ((object[])data.GetData()).Length + " parameters");  // kinda expensive print?
            localProp.callMethod_LocalClientOnly((object[])data.GetData());
        }


        //print("Server pinged client " + thisClientId + " (originally from client " + originalSender + ") with " + data.getCurrentValue().ToString());
        if (debug)
            print("Server pinged client " + thisClientId + " (originally from client " + originalSender + ") with " + data.GetData().ToString());
    }







    /// <summary>
    /// Multiplayer synced and recorded/played-back property, for animating a variable or method in another script
    /// (Doesn't seem to work when you make one for a variable in the current script, only other scripts? Can't read its data? Idk. TODO, test more)
    /// </summary>
    public class SyncedProperty : Record.AnimatedProperty
    {

        // TODO have the server make sure this dict is synced up sometimes
        /// <summary>
        /// All synced properties (that have unique IDs back from the server)
        /// </summary>
        public static Dictionary<int, SyncedProperty> SyncedProperties;
        /// <summary>
        /// A code unique to this property, the same across all its instances on all clients, for network syncing
        /// </summary>
        public int identifier;
        /// <summary>
        /// Can't delete in a foreach loop, have to go over them after
        /// </summary>
        public static List<int> SyncedPropertiesToDelete;

        /// <summary>
        /// Will avoid adding SyncedProperties with this ID to the dictionary (like if it's waiting on an ID from the server). Make sure to add it later
        /// </summary>
        public const int invalidIdentifier = int.MinValue;

        ///// <summary>
        ///// A client side list of all syncedProperties by their synced-variable/function etc (ie propertyOrField) reference, for easy access
        ///// </summary>
        //public static Dictionary<object, SyncedProperty> getLocalSyncedProperty;


        /// <summary>
        /// True if, on this client machine, this machine 'owns' it (like it's a player model or something)
        /// </summary>
        public bool IsOwner;
        /// <summary>
        /// Datatype used in networking. See TypeToInt class for types
        /// </summary>
        private int _dataType = 0;

        /// <summary>
        /// Disable to stop syncing, for now
        /// </summary>
        public bool syncingEnabled = true;



        /// <summary>
        /// Have to send data within NedData's, because the receiving RPC rebuilds the object from scratch. 
        /// So sending a SyncedProperty directly doesn't work, since the new one has no references. (Also, super wasteful)
        /// </summary>
        public NetData netData;





        // god I fucking hate C# constructor chaining I want to slap the bitch that decided you can't call them from within the constructor body
        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="_identifier"></param>
        /// <param name="_animatedObject"></param>
        /// <param name="propertyOrField"></param>
        /// <param name="_gameObject"></param>
        /// <param name="_clip"></param>
        /// <param name="isOwner"></param>
        public SyncedProperty(int _identifier, object _animatedObject, object propertyOrField, GameObject _gameObject, Record.Clip _clip, bool isOwner) : base(_animatedObject, propertyOrField, _gameObject, _clip)
        {
            //constructor(_identifier, _animatedObject, propertyOrField, _gameObject, _clip, isOwner);
            constructor(_identifier, isOwner, obj);
        }

        /// <summary>
        /// For adding method calls
        /// </summary>
        public SyncedProperty(int _identifier, Component script, string methodName, object[] parameters, Record.Clip _clip, bool isOwner) : base(script, methodName, parameters, _clip)
        {
            constructor(_identifier, isOwner, obj);
        }

        /// <summary>
        /// Make sure to only call this on one client, so you don't double up effects!
        /// </summary>
        /// <param name="parameters"></param>
        public void callMethod(object[] parameters)
        {
            // TODO

            callMethod_LocalClientOnly(parameters);


            // sync across network
            // half copy pasted from the sync() function tbh
            if (netData == null)
                netData = new NetData(parameters);
            else
                netData.setData(parameters);

            //SmashMulti.syncSyncedProperty(netData);
            Multi.syncSyncedProperty(identifier, netData);
        }

        /// <summary>
        /// Used by networking system to do method calls it's received over the net
        /// </summary>
        /// <param name="parameters"></param>
        public void callMethod_LocalClientOnly(object[] parameters)
        {
            if (instance.debug)
            {
                print("method call: " + ((MethodInfo)obj).ToString() + ", parameters " + parameters.Length);
                foreach (object o in parameters)
                {
                    print("parameter " + o.ToString());
                }
            }

            ((MethodInfo)obj).Invoke(animatedComponent, parameters);


        }



        //public SyncedProperty(object _obj, GameObject _gameObject, Record.Clip _clip, bool isOwner) : base(_obj, _gameObject, _clip)
        /// <summary>
        /// generates new SyncedProperty ID
        /// </summary>
        /// <param name="_animatedObject"></param>
        /// <param name="propertyOrField"></param>
        /// <param name="_gameObject"></param>
        /// <param name="_clip"></param>
        /// <param name="isOwner"></param>
        public SyncedProperty(object _animatedObject, object propertyOrField, GameObject _gameObject, Record.Clip _clip, bool isOwner) : base(_animatedObject, propertyOrField, _gameObject, _clip)
        {
            //constructor(getUniqueIdentifier(), _animatedObject, propertyOrField, _gameObject, _clip, isOwner);
            constructor(getUniqueIdentifier(), isOwner, obj);
        }

        //void constructor(int _identifier, object _animatedObject, object propertyOrField, GameObject _gameObject, Record.Clip _clip, bool isOwner)
        void constructor(int _identifier, bool isOwner, object propertyOrField)
        {
            identifier = _identifier;
            //print("syncedProperty: " + propertyOrField + " identifier: " + identifier);
            IsOwner = isOwner;


            //if (IsOwner)
            //{
            if (SyncedProperties == null)
                SyncedProperties = new Dictionary<int, SyncedProperty>();
            //identifier = getUniqueIdentifier();

            //if (getLocalSyncedProperty == null)
            //    getLocalSyncedProperty = new Dictionary<object, SyncedProperty>();
            //print("propertyOrField " + propertyOrField + " " + propertyOrField.GetType());
            //if (getLocalSyncedProperty.ContainsKey(propertyOrField))
            //    Debug.LogError("getLocalSyncedProperty already contains this propertyOrField! " + propertyOrField.ToString() + " " + propertyOrField.GetType());
            //else
            //{
            //    print("getLocalSyncedProperty doesn't contain this propertyOrField! " + propertyOrField.ToString() + " " + propertyOrField.GetType());
            //    getLocalSyncedProperty.Add(propertyOrField, this);
            //}


            // TODO
            // RPC sync
            // trigger it to spawn new instances across other clients if not done so?
            // handle players having been spawned?

            if (identifier != invalidIdentifier)
                SyncedProperties.Add(identifier, this);
            //}
            // TODO


        }



        public static int getUniqueIdentifier()
        {
            if (SyncedProperties == null)
                SyncedProperties = new Dictionary<int, SyncedProperty>();
            int id;
            do
            {
                id = (int)(UnityEngine.Random.value * (2f * (int.MaxValue - 1)) - (int.MaxValue - 2));  // idk if the safety minuses are neccessary
            } while (SyncedProperties.ContainsKey(id));
            return id;
        }

        ///// <summary>
        ///// DON'T USE! Empty constructor required for INetworkSerializable. 
        ///// </summary>
        //public SyncedProperty()
        //{
        //}

        public void sync()
        {
            if (syncingEnabled)
            {
                // TODO check for dirtiness before syncing

                //if (IsOwner)

                if (obj is MethodInfo)
                    return;     // don't try and sync these every frame. Just wait for them to be called and replicate them then

                //object data = getCurrentValue();
                //if (data == null)
                //    return;

                if (gameObject == null) // only possible way to guess if the syncedProperty has been deleted? I've tried half a dozen other ways, false positives abound
                    return;

                if (netData == null)
                    netData = new NetData(getCurrentValue());
                else
                    netData.setData(getCurrentValue());

                
                // breaks half the properties!
                //if (netData.GetData() as UnityEngine.Object == null)    // weird null check, for deleted variables
                //{
                //    // Not sure of property is deleted or just hasn't been set yet! Just ignore it for now.
                //    // TODO maybe delete them based on their gameobjects being deleted..? Idk

                //    //Debug.Log("data is null! Deleting SyncedProperty");
                //    ////SyncedProperties.Remove(identifier);
                //    //if (SyncedPropertiesToDelete == null)
                //    //    SyncedPropertiesToDelete = new List<int>();
                //    //SyncedPropertiesToDelete.Add(identifier);
                //    return;
                //}


                //SmashMulti.syncSyncedProperty(netData);
                Multi.syncSyncedProperty(identifier, netData);
            }
        }
        /// <summary>
        /// Call this every frame after the sync() function, to delete any properties that were deleted this frame
        /// </summary>
        public static void deleteDeadProperties()
        {
            if (SyncedPropertiesToDelete == null)
                SyncedPropertiesToDelete = new List<int>();
            foreach (int id in SyncedPropertiesToDelete)
            {
                SyncedProperties.Remove(id);
            }
            SyncedPropertiesToDelete.Clear();
        }


        /// <summary>
        /// Get the id from the script variable reference in the scene
        /// </summary>
        /// <returns></returns>
        public object getCurrentValue()
        {
            // from Record.cs's "GenericFrame"
            object data = null;
            // none of these checks can tell if it's deleted or just hasn't been set yet
            //if (obj == null)
            //    print("destroyed1");
            //if ((obj as UnityEngine.Object) == null)
            //    print("Destroyed2");
            //if (ReferenceEquals(obj, null))
            //    print("Destroyed3");
            //if (!(obj is UnityEngine.Object))
            //    print("destroyed4");


            //if ((obj as UnityEngine.Object) == null)        // variable is either deleted or set to null, can't tell which
            //    return null;


            //try
            //{
            //if (property.obj is FieldInfo)
            if (obj is FieldInfo)
                {
                    //data = ((FieldInfo)property.obj).GetValue(property.animatedComponent);
                    data = ((FieldInfo)obj).GetValue(animatedComponent);
                }
                else if (obj is PropertyInfo)
                {
                    data = ((PropertyInfo)obj).GetValue(animatedComponent);
                }
                else if (obj is Transform)
                {
                    //print("transform");
                    data = obj;
                }
                else if (obj is MethodInfo)
                {
                    // do nothing; methods get synced upon being called
                }
                else
                {
                    Debug.LogError("unknown type");
                }
            //
            //catch (MissingReferenceException)     // unreliable; never catches the exception
            //{
            //    //Debug.LogError("getCurrentValue error: " + e.ToString());
            //    print("SyncedProperty has been deleted!");
            //    if (SyncedPropertiesToDelete == null)
            //        SyncedPropertiesToDelete = new List<int>();       // TODO, figure out how to delete dead properties later, free up their IDs
            //    SyncedPropertiesToDelete.Add(identifier);
            //}
            return data;
        }

        /// <summary>
        /// Set the id to the script variable referenced in the scene
        /// </summary>
        public void setCurrentValue(object data)
        {
            // from Record.cs's "GenericFrame"
            //if (property.obj is FieldInfo)
            if (obj is FieldInfo)
            {
                ((FieldInfo)obj).SetValue(animatedComponent, data);
            }
            else if (obj is PropertyInfo)
            {
                ((PropertyInfo)obj).SetValue(animatedComponent, data);
            }
            // TODO transforms?
            else if (obj is Transform)
            {
                Transform tf = (Transform)obj;

                tf.position = ((Transform)data).position;
                tf.rotation = ((Transform)data).rotation;
                tf.localScale = ((Transform)data).localScale;

                if (instance.debug)
                    print("transform! Local " + ((Transform)obj).position +" "+ ((Transform)obj).rotation);
                if (instance.debug)
                    print("transform! DATA " + ((Transform)data).position +" "+ ((Transform)data).rotation);
            }
            else if (obj is MethodInfo)
            {
                // do nothing
            }
            else
            {
                Debug.LogError("unknown type");
            }
        }



        //public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        //{
        //    object _data = null;

        //    if (serializer.IsWriter)
        //    {
        //        _data = getCurrentValue();
        //        if (_dataType == 0)
        //            _dataType = TypeToInt.Int(_data.GetType());
        //    }

        //    //if (serializer.IsWriter)
        //    //{
        //    serializer.SerializeValue(ref _dataType);   // these calls handle both sending/serializing data and receiving/deserializing data
        //    switch (_dataType)
        //    {
        //        case int when _dataType == TypeToInt.Int(typeof(int)):
        //            int intData = 0;
        //            if (serializer.IsWriter)
        //                intData = (int)_data;
        //            serializer.SerializeValue(ref intData);
        //            _data = intData;    // if receiving data, update the local copy
        //            break;

        //        case int when _dataType == TypeToInt.Int(typeof(float)):
        //            float floatData = 0;
        //            if (serializer.IsWriter)
        //                floatData = (float)_data;
        //            serializer.SerializeValue(ref floatData);
        //            _data = floatData;
        //            break;

        //        case int when _dataType == TypeToInt.Int(typeof(string)):
        //            string stringData = "";
        //            if (serializer.IsWriter)
        //                stringData = (string)_data;
        //            serializer.SerializeValue(ref stringData);
        //            _data = stringData;
        //            break;

        //        case int when _dataType == TypeToInt.Int(typeof(Vector3)):
        //            Vector3 vector3Data = Vector3.zero;
        //            if (serializer.IsWriter)
        //                vector3Data = (Vector3)_data;
        //            serializer.SerializeValue(ref vector3Data);
        //            _data = vector3Data;
        //            break;

        //        case int when _dataType == TypeToInt.Int(typeof(Transform)):
        //            Transform transformData = SmashMulti.instance.transform;    // just need a random non-null id
        //            if (serializer.IsWriter)
        //                transformData = (Transform)_data;
        //            Vector3 position = transformData.position;
        //            Quaternion rotation = transformData.rotation;
        //            Vector3 scale = transformData.localScale;
        //            serializer.SerializeValue(ref position);
        //            serializer.SerializeValue(ref rotation);
        //            serializer.SerializeValue(ref scale);
        //            // receiving data
        //            transformData.position = position;
        //            transformData.rotation = rotation;
        //            transformData.localScale = scale;
        //            _data = transformData;
        //            break;

        //        // ... add other types similarly

        //        default:
        //            // Handle the case where the type is not recognized.
        //            // This could be an error or a default serialization logic.
        //            Debug.LogError("Type not recognized: Type: " + _dataType + " data: " + _data);
        //            break;
        //    }

        //    if (serializer.IsReader)
        //        setCurrentValue(_data);
        //}


    }




    /// <summary>
    /// For passing variable Types over the network with minimal bandwidth. Speedy, uses IFs/switches
    /// </summary>
    public static class TypeToInt
    {
        // speedy lookups
        // TODO why did GPT 4 do it 2 different ways?
        public static int Int(Type type)
        {
            if (type == typeof(int)) return 1;
            if (type == typeof(float)) return 2;
            if (type == typeof(double)) return 3;
            if (type == typeof(string)) return 4;
            if (type == typeof(Vector3)) return 5;
            if (type == typeof(Transform)) return 6;
            if (type == typeof(Entity)) return 7;
            if (type == typeof(bool)) return 8;
            if (type == typeof(Color)) return 9;
            if (type == typeof(ulong)) return 10;
            // ... Add other types as needed

            throw new ArgumentException("Unsupported type: " + type);
        }

        /// <summary>
        /// Write like this: dict.GetTypeCode(typeof(float))  or like this dict.GetTypeCode(typeof(float))
        /// </summary>
        public static Type Type(int code)
        {
            switch (code)
            {
                case 1: return typeof(int);
                case 2: return typeof(float);
                case 3: return typeof(double);
                case 4: return typeof(string);
                case 5: return typeof(Vector3);
                case 6: return typeof(Transform);
                case 7: return typeof(Entity);
                case 8: return typeof(bool);
                case 9: return typeof(Color);
                case 10: return typeof(ulong);
                // ... Add other types as needed

                default: throw new ArgumentException("Unsupported code: " + code);
            }
        }

    }

    /// <summary>
    /// A class for sending generic data through unity RPC calls. (Unity's RPCs don't support generics)
    /// </summary>
    public class NetData : INetworkSerializable
    {
        // GPT 4 wrote a lot of this (though it made mistakes)
        private object _data = null;
        //private Type _dataType;

        /// <summary>
        /// The most recently serialized data (sent or received), for recording gameplay
        /// </summary>
        byte[] byteArray;

        private int _dataType = 0;
        private bool isList = false;
        private bool isObjectList = false;

        public NetData()
        {
            // Empty constructor required for INetworkSerializable
        }

        public NetData(object data)
        {
            setData(data);
        }

        public object GetData()
        { return _data; }

        public void setData(object data)
        {
            //_data = data; 

            _data = data;
            if (data is object[])
            {
                isObjectList = true;
            } else if (data is IList)   // object[] arrays/lists will trigger this too, then cause an error lol
            {
                isList = true;
                _dataType = TypeToInt.Int(data.GetType().GetGenericArguments()[0]);
            }
            else
                _dataType = TypeToInt.Int(data.GetType());
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            // unsafe      // Capture all the networking data in a byte array, to be recorded to a file for playback
            // {
            //     byte* ptrStart = null;
            //     FastBufferReader reader;
            //     if (serializer.IsReader)
            //     {
            //         reader = serializer.GetFastBufferReader();
            //         //print("reader " + reader.GetUnsafePtrAtCurrentPosition())
            //         //unsafe
            //         //{
            //         ptrStart = reader.GetUnsafePtrAtCurrentPosition();
            //         // Your code using the pointer
            //         //}
            //     }



            // these calls handle both sending/serializing data and receiving/deserializing data
            serializer.SerializeValue(ref isList);   
            serializer.SerializeValue(ref isObjectList); 
            serializer.SerializeValue(ref _dataType);


            if (isObjectList)
            {
                // Handle object array serialization and deserialization
                object[] dataArray = null;
                int arrayLength = 0;

                if (serializer.IsWriter)
                {
                    dataArray = (object[])_data;
                    arrayLength = dataArray.Length;
                }
                serializer.SerializeValue(ref arrayLength);

                if (serializer.IsReader)
                    dataArray = new object[arrayLength];


                for (int i = 0; i < arrayLength; i++)
                {
                    int objType = 0;
                    object item = null;

                    if (serializer.IsWriter)
                    {
                        item = dataArray[i];
                        //print("item.GetType() " + item.GetType());
                        objType = TypeToInt.Int(item.GetType());
                    }
                    serializer.SerializeValue(ref objType);

                    if (serializer.IsReader)
                        item = Activator.CreateInstance(TypeToInt.Type(objType));

                    item = serializeVariable(item, objType, serializer);

                    if (serializer.IsReader)
                        dataArray[i] = item;
                }

                if (serializer.IsReader)
                {
                    _data = dataArray;
                }
            }
            else if (isList)
            {
                int listCount = 0;
                if (serializer.IsWriter)
                    listCount = ((IList)_data).Count;
                serializer.SerializeValue(ref listCount);
                if (serializer.IsReader)
                    _data = Activator.CreateInstance(typeof(List<>).MakeGenericType(TypeToInt.Type(_dataType)), listCount);

                // do list
                // Get the type of the items in the list
                Type itemType = TypeToInt.Type(_dataType);

                // Cast the _data to an IList for easier manipulation
                IList dataList = (IList)_data; 

                if (instance.debug)
                    print("Serializing list with " + listCount + " IsWriter " + serializer.IsWriter);

                for (int i = 0; i < listCount; i++)
                {

                    // Serialize each item in the list
                    //object item;
                    //if (serializer.IsWriter)
                    //    item = serializeVariable(dataList[i], _dataType, serializer);
                    //else
                    //    item = Activator.CreateInstance(itemType);

                    object item = Activator.CreateInstance(itemType);
                    if (serializer.IsWriter)
                        item = dataList[i];

                    //serializer.SerializeValue(ref item, itemType);
                    //serializer.SerializeValue(ref item);
                    item = serializeVariable(item, _dataType, serializer);

                    if (instance.debug)
                    {
                        print("item of type " + TypeToInt.Type(_dataType));
                        if (_dataType == 7)
                            print("is entity; local_entityID " + ((Entity)item).local_entityID);
                    }

                    if (serializer.IsReader)
                        dataList.Add(item);
                }
                    //else
                    //{
                    //    // Deserialize each item and add it to the list
                    //    object item = Activator.CreateInstance(itemType);
                    //    serializer.SerializeValue(ref item, itemType);
                    //}
                //}
            }
            else
            {
                _data = serializeVariable(_data, _dataType, serializer);
            }


                // if (serializer.IsReader)
                // {
                //     byte* ptrEnd = reader.GetUnsafePtrAtCurrentPosition();

                //     int size = (int)(ptrEnd - ptrStart);
                //     //print("_dataType " + TypeToInt.Type(_dataType).ToString() + " data " + _data + " size " + size);

                //     byteArray = new byte[size];
                //     //System.Runtime.InteropServices.Marshal.Copy((IntPtr)ptrEnd, byteArray, 0, size);
                //     System.Runtime.InteropServices.Marshal.Copy((IntPtr)ptrStart, byteArray, 0, size);

                //     string hexString = BitConverter.ToString(byteArray);
                //     //print(hexString);

                //     if (_dataType== TypeToInt.Int(typeof(Entity)))
                //     {
                        
                //         print("Saved data");
                //         //Multi.instance.testRec.byteList = (List<byte>)_data;
                //         Multi.instance.testRec.byteArray = byteArray;
                //         Multi.instance.testRec.SaveToFile(); 
                //     }
                // }


            //}   //  /unsafe
        }

        /// <summary>
        /// sends and receives arbitrary variables/objects (but not lists)
        /// </summary>
        object serializeVariable<T>(object _data, int _dataType, BufferSerializer<T> serializer) where T : IReaderWriter
        {
            switch (_dataType)
            {
                case int when _dataType == TypeToInt.Int(typeof(int)):
                    int intData = 0;
                    if (serializer.IsWriter)
                        intData = (int)_data;
                    serializer.SerializeValue(ref intData);
                    _data = intData;    // if receiving data, update the local copy
                    break;

                case int when _dataType == TypeToInt.Int(typeof(float)):
                    float floatData = 0;
                    if (serializer.IsWriter)
                        floatData = (float)_data;
                    serializer.SerializeValue(ref floatData);
                    _data = floatData;
                    break;

                case int when _dataType == TypeToInt.Int(typeof(string)):
                    string stringData = "";
                    if (serializer.IsWriter)
                        stringData = (string)_data;
                    serializer.SerializeValue(ref stringData);
                    _data = stringData;
                    break;

                case int when _dataType == TypeToInt.Int(typeof(Vector3)):
                    Vector3 vector3Data = Vector3.zero;
                    if (serializer.IsWriter)
                        vector3Data = (Vector3)_data;
                    serializer.SerializeValue(ref vector3Data);
                    _data = vector3Data;
                    break;

                case int when _dataType == TypeToInt.Int(typeof(Transform)):
                    Transform transformData = SmashMulti.instance.transform;    // just need a random non-null id
                    if (serializer.IsWriter)
                        transformData = (Transform)_data;
                    Vector3 position = transformData.position;
                    Quaternion rotation = transformData.rotation;
                    Vector3 scale = transformData.localScale;
                    serializer.SerializeValue(ref position);
                    serializer.SerializeValue(ref rotation);
                    serializer.SerializeValue(ref scale);
                    // receiving data
                    transformData.position = position;
                    transformData.rotation = rotation;
                    transformData.localScale = scale;
                    _data = transformData;
                    break;

                case int when _dataType == TypeToInt.Int(typeof(Entity)):
                    Entity entityData;
                    if (serializer.IsWriter)
                        entityData = (Entity)_data;
                    else
                        entityData = new Entity();
                    serializer.SerializeValue(ref entityData);
                    // receiving data
                    _data = entityData;
                    break;

                case int when _dataType == TypeToInt.Int(typeof(bool)):
                    bool boolData = false;
                    if (serializer.IsWriter)
                        boolData = (bool)_data;
                    serializer.SerializeValue(ref boolData);
                    _data = boolData;
                    break;

                case int when _dataType == TypeToInt.Int(typeof(Color)):
                    Color colorData = Color.red;
                    if (serializer.IsWriter)
                        colorData = (Color)_data;
                    serializer.SerializeValue(ref colorData);
                    _data = colorData;
                    break;

                case int when _dataType == TypeToInt.Int(typeof(ulong)):
                    ulong ulongData = 0;
                    if (serializer.IsWriter)
                        ulongData = (ulong)_data;
                    serializer.SerializeValue(ref ulongData);
                    _data = ulongData;
                    break;

                // ... add other types similarly

                default:
                    // Handle the case where the type is not recognized.
                    // This could be an error or a default serialization logic.
                    Debug.LogError("Type not recognized: Type: " + _dataType + " data: " + _data);
                    break;
            }
            return _data;
        }   
    }

}








/// <summary>
/// REPLACEMENT for unity NetworkBehaviour (since I need to set IsOwner manually on manual spawns, and they locked that off, f*ckers)
/// </summary>
public abstract class NetBehaviour : MonoBehaviour
{
    /// <summary>
    /// Whether to run game logic, or just let the multiplayer or recording-playback system control this intance
    /// </summary>
    public bool IsSimulating;

    public bool IsOwner;
    public bool IsServer;

    /// <summary>
    /// Equivalent to 'gameObject.GetComponent<NetworkObject>().OwnerClientId' in unity; if a client owns these objects, like say a player model,
    /// then this is the client's ID (equivalent to NetworkManager.Singleton.LocalClientId on the IsOwner side) 
    /// </summary>
    public ulong OwnerClientId;   // TODO this is normally aa feature of ''.
    //Set it myself. Is the 'NetworkManager.Singleton.LocalClientId' on the IsOwner side


    public abstract void OnNetworkSpawn();


}






/// <summary>
/// Adds recording/playback functionality to the Multi.cs multiplayer system, reusing its serialization system.
/// 2023 remake of Record.cs gameplay recording/playback system (from the OutdoorPacmanVR game).
/// Inspired by unity's animation system; Clips hold AnimatedProperties hold frames, etc. Except I'm doing away with Clips for the most part, they suck.
/// </summary>
public class Rec
{
    //public List<byte> byteList = new List<byte>();
    public byte[] byteArray;
    public string filePath = Path.Combine(Application.persistentDataPath, "smashRecording.dat");

    // Method to save the byte list to a file
    public void SaveToFile()
    {
        // Convert the list of bytes to an array for writing
        //byte[] byteArray = byteList.ToArray();
        //byte[] byteArray = byteList;

        // Write the byte array to the file
        File.WriteAllBytes(filePath, byteArray);

        Debug.Log($"Saved recording to {filePath}");
    }

    // Method to load the bytes from a file
    public void LoadFromFile()
    {
        // Check if the file exists before trying to read
        if (File.Exists(filePath))
        {
            // Read the bytes from the file
            byte[] byteArray = File.ReadAllBytes(filePath);

            // Clear the current list and add the loaded bytes
            //byteList.Clear();
            //byteList.AddRange(byteArray);
            //byteList = byteArray;

            Debug.Log($"Loaded recording from {filePath}");
        }
        else
        {
            Debug.LogWarning($"File not found: {filePath}");
        }
    }

}

