using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;



#if UNITY_EDITOR


[InitializeOnLoad]
public class PrefabDictionaryUpdater
{
    //private const string PrefabDictionaryPath = "Assets/PrefabDictionary.asset";
    public const string PrefabDictionaryPath = "Assets/Resources/PrefabDictionary.asset";

    static PrefabDictionaryUpdater()
    {
        EditorApplication.playModeStateChanged += UpdatePrefabDictionaryBeforePlay;
        BuildPlayerWindow.RegisterBuildPlayerHandler(BuildWithUpdatedDictionary);
    }

    private static void UpdatePrefabDictionaryBeforePlay(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            UpdatePrefabDictionary();
        } 
    }

    private static void BuildWithUpdatedDictionary(BuildPlayerOptions options)
    {
        UpdatePrefabDictionary();
        BuildPipeline.BuildPlayer(options);
    }

    private static void UpdatePrefabDictionary()
    {
        var prefabDict = AssetDatabase.LoadAssetAtPath<PrefabDictionary>(PrefabDictionaryPath);
        if (prefabDict == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            prefabDict = ScriptableObject.CreateInstance<PrefabDictionary>();
            
            //prefabDict.prefabLookup = new Dictionary<string, GameObject>();

            AssetDatabase.CreateAsset(prefabDict, PrefabDictionaryPath);
            Debug.Log("Created new PrefabDictionary asset!");
        }

        //PrefabDictionary.instance = prefabDict;

        //prefabDict.prefabLookup.Clear();
        prefabDict.values.Clear();
        prefabDict.keys.Clear();

        string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab");
        foreach (string prefabGUID in allPrefabs)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGUID);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab != null)
            {
                string uniqueKey = prefabPath + "/" + prefab.name;
                //prefabDict.prefabLookup[uniqueKey] = prefab;
                prefabDict.keys.Add(uniqueKey);
                prefabDict.values.Add(prefab);
                //Debug.Log("Added prefab to PrefabDictionary: " + uniqueKey + " value: " + prefabDict.prefabLookup[uniqueKey]);
                Debug.Log("Added prefab to PrefabDictionary: " + uniqueKey + " prefab gameobject: " + prefab);
                //Debug.Log("Added prefab to PrefabDictionary: " + prefabDict.prefabLookup[uniqueKey]);
            }
        }
        //prefabDict.prefabCountReport = prefabDict.prefabLookup.Count;
        prefabDict.prefabCountReport = prefabDict.keys.Count;

        EditorUtility.SetDirty(prefabDict);
        AssetDatabase.SaveAssets();
        Debug.Log("Updated PrefabDictionary!");
    }

    /// <summary>
    /// Gets the auto-created scriptable object from the Assets/Resources folder.
    /// </summary>
    /// <returns></returns>
    public static PrefabDictionary GetPrefabDictionary_ScriptableObject()
    {
        PrefabDictionary _instance = Resources.Load<PrefabDictionary>("PrefabDictionary");
        if (_instance == null)
            Debug.LogError("PrefabDictionary ScriptableObject not found in Resources folder!");
        return _instance;
    }

    /// <summary>
    /// Generates the prefab dictionary from the scriptable object's lists. Try to just run this once
    /// </summary>
    /// <returns></returns>
    public static Dictionary<string, GameObject> createDictionary_S_to_GO()
    {
        PrefabDictionary SO = GetPrefabDictionary_ScriptableObject();
        Dictionary<string, GameObject> prefabDict = new Dictionary<string, GameObject>();

        for (int i = 0; i < SO.keys.Count; i++)
        {
            //Debug.Log("PrefabDictionary key: " + SO.keys[i] + " value: " + SO.values[i]);
            prefabDict.Add(SO.keys[i], SO.values[i]);
        }
        return prefabDict;
    }

    /// <summary>
    /// Generates the prefab dictionary from the scriptable object's lists. Try to just run this once
    /// </summary>
    /// <returns></returns>
    public static Dictionary<GameObject, string> createDictionary_GO_to_S()
    {
        PrefabDictionary SO = GetPrefabDictionary_ScriptableObject();
        Dictionary<GameObject, string> prefabDict = new Dictionary<GameObject, string>();

        for (int i = 0; i < SO.keys.Count; i++)
        {
            //Debug.Log("PrefabDictionary key: " + SO.keys[i] + " value: " + SO.values[i]);
            prefabDict.Add(SO.values[i], SO.keys[i]);
        }
        return prefabDict;
    }
}
#endif


[CreateAssetMenu(menuName = "Prefab Dictionary", fileName = "PrefabDictionary")]
public class PrefabDictionary : ScriptableObject
{
    //internal static PrefabDictionary instance;

    public string temp = "It's working";
    [Tooltip("Read only, modifying does nothing")]
    public int prefabCountReport = 0;
    //public Dictionary<string, GameObject> prefabLookup = new Dictionary<string, GameObject>();
    //[SerializeField]
    //public Dictionary<string, GameObject> prefabLookup;

    [Tooltip("You can't serialize dictionaries for some fucking reason, so they can't be stored in ScriptableObjects- it fails silently. Have to rebuild the dict at runtime.")]
    [SerializeField]
    public List<string> keys = new List<string>();
    [SerializeField]
    public List<GameObject> values = new List<GameObject>();

    // Add any other methods or properties you need for the dictionary here.
}