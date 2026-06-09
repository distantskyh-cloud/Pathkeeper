using UnityEngine;

public class Enemy : MonoBehaviour
{
    // The enum remains so other systems (like Spawners or Tiles) can easily identify this unit
    public enum EnemyClass { Swordsman, Tanker, Rogue, Priest, Supporter, Paladin }

    [Header("Prefab Baseline Config")]
    [Tooltip("Match this dropdown option to the identity of this specific prefab asset.")]
    public EnemyClass currentClass;

    [Tooltip("Configure unique values for each prefab directly inside the Inspector window!")]
    public float maxHP = 100f;
    public float armorPercent = 0f; // e.g., 0.50f = 50% damage reduction
    [SerializeField] private float baseSpeed = 1.0f;

    [Header("Current Live Stats (Runtime Only)")]
    public float currentHP;

    private float dotDamagePerSecond;
    private float effectDurationTimer;
    private bool isEffectInfinite;

    private EnemyPathFinding pathfindingScript;
    private bool hasUsedPaladinHeal = false;

    void Awake()
    {
        pathfindingScript = GetComponent<EnemyPathFinding>();
    }

    // UPDATED: This completely respects independent prefab inspector data
    public void InitializeEnemy(EnemyClass targetClass)
    {
        currentClass = targetClass;

        // 1. Scale baseline max health via dynamic Wave Multipliers
        if (GameManager.Instance != null)
        {
            maxHP *= GameManager.Instance.enemyHealthMultiplier;
        }

        // 2. Set current health to our calculated max value
        currentHP = maxHP;

        // 3. Scale base speed via dynamic difficulty settings
        if (GameManager.Instance != null)
        {
            baseSpeed *= GameManager.Instance.enemySpeedMultiplier;
        }

        // 4. Safely push speed configuration right to your navigation script
        if (pathfindingScript == null) pathfindingScript = GetComponent<EnemyPathFinding>();
        if (pathfindingScript != null) pathfindingScript.speed = baseSpeed;
    }

    void Update()
    {
        if (effectDurationTimer > 0 || isEffectInfinite)
        {
            if (dotDamagePerSecond > 0)
            {
                TakeDamage(dotDamagePerSecond * Time.deltaTime, isStatusEffect: true);
            }

            if (!isEffectInfinite)
            {
                effectDurationTimer -= Time.deltaTime;
                if (effectDurationTimer <= 0) ResetStatusEffects();
            }
        }
    }

    public void ApplyTileHazard(TileProperty.HazardData hazard)
    {
        if (hazard.damage > 0) TakeDamage(hazard.damage, isStatusEffect: false);

        float multiplier = hazard.speedMult;
        if (multiplier <= 0)
        {
            if (hazard.duration == 0 && hazard.damage == 0 && hazard.dotDamage == 0)
            {
                multiplier = 1f; // Treat as a normal, non-slowing tile
            }
        }

        if (pathfindingScript != null)
        {
            pathfindingScript.speed = baseSpeed * multiplier;
        }

        dotDamagePerSecond = hazard.dotDamage;
        if (hazard.duration == -1)
        {
            isEffectInfinite = true;
            effectDurationTimer = 0;
        }
        else
        {
            isEffectInfinite = false;
            effectDurationTimer = hazard.duration;
        }
    }

    public void ResetStatusEffects()
    {
        dotDamagePerSecond = 0;
        isEffectInfinite = false;

        if (pathfindingScript != null) pathfindingScript.speed = baseSpeed;
    }

    // UPDATED: Fully exposed debug output tracking all 6 classes seamlessly
    public void TakeDamage(float incomingDamage, bool isStatusEffect)
    {
        float finalDamage = incomingDamage;

        // Apply defense reduction calculations only to physical hits (like spikes), bypass for DoTs
        if (!isStatusEffect)
        {
            finalDamage = incomingDamage * (1f - armorPercent);
        }

        currentHP -= finalDamage;

        // UNIVERSAL DEBUG LOG: Tracks incoming numbers, reductions, and remaining HP for any unit on screen
        Debug.Log($"[DAMAGE LIVE LOG] '{gameObject.name}' ({currentClass}) took {finalDamage:F1} damage (Type: {(isStatusEffect ? "DoT" : "Direct")}). Remaining HP: {currentHP:F1}/{maxHP}");

        // PALADIN MID-BOSS CLEANSE & SELF-HEAL TRIGGER:
        if (currentClass == EnemyClass.Paladin && !hasUsedPaladinHeal && currentHP <= (maxHP * 0.5f))
        {
            hasUsedPaladinHeal = true;
            currentHP += (maxHP * 0.35f);
            ResetStatusEffects();
            Debug.Log("[MID-BOSS TRIGGER] Paladin dropped below 50% HP! Casted Holy Cleanse and regenerated 35% health.");
        }

        if (currentHP <= 0)
        {
            Debug.Log($"[DEATH EVENT] '{gameObject.name}' ({currentClass}) health dropped to 0 and has been removed.");
            Destroy(gameObject);
        }
    }
}