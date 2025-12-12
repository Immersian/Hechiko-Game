using Cinemachine;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    public ParallaxCamera parallaxCamera;
    List<ParallaxLayer> parallaxLayers = new List<ParallaxLayer>();
    private ParallaxCamera[] allParallaxCameras;
    private ParallaxCamera currentActiveParallaxCamera;

    void Start()
    {
        FindAllParallaxCameras();
        SetLayers();
        UpdateActiveParallaxCamera();
    }

    void OnDisable()
    {
        // Unsubscribe from events
        if (currentActiveParallaxCamera != null)
        {
            currentActiveParallaxCamera.onCameraTranslate -= Move;
        }
    }

    void Update()
    {
        // Check if the active parallax camera has changed
        UpdateActiveParallaxCamera();
    }

    void FindAllParallaxCameras()
    {
        allParallaxCameras = FindObjectsOfType<ParallaxCamera>();
    }

    void UpdateActiveParallaxCamera()
    {
        // Find the parallax camera attached to the active virtual camera
        ParallaxCamera newActiveCamera = null;

        if (CameraManager.ActiveCamera != null)
        {
            newActiveCamera = CameraManager.ActiveCamera.GetComponent<ParallaxCamera>();
        }

        // If no parallax camera found on active camera, try to find the main one
        if (newActiveCamera == null && allParallaxCameras != null)
        {
            foreach (var cam in allParallaxCameras)
            {
                if (cam.gameObject.name.ToLower().Contains("main") ||
                    CameraManager.IsActiveCamera(cam.GetComponent<CinemachineVirtualCamera>()))
                {
                    newActiveCamera = cam;
                    break;
                }
            }
        }

        // If we found a new active camera, switch to it
        if (newActiveCamera != currentActiveParallaxCamera)
        {
            // Unsubscribe from old camera
            if (currentActiveParallaxCamera != null)
            {
                currentActiveParallaxCamera.onCameraTranslate -= Move;
            }

            // Subscribe to new camera
            currentActiveParallaxCamera = newActiveCamera;
            if (currentActiveParallaxCamera != null)
            {
                currentActiveParallaxCamera.onCameraTranslate += Move;
            }
        }
    }

    void SetLayers()
    {
        parallaxLayers.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            ParallaxLayer layer = transform.GetChild(i).GetComponent<ParallaxLayer>();

            if (layer != null)
            {
                layer.name = "Layer-" + i;
                parallaxLayers.Add(layer);
            }
        }
    }

    void Move(float delta)
    {
        foreach (ParallaxLayer layer in parallaxLayers)
        {
            layer.Move(delta);
        }
    }
}