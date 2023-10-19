using Kart;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor.UIElements;
//using UnityEditor.PackageManager;
using UnityEngine;
using static Record;
using static SmashMulti;
//using static UnityEditor.Progress;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Game specific code for multiplayer and recording/playback of gameplay. This should be all the game's multiplayer/recording code.
/// </summary>
//public class SmashMulti : MonoBehaviour

public class SmashMulti : NetworkBehaviour
{
    public static SmashMulti instance;

    [Header("VR setup (optional)")]
    public Transform XR_Origin;
    public Transform XR_Headset;

    public Transform leftHandObject;
    public Transform rightHandObject;

    List<XR_Dummies_Sync> dummyXR_Rigs;


    // TODO:
    // -Make it split off and save new clips with button presses, for making it easier to save highlights for later editing
    // -Make Record.cs save clips gradually as you go so it doesn't take forever at the end. (And load clips gradually)
    // --and make it async
    // --and an option to spawn a progress bar UI on a given camera/gameobject, for VR
    // -Will need a cycling clip that holds like 1024 frames, for server authoritative games. (This one won't be saved)
    /// <summary>
    /// A recording of the game, and tracks all multiplayer synced variables and methods
    /// </summary>
    public Record.Clip clip;

    ///// <summary>
    ///// 2 way dictionary for converting types to ints and back, for sending over the network with minimal bandwidth
    ///// </summary>
    //public static TypeToInt typeToInt;

    List<SyncedProperty> syncedProperties;  // not sure this list is the best way to hole synced variables/methods. A dictionary maybe? Both?

    //float testVar = 2;
    //SyncedProperty syncedTest;
    public override void OnNetworkSpawn()
    {
        clip = new Record.Clip("SmashGameClip " + UnityEngine.Random.value * 100000);

        dummyXR_Rigs = new List<XR_Dummies_Sync>();
        syncedProperties = new List<SyncedProperty>();
        // test
        //syncedTest = new SyncedProperty(testVar, gameObject, clip, IsOwner);
        //instance.syncedTest = new SyncedProperty(this, testVar, gameObject, instance.clip, IsOwner);


        //typeToInt = new TypeToInt();

    }

    private void Update()
    {
        //if (syncedTest != null)
        //    syncedTest.sync();
        //testVar += 1;

        foreach (SyncedProperty prop in syncedProperties)
        {
            if (prop.IsOwner)
                prop.sync();
        }

        //syncSyncedProperty();
    }



    public static void addXRRig(XR_Dummies_Sync rig)
    {
        instance.dummyXR_Rigs.Add(rig);

        // stuff to sync and record
        //instance.clip.addProperty(rig.XR_Origin, rig.XR_Origin.gameObject);  // TODO could it just infer the gameobject?
        //instance.clip.addProperty(rig.XR_Headset.parent, rig.XR_Headset.parent.gameObject);  // need to track the neck too for mouse looking up-down
        //instance.clip.addProperty(rig.XR_Headset, rig.XR_Headset.gameObject);
        //instance.clip.addProperty(rig.leftHandObject, rig.leftHandObject.gameObject);  
        //instance.clip.addProperty(rig.rightHandObject, rig.rightHandObject.gameObject); 

        bool isOwner = rig.IsOwner;

        // todo replace with SyncedProperty
        addSyncedObject(rig.XR_Origin, isOwner);
        addSyncedObject(rig.XR_Headset.parent, isOwner);
        addSyncedObject(rig.XR_Headset, isOwner);
        addSyncedObject(rig.leftHandObject, isOwner);
        addSyncedObject(rig.rightHandObject, isOwner);

        // test
        print("XR rig synced: " + rig.gameObject.name);


        //test
        //instance.syncedTest = new SyncedProperty(rig, rig.testfloat, rig.gameObject, instance.clip, rig.IsOwner);
        //instance.syncedProperties.Add(new SyncedProperty(rig, rig.testfloat, rig.gameObject, instance.clip, rig.IsOwner));
        instance.syncedProperties.Add(new SyncedProperty(rig, rig.testFloat2, rig.gameObject, instance.clip, rig.IsOwner));
    }


    // TODO move to Multi.cs

    public static void addSyncedObject(Transform obj, bool IsOwner)
    {
        instance.clip.addProperty(obj, obj.gameObject);


    }



    void Awake()
    {
        //if ( instance == null)
        //    Startup();

        if (instance != null)
            Debug.LogError("More than one SmashMulti in scene");
        instance = this;
    }

    NetData testVal = new NetData(42);


