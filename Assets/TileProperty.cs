using UnityEngine;

public class TileProperty : MonoBehaviour
{
    public enum TileType { Normal, Spike, Slow }
    public TileType type = TileType.Normal;

    public void SetType(TileType newType)
    {
        type = newType;

        // Visual Placeholder logic
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (type == TileType.Spike) sr.color = new Color(0.5f, 0f, 0f); // Dark Red
        if (type == TileType.Slow) sr.color = new Color(0.5f, 0.5f, 0f); // Brown/Mud
    }

    // Sets the color of the tile depending on the property
    public void RefreshVisuals(bool isHighlighted)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (isHighlighted)
        {
            sr.color = Color.yellow;
        }
        else
        {
            // Define colors based on type
            switch (type)
            {
                case TileType.Spike: sr.color = new Color(0.5f, 0f, 0f); break;
                case TileType.Slow: sr.color = new Color(0.5f, 0.5f, 0f); break;
                default: sr.color = Color.white; break;
            }
        }
    }

    private void OnMouseOver()
    {
        // This helps you verify types without looking at the Inspector
        if (type == TileType.Spike)
        {
            // You could eventually show a UI popup here
        }
    }
}