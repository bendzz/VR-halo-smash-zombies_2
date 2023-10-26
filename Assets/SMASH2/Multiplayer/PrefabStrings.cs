using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;


/// <summary>
/// Drop this on some gameobject that spawns when your game starts. It'll gather prefabs during editor mode then save them in [SerializeField]s for runtime.
/// For network spawning prefabs. Generates string to prefab to string dictionaries
/// </summary>
[ExecuteAlways]
public class PrefabStrings : MonoBehaviour
{
    // Singleton pattern
    public static PrefabStrings Instance;

    // Static dictionary to be used internally
    private static Dictionary<string, GameObject> internalDict = new Dictionary<string, GameObject>();

    // Serializable lists to be exposed in the editor
    [SerializeField]
    public List<string> keys = new List<string>();
    [SerializeField]
    public List<GameObject> values = new List<GameObject>();

    // Flag for the editor to trigger the update
    [Tooltip("Toggle this in the editor to update the dictionary")]
    public bool updateDict = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Another instance of PrefabStrings already exists!");
        }
        Instance = this;
        //UpdateInternalDict();

        if (Application.IsPlaying(gameObject))
            DontDestroyOnLoad(this);
    }

    private void Update()
    {
        // Editor-specific logic
        if (!Application.IsPlaying(gameObject) || updateDict)
        {
            UpdateInternalDict();
            TransferDictToLists();
            updateDict = false;
        }
    }

    [ContextMenu("Update Dictionary")] 
    void UpdateInternalDict()
    {
#if UNITY_EDITOR

        internalDict.Clear();

        string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab");
        foreach (string prefabGUID in allPrefabs)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGUID);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab != null)
            {
                string uniqueKey = prefabPath + prefab.name;

                if (!internalDict.ContainsKey(uniqueKey))
                {
                    internalDict.Add(uniqueKey, prefab);
                }
            }
        }
#endif
    }

    void TransferDictToLists()
    {
        keys.Clear();
        values.Clear();

        foreach (KeyValuePair<string, GameObject> kvp in internalDict)
        {
            keys.Add(kvp.Key);
            values.Add(kvp.Value);
        }
    }

    public static Dictionary<string, GameObject> CreateDictionary_S_to_GO()
    {
        Dictionary<string, GameObject> dict = new Dictionary<string, GameObject>();
        print("instance " + Instance);
        print ("instance.keys " + Instance.keys);
        for (int i = 0; i < Instance.keys.Count; i++)
        {
            dict[Instance.keys[i]] = Instance.values[i];
        }
        return dict;
    }

    public static Dictionary<GameObject, string> CreateDictionary_GO_to_S()
    {
        Dictionary<GameObject, string> dict = new Dictionary<GameObject, string>();
        for (int i = 0; i < Instance.values.Count; i++)
        {
            dict[Instance.values[i]] = Instance.keys[i];
        }
        return dict;
    }

}
