using UnityEngine;

// ------------------------------------------------------------
// LANTransportBootstrap.cs — minimal direct-IP start/join without UGS

using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class LANTransportBootstrap : MonoBehaviour
{
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
}

// ------------------------------------------------------------
// AndroidManifest additions (place in Assets/Plugins/Android/AndroidManifest.xml)
// <uses-permission android:name="android.permission.ACCESS_WIFI_STATE" />
// <uses-permission android:name="android.permission.CHANGE_WIFI_MULTICAST_STATE" />
