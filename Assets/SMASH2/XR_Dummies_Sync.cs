using Kart;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


[Tooltip("multiplayer synced duplicates of the XR head/hands, for player prefab copies to use on each machine")]
public class XR_Dummies_Sync : NetworkBehaviour
{
    public Transform XR_Origin;
    public Transform XR_Headset;
    public Transform leftHandObject;
    public Transform rightHandObject;


    // Start is called before the first frame update
    void Start()
    {
    }

    public override void OnNetworkSpawn()
    {
        //NetworkObject no = XR_SyncedDummies.gameObject.AddComponent<NetworkObject>(); 
        //NetworkObject no = XR_SyncedDummies.gameObject.AddComponent<NetworkObject>(); 

        MyMultiplayer mm = MyMultiplayer.instance;

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
            mm.XR_Headset.GetComponent<AudioListener>().enabled = false;
            mm.XR_Headset.GetComponent<Camera>().enabled = false;
        }
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
        syncXRPositions();

    }

    void syncXRPositions()
    {
        MyMultiplayer mm = MyMultiplayer.instance;

        //syncTransform(mm.XR_Origin, XR_Origin);
        syncTransform(mm.XR_Headset, XR_Headset);
        syncTransform(mm.leftHandObject, leftHandObject);
        syncTransform(mm.rightHandObject, rightHandObject);

    }

    // Update is called once per frame
    void Update()
    {
        syncXRPositions();
    }

    private void LateUpdate()
    {
        syncXRPositions();
    }
}
