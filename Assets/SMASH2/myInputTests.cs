using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.VisualScripting;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using static myInputTests;

//public class myInputTests : MonoBehaviour
public class myInputTests : NetworkBehaviour
{
    //public InputDevice rightHand;

    public Transform XR_Origin;
    public Transform XR_Headset;

    public Transform leftHandObject;
    public Transform rightHandObject;

    public Rigidbody body;


    // Other scripts
    public FeetCollider feetCollider;
    public SmashCharacter smashCharacter;


    public float runAccelerate = 10;
    public float runMax = 19.6f;    // usain bolt

    [Tooltip("degrees per second turn with joystick")]
    public float turnSpeed = 360;

    [Tooltip("Because it rotates the VR player's body vertically and that'd be nauseating af if it happened by accident. Also, if false, cursor is fixed to screen center")]
    bool disableMouseVertical = false;

    [Tooltip("extra rotation added to the headset rotation; like using the right joystick to spin your character")]
    public Quaternion extraRotation = Quaternion.identity;
    [Tooltip("Used for looking vertically with the mouse")]
    public Quaternion cameraTilt = Quaternion.identity;


    // work values
    Vector3 oldHeadset;
    quaternion oldextraRotation;


    public Hand leftie;
    public Hand rightie;
    List<Hand> hands = new List<Hand>();






    // Start is called before the newest frame update
    void Start()
    //public override void OnNetworkSpawn()
    {

        //oldHeadset = XR_Headset.position;
        //oldextraRotation = extraRotation;


        //leftie = new Hand(leftHandObject, true);
        //rightie = new Hand(rightHandObject, false);
        //hands.Add(leftie);
        //hands.Add(rightie);
    }

    public override void OnNetworkSpawn()
    {
        // multiplayer setup
        //if (IsOwner)
        //{
        //XR_Origin = MyMultiplayer.instance.XR_Origin_Synced;
        //XR_Headset = MyMultiplayer.instance.XR_Headset_Synced;
        //leftHandObject = MyMultiplayer.instance.leftHandObject_Synced;
        //rightHandObject = MyMultiplayer.instance.rightHandObject_Synced;

        //print("used XR dummies");

            //XR_Headset = MyMultiplayer.instance.XR_Headset;

            //leftHandObject = MyMultiplayer.instance.leftHandObject;
            //rightHandObject = MyMultiplayer.instance.rightHandObject;
        //} else
        //{
        //    // spawn network synced dummy objects representing the other player's hands  


        //}



        // VR setup

        oldHeadset = XR_Headset.position;
        oldextraRotation = extraRotation;


        leftie = new Hand(leftHandObject, true);
        rightie = new Hand(rightHandObject, false);
        hands.Add(leftie);
        hands.Add(rightie);


        //if (!IsOwner) // disable VR camera/hands attachments..?
        print("spawned myInputTests for " + this.name);
    }





    private void FixedUpdate()
    {


        leftie.getDevice(XRNode.LeftHand);
        rightie.getDevice(XRNode.RightHand);


        leftie.update();
        rightie.update();

          
        // get the player rotated right before applying movement acceleration
        syncPlayerAndHeadset();


        // Apply player movement
        //VR_JoystickRunning();
        PC_mouseRunning();
        VR_Smash();

        // character model's scripts update
        //if (smashCharacter != null)
        //    SmashCharacter_Update(smashCharacter);


        syncPlayerAndHeadset();
    }

    public class Hand
    {
        public List<InputDevice> devices = new List<InputDevice>();   // todo throw warning if multiples
        public InputDevice device;
        /// <summary>
        /// scene object that moves with the VR controller
        /// </summary>
        public Transform transform;

        /// <summary>
        /// IsLeft or right hand?
        /// </summary>
        public bool isLeft = false;


        //public Vector3 pos = Vector3.zero;
        public Vector2 joystick = new Vector2();
        public bool joystickClick = false;
        public float grip = 0;
        public float trigger = 0;
        public bool A = false;  // todo touches
        public bool B = false;

        public bool isActive = false;   // TODO

        int deviceCount = 0;

