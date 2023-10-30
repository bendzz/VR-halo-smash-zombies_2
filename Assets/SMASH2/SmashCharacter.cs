//using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
//using System.Drawing;
//using System.Drawing;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using Unity.XR.CoreUtils;
//using Unity.Collections;
//using Unity.Netcode;
//using Unity.Services.Authentication;
//using Unity.Services.Lobbies;
//using Unity.Services.Lobbies.Models;
//using Unity.VisualScripting;
//using Unity.XR.CoreUtils;
using UnityEngine;



/// <summary>
/// Script to control smash bros style gameplay stuff, and enemy visuals
/// </summary>
//public class myInputTests : MonoBehaviour
//public class SmashCharacter : NetworkBehaviour
public class SmashCharacter : NetBehaviour
{
    public Rigidbody body;
    public FeetCollider feetCollider;

    public SmashGame smashGame;

    // other scripts
    // To get the hands and head positions; going to try and keep the enemy decoupled from this tho, for later AI and networking etc
    public myInputTests input;
    public XR_Dummies_Sync xr_Dummies_Sync;

    // game settings
    public string playerName = "default player";
    /// <summary>
    /// A long random ID string grabbed from the unity Lobby service. Much more unique than OwnerClientId
    /// </summary>
    public string PlayerId;


    [Tooltip("A particle system prefab to shoot out of the jets")]
    public GameObject handJetsParticlesPrefab;

    [Tooltip("jet prefab with script")]
    public GameObject handJetPrefab;

    // model assets
    public GameObject Head_VR;
    public GameObject Head_PC;
    public SwordAnimate Sword;
    public GameObject SwordPrefab;

    public float damage = 0;    // smash bros damage, amplifies knockback
    float oldDamage = 0;

    // Output
    public bool stunLocked = false;    // if true, controls are unresponsive    // unused yet
    public bool limp = false;   // after a stun, the character is limp until controls are pressed again. They smash into the ground
    public bool dead = false;
    public float respawnTimer = 0;


    public Hand leftie = new Hand();
    public Hand rightie = new Hand();
    Transform head;

    // temp
    //[SerializeField] Vector3 swordStart;
    //[SerializeField] Vector3 swordEnd;
    Vector3 swordStart = new Vector3(0,0,-120);
    Vector3 swordEnd = new Vector3(0, -30, -297);
    //Vector3 swordEnd = new Vector3(0, 0, -297);

    public List<Hand> hands;
    float stunTimer = 0; // how many seconds left in the stun

    /// <summary>
    /// All characters in the scene, keyed with their OwnerClientId
    /// </summary>
    //public static List<SmashCharacter> characters = new List<SmashCharacter>();
    public static Dictionary<ulong, SmashCharacter> characters = new Dictionary<ulong, SmashCharacter>();   // TODO make this a Multi.getEntityByClientId() function
    
    public static Dictionary<string, SmashCharacter> characters_byPlayerId = new Dictionary<string, SmashCharacter>();   // TODO make this a Multi.getEntityByClientId() function

    public Color playerColor;
    //[Tooltip("Set by gameObject.GetComponent<NetworkObject>().OwnerClientId")]
    //[Tooltip("Set by NetworkManager.Singleton.LocalClientId on the IsOwner side. Takes a few moments to sync over the network to other clones")]
    //public ulong OwnerClientId = ulong.MaxValue;     // TODO make this a Multi.getEntityByClientId() function

    public List<Color> playerColors;
    public static Dictionary<Color, SmashCharacter> inUsePlayerColors;

    InfoCard infoCard;

    // multiplayer use
    public Multi.SyncedProperty applyDamageSynced;  
    public Multi.SyncedProperty registerPlayer_Synced;  
    public bool VR_mode = false;

    //MeshRenderer meshRenderer;
    Material glowMaterial;


    Multi.Entity entity;




