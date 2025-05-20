using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager instance;
    [HideInInspector] public Controller inputControl;
    [HideInInspector] public PlayerInput playerInput;

    public enum CurrentDevice { KeyboardMouse, Gamepad, Touch }
    public CurrentDevice currentDevice { get; private set; } = CurrentDevice.KeyboardMouse;
    public System.Action<CurrentDevice> onDeviceChanged;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        inputControl = new Controller();
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        inputControl.Enable();
        InputSystem.onActionChange += OnActionChange;
    }

    private void OnDisable()
    {
        inputControl.Disable();
        InputSystem.onActionChange -= OnActionChange;
    }
    public void SetGameplayInputEnabled(bool enable)
    {
        if (enable)
        {
            inputControl.Gameplay.Enable();
        }
        else
        {
            inputControl.Gameplay.Disable();
        }
    }
    public void SetDialogueInputEnabled(bool enable)
    {
        if (enable)
        {
            inputControl.Dialogue.Interact.Enable();
        }
        else
        {
            inputControl.Dialogue.Interact.Disable();
        }
    }    
    public void SetLookInputEnabled(bool enable)
    {
        if (enable)
        {
            inputControl.Gameplay.Look.Enable();
        }
        else
        {
            inputControl.Gameplay.Look.Disable();
        }
    }
    private void OnActionChange(object obj, InputActionChange change)
    {
        if (change == InputActionChange.ActionPerformed)
        {
            var inputAction = (InputAction)obj;
            var lastControl = inputAction.activeControl;
            var lastDevice = lastControl.device;

            CurrentDevice newDevice;
            if (lastDevice is Gamepad)
            {
                newDevice = CurrentDevice.Gamepad;
            }
            else if (lastDevice is Keyboard || lastDevice is Mouse)
            {
                newDevice = CurrentDevice.KeyboardMouse;
            }
            else if (lastDevice is Touchscreen)
            {
                newDevice = CurrentDevice.Touch;
            }
            else
            {
                newDevice = currentDevice; // no change
            }

            if (newDevice != currentDevice)
            {
                currentDevice = newDevice;
                onDeviceChanged?.Invoke(currentDevice);
            }
        }
    }

    // Helper method to check current device
    public bool IsGamepad()
    {
        return currentDevice == CurrentDevice.Gamepad;
    }

    public bool IsKeyboardMouse()
    {
        return currentDevice == CurrentDevice.KeyboardMouse;
    }
}