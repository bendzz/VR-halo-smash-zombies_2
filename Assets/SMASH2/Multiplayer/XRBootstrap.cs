// XRBootstrap.cs — drop into Assets/SMA5H2/Multiplayer/ (or anywhere in project)
// Purpose: Start XR only on Quest devices. Keep XR off on phones. Safe in Editor.

// NOTE: I don't think XR Boostrap does anything.

using System;
using System.Collections;
using UnityEngine;
#if UNITY_XR_MANAGEMENT
using UnityEngine.XR.Management;
#endif


public class XRBootstrap : MonoBehaviour
{
    [Tooltip("Enable XR on Quest devices. On phones, XR stays off.")]
    public bool enableVrOnQuest = true;

    private void Awake()
    {
        // Ensure nothing auto-starts before we decide
        DisableXRImmediate();
    }

    private void OnEnable()
    {
        StartCoroutine(Bootstrap());
    }

    private IEnumerator Bootstrap()
    {
        // Editor or non-Android: keep XR off
#if !UNITY_ANDROID || UNITY_EDITOR
        DisableXRImmediate();
        yield break;
#else
        if (!enableVrOnQuest)
        {
            DisableXRImmediate();
            yield break;
        }

        bool isQuest = IsQuestDevice();
        if (isQuest)
        {
#if UNITY_XR_MANAGEMENT
            if (XRGeneralSettings.Instance != null && XRGeneralSettings.Instance.Manager != null)
            {
                yield return XRGeneralSettings.Instance.Manager.InitializeLoader();
                XRGeneralSettings.Instance.Manager.StartSubsystems();
            }
#endif
        }
        else
        {
            DisableXRImmediate();
        }
        yield break;
#endif
    }

    private void DisableXRImmediate()
    {
#if UNITY_XR_MANAGEMENT
        var mgr = XRGeneralSettings.Instance ? XRGeneralSettings.Instance.Manager : null;
        if (mgr != null)
        {
            try { mgr.StopSubsystems(); } catch { }
            try { mgr.DeinitializeLoader(); } catch { }
        }
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private bool IsQuestDevice()
    {
        print("Checking if this is a Quest device...");
        try
        {
            using (var build = new AndroidJavaClass("android.os.Build"))
            {
                bool isQuest = false;
                string manufacturer = build.GetStatic<string>("MANUFACTURER") ?? string.Empty;
                string model = build.GetStatic<string>("MODEL") ?? string.Empty;
                // Many Quests report manufacturer "Oculus" (or Meta) and model containing "Quest"
                if (manufacturer.Equals("Oculus", StringComparison.OrdinalIgnoreCase) ||
                    manufacturer.Equals("Meta", StringComparison.OrdinalIgnoreCase)) isQuest = true;
                if (model.IndexOf("Quest", StringComparison.OrdinalIgnoreCase) >= 0) isQuest = true;
                if (isQuest)
                {
                    print($"XRBootstrap: Detected Quest device {manufacturer} {model}, enabling XR.");
                    return true;
                }
                print($"XRBootstrap: Detected device {manufacturer} {model}, not a Quest.");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"XRBootstrap: Android build query failed, assuming not Quest. {e.Message}");
        }
        return false;
    }
#else
    private bool IsQuestDevice() => false;
#endif
}

// ------------------------------------------------------------
// AndroidMulticastLock.cs — add to your LAN scene when using discovery on Android/Quest

#if UNITY_ANDROID && !UNITY_EDITOR
public class AndroidMulticastLock : MonoBehaviour
{
    private AndroidJavaObject _lock;
    private void OnEnable()
    {
        try
        {
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            using var wifi = activity.Call<AndroidJavaObject>("getSystemService", "wifi");
            _lock = wifi.Call<AndroidJavaObject>("createMulticastLock", "lan-discovery");
            _lock.Call("setReferenceCounted", true);
            _lock.Call("acquire");
            print("Lan enabled");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"AndroidMulticastLock: failed to acquire: {e.Message}");
        }
    }

    private void OnDisable()
    {
        try { _lock?.Call("release"); } catch { }
        _lock = null;
    }
}
#endif

