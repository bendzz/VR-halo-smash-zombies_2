using System.Collections;
using System.Collections.Generic;
using UnityEngine;



/// <summary>
/// Startup info for multiplayer (since the Player tends to spawn in before any other scripts)
/// </summary>
[CreateAssetMenu]
public class SO_MyMultiplayer : ScriptableObject
{
    [Header("VR setup (optional)")]
    //[Tooltip("Need to put the NetworkObject on this one or the stack will break")]
    //public Transform XR_SetupTopLevelGameobject;
    public Transform XR_Origin;
    public Transform XR_Headset;

    public Transform leftHandObject;
    public Transform rightHandObject;

}
