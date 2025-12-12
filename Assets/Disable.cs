using UnityEngine;

public class Disable : MonoBehaviour
{
    [Header("References")]
    public InventoryManager inventoryManager;
    public GameObject[] panels;

    [Header("Device-Specific Objects")]
    [SerializeField] private GameObject keyboardMouseObjects;
    [SerializeField] private GameObject gamepadObjects;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private void Start()
    {
        // Subscribe to device changes
        if (InputManager.instance != null)
        {
            InputManager.instance.onDeviceChanged += OnInputDeviceChanged;
        }

        // Set initial device display
        UpdateDeviceDisplay();
    }

    private void OnInputDeviceChanged(InputManager.CurrentDevice newDevice)
    {
        UpdateDeviceDisplay();
    }

    private void UpdateDeviceDisplay()
    {
        if (InputManager.instance == null) return;

        bool isGamepad = InputManager.instance.IsGamepad();
        bool isKeyboardMouse = InputManager.instance.IsKeyboardMouse();

        // Show/hide device-specific objects
        if (keyboardMouseObjects != null)
        {
            keyboardMouseObjects.SetActive(isKeyboardMouse);
        }

        if (gamepadObjects != null)
        {
            gamepadObjects.SetActive(isGamepad);
        }

        if (debugLogs) Debug.Log($"Pause menu device display updated - Gamepad: {isGamepad}, Keyboard/Mouse: {isKeyboardMouse}");
    }

    // Animation event: Called at the start of switch animation to hide current panel
    public void OnSwitchAnimationStart()
    {
        if (inventoryManager == null)
        {
            Debug.LogWarning("InventoryManager reference not set!");
            return;
        }

        int currentPanelIndex = inventoryManager.GetCurrentPanelIndex();

        if (currentPanelIndex >= 0 && currentPanelIndex < panels.Length)
        {
            panels[currentPanelIndex].SetActive(false);
            if (debugLogs) Debug.Log($"Disabled panel {currentPanelIndex}");
        }
    }

    // Animation event: Called at the end of switch animation to show target panel
    public void OnSwitchAnimationComplete()
    {
        if (inventoryManager == null)
        {
            Debug.LogWarning("InventoryManager reference not set!");
            return;
        }

        int targetPanelIndex = inventoryManager.GetTargetPanelIndex();

        if (targetPanelIndex >= 0 && targetPanelIndex < panels.Length)
        {
            panels[targetPanelIndex].SetActive(true);
            inventoryManager.CompletePanelSwitch();
            if (debugLogs) Debug.Log($"Enabled panel {targetPanelIndex}");
        }
    }

    public void DisableInventoryPanel()
    {
        gameObject.SetActive(false);
        if (debugLogs) Debug.Log("Inventory panel disabled");
    }

    // Method to manually update device display (useful when enabling the pause menu)
    public void RefreshDeviceDisplay()
    {
        UpdateDeviceDisplay();
    }

    private void OnEnable()
    {
        // Update device display when object becomes active
        UpdateDeviceDisplay();
    }

    private void OnDisable()
    {
        // Unsubscribe from device changes when disabled
        if (InputManager.instance != null)
        {
            InputManager.instance.onDeviceChanged -= OnInputDeviceChanged;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from device changes when destroyed
        if (InputManager.instance != null)
        {
            InputManager.instance.onDeviceChanged -= OnInputDeviceChanged;
        }
    }
}