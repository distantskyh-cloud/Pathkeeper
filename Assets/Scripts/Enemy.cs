using UnityEngine;

public class Enemy : MonoBehaviour
{
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

    [Header("Economy Rewards")]
    [Tooltip("The amount of gold given to the player when this specific type of enemy is defeated.")]
    public int goldBountyReward = 15;

    void Awake()
    {
        pathfindingScript = GetComponent<EnemyPathFinding>();
    }

    public void InitializeEnemy(EnemyClass targetClass)
    {
        currentClass = targetClass;

        if (GameManager.Instance != null)
        {
            maxHP *= GameManager.Instance.enemyHealthMultiplier;
        }

        currentHP = maxHP;

        if (GameManager.Instance != null)
        {
            baseSpeed *= GameManager.Instance.enemySpeedMultiplier;
        }

        if (pathfindingScript == null) pathfindingScript = GetComponent<EnemyPathFinding>();
        if (pathfindingScript != null) pathfindingScript.speed = baseSpeed;
    }

    void Update()
    {
        if (effectDurationTimer > 0 || isEffectInfinite)
        {
            if (dotDamagePerSecond > 0)
            {
                // Correctly routes status effect ticks into our consolidated damage method
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
                multiplier = 1f;
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

    // --- CONSOLIDATED DAMAGE METHOD ---
    public void TakeDamage(float incomingDamage, bool isStatusEffect)
    {
        float finalDamage = incomingDamage;

        // Apply armor reduction to physical hits, bypass for damage over time
        if (!isStatusEffect)
        {
            finalDamage = incomingDamage * (1f - armorPercent);
        }

        currentHP -= finalDamage;

        Debug.Log($"[DAMAGE LIVE LOG] '{gameObject.name}' ({currentClass}) took {finalDamage:F1} damage (Type: {(isStatusEffect ? "DoT" : "Direct")}). Remaining HP: {currentHP:F1}/{maxHP}");

        // PALADIN MID-BOSS CLEANSE & SELF-HEAL TRIGGER
        if (currentClass == EnemyClass.Paladin && !hasUsedPaladinHeal && currentHP <= (maxHP * 0.5f))
        {
            hasUsedPaladinHeal = true;
            currentHP += (maxHP * 0.35f);
            ResetStatusEffects();
            Debug.Log("[MID-BOSS TRIGGER] Paladin dropped below 50% HP! Casted Holy Cleanse and regenerated 35% health.");
        }

        // Trigger our centralized death function when health is depleted
        if (currentHP <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"[DEATH EVENT] '{gameObject.name}' ({currentClass}) health dropped to 0.");

        // ECONOMY REWARD HOOK: Hand over cash bounty upon unit death!
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddGold(goldBountyReward);
            Debug.Log($"[BOUNTY COLLECTED] +{goldBountyReward}g gained from killing {gameObject.name}!");
        }
        else
        {
            Debug.LogWarning("[ECONOMY WARNING] Tried to award gold, but EconomyManager.Instance is missing in the scene!");
        }

        Destroy(gameObject);
    }
}