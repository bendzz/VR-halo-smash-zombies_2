//using Kart;
//using Newtonsoft.Json.Linq;
//using System;
//using System.Collections;
using System.Collections.Generic;
//using System.Globalization;
//using System.Reflection;
using Unity.Netcode;
//using Unity.VisualScripting;
//using UnityEditor.UIElements;
using System.Reflection;

using UnityEngine;
using static Multi;
using static Record;
using System.Linq.Expressions;
using System;
//using Mono.Cecil.Cil;
//using static SmashMulti;


/// <summary>
/// Game specific code for multiplayer and recording/playback of gameplay. This should be all the game's multiplayer/recording code.
/// </summary>
//public class SmashMulti : MonoBehaviour

public class SmashMulti : NetworkBehaviour
{
    public static SmashMulti instance;

    [Header("VR setup (optional)")]
    public Transform XR_Origin;
    public Transform XR_Headset;

    public Transform leftHandObject;
    public Transform rightHandObject;

    List<XR_Dummies_Sync> dummyXR_Rigs;

    //List<SmashCharacter> smashCharacters;
    //Dictionary<string, SmashCharacter> smashCharacters;

    Dictionary<string, SmashCharacter> smashCharacters;


    // refs
    Multi multi;
    Clip clip;


    /// <summary>
    /// Spawned when a new player joins the scene
    /// </summary>
    public GameObject playerPrefab;



    void Awake()
    {
        if (instance != null)
            Debug.LogError("More than one SmashMulti in scene");
        instance = this;


    }

    public override void OnNetworkSpawn()
    {
        multi = Multi.instance;
        clip = multi.clip;

        clip = new Record.Clip("SmashGameClip " + UnityEngine.Random.value * 100000);


        dummyXR_Rigs = new List<XR_Dummies_Sync>();

        print("OnNetworkSpawn. IsHost " + IsHost + " IsServer " + IsServer + " IsClient " + IsClient + " IsOwner " + IsOwner + " IsOwnedByServer " + IsOwnedByServer);





        //spawnPlayer();
        GameObject c = Multi.netSpawnPrefab_ToServer(playerPrefab, true, NetworkManager.Singleton.LocalClientId);



    }





    //public void playerJoined(ulong clientId)
    //{

    //}

    //public void spawnPlayer()
    //{
    //    GameObject c;
    //    if (playerPrefab)
    //        c = Instantiate(playerPrefab, transform.position, Quaternion.identity);
    //    else
    //    {
    //        Debug.LogError("Player prefab is not assigned!"); return;
    //    }


    //    //// Tell the server to replicate it to clients
    //    //// create SyncedProperties for each player variable/method, with unique IDs, then pass those IDs in order to other clients to spawn and sync theirs

    //    ////make a string of IDs in order, and then pass those into a method to attach them to the player script variables, again in orde
    //    //List<int> paramIDs = new List<int>();
    //    //for (int i = 0; i < 3; i++)
    //    //{
    //    //    paramIDs.Add(SyncedProperty.getUniqueIdentifier());
    //    //}
    //    //addSyncedParametersToPlayer(c, paramIDs);
    //}

    ///// <summary>
    ///// 
    ///// </summary>
    ///// <param name="c">top level Character gameobject</param>
    ///// <param name="paramIDs">SyncedProperty unique IDs, enough for every parameter</param>
    //public void addSyncedParametersToPlayer(GameObject c, List<int> paramIDs)
    //{
    //    print("setting up character " + c);
    //    myInputTests inputTests = c.GetComponentInChildren<myInputTests>();
    //    SmashCharacter smashCharacter = c.GetComponentInChildren<SmashCharacter>(); 
    //    XR_Dummies_Sync xrDummiesSync = c.GetComponentInChildren<XR_Dummies_Sync>();
    //    FeetCollider feetCollider = c.GetComponentInChildren<FeetCollider>();

