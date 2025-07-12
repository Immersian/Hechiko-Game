using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(EdgeCollider2D))]
[RequireComponent(typeof(WaterTriggerHandler))]
public class InteractableWater : MonoBehaviour
{
    [Header("Springs")]
    [SerializeField] private float _spriteConstant = 1.4f;
    [SerializeField] private float _damping = 1.1f;
    [SerializeField] private float _spread = 6.5f;
    [SerializeField, Range(1, 10)] private int _wavePropogationIterations = 8;
    [SerializeField, Range(1, 20)] private float _speedMult = 5.5f;

    [Header("Force")]
    public float ForceMultiplier = 0.2f;
    [Range(1f, 50f)] public float MaxForce = 5f;

    [Header("Collision")]
    [SerializeField, Range(1f, 10f)] private float _playerCollisionRadiusMult = 4.15f;

    [Header("Mesh Generation")]
    [Range(2, 500)]
    public int NumOfXVertices = 70;
    public float Width = 10f;
    public float Height = 4f;
    public Material WaterMaterial;
    private const int NUM_OF_Y_VERTICES = 2;

    [Header("Gizmo")]
    public Color GizmoColor = Color.white;

    private Mesh _mesh;
    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
    private Vector3[] _vertices;
    private int[] _topVerticesIndex;

    private EdgeCollider2D _coll;

    private class WaterPoint
    {
        public float velocity, acceleration, pos, targetHeight;
    }

    private List <WaterPoint> _waterPoints = new List<WaterPoint>();

    private void Start()
    {
        _coll = GetComponent<EdgeCollider2D>();
        GenerateMesh();
        CreateWaterPoints();
    }
    private void Reset()
    {
        _coll = GetComponent<EdgeCollider2D>();
        _coll.isTrigger = true;
    }

    private void FixedUpdate()
    {
        // Update spring positions
        for (int i = 1; i < _waterPoints.Count - 1; i++)
        {
            WaterPoint point = _waterPoints[i];
            float x = point.pos - point.targetHeight;
            point.acceleration = -_spriteConstant * x - _damping * point.velocity;
            point.velocity += point.acceleration * _speedMult * Time.fixedDeltaTime;
            point.pos += point.velocity * _speedMult * Time.fixedDeltaTime;
            _vertices[_topVerticesIndex[i]].y = point.pos;
        }

        // Wave propagation - FIXED
        for (int j = 0; j < _wavePropogationIterations; j++)
        {
            for (int i = 1; i < _waterPoints.Count - 1; i++)
            {
                float leftDelta = _spread * (_waterPoints[i].pos - _waterPoints[i - 1].pos) * _speedMult * Time.fixedDeltaTime;
                _waterPoints[i - 1].velocity += leftDelta;

                float rightDelta = _spread * (_waterPoints[i].pos - _waterPoints[i + 1].pos) * _speedMult * Time.fixedDeltaTime;
                _waterPoints[i + 1].velocity += rightDelta; // Fixed: was adding to wrong point
            }
        }

        _mesh.vertices = _vertices;
        _mesh.RecalculateNormals();
    }

    public void Splash(Collider2D collision, float force)
    {
        float radius = collision.bounds.extents.x * _playerCollisionRadiusMult;
        Vector2 center = collision.transform.position;

        for (int i = 0; i < _waterPoints.Count; i++)
        {
            Vector2 vertexWorldPos = transform.TransformPoint(_vertices[_topVerticesIndex[i]]);

            if (IsPointInsideCircle(vertexWorldPos, center, radius))
            {
                _waterPoints[i].velocity = force;
            }
        }
    }

    private bool IsPointInsideCircle(Vector2 point, Vector2 center, float radius)
    {
        float distanceSquared = (point - center).sqrMagnitude;
        return distanceSquared <= radius * radius;
    }

