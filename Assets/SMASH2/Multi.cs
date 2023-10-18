using Kart;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
//using UnityEditor.PackageManager;
using UnityEngine;
using static Multi;
//using static UnityEditor.Progress;


//public class Multi : MonoBehaviour
/// <summary>
/// A big class to hold all the networked variables and functions, for server authority and client resolving
/// </summary>
public class Multi : NetworkBehaviour
{
    public static Multi instance;

    [Header("VR setup (optional)")]
    public Transform XR_Origin;
    public Transform XR_Headset;

    public Transform leftHandObject;
    public Transform rightHandObject;



    // generic network variables for server authority
    //List <object> networkVariables = new List<object>();
    List <metaVarA> metaVars = new List<metaVarA>();

    //[Tooltip("A top level object to hold the network synced copies of the XR device transforms, for one player")]
    //Transform XR_SyncedDummies;

    void Awake()
    {
        //if ( instance == null)
        //    Startup();

        if (instance != null)
            Debug.LogError("More than one MyMultiplayer in scene");
        instance = this;
    }

    ///// <summary>
    ///// Sometimes scripts start up before Multi and need to boot it early
    ///// </summary>
    //public static void Startup()
    //{
    //    if (instance != null)
    //        Debug.LogError("More than one Multi in scene");
    //    instance = this;
    //}


    public override void OnNetworkSpawn()
    {



        // test code
        netVar
    }

    //public static NetworkVariable<T> newVar(ref object original)
    public static NetworkVariable<T> CreateNetworkVariable<T>(ref T original)
    {
        NetworkVariable<T> nv = new NetworkVariable<T>(original);



        return nv;


        //NetworkVar<T> newVar = new NetworkVar<T>(variable);
        //networkVariables.Add(newVar);
        //return newVar;

    }


    public abstract class metaVarA
    {
    }

    /// <summary>
    /// metadata for the stored networkVariable
    /// </summary>
    public class metaVar<T> : metaVarA
    {
        NetworkVariable<T> netVar;
        private Func<T> _get;
        private Action<T> _set;

        public metaVar(Func<T> @get, Action<T> @set)
        {
            _get = @get;
            _set = @set;
        }

        public T Value
        {
            get { return _get(); }
            set { _set(value); }
        }

    }


    //public class NetworkVariableBuilder
    //{
    //    public NetworkVariable<T> CreateNetworkVariable<T>(T initialValue)
    //    {
    //        NetworkVariable<T> netVar = new NetworkVariable<T>(initialValue);
    //        // You can set other properties of netVar here if needed, like permissions, etc.
    //        return netVar;
    //    }
    //}
}
