using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;


public static class UIShortcuts // a non-generic static class so these extensions work
{
    // Smaller/Easier to type UI button press checks
    public static bool Clicked(this Component self, string id)
=> UI.Clicked(self.gameObject, id);
}

public class UI : MonoBehaviour
{
    public TMP_Text text;
    public Transform debugTransform;
    public Transform debugHitLocation;

    public static Camera cam;


    public static Dictionary<GameObject, Dictionary<string, Link>> Links;
    /// <summary>
    /// All the texts containing all the links, for checking which links are moused over (and retrieving the links)
    /// </summary>
    public static Dictionary<TMP_Text, List<Link>> textMeshPros;

    public class Link
    {
        public string id;
        public TMP_Text text;
        //public TMP_LinkInfo linkInfo => textMeshPro.textInfo.linkInfo.Find(x => x.GetLinkID() == id); // lazy lookup
        //public TMP_LinkInfo linkInfo;
        public int linkIndex;
        public GameObject owner;

        // status
        public bool hovered;
        public bool oldHovered;

        public bool clicked;
        public bool oldClicked;

        /// <summary>
        /// Modify to override link colors
        /// </summary>
        public LinkColors linkColors = null;

        //public static Dictionary<TMP_Text, 


        //public Link(string _id, TMP_Text _textMeshPro, GameObject _owner, TMP_LinkInfo _LinkInfo)
        public Link(string _id, TMP_Text _textMeshPro, GameObject _owner, int _linkIndex)
        {
            id = _id;
            text = _textMeshPro;
            owner = _owner;
            linkIndex = _linkIndex;
        }

        public void updateColors()
        {
            updateLinkColors(this);
        }

        // TODO custom link/click colors for different links (to show which is selected etc)
    }

    [System.Serializable]
    public class LinkColors
    {
        //[SerializeField]
        public Color normal = new Color(0.3f, 0.6f, 1f); // nice blue
        //[SerializeField]
        public Color hovered = new Color(0.5f, 0.8f, 1f); // lighter blue
        //Color clicked = new Color(0.1f, 0.4f, 0.8f); // dark blue
        //[SerializeField]
        public Color clicked = new Color(1, 1, 1);
        //[SerializeField]
        public Color held = new Color(1, 1, 0);   // yellow
    }

    public LinkColors defaultLinkColors;



    /// <summary>
    /// Global instance of this singleton script
    /// </summary>
    public static UI instance;

