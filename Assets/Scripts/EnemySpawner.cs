using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public struct Wave
    {
        public string waveName;
        [Tooltip("The exact sequence of enemy prefabs that will spawn during this wave.")]
        public List<GameObject> enemyPrefabsToSpawn;
        [Tooltip("Time in seconds between each enemy spawn in this wave.")]
        public float spawnInterval;
    }

    [Header("Wave Design Layouts")]
    public List<Wave> allWaves;

    [Header("Runtime Tracker Info (Read Only)")]
    public int currentWaveIndex = 0;
    private int currentEnemyIndex = 0;
    private float spawnTimer;
    private bool isWaveActive = false;

    private GridManager gridManager;

    void Start()
    {
        gridManager = FindObjectOfType<GridManager>();

        if (gridManager == null)
        {
            Debug.LogError("[SPAWNER ERROR] Could not find GridManager in the scene!");
        }
        // DELETED: StartWave(0) was removed from here so it doesn't fire on empty lists!
    }

    void Update()
    {
        // 1. If a wave is active and still has enemies to drop, handle the timer
        if (isWaveActive && allWaves != null && currentWaveIndex < allWaves.Count)
        {
            Wave currentWave = allWaves[currentWaveIndex];

            if (currentEnemyIndex < currentWave.enemyPrefabsToSpawn.Count)
            {
                spawnTimer += Time.deltaTime;
                if (spawnTimer >= currentWave.spawnInterval)
                {
                    spawnTimer = 0f;
                    SpawnNextEnemyFromWave(currentWave);
                }
            }
            // 2. If we finished spawning, check if the board is completely clear of enemies
            else
            {
                // Find all active enemies currently alive on the map
                Enemy[] enemiesOnField = FindObjectsOfType<Enemy>();

                if (enemiesOnField.Length == 0)
                {
                    isWaveActive = false;
                    Debug.Log($"[WAVE CLEAN CLEAR] All enemies from {currentWave.waveName} have been defeated! Preparing next wave...");

                    // Trigger intermission delay only when the field is 100% empty
                    Invoke(nameof(TriggerNextWave), 5f);
                }
            }
        }
    }

    void SpawnNextEnemyFromWave(Wave wave)
    {
        if (gridManager == null || gridManager.currentPathWorldPositions == null || gridManager.currentPathWorldPositions.Count == 0)
        {
            Debug.LogWarning("[SPAWNER SKIP] Path tracing coordinates are empty!");
            return;
        }

        // REMOVED: The old immediate completion check has been deleted from here!

        GameObject selectedPrefab = wave.enemyPrefabsToSpawn[currentEnemyIndex];

        if (selectedPrefab != null)
        {
            Vector3 spawnPos = gridManager.currentPathWorldPositions[0];
            GameObject activeEnemy = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);

            Enemy enemyScript = activeEnemy.GetComponent<Enemy>();
            EnemyPathFinding movementScript = activeEnemy.GetComponent<EnemyPathFinding>();

            if (enemyScript != null)
            {
                enemyScript.InitializeEnemy(enemyScript.currentClass);
            }

            if (movementScript != null)
            {
                movementScript.SetPath(gridManager.currentPathWorldPositions);
            }

            Debug.Log($"[SPAWNED] {activeEnemy.name} has entered the field ({currentEnemyIndex + 1}/{wave.enemyPrefabsToSpawn.Count}).");
        }

        currentEnemyIndex++;
    }

    public void StartWave(int index)
    {
        // If the player cleared everything OR if they forgot to design waves in the inspector
        if (allWaves == null || allWaves.Count == 0 || index >= allWaves.Count)
        {
            Debug.Log("[VICTORY] All designed waves have been cleared!");
            if (GameManager.Instance != null)
            {
                // FIXED: Changed your variable assignment loop to a clean State change call
                GameManager.Instance.ChangeState(GameManager.GameState.Victory);
            }
            return;
        }

        currentWaveIndex = index;
        currentEnemyIndex = 0;
        spawnTimer = 0f;
        isWaveActive = true;

        // Sync back to our developer GameManager tracker metrics
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentWave = currentWaveIndex + 1;
            GameManager.Instance.totalWaves = allWaves.Count;
        }

        Debug.Log($"[WAVE STARTED] Now playing: {allWaves[currentWaveIndex].waveName} (Wave {currentWaveIndex + 1}/{allWaves.Count})");
    }

    void TriggerNextWave()
    {
        StartWave(currentWaveIndex + 1);
    }
}