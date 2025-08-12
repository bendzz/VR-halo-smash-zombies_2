using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.VisualScripting;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using TMPro;
using System;
using System.Collections.Generic;

public class LAN : MonoBehaviour
{



    private void OnEnable()
    {
        // Start LAN discovery



        // switch to LAN mode
        // reconfigure UI to work with raycasts, for mobile/oculus eye tracking/hand lasers

        // Call HostLan() using GetWifiIPv4() address



        AndroidMulticastLock_Start();



    }

    void Update()
    {


    }


    /// <summary>
    /// "Use that IP to create your UDP socket + compute the subnet broadcast (don’t just use 255.255.255.255)."
    /// </summary>
    /// <returns></returns>
    static IPAddress GetWifiIPv4()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType != NetworkInterfaceType.Wireless80211) continue;
            foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                    return ua.Address;
        }
        return IPAddress.Any;
    }




    // Start and stop LAN
    public ushort lanPort = 7777;

    public void HostLan()
    {
        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        utp.SetConnectionData("0.0.0.0", lanPort, "0.0.0.0");
        NetworkManager.Singleton.StartHost();
        // TODO: start discovery advertise here
    }

    public void JoinLan(string hostIp)
    {
        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        utp.SetConnectionData(hostIp, lanPort);
        NetworkManager.Singleton.StartClient();
    }






    // Enable LAN on android
    private AndroidJavaObject _lock;
    /// <summary>
    /// AndroidMulticastLock.cs — add to your LAN scene when using discovery on Android/Quest
    /// </summary>
    public void AndroidMulticastLock_Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            using var wifi = activity.Call<AndroidJavaObject>("getSystemService", "wifi");
            _lock = wifi.Call<AndroidJavaObject>("createMulticastLock", "lan-discovery");
            _lock.Call("setReferenceCounted", true);
            _lock.Call("acquire");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"AndroidMulticastLock: failed to acquire: {e.Message}");
        }
#endif
    }
    private void OnDisable()
    {
        try { _lock?.Call("release"); } catch { }
        _lock = null;
    }



}



// TODO! (How?)
// ------------------------------------------------------------
// AndroidManifest additions (place in Assets/Plugins/Android/AndroidManifest.xml)
// <uses-permission android:name="android.permission.ACCESS_WIFI_STATE" />
// <uses-permission android:name="android.permission.CHANGE_WIFI_MULTICAST_STATE" />