    public void ResetEdgeCollider()
    {
        _coll = GetComponent<EdgeCollider2D>();

        Vector2[] newPoints = new Vector2[2];

        Vector2 firstPoint = new Vector2(_vertices[_topVerticesIndex[0]].x, _vertices[_topVerticesIndex[0]].y);
        newPoints[0] = firstPoint;

        Vector2 secondPoint = new Vector2(_vertices[_topVerticesIndex[_topVerticesIndex.Length - 1]].x, _vertices[_topVerticesIndex[_topVerticesIndex.Length - 1]].y);
        newPoints[1] = secondPoint;

        _coll.offset = Vector2.zero;
        _coll.points = newPoints;
    }

    public void GenerateMesh()
    {
        _mesh = new Mesh();

        // Create vertices
        _vertices = new Vector3[NumOfXVertices * NUM_OF_Y_VERTICES];
        _topVerticesIndex = new int[NumOfXVertices];

        for (int y = 0; y < NUM_OF_Y_VERTICES; y++)
        {
            for (int x = 0; x < NumOfXVertices; x++)
            {
                float xPos = (x / (float)(NumOfXVertices - 1)) * Width - Width / 2;
                float yPos = (y / (float)(NUM_OF_Y_VERTICES - 1)) * Height - Height / 2;
                _vertices[y * NumOfXVertices + x] = new Vector3(xPos, yPos, 0f);

                if (y == NUM_OF_Y_VERTICES - 1)
                {
                    _topVerticesIndex[x] = y * NumOfXVertices + x;
                }
            }
        }

        // Create triangles - fixed calculation
        int numQuads = (NumOfXVertices - 1) * (NUM_OF_Y_VERTICES - 1);
        int[] triangles = new int[numQuads * 6];
        int index = 0;

        for (int y = 0; y < NUM_OF_Y_VERTICES - 1; y++)
        {
            for (int x = 0; x < NumOfXVertices - 1; x++)
            {
                int current = y * NumOfXVertices + x;
                int next = current + 1;
                int currentBottomNextRow = current + NumOfXVertices;
                int nextBottomNextRow = next + NumOfXVertices;

                // First triangle
                triangles[index++] = current;
                triangles[index++] = currentBottomNextRow;
                triangles[index++] = next;

                // Second triangle
                triangles[index++] = next;
                triangles[index++] = currentBottomNextRow;
                triangles[index++] = nextBottomNextRow;
            }
        }

        // Create UVs
        Vector2[] uvs = new Vector2[_vertices.Length];
        for (int i = 0; i < _vertices.Length; i++)
        {
            uvs[i] = new Vector2((_vertices[i].x + Width / 2) / Width, (_vertices[i].y + Height / 2) / Height);
        }

        // Set up mesh components
        if (_meshRenderer == null)
            _meshRenderer = GetComponent<MeshRenderer>();

        if (_meshFilter == null)
            _meshFilter = GetComponent<MeshFilter>();

        _meshRenderer.material = WaterMaterial;

        _mesh.vertices = _vertices;
        _mesh.triangles = triangles;
        _mesh.uv = uvs;

        _mesh.RecalculateBounds();
        _mesh.RecalculateNormals();
        CreateWaterPoints(); // Recreate water points when mesh changes
        ResetEdgeCollider(); // Update collider to match new mesh

        _meshFilter.mesh = _mesh;
    }

    private void CreateWaterPoints()
    {
        _waterPoints .Clear();

        for (int i = 0; i < _topVerticesIndex.Length; i++)
        {
            _waterPoints.Add(new WaterPoint
            {
                pos = _vertices[_topVerticesIndex[i]].y,
                targetHeight = _vertices[_topVerticesIndex[i]].y,
            });
        }
    }
}
[CustomEditor(typeof(InteractableWater))]
public class InteractableWaterEditor : Editor
{
    private InteractableWater _water;

    private void OnEnable()
    {
        _water = (InteractableWater)target;
    }

