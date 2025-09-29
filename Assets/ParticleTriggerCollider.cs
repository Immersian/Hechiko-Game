using UnityEngine;
using System.Collections;

public class ParticleColorTriggerController : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private LayerMask playerLayer = 1 << 3;
    [SerializeField] private bool useTagInstead = true;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float exitDelay = 0.1f; // Small delay to prevent flickering

    [Header("Particle Color Changer Reference")]
    [SerializeField] private ParticleColorChanger particleColorChanger;

    private bool playerInTrigger = false;
    private Coroutine exitCoroutine;

    private void Start()
    {
        if (particleColorChanger == null)
        {
            particleColorChanger = GetComponent<ParticleColorChanger>();
        }

        if (particleColorChanger == null)
        {
            Debug.LogError("ParticleColorChanger not found! Please assign a reference.", this);
        }
        else
        {
            particleColorChanger.enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsPlayer(collision)) return;

        Debug.Log($"Player entered trigger: {gameObject.name}");

        // Cancel any pending exit
        if (exitCoroutine != null)
        {
            StopCoroutine(exitCoroutine);
            exitCoroutine = null;
        }

        if (!playerInTrigger)
        {
            playerInTrigger = true;
            if (particleColorChanger != null)
            {
                particleColorChanger.enabled = true;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsPlayer(collision)) return;

        Debug.Log($"Player exited trigger: {gameObject.name}");

        // Start exit delay to prevent flickering between adjacent triggers
        if (playerInTrigger)
        {
            exitCoroutine = StartCoroutine(ExitDelay());
        }
    }

    private IEnumerator ExitDelay()
    {
        yield return new WaitForSeconds(exitDelay);
        playerInTrigger = false;
        if (particleColorChanger != null)
        {
            particleColorChanger.enabled = false;
        }
        exitCoroutine = null;
    }

    private bool IsPlayer(Collider2D collision)
    {
        if (useTagInstead)
        {
            return collision.CompareTag(playerTag);
        }
        else
        {
            return playerLayer == (playerLayer | (1 << collision.gameObject.layer));
        }
    }

    [ContextMenu("Enable Color Change")]
    private void EnableColorChange()
    {
        if (particleColorChanger != null)
        {
            particleColorChanger.enabled = true;
        }
    }

    [ContextMenu("Disable Color Change")]
    private void DisableColorChange()
    {
        if (particleColorChanger != null)
        {
            particleColorChanger.enabled = false;
        }
    }
}