    public override void OnNetworkSpawn()   // TODO haven't changed anything for networking yet...
    {
        //OwnerClientId = networkedGameObject.GetComponent<NetworkObject>().OwnerClientId;
        //if (IsOwner)
        //    OwnerClientId = NetworkManager.Singleton.LocalClientId;

        // sync to input script (if it exists)
        if (input != null)
        {
            print("init " + this.name + " input " + input.name);
            //print(input.leftie);
            //leftie = new Hand();
            leftie.transform = input.leftie.transform;
            rightie.transform = input.rightie.transform;
            head = input.XR_Headset;
        }
        else
        {
            // AI controlled then
            leftie.transform = rightie.transform = head = transform;
            limp = true;    // for debugging
        }

        hands = new List<Hand>();
        hands.Add(leftie);
        hands.Add(rightie);

        foreach (Hand hand in hands)
        {
            //Hand.handJetFlames = Instantiate(handJetsParticlesPrefab, Hand.rectTransform).GetComponent<ParticleSystem>();
            hand.setHandjet(handJetsParticlesPrefab.transform);
            if (handJetPrefab != null)
                hand.handJet = Instantiate(handJetPrefab, hand.transform);
        }


        // player info Card
        infoCard = new InfoCard(transform);
        infoCard.setDefaultFont();


        if (IsOwner)
        {
            playerName = LobbyMultiplayer.instance.PlayerName;
            PlayerId = LobbyMultiplayer.instance.PlayerId;
            transform.parent.name = "Player:" + playerName;
        }
        print("SmashCharacter IsOwner " + IsOwner);


        // set up SyncedProperties for all variables and synced function calls, in a fixed order
        //entity = new Multi.Entity(this);
        entity = new Multi.Entity();
        //entity.addToLocalEntities();

        entity.setCurrents(this, head.gameObject, IsOwner);
        entity.addSyncedProperty(head.transform);

        entity.setCurrents(input.XR_Origin.gameObject, input.XR_Origin.gameObject, IsOwner);
        entity.addSyncedProperty(input.XR_Origin.transform);

        entity.setCurrents(this, this.gameObject, IsOwner);
        entity.addSyncedProperty(playerName);
        entity.addSyncedProperty(PlayerId);
        entity.addSyncedProperty(damage);
        //entity.addSyncedProperty(OwnerClientId);

        //entity.setCurrents(Head_VR.gameObject, Head_VR.gameObject, Head_VR.gameObject.active);
        entity.addSyncedProperty(VR_mode);
        entity.addSyncedProperty(playerColor);


        //object[] parameters = { 42.2f, Vector3.zero };
        //applyDamageSynced = entity.addSyncedMethodCall("applyDamage", parameters);

        //applyDamage(float Damage, Vector3 throwBack)
        applyDamageSynced = entity.addSyncedMethodCall("applyDamage", new object[] {42.2f, Vector3.zero});

        //registerPlayer_Synced = entity.addSyncedMethodCall("registerPlayer", new object[] { OwnerClientId });

        entity.setCurrents(body, gameObject, IsOwner);  // rigidbody
        entity.addSyncedProperty(body.velocity);
        entity.addSyncedProperty(body.angularVelocity);
        entity.addSyncedProperty(body.useGravity);
        entity.addSyncedProperty(transform);

        foreach(Hand hand in hands)
        {
            entity.setCurrents(hand, gameObject, IsOwner);
            entity.addSyncedProperty(hand.transform);
            entity.addSyncedProperty(hand.thruster);
            //entity.addSyncedProperty(hand.handJet.transform);
            entity.addSyncedProperty(hand.thrusterDirection);
            //entity.addSyncedProperty(hand.handJetFlames.transform); // this should never move, but it's desyncing, so let's try this

        }

        //if (!characters.Contains(this))
        //    characters.Add(this);
        //print("NetworkManager.Singleton.OwnerClientId " + NetworkManager.Singleton.OwnerClientId);
        //OwnerClientId = NetworkManager.Singleton.OwnerClientId;
        //if (!characters.ContainsKey(OwnerClientId))
        //    characters.Add(OwnerClientId, this);

        //if (IsOwner)
        //    registerPlayer_Synced.callMethod(new object[] { NetworkManager.Singleton.OwnerClientId });  // not working, too early, no syncedproperty IDs yet




        body.isKinematic = false;   // why the hell is this suddenly getting set to true upon spawn? Bloody weird

        Sword.body.isKinematic = true;
        Sword.held = true;
        Sword.holder_PlayerId = PlayerId;

        // set glow
        playerColor = getFreshColor();
        inUsePlayerColors.Add(playerColor, this);

        MeshRenderer meshRenderer = Head_PC.GetComponent<MeshRenderer>();
        glowMaterial = GetMaterialInstanceByName(meshRenderer, "glow");
        //print("glowMaterial " + glowMaterial.name);
        meshRenderer = Head_VR.GetComponent<MeshRenderer>();
        glowMaterial = GetMaterialInstanceByName(meshRenderer, "headsetGlow", glowMaterial);
        //print("glowMaterial " + glowMaterial.name);


        // register player to dicts
        if (!characters.ContainsKey(OwnerClientId))
            characters.Add(OwnerClientId, this);
        else 
            print("duplicate key! " + OwnerClientId);

        smashGame = SmashGame.instance;
    }
    ///// <summary>
    ///// Because 'NetworkManager.Singleton.OwnerClientId' returns 0 if not owner ig, so it has to be network propagated out
    ///// </summary>
    ///// <param name="LocalClientId"></param>
    //public void registerPlayer(ulong LocalClientId)
    //{
    //    print("Registered smashPlayer with LocalClientId " + LocalClientId);
    //    characters.Add(LocalClientId, this);
    //}

