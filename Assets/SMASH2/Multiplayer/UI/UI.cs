using TMPro;
using UnityEngine;


public class UI : MonoBehaviour
{
    // public TextWithLinkEvents textWithLinks;

    // public ClickableTMPText clickableText;

    public TMP_Text text;
    public Transform testtransform;

    public Transform hitLocation;

    public static Camera cam;


    void Start()
    {
        //textWithLinks = Instantiate<TextWithLinkEvents>(textPrefab, transform);


        // textWithLinks.SpawnText("New test    Text <link=\"TempID\">[link]</link> Goes here");
        // clickableText.SpawnText("New clickable test    Text <link=\"TempID\">[link]</link> Goes here");

        print("text" + text.text);


        cam = Camera.main;
    }

    void Update()
    {




        //print("FindIntersectingLink " + TMP_TextUtilities.FindIntersectingLink(text, testtransform.position, Camera.main));
        //print("FindIntersectingCharacter " + TMP_TextUtilities.FindIntersectingCharacter(text, testtransform.position, Camera.main, true));
        //print("FindIntersectingCharacter " + TMP_TextUtilities.FindIntersectingCharacter(text, Input.mousePosition, Camera.main, true));

        Debug.DrawRay(testtransform.position, testtransform.forward.normalized * 10f, Color.red, 0f, false);

        Vector3 worldHit = new Vector3();
        //if (rayHitTextPlane(text, testtransform.position, cam.transform.forward, out worldHit))
        if (rayHitTextPlane(text, testtransform.position, testtransform.forward, out worldHit))
        {
            hitLocation.position = worldHit;
            //print("rayHitTextPlane " + worldHit);

            var screen = cam.WorldToScreenPoint(worldHit);
            //int link = TMP_TextUtilities.FindIntersectingLink(text, screen, cam);
            int link = TMP_TextUtilities.FindIntersectingCharacter(text, screen, cam, true);
            if (link != -1)
            {
                //string id = text.textInfo.linkInfo[link].GetLinkID();
                // handle link...

                print($"Found link {link} at {screen} on {text.name}");
            }

            // Or character:
            // int ch = TMP_TextUtilities.FindIntersectingCharacter(tmp, screen, cam, true);
        }
        else
        {
            hitLocation.position = Vector3.zero;
        }

    }


    /// <summary>
    /// Does the ray hit the text's plane within the bounding box, and if so where?
    /// </summary>
    /// <param name="tmp">The textMeshPro text</param>
    /// <param name="worldHit">Position in 3D space of the hit (convert to screen space next)</param>
    /// <returns></returns>
    public static bool rayHitTextPlane(TMP_Text tmp, Vector3 rayStartPosition, Vector3 rayDirection, out Vector3 worldHit)
    {
        // handRay: Ray from your controller/hand
        // cam: the camera that renders the text
        // tmp: your TextMeshPro (3D) component
        if (RayTMPPlane(rayStartPosition, rayDirection, tmp.transform, out worldHit, out _))
        {
            // Cheap early-out: only call TMP utilities if inside the overall text AABB
            if (InTMPBounds(tmp, worldHit))
            {
                return true;
                // var screen = cam.WorldToScreenPoint(worldHit);
                // int link = TMP_TextUtilities.FindIntersectingLink(tmp, screen, cam);
                // if (link != -1)
                // {
                //     string id = tmp.textInfo.linkInfo[link].GetLinkID();
                //     // handle link...
                // }

                // // Or character:
                // // int ch = TMP_TextUtilities.FindIntersectingCharacter(tmp, screen, cam, true);
            }
        }
        return false;
    }



    // RAYCAST MATH
    // Ray vs arbitrary plane defined by point+normal
    public static bool RayPlaneIntersect(
        Vector3 rayStartPosition, Vector3 rayDirection, Vector3 planePoint, Vector3 planeNormal,
        out float t, out Vector3 hit, float epsilon = 1e-6f)
    {
        t = 0f; hit = default;
        float denom = Vector3.Dot(planeNormal, rayDirection);
        if (Mathf.Abs(denom) < epsilon) return false; // parallel (or almost)

        t = Vector3.Dot(planePoint - rayStartPosition, planeNormal) / denom;
        if (t < 0f) return false; // plane is behind the ray origin

        hit = rayStartPosition + rayDirection * t;
        return true;
    }
    // Plane aligned with the camera (normal = cam.forward), passing through a world point
    public static bool RayCameraAlignedPlane(
        Vector3 rayStartPosition, Vector3 rayDirection, Camera cam, Vector3 worldPointOnPlane,
        out Vector3 hit, out float t)
    {
        return RayPlaneIntersect(rayStartPosition, rayDirection, worldPointOnPlane, cam.transform.forward, out t, out hit);
    }
    // Plane aligned with a TextMeshPro’s own surface (normal = text.forward), through its origin
    public static bool RayTMPPlane(
        Vector3 rayStartPosition, Vector3 rayDirection, Transform textTransform, out Vector3 hit, out float t)
    {
        return RayPlaneIntersect(rayStartPosition, rayDirection, textTransform.position, textTransform.forward, out t, out hit);
    }



    // Quick AABB test in text local space
    public static bool InTMPBounds(TMP_Text tmp, Vector3 worldPoint)
    {
        var b = tmp.textBounds; // local-space bounds of rendered text
        var local = tmp.transform.InverseTransformPoint(worldPoint);
        // Collapse Z to the bounds’ plane so Contains() works for “flat” text
        local.z = b.center.z;
        return b.Contains(local);
    }



    // void OnEnable()
    // {
    //     LinkBus.HoverEnter += OnHoverEnter;
    //     LinkBus.HoverExit += OnHoverExit;
    //     LinkBus.Click += OnClick;
    // }
    // void OnDisable()
    // {
    //     LinkBus.HoverEnter -= OnHoverEnter;
    //     LinkBus.HoverExit -= OnHoverExit;
    //     LinkBus.Click -= OnClick;
    // }

    // void OnHoverEnter(LinkBus.LinkContext ctx)
    // {
    //     // e.g., highlight the word
    //     Debug.Log($"Hover ENTER {ctx.id} on {ctx.owner.name}");
    // }

    // void OnHoverExit(LinkBus.LinkContext ctx)
    // {
    //     // e.g., remove highlight
    //     Debug.Log($"Hover EXIT {ctx.id}");
    // }

    // void OnClick(LinkBus.LinkContext ctx)
    // {
    //     // Route by prefix like an input action
    //     // if (ctx.id.StartsWith("open:")) OpenThing(ctx.id.Substring(5), ctx.owner);
    //     // else if (ctx.id == "equip:sword") EquipSword();

    //     Debug.Log($"Click {ctx.id} on {ctx.owner.name} at {ctx.screenPos}");
    // }



}
