using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private string itemName;
    [SerializeField] private Sprite sprite;
    [TextArea]
    [SerializeField] private string itemDescription;


    [Header("Collision")]
    [SerializeField] private bool useTrigger = true;

    private void Start()
    {
        // Check if this item already exists in inventory
        if (InventoryManager.Instance.HasItem(itemName))
        {
            Destroy(gameObject);
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("No InventoryManager found in scene!");
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
        if (InventoryManager.Instance != null)
        {
            bool added = InventoryManager.Instance.AddItem(itemName, sprite, itemDescription);
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