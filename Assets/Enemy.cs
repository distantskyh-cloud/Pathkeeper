using UnityEngine;

public class Enemy : MonoBehaviour
{
    // Extended to include the Tier-2 Support Specialists and the Mid-Boss Champion
    public enum EnemyClass { Swordsman, Tanker, Rogue, Priest, Supporter, Paladin }
    public EnemyClass currentClass;

    [Header("Current Live Stats")]
    public float maxHP;
    public float currentHP;
    public float armorPercent; // e.g., 0.50f = 50% damage reduction

    private float baseSpeed;
    private float dotDamagePerSecond;
    private float effectDurationTimer;
    private bool isEffectInfinite;

    private EnemyPathFinding pathfindingScript;

    // Paladin unique runtime check to ensure his "Lay on Hands" mechanic only activates once
    private bool hasUsedPaladinHeal = false;

    void Awake()
    {
        pathfindingScript = GetComponent<EnemyPathFinding>();
    }

    public void InitializeEnemy(EnemyClass targetClass)
    {
        currentClass = targetClass;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();

        // Set native stats based on the adventurer's class archetype
        switch (currentClass)
        {
            case EnemyClass.Swordsman:
                maxHP = 100f; armorPercent = 0f; baseSpeed = 1.0f;
                SetPlaceholderVisuals(sr, "#0000FF", new Vector3(1f, 1f, 1f)); // Blue
                break;
            case EnemyClass.Tanker:
                maxHP = 200f; armorPercent = 0.50f; baseSpeed = 0.6f;
                SetPlaceholderVisuals(sr, "#4A4A4A", new Vector3(1.4f, 1.4f, 1f)); // Dark Grey
                break;
            case EnemyClass.Rogue:
                maxHP = 60f; armorPercent = 0f; baseSpeed = 1.8f;
                SetPlaceholderVisuals(sr, "#FFD700", new Vector3(0.7f, 0.7f, 1f)); // Gold/Yellow
                break;
            case EnemyClass.Priest:
                maxHP = 75f; armorPercent = 0f; baseSpeed = 0.9f;
                SetPlaceholderVisuals(sr, "#00FFCC", new Vector3(0.9f, 0.9f, 1f)); // Teal
                break;
            case EnemyClass.Supporter:
                maxHP = 80f; armorPercent = 0.05f; baseSpeed = 1.0f;
                SetPlaceholderVisuals(sr, "#FF00FF", new Vector3(0.9f, 0.9f, 1f)); // Magenta/Pink
                break;
            case EnemyClass.Paladin:
                maxHP = 350f; armorPercent = 0.40f; baseSpeed = 0.7f;
                SetPlaceholderVisuals(sr, "#FFFFCC", new Vector3(1.5f, 1.5f, 1f)); // Big Golden-White
                break;
        }
        currentHP = maxHP;

        // HOOK TO GAMEMANAGER: Scales max and current health based on selected difficulty
        if (GameManager.Instance != null)
        {
            maxHP *= GameManager.Instance.enemyHealthMultiplier;
            currentHP = maxHP;
        }

        // HOOK TO GAMEMANAGER: Scales native speed based on selected difficulty
        if (GameManager.Instance != null)
        {
            baseSpeed *= GameManager.Instance.enemySpeedMultiplier;
        }

        // Ensure the pathfinding script gets the speed immediately upon initialization
        if (pathfindingScript == null) pathfindingScript = GetComponent<EnemyPathFinding>();
        if (pathfindingScript != null) pathfindingScript.speed = baseSpeed;
    }

    void SetPlaceholderVisuals(SpriteRenderer sr, string hexColor, Vector3 scale)
    {
        transform.localScale = scale;
        if (sr != null && ColorUtility.TryParseHtmlString(hexColor, out Color customColor))
        {
            sr.color = customColor;
        }
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

        // 1. EMERGENCY BASE SPEED FALLBACK: If baseSpeed isn't initialized yet, recover it safely with difficulty scale included
        if (baseSpeed <= 0)
        {
            AssignBaseSpeedByClass();
        }

        // 2. TILE HAZARD SAFETY GUARD: 
        // If speedMult is exactly 0, check if this is an intentional Pitfall or Freeze hazard.
        // If it's just an uninitialized normal tile, default the multiplier to 1f (full speed).
        float multiplier = hazard.speedMult;
        if (multiplier <= 0)
        {
            // If your hazard doesn't have duration or damage, it's a default/empty tile struct!
            if (hazard.duration == 0 && hazard.damage == 0 && hazard.dotDamage == 0)
            {
                multiplier = 1f; // Treat as a normal, non-slowing tile
            }
        }

        // 3. Apply the calculated speed to the pathfinder safely
        if (pathfindingScript != null)
        {
            pathfindingScript.speed = baseSpeed * multiplier;
        }

        // Handle status durations
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

        // Ensure baseSpeed is valid before resetting, applying difficulty multiplier automatically
        if (baseSpeed <= 0)
        {
            AssignBaseSpeedByClass();
        }

        if (pathfindingScript != null) pathfindingScript.speed = baseSpeed;
    }

    /// <summary>
    /// Helper method to ensure fallbacks and initial layout both evaluate the GameManager difficulty speed rules cleanly
    /// </summary>
    private void AssignBaseSpeedByClass()
    {
        switch (currentClass)
        {
            case EnemyClass.Swordsman: baseSpeed = 1.0f; break;
            case EnemyClass.Tanker: baseSpeed = 0.6f; break;
            case EnemyClass.Rogue: baseSpeed = 1.8f; break;
            case EnemyClass.Priest: baseSpeed = 0.9f; break;
            case EnemyClass.Supporter: baseSpeed = 1.0f; break;
            case EnemyClass.Paladin: baseSpeed = 0.7f; break;
        }

        if (GameManager.Instance != null)
        {
            baseSpeed *= GameManager.Instance.enemySpeedMultiplier;
        }
    }

    public void TakeDamage(float incomingDamage, bool isStatusEffect)
    {
        float finalDamage = incomingDamage;
        if (!isStatusEffect) finalDamage = incomingDamage * (1f - armorPercent);

        currentHP -= finalDamage;
        Debug.Log($"{gameObject.name} ({currentClass}) HP: {currentHP:F1}/{maxHP}");

        // ⭐ PALADIN MID-BOSS CLEANSE & SELF-HEAL TRIGGER:
        // Triggers when he drops below or equal to 50% max HP.
        if (currentClass == EnemyClass.Paladin && !hasUsedPaladinHeal && currentHP <= (maxHP * 0.5f))
        {
            hasUsedPaladinHeal = true;
            currentHP += (maxHP * 0.35f); // Restores 35% of max health
            ResetStatusEffects();        // Wipes poison, burn, and tile slows instantly!
            Debug.Log("[MID-BOSS] Paladin activated Holy Cleanse & Heal!");
        }

        if (currentHP <= 0) Destroy(gameObject);
    }
}