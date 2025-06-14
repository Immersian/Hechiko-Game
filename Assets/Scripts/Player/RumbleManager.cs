using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class RumbleManager : MonoBehaviour
{
    public static RumbleManager instance;
    private Gamepad pad;
    private Coroutine stopRumbleAfterTimeCoroutine;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Optional: Keep between scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Wait until InputManager is ready
        StartCoroutine(InitializeAfterInputManager());
    }

    private IEnumerator InitializeAfterInputManager()
    {
        // Wait until InputManager exists and is initialized
        while (InputManager.instance == null || InputManager.instance.playerInput == null)
        {
            yield return null;
        }

        // Now safe to setup events
        InputManager.instance.playerInput.onControlsChanged += SwitchControls;
    }

    public void RumblePulse(float lowFrequency, float highFrequency, float duration)
    {
        pad = Gamepad.current;

        if (pad != null)
        {
            // Stop any existing rumble
            if (stopRumbleAfterTimeCoroutine != null)
            {
                StopCoroutine(stopRumbleAfterTimeCoroutine);
            }

            pad.SetMotorSpeeds(lowFrequency, highFrequency);
            stopRumbleAfterTimeCoroutine = StartCoroutine(StopRumble(duration, pad));
        }
    }

    private IEnumerator StopRumble(float duration, Gamepad pad)
    {
        yield return new WaitForSeconds(duration);
        pad.SetMotorSpeeds(0f, 0f);
    }

    private void SwitchControls(PlayerInput input)
    {
        Debug.Log("Device is now: " + input.currentControlScheme);
        // Reset rumble when controls change
        if (pad != null)
        {
            pad.SetMotorSpeeds(0f, 0f);
        }
    }

    private void OnDisable()
    {
        // Safely unsubscribe
        if (InputManager.instance != null &&
            InputManager.instance.playerInput != null)
        {
            InputManager.instance.playerInput.onControlsChanged -= SwitchControls;
        }

        // Ensure rumble stops when disabled
        if (pad != null)
        {
            pad.SetMotorSpeeds(0f, 0f);
        }
    }

    private void OnDestroy()
    {
        // Extra safety for scene changes
        if (pad != null)
        {
            pad.SetMotorSpeeds(0f, 0f);
        }
    }
}