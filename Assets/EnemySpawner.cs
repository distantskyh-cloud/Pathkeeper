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
        if (gridManager.currentPathWorldPositions.Count > 0)
        {
            GameObject enemy = Instantiate(enemyPrefab, gridManager.currentPathWorldPositions[0], Quaternion.identity);
            enemy.GetComponent<EnemyPathFinding>().SetPath(gridManager.currentPathWorldPositions);
        }
    }
}