    public override VisualElement CreateInspectorGUI()
    {
        VisualElement root = new VisualElement();

        InspectorElement.FillDefaultInspector(root, serializedObject, this);

        root.Add(new VisualElement { style = { height = 10  } });

        Button generateMeshButton = new Button(() => _water.GenerateMesh())
        {
            text = "Generate Mesh"
        };
        root.Add(generateMeshButton);

        Button placeEdgeColliderButton = new Button(() => _water.ResetEdgeCollider())
        {
            text = "Place Edge Collider"
        };
        root.Add(placeEdgeColliderButton);

        return root;
    }

    private void ChangeDimensions(ref float width, ref float height, float calculatedWidthMax, float calculatedHeightMax)
    {
        width = Mathf.Max(0.1f, calculatedWidthMax);
        height = Mathf.Max(0.1f, calculatedHeightMax);
    }

    private void OnSceneGUI()
    {
        // Draw the wireframe box
        Handles.color = _water.GizmoColor;
        Vector3 center = _water.transform.position;
        Vector3 size = new Vector3(_water.Width, _water.Height, 0.1f);
        Handles.DrawWireCube(center, size);

        // Handles for width and height
        float handleSize = HandleUtility.GetHandleSize(center) * 0.1f;
        Vector3 snap = Vector3.one * 0.1f;

        // Corner handles
        Vector3[] corners = new Vector3[4];
        corners[0] = center + new Vector3(-_water.Width / 2, -_water.Height / 2, 0); // Bottom-left
        corners[1] = center + new Vector3(_water.Width / 2, -_water.Height / 2, 0);  // Bottom-right
        corners[2] = center + new Vector3(-_water.Width / 2, _water.Height / 2, 0);  // Top-left
        corners[3] = center + new Vector3(_water.Width / 2, _water.Height / 2, 0);   // Top-right

        // Handle for each corner
        EditorGUI.BeginChangeCheck();
        Vector3 newBottomLeft = Handles.FreeMoveHandle(corners[0], handleSize, snap, Handles.CubeHandleCap);
        if (EditorGUI.EndChangeCheck())
        {
            ChangeDimensions(ref _water.Width, ref _water.Height, corners[1].x - newBottomLeft.x, corners[3].y - newBottomLeft.y);
            _water.transform.position += new Vector3((newBottomLeft.x - corners[0].x) / 2, (newBottomLeft.y - corners[0].y) / 2, 0);
        }

        EditorGUI.BeginChangeCheck();
        Vector3 newBottomRight = Handles.FreeMoveHandle(corners[1], handleSize, snap, Handles.CubeHandleCap);
        if (EditorGUI.EndChangeCheck())
        {
            ChangeDimensions(ref _water.Width, ref _water.Height, newBottomRight.x - corners[0].x, corners[3].y - newBottomRight.y);
            _water.transform.position += new Vector3((newBottomRight.x - corners[1].x) / 2, (newBottomRight.y - corners[1].y) / 2, 0);
        }

        EditorGUI.BeginChangeCheck();
        Vector3 newTopLeft = Handles.FreeMoveHandle(corners[2], handleSize, snap, Handles.CubeHandleCap);
        if (EditorGUI.EndChangeCheck())
        {
            ChangeDimensions(ref _water.Width, ref _water.Height, corners[3].x - newTopLeft.x, newTopLeft.y - corners[0].y);
            _water.transform.position += new Vector3((newTopLeft.x - corners[2].x) / 2, (newTopLeft.y - corners[2].y) / 2, 0);
        }

        EditorGUI.BeginChangeCheck();
        Vector3 newTopRight = Handles.FreeMoveHandle(corners[3], handleSize, snap, Handles.CubeHandleCap);
        if (EditorGUI.EndChangeCheck())
        {
            ChangeDimensions(ref _water.Width, ref _water.Height, newTopRight.x - corners[2].x, newTopRight.y - corners[1].y);
            _water.transform.position += new Vector3((newTopRight.x - corners[3].x) / 2, (newTopRight.y - corners[3].y) / 2, 0);
        }

        // Update the mesh if the handles are moved
        if (GUI.changed)
        {
            _water.GenerateMesh();
        }
    }
}
