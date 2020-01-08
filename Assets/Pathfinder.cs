using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Pathfinder : MonoBehaviour
{
    const float WALL_COST = 10000.0f;
    const float EMPTY_COST = 1.0f;

    // Nested types.
    public class Map
    {
        public Map(Texture2D input)
        {
            WIDTH = input.width;
            HEIGHT = input.height;

            VALUES = new Node[WIDTH, HEIGHT];

            Color color;
            for (int x = 0; x < WIDTH; x++)
            {
                for (int y = 0; y < HEIGHT; y++)
                {
                    color = input.GetPixel(x,y);
                    if (color == Color.white)
                    {
                        VALUES[x, y] = new Node(EMPTY_COST, new Vector2Int(x,y));
                    }
                    else if (color == Color.black)
                    {
                        VALUES[x, y] = new Node(WALL_COST, new Vector2Int(x, y));
                    }
                    else if (color == Color.blue)
                    {
                        VALUES[x, y] = new Node(EMPTY_COST, new Vector2Int(x, y));
                        STARTING_NODE = VALUES[x, y];
                    }
                    else if (color == Color.red)
                    {
                        VALUES[x, y] = new Node(EMPTY_COST, new Vector2Int(x, y));
                        TARGET_NODE = VALUES[x, y];
                    }
                    else
                    {
                        Debug.LogError("Unexpected color in input texture: " + color.ToString());
                    }
                }
            }

            for (int x = 0; x < WIDTH; x++)
            {
                for (int y = 0; y < HEIGHT; y++)
                {
                    VALUES[x, y].h = Vector2Int.Distance(TARGET_NODE.POSITION, VALUES[x,y].POSITION);
                    VALUES[x, y].g = VALUES[x,y].TRAVERSAL_COST; // + cumulated value added later by A*.
                }
            }

            Debug.Assert(STARTING_NODE != null && TARGET_NODE != null);
        }
        public KeyValuePair<Node, bool>[] GetNeighborsOf(Node node)
        {
            List<KeyValuePair<Node, bool>> neighborPositions = new List<KeyValuePair<Node, bool>>();

            if (node.POSITION.x - 1 >= 0)
            {
                // L
                neighborPositions.Add(new KeyValuePair<Node, bool>(VALUES[node.POSITION.x - 1, node.POSITION.y], false));
            }
            if (node.POSITION.x + 1 <= WIDTH - 1)
            {
                // R
                neighborPositions.Add(new KeyValuePair<Node, bool>(VALUES[node.POSITION.x + 1, node.POSITION.y], false));
            }
            if (node.POSITION.y - 1 >= 0)
            {
                // T
                neighborPositions.Add(new KeyValuePair<Node, bool>(VALUES[node.POSITION.x, node.POSITION.y - 1], false));
            }
            if (node.POSITION.y + 1 <= HEIGHT - 1)
            {
                // B
                neighborPositions.Add(new KeyValuePair<Node, bool>(VALUES[node.POSITION.x, node.POSITION.y + 1], false));
            }
            if (node.POSITION.x - 1 >= 0 && node.POSITION.y - 1 >= 0)
            {
                // TL
                neighborPositions.Add(new KeyValuePair<Node, bool>(VALUES[node.POSITION.x - 1, node.POSITION.y - 1], true));
            }
            if (node.POSITION.x + 1 <= WIDTH - 1 && node.POSITION.y + 1 <= HEIGHT - 1)
            {
                // BR
                neighborPositions.Add(new KeyValuePair<Node, bool>(VALUES[node.POSITION.x + 1, node.POSITION.y + 1], true));
            }
            if (node.POSITION.x - 1 >= 0 && node.POSITION.y + 1 <= HEIGHT - 1)
            {
                // BL
                neighborPositions.Add(new KeyValuePair<Node, bool>(VALUES[node.POSITION.x - 1, node.POSITION.y + 1], true));
            }
            if (node.POSITION.x + 1 <= WIDTH - 1 && node.POSITION.y - 1 >= 0)
            {
                // TR
                neighborPositions.Add(new KeyValuePair<Node, bool>(VALUES[node.POSITION.x + 1, node.POSITION.y - 1], true));
            }

            return neighborPositions.ToArray();
        }

        public readonly Node[,] VALUES;
        public readonly Node STARTING_NODE;
        public readonly Node TARGET_NODE;
        public readonly int WIDTH, HEIGHT;
    }
    public class Node
    {
        public Node(float traversalCost, Vector2Int position)
        {
            TRAVERSAL_COST = traversalCost;
            POSITION = position;
        }

        public readonly float TRAVERSAL_COST;
        public readonly Vector2Int POSITION;

        public float h;
        public float g;
        public float TotalCost
        {
            get
            {
                return g + h;
            }
        }
        public Node parent;
    }

    // Serialized fields.
    [SerializeField] Texture2D input = null;
    Map map = null;
    Vector2Int[] path = new Vector2Int[0];
    int cycles = 0;

    // Inherited methods.
    private void Start()
    {
        ResetMap();
    }
    readonly Vector3 SQUARE_SIZE = new Vector3(1f, 1f, 1f);
    readonly Color WHITE = new Color(1, 1, 1, 1f);
    readonly Color BLACK = new Color(0, 0, 0, 1f);
    readonly Color BLUE = new Color(0, 0, 1, 1f);
    readonly Color RED = new Color(1, 0, 0, 1f);
    readonly Color GREEN = new Color(0, 1, 0, 1f);
    readonly Color GRAY = new Color(0.5f, 0.5f, 0.5f, 1f);
    readonly Color DARK_GRAY = new Color(0.25f, 0.25f, 0.25f, 1f);
    List<Node> updatedNodes = new List<Node>();
    void OnDrawGizmos()
    {
        if (map != null)
        {
            /*var color = Color.black;
            for (int x = 0; x < map.WIDTH; x++)
            {
                for (int y = 0; y < map.HEIGHT; y++)
                {
                    if (updatedNodes.Contains(map.VALUES[x, y]))
                    {
                        color = Color.red;
                    }
                    else
                    {
                        color = Color.black;
                    }

                    drawString(map.VALUES[x, y].TotalCost.ToString(), new Vector3(x, y, 0), Color.black);
                    drawString("g: " + map.VALUES[x, y].g.ToString(), new Vector3(x - 0.25f, y + 0.25f, 0), Color.black);
                    drawString("h: " + map.VALUES[x, y].h.ToString(), new Vector3(x + 0.25f, y + 0.25f, 0), Color.black);
                }
            }*/

            for (int x = 0; x < map.WIDTH; x++)
            {
                for (int y = 0; y < map.HEIGHT; y++)
                {
                    Gizmos.color = map.VALUES[x, y].TRAVERSAL_COST >= WALL_COST ? BLACK : WHITE;
                    Gizmos.DrawCube(new Vector3(x,y,0), SQUARE_SIZE);
                }
            }

            /*Gizmos.color = GRAY;
            foreach (var tile in openSet)
            {
                Gizmos.DrawCube((Vector2)tile.POSITION, SQUARE_SIZE);
            }
            Gizmos.color = DARK_GRAY;
            foreach (var tile in closedSet)
            {
                Gizmos.DrawCube((Vector2)tile.POSITION, SQUARE_SIZE);
            }*/

            Gizmos.color = GREEN;
            if (path != null)
            {
                foreach (var tile in path)
                {
                    Gizmos.DrawCube((Vector2)tile, SQUARE_SIZE);
                }
            }

            Gizmos.color = BLUE;
            Gizmos.DrawCube((Vector2)map.STARTING_NODE.POSITION, SQUARE_SIZE);

            Gizmos.color = RED;
            Gizmos.DrawCube((Vector2)map.TARGET_NODE.POSITION, SQUARE_SIZE);

            /*Gizmos.color = GREEN;
            for (int x = 0; x < map.WIDTH; x++)
            {
                for (int y = 0; y < map.HEIGHT; y++)
                {
                    if (map.VALUES[x, y].parent != null)
                    {
                        Gizmos.DrawLine((Vector2)map.VALUES[x, y].POSITION, (Vector2)map.VALUES[x, y].parent.POSITION);
                    }
                }
            }*/
        }
    }
    private void OnGUI()
    {
        if (GUILayout.Button("NextStep"))
        {
            path = AStar(cycles++, true);
        }
        if (GUILayout.Button("Reset"))
        {
            ResetMap();
        }
    }
    void ResetMap()
    {
        openSet.Clear();
        closedSet.Clear();
        path = new Vector2Int[0];
        map = new Map(input);
        cycles = 0;
        updatedNodes.Clear();

        map.STARTING_NODE.g = map.STARTING_NODE.TRAVERSAL_COST;
        openSet.Add(map.STARTING_NODE);
    }

    // A* methods.
    List<Node> openSet = new List<Node>();
    List<Node> closedSet = new List<Node>();
    Vector2Int[] AStar(int cycles, bool disableStepByStep)
    {
        updatedNodes.Clear();

        Node current = map.STARTING_NODE;
        current.g = 0;
        openSet.Add(current);

        while (openSet.Count > 0 && (disableStepByStep || cycles-- >= 0))
        {
            openSet.Sort((x,y) => x.TotalCost.CompareTo(y.TotalCost));
            current = openSet[0];

            if (current == map.TARGET_NODE)
            {
                return FindPath(current);
            }

            openSet.Remove(current);

            foreach (var child in map.GetNeighborsOf(current))
            {
                var newCost = current.g + child.Key.TRAVERSAL_COST * (child.Value ? 1.41421f : 1);
                if (openSet.Contains(child.Key))
                {
                    if (child.Key.g <= newCost) continue;
                }
                else if (closedSet.Contains(child.Key))
                {
                    if (child.Key.g <= newCost) continue;
                }
                else
                {
                    openSet.Add(child.Key);
                }

                child.Key.g = newCost;
                child.Key.parent = current;
            }

            closedSet.Add(current);
        }

        return FindPath(current);
    }
    Vector2Int[] FindPath(Node currentNode)
    {
        if (currentNode != null)
        {
            List<Vector2Int> path = new List<Vector2Int>();

            while (currentNode.parent != null)
            {
                path.Add(currentNode.POSITION);
                currentNode = currentNode.parent;
            }

            path.Reverse();
            return path.ToArray();
        }
        else
        {
            return null;
        }
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
