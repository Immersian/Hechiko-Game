using UnityEngine;

public class CameraMovementDetector : MonoBehaviour
{
    public ParallaxCamera parallaxCamera;
    private Vector3 lastCameraPosition;

    void Start()
    {
        if (parallaxCamera == null)
            parallaxCamera = GetComponent<ParallaxCamera>();

        lastCameraPosition = transform.position;
    }

    void Update()
    {
        // Calculate how much the camera actually moved in world space
        Vector3 currentPosition = transform.position;
        float deltaX = lastCameraPosition.x - currentPosition.x;

        // Only trigger parallax if there was actual movement
        if (deltaX != 0 && parallaxCamera != null && parallaxCamera.onCameraTranslate != null)
        {
            parallaxCamera.onCameraTranslate(deltaX);
        }

        lastCameraPosition = currentPosition;
    }
}