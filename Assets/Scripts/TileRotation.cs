using UnityEngine;

public class TileRotation : MonoBehaviour
{
    public enum Direction { Up, Right, Down, Left }
    public Direction currentDirection;

    [HideInInspector] public int gridX;
    [HideInInspector] public int gridY;

    private void OnMouseDown()
    {
        // 1. ISOLATED ROTATION LOCK CHECK
        // If rotation is disabled via dev tools, stop execution here immediately.
        // This keeps the tile direction completely frozen so swapping can happen smoothly.
        if (GameManager.Instance != null && !GameManager.Instance.isTileRotationEnabled)
        {
            Debug.Log($"[ROTATION LOCKED] Tile ({gridX}, {gridY}) skipped spin update. (Swapping or Selection input can still process).");
            return;
        }

        // 2. ACTIVE WAVE COMBAT LOCK
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null && spawner.IsWaveRunning())
        {
            Debug.Log("[TACTICAL LOCK] Cannot rotate path layouts while a combat wave is in progress!");
            return;
        }

        // 3. EXECUTE ROTATION
        transform.Rotate(0, 0, -90f);
        UpdateDirection();

        // 4. RE-CALCULATE LEVEL GRAPH PATH
        GridManager grid = FindObjectOfType<GridManager>();
        if (grid != null)
        {
            grid.TracePath();
        }
    }

    void UpdateDirection()
    {
        if (currentDirection == Direction.Left)
            currentDirection = Direction.Up;
        else
            currentDirection++;
    }
}