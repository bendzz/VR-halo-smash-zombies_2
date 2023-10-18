using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class DamageNumbers : MonoBehaviour
{
    public static DamageNumbers instance;

    //public Camera cam;
    //public static Camera camInstance;


    // Start is called before the first frame update
    void Start()
    {
        if (instance != null)
            Debug.LogError("More than one DamageNumbers in scene!");
        instance = this;

        //if (cam == null)
        //    cam = Camera.main;
        //camInstance = cam;
    }

    // update is called once per frame
    void Update()
    {
        Card.UpdateAll();
    }


    /// <summary>
    /// Damage cards, to display numbers in the air
    /// </summary>
    public class Card{
        // object pool
        public static List<Card> liveCards;
        public static Stack<Card> deadCards;

        //public Transform rectTransform;
        public RectTransform rectTransform; // because text's rectTransform replaces/deletes the normal Transform
        public GameObject gameObject;
        public TextMeshPro info;
        public Vector3 velocity = Vector3.zero;
        public Vector3 gravity = Vector3.zero;
        /// <summary>
        /// If .5, velocity will halve in 1 second
        /// </summary>
        public float drag = 0;

        //public TextMeshPro text;
        public float timeLeft = 0;


        public static Card newCardFormatted(Transform hitChar, float damage)
        {
            float m = 20;   // max damage for color coding
            float mq = m / 4;
            float d = damage;

            Color color = new Color(Mathf.Clamp01(3 - d / mq) + Mathf.Clamp01((d / mq - 4) / 4), Mathf.Clamp01(2 - d / mq), Mathf.Clamp01(1 - d / mq) + Mathf.Clamp01(d / mq - 2.8f), 1);

            //Vector3 offset = hitChar.right * (Random.value - .5f) * 2 + Vector3.up;
            //Vector3 offset = camInstance.transform.right * (Random.value - .5f) * 2 + Vector3.up;
            //Vector3 offset = camInstance.transform.right * (Random.value - .5f) * 2 + Vector3.up * (Random.value - .5f) * 2;
            Vector3 offset = Random.onUnitSphere / 1.5f;
            //offset = offset.normalized * .3f + offset;

            Card card = newCard(hitChar.position + offset * .3f, damage.ToString("F1"), 1.5f, .3f + damage / 2f, color);

            card.velocity = offset + Vector3.up;
            card.drag = 4f;
            card.gravity = Physics.gravity / 10;

            //card.rectTransform.LookAt(camInstance.transform);
            if (Camera.main != null)
                card.rectTransform.LookAt(Camera.main.transform);
            card.rectTransform.Rotate(0, 180, 0);

            return card;
        }

        /// <summary>
        /// Create a new Card (likely an old one from its object pool)
        /// </summary>
        public static Card newCard(Vector3 startPoint, string text, float timeLeft, float scale, Color color)
        {
            if (liveCards == null)
                liveCards = new List<Card>();
            if (deadCards == null)
                deadCards = new Stack<Card>();

            Card card;
            if (deadCards.Count > 0)
            {
                card = deadCards.Pop(); 
                card.gameObject.SetActive(true);
            }
            else
            {
                card = new Card();
                card.gameObject = new GameObject("DamageCard");
                //card.rectTransform = card.gameObject.rectTransform;
                //card.rectTransform = card.gameObject.transform;

                card.info = card.gameObject.AddComponent<TextMeshPro>();

            }

            // text
            {
                //GameObject go = card.rectTransform.gameObject;
                TextMeshPro info = card.info;

                card.rectTransform = card.gameObject.GetComponent<RectTransform>();
                //go.GetComponent<RectTransform>().sizeDelta = new Vector2(1, 1);
                //go.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                card.rectTransform.sizeDelta = new Vector2(1, 1);
                card.rectTransform.anchoredPosition = Vector2.zero;
                card.gameObject.transform.localPosition = new Vector3(0, 0, 0);

                card.info.text = "<align=\"center\">" + text;

                //info.fontSize = 2;
                info.fontSize = scale;
                info.enableWordWrapping = false;
                info.fontStyle = FontStyles.SmallCaps;

                //info.fontMaterial = Resources.Load("LiberationSans SDF", typeof(Material)) as Material;

                //info.faceColor = new Color(1, .1f, .3f, 1);
                //info.faceColor = new Color(1, 1, 0, 1);
                info.faceColor = color;

                info.fontMaterial.shader = Shader.Find("TextMeshPro/Distance Field");
                // set text shader main color
                //info.fontMaterial.SetColor("_FaceColor", new Color(1, .1f, .3f, 1));
                //info.fontMaterial.SetColor("_FaceColor", new Color(1, 1, 0, 1));

                info.fontMaterial.SetFloat("_Underlay", 1);
                //info.fontSharedMaterial.SetColor("_UnderlayColor", new Color(.5f, 0, 0, 1));
                info.fontMaterial.SetColor("_UnderlayColor", new Color(0, 0, 0, 1));
                info.fontMaterial.SetFloat("_UnderlayDilate", 0.755f);
                //// glow rim
                //info.fontSharedMaterial.SetFloat("_Glow", 1);
                //info.fontSharedMaterial.SetColor("_GlowColor", new Color(0, 1, 0, .5f));
                //info.fontSharedMaterial.SetFloat("_GlowOffset", 1f);
                //info.fontSharedMaterial.SetFloat("_GlowInner", .595f);
            }

            //card.rectTransform.parent = DamageNumbers.instance.transform;
            card.rectTransform.SetParent(DamageNumbers.instance.transform);
            card.rectTransform.position = startPoint;
            card.timeLeft = timeLeft;


            Card.liveCards.Add(card);
            return card;
        }
        public static void UpdateAll()
        {
            if (liveCards == null)
                return;
            if (liveCards.Count == 0)
                return;

            for (int i = 0; i < liveCards.Count; i++)
            {
                liveCards[i].Update();
            }
        }

        public void Update()
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0)
            {
                liveCards.Remove(this);
                deadCards.Push(this);
                gameObject.SetActive(false);
                
            }
            else
            {
                //rectTransform.position += Vector3.up * Time.deltaTime * 1;
                rectTransform.position += velocity * Time.deltaTime;
                velocity += gravity * Time.deltaTime;

                velocity *= 1 - drag * Time.deltaTime;  // not sure this actually normalizes it over time

            }
        }

    }

}
