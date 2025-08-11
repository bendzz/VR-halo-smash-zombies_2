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


        //public Link(string _id, TMP_Text _textMeshPro, GameObject _owner, TMP_LinkInfo _LinkInfo)
        public Link(string _id, TMP_Text _textMeshPro, GameObject _owner, int _linkIndex)
        {
            id = _id;
            text = _textMeshPro;
            owner = _owner;
            linkIndex = _linkIndex;
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
        Color color = UI.instance.defaultLinkColors.normal;
        if (link.linkColors != null)
        {
            color = link.linkColors.normal;
        }
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


    // static Regex RxOpen = new(@"(?i)<link\b[^>]*>");
    // static Regex RxClose = new(@"(?i)</link>");
    // static string StripTags(string x) => Regex.Replace(x, @"<[^>]+>", "");
    // static string EscAttr(string x) => x.Replace("\"", "&quot;");
    // public static void updateLinkColors(Link link, bool ForceMeshUpdate = true)
    // {

    //     // Cache all opening-tag matches once
    //     string s = link.text.text;
    //     var opens = RxOpen.Matches(s);

    //     var ti = link.text.textInfo;


    //     {
    //         var li = ti.linkInfo[link.linkIndex];
    //         var open = opens[link.linkIndex];

    //         int openEnd = open.Index + open.Length;    // where inner content starts (for color insert)

    //         // // If ID is blank, set it to the link body (visible text)
    //         // if (string.IsNullOrEmpty(li.GetLinkID()))
    //         // {
    //         //     string bodyVis = StripTags(li.GetLinkText()).Trim();
    //         //     if (string.IsNullOrEmpty(bodyVis)) bodyVis = "link";
    //         //     string newOpen = $"<link=\"{EscAttr(bodyVis)}\">";

    //         //     s = s.Remove(open.Index, open.Length)
    //         //         .Insert(open.Index, newOpen);

    //         //     openEnd = open.Index + newOpen.Length; // opener length changed
    //         // }

    //         // color the links
    //         {
    //             string hex = ColorUtility.ToHtmlStringRGBA(new Color(0.3f, 0.6f, 1f));
    //             s = s.Insert(openEnd, $"<color=#{hex}>");

    //             // find this link's closing tag AFTER the opener we just wrote
    //             var close = new Regex(RxClose.ToString(), RegexOptions.IgnoreCase).Match(s, openEnd);
    //             if (close.Success)
    //                 s = s.Insert(close.Index, "</color>");
    //         }
    //     }




    //     link.text.text = s;
    //     if (ForceMeshUpdate)
    //         link.text.ForceMeshUpdate();
    // }



    // /// <summary>
    // /// Add links to global dictionary, update text colors.
    // /// (Note: Links with no ID, like <link>myLink</link>, will substitute in the myLink text as the ID)
    // /// </summary>
    // /// <param name="text"></param>
    // public static void updateLinkColors(TMP_Text text)
    // {

    //     if (text == null) { Debug.LogWarning("addLinks: text is null"); return; }


    //     text.ForceMeshUpdate();
    //     var ti = text.textInfo;
    //     if (ti.linkCount == 0) { Debug.LogWarning("No <link> tags found in " + text.text); return; }




    //     Regex RxOpen = new(@"(?i)<link\b[^>]*>");
    //     Regex RxClose = new(@"(?i)</link>");
    //     string StripTags(string x) => Regex.Replace(x, @"<[^>]+>", "");
    //     string EscAttr(string x) => x.Replace("\"", "&quot;");

    //     // Cache all opening-tag matches once
    //     string s = text.text;
    //     var opens = RxOpen.Matches(s);


    //     // Iterate backwards so earlier indices stay valid
    //     for (int i = ti.linkCount - 1; i >= 0; i--)
    //     {
    //         var li = ti.linkInfo[i];
    //         var open = opens[i];

    //         int openEnd = open.Index + open.Length;    // where inner content starts (for color insert)

    //         // If ID is blank, set it to the link body (visible text)
    //         if (string.IsNullOrEmpty(li.GetLinkID()))
    //         {
    //             string bodyVis = StripTags(li.GetLinkText()).Trim();
    //             if (string.IsNullOrEmpty(bodyVis)) bodyVis = "link";
    //             string newOpen = $"<link=\"{EscAttr(bodyVis)}\">";

    //             s = s.Remove(open.Index, open.Length)
    //                 .Insert(open.Index, newOpen);

    //             openEnd = open.Index + newOpen.Length; // opener length changed
    //         }

    //         // color the links
    //         {
    //             string hex = ColorUtility.ToHtmlStringRGBA(new Color(0.3f, 0.6f, 1f));
    //             s = s.Insert(openEnd, $"<color=#{hex}>");

    //             // find this link's closing tag AFTER the opener we just wrote
    //             var close = new Regex(RxClose.ToString(), RegexOptions.IgnoreCase).Match(s, openEnd);
    //             if (close.Success)
    //                 s = s.Insert(close.Index, "</color>");
    //         }
    //     }

    //     text.text = s;
    //     text.ForceMeshUpdate();
    // }

    //     text.ForceMeshUpdate();                       // ensure textInfo is current
    //     var ti = text.textInfo;
    //     //var fullText = text.text;
    //     int count = ti.linkCount;

    //     if (count == 0) { Debug.Log("No <link> tags found in " + text.text); return; }

    //     for (int i = 0; i < count; i++)
    //     {
    //         TMP_LinkInfo li = ti.linkInfo[i];
    //         string id = li.GetLinkID();
    //         string body = li.GetLinkText();
    //         // Debug.Log($"Link[{i}] id='{id}' text=\"{body}\" " +
    //         //           $"firstChar={li.linkTextfirstCharacterIndex} len={li.linkTextLength}");

    //         if (id == "")
    //         {
    //             print($"Link[{i}] has no ID, using text as ID: \"{body}\"");
    //             //text.text = text.text.Replace(body, $"<link={body}>{body}</link>");
    //         }



    //         var col = new Color(0.3f, 0.6f, 1f); // nice blue
    //         string hex = ColorUtility.ToHtmlStringRGBA(col);



    //     }

    //         string s = text.text;
    //                 // Add <color=...> right after each <link ...>
    //         s = Regex.Replace(s, @"(?i)(<link\b[^>]*>)", $"$1<color=#{hex}>");

    //         // Close the color before </link>
    //         s = Regex.Replace(s, @"(?i)</link>", "</color></link>");
    //         text.ForceMeshUpdate();
    //         text.text = s;
    // }



    void Update()
    {


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

    }


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