    /// <summary>
    /// Because you can't do network RPCs in classes that don't fucking inherit from NetworkBehaviour, and those are hard/expensive to spawn
    /// </summary>
    /// <param name="prop"></param>
    public static void syncSyncedProperty(SyncedProperty prop)
    //public static void syncSyncedProperty()
    {
        //if (prop.IsOwner)
        //if (instance.IsOwner)
        //{
            //pingServerRpc(Time.time);   // send to server

            //object data = prop.getCurrentValue();
            //instance.pingServerRpc((FieldInfo)data);

            instance.pingServerRpc(prop);


            
            //instance.pingServerRpc(instance.testVal);
        //}
        //else
        //{
        //    print("not owner");
        //}
    }
    //[ServerRpc]
    [ServerRpc(RequireOwnership = false)]
    //public void pingServerRpc(NetData data, ServerRpcParams pars = default)
    public void pingServerRpc(SyncedProperty data, ServerRpcParams pars = default)
    {
        //data.setDataType(42.GetType());
        var clientId = pars.Receive.SenderClientId;
        //print(clientId + " pinged the server with " + data.ToString());
        //print(clientId + " pinged the server with " + data.ToString() + " data " + data.GetData().ToString());
        print(clientId + " pinged the server with " + data.getCurrentValue().ToString());

        pingClientRpc(data, clientId);  // send to all clients
    }


    [ClientRpc]
    //public void pingClientRpc(NetData data, ulong originalSender, ClientRpcParams pars = default)
    public void pingClientRpc(SyncedProperty data, ulong originalSender, ClientRpcParams pars = default)
    {
        var thisClientId = NetworkManager.Singleton.LocalClientId;
        //print("Server pinged client " + thisClientId + " (originally from client " + originalSender + ") with " + data.GetData().ToString());
        print("Server pinged client " + thisClientId + " (originally from client " + originalSender + ") with " + data.getCurrentValue().ToString());
    }






    // todo move to Multi.cs

    /// <summary>
    /// Synced and animated property
    /// (Doesn't seem to work when you make one for a variable in the current script, only other scripts? Idk. TODO, test more)
    /// </summary>
    public class SyncedProperty : Record.AnimatedProperty, INetworkSerializable
    {
        //NetworkVariable<T> netVar;

        //RPC // todo

        public bool IsOwner;
        /// <summary>
        /// Datatype used in networking. See TypeToInt class for types
        /// </summary>
        private int _dataType = 0;


        //public SyncedProperty(object _obj, GameObject _gameObject, Record.Clip _clip, bool isOwner) : base(_obj, _gameObject, _clip)
        public SyncedProperty(object _animatedObject, object propertyOrField, GameObject _gameObject, Record.Clip _clip, bool isOwner) : base(_animatedObject, propertyOrField, _gameObject, _clip)
        {
            print("syncedProperty: " + propertyOrField);
            IsOwner = isOwner;
            // TODO
        }

        /// <summary>
        /// DON'T USE! Empty constructor required for INetworkSerializable. 
        /// </summary>
        public SyncedProperty()
        {
        }

        public void sync()
        {
            //[ServerRpc]


            //if (IsOwner)
            //{
            //    //pingServerRpc(Time.time);   // send to server

            //    object data = getCurrentValue();
            //    pingServerRpc(data);
            //}



            SmashMulti.syncSyncedProperty(this);
        }

        /// <summary>
        /// Converting the variable to an object creates garbage collection FYI, good to do it more directly if possible
        /// </summary>
        /// <returns></returns>
        public object getCurrentValue()
        {
            // from Record.cs's "GenericFrame"
            object data = null;

            //if (property.obj is FieldInfo)
            if (obj is FieldInfo)
            {
                //data = ((FieldInfo)property.obj).GetValue(property.animatedComponent);
                data = ((FieldInfo)obj).GetValue(animatedComponent);
            }
            else if (obj is PropertyInfo)
            {
                data = ((PropertyInfo)obj).GetValue(animatedComponent);
            }
            // TODO transforms?

            return data;
        }

        public void setCurrentValue(object data)
        {
            // from Record.cs's "GenericFrame"
            //if (property.obj is FieldInfo)
            if (obj is FieldInfo)
            {
                ((FieldInfo)obj).SetValue(animatedComponent, data);
            }
            else if (obj is PropertyInfo)
            {
                ((PropertyInfo)obj).SetValue(animatedComponent, data);
            }
            // TODO transforms?
        }



        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            object _data = null;

            if (serializer.IsWriter)
            {
                _data = getCurrentValue();
                if (_dataType == 0)
                    _dataType = TypeToInt.Int(_data.GetType());
            }

