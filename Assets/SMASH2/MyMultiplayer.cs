using Kart;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
//using UnityEditor.PackageManager;
using UnityEngine;
//using static UnityEditor.Progress;


//public class MyMultiplayer : MonoBehaviour
public class MyMultiplayer : NetworkBehaviour
{
    public static MyMultiplayer instance;

    [Header("VR setup (optional)")]
    //[Tooltip("Need to put the NetworkObject on this one or the stack will break")]
    //public Transform XR_SetupTopLevelGameobject;
    public Transform XR_Origin;
    public Transform XR_Headset;

    public Transform leftHandObject;
    public Transform rightHandObject;

    //[Header("(Dummy copies of the XR devices, synced across the network. Use for game script inputs)")]
    //public Transform XR_Origin_Synced;
    //public Transform XR_Headset_Synced;

    //public Transform leftHandObject_Synced;
    //public Transform rightHandObject_Synced;



    [Tooltip("A top level object to hold the network synced copies of the XR device transforms, for one player")]
    Transform XR_SyncedDummies;

    void Awake()
    {
        if (instance != null)
            Debug.LogError("More than one MyMultiplayer in scene");
        instance = this;



    }



    public override void OnNetworkSpawn()
    {

    }



    private void FixedUpdate()
    {
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
