using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


// TODO:
// - need to do a second ground touch, extended downward some amount, to prevent the player from falling, and let them move
//   - but how will their model feet still touch the ground?
//   - (this will also fix downhill jitter and jitter approaching edges)
// - Should separate the biased ground slope from actual ground slope, and use the former for velocity while the latter for
//   positional correction, to prevent you superjumping by sprinting up a slope. (Will still need smoothing tho)
//    - just subtract the two to get the positional offset
// - A gradual push-you-up when on a platform moving up; otherwise it hits the jittery hard height limit
// -come up with a better way to accelerate you downslope than gravity (which makes you slowly sink)

// -use a scriptable object to set all the basic startup settings, and have a few presets
// -Make it so you can only run on really tilted ground normals if you're running At the wall, or were within a second ago


/// <summary>
/// Smooths out complex and cluttered ground to keep VR movement uber smooth
/// </summary>
public class FeetCollider : MonoBehaviour
{
    public bool debug = true;
    public bool debugRays = true;
    [Tooltip("Use slowmo to debug the running, IF debug mode is on")]
    public float gameSpeed = 1;

    public Rigidbody body;

    [Header("Startup settings")]
    [Tooltip("The max height this player can stand to; to determine if VR players are crouching, or force a crouch")]
    public float playerDefaultHeight = 1.8f;
    [Tooltip("Adjust this dynamically to match the VR headset height")]
    public float playerHeight = 1.8f;
    [Tooltip("how high up to case the ground-finding rays from. 0.5 is halway.")]
    public float raycastPoint = 0.6f;

    [Tooltip("how much shorter/closer to the ground the player can get before they're gently pushed upward. 0.15 is 15% of their height. Too small a deadzone will expose groundHeight jitter")]
    public float heightDeadzone = 0.15f;
    [Tooltip("If the player is shorter than X percentage of their height, they'll be forced upward. Most relevant on lifts or while running fast")]
    public float hardLimitHeight = 0.4f;

    [Tooltip("When going down a slope, let the player sink by X% of their height so they don't keep bouncing off the slope")]
    public float downslopeLowering = 0.0f;  // todo unused?

    public float sphereCastRadius = 0.1f;

    public int xRays = 5;
    public int yRays = 3;

    [Tooltip("the degrees the legs can reach outward, ie the reach of the player's legs. (Note: Might be short by up to 1 ray size)")]
    public float xDegrees = 60;
    public float yDegrees = 30;

    public float tiltForward = 30;

    public float rayLength = 3;
    // TODO proper leg length and legs origin based on height..? Might break while crawling

    //[Tooltip("The point to cast rays from on the player. Offset it up a bit in case the headset is at 0,0,0")]
    //public Vector3 centerOffset = Vector3.up * .2f;

    [Tooltip("How many frames to smooth the ground height over")]
    public int smoothing = 10;

    // TODO
    //[Tooltip("if it's .5, the vector will be smoothed over the amount of frames it takes to travel .5 meters.")]
    //public float groundSlopeSmoothing = .3f;
    [Tooltip("Max number of frames the ground vector can be smoothed over")]
    public int maxGroundSlopeSmoothing = 10;

    //public float rayAccelSmoothing = 2;


    [Header("Dynamically Changed during play (by other scripts)")]
    [Tooltip("If true, the player will slide up vertical walls. If false, they can only run up slopes")]
    public bool runUpWalls = true;
    // todo add a proper crouch/limp setting
    [Tooltip("Amount of player speed that will remain after 1 second of touching the ground")]
    public float drag = .5f;



    [Header("Outputs to other scripts")]
    [Tooltip("output for other scripts to use; the weighted average height of the floor it can reach. NAN means no floor.")]
    public float groundHeight = 0;
    public float smoothedGroundHeight = 0;
    [Tooltip("output for other scripts to use; the weighted average angle of the floor, for applying running force.")]
    public Vector3 surfaceNormal = Vector3.zero;

    [Tooltip("the slope in the currently moving direction")]
    public Vector2 groundSlope = new Vector2();
    public bool isGrounded = false;



    List<ray> rays = new List<ray>();

    /// <summary>
    /// To make a running average of the player height over time
    /// </summary>
    ScrollList<float> heightsList = new ScrollList<float>();
    const float defaultHeight = -999999;

    ScrollList<Vector2> groundSlopeList = new ScrollList<Vector2>();

    //void Start()
    //{

    //}



