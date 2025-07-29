// TODO LATER:
// - Every transform/velocity frame gets recorded; need a way to ignore tiny changes, only record unique frames

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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


    public bool isRecording = false;
    bool wasRecording = false; // used to detect when recording starts/stops

    public bool isPlaying = false;
    bool wasPlaying = false; // used to detect when playback starts/stops

    public bool isLooping = true;


    [HideInInspector]
    /// <summary>
    /// Used to check how long it was since the last frame was recorded, to add to the clipTime/length
    /// </summary>
    public float lastFrameTImestamp = 0;
    
    //[HideInInspector]
    //public float startedRecordingTime = 0f;
    [Tooltip("The playback/recording time of this clip")]
    public float clipTime = 0;

    [Tooltip("End 'end' of the clip. (Note, frames might go past this if the code screws up)")]
    public float clipLength = 0;
    







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
        public string info;

        [HideInInspector]
        [SerializeReference]    // avoid infinite loops during serialization
        public Clip parentClip;

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

        public void updateFrameCounts()
        {
            //loop through properties
            foreach (var property in properties)
            {
                property.frameCount = property.frames.Count;
            }
        }
    }

 
    [Serializable]
    public class Property
    {
        [HideInInspector]
        public string info;
        public bool canPlayBack = true;
        public bool canRecord = true;

        [HideInInspector]
        [SerializeReference]    // Tell it not to serialize the whole parent object or you get an infinite loop and unity hardlocks
        public Entity parentEntity; 

        public Multi.SyncedProperty property;

        [HideInInspector]
        [SerializeReference]
        public List<Frame> frames;

        //public int frameCount => frames.Count;
        [Tooltip("(Only updated periodically, or the editor lags)")]
        public int frameCount = 0;


        public Property(Entity _entity, Multi.SyncedProperty _proprerty)
        {
            //parentEntity = _entity;

            property = _proprerty;

            //if (_entity != null)
            info = "Property: GO: " + property.gameObject.name + " AC: " + property.animatedComponent + " obj: " + property.obj;
            //else
            //    info = "Property: (No entity set) " + property.gameObject.name + " " + property.animatedComponent + " " + property.obj;

            frames = new List<Frame>();
        }

    }

    //[Serializable]
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



