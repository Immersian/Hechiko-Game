using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonSelectionToggle : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [Header("Target Object")]
    [SerializeField] private GameObject targetObject;

    [Header("Button Reference")]
    [SerializeField] private Button targetButton;

    private void Start()
    {
        // If no button is assigned, try to get it from this GameObject
        if (targetButton == null)
        {
            targetButton = GetComponent<Button>();
        }

        // If no target object is assigned, use this GameObject
        if (targetObject == null)
        {
            targetObject = gameObject;
        }

        // Set initial state based on whether the button is selected
        if (targetButton != null)
        {
            bool isSelected = EventSystem.current.currentSelectedGameObject == targetButton.gameObject;
            targetObject.SetActive(isSelected);
        }
    }

    private void Update()
    {
        // Optional: Continuously check selection status if needed
        // This can be useful if selection changes through other means
        if (targetButton != null && targetObject != null)
        {
            bool isSelected = EventSystem.current.currentSelectedGameObject == targetButton.gameObject;
            if (targetObject.activeSelf != isSelected)
            {
                targetObject.SetActive(isSelected);
            }
        }
    }

    // Called when the button becomes selected
    public void OnSelect(BaseEventData eventData)
    {
        if (targetObject != null)
        {
            targetObject.SetActive(true);
        }
    }

    // Called when the button becomes deselected
    public void OnDeselect(BaseEventData eventData)
    {
        if (targetObject != null)
        {
            targetObject.SetActive(false);
        }
    }

    // Optional: Public method to manually refresh the state
    public void RefreshState()
    {
        if (targetButton != null && targetObject != null)
        {
            bool isSelected = EventSystem.current.currentSelectedGameObject == targetButton.gameObject;
            targetObject.SetActive(isSelected);
        }
    }
}