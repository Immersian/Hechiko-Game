using UnityEngine;
using Cinemachine;

public class CameraTrigger2D : MonoBehaviour
{
    [Header("Camera Settings")]
    public CinemachineVirtualCamera targetVirtualCamera;
    public bool returnToMainCameraOnExit = true;

    [Header("Auto-find Settings")]
    public bool autoFindMainCamera = true;
    public string targetCameraTag = ""; // Optional: use tag to identify target camera

    private CinemachineVirtualCamera mainVirtualCamera;
    private CameraManager cameraManager;

    void Start()
    {
        // Find CameraManager in scene
        cameraManager = FindObjectOfType<CameraManager>();

        if (cameraManager == null)
        {
            Debug.LogError("CameraManager not found in scene!", this);
            return;
        }

        // Auto-find main camera if enabled
        if (autoFindMainCamera)
        {
            FindMainCamera();
        }

        // If target camera is not assigned but we have a tag, try to find it by tag
        if (targetVirtualCamera == null && !string.IsNullOrEmpty(targetCameraTag))
        {
            GameObject taggedCamera = GameObject.FindGameObjectWithTag(targetCameraTag);
            if (taggedCamera != null)
            {
                targetVirtualCamera = taggedCamera.GetComponent<CinemachineVirtualCamera>();
                if (targetVirtualCamera == null)
                {
                    Debug.LogWarning($"Found object with tag '{targetCameraTag}' but it has no CinemachineVirtualCamera component!", this);
                }
            }
        }

        // Validate setup
        if (targetVirtualCamera == null)
        {
            Debug.LogError("Target Virtual Camera is not assigned and could not be found automatically!", this);
        }
    }

    private void FindMainCamera()
    {
        // Try to get main camera from CameraManager if it has a reference
        if (cameraManager != null)
        {
            // You might need to adjust this based on your CameraManager implementation
            // Common patterns include:
            // - cameraManager.MainCamera property
            // - cameraManager.GetMainCamera() method
            // - cameraManager storing main camera in a field

            // If CameraManager has a main camera reference, use it
            var cameraManagerType = cameraManager.GetType();
            var mainCameraField = cameraManagerType.GetField("mainVirtualCamera");
            var mainCameraProperty = cameraManagerType.GetProperty("MainVirtualCamera");

            if (mainCameraField != null && mainCameraField.FieldType == typeof(CinemachineVirtualCamera))
            {
                mainVirtualCamera = (CinemachineVirtualCamera)mainCameraField.GetValue(cameraManager);
            }
            else if (mainCameraProperty != null && mainCameraProperty.PropertyType == typeof(CinemachineVirtualCamera))
            {
                mainVirtualCamera = (CinemachineVirtualCamera)mainCameraProperty.GetValue(cameraManager);
            }
        }

        // Fallback: find camera with "MainCamera" tag or highest priority
        if (mainVirtualCamera == null)
        {
            CinemachineVirtualCamera[] allCameras = FindObjectsOfType<CinemachineVirtualCamera>();
            if (allCameras.Length > 0)
            {
                // Find camera with "MainCamera" tag first
                foreach (var cam in allCameras)
                {
                    if (cam.CompareTag("MainCamera"))
                    {
                        mainVirtualCamera = cam;
                        break;
                    }
                }

                // If no tagged camera found, use the one with highest priority
                if (mainVirtualCamera == null)
                {
                    CinemachineVirtualCamera highestPriorityCamera = allCameras[0];
                    foreach (var cam in allCameras)
                    {
                        if (cam.Priority > highestPriorityCamera.Priority)
                        {
                            highestPriorityCamera = cam;
                        }
                    }
                    mainVirtualCamera = highestPriorityCamera;
                }
            }
        }

        if (mainVirtualCamera != null)
        {
            Debug.Log($"Auto-found main camera: {mainVirtualCamera.name}", this);
        }
        else
        {
            Debug.LogWarning("Could not auto-find main virtual camera!", this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && targetVirtualCamera != null && cameraManager != null)
        {
            CameraManager.SwitchCamera(targetVirtualCamera);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && returnToMainCameraOnExit && mainVirtualCamera != null && cameraManager != null)
        {
            CameraManager.SwitchCamera(mainVirtualCamera);
        }
    }

    // Public methods for manual control
    public void SetTargetCamera(CinemachineVirtualCamera newTarget)
    {
        targetVirtualCamera = newTarget;
    }

    public void SetMainCamera(CinemachineVirtualCamera newMain)
    {
        mainVirtualCamera = newMain;
    }

    // Editor helper to quickly assign cameras
    [ContextMenu("Auto-Assign Cameras")]
    private void AutoAssignCameras()
    {
        CinemachineVirtualCamera[] cameras = FindObjectsOfType<CinemachineVirtualCamera>();

        if (cameras.Length >= 2)
        {
            // Assume the camera with highest priority is main
            CinemachineVirtualCamera highestPriority = cameras[0];
            CinemachineVirtualCamera lowestPriority = cameras[0];

            foreach (var cam in cameras)
            {
                if (cam.Priority > highestPriority.Priority) highestPriority = cam;
                if (cam.Priority < lowestPriority.Priority) lowestPriority = cam;
            }

            mainVirtualCamera = highestPriority;
            targetVirtualCamera = lowestPriority;

            Debug.Log($"Auto-assigned: Main={highestPriority.name}, Target={lowestPriority.name}");
        }
    }
}