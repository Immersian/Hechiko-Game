using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private string itemName;
    [SerializeField] private Sprite sprite;
    [TextArea]
    [SerializeField] private string itemDescription;

    [Header("Collision")]
    [SerializeField] private bool useTrigger = true;

    private InventoryManager inventoryManager;

    private void Start()
    {
        // Find the InventoryManager in the scene
        inventoryManager = FindObjectOfType<InventoryManager>();

        if (inventoryManager == null)
        {
            Debug.LogError("No InventoryManager found in scene!");
            return;
        }

        // Check if this item already exists in inventory
        if (inventoryManager.HasItem(itemName))
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!useTrigger && collision.gameObject.CompareTag("Player"))
        {
            TryAddItemToInventory();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (useTrigger && other.CompareTag("Player"))
        {
            TryAddItemToInventory();
        }
    }

    private void TryAddItemToInventory()
    {
        if (inventoryManager == null)
        {
            // Try to find it again in case it wasn't available at Start
            inventoryManager = FindObjectOfType<InventoryManager>();
        }

        if (inventoryManager != null)
        {
            bool added = inventoryManager.AddItem(itemName, sprite, itemDescription);
            if (added)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            Debug.LogWarning("Tried to add item but no InventoryManager found");
        }
    }
}