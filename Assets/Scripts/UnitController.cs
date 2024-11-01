using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UnitController : MonoBehaviour
{
    public float moveSpeed = 5f;
    private List<Vector3> path;
    private int targetIndex;

    Pathfinding pathfinding;

    void Start()
    {
        pathfinding = FindObjectOfType<Pathfinding>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            targetPosition.z = 0;
            RequestPath(targetPosition);
        }
    }

    void RequestPath(Vector3 targetPosition)
    {
        StopCoroutine("FollowPath");
        path = pathfinding.FindPath(transform.position, targetPosition);
        if (path != null && path.Count > 0)
        {
            targetIndex = 0;
            StartCoroutine("FollowPath");
        }
    }

    IEnumerator FollowPath()
    {
        Vector3 currentWaypoint = path[0];

        while (true)
        {
            if (transform.position == currentWaypoint)
            {
                targetIndex++;
                if (targetIndex >= path.Count)
                {
                    yield break;
                }
                currentWaypoint = path[targetIndex];
            }

            transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }
}