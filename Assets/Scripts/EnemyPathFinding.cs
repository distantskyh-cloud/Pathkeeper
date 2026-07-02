using UnityEngine;
using System.Collections.Generic;

public class EnemyPathFinding : MonoBehaviour
{
    [HideInInspector] public float speed = 1f;
    private List<Vector3> localPathPoints = new List<Vector3>(); // Private independent path copy
    private int targetIndex = 0;

    public void SetPath(List<Vector3> masterPath, Enemy.EnemyClass enemyClass)
    {
        if (masterPath == null || masterPath.Count == 0) return;

        // FIX: Create an independent clone copy so GridManager.Clear() doesn't delete it!
        localPathPoints = new List<Vector3>(masterPath);
        targetIndex = 0;

        transform.position = localPathPoints[0];

        switch (enemyClass)
        {
            case Enemy.EnemyClass.Swordsman: speed = 1.0f; break;
            case Enemy.EnemyClass.Tanker: speed = 0.6f; break;
            case Enemy.EnemyClass.Rogue: speed = 1.8f; break;
        }
    }

    void Update()
    {
        // Safety lock to prevent index crashes
        if (localPathPoints == null || localPathPoints.Count == 0 || targetIndex >= localPathPoints.Count) return;

        transform.position = Vector3.MoveTowards(transform.position, localPathPoints[targetIndex], speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, localPathPoints[targetIndex]) < 0.1f)
        {
            targetIndex++;
        }

        if (targetIndex >= localPathPoints.Count)
        {
            Destroy(gameObject);
        }
    }
}