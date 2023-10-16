using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

public class LobbyUI : MonoBehaviour
{
    [Tooltip("The game scene to create/load when CreateLobby is called")]
    [SerializeField] string gameScene = "TestSmashScene";

    /// <summary>
    /// Will spin this to indicate creating lobby
    /// </summary>
    [SerializeField] Transform sign;
    public bool finishedAndWaiting = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }


    int deviceCount = 0;
    bool oldA = false;
    bool oldB = false;

    bool oldC = false;
    bool oldJ = false;
    // Update is called once per frame
    void Update()
    {
        if (finishedAndWaiting)
        {
            if (sign != null)   // script keeps running even after scene has been destroyed ig
                sign.rotation *= Quaternion.Euler(0, 360 * Time.deltaTime, 0);

            return;
        }

        //print("waiting on host/client selection");
        List<InputDevice> devices = new List<InputDevice>();   // todo throw warning if multiples
        InputDevice device;

        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
        //if (devices.Count > 0 && devices[0].isValid)
        if (devices.Count > 0)
        {
            //print("device count: " + devices.Count);
            if (deviceCount != devices.Count)
            {
                deviceCount = devices.Count;
                print("device count: " + devices.Count);
            }

            foreach (var d in devices)
            {
                //device = devices[0];
                device = d;
                if (!devices[0].isValid)
                    continue;

                //print("righthand found");

                bool A = false;  // todo touches
                bool B = false;

                if (device.TryGetFeatureValue(CommonUsages.primaryButton, out A)) { }
                if (device.TryGetFeatureValue(CommonUsages.secondaryButton, out B)) { }


                if (B && !oldB)
                {
                    CreateGame();
                }
                if (A && !oldA)
                {
                    JoinGame();
                }

                oldA = A;
                oldB = B;
            }
        }

        // keyboard backup, cause the buttons stopped working
        bool C = Input.GetKeyDown(KeyCode.C);
        bool J = Input.GetKeyDown(KeyCode.J);
        if (C && !oldC)
        {
            finishedAndWaiting = true;
            CreateGame();
        }
        if (J && !oldJ)
        {
            JoinGame();
        }
        oldC = C;
        oldJ = J;
    }



    async void CreateGame()
    {
        await LobbyMultiplayer.instance.CreateLobby();
        // Only the host can change the Scene
        Loader.LoadNetwork(gameScene);
    }

    async void JoinGame()
    {
        await LobbyMultiplayer.instance.QuickJoinLobby();
    }

}

/// <summary>
/// https://youtu.be/zimljd4Rxr0?list=PLnJJ5frTPwRN79MQt13JVCjZ9WVPKUjO3&t=1049 idk why this is a separate method
/// </summary>
public static class Loader
{
    public static void LoadNetwork(string sceneName)
    {
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}