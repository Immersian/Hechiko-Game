using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletScript : MonoBehaviour
{
    private GameObject player;
    private Rigidbody2D rb;

    public float force;
    [Header("Collision Settings")]
    public LayerMask groundLayer; // Assign this in the Inspector to the ground layer
    public LayerMask playerLayer; // Assign this in the Inspector to the player layer
    public float destroyDelay = 0.1f;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            Vector3 direction = player.transform.position - transform.position;
            rb.velocity = direction.normalized * force;

            float rot = Mathf.Atan2(-direction.y, -direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, rot + 180);
        }
        else
        {
            Destroy(gameObject); // No player found, destroy bullet
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Get the layer of the collided object
        int otherLayer = other.gameObject.layer;

        // Check if collided with player or ground using layers
        if ((groundLayer.value & (1 << otherLayer)) != 0 ||
            (playerLayer.value & (1 << otherLayer)) != 0)
        {
            // Destroy the bullet after a slight delay
            Destroy(gameObject, destroyDelay);
        }
    }

    // Alternatively, if you're using collisions instead of triggers
    /*
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Destroy the bullet after a slight delay
            Destroy(gameObject, destroyDelay);
        }
    }
    */
}