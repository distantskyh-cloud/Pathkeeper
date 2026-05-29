using UnityEngine;

public class Enemy : MonoBehaviour
{
    public enum EnemyClass { Swordsman, Tanker, Rogue }
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

    void Awake()
    {
        pathfindingScript = GetComponent<EnemyPathFinding>();
    }

    public void InitializeEnemy(EnemyClass targetClass)
    {
        currentClass = targetClass;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();

        switch (currentClass)
        {
            case EnemyClass.Swordsman:
                maxHP = 100f; armorPercent = 0f; baseSpeed = 1.0f;
                SetPlaceholderVisuals(sr, "#0000FF", new Vector3(1f, 1f, 1f)); // Blue
                break;
            case EnemyClass.Tanker:
                maxHP = 200f; armorPercent = 0.50f; baseSpeed = 0.6f;
                SetPlaceholderVisuals(sr, "#4A4A4A", new Vector3(1.4f, 1.4f, 1f)); // Dark Gray/Big
                break;
            case EnemyClass.Rogue:
                maxHP = 60f; armorPercent = 0f; baseSpeed = 1.8f;
                SetPlaceholderVisuals(sr, "#FFD700", new Vector3(0.7f, 0.7f, 1f)); // Gold/Small
                break;
        }
        currentHP = maxHP;
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

        if (pathfindingScript != null) pathfindingScript.speed = baseSpeed * hazard.speedMult;

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

    void ResetStatusEffects()
    {
        dotDamagePerSecond = 0;
        isEffectInfinite = false;
        if (pathfindingScript != null) pathfindingScript.speed = baseSpeed;
    }

    public void TakeDamage(float incomingDamage, bool isStatusEffect)
    {
        float finalDamage = incomingDamage;
        if (!isStatusEffect) finalDamage = incomingDamage * (1f - armorPercent);

        currentHP -= finalDamage;
        Debug.Log($"{gameObject.name} ({currentClass}) HP: {currentHP:F1}/{maxHP}");

        if (currentHP <= 0) Destroy(gameObject);
    }
}