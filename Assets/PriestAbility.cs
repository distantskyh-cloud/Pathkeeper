using UnityEngine;

public class PriestAbility : MonoBehaviour
{
    [Header("Priest Aura Settings")]
    public float cleanseRadius = 2.5f;
    public float cleanseInterval = 1.5f; // Cleanses every 1.5 seconds
    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= cleanseInterval)
        {
            CleanseNearbyAllies();
            timer = 0f;
        }
    }

    void CleanseNearbyAllies()
    {
        // Find all colliders in radius
        Collider2D[] nearbyAllies = Physics2D.OverlapCircle(transform.position, cleanseRadius);

        foreach (Collider2D allyCollider in nearbyAllies)
        {
            Enemy ally = allyCollider.GetComponent<Enemy>();

            // If it's a valid adventurer and NOT the priest themselves, cleanse them!
            if (ally != null && ally.gameObject != this.gameObject)
            {
                ally.ResetStatusEffects();
            }
        }
    }

    // Visualizes the aura radius in Unity's Scene view for debugging
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, cleanseRadius);
    }
}