using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static Multi;

public class SwordAnimate : NetBehaviour
{
    //public SyncedProperty holder_SyncedProperty;    // used to get the player that held/threw the sword, over the network
    //public ulong holder_PlayerId;    // used to get the player that held/threw the sword, over the network
    public string holder_PlayerId;    // used to get the player that held/threw the sword, over the network
    public SmashCharacter holder;
    public GameObject sword;
    public GameObject swordTipPoint;
    public Rigidbody body;

    public Collider colliderPhysical;
    public Collider collderTrigger;

    public bool held = false;

    public float lifeTimer = 0;
    public bool dying = false;
    public bool energized = false;

    public Vector3 scale_Original;
    public Vector3 localPosition_Original;
    public Quaternion localRotation_Original;
    public float scale = 1;

    // I-frames
    public SmashCharacter dontHitTwice = null;
    public float dontHitTwiceTimer = 0;

    public List<Color> colorList;






    /// <summary>
    /// For estimating swing speed
    /// </summary>
    ScrollList<Vector3> swordTipPositions;
    ScrollList<Vector3> swordTipLocalPositions;

    MeshRenderer renderer;
    Material swordGlowMaterial;

    Multi.Entity entity;

    SyncedProperty body_velocity;
    SyncedProperty body_angularVelocity;

    // Start is called before the first frame update
    public override void OnNetworkSpawn()
    {
        print("OnNetworkSpawn Sword: " + gameObject.name);
        scale_Original = sword.transform.localScale;
        localPosition_Original = sword.transform.localPosition;
        localRotation_Original = sword.transform.localRotation;

        gameObject.layer = 2;    //testing

        SetColliders();

        swordTipPositions = new ScrollList<Vector3>(4);
        swordTipLocalPositions = new ScrollList<Vector3>(4);


        renderer = GetComponent<MeshRenderer>();
        swordGlowMaterial = GetMaterialInstanceByName(renderer, "swordGlow");


        //swordGlowMaterial.SetColor("_Color", Color.red);
        //swordGlowMaterial.SetColor("_EmissionColor", Color.red);

        // To ensure that emission is effective, you might also want to enable emission globally
        DynamicGI.SetEmissive(renderer, swordGlowMaterial.GetColor("_EmissionColor"));



        //entity = new Multi.Entity(this);
        entity = new Multi.Entity();
        //entity.addToLocalEntities();

        entity.setCurrents(this, this.gameObject, IsOwner);
        entity.addSyncedProperty(transform);
        entity.addSyncedProperty(scale);
        entity.addSyncedProperty(lifeTimer);
        //entity.addSyncedProperty(holder_SyncedProperty);
        entity.addSyncedProperty(holder_PlayerId);

        entity.setCurrents(body, gameObject, IsOwner);  // rigidbody
        entity.addSyncedProperty(body.isKinematic);     // otherwise it throws a bunch of "nooo you can't set velocity on kinematics!" errors
        body_velocity = entity.addSyncedProperty(body.velocity);
        body_angularVelocity = entity.addSyncedProperty(body.angularVelocity);

        tryToSetColors();
    }


    /// <summary>
    /// Finds the material, creates an instance of it, assigns it to the MeshRenderer, returns the instance
    /// </summary>
    private static Material GetMaterialInstanceByName(MeshRenderer renderer, string materialName)
    {
        foreach (Material mat in renderer.materials)
        {
            if (mat.name.StartsWith(materialName)) // Using StartsWith because Unity appends " (Instance)" to material names when calling .materials
            {
                Material newMatInstance = new Material(mat);
                renderer.material = newMatInstance; // Assign the instance back to the MeshRenderer
                return newMatInstance;
            }
        }

        return null; // Return null if the material wasn't found
    }


