using UnityEngine;
using System.Collections.Generic;

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

    private List<WaterPoint> _waterPoints = new List<WaterPoint>();

    private void Start()
    {
        InitializeComponents();
        GenerateMesh();
        CreateWaterPoints();
    }

    private void InitializeComponents()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();
        _coll = GetComponent<EdgeCollider2D>();
        _coll.isTrigger = true;

        if (WaterMaterial != null)
        {
            _meshRenderer.material = WaterMaterial;
        }
        else
        {
            Debug.LogWarning("WaterMaterial is not assigned. Using default material.");
        }
    }

    private void FixedUpdate()
    {
        if (_mesh == null || _waterPoints.Count == 0) return;

        UpdateSpringPositions();
        PropagateWaves();

        _mesh.vertices = _vertices;
        _mesh.RecalculateNormals();
        ResetEdgeCollider(); // Update collider with new vertices
    }

    private void UpdateSpringPositions()
    {
        for (int i = 1; i < _waterPoints.Count - 1; i++)
        {
            WaterPoint point = _waterPoints[i];
            float x = point.pos - point.targetHeight;
            point.acceleration = -_spriteConstant * x - _damping * point.velocity;
            point.velocity += point.acceleration * _speedMult * Time.fixedDeltaTime;
            point.pos += point.velocity * _speedMult * Time.fixedDeltaTime;
            _vertices[_topVerticesIndex[i]].y = point.pos;
        }
    }

    private void PropagateWaves()
    {
        for (int j = 0; j < _wavePropogationIterations; j++)
        {
            for (int i = 1; i < _waterPoints.Count - 1; i++)
            {
                float leftDelta = _spread * (_waterPoints[i].pos - _waterPoints[i - 1].pos) * _speedMult * Time.fixedDeltaTime;
                _waterPoints[i - 1].velocity += leftDelta;

                float rightDelta = _spread * (_waterPoints[i].pos - _waterPoints[i + 1].pos) * _speedMult * Time.fixedDeltaTime;
                _waterPoints[i + 1].velocity += rightDelta;
            }
        }
    }

    public void Splash(Collider2D collision, float force)
    {
        if (_waterPoints.Count == 0) return;

        float radius = collision.bounds.extents.x * _playerCollisionRadiusMult;
        Vector2 center = collision.transform.position;

        for (int i = 0; i < _waterPoints.Count; i++)
        {
            if (IsPointInsideCircle(transform.TransformPoint(_vertices[_topVerticesIndex[i]]), center, radius))
            {
                _waterPoints[i].velocity = Mathf.Clamp(force * ForceMultiplier, -MaxForce, MaxForce);
            }
        }
    }

    private bool IsPointInsideCircle(Vector2 point, Vector2 center, float radius)
    {
        return (point - center).sqrMagnitude <= radius * radius;
    }

    public void GenerateMesh()
    {
        if (NumOfXVertices < 2)
        {
            Debug.LogError("NumOfXVertices must be at least 2.");
            return;
        }

        _mesh = new Mesh();
        _meshFilter.mesh = _mesh;

        CreateVertices();
        CreateTriangles();
        CreateUVs();
        FinalizeMesh();
    }

    private void CreateVertices()
    {
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
    }

    private void CreateTriangles()
    {
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

                triangles[index++] = current;
                triangles[index++] = currentBottomNextRow;
                triangles[index++] = next;

                triangles[index++] = next;
                triangles[index++] = currentBottomNextRow;
                triangles[index++] = nextBottomNextRow;
            }
        }

        _mesh.triangles = triangles;
    }

    private void CreateUVs()
    {
        Vector2[] uvs = new Vector2[_vertices.Length];
        for (int i = 0; i < _vertices.Length; i++)
        {
            uvs[i] = new Vector2(
                (_vertices[i].x + Width / 2) / Width,
                (_vertices[i].y + Height / 2) / Height
            );
        }
        _mesh.uv = uvs;
    }

    private void FinalizeMesh()
    {
        _mesh.RecalculateBounds();
        _mesh.RecalculateNormals();
    }

    public void ResetEdgeCollider()
    {
        if (_coll == null || _topVerticesIndex == null) return;

        Vector2[] newPoints = new Vector2[NumOfXVertices];
        for (int i = 0; i < NumOfXVertices; i++)
        {
            newPoints[i] = new Vector2(
                _vertices[_topVerticesIndex[i]].x,
                _vertices[_topVerticesIndex[i]].y
            );
        }
        _coll.points = newPoints;
    }

    private void CreateWaterPoints()
    {
        _waterPoints.Clear();
        for (int i = 0; i < _topVerticesIndex.Length; i++)
        {
            _waterPoints.Add(new WaterPoint
            {
                pos = _vertices[_topVerticesIndex[i]].y,
                targetHeight = _vertices[_topVerticesIndex[i]].y
            });
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(Width, Height, 0.1f));
    }
}