    bool pushUpToDeadzone = false;
    private void FixedUpdate()
    {
        if (debug)
            Time.timeScale = gameSpeed;


        if (heightsList.maxSize != smoothing)
            heightsList.sizeAndFillEmptyList(smoothing, defaultHeight);

        if (groundSlopeList.maxSize != maxGroundSlopeSmoothing)
            groundSlopeList = new ScrollList<Vector2>(maxGroundSlopeSmoothing);



        float movingAngleXZ = -Vector2.SignedAngle(Vector2.up, new Vector2(body.linearVelocity.x, body.linearVelocity.z));
        //print(movingAngleXZ); 

        Vector3 feetPos = body.position - Vector3.up * (playerHeight * .5f);    // Where the feet would be standing still


        Vector3 playerForce = Physics.gravity + body.linearVelocity;
        //Vector3 playerForce = Physics.gravity;
        playerForce = Vector3.Normalize(playerForce);
        surfaceNormal = Vector3.zero;

        Vector3 raycastOrigin = body.position + new Vector3(0,playerHeight * (raycastPoint - .5f), 0);


        groundHeight = 0;
        Vector3 raysCenterOfMass = Vector3.zero;    // center of the ray hits
        float surfaceWeight = 0;
        int rayi = 0;
        // cast rays
        for (int x = 1; x <= xRays; x++)
        {
            for (int y = 1; y <= yRays; y++)
            {
                float xx = x - ((xRays + 1) / 2);
                float yy = y - ((yRays + 1) / 2);

                float xAngle = (xx + Random.value - .5f) * (xDegrees / (float)xRays * 2);
                float yAngle = (yy + Random.value - .5f) * (yDegrees / (float)yRays * 2);

                //Vector3 direction = Quaternion.Euler(xAngle, body.rotation.eulerAngles.y, yAngle) * playerForce;
                Vector3 direction = Quaternion.Euler(xAngle, 0, yAngle) * Vector3.down;
                direction = Quaternion.Euler(-tiltForward, movingAngleXZ, 0) * direction;


                RaycastHit hiti;
                //Physics.SphereCast(rectTransform.position, 0.1f, direction, out hiti, rayLength, 1, QueryTriggerInteraction.Ignore);
                Physics.SphereCast(raycastOrigin, sphereCastRadius, direction, out hiti, rayLength, 1, QueryTriggerInteraction.Ignore);


                //// initialize ray
                rayi = (y - 1) * xRays + x - 1;
                if (rays.Count <= rayi)
                {
                    do
                    {
                        //rays.Add(new ray(smoothing));
                        rays.Add(new ray());
                    } while (rays.Count <= rayi);
                }
                rays[rayi].hit = hiti;


                if (hiti.point == Vector3.zero) continue;

                if (debug && debugRays)
                {
                    Debug.DrawLine(raycastOrigin, direction * rayLength + raycastOrigin, new Color(0, 1, 1f));
                    Debug.DrawLine(raycastOrigin, hiti.point, new Color(1, 0, .5f));
                }





                Vector3 n = hiti.normal;
                float weighting;
                if (runUpWalls)
                {
                    //weighting = Mathf.Pow(Vector3.Dot(-n, playerForce), 2); // lets you run up walls really well
                    weighting = Mathf.Pow(Vector3.Dot(-n, (playerForce + Vector3.down).normalized), 2); // lets you run up walls if sprinting
                } else
                    weighting = Mathf.Pow(Vector3.Dot(-n, Vector3.down), 2);    // no wall running


                    
                surfaceNormal += n * weighting;


                //weighting /= (hiti.point - (feetPos + Vector3.up * 2)).magnitude * 100;  // focus on points around feet
                weighting /= (hiti.point - (feetPos + Vector3.up * 0)).magnitude * 100;  // focus on points around feet
                //weighting = 0;
                //if (hiti.point.y < feetPos.y)   // TEMP for debugging, TODO
                //    weighting = 0;

                groundHeight += hiti.point.y * weighting;
                surfaceWeight += weighting;


                raysCenterOfMass += hiti.point * weighting;



                //if (debug)
                //Debug.DrawLine(hiti.point, hiti.point + Vector3.up * Mathf.Abs(accel) * 3, new Color(1, 0, 0f));
                //    Debug.DrawLine(hiti.point, hiti.point + Vector3.up * (1 - accelDamping), new Color(1, 0, 0f));

                hiti = new RaycastHit();
            }
        } 

        groundHeight /= surfaceWeight;
        raysCenterOfMass /= surfaceWeight;

        // Calc smoothedHeight
        // (note; not currently used for anything)
        {
            heightsList.append(groundHeight);

            smoothedGroundHeight = 0;
            float totalHeights = 0;
            for (int i = 0; i < heightsList.maxSize; i++)
            {
                float h = heightsList.list[i];
                if (h != defaultHeight)
                {
                    smoothedGroundHeight += h;
                    totalHeights += 1;
                }
            }
            smoothedGroundHeight /= totalHeights;
        }

        // isGrounded?
        isGrounded = false;
        if (!float.IsNaN(groundHeight))
        {
            Vector3 p = body.position;

            if (groundHeight > p.y + playerHeight * -.5f)   // if intersecting player
                isGrounded = true;
        }


        // Calc groundslope, using the center of the ray hits
        {
            groundSlope = new Vector2();
            Vector3 forwardVector = Quaternion.Euler(0, movingAngleXZ, 0) * Vector3.forward;    // on a flat plane

            // A bias so if the player is too high or low, it automatically corrects while running
            float bias = feetPos.y - raysCenterOfMass.y;
            if (!isGrounded)
                bias = 0;   // cause otherwise it'll force you into the ground when u land, make u bounce a bunch

            for (int i = 0; i < rayi; i++)
            {
                ray r = rays[i];
                Vector3 p = r.hit.point;
                if (p == Vector3.zero) continue;


                Vector3 spos = p - raysCenterOfMass;

                float sx = Vector3.Dot(forwardVector, spos);
                float sy = Vector3.Dot(Vector3.up, spos);

                if (sx < 0) // if the ray hit behind the player, flip it
                {
                    sx *= -1;
                    sy *= -1;
                }

                sy -= bias;


                Vector3 n = r.hit.normal;
                float weighting;
                if (runUpWalls)
                    weighting = Mathf.Pow(Vector3.Dot(-n, playerForce), 2);   // lets you run up walls if sprinting, a bit anyway
                else
                    weighting = Mathf.Pow(Vector3.Dot(-n, Vector3.down), 5);

                //groundSlope += new Vector2(sx, sy);
                groundSlope += new Vector2(sx, sy) * weighting;
            }



        }


        // travel them along the groundslope
        float yAccel = 0;   // yAccel due to ground slope, over 1 frame
        {
            Vector2 gs = groundSlope.normalized;


            Vector2 sgs = Vector2.zero;
            // smooth the groundslope
            {
                groundSlopeList.append(gs);
                float totalSlopes = 0;
                for (int i = 0; i < groundSlopeList.maxSize; i++)
                {
                    Vector2 s = groundSlopeList.list[i];
                    if (s != Vector2.zero)
                    {
                        sgs += s;
                        totalSlopes += 1;
                    }
                }
                sgs = sgs.normalized;
            }

            Vector3 travelVector = (Quaternion.Euler(0, movingAngleXZ, 0) * Vector3.forward) * sgs.x + Vector3.up * sgs.y;
            //float magnitude = Vector3.Dot(body.velocity, travelVector) * Time.deltaTime;
            //float magnitude = body.velocity.magnitude * Time.deltaTime;
            float magnitude = body.linearVelocity.magnitude;
            yAccel = travelVector.y * magnitude;
            float minHeight = yAccel * Time.deltaTime + body.position.y;
            
            
            //if (body.position.y < minHeight)  // doesn't do anything..?
            //{
            //    body.position = new Vector3(body.position.x, minHeight, body.position.z);   // todo use velocity instead of position
            //}



            //print("sgs" + sgs);
            if (debug)
                Debug.DrawLine(transform.position, transform.position + travelVector, new Color(1, 1, 0));
                //Debug.DrawLine(rectTransform.position, rectTransform.position + new Vector3(0, sgs.y, sgs.x), new Color(1, 1, 0));
        }



            // Push the player up from ground if they're too low
            {
            //heightDeadzone
            //hardLimitHeight


            if (isGrounded) {
                Vector3 p = body.position;
                float hardLimitCutoff = playerHeight * (-.5f + hardLimitHeight);
                if (groundHeight > p.y + hardLimitCutoff)
                {
                    body.position = new Vector3(p.x, groundHeight - hardLimitCutoff, p.z);
                }



                // TODO gradual push up when past deadzone
                // Should only really be needed when on a moving platform
                //p = body.position;
                //float deadZoneCutoff = playerHeight * (-.5f + heightDeadzone);
                //if (groundHeight > p.y + deadZoneCutoff)
                //{
                //    //body.position = new Vector3(p.x, groundHeight - deadZoneCutoff, p.z);
                //    pushUpToDeadzone = true;
                //}


                //if (pushUpToDeadzone)
                //{
                //    float deadZoneMiddle = playerHeight * (-.5f + (heightDeadzone / 2));





                //    if (groundHeight < p.y + deadZoneMiddle)
                //        pushUpToDeadzone = false;
                //}
            }
            //}

            // travel along the groundslope; the main means of vertical movement
            if (isGrounded)
            {
                Vector3 v = body.linearVelocity;
                body.useGravity = false;
                if (yAccel > 0)
                {
                    //body.useGravity = false;

                    if (body.linearVelocity.y < yAccel)
                        body.linearVelocity = new Vector3(v.x, yAccel, v.z);
                }
                else
                {
                    // TODO delete downslopeLowering?

                    // make it so you can go Down slopes; fake the gravity

                    // TODO make this work, later
                    //float yAccel1 = yAccel;
                    //if (yAccel1 < Physics.gravity.y)
                    //    yAccel1 = Physics.gravity.y;
                    ////print("faked gravity " + yAccel1);
                    //body.AddForce(Vector3.up * yAccel1, ForceMode.Acceleration);


                    if (groundHeight > body.position.y - playerHeight * (.5f - downslopeLowering))
                    {
                        if (body.linearVelocity.y < yAccel)
                            //body.velocity = new Vector3(v.x, yAccel / Time.deltaTime, v.z);
                            body.linearVelocity = new Vector3(v.x, yAccel, v.z);
                        //print("body velocity " + (body.velocity ) + " " + yAccel);
                    }





                    body.useGravity = true;
                    //print("Going down, yAccel is " + yAccel / Time.deltaTime);
                }

                if (debug)
                    print("TOUCHING, Y is set to " + body.linearVelocity.y);
            } else
            {
                //print("falling, Y: " + body.velocity.y);
                if (debug)
                    print("falling");
                body.useGravity = true;
            }

            if (debug)
            {
                Debug.DrawRay(new Vector3(body.position.x, body.position.y - (playerHeight * .5f), body.position.z), Quaternion.Euler(0, movingAngleXZ, 0) * Vector3.forward, new Color(1, 1, 1));
                if (isGrounded)
                    Debug.DrawRay(new Vector3(body.position.x, groundHeight, body.position.z), Quaternion.Euler(0, movingAngleXZ, 0) * Vector3.forward, new Color(0, 1, 0));
                else
                    Debug.DrawRay(new Vector3(body.position.x, groundHeight, body.position.z), Quaternion.Euler(0, movingAngleXZ, 0) * Vector3.forward, new Color(1, .3f, 0));
            }
        }


        //surfaceNormal = Vector3.Normalize(surfaceNormal);
        //print(groundHeight);
        if (debug)
            Debug.DrawLine(transform.position, transform.position + surfaceNormal, new Color(0, 1, 0));




        // drag
        if (isGrounded)
        {
            float factor = Mathf.Pow(0.5f, Time.deltaTime);
            body.linearVelocity = body.linearVelocity * factor;
        }



        //Physics.SphereCastNonAlloc(rectTransform.position, 0.1f, Vector3.down, hits, rayLength, 1, QueryTriggerInteraction.Ignore);
        //print(hits[0].distance); 
    }




