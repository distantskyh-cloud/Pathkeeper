using UnityEngine;
using System.Collections.Generic;

public class EnemyAbilities : MonoBehaviour
{
    private Enemy myEnemyComponent;

    [Header("Priest Single-Cast Settings")]
    public float priestTargetSearchRadius = 3.5f;
    public float priestHealFraction = 0.25f; // Restores 25% of target's Max HP
    private bool hasUsedPriestEmergencyCast = false;

    [Header("Bard (Supporter) Limited-Charge Settings")]
    public float bardAuraRadius = 3.5f;
    public float bardActionInterval = 3.0f; // Triggers every N seconds
    public int maxBardCharges = 2;          // Restricted strictly to 2 instances
    public float bardHealFraction = 0.10f;  // Considerably weaker than the Priest (10% vs 25%)

    private float bardTimer;
    private int bardsUsedCharges = 0;

    void Start()
    {
        myEnemyComponent = GetComponent<Enemy>();

        // Stagger startup clock slightly to avoid physics multi-cast frame alignment spikes
        bardTimer = Random.Range(0f, 0.5f);
    }

    void Update()
    {
        if (myEnemyComponent == null || myEnemyComponent.currentHP <= 0) return;

        switch (myEnemyComponent.currentClass)
        {
            case Enemy.EnemyClass.Priest:
                if (!hasUsedPriestEmergencyCast)
                {
                    HandlePriestEmergencyLogic();
                }
                break;

            case Enemy.EnemyClass.Supporter:
                if (bardsUsedCharges < maxBardCharges)
                {
                    HandleBardInspiration();
                }
                break;
        }
    }

    // =========================================================================
    // PRIEST: SINGLE-CAST EMERGENCY HEAL & CLEANSE (LOWEST HP NON-PRIEST)
    // =========================================================================
    void HandlePriestEmergencyLogic()
    {
        Collider2D[] entitiesInRange = Physics2D.OverlapCircleAll(transform.position, priestTargetSearchRadius);

        Enemy lowestHPAly = null;
        float lowestHealthPercentage = 1.0f;

        foreach (Collider2D col in entitiesInRange)
        {
            Enemy ally = col.GetComponent<Enemy>();
            if (ally != null && ally.currentHP > 0)
            {
                if (ally.currentClass != Enemy.EnemyClass.Priest && ally.currentHP < ally.maxHP)
                {
                    float healthPercent = ally.currentHP / ally.maxHP;
                    if (healthPercent < lowestHealthPercentage)
                    {
                        lowestHealthPercentage = healthPercent;
                        lowestHPAly = ally;
                    }
                }
            }
        }

        if (lowestHPAly != null)
        {
            hasUsedPriestEmergencyCast = true;

            float calculatedHeal = lowestHPAly.maxHP * priestHealFraction;
            lowestHPAly.currentHP = Mathf.Min(lowestHPAly.maxHP, lowestHPAly.currentHP + calculatedHeal);
            lowestHPAly.ResetStatusEffects();

            Debug.Log($"[PRIEST CLUTCH CAST] Spent single miracle! Healed '{lowestHPAly.gameObject.name}' for {calculatedHeal:F1} HP and Cleansed status debuffs.");
            Debug.DrawLine(transform.position, lowestHPAly.transform.position, Color.cyan, 1.5f);
        }
    }

    // =========================================================================
    // BARD: CYCLIC LIMITED-CHARGE RANDOM TARGET BUFF (Max 2 Uses)
    // =========================================================================
    void HandleBardInspiration()
    {
        bardTimer += Time.deltaTime;
        if (bardTimer >= bardActionInterval)
        {
            bardTimer = 0f;
            ExecuteLimitedBardSong();
        }
    }

    void ExecuteLimitedBardSong()
    {
        Collider2D[] alliesInRange = Physics2D.OverlapCircleAll(transform.position, bardAuraRadius);
        List<Enemy> potentialTargets = new List<Enemy>();

        foreach (Collider2D col in alliesInRange)
        {
            Enemy ally = col.GetComponent<Enemy>();
            if (ally != null && ally.currentHP > 0)
            {
                potentialTargets.Add(ally);
            }
        }

        if (potentialTargets.Count == 0) return;

        // Pick exactly ONE completely random ally out of the crowd
        int targetIndex = Random.Range(0, potentialTargets.Count);
        Enemy randomAlly = potentialTargets[targetIndex];

        bardsUsedCharges++;

        // Roll a random buff type: 0 = Haste, 1 = Armor, 2 = Minor Heal
        int rolledBuffType = Random.Range(0, 3);
        string buffName = "";

        switch (rolledBuffType)
        {
            case 0: // HASTE SPEED BOOST
                EnemyPathFinding movement = randomAlly.GetComponent<EnemyPathFinding>();
                if (movement != null)
                {
                    StartCoroutine(ApplyTemporaryHaste(movement, 1.4f, 1.5f)); // +40% speed for 1.5s
                    buffName = "Haste Velocity Melody (+40% Speed)";
                }
                break;

            case 1: // ARMOR PROTECTIVE SHIELD
                StartCoroutine(ApplyTemporaryArmor(randomAlly, 0.25f, 2.0f)); // +25% armor defense for 2.0s
                buffName = "Iron Shroud Ballad (+25% Armor)";
                break;

            case 2: // MINOR RENEWAL HEAL
                float minorHealValue = randomAlly.maxHP * bardHealFraction;
                randomAlly.currentHP = Mathf.Min(randomAlly.maxHP, randomAlly.currentHP + minorHealValue);
                buffName = $"Minor Renewal Chant (Restored {minorHealValue:F1} HP)";
                break;
        }

        Debug.Log($"[BARD SONG {bardsUsedCharges}/{maxBardCharges}] Targeted random ally '{randomAlly.gameObject.name}' ({randomAlly.currentClass}) with: {buffName}.");
        Debug.DrawLine(transform.position, randomAlly.transform.position, Color.magenta, 1.2f);
    }

    // --- TEMPORARY BONUS COROUTINE HANDLERS ---
    System.Collections.IEnumerator ApplyTemporaryHaste(EnemyPathFinding pathing, float mult, float duration)
    {
        float baseline = pathing.speed;
        pathing.speed *= mult;
        yield return new WaitForSeconds(duration);
        if (pathing != null) pathing.speed = baseline;
    }

    System.Collections.IEnumerator ApplyTemporaryArmor(Enemy target, float addedArmor, float duration)
    {
        target.armorPercent += addedArmor;
        yield return new WaitForSeconds(duration);
        if (target != null) target.armorPercent -= addedArmor;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, bardAuraRadius);
    }
}