using Kart;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


[Tooltip("multiplayer synced duplicates of the XR head/hands, for player prefab copies to use on each machine")]
public class XR_Dummies_Sync : NetworkBehaviour
{
    //public SO_MyMultiplayer so_MyMultiplayer;

    [Tooltip("The network synced copies/dummy objects, that copy the movement of the XR devices in the player's scene")]
    public Transform XR_Origin;
    public Transform XR_Headset;
    public Transform leftHandObject;
    public Transform rightHandObject;


    // Start is called before the first frame update
    void Start()
    {
    }

    MyMultiplayer mm = null;
    public override void OnNetworkSpawn()
    {
        // trySettingUpXR()
    }


    void setupItem(Transform imitator)
    {
        imitator.gameObject.AddComponent<ClientNetworkTransform>();
    }

    void syncTransform(Transform original, Transform imitator)
    {
        imitator.localPosition = original.localPosition;
        imitator.localRotation = original.localRotation;
        imitator.localScale = original.localScale;
        imitator.gameObject.SetActive(original.gameObject.activeSelf);
    }

    private void FixedUpdate()
    {
        trySettingUpXR();

        syncXRPositions();

    }

    void syncXRPositions()
    {
        //MyMultiplayer mm = MyMultiplayer.instance;

        //syncTransform(mm.XR_Origin, XR_Origin);
        if (mm == null)
            return;
        syncTransform(mm.XR_Headset, XR_Headset);
        syncTransform(mm.leftHandObject, leftHandObject);
        syncTransform(mm.rightHandObject, rightHandObject);

        //
    }

    /// <summary>
    /// The damn network async spawns the player before the scene and its singletons, so keep trying until those spawn in
    /// </summary>
    void trySettingUpXR()
    {
        if (MyMultiplayer.instance == null)
        { 
            print("MyMultiplayer not Awake yet"); 
            return;
        }

        if (mm != null)
            return; // already complete

        print("MyMultiplayer FOUND! Running setup");

        //NetworkObject no = XR_SyncedDummies.gameObject.AddComponent<NetworkObject>(); 
        //NetworkObject no = XR_SyncedDummies.gameObject.AddComponent<NetworkObject>(); 


        // damn network async calls load the player before the MyMultiplayer singleton script awakes, so we have to find it manually
        mm = MyMultiplayer.instance;
        //mm = FindFirstObjectByType<MyMultiplayer>();
        //print("mm " + mm);

        //setupItem(XR_Origin);
        setupItem(XR_Headset);
        setupItem(leftHandObject);
        setupItem(rightHandObject);

        //if (!IsOwner)
        if (!IsOwner)
        {
            XR_Headset.GetComponent<AudioListener>().enabled = false;
            XR_Headset.GetComponent<Camera>().enabled = false;
            //Camera cam = XR_Headset.GetComponent<Camera>();
            //cam.Priority = 0;
        }

        // disable original VR cam
        {
            //print("mm.XR_Headset " + mm.XR_Headset);
            //print("mm.mm.XR_Headset.GetComponent<AudioListener>() " + mm.XR_Headset.GetComponent<AudioListener>());
            //mm.XR_Headset.GetComponent<AudioListener>().enabled = false;
            //mm.XR_Headset.GetComponent<Camera>().enabled = false;

            //
            mm.XR_Headset.GetComponent<AudioListener>().enabled = false;
            mm.XR_Headset.GetComponent<Camera>().enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //trySettingUpXR();


        syncXRPositions();
    }

    private void LateUpdate()
    {
        syncXRPositions();
    }
}
