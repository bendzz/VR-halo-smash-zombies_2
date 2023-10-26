using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordAnimate : MonoBehaviour
{
    public GameObject sword;
    public Rigidbody body;
    public bool held = false;

    public float lifeTimer = 0;
    public bool dying = false;

    public Vector3 scale_Original;
    //public Vector3 position_Original;
    public float scale = 1;

    // Start is called before the first frame update
    void Start()
    {
        scale_Original = sword.transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        if (dying)
        {
            lifeTimer -= Time.deltaTime;
            if (lifeTimer < 0)
            {
                gameObject.transform.localScale /= (1 - lifeTimer * 1);

                if (gameObject.transform.localScale.x < .01f)
                    Destroy(gameObject);
                //Destroy(gameObject);
            }
        }

        if (scale < 1 && !dying)
        {
            scale += Time.deltaTime * (1f/3f);
            print("scaling " + scale);

            gameObject.transform.localScale = scale_Original * scale;
        }
    }

    /// <summary>
    /// Pretend you're a newly spawned sword growing back to size
    /// </summary>
    public void respawn()
    {
        scale = 0;
    }
}
