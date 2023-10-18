using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;



/// <summary>
/// Generic code for multiplayer and recording/playback of gameplay. This is the only script that should be networked.
/// Use for all networked variables and functions.
/// </summary>
public class Multi : NetworkBehaviour
{
    Multi instance;


    public override void OnNetworkSpawn()
    {
        if (instance != null)
            Debug.LogError("More than one Multi singleton in scene");
        instance = this;


    }



    // Update is called once per frame
    void Update()
    {
        
    }
}



/// <summary>
/// Wrapper for NetworkBehaviour. Gives an isSimulating flag compatible with multiplayer and recording/playback.
/// Be sure to register your gameobjects with your game's multiplayer singleton!
public abstract class netBehaviour : NetworkBehaviour
{
    /// <summary>
    /// Whether to run game logic, or just let the multiplayer or recording-playback system control this intance
    /// </summary>
    public bool isSimulating {  get; }


}