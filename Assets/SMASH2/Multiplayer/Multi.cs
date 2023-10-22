using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Netcode;
using Unity.VisualScripting.FullSerializer;
using UnityEditor.PackageManager;
using UnityEngine;



/// <summary>
/// Generic code for multiplayer and recording/playback of gameplay. This is the only script that should be networked (hopefully, we'll see)
/// Use for all networked variables and functions.
/// Note: Can only sync public variables and methods
/// </summary>
public class Multi : NetworkBehaviour
{
    public static Multi instance;

    /// <summary>
    /// All prefab instances that have server IDs (ie the same on all clients)
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


    public override void OnNetworkSpawn()
    {
        if (instance != null)
            Debug.LogError("More than one Multi singleton in scene");
        instance = this;

        clip = new Record.Clip("SmashClip " + UnityEngine.Random.value * 1000);

        syncedPrefabs = new Dictionary<int, GameObject>();
    }



    // Update is called once per frame
    void Update()
    {
        
    }

    private void LateUpdate()
    {
        

        if (SyncedProperty.SyncedProperties == null)
            return;
        // Add sync calls scripts can use. (syncProperty(ref variable or method) etc)
        foreach (SyncedProperty prop in SyncedProperty.SyncedProperties.Values)
        {
            if (prop.IsOwner)
                prop.sync();
        }
    }



    // netspawnprefab
    // - finds all entities and tags then with a global prefab code
    // - tells the server to tell all clients to spawn the prefab
    //   -server comes up with unique SyncedProperty IDs for each property in the prefab, and sends them to all clients (including the original spawner)
    //   -The clients assign those IDs using their entity's property lists, to sync all properties of this prefab across the network
    // (Also add the SyncedProperties to their dictionary once they have an ID)

    // first spawns a local copy on client, then tells server and gets IDs back

    public static GameObject netSpawnPrefab_ToServer(GameObject prefab, bool IsOwner)
    {
        GameObject c = null;
        if (prefab)
            c = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        else
            Debug.LogError("Prefab is not assigned!");

        //if (instance.IsServer)
        //{
        //    instance.syncedPrefabs.Add(0, c);
        //}

        // setup netbehaviours
        List<NetBehaviour> netBehaviours = GetAllNetBehavioursInPrefab(c);
        foreach (var netBehaviour in netBehaviours)
        {
            netBehaviour.OnNetworkSpawn();
            netBehaviour.IsOwner = IsOwner;
            netBehaviour.IsServer = instance.IsServer;
        }

        // give entities temp local-client codes, before the server gives them real ones
        List<Multi.Entity> prefabEntities = getAllEntitysInPrefab(c);
        foreach (var entity in prefabEntities)
        {
            //entity.syncedPrefabId = (int)(UnityEngine.Random.value * int.MinValue);
            //instance.syncedPrefabs.Add(entity.syncedPrefabId, prefab);

            entity.localPrefabId = getLocalPrefabID();
            instance.localPrefabs.Add(entity.localPrefabId, prefab);
        }

        var clientId = NetworkManager.Singleton.LocalClientId;

        print("Spawning Client (ID: " + clientId + "): netSpawnPrefab_ToServer complete for: " + prefab.name + 
            ", sent " + prefabEntities.Count + " entity to the server for global IDs");

        //instance.netSpawnPrefab_ServerRpc(prefabEntities);
        NetData data = new NetData(prefabEntities);
        instance.netSpawnPrefab_ServerRpc(data);    // one call per prefab, not per entity

        return c;
    }







    /// <summary>
    /// Called by the spawning client, runs on the server to generate global IDs and sync them to all clients
    /// </summary>
    /// <param name="data"></param>
    /// <param name="pars"></param>
    [ServerRpc(RequireOwnership = false)]   // Note: This singleton class always runs as !IsOwner on non server clients, for some reason.
    void netSpawnPrefab_ServerRpc(NetData data, ServerRpcParams pars = default)
    {
        ulong clientId = pars.Receive.SenderClientId;

        List<Multi.Entity> prefabEntities = (List<Multi.Entity>)data.GetData();

        print("Server: " + clientId + " pinged the server with " + prefabEntities.Count + " entities for syncing.");


        int uniquePrefabID = getSyncedPrefabID();


        // TODO
        // -need to get the entity's info/lists to the server, via serializing ig..?

        // -give each entity its new global prefab ID (but need to save the old one so the spawning client can match them back up)
        // -give each SyncedProperty a global ID
        // -send the info back to all clients
        foreach (var entity in prefabEntities)
        {
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
                print(p + " " + entity.PropertyIDs[p]);
            }
            // assign entity back to list?
        }

        data.setData(prefabEntities);

