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
public class SmashMulti : MonoBehaviour

//public class SmashMulti : NetworkBehaviour
{
    public static SmashMulti instance;

    [Header("VR setup (optional)")]
    public Transform XR_Origin;
    public Transform XR_Headset;

    public Transform leftHandObject;
    public Transform rightHandObject;





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


}
