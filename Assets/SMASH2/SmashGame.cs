using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmashGame : NetBehaviour
{
    [Tooltip("If you go outside this u dead")]
    public Collider safeZone;   // todo make this multiple zones combined, later..?
    [Tooltip("Objects to follow you along the walls warning you of their positions")]
    public GameObject wallWarning_prefab;
    public Transform respawnPoint;

    public static SmashGame instance;

    public Dictionary<string, PlayerStats> playerStats;


    //Multi.Entity entity;


    private void Start()
    {
        OnNetworkSpawn();   // idk if this is a good idea tbh
    }

    public override void OnNetworkSpawn()
    {
        if (instance != null)
            Debug.LogError("Multiple SmashGame instances detected. This is not allowed.");
        instance = this;

        playerStats = new Dictionary<string, PlayerStats>();

        // playerStats TODO. Server auth? Needs net syncing too


        // networking
        entity = new Multi.Entity(this);
        entity.setCurrents(this, this.gameObject, IsOwner);
        //entity.addSyncedProperty(head.transform);

        print("SmashGame spawned");
    }


    // Update is called once per frame
    void Update()
    {
        
    }




    /// <summary>
    /// Better to track these separate of player instances, incase players leave midway
    /// </summary>
    public class PlayerStats
    {
        string PlayerId;
        SmashCharacter character;

        int kills;
        int deaths;

        float damageDealt;
        float damageTaken;
    }
}