    //    if (inputTests == null) Debug.LogError("MyInputTests is missing!");
    //    if (smashCharacter == null) Debug.LogError("SmashCharacter is missing!");
    //    if (xrDummiesSync == null) Debug.LogError("XR_Dummies_Sync is missing!");
    //    if (feetCollider == null) Debug.LogError("FeetCollider is missing!");

    //    // link SyncedProperties to the player script variables, in order
    //    new Multi.SyncedProperty(paramIDs[0], smashCharacter, smashCharacter.hands[0].thruster, smashCharacter.gameObject, clip, smashCharacter.IsOwner);
    //    new Multi.SyncedProperty(paramIDs[1], smashCharacter, smashCharacter.hands[1].thruster, smashCharacter.gameObject, clip, smashCharacter.IsOwner);
    //    new Multi.SyncedProperty(paramIDs[2], smashCharacter, smashCharacter.transform, smashCharacter.gameObject, clip, smashCharacter.IsOwner);
    //}


    // pass spawn ID strings over the network in a list, for syncing the SyncedParamater IDs 



    private void Update()
    {


    }





    //public static void addXRRig(XR_Dummies_Sync rig)
    //{
    //    instance.dummyXR_Rigs.Add(rig);

    //    bool isOwner = rig.IsOwner;


    //    // todo replace with SyncedProperty. Not actually working atm
    //    addSyncedObject(rig.XR_Origin, isOwner);
    //    addSyncedObject(rig.XR_Headset.parent, isOwner);
    //    addSyncedObject(rig.XR_Headset, isOwner);
    //    addSyncedObject(rig.leftHandObject, isOwner);
    //    addSyncedObject(rig.rightHandObject, isOwner);

    //    // testing
    //    print("XR rig synced: " + rig.gameObject.name);



    //    //instance.testProperties.Add(new SyncedProperty(rig, rig.testFloat2, rig.gameObject, instance.clip, rig.IsOwner));
    //    //new Multi.SyncedProperty(rig, rig.testFloat2, rig.gameObject, instance.clip, rig.IsOwner);
    //}





    ///// <summary>
    ///// Called by player/AI player character script to let us know we have to set up their networking/recording properties.
    ///// Only runs on IsOwned scripts
    ///// </summary>
    //public static void characterSpawned(SmashCharacter c)
    //{
    //    print("Player spawned! Name " + c.playerName);

    //    if (instance.smashCharacters == null)
    //        //instance.smashCharacters = new List<SmashCharacter>();
    //        instance.smashCharacters = new Dictionary<string, SmashCharacter>();

    //    //instance.smashCharacters.Add(c);
    //    instance.smashCharacters.Add(c.PlayerId, c);



    //    if (c.IsOwner)
    //    {
    //        // ping the server to spawn syncedProperties on all clients with these IDs, and sync them to the local player copies
    //        //PlayerId

    //        foreach (SmashCharacter.Hand hand in c.hands)
    //        {
    //            //hand.thruster
    //            // spawn new syncedProperty on all clients, and have it be linked

    //            //string varName = GetFullVariableName(() => hand.thruster);
    //            //addSyncedProperty_Player(c, varName);

    //            //print(GetFullVariableName(() => hand.thruster));
    //            ////object result = FindVariable(varName, instance);
    //            //object result = FindVariable(varName, c);

    //            //print("result: " + GetFullVariableName(() => result));
    //            //setCurrentValue(c, result, .5f);
    //            //print("hand.thruster " + hand.thruster);
    //            //print("getCurrentValue " + getCurrentValue(c, result));
    //            //print("getCurrentValue " + getCurrentValue(c, hand.thruster));
    //            //print("getCurrentValue " + getCurrentValue(c, (object)hand.thruster));
    //            //print("result now " + result);
    //            ////print("result now " + ((float)result).ToString());
    //            ////print("result now " + ((float)result));
    //        }


    //    }
    //}