    // Update is called once per frame
    void Update()
    {

        if (dying)
        {
            lifeTimer -= Time.deltaTime;
            if (lifeTimer < 0)
            {
                gameObject.transform.localScale /= (1 - lifeTimer * 1);

                if (gameObject.transform.localScale.x < .01f)
                    Destroy(gameObject);
                //Destroy(gameObject);
            }
        }

        // grow respawned sword
        if (scale < 1 && held)
        {
            scale += Time.deltaTime * (1f/3f);
            //print("scaling " + scale);
        }
        gameObject.transform.localScale = scale_Original * scale;


        //if (held)
        //    colliderPhysical.enabled = false;
        //else
        colliderPhysical.enabled = true;

        // spin the sword!
        if (!held)
        {
            //print("velocity " + body.velocity.magnitude);
            if (body.velocity.magnitude > 5)
                energized = true;
            else
                energized = false;
        }
        if (energized)
        {
            body.maxAngularVelocity = 1000; // why is that limited to 7 by default blah
            body.angularVelocity = body.transform.TransformDirection(new Vector3(0, 0, -30));
        }



        if (dontHitTwice != null)
        {
            if (dontHitTwiceTimer < 0)
                dontHitTwice = null;
            else
                dontHitTwiceTimer -= Time.deltaTime;
        }

        if (held) {
            body_velocity.syncingEnabled = false;
            body_angularVelocity.syncingEnabled = false;
        } else
        {
            //print("enabling sword sync for " + gameObject.name);
            //print(body_velocity);
            //print(body_angularVelocity);
            body_velocity.syncingEnabled = true;
            body_angularVelocity.syncingEnabled = true;
        }

    }

    /// <summary>
    /// Only works if holder != null
    /// </summary>
    void tryToSetColors()
    {
        if (holder == null)
        {
            if (holder_PlayerId == "")
                return;
            if (SmashCharacter.characters_byPlayerId.ContainsKey(holder_PlayerId))
                holder = SmashCharacter.characters_byPlayerId[holder_PlayerId];
            else 
                Debug.Log("holder_PlayerId " + holder_PlayerId + " not found in SmashCharacter.characters_byPlayerId");
            print("holder_PlayerId " + holder_PlayerId);
        }
        else
        {
            swordTipLocalPositions.append(swordTipPoint.transform.position - holder.transform.position);    // won't account for spins, but ig that's good

            swordGlowMaterial.SetColor("_Color", holder.playerColor);
            swordGlowMaterial.SetColor("_EmissionColor", holder.playerColor);
        }
    }

    private void FixedUpdate()
    {

        tryToSetColors();

    }


    void OnTriggerEnter(Collider other)
    {

        IntangibleHitbox hitbox = other.gameObject.GetComponent<IntangibleHitbox>();
        if (hitbox != null)
        {
            //print("hitbox " + hitbox.gameObject.name);
            if (hitbox.representsObject != null)
            {
                //print("hitbox.representsObject " + hitbox.representsObject.name);
                SmashCharacter hitBoi = hitbox.representsObject.GetComponent<SmashCharacter>();
                if (hitBoi != null && hitBoi != dontHitTwice)
                {
                    print("HITBOI - " + hitBoi.playerName);

                    if (!held && energized) // throw damage
                    {
                        hitBoi.applyDamageFromDirection(20 * scale, body.velocity);

                        //body.velocity = -body.velocity * .5f;
                        body.velocity = (-body.velocity.normalized + Vector3.up).normalized * body.velocity.magnitude * .5f;

                        dontHitTwice = hitBoi;
                        dontHitTwiceTimer = .5f;
                    } else if (held)    // schwing
                    {
                        if (hitBoi == holder)
                            return;

                        if (hitBoi.body == null)    // why is this happening?
                        {
                            Debug.LogError(hitBoi.playerName + " has no body");
                            return;
                        }

                        Vector3 tipDelta = swordTipPositions.getOldest() - swordTipPositions.getNewest();
                        //print("tipDelta.magnitude " + tipDelta.magnitude);

                        Vector3 charDelta = (hitBoi.body.velocity - holder.body.velocity);
                        //print ("relative velocity " + charDelta.magnitude);
                        tipDelta -= charDelta * (Time.fixedDeltaTime * swordTipPositions.maxSize) * 2;  // lunging at them matters

                        //print("tipDelta.magnitude adjusted " + tipDelta.magnitude);

                        Vector3 localTipDelta = swordTipLocalPositions.getOldest() - swordTipLocalPositions.getNewest();
                        print("localTipDelta.magnitude " + localTipDelta.magnitude);

                        float swingMultiplier = Mathf.Clamp01((localTipDelta.magnitude - .2f) / 1.2f);  // only give them the hit if they swing
                        print("swingMultiplier " + swingMultiplier);

                        // tipDelta is like 1.7 on a swing and 3.4 if they rush each other

                        //hitBoi.applyDamageFromLocation(2 * scale, holder.transform.position);
                        //hitBoi.applyDamageFromLocation(15 * tipDelta.magnitude * scale, holder.transform.position);
                        hitBoi.applyDamageFromLocation(15 * tipDelta.magnitude * swingMultiplier * scale, holder.transform.position);


                        dontHitTwice = hitBoi;
                        dontHitTwiceTimer = .4f;
                    }
                }
            }
        }
        //}
    }

