using Kart;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
//using UnityEditor.PackageManager;
using UnityEngine;
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




    public override void OnNetworkSpawn()
    {
        clip = new Record.Clip("SmashGameClip " + UnityEngine.Random.value * 100000);

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

        // todo replace with syncedProperty
        addSyncedObject(rig.XR_Origin, isOwner);
        addSyncedObject(rig.XR_Headset.parent, isOwner);
        addSyncedObject(rig.XR_Headset, isOwner);
        addSyncedObject(rig.leftHandObject, isOwner);
        addSyncedObject(rig.rightHandObject, isOwner);


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


    //public override void OnNetworkSpawn()
    //{

    //}



    // todo move to Multi.cs

    public class syncedProperty : Record.AnimatedProperty
    {
        //NetworkVariable<T> netVar;

        //RPC // todo



        public syncedProperty(object _obj, GameObject _gameObject, Record.Clip _clip) : base(_obj, _gameObject, _clip)
        {
            // TODO
        }


        public void sync()
        {
            //[ServerRpc]
        }
    }

}
