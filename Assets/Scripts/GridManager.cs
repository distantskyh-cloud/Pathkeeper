using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Modular Tile Prefab References")]
    public GameObject Tile_Normal;
    public GameObject Tile_Slow;
    public GameObject Tile_Burn;
    public GameObject Tile_Freeze;
    public GameObject Tile_Pitfall;
    public GameObject Tile_Poison;
    public GameObject Tile_Static;
    public GameObject Tile_Bleed; // Handles unified Spike + Bleed payload!
    public GameObject Tile_Curse;

    [Header("Grid Dimensions")]
    public int width;
    public int height;

    [Header("Level Balance")]
    [Range(0f, 1f)]
    public float hazardChance = 0.2f;

    // This 2D array stores our tile references
    public TileRotation[,] allTiles;

    [Header("Path Tracking Coordinates")]
    public Vector2Int startCoords = new Vector2Int(0, 0);
    public Vector2Int endCoords = new Vector2Int(4, 4);
    public List<Vector3> currentPathWorldPositions = new List<Vector3>();

    void Start()
    {
        allTiles = new TileRotation[width, height];

        // Randomize the Y positions for Start and End points
        int randomStartY = Random.Range(0, height);
        int randomEndY = Random.Range(0, height);

        startCoords = new Vector2Int(0, randomStartY);
        endCoords = new Vector2Int(width - 1, randomEndY); // Works dynamically for any grid width

        GenerateGrid();

        // Apply starting visual highlight identifiers
        if (allTiles[startCoords.x, startCoords.y] != null)
            allTiles[startCoords.x, startCoords.y].GetComponent<SpriteRenderer>().color = Color.green;

        if (allTiles[endCoords.x, endCoords.y] != null)
            allTiles[endCoords.x, endCoords.y].GetComponent<SpriteRenderer>().color = Color.red;

        TracePath();

        // Automatically scale the camera perspective based on the grid height
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
                // 1. DETERMINE PREFAB TYPE FIRST BEFORE INSTANTIATING
                GameObject prefabToSpawn = Tile_Normal;

                // Randomly select a hazard variant if constraints match
                if (Random.value < hazardChance && !IsStartOrEnd(x, y))
                {
                    // Random choice mapping to the updated TileType enum sequence (skipping 'Normal' at 0)
                    int randomHazard = Random.Range(1, System.Enum.GetValues(typeof(TileProperty.TileType)).Length);
                    TileProperty.TileType targetType = (TileProperty.TileType)randomHazard;

                    switch (targetType)
                    {
                        case TileProperty.TileType.Slow: prefabToSpawn = Tile_Slow; break;
                        case TileProperty.TileType.Burn: prefabToSpawn = Tile_Burn; break;
                        case TileProperty.TileType.Freeze: prefabToSpawn = Tile_Freeze; break;
                        case TileProperty.TileType.Pitfall: prefabToSpawn = Tile_Pitfall; break;
                        case TileProperty.TileType.Poison: prefabToSpawn = Tile_Poison; break;
                        case TileProperty.TileType.Static: prefabToSpawn = Tile_Static; break;
                        case TileProperty.TileType.Bleed: prefabToSpawn = Tile_Bleed; break;
                        case TileProperty.TileType.Curse: prefabToSpawn = Tile_Curse; break;
                    }
                }

                // 2. INSTANTIATE THE CORRECT CHOSEN PREFAB
                Vector3 spawnPos = new Vector3(x - xOffset, y - yOffset, 0);
                GameObject newTile = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
                newTile.name = $"Tile_{x}_{y}";

                TileRotation tileScript = newTile.GetComponent<TileRotation>();

                if (tileScript != null)
                {
                    // Assign coordinate address tracking values
                    tileScript.gridX = x;
                    tileScript.gridY = y;
                    allTiles[x, y] = tileScript;

                    // Determine and assign orientation rules
                    TileRotation.Direction finalDir;

                    if (x == startCoords.x && y == startCoords.y)
                    {
                        finalDir = GetValidStartDirection(x, y);
                    }
                    else if (x == endCoords.x && y == endCoords.y)
                    {
                        finalDir = GetValidEndDirection(x, y);
                    }
                    else
                    {
                        finalDir = (TileRotation.Direction)Random.Range(0, 4);
                    }

                    tileScript.currentDirection = finalDir;
                    newTile.transform.eulerAngles = new Vector3(0, 0, (int)finalDir * -90f);
                }

                // Activate Goal Indicator overlay if it matches the destination node
                if (x == endCoords.x && y == endCoords.y)
                {
                    Transform goal = newTile.transform.Find("GoalIndicator");
                    if (goal != null)
                    {
                        goal.gameObject.SetActive(true);
                    }
                }
            }
        }
    }

    public void TracePath()
    {
        List<TileRotation> pathList = new List<TileRotation>();
        currentPathWorldPositions.Clear();
        Vector2Int currentPos = startCoords;
        bool goalReached = false;

        // Limit loop iterations to prevent infinite crashes
        for (int i = 0; i < (width * height); i++)
        {
            // 1. Boundary integrity safety check
            if (currentPos.x < 0 || currentPos.x >= width || currentPos.y < 0 || currentPos.y >= height)
            {
                Debug.Log("Path went out of bounds!");
                break;
            }

            TileRotation currentTile = allTiles[currentPos.x, currentPos.y];

            // 2. Loop repetition catch
            if (pathList.Contains(currentTile))
            {
                Debug.Log("Infinite Loop detected!");
                break;
            }

            pathList.Add(currentTile);
            currentPathWorldPositions.Add(currentTile.transform.position);

            // 3. Goal objective check
            if (currentPos == endCoords)
            {
                goalReached = true;
                break;
            }

            // 4. Update coordinates based on exit vectors
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
            if (tile == null || IsStartOrEnd(tile.gridX, tile.gridY)) continue;

            TileProperty tp = tile.GetComponent<TileProperty>();
            if (tp != null)
            {
                bool isPath = path.Contains(tile);

                // ONLY change color to yellow if it is a running path element AND a Normal tile type.
                if (isPath && tp.type == TileProperty.TileType.Normal)
                {
                    tile.GetComponent<SpriteRenderer>().color = Color.yellow;
                }
                else
                {
                    // Revert non-path or hazard tiles cleanly back to native colors
                    tp.RefreshVisuals(false);
                }
            }
        }
    }

    public void SwapTiles(TileRotation scriptA, TileRotation scriptB)
    {
        // 1. Safety Gate Check
        if (scriptA == null || scriptB == null) return;
        if (IsStartOrEnd(scriptA.gridX, scriptA.gridY) || IsStartOrEnd(scriptB.gridX, scriptB.gridY)) return;

        // Cache original placement keys
        int ax = scriptA.gridX;
        int ay = scriptA.gridY;
        int bx = scriptB.gridX;
        int by = scriptB.gridY;

        // 2. Swap Physical World Positions
        Vector3 tempPos = scriptA.transform.position;
        scriptA.transform.position = scriptB.transform.position;
        scriptB.transform.position = tempPos;

        // 3. Swap Internal Address Memory Variables
        scriptA.gridX = bx;
        scriptA.gridY = by;
        scriptB.gridX = ax;
        scriptB.gridY = ay;

        // 4. Update the Master Matrix Pointer Map
        allTiles[ax, ay] = scriptB;
        allTiles[bx, by] = scriptA;

        // 5. Update Hierarchy Names for Clearer Inspector Debugging
        scriptA.name = $"Tile_{bx}_{by}";
        scriptB.name = $"Tile_{ax}_{ay}";

        // 6. Force Dynamic Grid Direction Re-calculation
        TracePath();

        Debug.Log($"[GRID SUCCESS] Physically swapped {scriptA.name} with {scriptB.name}");
    }

    TileRotation.Direction GetValidStartDirection(int x, int y)
    {
        List<TileRotation.Direction> validDirections = new List<TileRotation.Direction>
        {
            TileRotation.Direction.Up,
            TileRotation.Direction.Right,
            TileRotation.Direction.Down,
            TileRotation.Direction.Left
        };

        validDirections.Remove(TileRotation.Direction.Left);

        if (y == 0) validDirections.Remove(TileRotation.Direction.Down);
        if (y == height - 1) validDirections.Remove(TileRotation.Direction.Up);

        int randomIndex = Random.Range(0, validDirections.Count);
        return validDirections[randomIndex];
    }

    TileRotation.Direction GetValidEndDirection(int x, int y)
    {
        return TileRotation.Direction.Right;
    }

    bool IsStartOrEnd(int x, int y)
    {
        return (x == startCoords.x && y == startCoords.y) || (x == endCoords.x && y == endCoords.y);
    }
}