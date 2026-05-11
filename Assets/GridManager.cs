using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public GameObject tilePrefab;
    public int width;
    public int height;

    [Header("Level Balance")]
    [Range(0f, 1f)]
    public float hazardChance = 0.2f;

    // Tile effect master settings
    [Header("Master Hazard Balancer")]
    // Spikes: High instant damage, no speed change
    public TileProperty.HazardData spikeStats = new TileProperty.HazardData { damage = 10f, speedMult = 1f, dotDamage = 0f, duration = 0f };

    // Pitfall: The "Delete" button
    public TileProperty.HazardData pitfallStats = new TileProperty.HazardData { damage = 999f, speedMult = 0f, dotDamage = 0f, duration = 0f };

    // Slow: No damage, cuts speed in half
    public TileProperty.HazardData slowStats = new TileProperty.HazardData { damage = 0f, speedMult = 0.5f, dotDamage = 0f, duration = 0f };

    // Freeze: Stops unit completely for a moment
    public TileProperty.HazardData freezeStats = new TileProperty.HazardData { damage = 0f, speedMult = 0f, dotDamage = 0f, duration = 1.5f };

    // Burn: Low instant damage, high DoT for a short time
    public TileProperty.HazardData burnStats = new TileProperty.HazardData { damage = 5f, speedMult = 1f, dotDamage = 4f, duration = 3f };

    // Poison: No instant damage, but lasts "forever" (-1)
    public TileProperty.HazardData poisonStats = new TileProperty.HazardData { damage = 0f, speedMult = 1f, dotDamage = 1f, duration = -1f };

    // Static: Slight slow and medium DoT
    public TileProperty.HazardData staticStats = new TileProperty.HazardData { damage = 2f, speedMult = 0.8f, dotDamage = 2f, duration = 2f };

    // Bleed: No instant damage, but movement-based (Logic handled in Unit script)
    public TileProperty.HazardData bleedStats = new TileProperty.HazardData { damage = 0f, speedMult = 1f, dotDamage = 3f, duration = 5f };

    // Curse: No damage, but sets a flag for 1.5x damage taken
    public TileProperty.HazardData curseStats = new TileProperty.HazardData { damage = 0f, speedMult = 1f, dotDamage = 0f, duration = 10f };

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
            if (IsStartOrEnd(tile.gridX, tile.gridY)) continue;

            bool isPath = path.Contains(tile);
            tile.GetComponent<TileProperty>().RefreshVisuals(isPath);
        }
    }

    TileRotation.Direction GetValidStartDirection(int x, int y)
    {
        // Create a list of all 4 possible directions
        List<TileRotation.Direction> validDirections = new List<TileRotation.Direction>
    {
        TileRotation.Direction.Up,
        TileRotation.Direction.Right,
        TileRotation.Direction.Down,
        TileRotation.Direction.Left
    };

        // 1. Always remove Left (since x is 0)
        validDirections.Remove(TileRotation.Direction.Left);

        // 2. If at the very bottom, remove Down
        if (y == 0)
            validDirections.Remove(TileRotation.Direction.Down);

        // 3. If at the very top, remove Up
        if (y == height - 1)
            validDirections.Remove(TileRotation.Direction.Up);

        // 4. Pick a random direction from the remaining safe options
        int randomIndex = Random.Range(0, validDirections.Count);
        return validDirections[randomIndex];
    }

    TileRotation.Direction GetValidEndDirection(int x, int y)
    {
        // For the End Tile, we usually want it pointing 'Off-screen' to the Right
        return TileRotation.Direction.Right;

        // If you want it to be random but safe:
        // Follow the same List.Remove logic as the Start tile, 
        // but remove 'Right' instead of 'Left'.
    }

    bool IsStartOrEnd(int x, int y)
    {
        return (x == startCoords.x && y == startCoords.y) || (x == endCoords.x && y == endCoords.y);
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

        TracePath();

        // This automatically scales the camera based on the grid height
        Camera.main.orthographicSize = (height / 2f) + 1f;
    }

    void GenerateGrid()
    {
        float xOffset = (width - 1) / 2f;
        float yOffset = (height - 1) / 2f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 spawnPos = new Vector3(x - xOffset, y - yOffset, 0);
                GameObject newTile = Instantiate(tilePrefab, spawnPos, Quaternion.identity);

                TileRotation tileScript = newTile.GetComponent<TileRotation>();

                TileRotation.Direction finalDir;

                if (x == startCoords.x && y == startCoords.y)
                {
                    // Use our special safe logic for the start tile
                    finalDir = GetValidStartDirection(x, y);
                }
                else
                {
                    // Use the standard 0-3 random for everything else
                    finalDir = (TileRotation.Direction)Random.Range(0, 4);
                }

                // Apply the direction to the script and the rotation
                tileScript.currentDirection = finalDir;
                newTile.transform.eulerAngles = new Vector3(0, 0, (int)finalDir * -90f);
                // -------------------------------

                if (x == endCoords.x && y == endCoords.y)
                {
                    // Find the child and turn "GoalIndicator" on
                    Transform goal = newTile.transform.Find("GoalIndicator");
                    if (goal != null) goal.gameObject.SetActive(true);
                }

                // Example for randomizing all types in GridManager
                if (Random.value < hazardChance && !IsStartOrEnd(x, y))
                {
                    // Picks a random hazard from the Enum (skipping 'Normal' at index 0)
                    int randomHazard = Random.Range(1, System.Enum.GetValues(typeof(TileProperty.TileType)).Length);
                    newTile.GetComponent<TileProperty>().SetType((TileProperty.TileType)randomHazard);

                    TileProperty tp = newTile.GetComponent<TileProperty>();

                    switch (tp.type)
                    {
                        case TileProperty.TileType.Spike: tp.currentData = spikeStats; break;
                        case TileProperty.TileType.Slow: tp.currentData = slowStats; break;
                        case TileProperty.TileType.Burn: tp.currentData = burnStats; break;
                        case TileProperty.TileType.Freeze: tp.currentData = freezeStats; break;
                        case TileProperty.TileType.Pitfall: tp.currentData = pitfallStats; break;
                        case TileProperty.TileType.Poison: tp.currentData = poisonStats; break;
                        case TileProperty.TileType.Static: tp.currentData = staticStats; break;
                        case TileProperty.TileType.Bleed: tp.currentData = bleedStats; break;
                        case TileProperty.TileType.Curse: tp.currentData = curseStats; break;
                    }
                }

                tileScript.gridX = x;
                tileScript.gridY = y;
                allTiles[x, y] = tileScript;
            }
        }
    }
}