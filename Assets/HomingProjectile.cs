using UnityEngine;

public class HomingProjectile : MonoBehaviour
{
    [Header("Homing Settings")]
    public float speed = 7f;
    public float turnRate = 90f;          // Degrees per second
    public LayerMask groundLayer;
    public float destroyDelayAfterHit = 0.2f;
    [Range(0, 180)] public float maxHomingAngle = 45f; // Max angle it can turn toward target

    [Header("Debug Visualization")]
    public bool showGizmos = true;
    public Color gizmoColor = Color.yellow;
    public float gizmoLength = 2f;

    private Transform target;
    private Rigidbody2D rb;
    private bool isDestroyed = false;
    private Vector2 currentDirection;

    // Initialize with target and settings
    public void Initialize(Transform target, float speed, float turnRate, LayerMask groundLayer)
    {
        this.target = target;
        this.speed = speed;
        this.turnRate = turnRate;
        this.groundLayer = groundLayer;
        rb = GetComponent<Rigidbody2D>();

        if (target != null && rb != null)
        {
            currentDirection = (target.position - transform.position).normalized;
            rb.velocity = currentDirection * speed;
            transform.right = currentDirection;
        }
    }

    private void FixedUpdate()
    {
        if (isDestroyed || target == null || rb == null) return;

        Vector2 desiredDirection = (target.position - transform.position).normalized;
        float angleToTarget = Vector2.SignedAngle(currentDirection, desiredDirection);

        // If target is outside max angle, don't home
        if (Mathf.Abs(angleToTarget) > maxHomingAngle)
        {
            rb.velocity = currentDirection * speed;
            return;
        }

        // Calculate max turn allowed this frame
        float maxTurnThisFrame = turnRate * Time.fixedDeltaTime;
        float actualTurn = Mathf.Clamp(angleToTarget, -maxTurnThisFrame, maxTurnThisFrame);

        // Rotate direction gradually
        currentDirection = Quaternion.Euler(0, 0, actualTurn) * currentDirection;
        currentDirection.Normalize();

        // Apply velocity and rotation
        rb.velocity = currentDirection * speed;
        float lookAngle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(lookAngle, Vector3.forward);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDestroyed) return;

        if (((1 << other.gameObject.layer) & groundLayer) != 0 || other.CompareTag("Player"))
        {
            isDestroyed = true;
            if (rb != null) rb.velocity = Vector2.zero;
            Destroy(gameObject, destroyDelayAfterHit);
        }
    }

    // Debug visualization (unchanged from your original)
    private void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying) return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + currentDirection * gizmoLength);

        if (target != null)
        {
            Vector2 toTarget = ((Vector2)target.position - (Vector2)transform.position).normalized;
            float currentAngle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;

            Vector2 leftBound = Quaternion.Euler(0, 0, maxHomingAngle) * currentDirection;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + leftBound * gizmoLength * 0.5f);

            Vector2 rightBound = Quaternion.Euler(0, 0, -maxHomingAngle) * currentDirection;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + rightBound * gizmoLength * 0.5f);

            DrawGizmoArc(transform.position, currentDirection, maxHomingAngle, gizmoLength * 0.5f);
        }
    }

    private void DrawGizmoArc(Vector2 center, Vector2 direction, float angle, float radius)
    {
        Vector2 startDir = Quaternion.Euler(0, 0, angle) * direction;
        Vector2 endDir = Quaternion.Euler(0, 0, -angle) * direction;

        int segments = 20;
        Vector2 prevPoint = center + startDir * radius;

        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            float currentAngle = Mathf.Lerp(angle, -angle, t);
            Vector2 currentDir = Quaternion.Euler(0, 0, currentAngle) * direction;
            Vector2 currentPoint = center + currentDir * radius;

            Gizmos.DrawLine(prevPoint, currentPoint);
            prevPoint = currentPoint;
        }
    }
}