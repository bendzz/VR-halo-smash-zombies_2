using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
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
    public byte[] byteArray;
    public string filePath;



    //public SmashCharacter testChar;
    
    /// <summary>
    /// the entities to record/playback
    /// </summary>
    //public Multi.Entity entity;
    public List<NetBehaviour> targetEntities;


    public void Start()
    {
        // Initialize the byte array or list if needed
        //byteList = new List<byte>();
        byteArray = new byte[0]; // or initialize with a specific size if needed

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

        if (targetEntities != null && targetEntities.Count > 0)
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
        // Convert the list of bytes to an array for writing
        //byte[] byteArray = byteList.ToArray();
        //byte[] byteArray = byteList;

        // Write the byte array to the file
        File.WriteAllBytes(filePath, byteArray);

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

