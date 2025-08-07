// TODO LATER:
// - Currently about every transform/velocity frame gets recorded; need a way to ignore tiny changes, only record unique frames
// - Game saving/loading: Need a way to store and restore the entire game scene hierarchy, and plug it back into clip entities upon load, and add new frames as the scene changes or stuff spawns/despawns.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
//using Unity.Android.Gradle.Manifest;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
//using Unity.Mathematics;
//using Unity.Netcode;
//using UnityEditor.EditorTools;
#if UNITY_EDITOR
using UnityEditorInternal;
#endif
 
using UnityEngine;

//using UnityEngine;
using Shapes;          // ← Requires “Shapes” package
using TMPro;
using Unity.Netcode;


//public class Rec : MonoBehaviour

/// <summary>
/// Adds recording/playback functionality to the Multi.cs multiplayer system, reusing its serialization system.
/// 2023 remake of Record.cs gameplay recording/playback system (from the OutdoorPacmanVR game).
/// Inspired by unity's animation system; Clips hold AnimatedProperties hold frames, etc (which is all inferred from the multiplayer entities/properties)
/// </summary>
//public class Rec : MonoBehaviour
public class Clip : MonoBehaviour
{
    public string clipName;

    //public List<byte> byteList = new List<byte>();
    //public byte[] byteArray;
    public string filePath;

    [HideInInspector]
    /// <summary>
    /// For backwards compability, to make it at least *kinda possible* to load old obsolete clip files in the future. (Treat this as a const variable; don't change unless the serializer is changed)
    /// </summary>
    public string CLipFileVersion = "CLipFileVersion 0.2";      // CLipFileVersion is a 'magic string' for helping identity these clip files if corrupted
    /// <summary>
    /// The CLipFileVersion of the clip file being loaded right now; only matters DURING loading, afterwards we're using the current, modern file version
    /// </summary>
    string version;


    public bool isRecording = false;
    bool wasRecording = false; // used to detect when recording starts/stops

    public bool isPlaying = false;
    bool wasPlaying = false; // used to detect when playback starts/stops

    public bool loop = true;


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

    /// <summary>
    /// When a clip is loading entities/properties, the newly spawned ones don't know which clip/script they belong to, so they read this. (It's seriallizer weirdness)
    /// </summary>
    public static Clip currentlyLoadingClip;





    // FILE I/O

    // /// <summary>
    // /// Used to save/load the clip to/from a file; Stores all the recordings. Piggybacks off Multiplayer code, but isn't network synced
    // /// </summary>
    // List<Multi.SyncedProperty> clipProperties;
    [Tooltip("Saves to a file the moment you press this")]
    public bool SaveFileNow = false;

    [Tooltip("WIP! NOT FUNCTIONAL YET!")]   // TODO
    public bool SaveFileOnExit = false;







    //public SmashCharacter testChar;

    /// <summary>
    /// the entities to record/playback
    /// </summary>
    //public Multi.Entity entity;
    public List<NetBehaviour> targetNetBehaviours = new List<NetBehaviour>();



    /// <summary>
    /// Just to record the original IsOwner value, so it can be restored after playback
    /// </summary>
    public class NetBehaviourInfo
    {
        /// <summary>
        /// The original IsOwner value, to reset it after playback. (It gets disabled (maybe?) to make scripts play nicer with playback)
        /// </summary>
        public bool Old_IsOwner = false;    // hope this doesn't ever change during gameplay...
        public NetBehaviour netBehaviour;

        public NetBehaviourInfo(NetBehaviour _netBehaviour)
        {
            netBehaviour = _netBehaviour;
            Old_IsOwner = netBehaviour.IsOwner;  // save the original IsOwner value
        }
    }
    /// <summary>
    /// Just to record the original IsOwner values, so they can be restored after playback
    /// </summary>
    public Dictionary<NetBehaviour, NetBehaviourInfo> netBehaviourInfos = new Dictionary<NetBehaviour, NetBehaviourInfo>();




    ClipSerializer clipSerializer = new ClipSerializer();

    //ClipFormatVersion

