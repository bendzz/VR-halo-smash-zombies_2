using UnityEngine;


/// <summary>
/// To make overly large hitboxes that don't physically collide with most of the world- Except stuff you put in Ignore Raycast layer.
/// Can work as isTrigger or physics collider (but be careful of physics overlaps)
/// </summary>
[RequireComponent(typeof(Collider))]
public class IntangibleHitbox : MonoBehaviour
{
    private Collider hitboxCollider;
    private const int IGNORE_RAYCAST_LAYER = 2;

    [Tooltip("Tell scripts that when they hit this hitbox, send their complaints to X gameobject")]
    public GameObject representsObject;

    private void Awake()
    {
        hitboxCollider = GetComponent<Collider>();

        if (hitboxCollider.isTrigger)
        {
            Debug.LogWarning("The collider on " + gameObject.name + " should be a physics collider, not a trigger.");
            return;
        }

        // Check if physics matrix is correctly set
        if (!Physics.GetIgnoreLayerCollision(IGNORE_RAYCAST_LAYER, 0))
        {
            Debug.LogWarning("WARNING: Setting layer 'Ignore Raycast' to have no physics collision! (Using this layer for intangible hitboxes)");
        }

        // Set physics matrix so that "Ignore Raycast" doesn't interact with any other layers, including itself
        for (int i = 0; i < 32; i++)
        {
            Physics.IgnoreLayerCollision(IGNORE_RAYCAST_LAYER, i, true);
        }

        // Then re-enable collisions with only "Ignore Raycast" layer itself.
        Physics.IgnoreLayerCollision(IGNORE_RAYCAST_LAYER, IGNORE_RAYCAST_LAYER, false);

        //hitboxCollider.excludeLayers = ~0;  // set to "Everything"
        hitboxCollider.excludeLayers = ~(1 << IGNORE_RAYCAST_LAYER);    // set to ignore "Everything" except "Ignore Raycast"

        gameObject.name += "_IntangibleHitbox";

        // Set the game object's layer to "Ignore Raycast"
        gameObject.layer = IGNORE_RAYCAST_LAYER;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == IGNORE_RAYCAST_LAYER)
        {
            Debug.Log(gameObject.name + " collided with " + collision.gameObject.name);
        }
    }
}
