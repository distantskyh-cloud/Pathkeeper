using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class TileProperty : MonoBehaviour
{
    [System.Serializable]
    public struct HazardData
    {
        public float damage;            // Instant damage (e.g. Spike entry hit)
        public float speedMult;         // Speed modifier (1 = normal, 0.5 = slow)
        public float dotDamage;         // Damage per second
        public float duration;          // Duration of effect (-1 for infinite)
    }

    [Header("Hazard Configuration")]
    public TileType type = TileType.Normal;
    public enum TileType { Normal, Slow, Burn, Freeze, Pitfall, Poison, Static, Bleed, Curse }

    public HazardData currentData;

    private void Awake()
    {
        // Enforce trigger physics configuration natively on startup
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null) col.isTrigger = true;

        ApplyHexColor();
    }

    // UPDATED: This now reads the enum Type directly to apply the proper color automatically!
    private void ApplyHexColor()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();

        if (sr == null) return;

        string hex = "#FFFFFF"; // Default White for Normal Tiles

        switch (type)
        {
            case TileType.Slow: hex = "#5C4033"; break; // Brown
            case TileType.Burn: hex = "#FF8C00"; break; // Orange
            case TileType.Freeze: hex = "#A5F2F3"; break; // Ice Blue
            case TileType.Pitfall: hex = "#000000"; break; // Black
            case TileType.Poison: hex = "#00FF00"; break; // Poison Green
            case TileType.Static: hex = "#FFFF00"; break; // Yellow
            case TileType.Bleed: hex = "#800000"; break; // Maroon
            case TileType.Curse: hex = "#4B0082"; break; // Indigo/Deep Purple
        }

        if (ColorUtility.TryParseHtmlString(hex, out Color customColor))
        {
            sr.color = customColor;
        }
    }

    // Consolidated from TileTrigger.cs: The tile now applies its own hazard rules directly
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Enemy enemy = collision.GetComponent<Enemy>();

        if (enemy != null)
        {
            enemy.ApplyTileHazard(currentData);
            Debug.Log($"[INTERACTION] Enemy ({enemy.currentClass}) stepped on tile prefab: {type}");
        }
    }

    public void RefreshVisuals(bool isHighlighted)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null) return;

        if (isHighlighted)
        {
            sr.color = Color.yellow;
        }
        else
        {
            ApplyHexColor();
        }
    }
}