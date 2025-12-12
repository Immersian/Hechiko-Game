using UnityEngine;
using Cinemachine;

public class ParallaxCamera : MonoBehaviour
{
    public delegate void ParallaxCameraDelegate(float deltaMovement);
    public ParallaxCameraDelegate onCameraTranslate;

    private float oldPosition;
    private Camera mainCamera;
    private CinemachineBrain cinemachineBrain;
    private CinemachineVirtualCamera virtualCamera;

    void Start()
    {
        mainCamera = Camera.main;
        cinemachineBrain = mainCamera != null ? mainCamera.GetComponent<CinemachineBrain>() : null;
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        oldPosition = GetEffectiveCameraPosition();
    }

    void Update()
    {
        // Only run in play mode
        if (!Application.isPlaying) return;

        float currentPosition = GetEffectiveCameraPosition();

        if (currentPosition != oldPosition)
        {
            if (onCameraTranslate != null)
            {
                float delta = oldPosition - currentPosition;
                onCameraTranslate(delta);
            }

            oldPosition = currentPosition;
        }
    }

    private float GetEffectiveCameraPosition()
    {
        // If this virtual camera is the active one, use the live camera position
        if (CameraManager.IsActiveCamera(virtualCamera))
        {
            return mainCamera.transform.position.x;
        }

        // Alternative method: Check if CinemachineBrain is using this camera
        if (cinemachineBrain != null && cinemachineBrain.ActiveVirtualCamera != null)
        {
            if (cinemachineBrain.ActiveVirtualCamera.VirtualCameraGameObject == gameObject)
            {
                return mainCamera.transform.position.x;
            }
        }

        // If this camera is not active, return the last known position to prevent shifting
        return oldPosition;
    }

    // Clean up delegate when destroyed
    private void OnDestroy()
    {
        if (onCameraTranslate != null)
        {
            System.Delegate[] delegates = onCameraTranslate.GetInvocationList();
            foreach (System.Delegate del in delegates)
            {
                onCameraTranslate -= (ParallaxCameraDelegate)del;
            }
        }
    }
}