    Color getFreshColor()
    {
        if (inUsePlayerColors == null)
            inUsePlayerColors = new Dictionary<Color, SmashCharacter>();

        List<Color> unusedColors = new List<Color>();

        foreach(Color color in playerColors)
        {
            if (!inUsePlayerColors.ContainsKey(color))
                unusedColors.Add(color);
        }

        if (unusedColors.Count == 0)
            unusedColors = playerColors;

        Color newColor = unusedColors[Random.Range(0, unusedColors.Count-1)];

        return newColor;
    }
    private static Material GetMaterialInstanceByName(MeshRenderer renderer, string materialName, Material replaceWithMaterial = null)
    {
        Material[] materials = renderer.materials;
        for (int m = 0; m < materials.Length; m++)
        {
            if (materials[m].name.StartsWith(materialName)) // Using StartsWith because Unity appends " (Instance)" to material names when calling .materials
            {
                if (replaceWithMaterial == null)
                    replaceWithMaterial = new Material(materials[m]);

                materials[m] = replaceWithMaterial; // Modify the array
            }
        }
        renderer.materials = materials; // Reassign the modified array back to the renderer
        return replaceWithMaterial;
    }


    private void Update()
    {
        //if (OwnerClientId != ulong.MaxValue) {
        //    if (!characters.ContainsKey(OwnerClientId)) // todo a bool check to save the dict check
        //    {
        //            registerPlayer(OwnerClientId);  // only register once the OwnerClientId has been synced over the network (it's 0 on !IsOwners by default)
        //    }
        //}
        if (PlayerId != "")
        {
            if (!characters_byPlayerId.ContainsKey(PlayerId))
            {
                characters_byPlayerId.Add(PlayerId, this);
                print("Registered player by PlayerId: " + PlayerId);
            }
        }

    }


    bool alertedSmashMulti = false;

    bool oldLeftClick = false;
    bool oldRightClick = false;

