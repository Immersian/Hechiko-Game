using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class ButtonHoverLerp : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [Header("Lerp Settings")]
    public RectTransform imageToMove;        // The image that will lerp
    public Transform targetPosition;         // Empty GameObject next to buttons
    public float lerpSpeed = 5f;             // Speed of the lerp movement

    [Header("Button References")]
    public Button[] buttons;                 // Array of buttons to track

    private Vector3 originalPosition;        // Starting position of the image
    private bool isSelected = false;         // Track if any button is selected

    void Start()
    {
        // Store the original position of the image
        if (imageToMove != null)
        {
            originalPosition = imageToMove.position;
        }

        // Add event listeners to all buttons
        foreach (Button button in buttons)
        {
            // Add this component to buttons if not already present
            if (button.gameObject != this.gameObject)
            {
                var hoverComponent = button.gameObject.AddComponent<ButtonHoverLerp>();
                hoverComponent.imageToMove = this.imageToMove;
                hoverComponent.targetPosition = this.targetPosition;
                hoverComponent.lerpSpeed = this.lerpSpeed;
                hoverComponent.buttons = this.buttons;
            }

            // Add click event to return image to original position
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    void Update()
    {
        if (imageToMove == null || targetPosition == null) return;

        // Determine target position based on selection state
        Vector3 target = isSelected ? targetPosition.position : originalPosition;

        // Smoothly lerp the image to the target position
        imageToMove.position = Vector3.Lerp(imageToMove.position, target, lerpSpeed * Time.deltaTime);
    }

    // Called when a button is selected
    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
    }

    // Called when a button is deselected
    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
    }

    // Called when a button is clicked
    private void OnButtonClicked()
    {
        isSelected = false;
    }

    // Editor function to help set up the target position
    void OnDrawGizmosSelected()
    {
        if (targetPosition != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPosition.position, 10f);
        }
    }
}