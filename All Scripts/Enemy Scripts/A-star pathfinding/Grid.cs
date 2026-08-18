using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
public class Grid : NetworkBehaviour
{ // class: contains grid data and pathfinding methods
    Node[,] grid;
    public Vector2 gridWorldSize;
    public float nodeRadius;
    public LayerMask unwalkableMask;
    float nodeDiameter;
    int gridSizeX, gridSizeY;
    public bool displayGridGizmos;
    public TerrainType[] walkableRegions;
    Dictionary<int, int> walkableRegionsDictionary = new Dictionary<int, int>();
    LayerMask walkableMask;
    int penaltyMin = int.MaxValue;
    int penaltyMax = int.MinValue;

    [System.Serializable]
    public class TerrainType
    { // Each layer has a default movement penalty
        public LayerMask terrainMask;
        public int terrainPenalty;
    }
    private void Awake()
    { // Initialize size, masks, and create grid
        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

        foreach (TerrainType region in walkableRegions) 
        { // Add walkable regions to bit mask and dictionary
            walkableMask.value |= region.terrainMask.value;
            walkableRegionsDictionary.Add((int)Mathf.Log(region.terrainMask.value, 2), region.terrainPenalty); // map layer->penalty
        }
        CreateGrid();
    }
    public int MaxSize => gridSizeX * gridSizeY;
    void CreateGrid()
    { // Declare nodes for entire grid
        grid = new Node[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft =
            transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;
        for (int countX = 0; countX < gridSizeX; countX++)
        { // iterate columns (x)
            for (int countY = 0; countY < gridSizeY; countY++)
            { // iterate rows (y), (x,y) represents coordinate of node on grid
                Vector3 worldPoint =
                    worldBottomLeft
                    + Vector3.right * (countX * nodeDiameter + nodeRadius)
                    + Vector3.forward * (countY * nodeDiameter + nodeRadius);
                bool walkable = !Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask);
                int movementPenalty = 0;
                Ray ray = new(worldPoint + Vector3.up * 50, Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hit, 100, walkableMask))
                { // set movement penalty of node (keep height at y = 0)
                    walkableRegionsDictionary.TryGetValue(hit.collider.gameObject.layer, out movementPenalty);
                }
                // Force node height to 0 regardless of raycast hit
                worldPoint.y = 0f;
                grid[countX, countY] = new Node(walkable, worldPoint, countX, countY, movementPenalty);
            }
        }
        BlurPenaltyMap(3);
    }

    void BlurPenaltyMap(int blurSize)   //blurSize: kernel radius
    { // Smoothen out movement penalties based on neighbouring nodes
        int kernelSize = blurSize * 2 + 1;
        int[,] penaltiesHP = new int[gridSizeX, gridSizeY];
        int[,] penaltiesVP = new int[gridSizeX, gridSizeY];

        for (int y = 0; y < gridSizeY; y++)
        { // horizontal blur: for each row
            for (int x = -kernelSize; x <= kernelSize; x++)
            { // accumulate initial horizontal kernel
                int sampleX = Mathf.Clamp(x, 0, kernelSize);
                penaltiesHP[0, y] += grid[sampleX, y].movementPenalty;
            }

            for (int x = 1; x < gridSizeX; x++)
            { // slide horizontal window across row
                int removeIndex = Mathf.Clamp(x - kernelSize - 1, 0, gridSizeX);
                int addIndex = Mathf.Clamp(x + kernelSize, 0, gridSizeX - 1);

                penaltiesHP[x, y] = penaltiesHP[x - 1, y]
                - grid[removeIndex, y].movementPenalty 
                + grid[addIndex, y].movementPenalty;
            }
        }
        // Use results from horizontal blur in vertical passes
        for (int x = 0; x < gridSizeX; x++)
        { // vertical blur: for each column
            for (int y = -kernelSize; y <= kernelSize; y++)
            { // accumulate initial vertical kernel
                int sampleY = Mathf.Clamp(y, 0, kernelSize);
                penaltiesVP[x, 0] += penaltiesHP[x, sampleY];
            }
            // Penalty of node on top column
            int blurredPenalty = Mathf.RoundToInt((float)penaltiesVP[x, 0] / (kernelSize * kernelSize));
            grid[x, 0].movementPenalty = blurredPenalty;

            for (int y = 1; y < gridSizeY; y++)
            { // slide vertical window down column
                int removeIndex = Mathf.Clamp(y - kernelSize - 1, 0, gridSizeY);
                int addIndex = Mathf.Clamp(y + kernelSize, 0, gridSizeY - 1);

                penaltiesVP[x, y] = penaltiesVP[x, y - 1] 
                - penaltiesHP[x, removeIndex] + penaltiesHP[x, addIndex];
                blurredPenalty = Mathf.RoundToInt((float)penaltiesVP[x, y] / (kernelSize * kernelSize));
                grid[x, y].movementPenalty = blurredPenalty;

                if (blurredPenalty > penaltyMax) penaltyMax = blurredPenalty;
                if (blurredPenalty < penaltyMin) penaltyMin = blurredPenalty;
            }
        }
    }
    public List<Node> GetNeighbours(Node node)
    { // Collect neighbour nodes around a node
        List<Node> neighbours = new();
        for (int x = -1; x <= 1; x++)
        { // iterate neighbour x offset
            for (int y = -1; y <= 1; y++)
            { // iterate neighbour y offset
                if (x == 0 && y == 0)
                    continue;
                int testX = node.gridX + x;
                int testY = node.gridY + y;
                if (testX >= 0 && testX < gridSizeX && testY >= 0 && testY < gridSizeY)
                {
                    neighbours.Add(grid[testX, testY]);
                }
            }
        }
        return neighbours;
    }

    public Node NodeFromWorldPoint(Vector3 worldPosition)
    { // Get node on grid corresponding to world position
        float percentX = (worldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x;
        float percentY = (worldPosition.z + gridWorldSize.y / 2) / gridWorldSize.y;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
        return grid[x, y];
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Draw overall grid bounds
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1f, gridWorldSize.y));

        if (!displayGridGizmos) return;
        if (grid == null) return;

        // Draw each node. Color by walkability and movement penalty.
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Node n = grid[x, y];
                if (n == null) continue;

                if (!n.walkable)
                {
                    Gizmos.color = Color.red;
                }
                else
                {
                    // Normalize penalty into 0..1 safely
                    int range = Mathf.Max(1, penaltyMax - penaltyMin);
                    float t = (float)(n.movementPenalty - penaltyMin) / range;
                    Gizmos.color = Color.Lerp(Color.white, Color.green, t);
                }
                n.worldPosition.y = 0f;
                Vector3 size = Vector3.one * (nodeDiameter - 0.05f);
                Gizmos.DrawCube(n.worldPosition, size);
            }
        }
    }
#endif
}
