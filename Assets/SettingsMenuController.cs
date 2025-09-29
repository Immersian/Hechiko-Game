using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SettingsMenuController : MonoBehaviour
{
    [SerializeField] private Button firstSelectedButton;

    private void OnEnable()
    {
        if (firstSelectedButton != null && firstSelectedButton.interactable)
        {
            // Simple selection
            firstSelectedButton.Select();

            // Ensure EventSystem knows about it
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);
            }
        }
    }
}