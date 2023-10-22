using Kart;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


[Tooltip("multiplayer synced duplicates of the XR head/hands, for player prefab copies to use on each machine. Kept in sync with a an XR rig in the scene")]
//public class XR_Dummies_Sync : NetworkBehaviour
//public class XR_Dummies_Sync : NetBehaviour
public class XR_Dummies_Sync : NetBehaviour
{
    //public SO_MyMultiplayer so_MyMultiplayer;

    [Tooltip("The network synced copies/dummy objects, that copy the movement of the XR devices in the player's scene")]
    public Transform XR_Origin;
    public Transform XR_Headset;
    public Transform leftHandObject;
    public Transform rightHandObject;

    public bool isOwnerPublic;  // debug

    public float testfloat = 2;
    public float testFloat2 = 2;

    // Start is called before the first frame update
    void Start()
    {
    }

    SmashMulti mm = null;
    public override void OnNetworkSpawn()
    {
        // trySettingUpXR()

        // Note: Runs before the game scene scripts are loaded, so I can't reference singletons for a few frames
    }


    void setupItem(Transform imitator)
    {
        //NetworkObject no = imitator.gameObject.AddComponent<NetworkObject>();
        //imitator.gameObject.AddComponent<ClientNetworkTransform>();
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

        testfloat += 1; 
        testFloat2 = testfloat + Time.time;
    }

    void syncXRPositions()
    {
        isOwnerPublic = IsOwner;
        //SmashMulti mm = SmashMulti.instance;

        //syncTransform(mm.XR_Origin, XR_Origin);
        if (!IsOwner)
            return; // let the network sync the other players' XR stuff

        if (mm == null)
            return;
        syncTransform(mm.XR_Headset, XR_Headset);
        syncTransform(mm.leftHandObject, leftHandObject);
        syncTransform(mm.rightHandObject, rightHandObject);


        // get the hands out of your face if the XR stuff isn't in use
        if (XR_Headset.position == leftHandObject.position)
            leftHandObject.position += Vector3.forward;
        if (XR_Headset.position == rightHandObject.position)
            rightHandObject.position += Vector3.forward;

    }

    /// <summary>
    /// The damn network async spawns the player before the scene and its singletons, so keep trying until those spawn in
    /// </summary>
    void trySettingUpXR()
    {
        if (SmashMulti.instance == null)
        { 
            print("SmashMulti not Awake yet"); 
            return;
        }

        if (mm != null)
            return; // already complete

        print("SmashMulti FOUND! Running setup");



        //SmashMulti.addXRRig(this);


        //NetworkObject no = XR_SyncedDummies.gameObject.AddComponent<NetworkObject>(); 
        //NetworkObject no = XR_SyncedDummies.gameObject.AddComponent<NetworkObject>(); 


        // damn network async calls load the player before the SmashMulti singleton script awakes, so we have to find it manually
        mm = SmashMulti.instance;
        //mm = FindFirstObjectByType<SmashMulti>();
        //print("mm " + mm);

        //setupItem(XR_Origin);
        setupItem(XR_Headset);
        setupItem(leftHandObject);
        setupItem(rightHandObject);

        //if (!IsOwner)
        if (!IsOwner)
        {
            // disable other player instances cameras
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

    // update is called once per frame
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
