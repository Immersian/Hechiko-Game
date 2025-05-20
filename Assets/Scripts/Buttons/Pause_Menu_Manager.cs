using SupanthaPaul;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems; // Required for EventSystem

public class Pause_Menu_Manager : MonoBehaviour
{
    public static bool Paused = false;
    public GameObject PauseMenuCanvas;
    public UnityEngine.UI.Button firstSelectedButton; // Assign in Inspector

    void Start()
    {
        Time.timeScale = 1.0f;
        PauseMenuCanvas.SetActive(false);
    }

    void Update()
    {
        if (InputManager.instance.inputControl.Pause.Pause.WasPressedThisFrame())
        {
            if (Paused)
            {
                Play();
            }
            else
            {
                Stop();
            }
        }
    }

    void Stop()
    {
        PauseMenuCanvas.SetActive(true);
        Time.timeScale = 0.0f;
        InputManager.instance.SetGameplayInputEnabled(false);

        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.DisableMovement();
        }

        // Set the first selected button
        if (firstSelectedButton != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);
        }

        Paused = true;
    }

    public void Play()
    {
        PauseMenuCanvas.SetActive(false);
        Time.timeScale = 1f;
        InputManager.instance.SetGameplayInputEnabled(true);

        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.EnableMovement();
        }

        // Clear selection when unpausing
        EventSystem.current.SetSelectedGameObject(null);

        Paused = false;
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Should Quit the game");
    }
}