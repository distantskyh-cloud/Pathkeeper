using UnityEngine;

public class SupporterAbility : MonoBehaviour
{
    [Header("Supporter Aura Settings")]
    public float buffRadius = 3f;
    public float buffInterval = 3f; // Sings a song every 3 seconds
    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= buffInterval)
        {
            ApplyRandomPartyBuff();
            timer = 0f;
        }
    }

    void ApplyRandomPartyBuff()
    {
        Collider2D[] nearbyAllies = Physics2D.OverlapCircle(transform.position, buffRadius);
        if (nearbyAllies.Length <= 1) return; // No one around to buff (including self)

        int randomBuffType = Random.Range(0, 3); // 0 = Haste, 1 = Protection, 2 = Motivation

        foreach (Collider2D allyCollider in nearbyAllies)
        {
            Enemy ally = allyCollider.GetComponent<Enemy>();
            if (ally != null)
            {
                switch (randomBuffType)
                {
                    case 0: // HASTE SONG
                        EnemyPathFinding path = ally.GetComponent<EnemyPathFinding>();
                        if (path != null) path.speed *= 1.35f; // 35% speed boost
                        break;

                    case 1: // IRON SKIN SONG
                        ally.armorPercent = Mathf.Clamp(ally.armorPercent + 0.15f, 0f, 0.85f); // Limit armor to 85% max
                        break;

                    case 2: // RALLY SONG
                        ally.currentHP = Mathf.Min(ally.currentHP + 10f, ally.maxHP); // Quick small burst heal
                        break;
                }
            }
        }
        Debug.Log($"[BARD] Performed Song Type [{randomBuffType}] to inspire the party!");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, buffRadius);
    }
}