using System;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    // Nested types.
    public class Map
    {
        // Overloads.
        public Node this[int x, int y]
        {
            get
            {
                return _VALUES[x, y];
            }
            set
            {
                _VALUES[x, y] = value;
            }
        }

        // Constructors.
        public Map(Texture2D input, float emptyCost, float wallCost, float slowDownCost, Color emptyColor, Color wallColor, Color slowDownColor, Color startColor, Color targetColor)
        {
            WIDTH = input.width;
            HEIGHT = input.height;

            _VALUES = new Node[WIDTH, HEIGHT];

            // Fill out Node array.
            Color color;
            for (int x = 0; x < WIDTH; x++)
            {
                for (int y = 0; y < HEIGHT; y++)
                {
                    color = input.GetPixel(x,y);
                    if (color == emptyColor)
                    {
                        _VALUES[x, y] = new Node(emptyCost, new Vector2Int(x,y));
                    }
                    else if (color == wallColor)
                    {
                        _VALUES[x, y] = new Node(wallCost, new Vector2Int(x, y));
                    }
                    else if (color == slowDownColor)
                    {
                        _VALUES[x, y] = new Node(slowDownCost, new Vector2Int(x, y));
                    }
                    else if (color == startColor)
                    {
                        _VALUES[x, y] = new Node(emptyCost, new Vector2Int(x, y));
                        STARTING_NODE = _VALUES[x, y];
                    }
                    else if (color == targetColor)
                    {
                        _VALUES[x, y] = new Node(emptyCost, new Vector2Int(x, y));
                        TARGET_NODE = _VALUES[x, y];
                    }
                    else
                    {
                        Debug.LogError("Unexpected color in input texture: " + color.ToString());
                    }
                }
            }

            // Check for inconsistencies.
            if (STARTING_NODE == null) Debug.LogError("Starting node is not set!");
            if (TARGET_NODE == null) Debug.LogError("Target node is not set!");
            if (STARTING_NODE == TARGET_NODE) Debug.LogError("Starting and target nodes are the same!");

            // Calculate heuristics (h variable) for all nodes.
            for (int x = 0; x < WIDTH; x++)
            {
                for (int y = 0; y < HEIGHT; y++)
                {
                    _VALUES[x, y].h = Vector2Int.Distance(TARGET_NODE.POSITION, _VALUES[x,y].POSITION);
                }
            }
        }

        // Public methods.
        /// <summary>
        /// Returns array of valid neighbors and boolean values to tell whether the neighbor was located diagonally from the input node or not.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public Tuple<Node, bool>[] GetNeighborsOf(Node node, bool allowDiagonalMovement)
        {
            var neighborPositions = new List<Tuple<Node, bool>>();

            // Fill return list with cardinal neighbors.
            if (node.POSITION.x - 1 >= 0)
            {
                // Left.
                neighborPositions.Add(new Tuple<Node, bool>(_VALUES[node.POSITION.x - 1, node.POSITION.y], false));
            }
            if (node.POSITION.x + 1 <= WIDTH - 1)
            {
                // Right.
                neighborPositions.Add(new Tuple<Node, bool>(_VALUES[node.POSITION.x + 1, node.POSITION.y], false));
            }
            if (node.POSITION.y - 1 >= 0)
            {
                // Top.
                neighborPositions.Add(new Tuple<Node, bool>(_VALUES[node.POSITION.x, node.POSITION.y - 1], false));
            }
            if (node.POSITION.y + 1 <= HEIGHT - 1)
            {
                // Bottom.
                neighborPositions.Add(new Tuple<Node, bool>(_VALUES[node.POSITION.x, node.POSITION.y + 1], false));
            }

            // Fill return list with diagonal neighbors if applicable.
            if (allowDiagonalMovement)
            {
                if (node.POSITION.x - 1 >= 0 && node.POSITION.y - 1 >= 0)
                {
                    // Top-Left.
                    neighborPositions.Add(new Tuple<Node, bool>(_VALUES[node.POSITION.x - 1, node.POSITION.y - 1], true));
                }
                if (node.POSITION.x + 1 <= WIDTH - 1 && node.POSITION.y + 1 <= HEIGHT - 1)
                {
                    // Bottom-Right.
                    neighborPositions.Add(new Tuple<Node, bool>(_VALUES[node.POSITION.x + 1, node.POSITION.y + 1], true));
                }
                if (node.POSITION.x - 1 >= 0 && node.POSITION.y + 1 <= HEIGHT - 1)
                {
                    // BottomLeft.
                    neighborPositions.Add(new Tuple<Node, bool>(_VALUES[node.POSITION.x - 1, node.POSITION.y + 1], true));
                }
                if (node.POSITION.x + 1 <= WIDTH - 1 && node.POSITION.y - 1 >= 0)
                {
                    // Top-Right.
                    neighborPositions.Add(new Tuple<Node, bool>(_VALUES[node.POSITION.x + 1, node.POSITION.y - 1], true));
                }
            }

            // Return resulting array.
            return neighborPositions.ToArray();
        }

        // Public const fields.
        public readonly int WIDTH, HEIGHT;
        public readonly Node STARTING_NODE;
        public readonly Node TARGET_NODE;

        // Private fields.
        readonly Node[,] _VALUES;
    }

    public class Node
    {
        // Constructors.
        public Node(float traversalCost, Vector2Int position)
        {
            TRAVERSAL_COST = traversalCost;
            POSITION = position;
            g = float.PositiveInfinity;
        }

        // Public const fields.
        public readonly float TRAVERSAL_COST;
        public readonly Vector2Int POSITION;

        // Public fields and properties.
        public float TotalCost
        {
            get
            {
                return g + h;
            }
        }
        public float h;
        public float g;
        public Node parent;
    }

    // Serialized fields.
    [SerializeField] Texture2D input = null;
    // Display options.
    [SerializeField] bool drawOpenSet = false;
    [SerializeField] bool drawClosedSet = false;
    [SerializeField] bool drawPath = false;
    [SerializeField] bool drawParents = false;
    [SerializeField] Vector2 tileSize = Vector2.one;
    [SerializeField] [Range(0.0f, 1.0f)] float tileTransparency = 0.5f; // Used to reduce transparency of openSet, closedSet and path tiles.
    // Parameters.
    [SerializeField] bool ignoreDiagonalCostIncrease = false; // Turn on to weigh diagonal movements identically to cardinal ones.
    [SerializeField] bool disableStepByStep = false; // Whether or not to display each subsequent step of pathdinding or do the whole calculation at once.
    [SerializeField] float wallCost = 10000.0f;
    [SerializeField] float emptyCost = 1.0f;
    [SerializeField] float slowDownCost = 5.0f;
    [SerializeField] bool allowDiagonalMovement = true;
    // Colors.
    [SerializeField] Color startColor = Color.green;
    [SerializeField] Color targetColor = Color.red;
    [SerializeField] Color pathColor = Color.yellow;
    [SerializeField] Color slowDownColor = Color.cyan;
    [SerializeField] Color wallColor = Color.black;
    [SerializeField] Color emptyColor = Color.white;
    [SerializeField] Color openSetColor = new Color(0.5f, 0.5f, 1.0f, 1.0f);
    [SerializeField] Color closedSetColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);

    // Const fields.
    const float ROOT_OF_TWO = 1.41421f; // Used to take into account the increased cost of traveling diagonally.

    // Private fields.
    Map map = null;
    int count = 0; // Limit pathfinding function iterations to this number. Increments via "Next" button.
    Vector2[] path = null;
    Node[] openSet = null;
    Node[] closedSet = null;

    // Inherited methods.
    private void Start()
    {
        ResetMap();
    }
    void OnDrawGizmos()
    {
        if (map != null)
        {
            // Draw base map.
            var tileCost = 0.0f;
            for (int x = 0; x < map.WIDTH; x++)
            {
                for (int y = 0; y < map.HEIGHT; y++)
                {
                    tileCost = map[x, y].TRAVERSAL_COST;
                    if (tileCost == emptyCost) // Empty or special tile.
                    {
                        if (map[x, y] == map.STARTING_NODE) // Starting node.
                        {
                            Gizmos.color = startColor;
                            Gizmos.DrawCube(new Vector3(x, y), tileSize);
                        }
                        else if (map[x, y] == map.TARGET_NODE) // Target node.
                        {
                            Gizmos.color = targetColor;
                            Gizmos.DrawCube(new Vector3(x, y), tileSize);
                        }
                        else
                        {
                            Gizmos.color = emptyColor;
                            Gizmos.DrawCube(new Vector3(x, y), tileSize);
                        }
                    }
                    else if (tileCost == wallCost) // Wall.
                    {
                        Gizmos.color = wallColor;
                        Gizmos.DrawCube(new Vector3(x, y), tileSize);
                    }
                    else if (tileCost == slowDownCost) // SlowDown area.
                    {
                        Gizmos.color = slowDownColor;
                        Gizmos.DrawCube(new Vector3(x, y), tileSize);
                    }
                    else
                    {
                        Debug.LogError("Unknown cost for tile:" + tileCost);
                    }

                    // Draw parents of nodes.
                    if (drawParents)
                    {
                        if (map[x, y].parent != null)
                        {
                            Gizmos.color = pathColor * tileTransparency;
                            Gizmos.DrawLine((Vector2)map[x, y].POSITION, (Vector2)map[x, y].parent.POSITION);
                        }
                    }
                }
            }

            // Pathfinding tiles.
            if (drawClosedSet && closedSet != null)
            {
                Gizmos.color = closedSetColor * tileTransparency;
                foreach (var item in closedSet)
                {
                    Gizmos.DrawCube((Vector2)item.POSITION, tileSize);
                }
            }
            if (drawOpenSet && openSet != null)
            {
                Gizmos.color = openSetColor * tileTransparency;
                foreach (var item in openSet)
                {
                    Gizmos.DrawCube((Vector2)item.POSITION, tileSize);
                }
            }
            if (drawPath && path != null)
            {
                Gizmos.color = pathColor * tileTransparency;
                foreach (var item in path)
                {
                    Gizmos.DrawCube(item, tileSize);
                }
            }
        }
    }
    private void OnGUI()
    {
        GUILayout.Label("Iteration: " + count);
        if (GUILayout.Button("Next"))
        {
            RunPathfinding();
        }
        if (GUILayout.Button("Reset"))
        {
            ResetMap();
        }
    }
    private void Update()
    {
        if (Input.GetKey(KeyCode.RightArrow))
        {
            RunPathfinding();
        }
    }

    // Private methods.
    void RunPathfinding()
    {
        // Calculate path.
        Tuple<Node[], Node[], Node> returnValue = AStar(++count, disableStepByStep);

        // Separate KeyValuePair into two lists for drawing if needed.
        if (drawOpenSet)
        {
            openSet = returnValue.Item1;
        }
        if (drawClosedSet)
        {
            closedSet = returnValue.Item2;
        }

        // Rewind path from last current node.
        path = RewindFromNode(returnValue.Item3);
    }
    void ResetMap()
    {
        openSet = null;
        closedSet = null;
        path = null;
        count = 0;
        map = new Map(input, emptyCost, wallCost, slowDownCost, emptyColor, wallColor, slowDownColor, startColor, targetColor);
    }
    /// <summary>
    /// Used to retreive the path found via pathfinding using Node's parents fields.
    /// </summary>
    Vector2[] RewindFromNode(Node currentNode)
    {
        if (currentNode != null)
        {
            List<Vector2> path = new List<Vector2>();

            while (currentNode.parent != null)
            {
                path.Add(currentNode.POSITION);
                currentNode = currentNode.parent;
            }

            return path.ToArray();
        }
        else
        {
            Debug.LogError("Node provided to RewindFromNode is null!");
            return null;
        }
    }

    // Pathfinding methods.
    /// <summary>
    /// Returns a tuple composed of the openSet, closedSet and the last current node in the iteration of the algorithm.
    /// </summary>
    /// <param name="cycles">Limit on the number of iterations the algorithm is allowed to go through before returning.</param>
    /// <param name="disableStepByStep">Whether or not to limit the algorithm on the number of iterations it may make.</param>
    /// <returns>Tuple composed of openSet, closedSet and last current node in the iteration of the algorithm.</returns>
    Tuple<Node[], Node[], Node> AStar(int cycles, bool disableStepByStep)
    {
        // Init sets.
        List<Node> openSet = new List<Node>();
        List<Node> closedSet = new List<Node>();

        // Add starting node to set and initialize it.
        Node current = map.STARTING_NODE;
        current.g = 0;
        openSet.Add(current);

        // Start the iterative loop.
        while (openSet.Count > 0 && (disableStepByStep || cycles-- > 0))
        {
            // Move the currently lowest costing node from the openList to the current node.
            current = openSet[0];
            openSet.Remove(current);

            // Early exit if a path is found.
            if (current == map.TARGET_NODE) goto RETURN_RESULT;

            // Expand current node.
            var newCost = 0.0f;
            foreach (var child in map.GetNeighborsOf(current, allowDiagonalMovement))
            {
                if (!closedSet.Contains(child.Item1))
                {
                    // Calculate the new cost for traveling to this neighbor.
                    if (ignoreDiagonalCostIncrease)
                    {
                        newCost = current.g + child.Item1.TRAVERSAL_COST;
                    }
                    else
                    {
                        newCost = current.g + child.Item1.TRAVERSAL_COST * (child.Item2 ? ROOT_OF_TWO : 1); // Apply a ROOT_OF_TWO multiplier to traversal cost if moving diagonally.
                    }

                    if (child.Item1.g >= newCost)
                    {
                        child.Item1.g = newCost;
                        child.Item1.parent = current;

                        if (!openSet.Contains(child.Item1))
                        {
                            openSet.Add(child.Item1);
                        }
                    }
                }
            }

            // Move current node to closedSet and sort the openSet for the next iteration.
            closedSet.Add(current);
            openSet.Sort((x, y) => x.TotalCost.CompareTo(y.TotalCost));
        }

        RETURN_RESULT:
            return new Tuple<Node[], Node[], Node>(openSet.ToArray(), closedSet.ToArray(), current);
    }

    static public void drawString(string text, Vector3 worldPos, Color? colour = null)
    {
        UnityEditor.Handles.BeginGUI();

        var restoreColor = GUI.color;

        if (colour.HasValue) GUI.color = colour.Value;
        var view = UnityEditor.SceneView.currentDrawingSceneView;
        Vector3 screenPos = view.camera.WorldToScreenPoint(worldPos);

        if (screenPos.y < 0 || screenPos.y > Screen.height || screenPos.x < 0 || screenPos.x > Screen.width || screenPos.z < 0)
        {
            GUI.color = restoreColor;
            UnityEditor.Handles.EndGUI();
            return;
        }

        Vector2 size = GUI.skin.label.CalcSize(new GUIContent(text));
        GUI.Label(new Rect(screenPos.x - (size.x / 2), -screenPos.y + view.position.height - 20, size.x, size.y), text);
        GUI.color = restoreColor;
        UnityEditor.Handles.EndGUI();
    }
}
