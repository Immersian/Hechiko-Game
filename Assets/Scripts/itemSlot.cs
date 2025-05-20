using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemSlot : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [Header("Item Data")]
    public string itemName;
    public Sprite itemSprite;
    public bool isFull;
    public string itemDescription;

    [Header("Item Slot")]
    [SerializeField] private Image itemImage;

    [Header("Selection UI")]
    [SerializeField] private GameObject selectionPanel;

    [Header("Item Description Slot")]
    public Image itemDescriptionImage;
    public TMP_Text ItemDescriptionNameText;
    public TMP_Text ItemDescriptionText;

    public void AddItemToSlot(string name, Sprite sprite, string description)
    {
        itemName = name;
        itemSprite = sprite;
        itemDescription = description;
        isFull = true;

        // Update the UI
        itemImage.sprite = sprite;
        itemImage.enabled = true;

        // Enable description image if it exists
        if (itemDescriptionImage != null)
            itemDescriptionImage.enabled = true;
    }

    public void ClearSlot()
    {
        itemName = "";
        itemSprite = null;
        isFull = false;

        // Update the UI
        itemImage.sprite = null;
        itemImage.enabled = false;
    }

    public void OnSelect(BaseEventData eventData)
    {
        selectionPanel.SetActive(true);
        if (isFull)
        {
            UpdateDescriptionUI();
        }
        else
        {
            ClearDescriptionUI();
        }
    }

    // Called when deselected
    public void OnDeselect(BaseEventData eventData)
    {
        selectionPanel.SetActive(false);
    }
    public void UpdateDescriptionUI()
    {
        // Only update if references exist
        if (itemDescriptionImage != null)
        {
            itemDescriptionImage.sprite = itemSprite;
            itemDescriptionImage.enabled = true;
        }

        if (ItemDescriptionNameText != null)
            ItemDescriptionNameText.text = itemName;

        if (ItemDescriptionText != null)
            ItemDescriptionText.text = itemDescription;
    }

    public void ClearDescriptionUI()
    {
        // Clear all description elements
        if (itemDescriptionImage != null)
        {
            itemDescriptionImage.sprite = null;
            itemDescriptionImage.enabled = false;
        }

        if (ItemDescriptionNameText != null)
            ItemDescriptionNameText.text = "No Item";

        if (ItemDescriptionText != null)
            ItemDescriptionText.text = "Select an item to view its description";
    }
}