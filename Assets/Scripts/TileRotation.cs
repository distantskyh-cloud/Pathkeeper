using UnityEngine;

public class TileRotation : MonoBehaviour
{
    // A list of possible directions
    public enum Direction { Up, Right, Down, Left }
    public Direction currentDirection;

    // Add these so the tile knows its 'address'
    [HideInInspector] public int gridX;
    [HideInInspector] public int gridY;

    private void OnMouseDown()
    {
        // 1. HARD LOCK: If a wave is active, block everything instantly
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null && spawner.IsWaveRunning())
        {
            Debug.Log("[TACTICAL LOCK] Cannot rotate path layouts while a combat wave is in progress!");
            return;
        }

        // 2. UNRESTRICTED ROTATION: If no wave is active, allow infinite clicks
        transform.Rotate(0, 0, -90f);
        UpdateDirection();

        // 3. RE-SCAN: Update the path map state
        GridManager grid = FindObjectOfType<GridManager>();
        if (grid != null)
        {
            grid.TracePath();
        }
    }

    void UpdateDirection()
    {
        // This cycles through our enum list
        if (currentDirection == Direction.Left)
            currentDirection = Direction.Up; // Loop back to start
        else
            currentDirection++; // Move to next direction (Right, then Down, etc.)
    }
}