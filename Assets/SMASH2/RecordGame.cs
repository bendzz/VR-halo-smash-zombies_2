using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecordGame : MonoBehaviour
{
    public Clip clip;

    Multi multi;

    /// <summary>
    /// Which entities have been grabbed from Multi into the clip
    /// </summary>
    Dictionary<Multi.Entity, Clip.Entity> trackedEntities;

    // Start is called before the first frame update
    void Start()
    {
        clip = gameObject.AddComponent<Clip>();

        multi = Multi.instance;

        trackedEntities = new Dictionary<Multi.Entity, Clip.Entity>();
        
        
        clip.isRecording = true; // start recording by default
    }



    // Update is called once per frame
    void Update()
    {
        
        // grab all entities from Multi that are not already in the clip
        foreach (var entity in Multi.Entity.localEntities)
        {
            if (entity.Value.parentScript == null)
                continue; // skip if no parent script
                
            if (!trackedEntities.ContainsKey(entity.Value))
            {
                clip.targetEntities.Add(entity.Value.parentScript);
                //trackedEntities.Add(entity.Value, entity.Value);
                trackedEntities.Add(entity.Value, null);
            }
        }
    }
}