        public Hand(Transform sceneObject, bool IsLeft)
        { transform = sceneObject;
            isLeft = IsLeft;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node">like XRNode.LeftHand</param>
        public void getDevice(XRNode node)
        {
            //devices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(node, devices);
            if (deviceCount != devices.Count)
            {
                if (devices.Count > 0 && devices[0].isValid)
                    device = devices[0];
                else
                    device = new InputDevice();
                if (devices.Count > 1)
                {
                    Debug.LogWarning("multiple devices at " + node);
                    foreach (InputDevice d in devices)
                    {
                        Debug.LogWarning("device " + d.name + " valid: " + d.isValid + " subsystem: " + d.subsystem + " manufacturer: " + d.manufacturer + " serialNumber: " + d.serialNumber);
                    }
                }

                Debug.Log(devices.Count + " devices found at XRNode " + node + ". device initialized: " + device.name);
                deviceCount = devices.Count;
            }
        }

        public void update()
        {
            //if (device == null || !device.isValid)
            if (!device.isValid)
                //Debug.LogWarning("device " + device + " name " + this + " is null or invalid"); return;
                return;

            // buttons
            if (device.TryGetFeatureValue(CommonUsages.primary2DAxis, out joystick)) { }
            if (device.TryGetFeatureValue(CommonUsages.grip, out grip)) { }
            if (device.TryGetFeatureValue(CommonUsages.trigger, out trigger)) { }
            if (device.TryGetFeatureValue(CommonUsages.primaryButton, out A)) { }
            if (device.TryGetFeatureValue(CommonUsages.secondaryButton, out B)) { }
            if (device.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out joystickClick)) { }
            //print($"leftGrip: {leftGrip}");
        }

        public class InputBuffer<T>
        {
            List<T> list;
            // scrollList?
        }



        // game specific stuff; WIP

        public Vector3 grabPoint = Vector3.zero;   // grab n throw yourself
        //public Vector3 velocity = Vector3.zero;
        public Vector3 initialBodyVelocity = Vector3.zero;   // body speed when you grabbed the air // unused atm, was gonna let you redirect velocity. TODO


        //// outputs for visuals
        public SmashCharacter.Hand s;
        //public float thrusterOutput = 0;
        //public Vector3 thrusterDirection = Vector3.zero;
    }





    //// keep the character model updating separate from input etc
    //void SmashCharacter_Update(SmashCharacter smashCharacter)
    //{
    //    smashCharacter.leftie.thruster = leftie.thrusterOutput / 50;
    //    smashCharacter.leftie.thrusterDirection = -leftie.thrusterDirection;
    //    smashCharacter.rightie.thruster = rightie.thrusterOutput / 50;
    //    smashCharacter.rightie.thrusterDirection = -rightie.thrusterDirection;
    //    //smashCharacter.rightie.thruster = .7f;
    //}



    float previousSmashRotation = 0;
    // throw yourself around
    void VR_Smash()
    {
        // rotation
        if (leftie.trigger > .5f && rightie.trigger > .5f)
        {
            Vector3 l = leftie.transform.localPosition;
            Vector3 r = rightie.transform.localPosition;

            if (previousSmashRotation == 0)
                previousSmashRotation = Vector2.SignedAngle(Vector2.up, (new Vector2(l.x, l.z) - new Vector2(r.x, r.z)));

            float currentRotation = Vector2.SignedAngle(Vector2.up, (new Vector2(l.x, l.z) - new Vector2(r.x, r.z)));


            float rotation = currentRotation - previousSmashRotation;
            extraRotation *= Quaternion.Euler(0, rotation, 0);

            //print("rotation " + rotation);
            previousSmashRotation = Vector2.SignedAngle(Vector2.up, (new Vector2(l.x, l.z) - new Vector2(r.x, r.z)));
        }
        else
            previousSmashRotation = 0;


        // movement
        // both hands? (combining the hands minimizes the drift when you try and rotate)
        Vector3 combinedDelta = Vector3.zero;
        if (leftie.trigger > .5f && rightie.trigger > .5f && (leftie.grabPoint != Vector3.zero && rightie.grabPoint != Vector3.zero))
        {
            combinedDelta = (leftie.grabPoint - leftie.transform.localPosition) + (rightie.grabPoint - rightie.transform.localPosition);
            combinedDelta /= 2;
        }
        // calc each Hand
        foreach (Hand hand in hands)
        {
            //hand.thrusterOutput = 0;
            if (hand.trigger > .5f) 
            {
                if (hand.grabPoint == Vector3.zero)
                {
                    hand.grabPoint = hand.transform.localPosition;
                    hand.initialBodyVelocity = body.velocity;
                }


                Vector3 delta = hand.grabPoint - hand.transform.localPosition;
                if (combinedDelta != Vector3.zero)
                    delta = combinedDelta;
                Vector3 worldDirection = hand.transform.parent.localToWorldMatrix * delta.normalized;

                Vector3 deltaWorld = delta.magnitude * worldDirection * 600;    // map the local space Hand movement to world space
                float speedDilution = Mathf.Clamp01(Vector3.Dot((deltaWorld - body.velocity * 10), deltaWorld));    // so you can only accelerate so fast in 1 direction, but can stop instantly
                

                Vector3 push = deltaWorld * speedDilution * 1;

                // apply results
                //hand.thrusterOutput = push.magnitude;
                //hand.thrusterDirection = push.normalized;
                body.AddForce(push, ForceMode.Acceleration);

                // update visuals
                {
                    if (hand.s == null)
                    {
                        if (hand.isLeft)
                            hand.s = smashCharacter.leftie;
                        else
                            hand.s = smashCharacter.rightie;
                    }

                    //sh.thruster = push.magnitude / 50;
                    hand.s.thruster = delta.magnitude * 10;
                    //sh.thrusterDirection = -push.normalized;
                    hand.s.thrusterDirection = -worldDirection;

                }


                //print("Accelerating " + force);
                //print("speedDilution " + speedDilution);
                //print("push " + push);
                hand.grabPoint = hand.transform.localPosition;
            } else
            {
                hand.grabPoint = Vector3.zero;
                hand.initialBodyVelocity = Vector3.zero;

                // visuals
                if (hand.s != null)
                {
                    hand.s.thruster = 0;
                }
            }
        }

    }

