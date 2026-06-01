using UnityEngine;

public class TileProperty : MonoBehaviour
{
    [System.Serializable]
    public struct HazardData
    {
        public float damage;            // Instant damage (Spikes/Pitfall)
        public float speedMult;         // 1.0 = normal, 0.5 = slow, 0 = freeze
        public float dotDamage;         // Damage per second (Burn/Poison)
        public float duration;          // How long the effect lasts
    }

    public HazardData currentData;

    // Tile types
    public enum TileType { Normal, Spike, Slow, Burn, Freeze, Pitfall, Poison, Static, Bleed, Curse }
    public TileType type = TileType.Normal;

    // Setting the tile type
    public void SetType(TileType newType)
    {
        type = newType;
        ApplyHexColor();
    }

    public void SetTileColor(Color customColor)
    {
        Renderer r = GetComponent<Renderer>();
        if (r is SpriteRenderer sr)
        {
            sr.color = customColor;
        }
        else if (r != null)
        {
            r.material.color = customColor;
        }
    }

    void ApplyHexColor()
    {
        string hex = "#FFFFFF"; // Default White

        switch (type)
        {
            case TileType.Spike: hex = "#808080"; break; // Gray
            case TileType.Slow: hex = "#5C4033"; break; // Brown
            case TileType.Burn: hex = "#FF8C00"; break; // Orange
            case TileType.Freeze: hex = "#A5F2F3"; break; // Ice Blue
            case TileType.Pitfall: hex = "#000000"; break; // Black
            case TileType.Poison: hex = "#228B22"; break; // Forest Green
            case TileType.Static: hex = "#FFFF00"; break; // Yellow
            case TileType.Bleed: hex = "#800000"; break; // Maroon
            case TileType.Curse: hex = "#4B0082"; break; // Indigo/Deep Purple
        }

        if (ColorUtility.TryParseHtmlString(hex, out Color customColor))
        {
            SetTileColor(customColor);
        }

        // --- SAFETY CORRECTION USING TILEROTATION ---
        TileRotation myRotationScript = GetComponent<TileRotation>();
        GridManager grid = FindObjectOfType<GridManager>();

        if (myRotationScript != null && grid != null)
        {
            // Compare TileRotation's grid coordinates to the GridManager's end coordinates
            if (grid.endCoords.x == myRotationScript.gridX && grid.endCoords.y == myRotationScript.gridY)
            {
                Transform goal = transform.Find("GoalIndicator");
                if (goal != null)
                {
                    goal.gameObject.SetActive(true);
                }
            }
        }
    }

    // Call this from GridManager's HighlightPath to maintain the hazard color
    public void RefreshVisuals(bool isHighlighted)
    {
        if (isHighlighted)
        {
            SetTileColor(Color.yellow);
        }
        else
        {
            ApplyHexColor(); // Reverts to the hex code assigned to the type
        }
    }
}