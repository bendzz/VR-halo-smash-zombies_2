using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(TextMeshPro))]
[RequireComponent(typeof(BoxCollider))]
public class ClickableTMPText : MonoBehaviour, IPointerMoveHandler, IPointerClickHandler
{
    [Header("Padding added around the text bounds (m)")]
    public Vector2 padding = new(0.01f, 0.006f);

    TextMeshPro _tmp;
    BoxCollider _col;
    int _hoveredLink = -1;

    void Awake()
    {
        _tmp = GetComponent<TextMeshPro>();
        _col = GetComponent<BoxCollider>();
        EnsureEventSystem();
        EnsurePhysicsRaycaster();
    }

    void OnEnable()
    {
        //TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTMPChanged);  // crashes it!
        RefitCollider();
    }
    void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTMPChanged);
    }

    void OnTMPChanged(object obj)
    {
        if (obj == _tmp) RefitCollider();
    }

    // Call after you change .text
    public void SpawnText(string content)
    {
        _tmp.text = content;
        _tmp.ForceMeshUpdate();
        RefitCollider();
    }

    void RefitCollider()
    {
        _tmp.ForceMeshUpdate();

        // Text bounds are in local space
        var b = _tmp.textBounds;
        var size = b.size;
        size.x += padding.x * 2f;
        size.y += padding.y * 2f;
        if (size.z < 0.01f) size.z = 0.01f; // thin but “hittable”

        _col.center = b.center;
        _col.size = size;
        _col.isTrigger = true; // optional
    }

    public void OnPointerMove(PointerEventData e)
    {
        var cam = e.enterEventCamera ?? e.pressEventCamera ?? Camera.main;
        int i = TMP_TextUtilities.FindIntersectingLink(_tmp, e.position, cam);
        if (i != _hoveredLink)
        {
            if (_hoveredLink != -1) Debug.Log($"Hover EXIT {_tmp.textInfo.linkInfo[_hoveredLink].GetLinkID()}");
            _hoveredLink = i;
            if (_hoveredLink != -1) Debug.Log($"Hover ENTER {_tmp.textInfo.linkInfo[_hoveredLink].GetLinkID()}");
        }
    }

    public void OnPointerClick(PointerEventData e)
    {
        var cam = e.pressEventCamera ?? Camera.main;
        int i = TMP_TextUtilities.FindIntersectingLink(_tmp, e.position, cam);
        if (i != -1)
        {
            var id = _tmp.textInfo.linkInfo[i].GetLinkID();
            Debug.Log($"CLICK {id} on {gameObject.name}");
            // TODO: broadcast or invoke your action here
        }
    }

    static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;

        var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }

    static void EnsurePhysicsRaycaster()
    {
        var cam = Camera.main;
        if (!cam) return;
        if (!cam.TryGetComponent<UnityEngine.EventSystems.PhysicsRaycaster>(out _))
            cam.gameObject.AddComponent<UnityEngine.EventSystems.PhysicsRaycaster>();
    }
}