    void PC_mouseRunning()
    {
        extraRotation = extraRotation * Quaternion.Euler(0, Input.GetAxis("Mouse X") * turnSpeed * Time.deltaTime, 0);
        cameraTilt = cameraTilt * Quaternion.Euler(-Input.GetAxis("Mouse Y") * turnSpeed * Time.deltaTime,0,0);
        
        if (!disableMouseVertical)
        {
            XR_Headset.parent.localRotation = Quaternion.identity * cameraTilt;
            //Cursor.lockState = CursorLockMode.Locked;
        } else
        {
            //Cursor.lockState = CursorLockMode.None;
        }

        //Vector3 vel = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")) * runAccelerate;   // lets you use VR joystick, bad >_>
        // letter T pressed on keyboard
        Vector3 vel = new Vector3(Input.GetKey(KeyCode.A) ? -1 : Input.GetKey(KeyCode.D) ? 1 : 0, 0, Input.GetKey(KeyCode.S) ? -1 : Input.GetKey(KeyCode.W) ? 1 : 0) * runAccelerate;





        vel = Quaternion.Euler(0,body.rotation.eulerAngles.y, 0) * vel;
        body.AddForce(vel, ForceMode.Acceleration);
    }

    // Uses the input values to make you run using the 2 joysticks and headset direction
    void VR_JoystickRunning()
    {
        extraRotation = extraRotation * Quaternion.Euler(0, rightie.joystick.x * turnSpeed * Time.deltaTime, 0);


        Vector3 vel = new Vector3(leftie.joystick.x, 0, leftie.joystick.y) * runAccelerate;
        vel = body.rotation * vel;
        body.AddForce(vel, ForceMode.Acceleration);
    }


    /// <summary>
    /// First it moves the player from headset movement, then it lines the headset up with the player. Then it updates oldHeadset, oldextraRotation
    /// </summary>
    void syncPlayerAndHeadset()
    {

        // sync player to headset
        {
            body.rotation = Quaternion.Euler(0, XR_Headset.localRotation.eulerAngles.y, 0) * extraRotation;
            Vector3 offset = (XR_Headset.localPosition - oldHeadset);
            offset = extraRotation * offset;
            if (offset.magnitude < .3f) body.position += offset;
        }



        // sync headset to player (by moving the playspace)
        {
            // rotation
            //Vector3 headPos = XR_Headset.position;
            XR_Origin.rotation *= extraRotation * Quaternion.Inverse(oldextraRotation); // TODO only sync Y rotation?
            //XR_Origin.position += headPos - XR_Headset.position;

            // position
            Vector3 headOffset = body.position - XR_Headset.position;    // todo include a head delta from rigidbody
            XR_Origin.position += headOffset;
        }


        oldHeadset = XR_Headset.localPosition;
        oldextraRotation = extraRotation;
    }


    // Update is called once per frame
    void Update()
    {
        syncPlayerAndHeadset(); // cause otherwise the headset falls behind the player model
    }




    // Originally was runningAverage in FeetCollider.cs
    /// <summary>
    /// 
    /// Overwrites the oldest value when appended; used for calculating a running average.
    /// Note: "oldest" will return blank values until the list has been filled once
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class runningAverage<T>
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

        public runningAverage()
        { list = new List<T>(); }

        public runningAverage(int MaxSize)
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

        public void debugPrintList()
        {
            for (int i = 0; i < maxSize; i++)
            {
                print(list[i]);
            }
        }
    }
}
