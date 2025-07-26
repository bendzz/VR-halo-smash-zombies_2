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
/// Inspired by unity's animation system; Clips hold AnimatedProperties hold frames, etc. Except I'm doing away with Clips for the most part, they suck.
/// </summary>
public class Rec : MonoBehaviour
{
    //public List<byte> byteList = new List<byte>();
    public byte[] byteArray;
    public string filePath;



    public SmashCharacter testChar;


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

 

 

    // public struct RecordingWriter : IReaderWriter
    // {
    //     public static readonly List<byte> Capture = new();

    //     // ----- required by the interface -----
    //     public bool IsReader => false;    // we only write
    //     public int Length => Capture.Count;
    //     public int Position { get; private set; }

    //     public void SerializeValue(ref byte value) => Write(ref value);
    //     public void SerializeValue(ref string s, bool _ = false)
    //     {
    //         var bytes = System.Text.Encoding.UTF8.GetBytes(s);
    //         int len = bytes.Length;
    //         SerializeValue(ref len);
    //         Capture.AddRange(bytes);
    //         Position += len;
    //     }

    //     // ... repeat tiny wrappers for other primitive overloads as needed ...

    //     // generic helper for blittable structs
    //     private unsafe void Write<T>(ref T value) where T : unmanaged
    //     {
    //         int size = UnsafeUtility.SizeOf<T>();
    //         byte* p = (byte*)UnsafeUtility.AddressOf(ref value);
    //         for (int i = 0; i < size; i++) Capture.Add(p[i]);
    //         Position += size;
    //     }

    //     // required but unused methods
    //     public bool PreCheck(int amount) => true;
    //     // other interface members can be empty or throw if they’re reader‑only
    // }


    public void Update()
    {

        if (testChar != null)
        {
            var bytes = Capture(testChar.entity);
            //print("bytes " + bytes);
            Print(bytes, "Captured Entity Data");
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

