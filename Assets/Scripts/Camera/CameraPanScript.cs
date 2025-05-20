using UnityEngine;
using Cinemachine;

public class CameraPanScript : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("Main virtual camera (should have lower priority)")]
    [SerializeField] private CinemachineVirtualCamera mainVirtualCamera;

    [Tooltip("Trigger virtual camera (should have higher priority)")]
    [SerializeField] private CinemachineVirtualCamera triggerVirtualCamera;

    [Header("Trigger Settings")]
    [Tooltip("Layer mask for objects that trigger the camera change")]
    [SerializeField] private LayerMask triggerLayer;

    [Tooltip("Should the camera revert when exiting the trigger?")]
    [SerializeField] private bool revertOnExit = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & triggerLayer) != 0)
        {
            if (triggerVirtualCamera != null)
            {
                // Set the trigger camera to highest priority
                triggerVirtualCamera.Priority = 100;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & triggerLayer) != 0 && revertOnExit)
        {
            if (mainVirtualCamera != null)
            {
                // Return priority to main camera
                mainVirtualCamera.Priority = 100;
                // Reset trigger camera priority to lower value
                if (triggerVirtualCamera != null)
                {
                    triggerVirtualCamera.Priority = 0;
                }
            }
        }
    }
}