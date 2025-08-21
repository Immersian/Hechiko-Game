using SupanthaPaul;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    public static bool MenuActivated = false;

    [Header("UI References")]
    public GameObject inventoryPanel;
    public Button firstSelectedButton;
    public ItemSlot[] itemSlots; // Array of all inventory slots
    [SerializeField] private Animator inventoryAnimator;

    [Header("Panel Switching")]
    public GameObject[] panels; // Array of panels in order: Inventory, Controls, Settings
    public Animator panelSwitchAnimator;
    [SerializeField] private int currentPanelIndex = 0;
    [SerializeField] private int targetPanelIndex = 0;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    // Public getters for the animation handler
    public int GetCurrentPanelIndex() => currentPanelIndex;
    public int GetTargetPanelIndex() => targetPanelIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (inventoryAnimator == null && inventoryPanel != null)
        {
            inventoryAnimator = inventoryPanel.GetComponent<Animator>();
        }
    }

    private void Update()
    {
        if (InputManager.instance.inputControl.Pause.Tab.WasPressedThisFrame())
        {
            ToggleInventory();
        }

        if (MenuActivated)
        {
            HandlePanelSwitching();
        }
    }

    public void ToggleInventory()
    {
        if (MenuActivated)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }

    private void HandlePanelSwitching()
    {
        if (InputManager.instance.inputControl.Pause.LB.WasPressedThisFrame())
        {
            SwitchPanel(-1);
        }

        if (InputManager.instance.inputControl.Pause.RB.WasPressedThisFrame())
        {
            SwitchPanel(1);
        }
    }

    private void SwitchPanel(int direction)
    {
        int newIndex = (currentPanelIndex + direction + panels.Length) % panels.Length;
        if (newIndex == currentPanelIndex) return;

        targetPanelIndex = newIndex;

        if (panelSwitchAnimator != null)
        {
            panelSwitchAnimator.SetInteger("Direction", direction);
            panelSwitchAnimator.SetTrigger("Switch");
        }
        else
        {
            DirectPanelSwitch();
        }

        if (debugLogs) Debug.Log($"Switching to panel: {panels[targetPanelIndex].name}");
    }

    // Called by animation handler when switch is complete
    public void CompletePanelSwitch()
    {
        currentPanelIndex = targetPanelIndex;
        SetPanelDefaultSelection();
    }

    // Fallback method for direct panel switching without animation
    private void DirectPanelSwitch()
    {
        panels[currentPanelIndex].SetActive(false);
        panels[targetPanelIndex].SetActive(true);
        currentPanelIndex = targetPanelIndex;
        SetPanelDefaultSelection();
    }

    private void DisableAllPanels()
    {
        foreach (GameObject panel in panels)
        {
            panel.SetActive(false);
        }
    }

    private void SetPanelDefaultSelection()
    {
        GameObject selectedObject = null;

        switch (currentPanelIndex)
        {
            case 0: // Inventory
                if (itemSlots.Length > 0 && itemSlots[0].gameObject.activeInHierarchy)
                {
                    selectedObject = itemSlots[0].gameObject;
                }
                break;
            case 1: // Controls
                // Add your controls panel default button here
                // selectedObject = controlsDefaultButton.gameObject;
                break;
            case 2: // Settings
                // Add your settings panel default button here
                // selectedObject = settingsDefaultButton.gameObject;
                break;
        }

        if (selectedObject != null)
        {
            EventSystem.current.SetSelectedGameObject(selectedObject);
        }
    }

    private void OpenInventory()
    {
        inventoryPanel.SetActive(true);

        if (inventoryAnimator != null)
        {
            inventoryAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            inventoryAnimator.SetTrigger("Open");
        }
        else
        {
            Debug.LogWarning("Inventory Animator not assigned!");
        }

        DisableAllPanels();
        panels[0].SetActive(true);
        currentPanelIndex = 0;
        targetPanelIndex = 0;

        MenuActivated = true;
        Time.timeScale = 0f;
        SetPanelDefaultSelection();

        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null) player.DisableMovement();

        CameraFollowObject cameraFollow = FindObjectOfType<CameraFollowObject>();
        if (cameraFollow != null) cameraFollow.DisableLookUpDown();
    }

    private void CloseInventory()
    {
        if (inventoryAnimator != null)
        {
            inventoryAnimator.SetTrigger("Close");
        }
        else
        {
            inventoryPanel.SetActive(false);
        }

        MenuActivated = false;
        Time.timeScale = 1f;

        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null) player.EnableMovement();

        CameraFollowObject cameraFollow = FindObjectOfType<CameraFollowObject>();
        if (cameraFollow != null) cameraFollow.EnableLookUpDown();

        InputManager.instance.SetGameplayInputEnabled(true);
        EventSystem.current.SetSelectedGameObject(null);

        if (debugLogs) Debug.Log("Inventory closed");
    }

    public void OnCloseAnimationComplete()
    {
        inventoryPanel.SetActive(false);
    }

    public bool AddItem(string itemName, Sprite itemSprite, string itemDescription)
    {
        foreach (ItemSlot slot in itemSlots)
        {
            if (!slot.isFull)
            {
                slot.AddItemToSlot(itemName, itemSprite, itemDescription);
                if (debugLogs) Debug.Log($"Added {itemName} to inventory");
                return true;
            }
        }

        if (debugLogs) Debug.Log("Inventory is full!");
        return false;
    }

    public bool HasItem(string itemName)
    {
        foreach (ItemSlot slot in itemSlots)
        {
            if (slot.isFull && slot.itemName == itemName)
            {
                return true;
            }
        }
        return false;
    }
}