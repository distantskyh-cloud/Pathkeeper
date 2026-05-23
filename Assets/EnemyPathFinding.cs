using UnityEngine;
using System.Collections.Generic;

public class EnemyPathFinding : MonoBehaviour
{
    public float speed = 2f;
    private List<Vector3> pathPoints;
    private int targetIndex = 0;

    public void SetPath(List<Vector3> newPath)
    {
        pathPoints = new List<Vector3>(newPath);
        targetIndex = 0;
        if (pathPoints.Count > 0)
        {
            transform.position = pathPoints[0];
        }
    }

    void Update()
    {
        if (pathPoints == null || targetIndex >= pathPoints.Count) return;

        // Move smoothly across the flat 2D plane
        transform.position = Vector3.MoveTowards(transform.position, pathPoints[targetIndex], speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, pathPoints[targetIndex]) < 0.1f)
        {
            targetIndex++;
        }

        if (targetIndex >= pathPoints.Count)
        {
            Destroy(gameObject);
        }
    }
}
