using UnityEngine;
using Cinemachine;

public class CameraTrigger2D : MonoBehaviour
{
    [Header("Camera Settings")]
    public CinemachineVirtualCamera targetVirtualCamera;
    public CinemachineVirtualCamera mainVirtualCamera; // Assign your main camera here
    public bool returnToMainCameraOnExit = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && targetVirtualCamera != null)
        {
            CameraManager.SwitchCamera(targetVirtualCamera);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && returnToMainCameraOnExit && mainVirtualCamera != null)
        {
            CameraManager.SwitchCamera(mainVirtualCamera);
        }
    }
}