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

    // Generic Status Effects
    private float dotDamagePerSecond;
    private float effectDurationTimer;
    private bool isEffectInfinite;

    // Unified Velocity Bleed Buff Data Tracking
    [Header("Bleed Status State")]
    public bool isBleeding = false;
    private float bleedBaseIntensityModifier;
    private Vector3 positionLastFrame;

    private EnemyPathFinding pathfindingScript;
    private bool hasUsedPaladinHeal = false;

    [Header("Economy Rewards")]
    [Tooltip("The amount of gold given to the player when this specific type of enemy is defeated.")]
    public int goldBountyReward = 15;

    void Awake()
    {
        pathfindingScript = GetComponent<EnemyPathFinding>();
    }

    void Start()
    {
        // Track baseline positioning to calculate real distance deltas natively
        positionLastFrame = transform.position;
    }

    public void InitializeEnemy(EnemyClass targetClass)
    {
        currentClass = targetClass;

        if (GameManager.Instance != null)
        {
            maxHP *= GameManager.Instance.enemyHealthMultiplier;
        }

        currentHP = maxHP;

        if (pathfindingScript != null && GameManager.Instance != null)
        {
            pathfindingScript.speed = baseSpeed * GameManager.Instance.enemySpeedMultiplier;
        }
    }

    /// <summary>
    /// Processes inbound tile statuses, incorporating custom class evasions and counters.
    /// </summary>
    public void ApplyTileHazard(TileProperty.HazardData payload, TileProperty.TileType hazardType)
    {
        // HARD COUTNERS CHECK: Slow, Freeze, and Bleed bypass ALL Rogue evasions completely!
        bool isHardCounter = (hazardType == TileProperty.TileType.Slow ||
                              hazardType == TileProperty.TileType.Freeze ||
                              hazardType == TileProperty.TileType.Bleed);

        if (currentClass == EnemyClass.Rogue && !isHardCounter)
        {
            // Rogue Evade Option 2: 25% Chance to fully dodge generic statuses
            if (Random.value <= 0.25f)
            {
                Debug.Log($"[ROGUE AGILITY] '{gameObject.name}' completely dodged the {hazardType} effect!");
                return;
            }
            else
            {
                // Mitigated failure penalty: Only suffer a reduced portion (e.g. 50%) of the payload profile
                payload.damage *= 0.5f;
                payload.dotDamage *= 0.5f;
                payload.duration *= 0.5f;
                Debug.Log($"[ROGUE MISSTEP] Avoid failed! Suffer 50% mitigated {hazardType} penalty.");
            }
        }

        // Direct upfront collision puncture damage application
        if (payload.damage > 0)
        {
            TakeDamage(payload.damage, isStatusEffect: false);
        }

        // CUSTOM MECHANIC: Check if this payload is an active Bleed application
        if (hazardType == TileProperty.TileType.Bleed)
        {
            isBleeding = true;
            // Map incoming dotDamage configuration setting to drive our scaling modifier
            bleedBaseIntensityModifier = payload.dotDamage > 0 ? payload.dotDamage : 5f;
            Debug.Log($"[BLOOD LETTING] '{gameObject.name}' is now bleeding permanently! Damage scales with movement activity.");
            return;
        }

        // Standard logic track for normal non-permanent status properties
        if (payload.dotDamage > 0 || payload.speedMult != 1f)
        {
            dotDamagePerSecond = payload.dotDamage;
            isEffectInfinite = (payload.duration == -1f);
            effectDurationTimer = isEffectInfinite ? 0f : payload.duration;

            if (pathfindingScript != null)
            {
                float runtimeMod = baseSpeed * (GameManager.Instance != null ? GameManager.Instance.enemySpeedMultiplier : 1f);
                pathfindingScript.speed = runtimeMod * payload.speedMult;
            }
        }
    }

    void Update()
    {
        if (currentHP <= 0) return;

        // 1. VELOCITY-BASED BLEED CALCULATOR LOOP
        if (isBleeding)
        {
            // Quantify real spatial tracking translation change done this frame
            float physicalDistanceMoved = Vector3.Distance(transform.position, positionLastFrame);

            if (physicalDistanceMoved > 0.0001f && pathfindingScript != null)
            {
                // Live tracking velocity calculation
                float activeSpeedMagnitude = pathfindingScript.speed;

                // Bleed Math Formulation: Base Factor * Spacial Step Distance * Velocity Scale Value
                float velocityBleedTick = bleedBaseIntensityModifier * physicalDistanceMoved * activeSpeedMagnitude;

                // Apply damage directly bypasses standard frame multipliers since it is already frame dependent
                TakeDamage(velocityBleedTick, isStatusEffect: true);
            }
        }
        // Cache current position index frame target update
        positionLastFrame = transform.position;

        // 2. RE-EVALUATE STANDARD TICK TIMERS
        if (isEffectInfinite || effectDurationTimer > 0)
        {
            if (dotDamagePerSecond > 0)
            {
                TakeDamage(dotDamagePerSecond * Time.deltaTime, isStatusEffect: true);
            }

            if (!isEffectInfinite)
            {
                effectDurationTimer -= Time.deltaTime;
                if (effectDurationTimer <= 0)
                {
                    ResetStatusEffects();
                }
            }
        }
    }

    public void ResetStatusEffects()
    {
        dotDamagePerSecond = 0f;
        effectDurationTimer = 0f;
        isEffectInfinite = false;

        // Clear physical bleed status clean
        isBleeding = false;

        if (pathfindingScript != null)
        {
            pathfindingScript.speed = baseSpeed * (GameManager.Instance != null ? GameManager.Instance.enemyHealthMultiplier : 1f);
        }
        Debug.Log($"[STATUS PURGE] '{gameObject.name}' has had all status ailments cleared cleanly.");
    }

    public void TakeDamage(float incomingDamage, bool isStatusEffect)
    {
        if (currentHP <= 0) return;

        float finalDamage = incomingDamage;

        // Status DoT calculations bypass traditional armor absorption plates natively
        if (!isStatusEffect)
        {
            finalDamage = incomingDamage * (1f - armorPercent);
        }

        currentHP -= finalDamage;

        // PALADIN MID-BOSS CLEANSE & FULL SELF-HEAL TRIGGER
        if (currentClass == EnemyClass.Paladin && !hasUsedPaladinHeal && currentHP <= (maxHP * 0.5f))
        {
            hasUsedPaladinHeal = true;
            currentHP = maxHP;
            ResetStatusEffects(); // Clears Bleed as well!
            Debug.Log("[MID-BOSS TRIGGER] Paladin dropped below 50% HP! Casted Lay on Hands: Restored to full health and purged all status ailments.");
        }

        if (currentHP <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"[DEATH EVENT] '{gameObject.name}' ({currentClass}) health dropped to 0.");

        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddGold(goldBountyReward);
            Debug.Log($"[BOUNTY COLLECTED] +{goldBountyReward}g gained from killing {gameObject.name}!");
        }

        Destroy(gameObject);
    }
}