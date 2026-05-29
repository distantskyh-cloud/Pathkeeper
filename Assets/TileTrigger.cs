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
            enemy.ApplyTileHazard(tileProperty.currentData);
            Debug.Log($"[INTERACTION] Enemy ({enemy.currentClass}) stepped on tile: {tileProperty.type}");
        }
    }
}