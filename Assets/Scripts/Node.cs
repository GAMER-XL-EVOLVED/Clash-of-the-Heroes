using UnityEngine;

public class Node
{
    public bool walkable;
    public Vector3Int position;
    public Node parent;
    public int gCost, hCost;

    public int fCost => gCost + hCost;

    public Node(bool _walkable, Vector3Int _position)
    {
        walkable = _walkable;
        position = _position;
    }
}
