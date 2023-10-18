using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

public class LobbyUI : MonoBehaviour
{
    [Tooltip("The game scene to create/load when CreateLobby is called")]
    [SerializeField] string gameScene = "TestSmashScene";

    [SerializeField] TextMeshProUGUI placardText;


    /// <summary>
    /// Will spin this to indicate creating lobby
    /// </summary>
    [SerializeField] Transform sign;

    [Header("outputs")]
    public bool finishedAndWaiting = false;
    public bool searchedForLobby = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    void spinSign()
    {
        if (sign != null)   // script keeps running even after scene has been destroyed ig
            sign.rotation *= Quaternion.Euler(0, 360 * Time.deltaTime, 0);
    }

    int deviceCount = 0;
    bool oldA = false;
    bool oldB = false;

    bool oldC = false;
    bool oldJ = false;
    // update is called once per frame
    void Update()
    {
        if (finishedAndWaiting)
        {
            spinSign();

            return;
        }
        if (searchedForLobby)
            spinSign();


        updatePlacard();


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
                    finishedAndWaiting = true;
                    CreateGame();
                }
                if (A && !oldA)
                {
                    searchedForLobby = true;
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
            searchedForLobby = true;
            JoinGame();
        }
        oldC = C;
        oldJ = J;
    }

    void updatePlacard()
    {
        placardText.text = "<size=30><b>Smash Multiplayer</b></size>\r\nCreate Lobby: Keyboard [C] or VR [B]\r\nQuick Join: Keyboard [J] or VR [A] \nLobbies:";

        //print("LobbyMultiplayer.instance " + LobbyMultiplayer.instance);
        //print("LobbyMultiplayer.instance.lobbies " + LobbyMultiplayer.instance.lobbies);
        //print("LobbyMultiplayer.instance.lobbies.Results " + LobbyMultiplayer.instance.lobbies.Results);

        //print("1");
        if (LobbyMultiplayer.instance.lobbies == null)
            return;
        //print("2");
        if (LobbyMultiplayer.instance.lobbies.Results == null)
            return;

        placardText.text += LobbyMultiplayer.instance.lobbies.Results.Count;    // TODO appending strings makes new string objects, causes garbage collection

        //print("Lobbies: " + LobbyMultiplayer.instance.lobbies.Results.Count);
        int counter = 0;
        foreach (var l in LobbyMultiplayer.instance.lobbies.Results)
        {
            //TimeSpan dt = l.Created - DateTime.Now;   // TODO a helper function to find elapsed time properly
            placardText.text += "\r\n <size=15><b>#" + counter + "</b> Host: <b>" + l.Data[LobbyMultiplayer.HOSTNAME].Value + "</b> Name: <b>" 
                + l.Name + "</b> Players:<b>" + l.Players.Count + "/" + l.MaxPlayers +
                "\r\n</b>Created: " + l.Created.Day + "th at: " + l.Created.Hour + ":" + l.Created.Minute + ":" + l.Created.Second
                + " V#: " + l.Version + " Id: " + l.Id;

            //print("Id " + l.Id + " HostId " + l.HostId + " Name " + l.Name + " Upid " + l.Upid + " Version " + l.Version + " IsPublic " + l.AvailableSlots
            //+ " Created " + l.Created +  " EnvironmentId " + l.EnvironmentId + " HasPassword " + l.HasPassword
            //+ " IsLocked " + l.IsLocked + " IsPrivate " + l.IsPrivate + " LastUpdated " + l.LastUpdated 
            //+ " MaxPlayers " + l.MaxPlayers + " Players " + l.Players.ToString());

            //" Data " + l.Data.ToString() +
            //" LobbyCode " + l.LobbyCode
            counter++;
        }

        //placardText

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