        //netSpawnPrefab_ClientRpc(data, clientId);  // send to all clients
        netSpawnPrefab_ClientRpc(clientId, data);  // send to all clients
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
    public void netSpawnPrefab_ClientRpc(ulong originalSender, NetData data, ClientRpcParams pars = default)
    {
        // WARNING the datastream seems to be getting corrupted after the NetData?? TODO
        var thisClientId = NetworkManager.Singleton.LocalClientId;
        // TODO originalSender is broken! 0!

        List<Multi.Entity> prefabEntities = (List<Multi.Entity>)data.GetData();
        print("entities received " + prefabEntities.Count);
        foreach(Multi.Entity entity in prefabEntities)
        {
            // print all entity data
            print("Entity found, syncedPrefabId ID " + entity.syncedPrefabId + " localPrefabId " + entity.localPrefabId + ", has " +
                entity.propertiesCount + " properties, local_entityID " + entity.local_entityID + " serverEntityId " + entity.serverEntityId +
                "");
            foreach(int propertyID in entity.PropertyIDs)
            {
                print("propertyID " + propertyID);
            }
        }

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

        Debug.Log($"Found {foundNetworkBehaviours.Count} scripts inheriting from NetworkBehaviour in the prefab.");
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

        //// Set the prefabID for each found Multi.Entity
        //foreach (var entity in prefabEntities)
        //{
        //    entity.syncedPrefabId = 11;   // TODO
        //}

        Debug.Log($"Found {foundEntities.Count} instances of Multi.Entity in the prefab.");

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
        /// A unique, server genertated ID for an instance of the prefab holding this entity, across all client machines.
        /// </summary>
        public int syncedPrefabId;

        /// <summary>
        /// A temp code the spawning client comes up with, so it can tell which of its spawned prefabs the server is sending it real IDs for in a sec
        /// </summary>
        public int localPrefabId;



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
        public int propertiesCount;    // for serialization


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


        /// <summary>
        /// see class summary
        /// </summary>
        /// <param name="script_Or_AnimatedObject"></param>
        public Entity()
        {
            properties = new List<SyncedProperty>();
            local_entityID = getUniqueLocalIdentifier();
        }

        /// <summary>
        /// Register on the local list so when the server sends back an entity copy with global property IDs, we can actually link it to the original
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

        /// <summary>
        /// Add all the properties your script wants to sync to this entity; script variables/methods, attached gameobject transforms and components, etc.
        /// The order of this list will be used to sync properties across all networked instances of this entity.
        /// Uses current_ etc etc values; MAKE SURE THEY'RE CORRECT!
        /// </summary>
        /// <param name="propertyOrField"></param>
        public void addSyncedProperty(object propertyOrField)
        {
            print(propertyOrField.ToString());
            properties.Add(new SyncedProperty(SyncedProperty.invalidIdentifier, current_AnimatedComponent,
                propertyOrField, current_gameObject, Multi.instance.clip, current_IsOwner));
            // TODO some sort of check to see if the current component/GO values match up? Or just use those derived values entirely?
        }

        // 
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            // these calls handle both sending/serializing data and receiving/deserializing data

            //if (serializer.IsWriter)
            //serializer.SerializeValue(ref local_entityID);   

            serializer.SerializeValue(ref localPrefabId);

            serializer.SerializeValue(ref local_entityID);   

            serializer.SerializeValue(ref serverSendToClients);

            if (!serverSendToClients)   // first, the spawning client sends the number of properties to the server
                propertiesCount = properties.Count;

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










    /// <summary>
    /// Because you can't do network RPCs in classes that don't fucking inherit from NetworkBehaviour, and those are hard/expensive to spawn
    /// </summary>
    /// <param name="prop"></param>
    public static void syncSyncedProperty(NetData prop)
    {
        instance.pingServerRpc(prop);
    }
    //[ServerRpc]
    [ServerRpc(RequireOwnership = false)]   // Note: This singleton class always runs as !IsOwner on non server clients, for some reason.
    public void pingServerRpc(NetData data, ServerRpcParams pars = default)
    //public void pingServerRpc(SyncedProperty data, ServerRpcParams pars = default)
    {
        //data.setDataType(42.GetType());
        var clientId = pars.Receive.SenderClientId;
        //print(clientId + " pinged the server with " + data.ToString());
        //print(clientId + " pinged the server with " + data.ToString() + " data " + data.GetData().ToString());
        print(clientId + " pinged the server with " + data.GetData().ToString());
        //print(clientId + " pinged the server with " + data.getCurrentValue().ToString());

        pingClientRpc(data, clientId);  // send to all clients
    }


    [ClientRpc]
    public void pingClientRpc(NetData data, ulong originalSender, ClientRpcParams pars = default)
    //public void pingClientRpc(SyncedProperty data, ulong originalSender, ClientRpcParams pars = default)
    {
        var thisClientId = NetworkManager.Singleton.LocalClientId;
        print("Server pinged client " + thisClientId + " (originally from client " + originalSender + ") with " + data.GetData().ToString());
        //print("Server pinged client " + thisClientId + " (originally from client " + originalSender + ") with " + data.getCurrentValue().ToString());
    }







    /// <summary>
    /// Multiplayer synced and recorded/played-back property, for animating a variable or method in another script
    /// (Doesn't seem to work when you make one for a variable in the current script, only other scripts? Can't read its data? Idk. TODO, test more)
    /// </summary>
    public class SyncedProperty : Record.AnimatedProperty
    {

        // TODO have the server make sure this dict is synced up sometimes
        /// <summary>
        /// All synced properties (ints are unique identifiers)
        /// </summary>
        public static Dictionary<int, SyncedProperty> SyncedProperties;
        /// <summary>
        /// A code unique to this property, the same across all its instances on all clients, for network syncing
        /// </summary>
        public int identifier;

        /// <summary>
        /// Will avoid adding SyncedProperties with this ID to the dictionary (like if it's waiting on an ID from the server). Make sure to add it later
        /// </summary>
        public const int invalidIdentifier = int.MinValue;

        //NetworkVariable<T> netVar;

        //RPC // todo

        public bool IsOwner;
        /// <summary>
        /// Datatype used in networking. See TypeToInt class for types
        /// </summary>
        private int _dataType = 0;




        /// <summary>
        /// Have to send data within NedData's, because the receiving RPC rebuilds the object from scratch. 
        /// So sending a SyncedProperty directly doesn't work, since the new one has no references. (Also, super wasteful)
        /// </summary>
        public NetData netData;





        // god I fucking hate C# constructor chaining I want to slap the bitch that decided you can't call them from within the constructor body

        public SyncedProperty(int _identifier, object _animatedObject, object propertyOrField, GameObject _gameObject, Record.Clip _clip, bool isOwner) : base(_animatedObject, propertyOrField, _gameObject, _clip)
        {
            constructor(_identifier, _animatedObject, propertyOrField, _gameObject, _clip, isOwner);
        }



        //public SyncedProperty(object _obj, GameObject _gameObject, Record.Clip _clip, bool isOwner) : base(_obj, _gameObject, _clip)
        public SyncedProperty(object _animatedObject, object propertyOrField, GameObject _gameObject, Record.Clip _clip, bool isOwner) : base(_animatedObject, propertyOrField, _gameObject, _clip)
        {
            constructor(getUniqueIdentifier(), _animatedObject, propertyOrField, _gameObject, _clip, isOwner);
        }

        void constructor(int _identifier, object _animatedObject, object propertyOrField, GameObject _gameObject, Record.Clip _clip, bool isOwner)
        {
            identifier = _identifier;
            print("syncedProperty: " + propertyOrField + " identifier: " + identifier);
            IsOwner = isOwner;


            //if (IsOwner)
            //{
            if (SyncedProperties == null)
                SyncedProperties = new Dictionary<int, SyncedProperty>();
            //identifier = getUniqueIdentifier();

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
            // TODO check for dirtiness before syncing

            //if (IsOwner)

            if (netData == null)
                netData = new NetData(getCurrentValue());
            else
                netData.setData(getCurrentValue());

            //SmashMulti.syncSyncedProperty(netData);
            Multi.syncSyncedProperty(netData);

        }

        /// <summary>
        /// Get the id from the script variable reference in the scene
        /// </summary>
        /// <returns></returns>
        public object getCurrentValue()
        {
            // from Record.cs's "GenericFrame"
            object data = null;

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
            // TODO transforms?

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
            // ... Add other types as needed

            throw new ArgumentException("Unsupported type");
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
                // ... Add other types as needed

                default: throw new ArgumentException("Unsupported code");
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

        private int _dataType = 0;
        private bool isList = false;

        public NetData()
        {
            // Empty constructor required for INetworkSerializable
        }

        public NetData(object data)
        {
            _data = data;
            if (data is IList) 
            {
                isList = true;
                _dataType = TypeToInt.Int(data.GetType().GetGenericArguments()[0]);
            } else
                _dataType = TypeToInt.Int(data.GetType());
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            //if (serializer.IsWriter)
            //{
            serializer.SerializeValue(ref isList);   // these calls handle both sending/serializing data and receiving/deserializing data
            serializer.SerializeValue(ref _dataType);

            if (isList)
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

                for (int i = 0; i < listCount; i++)
                {

                    // Serialize each item in the list
                    object item;
                    if (serializer.IsWriter)
                        //item = dataList[i];
                        item = serializeVariable(dataList[i], _dataType, serializer);
                    else
                        item = Activator.CreateInstance(itemType);


                    //serializer.SerializeValue(ref item, itemType);
                    //serializer.SerializeValue(ref item);
                    item = serializeVariable(item, _dataType, serializer);

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

                // ... add other types similarly

                default:
                    // Handle the case where the type is not recognized.
                    // This could be an error or a default serialization logic.
                    Debug.LogError("Type not recognized: Type: " + _dataType + " data: " + _data);
                    break;
            }
            return _data;
        }


        public object GetData()
        { return _data; }

        public void setData(object data)
        { _data = data; }

        //public void setDataType(Type dataType)
        //{ _dataType = dataType; }
    }

}








//// TODO, use?
///// <summary>
///// Wrapper for NetworkBehaviour. Gives an isSimulating flag compatible with multiplayer and recording/playback.
///// Be sure to register your gameobjects with your game's multiplayer singleton!
//public abstract class NetBehaviour : NetworkBehaviour
//{
//    /// <summary>
//    /// Whether to run game logic, or just let the multiplayer or recording-playback system control this intance
//    /// </summary>
//    public bool isSimulating {  get; }


//}


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

    public abstract void OnNetworkSpawn();

}