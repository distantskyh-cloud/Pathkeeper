using UnityEngine;
using System.Collections.Generic;

public class EnemyPathFinding : MonoBehaviour
{
    public float speed = 1f;
    private List<Vector3> localPathPoints = new List<Vector3>(); // Private independent path copy
    private int targetIndex = 0;

    public void SetPath(List<Vector3> masterPath)
    {
        if (masterPath == null || masterPath.Count == 0) return;

        localPathPoints = new List<Vector3>(masterPath);
        targetIndex = 0;

        transform.position = localPathPoints[0];
        // REMOVED THE SWITCH STATEMENT THAT OVERRIDES SPEED!
    }

    private float spawnDelayTimer = 0.5f; // Wait half a second before moving

    void Update()
    {
        // 1. Let the enemy exist for a few frames before applying pathing movement
        if (spawnDelayTimer > 0)
        {
            spawnDelayTimer -= Time.deltaTime;
            return;
        }

        // Safety lock to prevent index crashes
        if (localPathPoints == null || localPathPoints.Count == 0 || targetIndex >= localPathPoints.Count) return;

        transform.position = Vector3.MoveTowards(transform.position, localPathPoints[targetIndex], speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, localPathPoints[targetIndex]) < 0.1f)
        {
            targetIndex++;

            // ARRIVAL CHECKPOINT GATE:
            if (targetIndex >= localPathPoints.Count)
            {
                // Unit successfully cleared the labyrinth track path!
                Enemy enemyComponent = GetComponent<Enemy>();
                if (enemyComponent != null)
                {
                    // Cleanse status completely right before damage base transitions execute
                    enemyComponent.ResetStatusEffects();
                }

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.DamageBase(1);
                }

                Destroy(gameObject);
            }
        }
    }
}