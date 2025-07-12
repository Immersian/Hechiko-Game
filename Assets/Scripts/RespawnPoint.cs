using UnityEngine;
using System.Collections;
using SupanthaPaul;

public class RespawnPoint : MonoBehaviour
{
    public static RespawnPoint Instance { get; private set; }
    public Transform respawnPoint;

    [Header("Fade Settings")]
    [SerializeField] private float respawnDelay = 0.2f;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private CrossFade crossFade;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        // Auto-get CrossFade if not assigned
        if (crossFade == null)
        {
            crossFade = GetComponent<CrossFade>();
            if (crossFade == null)
            {
                Debug.LogError("No CrossFade component found on RespawnPoint!", this);
            }
        }

        // Ensure all child colliders are triggers
        EnsureChildTriggers();

        // Add trigger detection to all child objects
        SetupChildTriggerDetection();
    }

    // Make sure all child colliders are set as triggers
    private void EnsureChildTriggers()
    {
        Collider2D[] childColliders = GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D collider in childColliders)
        {
            if (collider.gameObject != gameObject) // Skip parent if it has a collider
            {
                collider.isTrigger = true;
            }
        }
    }

    // Add trigger detection components to all child objects
    private void SetupChildTriggerDetection()
    {
        foreach (Transform child in transform)
        {
            // Skip if already has a trigger detector
            if (child.GetComponent<ChildTriggerForwarder>() != null) continue;

            var detector = child.gameObject.AddComponent<ChildTriggerForwarder>();
            detector.respawnPoint = this;
        }
    }

    public void TriggerRespawn(Transform playerTransform)
    {
        StartCoroutine(RespawnPlayer(playerTransform));
    }

    private IEnumerator RespawnPlayer(Transform playerTransform)
    {
        // Freeze player during respawn
        var playerMovement = playerTransform.GetComponent<PlayerController>();
        if (playerMovement != null) playerMovement.DisableMovement();

        // Fade in
        if (crossFade != null)
        {
            yield return crossFade.FadeIn();
        }

        // Wait for the delay
        yield return new WaitForSecondsRealtime(respawnDelay);

        // Move player
        playerTransform.position = respawnPoint.position;

        // Fade out
        if (crossFade != null)
        {
            yield return crossFade.FadeOut();
        }

        // Unfreeze player
        if (playerMovement != null) playerMovement.EnableMovement();
    }
}

// Helper component for child objects
