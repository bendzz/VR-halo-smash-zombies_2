using System.Collections.Generic;
using Unity.Netcode;
//using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;


// depreciated, just use Relay next time from the start. And LobbyUI.cs etc instead of this
namespace Kart {
    public class NetworkStartUI : MonoBehaviour
    {
        [SerializeField] Button startHostButton;
        [SerializeField] Button startClientButton;

        void Start()
        {
            startHostButton.onClick.AddListener(StartHost);
            startClientButton.onClick.AddListener(StartClient);
        }

        void StartHost()
        {
            Debug.Log("Starting host");
            NetworkManager.Singleton.StartHost();
            Hide();  
        }

        void StartClient()
        {
            Debug.Log("Starting client");
            NetworkManager.Singleton.StartClient();
            Hide();
        }

        void Hide() => gameObject.SetActive(false);




        public bool selectionFinished = false;
        // new
        private void Update()
        {
            // need a way to start in VR
            if (!selectionFinished)
            {
                print("waiting on host/client selection");
                List<InputDevice> devices = new List<InputDevice>();   // todo throw warning if multiples
                InputDevice device;

                InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
                //if (devices.Count > 0 && devices[0].isValid)
                if (devices.Count > 0)
                {
                    print("device count: " + devices.Count);
                    foreach (var d in devices)
                    {
                        //device = devices[0];
                        device = d;
                        if (!devices[0].isValid)
                            continue;

                        print("righthand found");

                        bool A = false;  // todo touches
                        bool B = false;

                        if (device.TryGetFeatureValue(CommonUsages.primaryButton, out A)) { }
                        if (device.TryGetFeatureValue(CommonUsages.secondaryButton, out B)) { }


                        if (B)
                        {
                            selectionFinished = true;
                            StartHost(); 
                        }
                        if (A)
                        {
                            selectionFinished = true;
                            StartClient(); 
                        }

                        if (selectionFinished)
                            print("Host/client Selection finished");
                    }
                }

                // keyboard backup, cause the buttons stopped working
                if (Input.GetKeyDown(KeyCode.H))
                {
                    selectionFinished = true;
                    StartHost();
                }
                if (Input.GetKeyDown(KeyCode.C))
                {
                    selectionFinished = true;
                    StartClient();
                }
                

            }
        }
    }
}