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

        // STATUS
        public bool hovered;
        public bool oldHovered;

        // Clicked?
        /// <summary>
        /// Like the button was just pressed this frame
        /// </summary>
        public bool clickStart;
        public bool clickHeld;
        public bool oldHeld;
        public bool clickReleased;



        /// <summary>
        /// Modify to override link colors
        /// </summary>
        public LinkColors colors = null;

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
        public Color normal = new Color(0.3f, 0.6f, 1f); // nice blue
        public Color hovered = new Color(0.5f, 0.8f, 1f); // lighter blue
        //Color clicked = new Color(0.1f, 0.4f, 0.8f); // dark blue
        public Color held = new Color(1, 1, 0);   // yellow
        public Color released = new Color(1, 1, 1);

        public LinkColors() { }
        public LinkColors(Color normal, Color hovered, Color held, Color released)
        {
            this.normal = normal;
            this.hovered = hovered;
            this.held = held;
            this.released = released;
        }

    }

    public LinkColors defaultLinkColors;
    public LinkColors selectedOption = new LinkColors(Color.hotPink, Color.orange, Color.yellow, Color.red);


    /// <summary>
    /// Pick one from a list of links; it will be highlighted, the rest won't. Can query to see the selection.
    /// </summary>
    public class PickOneOption
    {
        public static List<PickOneOption> AllPickOneOptions = new List<PickOneOption>();

        public List<Link> links = new List<Link>();
        public int selection = 0;

        // public void AddLink(Link link)
        // { links.Add(link); }

        /// <summary>
        /// Run every frame after links are updated
        /// </summary>
        public static void updateAll()
        {
            foreach (var pickOne in AllPickOneOptions)
            {
                pickOne.updateLinks();
            }
        }
        void updateLinks()
        {
            for (int i = 0; i < links.Count; i++)
            {
                bool dirty = false;
                if (links[i].clickReleased)
                {
                    selection = i;
                    //print($"PickOneOption: Selected link {links[i].id} at index {i}");
                }
                if (i == selection)
                {
                    if (links[i].colors == null)
                        dirty = true;
                    links[i].colors = UI.instance.selectedOption; // highlight selected
                }
                else
                {
                    if (links[i].colors != null)
                        dirty = true;
                    links[i].colors = null; // unhighlight others
                }
                if (dirty)
                    links[i].updateColors();

                //links[i].colors = (i == selection) ? UI.instance.selectedOption : null;
            }
        }


        // public PickOneOption(List<Link> _links)
        // { links = _links; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameObject">The gameobject all the links are associated with</param>
        /// <param name="_linkIDs">The linkIDs to search from the global dictionary</param>
        public PickOneOption(GameObject gameObject, List<string> _linkIDs)
        {
            links = new List<Link>();
            foreach (var id in _linkIDs)
            {
                if (UI.Links.TryGetValue(gameObject, out var linkDict) && linkDict.TryGetValue(id, out var link))
                {
                    links.Add(link);
                    print($"PickOneOption: Added link '{id}' from {gameObject.name}");
                }
                else
                {
                    Debug.LogWarning($"PickOneOption: Link '{id}' not found on {gameObject.name}");
                }
            }
            AllPickOneOptions.Add(this);
        }
    }


    // /// <summary>
    // /// The link that's currently being clicked and held
    // /// </summary>
    // public static Link heldLink = null;


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

    /// <summary>
    /// Check if this link was just clickReleased
    /// </summary>
    /// <param name="gameObject"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public static bool Clicked(GameObject gameObject, string id)
    {
        //print($"Clicked {id} on {gameObject.name}");

        if (Links.TryGetValue(gameObject, out var linkDict) && linkDict.TryGetValue(id, out var link))
        {
            if (link.clickReleased)
            {
                return true; // clicked this frame
            }
        }
        else
        {
            Debug.LogError($"UI.Clicked: Link '{id}' not found on {gameObject.name}");
        }

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

            //print($"UI.addLinks: Added link '{id}' to {GO.name} with text '{li.GetLinkText()}'");
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

        // Pick the color
        //string hex = ColorUtility.ToHtmlStringRGBA(new Color(0.3f, 0.6f, 1f));
        // Use default link colors if not overridden
        LinkColors linkColors = link.colors ?? instance.defaultLinkColors;

        Color color = linkColors.normal;
        if (link.hovered)
            color = linkColors.hovered;
        if (link.clickHeld)
            color = linkColors.held;
        if (link.clickReleased)
            color = linkColors.released;

        string hex = ColorUtility.ToHtmlStringRGBA(color);


        // Apply color
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

                bool dirty = false;
                if (link.clickReleased)
                    dirty = true;
                if (link.oldHeld != link.clickHeld)
                    dirty = true;
                if (link.oldHovered != link.hovered)
                    dirty = true;


                link.clickStart = false;

                link.oldHeld = link.clickHeld;
                link.clickHeld = false;

                link.oldHovered = link.hovered;
                link.hovered = false;

                link.clickReleased = false;


                //if (link.oldClicked != link.clicked || link.oldHovered != link.hovered)
                if (dirty)
                    link.updateColors();
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
        Touch touch = default;    // android touches
        foreach (var text in textMeshPros)
        {
            // go through all input devices
            int index = CheckMouseOverLink(text.Key, cam);
            if (index != -1)
                touchedLink = text.Value[index];

            index = CheckTouchesOverLinks(text.Key, cam, out touch);
            if (index != -1)
                touchedLink = text.Value[index];

            // TODO other inputs; VR pointers, VR gaze
        }

        //Time.timeScale = .001f;


        if (touchedLink != null)
        {
            // Mouseover
            if (!touchedLink.hovered)
            {
                touchedLink.hovered = true;
                touchedLink.updateColors();
            }

            // Click held?
            //if (Input.GetMouseButtonDown(0) || (touch.phase == TouchPhase.Ended))
            if (Input.GetMouseButton(0) || (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary))
            {
                //print($"UI.Update: Clicked link {touchedLink.id} on {touchedLink.owner.name} " + Input.GetMouseButton(0));
                if (!touchedLink.oldHeld)
                    touchedLink.clickStart = true; // clicked this frame

                touchedLink.clickHeld = true;
                touchedLink.updateColors();
                //heldLink = touchedLink;
            }

            // Click released
            if (Input.GetMouseButtonUp(0) || (touch.phase == TouchPhase.Ended))
            {
                print($"UI.Update: Released link {touchedLink.id} on {touchedLink.owner.name}");
                touchedLink.clickHeld = false;
                touchedLink.clickReleased = true; // released this frame
                touchedLink.updateColors();
            }
            // else
            // {
            //     touchedLink.clickReleased = false;
            // }
        }

        PickOneOption.updateAll();
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
    public static int CheckTouchesOverLinks(TMP_Text tmp, Camera cam, out Touch touch)
    {
        touch = default;
        foreach (var t in Input.touches)
        {
            var ray = cam.ScreenPointToRay(t.position);
            if (!rayHitTextPlane(tmp, ray.origin, ray.direction, out var worldHit)) continue;

            var screen = cam.WorldToScreenPoint(worldHit);
            int link = TMP_TextUtilities.FindIntersectingLink(tmp, screen, cam);
            if (link == -1)
                continue;
            else
            {
                touch = t;
                return link;    // TODO no multitouch support here!
            }

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
