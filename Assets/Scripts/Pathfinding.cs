using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class Pathfinding : MonoBehaviour
{
    public Tilemap walkableTilemap;
    public Tilemap obstaclesTilemap;
    
    private Grid grid;

    void Awake()
    {
        grid = walkableTilemap.layoutGrid;
    }

    public List<Vector3> FindPath(Vector3 startWorldPos, Vector3 targetWorldPos)
    {
        Vector3Int startCell = grid.WorldToCell(startWorldPos);
        Vector3Int targetCell = grid.WorldToCell(targetWorldPos);

        Node startNode = new Node(true, startCell);
        Node targetNode = new Node(true, targetCell);

        List<Node> openSet = new List<Node>();
        HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode.position);

            if (currentNode.position == targetNode.position)
            {
                return RetracePath(startNode, currentNode);
            }

            foreach (Vector3Int neighborPos in GetNeighbors(currentNode.position))
            {
                if (closedSet.Contains(neighborPos) || !IsWalkable(neighborPos))
                    continue;

                Node neighbor = new Node(true, neighborPos);
                int newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);

                if (newMovementCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newMovementCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        return null;
    }

    List<Vector3> RetracePath(Node startNode, Node endNode)
    {
        List<Vector3> path = new List<Vector3>();
        Node currentNode = endNode;

        while (currentNode.position != startNode.position)
        {
            path.Add(grid.GetCellCenterWorld(currentNode.position));
            currentNode = currentNode.parent;
        }
        path.Reverse();
        return path;
    }

    int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.position.x - nodeB.position.x);
        int dstY = Mathf.Abs(nodeA.position.y - nodeB.position.y);

        if (dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }

    List<Vector3Int> GetNeighbors(Vector3Int position)
    {
        List<Vector3Int> neighbors = new List<Vector3Int>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                Vector3Int checkPos = new Vector3Int(position.x + x, position.y + y, position.z);
                neighbors.Add(checkPos);
            }
        }

        return neighbors;
    }

    bool IsWalkable(Vector3Int position)
    {
        return walkableTilemap.HasTile(position) && !obstaclesTilemap.HasTile(position);
    }
}