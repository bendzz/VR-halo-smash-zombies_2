using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEditor.EditorTools;
using UnityEngine;

//public class Rec : MonoBehaviour

/// <summary>
/// Adds recording/playback functionality to the Multi.cs multiplayer system, reusing its serialization system.
/// 2023 remake of Record.cs gameplay recording/playback system (from the OutdoorPacmanVR game).
/// Inspired by unity's animation system; Clips hold AnimatedProperties hold frames, etc (which is all inferred from the multiplayer entities/properties)
/// </summary>
//public class Rec : MonoBehaviour
public class Clip : MonoBehaviour
{
    //public List<byte> byteList = new List<byte>();
    //public byte[] byteArray;
    public string filePath;



    //public SmashCharacter testChar;
    
    /// <summary>
    /// the entities to record/playback
    /// </summary>
    //public Multi.Entity entity;
    public List<NetBehaviour> targetEntities;



    [Tooltip("The entities being recorded/played back")]
    public List<Entity> entities = new List<Entity>();
    Dictionary<NetBehaviour, Entity> entityLookup = new Dictionary<NetBehaviour, Entity>();

    [Serializable]
    public class Entity
    {
        [HideInInspector]
        [SerializeReference]    // avoid infinite loops during serialization
        public Clip parentClip;
        public string info;

        /// <summary>
        /// Original entity
        /// </summary>
        public Multi.Entity entity;
        //public List<Multi.SyncedProperty> properties = new List<Multi.SyncedProperty>();
        [Tooltip("Animation tracks for this entity; copied from the entity's multiplayer SyncedProperties")]
        public List<Property> properties;


        // constrauctor
        public Entity(Multi.Entity entity, Clip clip)
        {
            this.entity = entity;
            this.parentClip = clip;

            if (entity.parentScript != null)
                //info = "Entity. parentScript: " + entity.parentScript + " Gameobject: " + entity.parentScript.gameObject.name + "";
                info = "Entity: parentScript: " + entity.parentScript;
            else
                info = "Entity: (No parentScript set)";


            // copy properties
            properties = new List<Property>();
            foreach (Multi.SyncedProperty property in entity.properties)
            {
                Property p = new Property(this, property);
                properties.Add(p);
                //print("property added" + p.info);
            }

                //  Property p = new Property(this, entity.properties[1]);
                //  properties.Add(p);
        }
    }

 
    [Serializable]
    public class Property
    {
        public string info;
        public bool canPlayBack = true;

        [HideInInspector]
        [SerializeReference]    // Tell it not to serialize the whole parent object or you get an infinite loop and unity hardlocks
        public Entity parentEntity; 

        public Multi.SyncedProperty property;

        public List<Frame> frames = new List<Frame>();



        public Property(Entity _entity, Multi.SyncedProperty _proprerty)
        {
            //parentEntity = _entity;

            property = _proprerty;
            
            //if (_entity != null)
                info = "Property: GO: " + property.gameObject.name + " AC: " + property.animatedComponent + " obj: " + property.obj;
            //else
            //    info = "Property: (No entity set) " + property.gameObject.name + " " + property.animatedComponent + " " + property.obj;
        }




        //public int frameCount;

        // public GameObject gameObject;
        // public Component animatedComponent;
        // public object obj;  // the actual data, e.g. a Transform, Vector3, etc
        // public Multi.NetData netData;  // the serialized data for this property
    }

    [Serializable]
    public class Frame
    {
        public float time;
        public byte[] data; // byte array
        
        // TODO in curve handle, out curve handle, etc. Like unity frames
    }

    
    
    
    
    


    public void Start()
    {
        // Initialize the byte array or list if needed
        //byteList = new List<byte>();
        //byteArray = new byte[0]; // or initialize with a specific size if needed

        filePath = Path.Combine(Application.persistentDataPath, "smashRecording.dat");





        // var bytes = Capture(testChar.entity);
        // print("bytes " + bytes);


        //BufferSerializer<IReaderWriter> serializer = new BufferSerializer<IReaderWriter>();

    }


    public static byte[] Capture<T>(in T value, int initialCap = 1024)
        where T : INetworkSerializable
    {
        using var writer = new FastBufferWriter(initialCap, Allocator.Temp);
        writer.WriteNetworkSerializable(in value);   // reuse your code
        return writer.ToArray();
    }

    public static T Playback<T>(byte[] bytes) where T : INetworkSerializable, new()
    {
        using var reader = new FastBufferReader(bytes, Allocator.Temp);
        reader.ReadNetworkSerializable(out T value);
        return value;
    }





    public void Update()
    {

        // loop through targetEntities, check if they're already contained in entities list, add them if not
        if (targetEntities != null && targetEntities.Count > 0)
        {
            foreach (var target in targetEntities)
            {
                if (target != null && target.entity != null)
                {
                    var entity = target.entity;

                    // check if the entity is already in the entities list
                    //if (!entities.Exists(e => e.entity == entity))
                    if (!entityLookup.ContainsKey(target))
                    {
                        // add it if not
                        //Entity newEntity = new Entity { entity = entity, info = "Entity: " + entity.gameObject.name };
                        //entities.Add(newEntity);

                        Entity newEntity = new Entity(entity, this);
                        entities.Add(newEntity);

                        entityLookup.Add(target, newEntity);
                        
                        print("Clip "+ this.gameObject +" added new entity: " + newEntity.info);
                        //print("Clip "+ this.gameObject +" added new entity: ");
                    }
                }
            }
        }



        // Debugging: Print the captured data
            if (targetEntities != null && targetEntities.Count > 0 && false)
            {
                foreach (var target in targetEntities)
                {
                    if (target != null && target.entity != null)
                    {
                        var entity = target.entity;

                        var bytes = Capture(entity);
                        Print(bytes, "Captured Entity Data");

                        print("entity properties count: " + entity.properties.Count);
                        foreach (var property in entity.properties)
                        {
                            print("Property: gameObject " + property.gameObject + " animatedComponent " + property.animatedComponent + " obj " + property.obj);
                            if (property.netData != null)
                            {
                                bytes = Capture(property.netData);
                                Print(bytes, "Property Data");
                            }
                        }
                    }
                }
            }
        
    }


    public static void Print(byte[] bytes, string label = "Byte dump")
    {
        // Hex view
        Debug.Log($"{label} (hex): {BitConverter.ToString(bytes)}");

        // Decimal view
        Debug.Log($"{label} (dec): {string.Join(", ", bytes)}");
    }






    // Method to save the byte list to a file
    public void SaveToFile()
    {
        // Write the byte array to the file
        //File.WriteAllBytes(filePath, byteArray);

        Debug.Log($"Saved recording to {filePath}");
    }

    // Method to load the bytes from a file
    public void LoadFromFile()
    {
        // Check if the file exists before trying to read
        if (File.Exists(filePath))
        {
            // Read the bytes from the file
            byte[] byteArray = File.ReadAllBytes(filePath);

            // Clear the current list and add the loaded bytes
            //byteList.Clear();
            //byteList.AddRange(byteArray);
            //byteList = byteArray;

            Debug.Log($"Loaded recording from {filePath}");
        }
        else
        {
            Debug.LogWarning($"File not found: {filePath}");
        }
    }

}

