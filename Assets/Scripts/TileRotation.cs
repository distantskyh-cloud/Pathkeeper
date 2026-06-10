using UnityEngine;

public class TileRotation : MonoBehaviour
{
    // A list of possible directions
    public enum Direction { Up, Right, Down, Left }
    public Direction currentDirection;

    // Add these so the tile knows its 'address'
    [HideInInspector] public int gridX;
    [HideInInspector] public int gridY;

    // Inside TileRotation.cs
    private void OnMouseDown()
    {
        transform.Rotate(0, 0, -90f);
        UpdateDirection();

        // Find the GridManager in the scene and tell it to re-scan the path
        FindObjectOfType<GridManager>().TracePath();
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