    // update is called once per frame
    //void update()
    private void FixedUpdate()
    {
        //print("playerID " + PlayerId + " isOwner " + IsOwner + " body.velocity " + body.velocity);

        if (!dead)
        {
            glowMaterial.SetColor("_Color", playerColor);
            glowMaterial.SetColor("_EmissionColor", playerColor);

            transform.parent.name = "Player:" + playerName;

            // head visuals
            if (input != null)
            {
                if (IsOwner)
                {
                    Head_VR.SetActive(false);
                    Head_PC.SetActive(false);
                }
                else
                {
                    if (VR_mode)
                    {
                        Head_VR.SetActive(true);
                        Head_PC.SetActive(false);
                        //print("VR head!");
                    }
                    else
                    {
                        Head_VR.SetActive(false);
                        Head_PC.SetActive(true);
                        //print("PC head!");
                    }
                }
                //print("VRMode " + VR_mode + " IsOwner " + IsOwner);
                //if (IsOwner)
                //    VR_mode = xr_Dummies_Sync.VR_InUse;
                //VR_mode = input.VR_mode;
                //print("VRMode 2 " + VR_mode + " IsOwner " + IsOwner);

            }

            // PC player hands
            if (!VR_mode)
            {
                //print("keyboard mode!");
                Transform bod = xr_Dummies_Sync.XR_Origin;
                rightie.transform.localPosition = Vector3.right * .4f + Vector3.up * -.2f + Vector3.forward * .5f;
                rightie.transform.localRotation = Quaternion.Euler(-90, 0, 0);

                leftie.transform.localPosition = Vector3.right * -.4f + Vector3.up * -.2f + Vector3.forward * .5f;
                leftie.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            }



            if (damage != oldDamage)
            {
                DamageNumbers.Card.newCardFormatted(transform, damage - oldDamage);
            }

            if (limp)
            {
                feetCollider.playerHeight = feetCollider.playerDefaultHeight / 2;

                if (feetCollider.isGrounded)
                {
                    // todo make them bang and bounce on the ground like in smash bros
                    body.drag = 7;
                    body.angularDrag = 7;
                    feetCollider.runUpWalls = false;
                }
                else
                {
                    body.drag = 0;
                    body.angularDrag = 0;
                    feetCollider.runUpWalls = true;
                }
            }


            foreach (Hand hand in hands)
            {
                //hand.update();
                hand.update();

                //if (hand.thruster > 0)
            }

            if (input.rightie.trigger > 0 || input.leftie.trigger > 0)
            {
                if (IsOwner)
                {
                    VR_mode = true; // hacky
                                    //print("Switched to VR mode");
                }
            }
            if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
            {
                if (IsOwner)
                {
                    VR_mode = false; // hacky
                                     //print("Switched FROM VR mode");
                }
            }

            // Do damage

            // apply thruster damage to enemies
            //foreach(SmashCharacter enemy in characters)
            foreach (SmashCharacter enemy in characters.Values)
            {
                if (enemy == this)
                    continue;
                if (enemy == null)
                    continue;

                //Vector3 EnemyOffset = enemy.rectTransform.position - rectTransform.position;

                foreach (Hand hand in hands)
                {
                    // check for jet flame hits
                    if (hand.thruster > 0)
                    {
                        Vector3 EnemyOffset = enemy.transform.position - hand.transform.position;


                        float angleOffset = Vector3.Angle(hand.thrusterDirection, EnemyOffset);

                        float maxAngle = 45;
                        float angleLoss = .3f;  // how much strength can be lost from a glancing blow
                        float maxFlameLength = 6;  // If thrusters are at strength 1 (tho they go arbitrarily higher)
                        float flameMaxDamage = 140; // per second
                        if (angleOffset < maxAngle)
                        {
                            float angleScale = Mathf.Clamp01((maxAngle - (angleOffset * angleLoss)) / maxAngle);

                            //float distanceScale = Mathf.Clamp01((maxFlameLength - EnemyOffset.magnitude) / maxFlameLength);
                            float flameLength = maxFlameLength * hand.thruster;
                            float distanceScale = Mathf.Clamp01((flameLength - EnemyOffset.magnitude) / flameLength);

                            float damage = hand.thruster * angleScale * distanceScale * flameMaxDamage;

                            float damageInstant = damage * Time.fixedDeltaTime;
                            //enemy.damage += damageInstant;  // why does this not throw an error??

                            if (IsOwner)
                                enemy.applyDamageFromLocation(damageInstant, hand.transform.position);
                            //enemy.applyDamageSynced.callMethod(new object[] { damageInstant, hand.transform.position });    // calls it on our copy of the enemy, which syncs it across the network


                            //print("angleOffset: " + angleOffset + " angleScale: " + angleScale + " distanceScale: " + distanceScale + " hand.thruster " + hand.thruster + " flameMaxDamage " + flameMaxDamage + " damage: " + damage );
                        }
                    }
                }
            }




            //Sword throw
            bool swordThrow = false;
            if (Input.GetMouseButton(1) && !oldRightClick)
                swordThrow = true;
            if (input.rightie.B && !input.rightie.oldB)
                swordThrow = true;
            if (swordThrow)
            {
                if (IsOwner)
                {
                    //print("Sword throw!");
                    //GameObject thrownSword = Instantiate(Sword.gameObject, Sword.transform.position, Sword.transform.rotation);

                    //GameObject thrownSword = Multi.netSpawnPrefab_ToServer(Sword.gameObject, true, NetworkManager.Singleton.OwnerClientId);
                    GameObject thrownSword = Multi.netSpawnPrefab_ToServer(SwordPrefab, true, NetworkManager.Singleton.LocalClientId);
                    thrownSword.transform.position = Sword.transform.position;
                    thrownSword.transform.rotation = Sword.transform.rotation;

                    //Sword.gameObject.SetActive(false);

                    thrownSword.transform.parent = null;
                    SwordAnimate ts = thrownSword.GetComponent<SwordAnimate>();

                    ts.body.isKinematic = false;
                    ts.body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    ts.body.velocity = body.velocity;
                    thrownSword.layer = LayerMask.NameToLayer("Default");


                    Vector3 throwDir = head.forward;
                    if (VR_mode)
                        throwDir = -rightie.transform.up;
                    ts.body.AddForce(throwDir.normalized * 50, ForceMode.VelocityChange);

                    print("Threw sword, VR_mode " + VR_mode + " direction " + throwDir);

                    //ts.lifeTimer = 2;
                    ts.lifeTimer = 5;
                    ts.dying = true;
                    ts.held = false;
                    ts.holder_PlayerId = PlayerId;
                    ts.scale = Sword.scale;

                    //// spin the sword
                    //Vector3 localAngularVelocity = Vector3.zero;
                    //localAngularVelocity.z = -200f / ts.body.inertiaTensor.z;    // will get screwed up if the inertia tensor gets rotated
                    //ts.body.angularVelocity = ts.body.transform.TransformDirection(localAngularVelocity);


                    Sword.respawn();
                }
                //Sword.GetComponent<SwordAnimate>().throwSword();
            }


            // sword slash
            if (Input.GetMouseButton(0) && !oldLeftClick)
            {
                if (IsOwner)
                {
                    print("Sword slash!");

                    if (swordSwingTimer == 0)
                    {
                        swordSwingTimer += Time.fixedDeltaTime;
                    }

                }
            }
            if (swordSwingTimer > 0)
            {
                SwordAnimate sw = Sword.GetComponent<SwordAnimate>();

                //Sword.transform.localRotation = Quaternion.Slerp(Quaternion.Euler(0,0,-70), Quaternion.Euler(0, 0, 90), swordSwingTimer / swordSwingTime);
                //Sword.transform.localRotation = Quaternion.Slerp(Quaternion.Euler(swordStart), Quaternion.Euler(swordEnd), swordSwingTimer / swordSwingTime);

                // todo redo the start and finish points to swing where ur looking
                Sword.transform.localRotation = Quaternion.SlerpUnclamped(Quaternion.Euler(swordStart), Quaternion.Euler(swordEnd), (swordSwingTimer / (swordSwingTime * .6f)) - .5f);
                //Sword.transform.rotation = head.rotation * Quaternion.SlerpUnclamped(Quaternion.Euler(swordStart), Quaternion.Euler(swordEnd), (swordSwingTimer / (swordSwingTime * .6f)) - .5f);
                //Sword.transform.rotation = head.transform.TransformDirection(Quaternion.SlerpUnclamped(Quaternion.Euler(swordStart), Quaternion.Euler(swordEnd), (swordSwingTimer / (swordSwingTime * .6f)) - .5f));

                swordSwingTimer += Time.fixedDeltaTime;
                if (swordSwingTimer > swordSwingTime)
                {
                    swordSwingTimer = 0;
                    Sword.transform.localRotation = sw.localRotation_Original;
                }
            }




            // blown off the map?
            positionWallWarnings();






            infoCard.setTextValues(playerName, damage);
            if (input == null)
                infoCard.faceCamera(Camera.main);

            oldDamage = damage;
            oldLeftClick = Input.GetMouseButton(0);
            oldRightClick = Input.GetMouseButton(1);
        }
        else    // if dead
        {
            body.velocity = Vector3.zero;
            body.useGravity = false;

            body.transform.position += Vector3.up * 2 * Time.fixedDeltaTime;

            Head_PC.SetActive(false);
            Head_VR.SetActive(false);
            Sword.gameObject.SetActive(false);
            rightie.transform.gameObject.SetActive(false);
            leftie.transform.gameObject.SetActive(false);

            respawnTimer -= Time.fixedDeltaTime;
            if (respawnTimer <= 0)
            {
                respawn();
            }
        }
    }

