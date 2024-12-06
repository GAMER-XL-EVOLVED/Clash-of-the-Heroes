using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class UnitController : MonoBehaviour
{
    public float moveSpeed = 5f;
    private List<Vector3> path;
    private int targetIndex;
    public GameManager.Team team;

    Pathfinding pathfinding;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        pathfinding = FindObjectOfType<Pathfinding>();

        // Register with GameManager based on team
        if (team == GameManager.Team.Blue)
        {
            GameManager.Instance.blueTeamUnits.Add(this);
        }
        else
        {
            GameManager.Instance.redTeamUnits.Add(this);
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateVisuals();
    }

    void OnDestroy()
    {
        // Unregister from GameManager
        if (team == GameManager.Team.Blue)
        {
            GameManager.Instance.blueTeamUnits.Remove(this);
        }
        else
        {
            GameManager.Instance.redTeamUnits.Remove(this);
        }
    }

    void Update()
    {
        // Check if mouse is over UI element
        if (Input.GetMouseButtonDown(0) && GameManager.Instance.IsUnitsTurn(this) && !IsPointerOverUI())
        {
            Vector3 targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            targetPosition.z = 0;
            RequestPath(targetPosition);
        }

        UpdateVisuals();
    }

    private bool IsPointerOverUI()
    {
        // Check if the mouse is over a UI element
        return UnityEngine.EventSystems.EventSystem.current != null && 
               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
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

    void UpdateVisuals()
    {
        if (spriteRenderer != null)
        {
            // Dim the sprite if it's not this unit's turn
            Color color = spriteRenderer.color;
            color.a = GameManager.Instance.IsUnitsTurn(this) ? 1f : 0.5f;
            spriteRenderer.color = color;
        }
    }
}