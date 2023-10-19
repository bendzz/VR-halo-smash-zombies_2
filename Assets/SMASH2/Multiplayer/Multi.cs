using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;



/// <summary>
/// Generic code for multiplayer and recording/playback of gameplay. This is the only script that should be networked.
/// Use for all networked variables and functions.
/// </summary>
public class Multi : NetworkBehaviour
{
    public static Multi instance;



    // TODO:
    // -Make it split off and save new clips with button presses, for making it easier to save highlights for later editing
    // -Make Record.cs save clips gradually as you go so it doesn't take forever at the end. (And load clips gradually)
    // --and make it async
    // --and an option to spawn a progress bar UI on a given camera/gameobject, for VR
    // -Will need a cycling clip that holds like 1024 frames, for server authoritative games. (This one won't be saved)
    /// <summary>
    /// A recording of the game, and tracks all multiplayer synced properties (ie script variables and methods)
    /// </summary>
    public Record.Clip clip;


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

    private void LateUpdate()
    {
        
        // Add sync calls scripts can use. (syncProperty(ref variable or method) etc)
        foreach (SyncedProperty prop in SyncedProperty.SyncedProperties.Values)
        {
            if (prop.IsOwner)
                prop.sync();
        }
    }


    /// <summary>
    /// Because you can't do network RPCs in classes that don't fucking inherit from NetworkBehaviour, and those are hard/expensive to spawn
    /// </summary>
    /// <param name="prop"></param>
    public static void syncSyncedProperty(NetData prop)
    {
        instance.pingServerRpc(prop);
    }
    //[ServerRpc]
    [ServerRpc(RequireOwnership = false)]   // Note: This singleton class always runs as !IsOwner on non server clients, for some reason.
    public void pingServerRpc(NetData data, ServerRpcParams pars = default)
    //public void pingServerRpc(SyncedProperty data, ServerRpcParams pars = default)
    {
        //data.setDataType(42.GetType());
        var clientId = pars.Receive.SenderClientId;
        //print(clientId + " pinged the server with " + data.ToString());
        //print(clientId + " pinged the server with " + data.ToString() + " data " + data.GetData().ToString());
        print(clientId + " pinged the server with " + data.GetData().ToString());
        //print(clientId + " pinged the server with " + data.getCurrentValue().ToString());

        pingClientRpc(data, clientId);  // send to all clients
    }


    [ClientRpc]
    public void pingClientRpc(NetData data, ulong originalSender, ClientRpcParams pars = default)
    //public void pingClientRpc(SyncedProperty data, ulong originalSender, ClientRpcParams pars = default)
    {
        var thisClientId = NetworkManager.Singleton.LocalClientId;
        print("Server pinged client " + thisClientId + " (originally from client " + originalSender + ") with " + data.GetData().ToString());
        //print("Server pinged client " + thisClientId + " (originally from client " + originalSender + ") with " + data.getCurrentValue().ToString());
    }






    // todo move to Multi.cs

    /// <summary>
    /// Multiplayer synced and recorded/played-back property, for animating a variable or method in another script
    /// (Doesn't seem to work when you make one for a variable in the current script, only other scripts? Can't read its data? Idk. TODO, test more)
    /// </summary>
    public class SyncedProperty : Record.AnimatedProperty
    {
        /// <summary>
        /// All synced properties (ints are unique identifiers)
        /// </summary>
        public static Dictionary<int, SyncedProperty> SyncedProperties;
        /// <summary>
        /// A code unique to this property, the same across all its instances on all clients, for network syncing
        /// </summary>
        public int identifier;

        //NetworkVariable<T> netVar;

        //RPC // todo

        public bool IsOwner;
        /// <summary>
        /// Datatype used in networking. See TypeToInt class for types
        /// </summary>
        private int _dataType = 0;




        /// <summary>
        /// Have to send data within NedData's, because the receiving RPC rebuilds the object from scratch. 
        /// So sending a SyncedProperty directly doesn't work, since the new one has no references. (Also, super wasteful)
        /// </summary>
        public NetData netData;

        //public SyncedProperty(object _obj, GameObject _gameObject, Record.Clip _clip, bool isOwner) : base(_obj, _gameObject, _clip)
        public SyncedProperty(object _animatedObject, object propertyOrField, GameObject _gameObject, Record.Clip _clip, bool isOwner) : base(_animatedObject, propertyOrField, _gameObject, _clip)
        {
            print("syncedProperty: " + propertyOrField);
            IsOwner = isOwner;



            if (IsOwner)
            {
                if (SyncedProperties == null)
                    SyncedProperties = new Dictionary<int, SyncedProperty>();
                identifier = getUniqueIdentifier();

                // TODO
                // RPC sync
                // trigger it to spawn new instances across other clients if not done so?
                // handle players having been spawned?


                SyncedProperties.Add(identifier, this);
            }



            // TODO
        }