    public void respawn()
    {
        Sword.gameObject.SetActive(true);
        rightie.transform.gameObject.SetActive(true);
        leftie.transform.gameObject.SetActive(true);

        body.useGravity = true;
        body.transform.position = smashGame.respawnPoint.position;
        //body.transform.rotation = smashGame.respawnPoint.rotation;
        body.velocity = Vector3.zero;
        damage = 0;
        oldDamage = damage;
        dead = false;
        respawnTimer = 0;
    }

    /// <summary>
    /// List of 6 gameobjects to position around the player, warning them of the walls
    /// </summary>
    List<GameObject> wallWarnings;
    void positionWallWarnings()
    {
        if (wallWarnings == null)
        {
            smashGame = SmashGame.instance;
            print(smashGame);
            print(smashGame.wallWarning_prefab);
            wallWarnings = new List<GameObject>();
            for (int i = 0; i < 6; i++)
            {
                GameObject wallWarning = Instantiate(smashGame.wallWarning_prefab);
                wallWarning.SetActive(true);    // why is it false by default
                wallWarnings.Add(wallWarning);
            }
        }

        Collider collider = SmashGame.instance.safeZone;

        if (collider is BoxCollider) // only supports 1 box collider atm
        {
            BoxCollider bCollider = (BoxCollider)collider;
            //Vector3 localPlayerPos = bCollider.transform.InverseTransformPoint(collider.transform.position); // Player's position in the collider's local space
            Vector3 localPlayerPos = bCollider.transform.InverseTransformPoint(transform.position); // Player's position in the collider's local space
            Vector3 halfExtents = bCollider.size * 0.5f;

            // Project player's position to each face
            Vector3[] projections = new Vector3[6];
            projections[0] = new Vector3(localPlayerPos.x, localPlayerPos.y, -halfExtents.z); // Front face
            projections[1] = new Vector3(localPlayerPos.x, localPlayerPos.y, halfExtents.z);  // Back face
            projections[2] = new Vector3(-halfExtents.x, localPlayerPos.y, localPlayerPos.z); // Left face
            projections[3] = new Vector3(halfExtents.x, localPlayerPos.y, localPlayerPos.z);  // Right face
            projections[4] = new Vector3(localPlayerPos.x, -halfExtents.y, localPlayerPos.z); // Bottom face
            projections[5] = new Vector3(localPlayerPos.x, halfExtents.y, localPlayerPos.z);  // Top face

            for (int i = 0; i < 6; i++)
            {
                Vector3 worldProjection = bCollider.transform.TransformPoint(projections[i]);
                wallWarnings[i].transform.position = worldProjection;
                //print("wallWarning " + i + " pos " + wallWarnings[i].transform.position);

                // Set their 'up' direction facing into the box collider.
                // The direction will be opposite to the projection direction from the box's center.
                Vector3 directionIntoBox = (projections[i] - localPlayerPos).normalized;
                wallWarnings[i].transform.up = collider.transform.TransformDirection(-directionIntoBox);

                // is u dead?
                // See if the wall piece flipped away from the center
                if (Vector3.Dot(collider.transform.position - wallWarnings[i].transform.position, wallWarnings[i].transform.up) < 0)
                {
                    print("DEAD");
                    Debug.Log($"Player is out of bounds on the {i}-th wall.");
                    respawnTimer = 2;
                    dead = true;

                    // face backwards
                    {
                        // Only consider the horizontal components of the velocity
                        Vector3 horizontalVelocity = new Vector3(body.velocity.x, 0, body.velocity.z);

                        // Calculate the backward direction of the velocity
                        Vector3 backwardDirection = -horizontalVelocity.normalized;
                        float angleToRotate = Vector3.SignedAngle(head.forward, backwardDirection, Vector3.up);
                        // Convert the angle into a quaternion
                        Quaternion rotation = Quaternion.Euler(0, angleToRotate, 0);

                        // Rotate the XR_Origin accordingly
                        input.XR_Origin.rotation *= rotation;
                    }
                    body.position += body.velocity.normalized * 5f;
                    body.velocity = Vector3.zero;
                    break;
                }

                //Vector3 toPlayer = transform.position - wallWarnings[i].transform.position;
                //print("toPlayer " + toPlayer);
                //print("wallWarnings[i].transform.up " + wallWarnings[i].transform.up);
                //float dotProduct = Vector3.Dot(wallWarnings[i].transform.up, toPlayer.normalized);

                //print("i " + i + " dotProduct " + dotProduct);
                //if (dotProduct < 0)
                //{
                //    print("DEAD");
                //    Debug.Log($"Player is out of bounds on the {i}-th wall.");
                //}
            }
        }
    }


