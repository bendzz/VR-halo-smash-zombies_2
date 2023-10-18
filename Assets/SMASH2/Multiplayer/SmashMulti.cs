using Kart;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Unity.Netcode;
using UnityEditor.UIElements;
//using UnityEditor.PackageManager;
using UnityEngine;
using static Record;
using static SmashMulti;
//using static UnityEditor.Progress;


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


    // TODO:
    // -Make it split off and save new clips with button presses, for making it easier to save highlights for later editing
    // -Make Record.cs save clips gradually as you go so it doesn't take forever at the end. (And load clips gradually)
    // --and make it async
    // --and an option to spawn a progress bar UI on a given camera/gameobject, for VR
    // -Will need a cycling clip that holds like 1024 frames, for server authoritative games. (This one won't be saved)
    /// <summary>
    /// A recording of the game, and tracks all multiplayer synced variables and methods
    /// </summary>
    public Record.Clip clip;


    List<SyncedProperty> syncedProperties;  // not sure this list is the best way to hole synced variables/methods. A dictionary maybe? Both?

    //float testVar = 2;
    //SyncedProperty syncedTest;
    public override void OnNetworkSpawn()
    {
        clip = new Record.Clip("SmashGameClip " + UnityEngine.Random.value * 100000);

        dummyXR_Rigs = new List<XR_Dummies_Sync>();
        syncedProperties = new List<SyncedProperty>();
        // test
        //syncedTest = new SyncedProperty(testVar, gameObject, clip, IsOwner);
        //instance.syncedTest = new SyncedProperty(this, testVar, gameObject, instance.clip, IsOwner);

    }

    private void Update()
    {
        //if (syncedTest != null)
        //    syncedTest.sync();
        //testVar += 1;

        foreach(SyncedProperty prop in syncedProperties)
        {
            if (prop.IsOwner)
                prop.sync();
        }
    }



    public static void addXRRig(XR_Dummies_Sync rig)
    {
        instance.dummyXR_Rigs.Add(rig);

        // stuff to sync and record
        //instance.clip.addProperty(rig.XR_Origin, rig.XR_Origin.gameObject);  // TODO could it just infer the gameobject?
        //instance.clip.addProperty(rig.XR_Headset.parent, rig.XR_Headset.parent.gameObject);  // need to track the neck too for mouse looking up-down
        //instance.clip.addProperty(rig.XR_Headset, rig.XR_Headset.gameObject);
        //instance.clip.addProperty(rig.leftHandObject, rig.leftHandObject.gameObject);  
        //instance.clip.addProperty(rig.rightHandObject, rig.rightHandObject.gameObject); 

        bool isOwner = rig.IsOwner;

        // todo replace with SyncedProperty
        addSyncedObject(rig.XR_Origin, isOwner);
        addSyncedObject(rig.XR_Headset.parent, isOwner);
        addSyncedObject(rig.XR_Headset, isOwner);
        addSyncedObject(rig.leftHandObject, isOwner);
        addSyncedObject(rig.rightHandObject, isOwner);

        // test
        print("XR rig synced: " + rig.gameObject.name);


        //test
        //instance.syncedTest = new SyncedProperty(rig, rig.testfloat, rig.gameObject, instance.clip, rig.IsOwner);
        //instance.syncedProperties.Add(new SyncedProperty(rig, rig.testfloat, rig.gameObject, instance.clip, rig.IsOwner));
        instance.syncedProperties.Add(new SyncedProperty(rig, rig.testFloat2, rig.gameObject, instance.clip, rig.IsOwner));
    }
     

    // TODO move to Multi.cs

    public static void addSyncedObject(Transform obj, bool IsOwner)
    {
        instance.clip.addProperty(obj, obj.gameObject);


    }



    void Awake()
    {
        //if ( instance == null)
        //    Startup();

        if (instance != null)
            Debug.LogError("More than one SmashMulti in scene");
        instance = this;
    }






    // todo move to Multi.cs

    /// <summary>
    /// Synced and animated property
    /// (Doesn't seem to work when you make one for a variable in the current script, only other scripts? Idk. TODO, test more)
    /// </summary>
    public class SyncedProperty : Record.AnimatedProperty
    {
        //NetworkVariable<T> netVar;

        //RPC // todo

        public bool IsOwner;


        //public SyncedProperty(object _obj, GameObject _gameObject, Record.Clip _clip, bool isOwner) : base(_obj, _gameObject, _clip)
        public SyncedProperty(object _animatedObject, object propertyOrField, GameObject _gameObject, Record.Clip _clip, bool isOwner) : base(_animatedObject, propertyOrField, _gameObject, _clip)
        {
            print("syncedProperty: " + propertyOrField);
            IsOwner = isOwner;
            // TODO
        }


        public void sync()
        {
            //[ServerRpc]


            if (IsOwner)
            {
                //pingServerRpc(Time.time);   // send to server

                object data = getCurrentValue();
                pingServerRpc(data);
            }

        }

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

            return data;
        }


        // test
        [ServerRpc]
        public void pingServerRpc(object data, ServerRpcParams pars = default)
        {
            var clientId = pars.Receive.SenderClientId;
            print(clientId + " pinged the server with " + data.ToString());

            pingClientRpc(data, clientId);  // send to all clients
        }

        [ClientRpc]
        public void pingClientRpc(object data, ulong originalSender, ClientRpcParams pars = default)
        {
            var thisClientId = NetworkManager.Singleton.LocalClientId;
            print("Server pinged client " + thisClientId + " (originally from client " + originalSender + ") with " + data.ToString());
        }


        //// test
        //[ServerRpc]
        //public void pingServerRpc(float time, ServerRpcParams pars = default)
        //{
        //    var clientId = pars.Receive.SenderClientId;
        //    //if (NetworkManager.ConnectedClients.ContainsKey(clientId))
        //    //{
        //    //    var client = NetworkManager.ConnectedClients[clientId];
        //    //    // Do things for the client (our local copy) that sent the RPC
        //    //    // client.PlayerObject.GetComponent<SmashCharacter>().pingServerRpc(time);
        //    //}
        //    print(clientId + " pinged the server with " + time);

        //    pingClientRpc(time, clientId);  // send to all clients
        //}

        //[ClientRpc]
        //public void pingClientRpc(float time, ulong originalSender, ClientRpcParams pars = default)
        //{
        //    var thisClientId = NetworkManager.Singleton.LocalClientId;
        //    print("Server pinged client " + thisClientId + " (originally from client " + originalSender + ") with " + time);
        //}





    }

}