    /// <summary>
    /// just hold some data for each height raycast, do running averages etc
    /// </summary>
    public class ray
    {
        ///// <summary>
        ///// just to keep track
        ///// </summary>
        //public static int totalRays = 0;

        //public runningAverage<float> previousVelocities;    // unused? Delete?
        ////public float currentHeight = 0;
        //public float previousHeight = 0;
        //public float currentVelocity = 0;

        public RaycastHit hit;

        //public ray(int scrollSize)
        //{
        //    previousVelocities = new runningAverage<float>(scrollSize);
        //}



        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="currentHeight"></param>
        ///// <param name="overwriteCurrent">if true, it just overwrites the most recent velocity on the list. If false, it appends a new one and bumps the oldest value- AND it updates previousHeight for later velocities</param>
        //public void setCurrentHeight(float currentHeight, bool overwriteCurrent)
        //{
        //    //float currentVelocity = currentHeight - previousHeight;
        //    currentVelocity = currentHeight - previousHeight;

        //    if (overwriteCurrent)
        //    {
        //        previousVelocities.overwriteNewest(currentVelocity);
        //    } else
        //    {
        //        previousHeight = currentHeight;
        //        previousVelocities.append(currentVelocity);
        //    }
        //}

        //public float getSmoothedAcceleration()
        //{
        //    // TODO exclude the most recent one?
        //    float avgVel = 0;
        //    for(int i = 0; i < previousVelocities.list.Count; i++) 
        //    {
        //        avgVel += previousVelocities.list[i];
        //    }

        //    avgVel /= previousVelocities.list.Count;

        //    return currentVelocity - avgVel;
        //}
    }

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
            list = new List<T>( new T[MaxSize]);

            for(int i = 0; i < MaxSize; i++)
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

        public void debugPrintList()
        {
            for (int i = 0; i < maxSize; i++)
            {
                print(list[i]);
            }
        }
    }
} 
