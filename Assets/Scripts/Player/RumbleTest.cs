using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RumbleTest : MonoBehaviour
{
    private void Update()
    {
        // Access the control scheme properly
        if (InputManager.instance.inputControl.Rumble.RumbleAction.WasPressedThisFrame())
        {
            RumbleManager.instance.RumblePulse(0.25f, 1f, 0.25f);
        }
    }
}