            //if (serializer.IsWriter)
            //{
            serializer.SerializeValue(ref _dataType);   // these calls handle both sending/serializing data and receiving/deserializing data
            switch (_dataType)
            {
                case int when _dataType == TypeToInt.Int(typeof(int)):
                    int intData = 0;
                    if (serializer.IsWriter)
                        intData = (int)_data;
                    serializer.SerializeValue(ref intData);
                    _data = intData;    // if receiving data, update the local copy
                    break;

                case int when _dataType == TypeToInt.Int(typeof(float)):
                    float floatData = 0;
                    if (serializer.IsWriter)
                        floatData = (float)_data;
                    serializer.SerializeValue(ref floatData);
                    _data = floatData;
                    break;

                case int when _dataType == TypeToInt.Int(typeof(string)):
                    string stringData = "";
                    if (serializer.IsWriter)
                        stringData = (string)_data;
                    serializer.SerializeValue(ref stringData);
                    _data = stringData;
                    break;

                case int when _dataType == TypeToInt.Int(typeof(Vector3)):
                    Vector3 vector3Data = Vector3.zero;
                    if (serializer.IsWriter)
                        vector3Data = (Vector3)_data;
                    serializer.SerializeValue(ref vector3Data);
                    _data = vector3Data;
                    break;

                case int when _dataType == TypeToInt.Int(typeof(Transform)):
                    Transform transformData = SmashMulti.instance.transform;    // just need a random non-null value
                    if (serializer.IsWriter)
                        transformData = (Transform)_data;
                    Vector3 position = transformData.position;
                    Quaternion rotation = transformData.rotation;
                    Vector3 scale = transformData.localScale;
                    serializer.SerializeValue(ref position);
                    serializer.SerializeValue(ref rotation);
                    serializer.SerializeValue(ref scale);
                    // receiving data
                    transformData.position = position;
                    transformData.rotation = rotation;
                    transformData.localScale = scale;
                    _data = transformData;
                    break;

                // ... add other types similarly

                default:
                    // Handle the case where the type is not recognized.
                    // This could be an error or a default serialization logic.
                    Debug.LogError("Type not recognized: Type: " + _dataType + " data: " + _data);
                    break;
            }

            if (serializer.IsReader)
                setCurrentValue(_data);
        }


    }




    /// <summary>
    /// For passing variable types over the network with minimal bandwidth. Speedy, uses IFs/switches
    /// </summary>
    public static class TypeToInt
    {
        // speedy lookups
        // TODO why did GPT 4 do it 2 different ways?
        public static int Int(Type type)
        {
            if (type == typeof(int)) return 1;
            if (type == typeof(float)) return 2;
            if (type == typeof(double)) return 3;
            if (type == typeof(string)) return 4;
            if (type == typeof(Vector3)) return 5;
            if (type == typeof(Transform)) return 6;
            // ... Add other types as needed

            throw new ArgumentException("Unsupported type");
        }

        /// <summary>
        /// Write like this: dict.GetTypeCode(typeof(float))  or like this dict.GetTypeCode(typeof(float))
        /// </summary>
        public static Type Type(int code)
        {
            switch (code)
            {
                case 1: return typeof(int);
                case 2: return typeof(float);
                case 3: return typeof(double);
                case 4: return typeof(string);
                case 5: return typeof(Vector3);
                case 6: return typeof(Transform);
                // ... Add other types as needed

                default: throw new ArgumentException("Unsupported code");
            }
        }

    }

    // temp
    public class NetData : INetworkSerializable
    {
        // GPT 4 wrote a lot of this (though it made mistakes)
        private object _data = null;
        //private Type _dataType;

        private int _dataType = 0;


        public NetData()
        {
            // Empty constructor required for INetworkSerializable
        }

        public NetData(object data)
        {
            _data = data;
            _dataType = TypeToInt.Int(data.GetType());
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            //if (serializer.IsWriter)
            //{
            serializer.SerializeValue(ref _dataType);   // these calls handle both sending/serializing data and receiving/deserializing data
            switch (_dataType)
            {
                case int when _dataType == TypeToInt.Int(typeof(int)):
                    int intData = 0;
                    if (serializer.IsWriter)
                        intData = (int)_data;
                    serializer.SerializeValue(ref intData);
                    _data = intData;    // if receiving data, update the local copy
                    break;

                case int when _dataType == TypeToInt.Int(typeof(float)):
                    float floatData = 0;
                    if (serializer.IsWriter)
                        floatData = (float)_data;
                    serializer.SerializeValue(ref floatData);
                    _data = floatData;
                    break;

                case int when _dataType == TypeToInt.Int(typeof(string)):
                    string stringData = "";
                    if (serializer.IsWriter)
                        stringData = (string)_data;
                    serializer.SerializeValue(ref stringData);
                    _data = stringData;
                    break;

                case int when _dataType == TypeToInt.Int(typeof(Vector3)):
                    Vector3 vector3Data = Vector3.zero;
                    if (serializer.IsWriter)
                        vector3Data = (Vector3)_data;
                    serializer.SerializeValue(ref vector3Data);
                    _data = vector3Data;
                    break;

                case int when _dataType == TypeToInt.Int(typeof(Transform)):
                    Transform transformData = SmashMulti.instance.transform;    // just need a random non-null value
                    if (serializer.IsWriter)
                        transformData = (Transform)_data;
                    Vector3 position = transformData.position;
                    Quaternion rotation = transformData.rotation;
                    Vector3 scale = transformData.localScale;
                    serializer.SerializeValue(ref position);
                    serializer.SerializeValue(ref rotation);
                    serializer.SerializeValue(ref scale);
                    // receiving data
                    transformData.position = position;
                    transformData.rotation = rotation;
                    transformData.localScale = scale;
                    break;

                // ... add other types similarly

                default:
                    // Handle the case where the type is not recognized.
                    // This could be an error or a default serialization logic.
                    Debug.LogError("Type not recognized: Type: " + _dataType + " data: " + _data);
                    break;
            }
        }


        public object GetData()
        { return _data; }

        public void setData(object data)
        { _data = data; }

        //public void setDataType(Type dataType)
        //{ _dataType = dataType; }
    }


}








