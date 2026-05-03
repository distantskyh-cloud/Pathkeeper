using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public GameObject tilePrefab;
    public int width = 5;
    public int height = 5;

    // This 2D array stores our tile references
    public TileRotation[,] allTiles;

    public Vector2Int startCoords = new Vector2Int(0, 0);
    public Vector2Int endCoords = new Vector2Int(4, 4);

    public void TracePath()
    {
        List<TileRotation> pathList = new List<TileRotation>();
        Vector2Int currentPos = startCoords;
        bool goalReached = false;

        // We limit the loop to the total number of tiles to prevent infinite crashes
        for (int i = 0; i < (width * height); i++)
        {
            // 1. Check if current position is within grid boundaries
            if (currentPos.x < 0 || currentPos.x >= width || currentPos.y < 0 || currentPos.y >= height)
            {
                Debug.Log("Path went out of bounds!");
                break;
            }

            TileRotation currentTile = allTiles[currentPos.x, currentPos.y];

            // 2. Check for Infinite Loops
            if (pathList.Contains(currentTile))
            {
                Debug.Log("Infinite Loop detected!");
                break;
            }

            pathList.Add(currentTile);

            // 3. Check if we reached the Goal
            if (currentPos == endCoords)
            {
                goalReached = true;
                break;
            }

            // 4. Move to the next coordinate based on the tile's currentDirection
            currentPos = GetNextCoords(currentPos, currentTile.currentDirection);

            Debug.Log($"Tracer at {currentPos} is moving {currentTile.currentDirection}");
        }

        HighlightPath(pathList);

        if (goalReached) Debug.Log("Path to Goal is VALID!");
        else Debug.Log("Path is incomplete.");
    }

    Vector2Int GetNextCoords(Vector2Int pos, TileRotation.Direction dir)
    {
        if (dir == TileRotation.Direction.Up) return new Vector2Int(pos.x, pos.y + 1);
        if (dir == TileRotation.Direction.Right) return new Vector2Int(pos.x + 1, pos.y);
        if (dir == TileRotation.Direction.Down) return new Vector2Int(pos.x, pos.y - 1);
        if (dir == TileRotation.Direction.Left) return new Vector2Int(pos.x - 1, pos.y);
        return pos;
    }

    void HighlightPath(List<TileRotation> path)
    {
        foreach (TileRotation tile in allTiles)
        {
            // Don't reset the Start and End colors
            if (tile == allTiles[startCoords.x, startCoords.y] || tile == allTiles[endCoords.x, endCoords.y])
                continue;

            tile.GetComponent<SpriteRenderer>().color = Color.white;
        }

        foreach (TileRotation tile in path)
        {
            // Don't turn the Start/End tiles yellow
            if (tile == allTiles[startCoords.x, startCoords.y] || tile == allTiles[endCoords.x, endCoords.y])
                continue;

            tile.GetComponent<SpriteRenderer>().color = Color.yellow;
        }
    }
    void Start()
    {
        allTiles = new TileRotation[width, height];

        // Randomize the Y positions
        int randomStartY = Random.Range(0, height);
        int randomEndY = Random.Range(0, height);

        startCoords = new Vector2Int(0, randomStartY);
        endCoords = new Vector2Int(width - 1, randomEndY); // Use width-1 so it works for any grid size

        GenerateGrid();

        // Visual indicator so we can see the new Start/End
        allTiles[startCoords.x, startCoords.y].GetComponent<SpriteRenderer>().color = Color.green;
        allTiles[endCoords.x, endCoords.y].GetComponent<SpriteRenderer>().color = Color.red;
    }

    void GenerateGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject newTile = Instantiate(tilePrefab, new Vector3(x, y, 0), Quaternion.identity);
                newTile.transform.parent = this.transform;

                // Store the TileRotation component in our array
                allTiles[x, y] = newTile.GetComponent<TileRotation>();
                // Give the tile its coordinates so it knows where it is
                allTiles[x, y].gridX = x;
                allTiles[x, y].gridY = y;
            }
        }
    }
}