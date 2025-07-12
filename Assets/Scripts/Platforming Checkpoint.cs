using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformingCheckpoint : MonoBehaviour
{
    [SerializeField] private BoxCollider2D checkpointTrigger;

    private void Awake()
    {
        // Auto-get reference if not set in Inspector
        if (checkpointTrigger == null)
            checkpointTrigger = GetComponent<BoxCollider2D>();

        // Ensure trigger is properly configured
        checkpointTrigger.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            RespawnPoint.Instance.respawnPoint = transform;
            checkpointTrigger.enabled = true;
        }
    }
}