    float swordSwingTime = .2f;
    float swordSwingTimer = 0;



    /// <summary>
    /// apply damage with character throwback. (TODO, pausing upon punch)
    /// </summary>
    /// <param name="Damage"></param>
    public void applyDamageFromLocation(float Damage, Vector3 source)
    {
        //print("Damage applied: " + Damage + " from distance " + (transform.position - source).ToString());

        applyDamageFromDirection(Damage, (transform.position - source).normalized);
    }
    public void applyDamageFromDirection(float Damage, Vector3 direction)
    {
        //Vector3 throwback = (transform.position - source).normalized * damage * .02f * Damage;
        Vector3 throwback = direction.normalized * damage * .02f * Damage;

        if (Damage > .1f)
        {
            applyDamageSynced.callMethod(new object[] { Damage, throwback });
        }
    }
    /// <summary>
    /// This function exists on dummy copies of players on the local client. The client calls it for damage, 
    /// it replicates over the network, and applies damage to the real players too
    /// </summary>
    public void applyDamage(float Damage, Vector3 throwBack)
    {
        print("Damage applied: " + Damage + " with throwback " + (throwBack).ToString());
        

        damage += Damage;
        body.AddForce(throwBack, ForceMode.Impulse);
    }

    public float getDamage() { return damage; }