        int getUniqueIdentifier()
        {
            int id;
            do
            {
                id = (int)(UnityEngine.Random.value * (2f * (int.MaxValue - 1)) - (int.MaxValue - 2));  // idk if the safety minuses are neccessary
            } while (SyncedProperties.ContainsKey(id));
            return id;
        }

        ///// <summary>
        ///// DON'T USE! Empty constructor required for INetworkSerializable. 
        ///// </summary>
        //public SyncedProperty()
        //{
        //}

        public void sync()
        {
            // TODO check for dirtiness before syncing

            //if (IsOwner)

            if (netData == null)
                netData = new NetData(getCurrentValue());
            else
                netData.setData(getCurrentValue());

            //SmashMulti.syncSyncedProperty(netData);
            Multi.syncSyncedProperty(netData);

        }

        /// <summary>
        /// Get the value from the script variable reference in the scene
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

        /// <summary>
        /// Set the value to the script variable referenced in the scene
        /// </summary>
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



        //public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        //{
        //    object _data = null;

        //    if (serializer.IsWriter)
        //    {
        //        _data = getCurrentValue();
        //        if (_dataType == 0)
        //            _dataType = TypeToInt.Int(_data.GetType());
        //    }

        //    //if (serializer.IsWriter)
        //    //{
        //    serializer.SerializeValue(ref _dataType);   // these calls handle both sending/serializing data and receiving/deserializing data
        //    switch (_dataType)
        //    {
        //        case int when _dataType == TypeToInt.Int(typeof(int)):
        //            int intData = 0;
        //            if (serializer.IsWriter)
        //                intData = (int)_data;
        //            serializer.SerializeValue(ref intData);
        //            _data = intData;    // if receiving data, update the local copy
        //            break;

        //        case int when _dataType == TypeToInt.Int(typeof(float)):
        //            float floatData = 0;
        //            if (serializer.IsWriter)
        //                floatData = (float)_data;
        //            serializer.SerializeValue(ref floatData);
        //            _data = floatData;
        //            break;

        //        case int when _dataType == TypeToInt.Int(typeof(string)):
        //            string stringData = "";
        //            if (serializer.IsWriter)
        //                stringData = (string)_data;
        //            serializer.SerializeValue(ref stringData);
        //            _data = stringData;
        //            break;

        //        case int when _dataType == TypeToInt.Int(typeof(Vector3)):
        //            Vector3 vector3Data = Vector3.zero;
        //            if (serializer.IsWriter)
        //                vector3Data = (Vector3)_data;
        //            serializer.SerializeValue(ref vector3Data);
        //            _data = vector3Data;
        //            break;

        //        case int when _dataType == TypeToInt.Int(typeof(Transform)):
        //            Transform transformData = SmashMulti.instance.transform;    // just need a random non-null value
        //            if (serializer.IsWriter)
        //                transformData = (Transform)_data;
        //            Vector3 position = transformData.position;
        //            Quaternion rotation = transformData.rotation;
        //            Vector3 scale = transformData.localScale;
        //            serializer.SerializeValue(ref position);
        //            serializer.SerializeValue(ref rotation);
        //            serializer.SerializeValue(ref scale);
        //            // receiving data
        //            transformData.position = position;
        //            transformData.rotation = rotation;
        //            transformData.localScale = scale;
        //            _data = transformData;
        //            break;

        //        // ... add other types similarly

        //        default:
        //            // Handle the case where the type is not recognized.
        //            // This could be an error or a default serialization logic.
        //            Debug.LogError("Type not recognized: Type: " + _dataType + " data: " + _data);
        //            break;
        //    }

        //    if (serializer.IsReader)
        //        setCurrentValue(_data);
        //}


    }




    /// <summary>
    /// For passing variable Types over the network with minimal bandwidth. Speedy, uses IFs/switches
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

    /// <summary>
    /// A class for sending generic data through unity RPC calls. (Unity's RPCs don't support generics)
    /// </summary>
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








// TODO, use?
/// <summary>
/// Wrapper for NetworkBehaviour. Gives an isSimulating flag compatible with multiplayer and recording/playback.
/// Be sure to register your gameobjects with your game's multiplayer singleton!
public abstract class NetBehaviour : NetworkBehaviour
{
    /// <summary>
    /// Whether to run game logic, or just let the multiplayer or recording-playback system control this intance
    /// </summary>
    public bool isSimulating {  get; }


}