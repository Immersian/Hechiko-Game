using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float moveSpeed = 2.0f;

    private Vector3 lastPosition;
    private Vector3 currentTarget;
    private Vector3 platformVelocity;

    void Start()
    {
        currentTarget = pointB.position;
        lastPosition = transform.position;
    }

    void FixedUpdate()
    {
        // Store position before movement
        lastPosition = transform.position;

        // Move platform directly
        transform.position = Vector3.MoveTowards(transform.position, currentTarget, moveSpeed * Time.deltaTime);

        // Calculate actual movement velocity
        platformVelocity = (transform.position - lastPosition) / Time.deltaTime;

        // Switch target when reaching current target
        if (Vector3.Distance(transform.position, currentTarget) < 0.01f)
        {
            currentTarget = (currentTarget == pointA.position) ? pointB.position : pointA.position;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.transform.parent = transform;
        }
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.transform.parent = null;
        }
    }
}