    /// <summary>
    /// play jet thruster animations etc
    /// </summary>
    public class Hand// : NetworkBehaviour
    {
        public GameObject handJet;

        public Transform transform;
        public float thruster = 0;
        //NetworkVariable<float> thrusterSynced = new NetworkVariable<float>();
        public Vector3 thrusterDirection = Vector3.zero;

        public ParticleSystem handJetFlames;
        public Vector3 handJetStartSize;

        // todo spawn particles on thrust
        public void setHandjet(Transform handJetParticles)
        {
            handJetFlames = Instantiate(handJetParticles, transform).GetComponent<ParticleSystem>();
            handJetStartSize = handJetFlames.transform.localScale;
            handJetFlames.Stop();
            //print("thrusterSynced");
            //thrusterSynced = new NetworkVariable<float>(0f);
            //thrusterSynced.Value = 0f;
        }

        public void update()
        {
            //thrusterSynced.Value = thruster;
            //if (IsOwner)
            //    thrusterSynced.Value = thruster;
            //thruster = thrusterSynced.Value;

            if (thruster > 0)
            {
                if (!handJetFlames.isPlaying)
                    handJetFlames.Play();
                handJetFlames.transform.localScale = handJetStartSize * thruster;

                Quaternion rotation = Quaternion.LookRotation(thrusterDirection, Vector3.up);
                handJetFlames.transform.rotation = rotation;
                //print("rotation " + rotation + " thruster " + thruster);
            }
            else
                handJetFlames.Stop();

            if (handJet != null)
            {
                HandJetAnimate script = handJet.GetComponent<HandJetAnimate>();

                script.thrust = thruster;
                script.direction = thrusterDirection;
            }
        }

