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
    public int width = 15; // Set via inspector or default to 15
    public int height = 15; // Set via inspector or default to 15

    [Header("Level Balance")]
    [Range(0f, 1f)]
    public float hazardChance = 0.2f;

    // This 2D array stores our tile references
    public TileRotation[,] allTiles;

    [Header("Path Tracking Coordinates")]
    public Vector2Int startCoords;
    public Vector2Int endCoords;
    public List<Vector3> currentPathWorldPositions = new List<Vector3>();

    [Header("Dynamic Path Puzzle Constraints")]
    public int minPathLength = 15;
    public int maxPathLength = 30;

    [Tooltip("Coordinates that the enemy path trace MUST step through to be verified as valid.")]
    public List<Vector2Int> mandatoryCheckpoints = new List<Vector2Int>();

    [HideInInspector] public bool isCurrentPathValid = false;
    [HideInInspector] public string pathValidationErrorMessage = "";

    void Start()
    {
        allTiles = new TileRotation[width, height];

        // 1. Establish the very first baseline positions on startup
        RandomizeSpawnAndEndPositions();

        // 2. Generate the visual world matrix layout
        GenerateGrid();

        // 3. Apply starting color highlight identifiers
        RefreshEdgeVisualMarkers();

        // 4. Trace our direction connections
        TracePath();

        // Automatically scale the camera perspective based on the grid height
        Camera.main.orthographicSize = (height / 2f) + 1f;
    }

    /// <summary>
    /// Helper logic to isolate edge node selection calculations cleanly
    /// </summary>
    private void RandomizeSpawnAndEndPositions()
    {
        // Left side edge: Column 0, completely random row height allocation
        int randomStartY = Random.Range(0, height);
        startCoords = new Vector2Int(0, randomStartY);

        // Right side edge: Last Column (width - 1), completely random row height allocation
        int randomEndY = Random.Range(0, height);
        endCoords = new Vector2Int(width - 1, randomEndY);
    }

    /// <summary>
    /// Colors the start spawn green and the end base target destination red
    /// </summary>
    private void RefreshEdgeVisualMarkers()
    {
        if (allTiles[startCoords.x, startCoords.y] != null)
            allTiles[startCoords.x, startCoords.y].GetComponent<SpriteRenderer>().color = Color.green;

        if (allTiles[endCoords.x, endCoords.y] != null)
            allTiles[endCoords.x, endCoords.y].GetComponent<SpriteRenderer>().color = Color.red;
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
                    tileScript.gridX = x;
                    tileScript.gridY = y;
                    allTiles[x, y] = tileScript;

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

        for (int i = 0; i < (width * height); i++)
        {
            if (currentPos.x < 0 || currentPos.x >= width || currentPos.y < 0 || currentPos.y >= height)
            {
                break;
            }

            TileRotation currentTile = allTiles[currentPos.x, currentPos.y];

            if (pathList.Contains(currentTile))
            {
                break;
            }

            pathList.Add(currentTile);
            currentPathWorldPositions.Add(currentTile.transform.position);

            if (currentPos == endCoords)
            {
                goalReached = true;
                break;
            }

            currentPos = GetNextCoords(currentPos, currentTile.currentDirection);
        }

        HighlightPath(pathList);

        bool lengthValid = pathList.Count >= minPathLength && pathList.Count <= maxPathLength;

        bool checkpointsValid = true;
        foreach (Vector2Int checkpoint in mandatoryCheckpoints)
        {
            bool matchFound = false;
            foreach (TileRotation node in pathList)
            {
                if (node.gridX == checkpoint.x && node.gridY == checkpoint.y)
                {
                    matchFound = true;
                    break;
                }
            }
            if (!matchFound)
            {
                checkpointsValid = false;
                break;
            }
        }

        isCurrentPathValid = goalReached && lengthValid && checkpointsValid;

        if (!goalReached) pathValidationErrorMessage = "❌ PATH INCOMPLETE: Route does not reach base!";
        else if (pathList.Count < minPathLength) pathValidationErrorMessage = $"❌ PATH TOO SHORT: Minimum length is {minPathLength} tiles (Current: {pathList.Count}).";
        else if (pathList.Count > maxPathLength) pathValidationErrorMessage = $"❌ PATH TOO LONG: Maximum length is {maxPathLength} tiles (Current: {pathList.Count}).";
        else if (!checkpointsValid) pathValidationErrorMessage = "❌ MISSING CHECKPOINTS: Route bypasses mandatory waypoint tile markers!";
        else pathValidationErrorMessage = "✅ PATH STABLE & SECURE";

        Debug.Log($"[PATH CONSTRAINTS LOG] Validation State: {isCurrentPathValid.ToString().ToUpper()} | Message: {pathValidationErrorMessage}");
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

                if (isPath && tp.type == TileProperty.TileType.Normal)
                {
                    tile.GetComponent<SpriteRenderer>().color = Color.yellow;
                }
                else
                {
                    tp.RefreshVisuals(false);
                }
            }
        }
    }

    public void SwapTiles(TileRotation scriptA, TileRotation scriptB)
    {
        if (scriptA == null || scriptB == null) return;
        if (IsStartOrEnd(scriptA.gridX, scriptA.gridY) || IsStartOrEnd(scriptB.gridX, scriptB.gridY)) return;

        int ax = scriptA.gridX; int ay = scriptA.gridY;
        int bx = scriptB.gridX; int by = scriptB.gridY;

        Vector3 tempPos = scriptA.transform.position;
        scriptA.transform.position = scriptB.transform.position;
        scriptB.transform.position = tempPos;

        scriptA.gridX = bx; scriptA.gridY = by;
        scriptB.gridX = ax; scriptB.gridY = ay;

        allTiles[ax, ay] = scriptB;
        allTiles[bx, by] = scriptA;

        scriptA.name = $"Tile_{bx}_{by}";
        scriptB.name = $"Tile_{ax}_{ay}";

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

    /// <summary>
    /// Invoked dynamically by GameManager to advance level setup phases
    /// </summary>
    public void GenerateNewWaveConstraints(int waveNumber)
    {
        // 1. Scale path boundaries dynamically based on our 15x15 calibration parameters
        switch (waveNumber)
        {
            case 1: minPathLength = 15; maxPathLength = 30; break;
            case 2: minPathLength = 16; maxPathLength = 35; break;
            case 3: minPathLength = 18; maxPathLength = 40; break;
            case 4: minPathLength = 20; maxPathLength = 50; break;
            default: minPathLength = 22; maxPathLength = 60; break;
        }

        // 2. Clear out legacy checkpoints
        mandatoryCheckpoints.Clear();

        // 3. Select a new randomized mandatory midpoint checkpoint targeting middle columns (columns 3 to 11)
        int safetyTimeoutAttempts = 0;
        while (mandatoryCheckpoints.Count < 1 && safetyTimeoutAttempts < 200)
        {
            safetyTimeoutAttempts++;
            int rx = Random.Range(3, width - 3);
            int ry = Random.Range(1, height - 1);

            if (!IsStartOrEnd(rx, ry))
            {
                mandatoryCheckpoints.Add(new Vector2Int(rx, ry));
            }
        }

        // 4. Force calculation pass to update current UI warning error flags instantly
        TracePath();
    }
}