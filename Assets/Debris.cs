using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Debris : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer; // Set this in Inspector to match your ground layer
    [SerializeField] private string playerTag = "Player"; // Set to whatever tag your player has

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if collided with ground (using layer) or player (using tag)
        if (((1 << other.gameObject.layer) & groundLayer) != 0 || other.CompareTag(playerTag))
        {
            Destroy(gameObject);
        }
    }

    // Optional: Also destroy if it goes off-screen
    private void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}