using Kart;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor.UIElements;
//using UnityEditor.PackageManager;
using UnityEngine;
using static Record;
using static SmashMulti;
//using static UnityEditor.Progress;
using UnityEngine;
using Unity.Netcode;

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

    // refs
    Multi multi;
    Clip clip;




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


    }

    private void Update()
    {

    }



    public static void addXRRig(XR_Dummies_Sync rig)
    {
        instance.dummyXR_Rigs.Add(rig);

        bool isOwner = rig.IsOwner;

        // todo replace with SyncedProperty
        addSyncedObject(rig.XR_Origin, isOwner);
        addSyncedObject(rig.XR_Headset.parent, isOwner);
        addSyncedObject(rig.XR_Headset, isOwner);
        addSyncedObject(rig.leftHandObject, isOwner);
        addSyncedObject(rig.rightHandObject, isOwner);

        // test
        print("XR rig synced: " + rig.gameObject.name);



        //instance.testProperties.Add(new SyncedProperty(rig, rig.testFloat2, rig.gameObject, instance.clip, rig.IsOwner));
        new Multi.SyncedProperty(rig, rig.testFloat2, rig.gameObject, instance.clip, rig.IsOwner);
    }


    // TODO move to Multi.cs later
    public static void addSyncedObject(Transform obj, bool IsOwner)
    {
        //instance.clip.addProperty(obj, obj.gameObject);
        // TODO

    }







}