    private void OnTriggerStay(Collider other)
    {
        // check if it's already intersecting a player, warn that swings won't work by turning red
        IntangibleHitbox hitbox = other.gameObject.GetComponent<IntangibleHitbox>();
        if (hitbox != null)
        {
            //print("hitbox " + hitbox.gameObject.name);
            if (hitbox.representsObject != null)
            {
                //print("hitbox.representsObject " + hitbox.representsObject.name);
                SmashCharacter hitBoi = hitbox.representsObject.GetComponent<SmashCharacter>();
                if (hitBoi != null && hitBoi != dontHitTwice)
                {
                    if (hitBoi == holder)
                        return;

                    swordGlowMaterial.SetColor("_Color", Color.red);
                    swordGlowMaterial.SetColor("_EmissionColor", Color.red);
                    DynamicGI.SetEmissive(renderer, swordGlowMaterial.GetColor("_EmissionColor"));

                }
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        DynamicGI.SetEmissive(renderer, swordGlowMaterial.GetColor("_EmissionColor"));
    }

    void OnCollisionEnter(Collision collision)
    {
        //print("COLLISION! " + collision.gameObject.name);
    }




    //LayerMask playerLayerMask;
    //MeshCollider triggerCollider;

    //GameObject SetColliders()
    void SetColliders()
    {
        MeshCollider[] meshColliders = GetComponents<MeshCollider>();
        foreach (var meshCollider in meshColliders)
        {
            if (meshCollider.isTrigger)
            {
                collderTrigger = meshCollider;
            }
            else
                colliderPhysical = meshCollider;
        }
        //return null; // Return null if no hit
    }



    /// <summary>
    /// Pretend you're a newly spawned sword growing back to size
    /// </summary>
    public void respawn()
    {
        scale = 0;
    }









    // marginally more updated than the one in FeetCollider.cs
    /// <summary>
    /// Overwrites the oldest value when appended; used for calculating a running average.
    /// Note: "oldest" will return blank values until the list has been filled once
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ScrollList<T>
    {
        public List<T> list;
        public int maxSize = 0;
        /// <summary>
        /// the list index of the "newest" value in the runningAverage
        /// </summary>
        public int newest = 0;
        /// <summary>
        /// the list index of the value that'll be overwritten soonest
        /// </summary>
        public int oldest = 0;

        public ScrollList()
        { list = new List<T>(); }

        public ScrollList(int MaxSize)
        {
            maxSize = MaxSize;
            list = new List<T>(new T[MaxSize]);
        }




        /// <summary>
        /// populates the list with your given value; use at the start
        /// </summary>
        /// <param name="MaxSize"></param>
        /// <param name="defaultValue"></param>
        public void sizeAndFillEmptyList(int MaxSize, T defaultValue)
        {
            list = new List<T>(new T[MaxSize]);

            for (int i = 0; i < MaxSize; i++)
            {
                list[i] = defaultValue;
            }
            maxSize = MaxSize;
        }

        /// <summary>
        /// Lets you change the newest item in the runningAverage but doesn't shift the others down
        /// </summary>
        public void overwriteNewest(T value)
        {
            list[newest] = value;
        }

        /// <summary>
        /// adds a new value to the runningAverage, returns the oldest value and overwrites it
        /// </summary>
        /// <param name="value"></param>
        public T append(T value)
        {
            T deletedValue = list[oldest];
            list[oldest] = value;

            newest = oldest;
            oldest++;

            if (oldest >= maxSize)
                oldest = 0;

            return deletedValue;
        }

        /// <summary>
        /// Doesn't change the list; just returns the oldest value
        /// </summary>
        /// <returns></returns>
        public T getOldest()
        {
            return list[oldest];
        }

        public T getNewest()
        {
            return list[newest];
        }

        public void debugPrintList()
        {
            for (int i = 0; i < maxSize; i++)
            {
                print(list[i]);
            }
        }
    }
}
