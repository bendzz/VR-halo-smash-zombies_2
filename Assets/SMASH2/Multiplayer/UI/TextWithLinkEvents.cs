using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using System;
//using UnityEngine;


public class TextWithLinkEvents : MonoBehaviour, IPointerClickHandler, IPointerMoveHandler
{
    [Header("Assign a 3D TextMeshPro prefab (MeshRenderer variant)")]
    public TextMeshPro textPrefab;

    TMP_Text _text;                 // instance
    int _hovered = -1;              // current hovered link index, -1 = none

    // Call this to create/replace the text under this GameObject
    public void SpawnText(string content)
    {
        if (_text != null) Destroy(_text.gameObject);
        _text = Instantiate(textPrefab, transform);
        _text.text = content;
        _text.enableWordWrapping = true;
        _text.ForceMeshUpdate();
    }

    // Mouse / touch move -> per-link hover detection
    public void OnPointerMove(PointerEventData e)
    {
        print($"OnPointerMove {e.position} on {_text.name}");
        if (_text == null) return;
        var cam = e.pressEventCamera ?? e.enterEventCamera ?? Camera.main;
        ProbeAtScreenPosition(e.position, cam);
    }

    // Mouse / touch click
    public void OnPointerClick(PointerEventData e)
    {
        print($"OnPointerClick {e.position} on {_text.name}");
        if (_text == null) return;
        var cam = e.pressEventCamera ?? e.enterEventCamera ?? Camera.main;
        int link = TMP_TextUtilities.FindIntersectingLink(_text, e.position, cam);
        if (link != -1) LinkBus.RaiseClick(BuildContext(link, e.position, cam));
    }

    // --- Optional: call this from a VR reticle/controller script each frame ---
    public void ProbeAtScreenPosition(Vector2 screenPos, Camera cam)
    {
        int link = TMP_TextUtilities.FindIntersectingLink(_text, screenPos, cam);

        if (link != _hovered)
        {
            if (_hovered != -1) LinkBus.RaiseHoverExit(BuildContext(_hovered, screenPos, cam));
            _hovered = link;
            if (_hovered != -1) LinkBus.RaiseHoverEnter(BuildContext(_hovered, screenPos, cam));
        }
    }

    LinkBus.LinkContext BuildContext(int linkIndex, Vector2 screenPos, Camera cam)
    {
        var li = _text.textInfo.linkInfo[linkIndex];
        // Approx world-space center of the link by scanning visible characters in the span
        Vector3 min = new(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new(float.MinValue, float.MinValue, float.MinValue);

        for (int c = 0; c < li.linkTextLength; c++)
        {
            int charIndex = li.linkTextfirstCharacterIndex + c;
            if (charIndex < 0 || charIndex >= _text.textInfo.characterCount) continue;
            var ch = _text.textInfo.characterInfo[charIndex];
            if (!ch.isVisible) continue;

            var bl = _text.transform.TransformPoint(ch.bottomLeft);
            var tr = _text.transform.TransformPoint(ch.topRight);
            min = Vector3.Min(min, bl);
            max = Vector3.Max(max, tr);
        }

        var center = (min.x <= max.x) ? (min + max) * 0.5f : transform.position; // fallback

        return new LinkBus.LinkContext
        {
            id = li.GetLinkID(),
            owner = gameObject,
            linkIndex = linkIndex,
            screenPos = screenPos,
            worldCenter = center
        };
    }
}






public static class LinkBus
{
    public struct LinkContext
    {
        public string id;           // <link="id">
        public GameObject owner;    // the Text object
        public int linkIndex;       // TMP link index
        public Vector2 screenPos;   // pointer position
        public Vector3 worldCenter; // approx center of the link in world space
    }

    public static event Action<LinkContext> HoverEnter;
    public static event Action<LinkContext> HoverExit;
    public static event Action<LinkContext> Click;

    public static void RaiseHoverEnter(in LinkContext ctx) => HoverEnter?.Invoke(ctx);
    public static void RaiseHoverExit(in LinkContext ctx) => HoverExit?.Invoke(ctx);
    public static void RaiseClick(in LinkContext ctx) => Click?.Invoke(ctx);
}