    // temp
    public static void setCurrentValue(object baseScript, object obj, object data)
    {
        // from Record.cs's "GenericFrame"
        //if (property.obj is FieldInfo)
        if (obj is FieldInfo)
        {
            ((FieldInfo)obj).SetValue(baseScript, data);
        }
        else if (obj is PropertyInfo)
        {
            ((PropertyInfo)obj).SetValue(baseScript, data);
        }
        // TODO transforms?
    }
    // temp
    public static object getCurrentValue(object baseScript, object obj)
    {
        // from Record.cs's "GenericFrame"
        object data = null;

        //if (property.obj is FieldInfo)
        if (obj is FieldInfo)
        {
            //data = ((FieldInfo)property.obj).GetValue(property.animatedComponent);
            data = ((FieldInfo)obj).GetValue(baseScript);
        }
        else if (obj is PropertyInfo)
        {
            data = ((PropertyInfo)obj).GetValue(baseScript);
        }
        // TODO transforms?

        return data;
    }

    ///// <summary>
    ///// gets the path to the variable from the gameobject; like script.subClass.variable etc. For syncing variables on the network
    ///// </summary>
    //public static string GetFullVariableName<T>(Expression<Func<T>> expr)
    //{
    //    var body = (MemberExpression)expr.Body;
    //    string varName = body.Member.Name;

    //    Type type = body.Member.DeclaringType;
    //    return $"{type.DeclaringType.Name}.{type.Name}.{varName}";
    //}

    //// doesn't seem to work
    //public static object FindVariable(string variablePath, object rootObject)
    //{
    //    if (rootObject == null || string.IsNullOrEmpty(variablePath))
    //        return null;

    //    string[] parts = variablePath.Split('.');
    //    object currentObject = rootObject;
         
    //    // We start from index 1 because the root object is already provided (SmashCharacter in this case)
    //    for (int i = 1; i < parts.Length; i++)
    //    {
    //        if (currentObject == null)
    //            return null;

    //        Type currentType = currentObject.GetType();

    //        // Try to get the member as a field first
    //        FieldInfo field = currentType.GetField(parts[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    //        if (field != null)
    //        {
    //            currentObject = field.GetValue(currentObject);
    //            continue;
    //        }

    //        // If it's not a field, try to get it as a property
    //        PropertyInfo property = currentType.GetProperty(parts[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    //        if (property != null)
    //        {
    //            currentObject = property.GetValue(currentObject);
    //            continue;
    //        }

    //        // If neither a field nor a property, return null
    //        return null;
    //    }

    //    return currentObject;
    //}






    // TODO move to Multi.cs later

    //public static void addSyncedProperty_Player(SmashCharacter c, object animatedComponent)
    public static void addSyncedProperty_Player(SmashCharacter c, string varName)
    {
        
    }


    [ServerRpc(RequireOwnership = false)]   // Note: This singleton class always runs as !IsOwner on non server clients, for some reason.
    public void server_AddSyncedProperty_Player_ServerRpc(NetData data, int identifier, ServerRpcParams pars = default)
    {
        var clientId = pars.Receive.SenderClientId;
        print(clientId + " pinged the server with " + data.GetData().ToString());

        //syncParam_ClientRpc(data, clientId);  // send to all clients


    }

    // TODO batch the properties together for a single player, into one big RPC. Maybe with a dictionary
    // todo that'll require a new sending system >_> dict int id and serializer
    [ClientRpc]
    public void client_addSyncedProperty_Player_ClientRpc(string playerID, int identifier, string varName, ulong originalSender, ClientRpcParams pars = default)
    {
        var thisClientId = NetworkManager.Singleton.LocalClientId;
        //print("Server pinged client " + thisClientId + " (originally from client " + originalSender + ") with " + data.GetData().ToString());
    
        // spawn SyncedProperty on this client with the given ID

        SmashCharacter c = smashCharacters[playerID];

        //Multi.SyncedProperty sp = new Multi.SyncedProperty(identifier, c, , c.gameObject, instance.clip, c.IsOwner);
    
    }






    public static void addSyncedObject(Transform obj, bool IsOwner)
    {
        //instance.clip.addSyncedProperty(obj, obj.gameObject);
        // TODO

    }


}



