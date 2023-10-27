using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordAnimate : MonoBehaviour
{
    public GameObject sword;
    public Rigidbody body;
    Collider colliderPhysical;
    Collider collderTrigger;

    public bool held = false;

    public float lifeTimer = 0;
    public bool dying = false;
    public bool energized = false;

    public Vector3 scale_Original;
    //public Vector3 position_Original;
    public float scale = 1;

    // I-frames
    public SmashCharacter dontHitTwice = null;
    public float dontHitTwiceTimer = 0;

    // Start is called before the first frame update
    void Start()
    {
        scale_Original = sword.transform.localScale;

        gameObject.layer = 2;    //testing

        SetColliders();
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
        if (scale < 1 && !dying)
        {
            scale += Time.deltaTime * (1f/3f);
            //print("scaling " + scale);

            gameObject.transform.localScale = scale_Original * scale;
        }

        if (held)
            colliderPhysical.enabled = false;
        else
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
    }



    void OnTriggerEnter(Collider other)
    {
        if (!held && energized)  // throw damage
        {
            //Debug.Log("Sword trigger hit! " + other.gameObject.name);

            //int playerLayer = LayerMask.NameToLayer("Player");
            //if (other.gameObject.layer == playerLayer)
            //{
            //    Debug.Log("Triggered by Player! Object " + other.gameObject.name);
            //}

            //SmashCharacter hitBoi = FindSmashCharacterInParents(other.gameObject);
            //if (hitBoi != null)
            //{
            //    print("hitBoi " + other.gameObject);
            //}

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
                        print("HITBOI! " + hitBoi.damage);
                        hitBoi.applyDamageFromDirection(20 * scale, body.velocity);

                        //body.velocity = -body.velocity * .5f;
                        body.velocity = (-body.velocity.normalized + Vector3.up).normalized * body.velocity.magnitude * .5f;

                        dontHitTwice = hitBoi;
                        dontHitTwiceTimer = .5f;
                    }
                }
            }
        }
    }


    //public SmashCharacter FindSmashCharacterInParents(GameObject obj)
    //{
    //    Transform currentTransform = obj.transform;
    //    while (currentTransform != null)
    //    {
    //        SmashCharacter smashCharacter = currentTransform.GetComponent<SmashCharacter>();
    //        if (smashCharacter != null)
    //        {
    //            return smashCharacter;
    //        }
    //        currentTransform = currentTransform.parent;
    //    }
    //    return null;
    //}

    //private void OnTriggerEnter(Collider other)
    //{
    //    if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
    //    {
    //        GameObject playerObject = other.gameObject;
    //        // Do something with playerObject if needed
    //        Debug.Log("Player has entered the trigger! " + playerObject.name);
    //    } else
    //    {
    //        Debug.Log("Sword trigger hit! " + other.gameObject.name);

    //        // wish I could do better here but unity doesn't have good isTrigger collision info
    //        Vector3 closestPoint = other.ClosestPoint(transform.position);

    //        // Calculate the direction from this object to the closest point
    //        Vector3 rayDirection = closestPoint - transform.position;

    //        print("rayDirection" + rayDirection);
    //        RaycastHit hit;
    //        if (Physics.Raycast(transform.position, rayDirection, out hit, rayDirection.magnitude))
    //        {
    //            Vector3 hitNormal = hit.normal;

    //            // Reflect the velocity and damp it
    //            body.velocity = Vector3.Reflect(body.velocity, hitNormal) * 0.5f;
    //            print("Reflected! " + body.velocity + " " + hitNormal);
    //        }
    //    }
    //}


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
}
