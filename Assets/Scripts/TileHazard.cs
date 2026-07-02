using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class TileHazard : MonoBehaviour
{
    public enum HazardType { Poison, Burn, Static, Freeze, Bleed, Curse }

    [Header("Tile Metadata")]
    public HazardType type;
    public string tileName;

    [Header("Hazard Payload Configuration")]
    [Tooltip("Immediate, lump-sum damage dealt the instant an adventurer steps on this tile.")]
    public float directDamage = 0f;

    [Tooltip("Damage dealt per second over time while the status effect is active.")]
    public float damagePerSecond = 0f;

    [Tooltip("Movement speed multiplier. 1 = Normal speed, 0.5 = Half speed, 0 = Frozen solid.")]
    [Range(0f, 2f)] public float speedMultiplier = 1f;

    [Tooltip("How long the status lasts in seconds. Set to -1 for permanent/infinite effects until cleansed.")]
    public float effectDuration = 0f;

    private void Awake()
    {
        // Automatically ensure the tile's collider is configured correctly as a passive detector
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the overlapping object contains your Enemy framework component
        Enemy adventurer = other.GetComponent<Enemy>();
        if (adventurer != null)
        {
            // Build the exact structural payload your Enemy script expects natively
            TileProperty.HazardData payload;
            payload.damage = directDamage;
            payload.dotDamage = damagePerSecond;
            payload.speedMult = speedMultiplier;
            payload.duration = effectDuration;

            // Safe explicit conversion map from TileHazard.HazardType to TileProperty.TileType
            TileProperty.TileType synchronizedType = TileProperty.TileType.Normal;

            switch (type)
            {
                case HazardType.Poison: synchronizedType = TileProperty.TileType.Poison; break;
                case HazardType.Burn: synchronizedType = TileProperty.TileType.Burn; break;
                case HazardType.Static: synchronizedType = TileProperty.TileType.Static; break;
                case HazardType.Freeze: synchronizedType = TileProperty.TileType.Freeze; break;
                case HazardType.Bleed: synchronizedType = TileProperty.TileType.Bleed; break;
                case HazardType.Curse: synchronizedType = TileProperty.TileType.Curse; break;
            }

            // Pass both the stats payload and the cleanly mapped tile identifier type
            adventurer.ApplyTileHazard(payload, synchronizedType);
            Debug.Log($"[DUNGEON HASSLE] {adventurer.gameObject.name} entered {tileName} ({type}). Sending processing instructions.");
        }
    }
}