    /// <summary>
    /// Entry point to serialize the clip data for file I/O. (The normal Clip class can't be serialized since it's a monobehaviour; no default constructor)
    /// </summary>
    [Serializable]
    public class ClipSerializer : INetworkSerializable
    {
        public ClipSerializer()
        { }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            Clip clip = currentlyLoadingClip;

            if (serializer.IsWriter)
                serializer.SerializeValue(ref clip.CLipFileVersion);
            else
                serializer.SerializeValue(ref clip.version);

            serializer.SerializeValue(ref clip.clipLength);
            serializer.SerializeValue(ref clip.clipName);   // likely redundant

            // iffy settings to load
            serializer.SerializeValue(ref clip.loop);
            serializer.SerializeValue(ref clip.SaveFileOnExit);

            //serializer.SerializeValue(ref info);

            // if (serializer.IsReader)
            //     parentClip = currentlyLoadingClip; // newly spawned entities don't know which clip they belong to


            //Multi.SyncedProperty syncedProperty = new Multi.SyncedProperty(Multi.SyncedProperty.invalidIdentifier, this, clip.entities, clip.gameObject, Multi.instance.clip, true);    // Holds the entities list (and all sublists; properties, frames etc)
            Multi.SyncedProperty syncedProperty = new Multi.SyncedProperty(Multi.SyncedProperty.invalidIdentifier, clip, clip.entities, clip.gameObject, Multi.instance.clip, true);    // Holds the entities list (and all sublists; properties, frames etc)
            syncedProperty.netData = new Multi.NetData(syncedProperty.getCurrentValue());

            //byte[] bytes = Capture(syncedProperty.netData);
            //Print(bytes, "Captured Clip Property Data");

            serializer.SerializeValue(ref syncedProperty.netData);

            if (serializer.IsReader)
            {
                //syncedProperty.netData = Playback<Multi.NetData>(bytes);
                syncedProperty.setCurrentValue(syncedProperty.netData.GetData()); // apply the data to the script variable

                //print("property count: " + properties.Count);
            }

            syncedProperty.netData = null;  // save a little ram

