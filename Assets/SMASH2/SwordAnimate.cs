using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordAnimate : MonoBehaviour
{
    public GameObject sword;

    public Vector3 scale_Original;
    //public Vector3 position_Original;

    // Start is called before the first frame update
    void Start()
    {
        scale_Original = sword.transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