void updateEntityFrameCounts()  // just so I can see them in inspector
    {
        // loop through entities and update their frame counts
        foreach (var entity in entities)
        {
            if (entity != null)
            {
                entity.updateFrameCounts();
            }
        }
    }



    void incrementClipTime()    // tick clip forward 1 frame, during recording/playback
    {
        float currentTime = Time.time;
        float frameDelta = currentTime - lastFrameTImestamp;
        lastFrameTImestamp = currentTime;

        clipTime += frameDelta;
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

                        print("Clip " + this.gameObject + " added new entity: " + newEntity.info);
                        //print("Clip "+ this.gameObject +" added new entity: ");
                    }
                }
            }
        }


        // recording start/stop
        {
            if (isRecording && !wasRecording)   // recording just started
            {
                //startedRecordingTime = Time.time;

                lastFrameTImestamp = Time.time;
                clipTime = clipLength;  // just start all recordings from the end of the clip, for now

                wasRecording = isRecording;
                //print("Recording started at " + startedRecordingTime);
                print("Recording started at" + Time.time + ". Clip length: " + clipLength);
            }
            else if (!isRecording && wasRecording)  // recording just stopped
            {
                wasRecording = isRecording;
                updateEntityFrameCounts();
                //print("Recording stopped at " + Time.time + ". Duration: " + (Time.time - startedRecordingTime));
                print("Recording stopped at " + Time.time + ". New duration: " + clipLength);
            }
        }


        // playback start/stop
        {
            if (isPlaying && !wasPlaying)   // playback just started
            {
                wasPlaying = isPlaying;
                lastFrameTImestamp = Time.time;
                print("Playback started at " + Time.time);
            }
            else if (!isPlaying && wasPlaying)  // playback just stopped
            {
                wasPlaying = isPlaying;
                print("Playback stopped at " + Time.time);
            }
        }


        if (isRecording)    // record frames
        {
            isPlaying = false;  // stop playback if recording (I figure if it was a mistake, this will alert the user best)

            // clipTime = Time.time - startedRecordingTime;
            // clipLength = clipTime;

            // TODO should add 1 extra frame time when recording stops, so it's not mushed together
            // TODO should add a highlight to frames that are the first frame in a recording or cut, for easier editing
            {
                incrementClipTime();
                clipLength = clipTime;  // TODO what if u start recording halfway through a clip? (Also how would it overwrite the clip? Is that even a good idea?)
            }


            // Loop through each entity and its properties, capturing their data
            foreach (var entity in entities)
            {
                if (entity != null && entity.entity != null)
                {
                    // Capture the data for each property
                    foreach (var property in entity.properties)
                    {
                        if (property.canRecord)
                        {
                            if (property.property.netData == null)
                                continue;

                            // Create a new frame for this entity
                            Frame frame = new Frame();
                            //frame.time = Time.time - startedRecordingTime;
                            frame.time = clipTime;


                            // Capture the property's data
                            var bytes = Capture(property.property.netData);
                            frame.data = bytes; // Store the captured data in the frame


                            // check for duplicate frames
                            if (property.frames.Count > 1)
                            {
                                // check if the frame bytedata is identical
                                if (property.frames[property.frames.Count - 1].data.SequenceEqual(frame.data))
                                {
                                    // skip adding this frame, it's a duplicate
                                    continue;
                                }
                            }


                            // record frame
                            property.frames.Add(frame);
                            //property.frameCount = property.frames.Count;
                        }
                    }
                }
            }
        }

        if (isPlaying)
        {
            incrementClipTime();
            if (clipTime > clipLength)  // reset clipTime if it goes past the end of the clip
            {
                if (isLooping)
                    clipTime = 0;
                else
                {
                    // ???
                }
            }


            // Loop through each entity and its properties, capturing their data
            foreach (var entity in entities)
            {
                if (entity != null && entity.entity != null)
                {
                    // Capture the data for each property
                    foreach (var property in entity.properties)
                    {
                        if (property.canRecord)
                        {
                            if (property.property.netData == null)
                                continue;

                            int frameIndex = FindFrameBefore(property.frames, clipTime);
                            if (frameIndex == -1)
                            {
                                // Clip time is before first frame; play that frame
                                frameIndex = 0;
                            }

                            // TODO interpolation between frames

                            // Get the frame data
                            byte[] frameData = property.frames[frameIndex].data;



                            // apply frame

                            //property.property.netData = Playback<Multi.INetData>(frameData); 
                            property.property.netData = Playback<Multi.NetData>(frameData);

                            property.property.setCurrentValue(property.property.netData.GetData()); // apply the data to the property
                            
                            
                            // If you know the concrete type of netData, use it here instead of Multi.INetData.
                            // For example, if netData is always of type MyNetData, use Playback<MyNetData>(frameData).
                            // Otherwise, you must use the correct type argument explicitly.
                            // Example:
                            // property.property.netData = Playback<MyNetData>(frameData);

                            // If you need to support multiple types, you may need to store the type info and use reflection or a type switch.
                            // localProp.netData = data;   // idk if this helps
                            // localProp.setCurrentValue(data.GetData());

                        }
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


    /// <summary>
    /// Returns the index of the frame whose time is the last one < clipTime.
    /// Returns -1 if no such frame exists (i.e. clipTime is before the first frame).
    /// Assumes property.frames is sorted ascending by Frame.time.
    /// </summary>
    public static int FindFrameBefore(List<Frame> frames, float clipTime)   // TODO could I prime this with the last frame index, so it doesn't have to start from 0 every time? Probably thrashing the cache
    {
        int lo = 0;
        int hi = frames.Count - 1;
        int result = -1;               // keeps track of best candidate so far

        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;  // faster int division by 2

            if (frames[mid].time < clipTime)
            {
                result = mid;          // mid is valid—search right half for a later one
                lo = mid + 1;
            }
            else                       // mid.time >= clipTime → search left half
            {
                hi = mid - 1;
            }
        }
        return result;
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

