using UnityEngine;

public class TileTrigger : MonoBehaviour
{
    private TileProperty tileProperty;

    void Start()
    {
        tileProperty = GetComponent<TileProperty>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Enemy enemy = collision.GetComponent<Enemy>();

        if (enemy != null && tileProperty != null)
        {
            // Informational tracker only - we bypass calling ApplyTileHazard here to let TileProperty handle it cleanly!
            TileRotation tr = GetComponent<TileRotation>();
            if (tr == null) tr = GetComponentInParent<TileRotation>();
            string coordsString = (tr != null) ? $"({tr.gridX}, {tr.gridY})" : "(Unknown Coords)";

            Debug.Log($"<color=#708090>[TRIGGER PASS] Sub-collider read-out on {tileProperty.type} Tile at {coordsString} for '{enemy.gameObject.name}'.</color>");
        }
    }
}