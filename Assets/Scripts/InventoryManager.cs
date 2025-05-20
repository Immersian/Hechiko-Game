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

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Optional: Keep inventory between scenes
    }

    private void Update()
    {
        if (InputManager.instance.inputControl.Pause.Tab.WasPressedThisFrame())
        {
            ToggleInventory();
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

    private void OpenInventory()
    {
        inventoryPanel.SetActive(true);
        Time.timeScale = 0f;
        MenuActivated = true;

        // Set first selection
        if (itemSlots.Length > 0)
        {
            EventSystem.current.SetSelectedGameObject(itemSlots[0].gameObject);

            // Manually trigger description update for first slot
            if (itemSlots[0].isFull)
                itemSlots[0].UpdateDescriptionUI();
            else
                itemSlots[0].ClearDescriptionUI();
        }

        // Disable player and camera
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null) player.DisableMovement();

        CameraFollowObject cameraFollow = FindObjectOfType<CameraFollowObject>();
        if (cameraFollow != null) cameraFollow.DisableLookUpDown();
    }

    private void CloseInventory()
    {
        inventoryPanel.SetActive(false);
        Time.timeScale = 1f;
        MenuActivated = false;

        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.EnableMovement();
        }
        CameraFollowObject cameraFollow = FindObjectOfType<CameraFollowObject>();
        if (cameraFollow != null) cameraFollow.EnableLookUpDown();

        //GetComponent<CameraFollowObject>().EnableLookUpDown();

        InputManager.instance.SetGameplayInputEnabled(true);
        EventSystem.current.SetSelectedGameObject(null);

        if (debugLogs) Debug.Log("Inventory closed");
    }

    public bool AddItem(string itemName, Sprite itemSprite, string itemDescription)
    {
        // Find first empty slot
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