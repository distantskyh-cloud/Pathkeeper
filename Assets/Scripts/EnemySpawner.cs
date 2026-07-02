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

    [Header("Runtime Tracker Info")]
    public int currentWaveIndex = 0;
    public int currentEnemyIndex = 0;
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
    }

    void Update()
    {
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
            else
            {
                Enemy[] enemiesOnField = FindObjectsOfType<Enemy>();

                if (enemiesOnField.Length == 0)
                {
                    isWaveActive = false;
                    Debug.Log($"[WAVE CLEAN CLEAR] All enemies from {currentWave.waveName} have been defeated!");

                    if (EconomyManager.Instance != null)
                    {
                        int completionBonus = 50;
                        EconomyManager.Instance.AddGold(completionBonus);
                        Debug.Log($"[ECONOMY] Awarded +{completionBonus}g Wave Clear Payout!");
                    }

                    currentEnemyIndex = 0;
                    currentWaveIndex++;

                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.AdvanceWave();
                    }
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

                // AUTOMATIC ASSIGNMENT INJECTION PIPELINE
                if (enemyScript.currentClass == Enemy.EnemyClass.Priest ||
                    enemyScript.currentClass == Enemy.EnemyClass.Supporter)
                {
                    if (activeEnemy.GetComponent<EnemyAbilities>() == null)
                    {
                        activeEnemy.AddComponent<EnemyAbilities>();
                    }
                }
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
        if (allWaves == null || allWaves.Count == 0 || index >= allWaves.Count)
        {
            Debug.Log("[VICTORY] All designed waves have been cleared!");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ChangeState(GameManager.GameState.Victory);
            }
            return;
        }

        currentWaveIndex = index;
        currentEnemyIndex = 0;
        spawnTimer = 0f;
        isWaveActive = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentWave = currentWaveIndex + 1;
            GameManager.Instance.totalWaves = allWaves.Count;
        }

        Debug.Log($"[WAVE STARTED] Now playing: {allWaves[currentWaveIndex].waveName} (Wave {currentWaveIndex + 1}/{allWaves.Count})");
    }

    public bool IsWaveRunning()
    {
        return isWaveActive;
    }

    void TriggerNextWave()
    {
        StartWave(currentWaveIndex + 1);
    }
}