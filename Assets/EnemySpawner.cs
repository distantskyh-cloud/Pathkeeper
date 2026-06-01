using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float spawnInterval = 3f;
    private float timer;
    private GridManager gridManager;

    void Start()
    {
        gridManager = FindObjectOfType<GridManager>();

        if (gridManager == null)
        {
            Debug.LogError("[SPAWNER ERROR] Could not find GridManager in the scene! Make sure it exists.");
        }
        else
        {
            Debug.Log("[SPAWNER INITIALIZED] Successfully linked to GridManager.");
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnEnemy();
            timer = 0;
        }
    }

    void SpawnEnemy()
    {
        if (gridManager == null)
        {
            Debug.LogWarning("[SPAWNER SKIP] Cannot spawn enemy because GridManager is null.");
            return;
        }

        if (gridManager.currentPathWorldPositions != null && gridManager.currentPathWorldPositions.Count > 0)
        {
            // This is the line that was missing!
            Vector3 spawnPos = gridManager.currentPathWorldPositions[0];

            // 1. Instantiate the object safely at the spawn position
            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

            // 2. Select a random class
            Enemy.EnemyClass randomClass = (Enemy.EnemyClass)Random.Range(0, System.Enum.GetValues(typeof(Enemy.EnemyClass)).Length);

            // 3. Setup and link scripts safely
            Enemy enemyScript = enemy.GetComponent<Enemy>();
            if (enemyScript == null) enemyScript = enemy.AddComponent<Enemy>();

            EnemyPathFinding movementScript = enemy.GetComponent<EnemyPathFinding>();
            if (movementScript == null) movementScript = enemy.AddComponent<EnemyPathFinding>();

            // 4. Initialize parameters (Enemy FIRST, then Pathfinding)
            enemyScript.InitializeEnemy(randomClass);
            movementScript.SetPath(gridManager.currentPathWorldPositions);

            // --- TRACKING DEBUG LOG ---
            Debug.Log($"[SPAWNER SUCCESS] Spawned {randomClass} at world coordinates: {spawnPos}. Path points count: {gridManager.currentPathWorldPositions.Count}");
        }
        else
        {
            Debug.LogWarning($"[SPAWNER WARNING] Cannot spawn enemy. 'currentPathWorldPositions' is either null or empty! Count: {(gridManager.currentPathWorldPositions != null ? gridManager.currentPathWorldPositions.Count : -1)}");
        }
    }
}