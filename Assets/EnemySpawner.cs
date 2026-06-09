using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Unique Enemy Prefab Variants")]
    public GameObject swordsmanPrefab;
    public GameObject tankerPrefab;
    public GameObject roguePrefab;
    public GameObject priestPrefab;
    public GameObject supporterPrefab;
    public GameObject paladinPrefab;

    public float spawnInterval = 3f;
    private float timer;
    private GridManager gridManager;

    void Start()
    {
        gridManager = FindObjectOfType<GridManager>();
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
        if (gridManager == null || gridManager.currentPathWorldPositions == null || gridManager.currentPathWorldPositions.Count == 0) return;

        Vector3 spawnPos = gridManager.currentPathWorldPositions[0];

        // 1. Pick a random unit class profile index to simulate waves for now
        Enemy.EnemyClass randomClass = (Enemy.EnemyClass)Random.Range(0, System.Enum.GetValues(typeof(Enemy.EnemyClass)).Length);

        // 2. Select the matching distinct prefab asset container
        GameObject chosenPrefab = swordsmanPrefab;
        switch (randomClass)
        {
            case Enemy.EnemyClass.Swordsman: chosenPrefab = swordsmanPrefab; break;
            case Enemy.EnemyClass.Tanker: chosenPrefab = tankerPrefab; break;
            case Enemy.EnemyClass.Rogue: chosenPrefab = roguePrefab; break;
            case Enemy.EnemyClass.Priest: chosenPrefab = priestPrefab; break;
            case Enemy.EnemyClass.Supporter: chosenPrefab = supporterPrefab; break;
            case Enemy.EnemyClass.Paladin: chosenPrefab = paladinPrefab; break;
        }

        // 3. Instantiate the exact pre-configured asset archetype cleanly
        GameObject enemy = Instantiate(chosenPrefab, spawnPos, Quaternion.identity);

        Enemy enemyScript = enemy.GetComponent<Enemy>();
        EnemyPathFinding movementScript = enemy.GetComponent<EnemyPathFinding>();

        if (enemyScript != null) enemyScript.InitializeEnemy(randomClass);
        if (movementScript != null) movementScript.SetPath(gridManager.currentPathWorldPositions);
    }
}