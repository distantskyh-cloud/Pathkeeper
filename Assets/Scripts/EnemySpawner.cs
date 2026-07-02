using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    public GameObject[] enemyPrefabs;   // Drag multiple enemy prefabs here

    public float spawnInterval = 3f;

    private float timer;
    private GridManager gridManager;

    void Start()
    {
        gridManager = FindObjectOfType<GridManager>();

        if (gridManager == null)
        {
            Debug.LogError("[SPAWNER ERROR] Could not find GridManager!");
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
        if (gridManager == null) return;

        if (gridManager.currentPathWorldPositions == null ||
            gridManager.currentPathWorldPositions.Count == 0)
        {
            Debug.LogWarning("No valid path found.");
            return;
        }

        if (enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("No enemy prefabs assigned!");
            return;
        }

        Vector3 spawnPos = gridManager.currentPathWorldPositions[0];

        // Pick a random prefab
        GameObject selectedPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        // Spawn it
        GameObject enemy = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);

        // Get scripts
        Enemy enemyScript = enemy.GetComponent<Enemy>();
        EnemyPathFinding movementScript = enemy.GetComponent<EnemyPathFinding>();

        if (enemyScript == null)
            enemyScript = enemy.AddComponent<Enemy>();

        if (movementScript == null)
            movementScript = enemy.AddComponent<EnemyPathFinding>();

        // Initialize using the prefab's assigned class
        enemyScript.InitializeEnemy(enemyScript.currentClass);
        movementScript.SetPath(gridManager.currentPathWorldPositions, enemyScript.currentClass);

        Debug.Log($"Spawned {enemyScript.currentClass}");
    }
}