//public class NetData : INetworkSerializable
//{
//    // GPT 4 wrote a lot of this (though it made mistakes)
//    private object _data;
//    private Type _dataType;

//    public NetData()
//    {
//        // Empty constructor required for INetworkSerializable
//    }

//    public NetData(object data)
//    {
//        _data = data;
//        _dataType = data.GetType(); // Store the data type for later use.
//    }

//    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
//    {
//        //if (serializer.IsWriter)
//        //{
//        serializer.SerializeValue(ref _dataType);
//        switch (_dataType)
//        {
//            //case Type intType when intType == typeof(int):
//            case Type when _dataType.Equals(typeof(int)):
//                int intData = (int)_data;
//                serializer.SerializeValue(ref intData);
//                //serializer.SerializeValue(ref (Int32)_data);
//                //serializer.SerializeValue(ref (_data as Int32));
//                _data = intData;    // if receiving data, update the local copy
//                break;

//            case Type when _dataType.Equals(typeof(float)):
//                float floatData = (float)_data;
//                serializer.SerializeValue(ref floatData);
//                _data = floatData;
//                break;

//            case Type when _dataType.Equals(typeof(string)):
//                string stringData = (string)_data;
//                serializer.SerializeValue(ref stringData);
//                _data = stringData;
//                break;

//            case Type when _dataType.Equals(typeof(Vector3)):
//                Vector3 vector3Data = (Vector3)_data;
//                serializer.SerializeValue(ref vector3Data);
//                _data = vector3Data;
//                break;

//            case Type when _dataType.Equals(typeof(Transform)):
//                Transform transformData = (Transform)_data;
//                Vector3 position = transformData.position;
//                Quaternion rotation = transformData.rotation;
//                Vector3 scale = transformData.localScale;
//                serializer.SerializeValue(ref position);
//                serializer.SerializeValue(ref rotation);
//                serializer.SerializeValue(ref scale);
//                // TODO receiving data
//                break;

//            // ... add other types similarly

//            default:
//                // Handle the case where the type is not recognized.
//                // This could be an error or a default serialization logic.
//                Debug.LogError("Type not recognized: Type: " + _dataType + " data: " + _data);
//                break;
//        }
//    }
//        //}
//        //else
//        //{
//        //    // receive data
//        //    switch (_dataType)
//        //    {
//        //        case Type when _dataType.Equals(typeof(int)):
//        //            //_data = serializer.ReadValue<int>();
//        //            _data = serializer.;
//        //            break;

//        //        case Type when _dataType.Equals(typeof(float)):
//        //            _data = serializer.ReadValue<float>();
//        //            break;

//        //            // ... Add other types as needed
//        //    }


//        //}

//    public object GetData()
//    { return _data; }

//    public void setData(object data)
//    { _data = data; }

//    public void setDataType(Type dataType)
//    { _dataType = dataType; }
//}




//public static class FastBufferWriterExtensions
//{
//    public static void WriteValueSafe(this FastBufferWriter writer, in System.Reflection.FieldInfo value)
//    {
//        // Serialize the FieldInfo into a format you can send over the network.
//        // For example, you might just send the field's name and the declaring type's name.
//        string serializedData = value.DeclaringType.FullName + "." + value.Name;
//        writer.WriteValueSafe(serializedData);
//        //writer.WriteValueSafe<FieldInfo>(value);
//    }
//}

//public static class FastBufferReaderExtensions
//{
//    public static void ReadValueSafe(this FastBufferReader reader, out System.Reflection.FieldInfo value)
//    {
//        // Deserialize the FieldInfo from the format you've chosen.
//        string serializedData;
//        reader.ReadValueSafe(out serializedData);

//        string[] parts = serializedData.Split('.');
//        if (parts.Length == 2)
//        {
//            Type type = Type.GetType(parts[0]);
//            value = type.GetField(parts[1]);
//        }
//        else
//        {
//            value = null;
//        }
//    }
//}