        // test
        //private void onChange(float previous, float current)
        //{
        //    print(current);
        //}
    }

}







/// <summary>
/// The info above the player's head. (Meant to be easy to slot into new projects, fyi)
/// </summary>
public class InfoCard   // separate all this into a class for mess
{
    GameObject infoCard;
    TextMeshPro info;

    public InfoCard(Transform transform)
    {
        infoCard = new GameObject("InfoCard");
        infoCard.transform.parent = transform;

        info = infoCard.AddComponent<TextMeshPro>();
        infoCard.GetComponent<RectTransform>().sizeDelta = new Vector2(1, 1);
        infoCard.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        //infoCard.GetComponent<RectTransform>().localPosition = new Vector3(0, 0, 0);
        infoCard.transform.localPosition = new Vector3(0, 1f, 0);


        //setInfoCardValues(info, playerName, damage);
        //setDefaultFont(info);
    }



    //public void setInfoCardValues(TextMeshPro info, string PlayerName, float Damage)
    public void setTextValues(string PlayerName, float Damage)
    {
        //< color =#FF000088>

        float m = 200;   // max damage for color coding
        float mq = m / 4;
        float d = Damage;

        Color color = new Color(Mathf.Clamp01(3 - d / mq) + Mathf.Clamp01((d / mq - 4) / 4), Mathf.Clamp01(2 - d / mq), Mathf.Clamp01(1 - d / mq) + Mathf.Clamp01(d / mq - 2.8f), 1);


        //info.text = "<align=\"center\">" + PlayerName + "<br> <size=200%><color=#" + color.ToHexString() + ">" + Damage.ToString("F2") + "</color>%";
        //info.text = "<align=\"center\">" + PlayerName + "<br> <size=200%><color=#" + color.ToHexString() + ">" + Damage.ToString("F0") + "</color>%";
        info.text = "<align=\"center\">" + PlayerName + "<br> <size=200%><color=#" + color.ToHexString() + ">" + Damage.ToString("F0") + "</color>%";

        //if (input == null)  // only for other characters; TODO, a better way to do this (keep the info card from freaking out)
        //{

        //}
    }

    /// <summary>
    /// Only call this for infocards that aren't the player, or it'll freak out. (use inputScript == null to check)
    /// </summary>
    /// <param name="cam"></param>
    public void faceCamera(Camera cam)
    {
        info.rectTransform.LookAt(Camera.main.transform);
        info.rectTransform.Rotate(0, 180, 0);
    }

    //public void setDefaultFont(TextMeshPro info)  // default font
    public void setDefaultFont()
    {
        info.fontSize = 2;
        info.enableWordWrapping = false;
        info.fontStyle = FontStyles.SmallCaps;

        //info.fontMaterial

        //info.fontMaterial = Resources.Load("LiberationSans SDF", typeof(Material)) as Material;

        info.fontMaterial.shader = Shader.Find("TextMeshPro/Distance Field");

        info.faceColor = new Color(1, 1, 1, 1); // makes a new material instance so these settings don't hit all instances..? Not sure... http://digitalnativestudios.com/textmeshpro/docs/ScriptReference/TextMeshPro-fontMaterial.html
        info.fontMaterial.SetColor("_FaceColor", new Color(1, 1, 1, 1));

        info.fontMaterial.SetFloat("_Underlay", 1);
        //info.fontSharedMaterial.SetColor("_UnderlayColor", new Color(.5f, 0, 0, 1));
        info.fontMaterial.SetColor("_UnderlayColor", new Color(0, 0, 0, 1));
        info.fontMaterial.SetFloat("_UnderlayDilate", 0.755f);
        // glow rim
        info.fontMaterial.SetFloat("_Glow", 1);
        info.fontMaterial.SetColor("_GlowColor", new Color(0, 1, 0, .5f));
        info.fontMaterial.SetFloat("_GlowOffset", 1f);
        info.fontMaterial.SetFloat("_GlowInner", .595f);

        info.faceColor = new Color(1, 1, 1, 1);
    }
}