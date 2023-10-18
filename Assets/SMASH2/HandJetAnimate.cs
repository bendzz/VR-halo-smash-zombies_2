using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drop on the handjet object to animate its intracacies
/// </summary>
public class HandJetAnimate : MonoBehaviour
{
    public Transform fan1;
    public Transform fan2;

    public Transform flare1;
    public Transform flare2;
    public Transform flare3;


    // script inputs
    public float thrust;
    public Vector3 direction;


    // make the visuals ramp down slower than the thrust
    float fanPower = 0;
    float flarePower = 0;

    // Start is called before the first frame update
    void Start()
    {

        instantiateNewMaterial(flare1);
        instantiateNewMaterial(flare2);
        instantiateNewMaterial(flare3);

        //setModelColor(flare1, new Color(1, 0, 1, .3f));
        //setModelColor(flare2, new Color(0, 1, 0, 1));



    }

    void instantiateNewMaterial(Transform model)
    {
        Material mat = flare1.GetComponent<MeshRenderer>().sharedMaterial;
        Material mat1 = new Material(mat);
        flare1.GetComponent<MeshRenderer>().material = mat1;
    }
    void setModelColor(Transform model, Color color)
    {
        Material mat = model.GetComponent<MeshRenderer>().sharedMaterial;
        mat.SetColor("_BaseColor", color);
    }


    // update is called once per frame
    void Update()
    {
        // point thrust direction
        if (thrust > .001f)
        {
            //    transform.rotation = Quaternion.LookRotation(-direction, Vector3.up);
            Quaternion thrustTarget = Quaternion.LookRotation(-direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, thrustTarget, Mathf.Clamp01(thrust));
        }


        // fans
        {
            float maxFanSpeed = 5000;
            if (fanPower > 0)
                fanPower -= Time.deltaTime * (maxFanSpeed + fanPower) * .5f;

            if (fanPower < thrust * maxFanSpeed)
                fanPower = thrust * maxFanSpeed;

            //fan1.Rotate(fan1.transform.forward, fanPower * Time.deltaTime);
            //fan2.Rotate(fan2.transform.forward, -fanPower * Time.deltaTime);

            Vector3 a = fan1.localRotation.eulerAngles;
            fan1.localRotation = Quaternion.Euler(a.x, a.y, a.z + -fanPower * Time.deltaTime);

            a = fan2.localRotation.eulerAngles;
            fan2.localRotation = Quaternion.Euler(a.x, a.y, a.z + -fanPower * Time.deltaTime);
        }

        // flares
        {
            float maxFlarePower = .1f;
            if (flarePower > 0)
                flarePower -= Time.deltaTime * (maxFlarePower + flarePower) * 1.5f;

            if (flarePower < thrust * maxFlarePower)
                flarePower = thrust * maxFlarePower;


            flare1.localScale = Vector3.one * Mathf.Clamp01((flarePower - (maxFlarePower * .5f)) * 1.5f);
            flare2.localScale = Vector3.one * flarePower;
            flare3.localScale = Vector3.one * flarePower + Vector3.forward * flarePower * 3f;

            // spin them randomly
            flare1.localRotation = Quaternion.Euler(flare1.localRotation.eulerAngles.x, flare1.localRotation.eulerAngles.y, Random.value * 360);
            flare2.localRotation = Quaternion.Euler(flare1.localRotation.eulerAngles.x, flare1.localRotation.eulerAngles.y, Random.value * 360);
            flare3.localRotation = Quaternion.Euler(flare1.localRotation.eulerAngles.x, flare1.localRotation.eulerAngles.y, Random.value * 360);


            //setModelColor(flare1, new Color(1, 0, 1, .3f));
        }
    }
}
