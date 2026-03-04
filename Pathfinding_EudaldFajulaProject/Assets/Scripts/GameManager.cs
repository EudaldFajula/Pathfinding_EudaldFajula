using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int Size;
    public BoxCollider2D Panel;
    public GameObject token;        // Default token (all nodes)
    public GameObject tokenStart;   // Token for start node
    public GameObject tokenEnd;     // Token for end node
    public GameObject tokenPath;    // Token for path nodes

    private Node[,] NodeMatrix;
    private int startPosx, startPosy;
    private int endPosx, endPosy;

    private GameObject[,] spawnedTokens;

    void Awake()
    {
        Instance = this;
        Calculs.CalculateDistances(Panel, Size);
    }

    private void Start()
    {
        startPosx = Random.Range(0, Size);
        startPosy = Random.Range(0, Size);
        do
        {
            endPosx = Random.Range(0, Size);
            endPosy = Random.Range(0, Size);
        } while (endPosx == startPosx && endPosy == startPosy);

        NodeMatrix = new Node[Size, Size];
        spawnedTokens = new GameObject[Size, Size];

        CreateNodes();
    }

    public float SpawnDelay = 0.05f; // delay between each token spawn (seconds)
    public float PathDelay = 0.08f; // delay between each path node highlight (seconds)

    public void CreateNodes()
    {
        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                NodeMatrix[i, j] = new Node(i, j, Calculs.CalculatePoint(i, j));
                NodeMatrix[i, j].Heuristic = Calculs.CalculateHeuristic(NodeMatrix[i, j], endPosx, endPosy);
            }
        }

        for (int i = 0; i < Size; i++)
            for (int j = 0; j < Size; j++)
                SetWays(NodeMatrix[i, j], i, j);

        StartCoroutine(SpawnAndPathfind());
    }

    private IEnumerator SpawnAndPathfind()
    {
        //Spawn all nodes one by one
        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                spawnedTokens[i, j] = Instantiate(token, NodeMatrix[i, j].RealPosition, Quaternion.identity);
                yield return new WaitForSeconds(SpawnDelay);
            }
        }

        //Mark start and end
        ReplaceToken(startPosx, startPosy, tokenStart);
        ReplaceToken(endPosx, endPosy, tokenEnd);

        //Run path node by node
        List<Node> path = FindPath(NodeMatrix[startPosx, startPosy], NodeMatrix[endPosx, endPosy]);

        if (path != null)
        {
            Debug.Log($"Path found! {path.Count} nodes.");
            foreach (Node node in path)
            {
                Debug.Log($"  ({node.PositionX}, {node.PositionY})");

                bool isStart = node.PositionX == startPosx && node.PositionY == startPosy;
                bool isEnd = node.PositionX == endPosx && node.PositionY == endPosy;
                if (!isStart && !isEnd)
                    ReplaceToken(node.PositionX, node.PositionY, tokenPath);

                yield return new WaitForSeconds(PathDelay);
            }
        }
        else
        {
            Debug.LogWarning("No path found.");
        }
    }
    private List<Node> FindPath(Node startNode, Node endNode)
    {
        List<Node> openList = new List<Node>();
        HashSet<Node> closedList = new HashSet<Node>();

        Dictionary<Node, float> gCost = new Dictionary<Node, float>();
        Dictionary<Node, float> fCost = new Dictionary<Node, float>();

        startNode.NodeParent = null;
        gCost[startNode] = 0f;
        fCost[startNode] = startNode.Heuristic;
        openList.Add(startNode);

        while (openList.Count > 0)
        {
            Node current = openList.OrderBy(n => fCost.ContainsKey(n) ? fCost[n] : float.MaxValue).First();

            if (current == endNode)
                return ReconstructPath(endNode);

            openList.Remove(current);
            closedList.Add(current);

            foreach (Way way in current.WayList)
            {
                Node neighbor = way.NodeDestiny;
                if (closedList.Contains(neighbor)) continue;

                float tentativeG = gCost[current] + way.Cost;
                if (!gCost.ContainsKey(neighbor) || tentativeG < gCost[neighbor])
                {
                    neighbor.NodeParent = current;
                    gCost[neighbor] = tentativeG;
                    fCost[neighbor] = tentativeG + neighbor.Heuristic;
                    way.ACUMulatedCost = tentativeG;

                    if (!openList.Contains(neighbor))
                        openList.Add(neighbor);
                }
            }
        }

        return null;
    }

    private List<Node> ReconstructPath(Node endNode)
    {
        List<Node> path = new List<Node>();
        Node current = endNode;
        while (current != null)
        {
            path.Add(current);
            current = current.NodeParent;
        }
        path.Reverse();
        return path;
    }

    #region Helpers

    private void ReplaceToken(int x, int y, GameObject prefab)
    {
        if (prefab == null) return;
        Destroy(spawnedTokens[x, y]);
        spawnedTokens[x, y] = Instantiate(prefab, NodeMatrix[x, y].RealPosition, Quaternion.identity);
    }

    public void DebugMatrix()
    {
        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                Debug.Log($"Element ({j}, {i}) | Pos: {NodeMatrix[i, j].RealPosition} | H: {NodeMatrix[i, j].Heuristic}");
                foreach (var way in NodeMatrix[i, j].WayList)
                    Debug.Log($"  -> ({way.NodeDestiny.PositionX}, {way.NodeDestiny.PositionY})");
            }
        }
    }

    public void SetWays(Node node, int x, int y)
    {
        node.WayList = new List<Way>();

        if (x > 0)
        {
            node.WayList.Add(new Way(NodeMatrix[x - 1, y], Calculs.LinearDistance));
            if (y > 0)
                node.WayList.Add(new Way(NodeMatrix[x - 1, y - 1], Calculs.DiagonalDistance));
        }
        if (x < Size - 1)
        {
            node.WayList.Add(new Way(NodeMatrix[x + 1, y], Calculs.LinearDistance));
            if (y > 0)
                node.WayList.Add(new Way(NodeMatrix[x + 1, y - 1], Calculs.DiagonalDistance));
        }
        if (y > 0)
        {
            node.WayList.Add(new Way(NodeMatrix[x, y - 1], Calculs.LinearDistance));
        }
        if (y < Size - 1)
        {
            node.WayList.Add(new Way(NodeMatrix[x, y + 1], Calculs.LinearDistance));
            if (x > 0)
                node.WayList.Add(new Way(NodeMatrix[x - 1, y + 1], Calculs.DiagonalDistance));
            if (x < Size - 1)
                node.WayList.Add(new Way(NodeMatrix[x + 1, y + 1], Calculs.DiagonalDistance));
        }
    }
    #endregion
}