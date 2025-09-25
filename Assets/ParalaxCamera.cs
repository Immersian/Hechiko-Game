using UnityEngine;
using Cinemachine;

[ExecuteInEditMode]
public class ParallaxCamera : MonoBehaviour
{
    public delegate void ParallaxCameraDelegate(float deltaMovement);
    public ParallaxCameraDelegate onCameraTranslate;

    private float oldPosition;
    private Camera mainCamera;
    private CinemachineBrain cinemachineBrain;

    void Start()
    {
        mainCamera = Camera.main;
        cinemachineBrain = mainCamera != null ? mainCamera.GetComponent<CinemachineBrain>() : null;
        oldPosition = GetEffectiveCameraPosition();
    }

    void Update()
    {
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
        // If we have a CinemachineBrain and it's active, use the live camera position
        if (cinemachineBrain != null && cinemachineBrain.IsLive(GetComponent<CinemachineVirtualCamera>()))
        {
            return mainCamera.transform.position.x;
        }

        // Fallback to the virtual camera's position
        return transform.position.x;
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