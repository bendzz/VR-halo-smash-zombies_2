using System.Collections;
using System.Collections.Generic;
//using System.Drawing;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;



/// <summary>
/// Script to control smash bros style gameplay stuff, and enemy visuals
/// </summary>
//public class myInputTests : MonoBehaviour
public class SmashCharacter : NetworkBehaviour
{
    public Rigidbody body;
    public FeetCollider feetCollider;


    // To get the hands and head positions; going to try and keep the enemy decoupled from this tho, for later AI and networking etc
    public myInputTests input;


    // game settings
    string playerName = "default player";


    [Tooltip("A particle system prefab to shoot out of the jets")]
    public GameObject handJetsParticlesPrefab;

    [Tooltip("jet prefab with script")]
    public GameObject handJetPrefab;


    private float damage = 0;    // smash bros damage, amplifies knockback
    float oldDamage = 0;

    // Output
    public bool stunLocked = false;    // if true, controls are unresponsive
    public bool limp = false;   // after a stun, the character is limp until controls are pressed again. They smash into the ground

    public Hand leftie = new Hand();
    public Hand rightie = new Hand();
    Transform head;


    public List<Hand> hands;
    float stunTimer = 0; // how many seconds left in the stun

    /// <summary>
    /// All characters in the scene
    /// </summary>
    public static List<SmashCharacter> characters = new List<SmashCharacter>();


    InfoCard infoCard;






    // Start is called before the first frame update
    void Start()
    //public override void OnNetworkSpawn()
    {
        //if (!characters.Contains(this))
        //    characters.Add(this);


        //// sync to input script (if it exists)
        //if (input != null)
        //{
        //    print("init " + this.name + " input " + input.name);
        //    print(input.leftie);
        //    leftie.transform = input.leftie.transform;
        //    rightie.transform = input.rightie.transform;
        //    head = input.XR_Headset;
        //}
        //else
        //{
        //    // AI controlled then
        //    leftie.transform = rightie.transform = head = transform;
        //    limp = true;    // for debugging
        //}

        //hands = new List<Hand>();
        //hands.Add(leftie);
        //hands.Add(rightie);

        //foreach (Hand hand in hands)
        //{
        //    //Hand.handJetFlames = Instantiate(handJetsParticlesPrefab, Hand.rectTransform).GetComponent<ParticleSystem>();
        //    hand.setHandjet(handJetsParticlesPrefab.transform);
        //    if (handJetPrefab != null)
        //        hand.handJet = Instantiate(handJetPrefab, hand.transform);
        //}


        //// player info Card
        //infoCard = new InfoCard(transform);
        //infoCard.setDefaultFont();
    }

    public override void OnNetworkSpawn()   // TODO haven't changed anything for networking yet...
    {
        if (!characters.Contains(this))
            characters.Add(this);


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
            playerName = LobbyMultiplayer.instance.PlayerName;
        // todo sync
    }



    // test
    [ServerRpc]
    public void pingServerRpc(float time, ServerRpcParams pars = default)
    {
        var clientId = pars.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.ContainsKey(clientId))
        {
            var client = NetworkManager.ConnectedClients[clientId];
            // Do things for the client (our local copy) that sent the RPC
            // client.PlayerObject.GetComponent<SmashCharacter>().pingServerRpc(time);
        }
        print(clientId + " pinged the server with " + time);

        pingClientRpc(time, clientId);  // send to all clients
    }

    [ClientRpc]
    public void pingClientRpc(float time, ulong originalSender, ClientRpcParams pars = default)
    {
        var thisClientId = NetworkManager.Singleton.LocalClientId;
        print("Server pinged client " + thisClientId + " (originally from client "+ originalSender + ") with " + time);
    }



    // update is called once per frame
    //void update()
    private void FixedUpdate()
    {
        if (IsOwner)
            pingServerRpc(Time.time);   // send to server



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
            } else
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
        }
        
        // apply damage to enemies
        foreach(SmashCharacter enemy in characters)
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


                        enemy.applyDamage(damageInstant, hand.transform.position);


                        //print("angleOffset: " + angleOffset + " angleScale: " + angleScale + " distanceScale: " + distanceScale + " hand.thruster " + hand.thruster + " flameMaxDamage " + flameMaxDamage + " damage: " + damage );
                    }
                }
            }
        }

        //// get and print the playerName from networking
        //if (NetworkManager.Singleton != null)
        //{
        //    if (NetworkManager.Singleton.ConnectedClients.TryGetValue(OwnerClientId, out var networkedClient))
        //    {
        //        //playerName = networkedClient.Player.DisplayName;
        //        playerName = networkedClient.PlayerObject.name;
        //        print("playerName: " + playerName);
        //    }
        //}
        //print("playerControllerId " + LobbyPlayerJoined);


        infoCard.setTextValues(playerName, damage);
        if (input == null)
            infoCard.faceCamera(Camera.main);

        oldDamage = damage;
    }


    /// <summary>
    /// apply damage with character throwback. (TODO, pausing upon punch)
    /// </summary>
    /// <param name="Damage"></param>
    public void applyDamage(float Damage, Vector3 source)
    {
        //Vector3 throwback = (transform.position - source).normalized * (damage + (10 * Mathf.Clamp01(damage - 2))) * .05f;
        Vector3 throwback = (transform.position - source).normalized * damage * .02f * Damage;
        body.AddForce(throwback, ForceMode.Impulse);

        damage += Damage;
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
            print("thrusterSynced");
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