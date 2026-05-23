using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public GameObject tilePrefab;
    public int width = 5;
    public int height = 5;

    [Header("Level Balance")]
    [Range(0f, 1f)]
    public float hazardChance = 0.1f;

    public TileRotation[,] allTiles;

    public Vector2Int startCoords = new Vector2Int(0, 0);
    public Vector2Int endCoords = new Vector2Int(4, 4);
    public List<Vector3> currentPathWorldPositions = new List<Vector3>();

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
                Debug.Log("Path went out of bounds!");
                break;
            }

            TileRotation currentTile = allTiles[currentPos.x, currentPos.y];

            if (pathList.Contains(currentTile))
            {
                Debug.Log("Infinite Loop detected!");
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
        List<TileRotation.Direction> validDirections = new List<TileRotation.Direction>
        {
            TileRotation.Direction.Up,
            TileRotation.Direction.Right,
            TileRotation.Direction.Down,
            TileRotation.Direction.Left
        };

        validDirections.Remove(TileRotation.Direction.Left);

        if (y == 0)
            validDirections.Remove(TileRotation.Direction.Down);

        if (y == height - 1)
            validDirections.Remove(TileRotation.Direction.Up);

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

    void Start()
    {
        allTiles = new TileRotation[width, height];

        int randomStartY = Random.Range(0, height);
        int randomEndY = Random.Range(0, height);

        startCoords = new Vector2Int(0, randomStartY);
        endCoords = new Vector2Int(width - 1, randomEndY);

        GenerateGrid();

        allTiles[startCoords.x, startCoords.y].GetComponent<SpriteRenderer>().color = Color.green;
        allTiles[endCoords.x, endCoords.y].GetComponent<SpriteRenderer>().color = Color.red;

        TracePath();

        Camera.main.orthographicSize = (height / 2f) + 1f;

        // Force the enemy spawner to reset its clock now that path coordinates exist
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.ResetSpawner();
        }
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
                    finalDir = GetValidStartDirection(x, y);
                }
                else
                {
                    finalDir = (TileRotation.Direction)Random.Range(0, 4);
                }

                tileScript.currentDirection = finalDir;
                newTile.transform.eulerAngles = new Vector3(0, 0, (int)finalDir * -90f);

                if (x == endCoords.x && y == endCoords.y)
                {
                    Transform goal = newTile.transform.Find("GoalIndicator");
                    if (goal != null) goal.gameObject.SetActive(true);
                }

                if (Random.value < hazardChance && !IsStartOrEnd(x, y))
                {
                    newTile.GetComponent<TileProperty>().SetType(TileProperty.TileType.Spike);
                }

                tileScript.gridX = x;
                tileScript.gridY = y;
                allTiles[x, y] = tileScript;
            }
        }
    }
}