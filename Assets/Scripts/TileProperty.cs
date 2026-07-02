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

    [Header("Upgrade Progression")]
    public int currentTier = 1;
    public const int maxTier = 3;

    public HazardData currentData;

    private void Awake()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null) col.isTrigger = true;

        // SAFETY FALLBACK: If speedMult is uninitialized (0) on a non-slowing/freeze tile,
        // automatically default it to 1f so enemies can move normally.
        if (currentData.speedMult == 0f && type != TileType.Slow && type != TileType.Freeze)
        {
            currentData.speedMult = 1f;
        }

        ApplyHexColor();
    }

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

    public void UpgradeTileTier()
    {
        if (currentTier >= maxTier) return;

        currentTier++;

        currentData.damage *= 1.5f;
        currentData.dotDamage *= 1.5f;

        if (type == TileType.Slow && currentData.speedMult > 0.2f)
        {
            currentData.speedMult -= 0.1f;
        }

        Debug.Log($"[UPGRADE SUCCESS] upgraded {gameObject.name} to Tier {currentTier}! Damage scaled up.");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Enemy enemy = collision.GetComponent<Enemy>();

        if (enemy != null)
        {
            GridManager grid = FindObjectOfType<GridManager>();
            TileRotation tr = GetComponent<TileRotation>();
            if (tr == null) tr = GetComponentInParent<TileRotation>();

            if (grid != null && tr != null && tr.gridX == grid.startCoords.x && tr.gridY == grid.startCoords.y)
            {
                return;
            }

            string coordsString = (tr != null) ? $"({tr.gridX}, {tr.gridY})" : "(Unknown Coords)";

            Debug.Log($"<color=#FF4500>[STEPPED ON TILE] Enemy '{enemy.gameObject.name}' stepped on {type} Tile at grid {coordsString}. Payload Data -> Direct Dmg: {currentData.damage}, DoT/Sec: {currentData.dotDamage}, Slow: {currentData.speedMult}</color>");

            enemy.ApplyTileHazard(currentData, type);
        }
    }

    public void RefreshVisuals(bool isHighlighted)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null) return;

        GridManager grid = FindObjectOfType<GridManager>();
        if (grid != null && grid.mandatoryCheckpoints.Count > 0)
        {
            TileRotation tr = GetComponent<TileRotation>();
            if (tr == null) tr = GetComponentInParent<TileRotation>();

            if (tr != null && grid.mandatoryCheckpoints.Contains(new Vector2Int(tr.gridX, tr.gridY)))
            {
                sr.color = Color.cyan;
                return;
            }
        }

        if (isHighlighted)
        {
            sr.color = Color.yellow;
        }
        else
        {
            EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
            GridManager gridRef = FindObjectOfType<GridManager>();

            if (spawner != null && spawner.IsWaveRunning() && gridRef != null)
            {
                TileRotation tr = GetComponent<TileRotation>();
                if (tr == null) tr = GetComponentInParent<TileRotation>();

                if (tr != null)
                {
                    Vector3 myWorldPos = new Vector3(tr.gridX, tr.gridY, 0f);

                    if (gridRef.currentPathWorldPositions.Contains(myWorldPos))
                    {
                        sr.color = Color.gray;
                        return;
                    }
                }
            }

            ApplyHexColor();
        }
    }
}