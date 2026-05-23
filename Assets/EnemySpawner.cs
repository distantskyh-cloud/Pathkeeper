using UnityEngine;
using System.Collections.Generic;

public enum EnemyType { Vanguard, Tank, Thief }

[System.Serializable]
public struct EnemyData
{
    public EnemyType type;
    public GameObject visualPrefab;
    public int hp;
    public float speed;
    public float damageReduction;
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Setup")]
    public GameObject enemyBasePrefab;
    public float spawnInterval = 3f;

    [Header("Enemy Configurations")]
    public List<EnemyData> enemyTypes = new List<EnemyData>();

    private float timer;
    private GridManager gridManager;
    private bool isReadyToSpawn = false;

    void Start()
    {
        gridManager = FindObjectOfType<GridManager>();
    }

    public void ResetSpawner()
    {
        timer = 0f;
        isReadyToSpawn = true;
    }

    void Update()
    {
        if (!isReadyToSpawn || gridManager == null || gridManager.currentPathWorldPositions.Count == 0)
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnEnemy();
            timer = 0;
        }
    }

    void SpawnEnemy()
    {
        if (gridManager != null && gridManager.allTiles != null)
        {
            // 1. Get the exact random start coordinates chosen by the GridManager
            int startX = gridManager.startCoords.x;
            int startY = gridManager.startCoords.y;

            // 2. Extract the actual Tile object from the grid array
            TileRotation startTile = gridManager.allTiles[startX, startY];

            if (startTile != null && enemyTypes.Count > 0)
            {
                EnemyData randomType = enemyTypes[Random.Range(0, enemyTypes.Count)];

                // 3. Match the tile's exact visual world position precisely
                Vector3 spawnPosition = startTile.transform.position;

                // 4. Instantiation
                GameObject enemy = Instantiate(enemyBasePrefab, spawnPosition, Quaternion.identity);

                if (randomType.visualPrefab != null)
                {
                    GameObject visual = Instantiate(randomType.visualPrefab, enemy.transform);
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = Quaternion.identity;

                    SpriteRenderer sr = visual.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sortingOrder = 50; // Lock rendering priority over tiles
                    }
                }

                EnemyPathFinding pathfinding = enemy.GetComponent<EnemyPathFinding>();
                if (pathfinding != null)
                {
                    pathfinding.SetPath(gridManager.currentPathWorldPositions);
                    pathfinding.speed = randomType.speed;
                }

                EnemyStats stats = enemy.GetComponent<EnemyStats>();
                if (stats != null)
                {
                    stats.Initialize(randomType.hp, randomType.damageReduction);
                }
            }
        }
    }
}