    void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("UI instance already exists! This should only be one per scene.");
            return;
        }
        instance = this;

        cam = Camera.main;

        Links = new Dictionary<GameObject, Dictionary<string, Link>>();
        textMeshPros = new Dictionary<TMP_Text, List<Link>>();
    }

    public static bool Clicked(GameObject gameObject, string id)
    {
        //print($"Clicked {id} on {gameObject.name}");
        /* your lookup */
        return false;
    }



    /// <summary>
    /// Add links to global dictionary, update text colors.
    /// (Note: Links with no ID, like <link>myLink</link>, will substitute in the myLink text as the ID)
    /// </summary>
    public static void addLinks(TMP_Text text)
    {
        addLinks(text, text.transform.parent.gameObject);
    }

    // TODO a remove links thing; remove from Links and textMeshPros

    /// <summary>
    /// Add links to global dictionary, update text colors.
    /// (Note: Links with no ID, like <link>myLink</link>, will substitute in the myLink text as the ID)
    /// </summary>
    /// <param name="GO">Override which gameobject is used for referencing</param>
    public static void addLinks(TMP_Text text, GameObject GO)
    {
        if (text == null) { Debug.LogWarning("addLinks: text is null"); return; }

        text.ForceMeshUpdate();
        var ti = text.textInfo;
        if (ti.linkCount == 0) { Debug.LogWarning("No <link> tags found in " + text.text); return; }

        {   // save refs
            if (!textMeshPros.ContainsKey(text))
                textMeshPros.Add(text, new List<Link>());
            else
                textMeshPros[text].Clear();
        }

        // Fill in blank link IDs
        string s = text.text;
        string StripTags(string x) => Regex.Replace(x, @"<[^>]+>", "");     // remove TMP/HTML tags
        string EscapeForAttr(string x) => x.Replace("\"", "&quot;");        // basic quote escape
        // Match <link> ... </link> (no attributes), case-insensitive, multiline
        text.text = Regex.Replace(s, @"(?is)<link>(.*?)</link>", m =>
        {
            string inner = m.Groups[1].Value;                 // keep original rich-text
            string visible = StripTags(inner).Trim();         // visible text only
            string id = EscapeForAttr(string.IsNullOrEmpty(visible) ? "link" : visible);
            return $"<link=\"{id}\">{inner}</link>";
        });
        text.ForceMeshUpdate();

        // Add/Update dictionary links
        Dictionary<string, Link> links = new Dictionary<string, Link>();
        // if (Links.ContainsKey(GO))
        //     links = Links[GO];
        // else
        // {
        //     links = new Dictionary<string, link>();
        //     Links.Add(GO, links);
        // }

        for (int i = 0; i < ti.linkCount; i++)
        {
            TMP_LinkInfo li = ti.linkInfo[i];
            string id = li.GetLinkID();
            if (string.IsNullOrEmpty(id)) continue; // skip empty IDs

            links[id] = new Link(id, text, GO, i);
            textMeshPros[text].Add(links[id]);

            print($"UI.addLinks: Added link '{id}' to {GO.name} with text '{li.GetLinkText()}'");
        }


        // Add links to Links
        if (Links.ContainsKey(GO))
        {
            Links[GO] = links; // update existing
        }
        else
        {
            Links.Add(GO, links); // add new
        }

        // update colors
        foreach (var kvp in links)
        {
            var link = kvp.Value;
            updateLinkColors(link, false);
        }
        text.ForceMeshUpdate();

        // // Iterate backwards so earlier indices stay valid
        // for (int i = ti.linkCount - 1; i >= 0; i--)
        // {
        //     var li = ti.linkInfo[i];
        //     string id = li.GetLinkID();
        //     string body = li.GetLinkText();

        // }
    }




    static readonly Regex RxOpen = new(@"(?i)<link\b[^>]*>");
    static readonly Regex RxClose = new(@"(?i)</link>");
    static readonly Regex RxColorAtStart = new(@"(?i)\G<color\b[^>]*>");

    static bool OnlyWhitespace(string s, int start, int end)
    {
        for (int i = start; i < end; i++) if (!char.IsWhiteSpace(s[i])) return false;
        return true;
    }

    public static void updateLinkColors(Link link, bool ForceMeshUpdate = true)
    {
        var text = link.text;
        text.ForceMeshUpdate();
        string s = text.text;

        var ti = text.textInfo;
        var opens = RxOpen.Matches(s);

        var li = ti.linkInfo[link.linkIndex];
        var open = opens[link.linkIndex];

        int openEnd = open.Index + open.Length;     // content start
        var close = RxClose.Match(s, openEnd);
        if (!close.Success) return;

        int contentStart = openEnd;
        int contentEnd = close.Index;

        //string hex = ColorUtility.ToHtmlStringRGBA(new Color(0.3f, 0.6f, 1f));
        // Use default link colors if not overridden
        LinkColors linkColors = link.linkColors ?? instance.defaultLinkColors;

        Color color = linkColors.normal;
        if (link.hovered)
            color = linkColors.hovered;
        else if (link.clicked)
            color = linkColors.clicked;

        string hex = ColorUtility.ToHtmlStringRGBA(color);

        // Case A: already wrapped exactly by <color>...</color> at the link boundaries
        var mColorOpen = RxColorAtStart.Match(s, contentStart);
        if (mColorOpen.Success)
        {
            // look for a </color> right before </link> (allow trailing whitespace)
            int lastClose = s.LastIndexOf("</color>", contentEnd, contentEnd - contentStart,
                                          System.StringComparison.OrdinalIgnoreCase);
            bool wrapsAll = lastClose >= 0 && OnlyWhitespace(s, lastClose + 8, contentEnd);
            if (wrapsAll)
            {
                // Replace just the opening color tag
                s = s.Remove(mColorOpen.Index, mColorOpen.Length)
                     .Insert(mColorOpen.Index, $"<color=#{hex}>");
            }
            else
            {
                // Not a full wrapper—fall through to add a new outer pair
                // (no removal; we’ll just add an outer color)
                s = s.Insert(contentEnd, "</color>");          // insert end first
                s = s.Insert(contentStart, $"<color=#{hex}>"); // then start
            }
        }
        else
        {
            // Case B: no color at start—add a new wrapper
            s = s.Insert(contentEnd, "</color>");
            s = s.Insert(contentStart, $"<color=#{hex}>");
        }

        text.text = s;
        if (ForceMeshUpdate) text.ForceMeshUpdate();
    }




    void Update()
    {
        // reset all links
        foreach (var kvp in Links)   // loop all links
        {
            foreach (var link in kvp.Value.Values)
            {
                //print($"UI.Update: Reset link {link.id} on {kvp.Key.name} " + link.hovered);
                if (link.text == null) continue;

                link.oldClicked = link.clicked;
                link.oldHovered = link.hovered;

                link.hovered = false;
                link.clicked = false;

                if (link.oldClicked != link.clicked || link.oldHovered != link.hovered)
                    link.updateColors();// update colors if changed
            }
        }



        //print("FindIntersectingLink " + TMP_TextUtilities.FindIntersectingLink(text, testtransform.position, Camera.main));
        //print("FindIntersectingCharacter " + TMP_TextUtilities.FindIntersectingCharacter(text, testtransform.position, Camera.main, true));
        //print("FindIntersectingCharacter " + TMP_TextUtilities.FindIntersectingCharacter(text, Input.mousePosition, Camera.main, true));

        if (debugTransform)
        {
            Debug.DrawRay(debugTransform.position, debugTransform.forward.normalized * 10f, Color.red, 0f, false);

            Vector3 worldHit = new Vector3();
            //if (rayHitTextPlane(text, testtransform.position, cam.transform.forward, out worldHit))
            if (rayHitTextPlane(text, debugTransform.position, debugTransform.forward, out worldHit))
            {
                if (debugHitLocation)
                    debugHitLocation.position = worldHit;
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
                debugHitLocation.position = Vector3.zero;
            }
        }


        // Check for link touches and clicks
        // Find the best link selection; the most centered one or nearest the camera    // TODO. (Use the depth from cam.WorldToScreenPoint?)
        Link touchedLink = null;
        foreach (var text in textMeshPros)
        {
            // go through all input devices
            int index = CheckMouseOverLink(text.Key, cam);
            if (index != -1)
                touchedLink = text.Value[index];

            index = CheckTouchesOverLinks(text.Key, cam);
            if (index != -1)
                touchedLink = text.Value[index];
        }


        // foreach (var kvp in Links)   // loop all links
        // {
        //     //var GO = kvp.Key;
        //     foreach (var linkKvp in kvp.Value)
        //     {
        //         var link = linkKvp.Value;
        //         if (link.text == null) continue; // skip if text is missing

        //         if (CheckMouseOverLink(link.text, cam))
        //             touchedLink = link;



        //         // TODO other inputs; VR pointers, VR gaze
        //     }
        // }
        if (touchedLink != null)
        {
            //print($"Mouse over link: {touchedLink.id}");
            if (!touchedLink.hovered)
            {
                touchedLink.hovered = true;
                //updateLinkColors(touchedLink);
                touchedLink.updateColors();
            }
        }


    }


    // INPUT DEVICES

    // --- Mouse ---
    public static int CheckMouseOverLink(TMP_Text tmp, Camera cam)
    {
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!rayHitTextPlane(tmp, ray.origin, ray.direction, out var worldHit)) return -1;

        var screen = cam.WorldToScreenPoint(worldHit);
        int link = TMP_TextUtilities.FindIntersectingLink(tmp, screen, cam);
        if (link != -1)
        {
            string id = tmp.textInfo.linkInfo[link].GetLinkID();
            //Debug.Log($"Mouse over link: {id}");
            return link;
        }
        return -1;
    }

    // --- Touch (Android/iOS) ---
    public static int CheckTouchesOverLinks(TMP_Text tmp, Camera cam)
    {
        foreach (var t in Input.touches)
        {
            var ray = cam.ScreenPointToRay(t.position);
            if (!rayHitTextPlane(tmp, ray.origin, ray.direction, out var worldHit)) continue;

            var screen = cam.WorldToScreenPoint(worldHit);
            int link = TMP_TextUtilities.FindIntersectingLink(tmp, screen, cam);
            if (link == -1)
                continue;
            else
                return link;    // TODO no multitouch support here!

            // string id = tmp.textInfo.linkInfo[link].GetLinkID();
            // switch (t.phase)
            // {
            //     case TouchPhase.Began:      Debug.Log($"Touch BEGAN on {id}"); break;
            //     case TouchPhase.Moved:
            //     case TouchPhase.Stationary: Debug.Log($"Touch HELD on {id}");  break;
            //     case TouchPhase.Ended:
            //     case TouchPhase.Canceled:   Debug.Log($"Touch ENDED on {id}"); break;
            // }
        }
        return -1;
    }






    // RAYCAST MATH

    /// <summary>
    /// Does the ray hit the text's plane within the bounding box, and if so where?
    /// </summary>
    /// <param name="tmp">The textMeshPro text</param>
    /// <param name="worldHit">Position in 3D space of the hit (convert to screen space next)</param>
    /// <returns></returns>
    public static bool rayHitTextPlane(TMP_Text tmp, Vector3 rayStartPosition, Vector3 rayDirection, out Vector3 worldHit)
    {
        if (RayTMPPlane(rayStartPosition, rayDirection, tmp.transform, out worldHit, out _))
        {
            // Cheap early-out: only call TMP utilities if inside the overall text AABB
            if (InTMPBounds(tmp, worldHit))
            {
                return true;
            }
        }
        return false;
    }


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



}
