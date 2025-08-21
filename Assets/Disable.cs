using UnityEngine;

public class Disable : MonoBehaviour
{
    [Header("References")]
    public InventoryManager inventoryManager;
    public GameObject[] panels;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

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
}