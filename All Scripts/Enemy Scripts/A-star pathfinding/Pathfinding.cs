using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using System.Collections;
using System.Diagnostics;
using System;
using System.Linq;
public class Pathfinding : NetworkBehaviour
{
    Grid grid;
    PathRequestManager requestManager;
    private void Awake()
    {   // Reference grid and path manager
        grid = GetComponent<Grid>();
        requestManager = GetComponent<PathRequestManager>();
    }
    public void StartFindPath(Vector3 start, Vector3 end)
    {   // Start the pathfinding coroutine
        StartCoroutine(FindPath(start, end));
    }
    IEnumerator FindPath(Vector3 start, Vector3 end)
    {   // Declare sets and start/end nodes
        Vector3[] waypoints = new Vector3[0];
        bool pathSuccess = false;
        Node startNode = grid.NodeFromWorldPoint(start);
        Node endNode = grid.NodeFromWorldPoint(end);
        Heap<Node> openSet = new(grid.MaxSize);
        HashSet<Node> closedSet = new();
        openSet.Add(startNode);
        while (openSet.Count > 0)
        {   // Repeat until there is no more nodes to analyse
            Node currentNode = openSet.RemoveFirst();
            closedSet.Add(currentNode);
            if (currentNode == endNode) 
            {   // Path has been found
                pathSuccess = true;
                break;
            }
            foreach (Node neighbour in grid.GetNeighbours(currentNode))
            {   // Look at nodes around the current node, find the best path
                if (!neighbour.walkable || closedSet.Contains(neighbour)) continue;
                int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour) + neighbour.movementPenalty; 
                if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                {   // Update the node's costs and parent if it is a better path
                    neighbour.gCost = newMovementCostToNeighbour;
                    neighbour.hCost = GetDistance(neighbour, endNode);
                    neighbour.parent = currentNode;
                    if (!openSet.Contains(neighbour)) openSet.Add(neighbour);
                    else openSet.UpdateItem(neighbour);
                }
            }
        }
        yield return null;  
        if (pathSuccess) waypoints = RetracePath(startNode, endNode);
        requestManager.FinishedProcessingPath(waypoints, pathSuccess);
    }

    Vector3[] RetracePath(Node startNode, Node endNode)
    {   // Retrace the path from end to start
        List<Node> path = new();
        Node currentNode = endNode;
        while (currentNode != startNode)
        {   // Use linked-list based logic to find each node in the path
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        Vector3[] waypoints = SimplifyPath(path);
        Array.Reverse(waypoints);
        return waypoints;
    }
    Vector3[] SimplifyPath(List<Node> path)
    {   // Remove nodes if they are in a straight line
        List<Vector3> waypoints = new();
        Vector2 oldDirection = Vector2.zero;
        for (int i = 1; i < path.Count; i++)
        {   // Add nodes that cause a change in direction
            Vector2 newDirection = new(path[i - 1].gridX - path[i].gridX, path[i - 1].gridY - path[i].gridY);
            if (newDirection != oldDirection) waypoints.Add(path[i].worldPosition);
            oldDirection = newDirection;
        }
        return waypoints.ToArray();
    }
    int GetDistance(Node nodeA, Node nodeB)
    {   // Manhattan distance, which includes diagonals, good for grid based movement
        int distX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int distY = Mathf.Abs(nodeA.gridY - nodeB.gridY);
        return 14 * Mathf.Min(distX, distY) + (Mathf.Max(distX, distY) - Mathf.Min(distX,distY)) * 10;
    }   
}
   