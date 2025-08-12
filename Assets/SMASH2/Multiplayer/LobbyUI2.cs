using System.Collections.Generic;
using TMPro;
using UnityEngine;

// replaces LobbyUI.cs. aug 2025
public class LobbyUI2 : MonoBehaviour
{

    public TMP_Text settingsText;


    UI.PickOneOption game;
    UI.PickOneOption multiplayer;
    void Start()
    {
        UI.addLinks(settingsText);


        game = new UI.PickOneOption(gameObject, new List<string> { "Camping '25", "SmashVR" });
        multiplayer = new UI.PickOneOption(gameObject, new List<string> { "LAN", "Online" });
    }



    void Update()
    {
        // TODO
        //UI.Links[gameObject]["Start New Lobby"];


        if (this.Clicked("Start New Lobby"))
        {
            print("Start New Lobby clicked");
        }
    }
}
