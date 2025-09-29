using UnityEngine;
using System.Collections;

public class TutorialPopUp : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas tutorialCanvas;
    [SerializeField] private Animator tutorialAnimator;
    [SerializeField] private BoxCollider2D triggerCollider;

    [Header("Settings")]
    [SerializeField] private string playerTag = "Player";

    [Header("Animation Parameters")]
    [SerializeField] private string inTriggerParameter = "InTrigger"; // Bool for visibility state

    private bool playerInTrigger = false;

    private void Start()
    {
        InitializeComponents();

        // Start with canvas ENABLED but in empty/transparent state
        if (tutorialCanvas != null)
        {
            tutorialCanvas.enabled = true; // Keep it enabled!
        }

        // Ensure collider is set as trigger
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }

        // Reset animator to empty state
        if (tutorialAnimator != null)
        {
            tutorialAnimator.SetBool(inTriggerParameter, false);
        }
    }

    private void InitializeComponents()
    {
        // Auto-get components if not assigned
        if (tutorialCanvas == null)
        {
            tutorialCanvas = GetComponent<Canvas>();
            if (tutorialCanvas == null)
            {
                tutorialCanvas = GetComponentInChildren<Canvas>();
            }
        }

        if (tutorialAnimator == null)
        {
            tutorialAnimator = GetComponent<Animator>();
            if (tutorialAnimator == null)
            {
                tutorialAnimator = GetComponentInChildren<Animator>();
            }
        }

        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<BoxCollider2D>();
            if (triggerCollider == null)
            {
                triggerCollider = GetComponentInChildren<BoxCollider2D>();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInTrigger = true;
            EnableTutorial();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInTrigger = false;
            DisableTutorial();
        }
    }

    private void EnableTutorial()
    {
        if (tutorialCanvas != null && tutorialAnimator != null)
        {
            // Make sure canvas is enabled
            tutorialCanvas.enabled = true;

            // Set the bool to true - this will play the show animation
            tutorialAnimator.SetBool(inTriggerParameter, true);

            Debug.Log("Tutorial activated: " + gameObject.name);
        }
    }

    private void DisableTutorial()
    {
        if (tutorialCanvas != null && tutorialAnimator != null)
        {
            // Set bool to false - this will play the hide animation
            tutorialAnimator.SetBool(inTriggerParameter, false);
        }
    }

    // Fixed Gizmos method
    private void OnDrawGizmos()
    {
        if (triggerCollider != null)
        {
            Gizmos.color = playerInTrigger ? Color.green : Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;

            Vector3 position = triggerCollider.offset;
            Vector3 size = new Vector3(triggerCollider.size.x, triggerCollider.size.y, 0.1f);

            Gizmos.DrawWireCube(position, size);
        }
    }

    private void OnDisable()
    {
        playerInTrigger = false;

        // Reset animator state
        if (tutorialAnimator != null)
        {
            tutorialAnimator.SetBool(inTriggerParameter, false);
        }
    }
}