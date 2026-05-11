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
    public enum TileType { Normal, Spike, Slow, Burn, Freeze, Pitfall, Poison, Static }
    public TileType type = TileType.Normal;

    // Setting the tile type
    public void SetType(TileType newType)
    {
        type = newType;
        ApplyHexColor();
    }

    void ApplyHexColor()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        string hex = "#FFFFFF"; // Default White

        switch (type)
        {
            case TileType.Spike: hex = "#808080"; break; // Gray
            case TileType.Slow: hex = "#5C4033"; break; // Brown
            case TileType.Burn: hex = "#8B0000"; break; // Dark Red
            case TileType.Freeze: hex = "#00FFFF"; break; // Cyan
            case TileType.Pitfall: hex = "#000000"; break; // Black
            case TileType.Poison: hex = "#800080"; break; // Purple
            case TileType.Static: hex = "#B8860B"; break; // Dark Yellow
        }

        if (ColorUtility.TryParseHtmlString(hex, out Color customColor))
        {
            sr.color = customColor;
        }
    }

    // Call this from GridManager's HighlightPath to maintain the hazard color
    public void RefreshVisuals(bool isHighlighted)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (isHighlighted)
        {
            sr.color = Color.yellow;
        }
        else
        {
            ApplyHexColor(); // Reverts to the hex code assigned to the type
        }
    }
}