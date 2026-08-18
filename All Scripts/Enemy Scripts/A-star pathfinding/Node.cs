using UnityEngine;

public class Node : IHeapItem<Node>
{
    public bool walkable;
    public Vector3 worldPosition;
    public int gridX, gridY;
    public Node parent;
    public int gCost;
    public int hCost;
    private int heapIndex;
    public int movementPenalty;
    public Node(bool _walkable, Vector3 _worldPos, int _gridX, int _gridY, int _movementPenalty)
    {
        this.walkable = _walkable;
        this.worldPosition = _worldPos;
        this.gridX = _gridX;
        this.gridY = _gridY;
        this.movementPenalty = _movementPenalty;
    }
    public int fCost => gCost + hCost;

    public int HeapIndex
    {
        get => heapIndex;
        set => heapIndex = value;
    }
    public int CompareTo(Node other)
    {
        int compare = fCost.CompareTo(other.fCost);
        if (compare == 0) compare = hCost.CompareTo(other.hCost);
        return -compare;
    }
}
