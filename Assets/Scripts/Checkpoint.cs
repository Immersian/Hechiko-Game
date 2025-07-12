using SupanthaPaul;
using UnityEngine;
using UnityEngine.EventSystems;

public class Checkpoint : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject interactionUI;
    [SerializeField] private GameObject saveMenuUI;
    [SerializeField] private GameObject firstSelectedButton;

    [Header("Player Control")]
    [SerializeField] private bool disableMovementDuringRest = true;

    private PlayerController playerController;
    private bool isResting = false;
    private bool playerInRange = false;

    private void Awake()
    {
        InitializeUI();
        FindPlayerController();
    }

    private void InitializeUI()
    {
        if (interactionUI != null) interactionUI.SetActive(false);
        if (saveMenuUI != null) saveMenuUI.SetActive(false);
    }

    private void FindPlayerController()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
        }
        else
        {
            Debug.LogWarning("Player not found in the scene");
        }
    }

    private void Update()
    {
        HandleInteractionInput();
    }

    private void HandleInteractionInput()
    {
        if (playerInRange && InputManager.instance.inputControl.Dialogue.Interact.WasPressedThisFrame())
        {
            StartResting();
        }
    }

    private void StartResting()
    {
        if (isResting) return;

        isResting = true;
        DisablePlayerMovement();
        HideInteractionPrompt();
        OpenSaveMenu();
    }

    private void DisablePlayerMovement()
    {
        if (disableMovementDuringRest && playerController != null)
        {
            playerController.DisableMovement();
        }
    }

    private void OpenSaveMenu()
    {
        if (saveMenuUI != null)
        {
            saveMenuUI.SetActive(true);
            SetFirstSelectedButton();
        }
    }

    private void SetFirstSelectedButton()
    {
        if (firstSelectedButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        }
    }

    public void CloseSaveMenu()
    {
        if (saveMenuUI != null)
        {
            saveMenuUI.SetActive(false);
        }

        isResting = false;
        EnablePlayerMovement();
        ClearSelectedUI();
    }

    private void EnablePlayerMovement()
    {
        if (disableMovementDuringRest && playerController != null)
        {
            playerController.EnableMovement();
        }
    }

    private void ClearSelectedUI()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void ShowInteractionPrompt()
    {
        if (interactionUI != null) interactionUI.SetActive(true);
    }

    private void HideInteractionPrompt()
    {
        if (interactionUI != null) interactionUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandlePlayerTriggerEnter(other.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandlePlayerTriggerEnter(collision.gameObject);
    }

    private void HandlePlayerTriggerEnter(GameObject playerObject)
    {
        if (playerObject.CompareTag("Player") && !isResting)
        {
            playerInRange = true;
            ShowInteractionPrompt();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        HandlePlayerTriggerExit(other.gameObject);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        HandlePlayerTriggerExit(collision.gameObject);
    }

    private void HandlePlayerTriggerExit(GameObject playerObject)
    {
        if (playerObject.CompareTag("Player"))
        {
            playerInRange = false;
            HideInteractionPrompt();
        }
    }
}