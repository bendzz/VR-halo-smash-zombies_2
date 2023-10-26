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
/// This-game specific multiplayer code. (not sure if this class is actually gonna do much tbh)
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





    private void Update()
    {


    }








}