            // TODO save NetBehaviourInfos too?
        }
    }




    [Tooltip("The entities being recorded/played back")]
    public List<Entity> entities = new List<Entity>();
    Dictionary<NetBehaviour, Entity> entityLookup = new Dictionary<NetBehaviour, Entity>();

    [Serializable]
    public class Entity : INetworkSerializable
    {
        [HideInInspector]
        public string info;

        [HideInInspector]
        [SerializeReference]    // avoid infinite loops during serialization
        public Clip parentClip;

        [Tooltip("Reassigns scene refs; Dropping a NetBehaviour here will reassign these recordings to control that script/entity- If they match property counts")]
        public NetBehaviour reassignEntityRefs;


        //Multi.NetData netData;

        /// <summary>
        /// Serializes the properties list for file I/O
        /// </summary>
        Multi.SyncedProperty syncedProperty;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter // Not for multiplayer; for saving/loading clips to files
        {

            serializer.SerializeValue(ref info);

            if (serializer.IsReader)
                parentClip = currentlyLoadingClip; // newly spawned entities don't know which clip they belong to


            syncedProperty = new Multi.SyncedProperty(Multi.SyncedProperty.invalidIdentifier, this, properties, parentClip.gameObject, Multi.instance.clip, true);
            syncedProperty.netData = new Multi.NetData(syncedProperty.getCurrentValue());

            //byte[] bytes = Capture(syncedProperty.netData);
            //Print(bytes, "Captured Clip Property Data");

            serializer.SerializeValue(ref syncedProperty.netData);

            if (serializer.IsReader)
            {
                //syncedProperty.netData = Playback<Multi.NetData>(bytes);
                syncedProperty.setCurrentValue(syncedProperty.netData.GetData()); // apply the data to the script variable

                //print("property count: " + properties.Count);
            }

            syncedProperty.netData = null;  // save a little ram

        }

        /// <summary>
        /// Original entity
        /// </summary>
        public Multi.Entity entity;
        //public List<Multi.SyncedProperty> properties = new List<Multi.SyncedProperty>();
        [Tooltip("Animation tracks for this entity; copied from the entity's multiplayer SyncedProperties")]
        public List<Property> properties;

        // /// <summary>
        // /// The original IsOwner value, to reset it after playback. (It gets disabled to make scripts play nicer with playback)
        // /// </summary>
        // bool old_IsOwner = false;   // TODO



        public Entity()
        { properties = new List<Property>(); }  // default constructor for serialization

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
    public class Property : INetworkSerializable
    {
        [HideInInspector]
        public string info;

        public GameObject gameObject;   // display for debugging


        public bool canPlayBack = true;



        public bool canRecord = true;

        [Tooltip("If true, will interpolate frames when playing back. Might be slower")]
        public bool interpolateFrames = true;

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

        //public int mostRecentFrame; // For other scripts overriding its playback to load the current frame quickly


        public Property()
        { frames = new List<Frame>(); }  // default constructor for serialization

        public Property(Entity _entity, Multi.SyncedProperty _proprerty)
        {
            reassignPropertyRefs(_proprerty);

            frames = new List<Frame>();
        }

        public void reassignPropertyRefs(Multi.SyncedProperty _proprerty)
        {
            //parentEntity = _entity;

            property = _proprerty;

            gameObject = property.gameObject;

            //if (_entity != null)
            info = "Property: GO: " + property.gameObject.name + " AC: " + property.animatedComponent + " obj: " + property.obj;
            //else
            //    info = "Property: (No entity set) " + property.gameObject.name + " " + property.animatedComponent + " " + property.obj;
        }



        /// <summary>
        /// Serializes the frames list for file I/O
        /// </summary>
        Multi.SyncedProperty syncedProperty;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter // for file I/O
        {
            serializer.SerializeValue(ref info);
            //serializer.SerializeValue(ref gameObject);
            serializer.SerializeValue(ref canPlayBack);
            serializer.SerializeValue(ref canRecord);


            syncedProperty = new Multi.SyncedProperty(Multi.SyncedProperty.invalidIdentifier, this, frames, currentlyLoadingClip.gameObject, Multi.instance.clip, true);
            syncedProperty.netData = new Multi.NetData(syncedProperty.getCurrentValue());

            serializer.SerializeValue(ref syncedProperty.netData);  // save the frames list

            if (serializer.IsReader)
            {
                // byte[] bytes = Capture(syncedProperty.netData);
                // Print(bytes, "Captured Property frames Data for " + info);

                syncedProperty.setCurrentValue(syncedProperty.netData.GetData()); // apply the data to the script variable

                // print("frames count: " + frames.Count);
                // if (frames.Count > 50)
                // {
                //     Print(frames[0].data, "frame 0");
                //     Print(frames[50].data, "frame 50");
                //     //print("frame 1 time: " + frames[0].data.ToString() + " frame 2 time: " + frames[1].data.ToString());
                // }
            }

            syncedProperty.netData = null;  // save a little ram
        }

    }



    //[Serializable]
    public class Frame : INetworkSerializable
    {
        public float time;
        public byte[] data; // byte array

        // TODO in curve handle, out curve handle, etc. Like unity frames



        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter // for file I/O
        {
            serializer.SerializeValue(ref time);
            serializer.SerializeValue(ref data);
            //print("Frame.NetworkSerialize: time: " + time + " data length: " + data.Length);
        }
    }

    [Tooltip("WARNING! WILL DELETE CURRENT CLIP! (Loads a clip file using the clipName/path)")]
    public bool LoadFileNow = false;














    //public Dictionary<float, int> tempDict;

    public void Start()
    {

        //filePath = Path.Combine(Application.persistentDataPath, "smashRecording.dat");
        filePath = Application.persistentDataPath; ;


        if (clipName == null || clipName == "")
        {
            // Generate a default name if none is provided
            clipName = "Clip_ D_ " + DateTime.Now.ToString("yy/MM/dd HH/mm/ss") + " RNG_ " + Mathf.Round(UnityEngine.Random.Range(0f, 999f)).ToString();
        }



        //tempDict = new Dictionary<string, int> { { "Alice", 10 }, { "Bob", 20 }, { "Carol", 30 } };
        //tempDict = new Dictionary<float, int> { { .1f, 10 }, { .2f, 20 }, { .3f, 30 } };


        // // I/O
        // clipProperties = new List<Multi.SyncedProperty>();

        // //clipProperties.Add(new Multi.SyncedProperty(Multi.SyncedProperty.invalidIdentifier, this, netBehaviourInfos, gameObject, Multi.instance.clip, true));
        // //clipProperties.Add(new Multi.SyncedProperty(Multi.SyncedProperty.invalidIdentifier, this, tempDict, gameObject, Multi.instance.clip, true));

        // clipProperties.Add(new Multi.SyncedProperty(Multi.SyncedProperty.invalidIdentifier, this, entities, gameObject, Multi.instance.clip, true));


        // print("clipProperties count: " + clipProperties.Count);

        HoloLabel.SpawnLabel("PRESS TO LAUNCH", new Vector3(0, 1.4f, 2f));
    }


    /// <summary>
    /// Runs the serialization code in a compatible object to turn it into byte code; for reusing 'Unity Netcode for Gameobjects' serialization code.
    /// </summary>
    public static byte[] Capture<T>(in T value, int initialCap = 1024)
        where T : INetworkSerializable
    {
        //using var writer = new FastBufferWriter(initialCap, Allocator.Temp);
        using var writer = new FastBufferWriter(1024, allocator: Allocator.Temp, int.MaxValue);       // grow as needed (up to about 2 GB)
        writer.WriteNetworkSerializable(in value);   // reuse your code
        return writer.ToArray();
    }

    /// <summary>
    /// Deserializes the byte code into a compatible object; for reusing 'Unity Netcode for Gameobjects' serialization code. 
    /// </summary>
    public static T Playback<T>(byte[] bytes) where T : INetworkSerializable, new()
    {
        using var reader = new FastBufferReader(bytes, Allocator.Temp);
        reader.ReadNetworkSerializable(out T value);
        return value;
    }



    void updateFrameCountsDisplay()  // just so I can see them in inspector
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

    //public void Update()
    public void LateUpdate()
    {

        // loop through targetEntities, check if they're already contained in entities list, add them if not
        if (targetNetBehaviours != null && targetNetBehaviours.Count > 0)
        {
            foreach (var target in targetNetBehaviours)
            {
                if (target == null)
                    continue;

                if (!netBehaviourInfos.ContainsKey(target))
                    netBehaviourInfos.Add(target, new NetBehaviourInfo(target));


                if (target.entity != null)
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

        // Loop all entities to check if they have a reassignEntityRefs set, use it if so
        foreach (var entity in entities)
        {
            if (entity != null && entity.reassignEntityRefs != null)
            {
                Multi.Entity netEntity = entity.reassignEntityRefs.entity;
                int netPropCount = netEntity.properties.Count;
                int recPropCount = entity.properties.Count;

                if (entity.reassignEntityRefs.entity == null)
                {
                    Debug.LogError("Entity " + entity.info + "'s reassignEntityRefs is null; skipping reassignEntityRefs");
                    entity.reassignEntityRefs = null;
                    continue; // skip this entity if the reassignEntityRefs is null
                }

                if (netPropCount != recPropCount)
                {
                    Debug.LogError("Entity " + entity.info + "'s reassignEntityRefs: " + entity.reassignEntityRefs + " have different property counts; netPropCount " + netPropCount + " vs recPropCount " + recPropCount);
                    entity.reassignEntityRefs = null;
                    continue; // skip this entity if the property counts don't match
                }
                print("Reassigning " + netPropCount + " properties of entity " + entity.info + " to reference " + entity.reassignEntityRefs + " properties instead.");
                //print("Reassigning " + netPropCount + " properties of entity " + entity

                //reassignPropertyRefs
                // Just lines up properties using their indices; Fragile...
                //foreach (var property in entity.properties)
                entity.entity = netEntity;
                //entity.parentScript = netEntity.parentScript; // reassign parentScript to the new entity's parentScript
                entity.parentClip = this;   // unncessary?
                for (int i = 0; i < netPropCount; i++)
                {
                    entity.properties[i].reassignPropertyRefs(netEntity.properties[i]);
                }
                entity.reassignEntityRefs = null;
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
                updateFrameCountsDisplay();
                //print("Recording stopped at " + Time.time + ". Duration: " + (Time.time - startedRecordingTime));
                print("Recording stopped at " + Time.time + ". New duration: " + clipLength);
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


            // Loop through each entity and its properties, capturing their frame data
            foreach (var entity in entities)
            {
                if (entity != null && entity.entity != null && entity.entity.parentScript != null)
                {
                    // Capture the data for each property
                    foreach (var property in entity.properties)
                    {
                        if (property.canRecord)
                        {
                            if (property.property.netData == null)
                                continue;


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


        // PLAYBACK
        {
            // playback start/stop
            {
                if (isPlaying && !wasPlaying)   // playback just started
                {
                    wasPlaying = isPlaying;
                    lastFrameTImestamp = Time.time;

                    foreach (var netBehaviourInfo in netBehaviourInfos)
                    {
                        netBehaviourInfo.Value.Old_IsOwner = netBehaviourInfo.Value.netBehaviour.IsOwner; // save the original IsOwner value
                        netBehaviourInfo.Value.netBehaviour.IsOwner = false; // set IsOwner to false for playback, so scripts play nicer
                        netBehaviourInfo.Value.netBehaviour.IsPlayBack = true;
                    }


                    print("Playback started at " + Time.time);
                }
                else if (!isPlaying && wasPlaying)  // playback just stopped
                {
                    wasPlaying = isPlaying;

                    foreach (var netBehaviourInfo in netBehaviourInfos)
                    {
                        netBehaviourInfo.Value.netBehaviour.IsOwner = netBehaviourInfo.Value.Old_IsOwner; // restore the original IsOwner value
                        netBehaviourInfo.Value.netBehaviour.IsPlayBack = false;
                    }

                    print("Playback stopped at " + Time.time);
                }
            }




            if (isPlaying)
            {
                incrementClipTime();
                if (clipTime > clipLength)  // reset clipTime if it goes past the end of the clip
                {
                    if (loop)
                        clipTime = 0;
                }


                // Loop through each entity and its properties, capturing their data
                foreach (var entity in entities)
                {
                    //print("1");
                    // print(entity);
                    // print(entity.entity);
                    // print(entity.entity.parentScript);
                    if (entity != null && entity.entity != null && entity.entity.parentScript != null)
                    {
                        //print("2");
                        // Capture the data for each property
                        foreach (var property in entity.properties)
                        {
                            //print("3");
                            if (property.canRecord)
                            {
                                //print("4");
                                if (property.property.netData == null)
                                    continue;
                                //print("5");
                                int frameIndex = FindFrameBefore(property.frames, clipTime);
                                if (frameIndex == -1)
                                {
                                    // Clip time is before first frame; play that frame
                                    frameIndex = 0;
                                }


                                // Get the frame data
                                byte[] frameData = property.frames[frameIndex].data;


                                // deserialize
                                property.property.netData = Playback<Multi.NetData>(frameData);
                                object frameObject = property.property.netData.GetData();   // the deserialized frame object


                                if (property.interpolateFrames && frameIndex + 1 < property.frames.Count)   // Frame interpolation
                                {
                                    float lerpPercent = (clipTime - property.frames[frameIndex].time) / (property.frames[frameIndex + 1].time - property.frames[frameIndex].time);

                                    if (frameObject is Transform)
                                        frameObject = new transformCopy((Transform)frameObject); // copy the transform to allow interpolation

                                    object frameObject2 = Playback<Multi.NetData>(property.frames[frameIndex + 1].data).GetData();   // TODO should only read this if it's a type we can interpolate, for perf; but how to do it cleanly?

                                    //frameObject = interpolateFrames(frameObject, property.frames[frameIndex + 1].data, lerpPercent);
                                    frameObject = interpolateFrames(frameObject, frameObject2, lerpPercent);
                                }


                                //property.property.setCurrentValue(property.property.netData.GetData()); // apply the data to the script variable
                                property.property.setCurrentValue(frameObject); // apply the data to the script/component variable



                            }
                        }
                    }
                }

            }
        }




        // File I/O
        // TODO these will break horribly with more than 1 item in clipProperties! (Would need a clever serialization method to concatenate then unjoin the byte data from each property. How is it done in multiplayer?)
        {
            string path = Path.Combine(filePath, clipName + ".dat");

            if (SaveFileNow)
            {
                SaveFileNow = false;

                currentlyLoadingClip = this;


                byte[] bytes = Capture(clipSerializer); // runs serializer
                //Print(bytes, "Captured Clip Property Data");



                File.WriteAllBytes(path, bytes);

                Debug.Log($"Saved recording to {path}");


            }

            if (LoadFileNow)
            {
                LoadFileNow = false;

                currentlyLoadingClip = this;

                if (File.Exists(path))
                {
                    byte[] bytes = File.ReadAllBytes(path);


                    Playback<ClipSerializer>(bytes);    // runs deserializer


                    updateFrameCountsDisplay();

                    //
#if UNITY_EDITOR
                    // tell the Inspector to rebuild its list   // To fix an error where if the entities list has 0 items, loading a clip will cause infinite UI errors forever
                    InternalEditorUtility.RepaintAllViews();
#endif

                    Debug.Log($"Loaded recording from {path}");
                }
                else
                {
                    Debug.LogWarning($"File not found: {path}");
                }
            }
        }







        // // Debugging: Print the entities/properties of the targetNetBehaviours list
        // if (targetNetBehaviours != null && targetNetBehaviours.Count > 0 && false)
        // {
        //     foreach (var target in targetNetBehaviours)
        //     {
        //         if (target != null && target.entity != null)
        //         {
        //             var entity = target.entity;

        //             var bytes = Capture(entity);
        //             Print(bytes, "Captured Entity Data");

        //             print("entity properties count: " + entity.properties.Count);
        //             foreach (var property in entity.properties)
        //             {
        //                 print("Property: gameObject " + property.gameObject + " animatedComponent " + property.animatedComponent + " obj " + property.obj);
        //                 if (property.netData != null)
        //                 {
        //                     bytes = Capture(property.netData);
        //                     Print(bytes, "Property Data");
        //                 }
        //             }
        //         }
        //     }
        // }

    }


    /// <summary>
    /// For printing byte arrays
    /// </summary>
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



    /// <summary>
    /// For interpolating between frames during playback.
    /// WARNING can't interpolate transforms (they overwrite each other into 1 instance!) Use transformCopy instead
    /// </summary>
    //public static object interpolateFrames(object frame1, byte[] frame2Bytes, float lerpPercent)
    public static object interpolateFrames(object frame1, object frame2, float lerpPercent)
    {
        lerpPercent = Mathf.Clamp01(lerpPercent);  // sometimes goes super negative when the clipTime is before the first frame  // unncessary?
                                                   //bool validType = false;

        if (frame1 is transformCopy)
        {

            transformCopy f1 = (transformCopy)frame1;
            transformCopy f2 = null;
            if (frame2 is Transform)
                f2 = new transformCopy((Transform)frame2);  // idk if this object change is net faster, but it is simpler
            else if (frame2 is transformCopy)
                f2 = (transformCopy)frame2;

            //Transform f2 = (Transform)frame2; 

            Vector3 localPosition = Vector3.Lerp(f1.localPosition, f2.localPosition, lerpPercent);
            Quaternion localRotation = Quaternion.Slerp(f1.localRotation, f2.localRotation, lerpPercent);
            Vector3 scale = Vector3.Lerp(f1.localScale, f2.localScale, lerpPercent);


            f1.localPosition = localPosition;
            f1.localRotation = localRotation;
            f1.localScale = scale;

            //print("lerped f1.localPosition: " + f1.localPosition.ToString("F5") + " f1.localRotation: " + f1.localRotation.ToString("F5"));

            return f1; // Return the modified frame1
        }
        //else
        //frame2 = Playback<Multi.NetData>(frame2Bytes).GetData();   // TODO should only read this if it's a type we can interpolate, for perf; but how do it 
        if (frame1 is float)
        {
            float f1 = (float)frame1;
            float f2 = (float)frame2;

            float interpolatedValue = Mathf.Lerp(f1, f2, lerpPercent);
            return interpolatedValue; // Return the interpolated value
        }
        else if (frame1 is Vector3)
        {
            Vector3 v1 = (Vector3)frame1;
            Vector3 v2 = (Vector3)frame2;

            Vector3 interpolatedValue = Vector3.Lerp(v1, v2, lerpPercent);
            return interpolatedValue; // Return the interpolated value
        }

        return frame1; // Failed to interpolate
    }

    /// <summary>
    /// Can't make copies of transforms; they overwrite each other unless you make whole gameobjects with them. So save copies in this. (For animation interpolation)
    /// </summary>
    public class transformCopy
    {
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;

        public transformCopy(Transform t)
        {
            localPosition = t.localPosition;
            localRotation = t.localRotation;
            localScale = t.localScale;
        }

        public void ApplyTo(Transform t)
        {
            t.localPosition = localPosition;
            t.localRotation = localRotation;
            t.localScale = localScale;
        }

    }
}



public class HoloLabel : MonoBehaviour
{
    [Header("Look & feel")]
    public Color textColor = new(0.85f, 0.95f, 1f);   // near-white
    public Color outlineColor = new(0.45f, 0.60f, 0.75f); // grey-blue
    public Color glowColor = new(0.25f, 0.45f, 0.75f);
    public Color panelColor = new(0.08f, 0.12f, 0.16f, 0.55f); // translucent
    public float cornerRadius = 0.02f;     // metres
    public Vector2 padding = new(0.04f, 0.02f);

    public static HoloLabel SpawnLabel(string message, Vector3 pos)
    {
        var go = new GameObject($"Label:{message}");
        var label = go.AddComponent<HoloLabel>();
        label.Init(message);
        go.transform.position = pos;
        return label;
    }

    void Init(string message)
    {
        // ---------- Text ----------
        var tmpGO = new GameObject("TMP");
        tmpGO.transform.SetParent(transform, false);
        var tmp = tmpGO.AddComponent<TextMeshPro>();
        tmp.fontSize = 0.6f;  // metres
        tmp.text = message;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = textColor;

        // Outline & glow (use SDF params)
        tmp.fontSharedMaterial = Instantiate(tmp.fontSharedMaterial);
        tmp.fontSharedMaterial.SetFloat("_OutlineWidth", 0.21f);
        tmp.fontSharedMaterial.SetColor("_OutlineColor", outlineColor);
        tmp.fontSharedMaterial.SetFloat("_GlowPower", 0.0f);    // enable glow
        tmp.fontSharedMaterial.SetFloat("_GlowOuter", 0.45f);
        tmp.fontSharedMaterial.SetColor("_GlowColor", glowColor);

        // ---------- Shapes panel (rounded rect) ----------
        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(transform, false);
        var shape = panelGO.AddComponent<ShapeRenderer>();
        var rect = panelGO.AddComponent<Rectangle>();

        // Size the rectangle after TMP generates its bounds
        StartCoroutine(DelaySize(panelGO.transform, tmp, rect));
    }

    System.Collections.IEnumerator DelaySize(Transform t, TextMeshPro tmp, Rectangle rect)
    {
        yield return null; // wait 1 frame for TMP to populate textBounds
        var b = tmp.textBounds.size;
        //rect.Size = new Vector2(b.x, b.y) + padding * 2;
        rect.CornerRadius = cornerRadius;
        rect.Color = panelColor;
        rect.Thickness = 0f;         // filled
        //rect.Rounded = true;
        rect.BlendMode = ShapesBlendMode.Transparent;
        rect.transform.localPosition = Vector3.back * 0.001f; // tiny push so panel is behind text
    }
}
