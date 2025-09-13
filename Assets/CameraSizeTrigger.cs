using UnityEngine;
using Cinemachine;

public class CameraSizeTrigger : MonoBehaviour
{
    [Header("Cinemachine Camera Settings")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private float targetOrthoSize = 15f;
    [SerializeField] private float transitionSpeed = 5f;

    [Header("Confinement Settings")]
    [SerializeField] private Collider2D zoomedConfinementBounds;
    [SerializeField] private bool useDynamicConfinement = true;

    private float originalOrthoSize;
    private bool isPlayerInTrigger = false;
    private CinemachineConfiner2D confiner;
    private Collider2D originalConfinerBounds;

    private void Start()
    {
        if (virtualCamera == null)
        {
            virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
        }

        if (virtualCamera != null)
        {
            originalOrthoSize = virtualCamera.m_Lens.OrthographicSize;

            // Get the confiner component
            confiner = virtualCamera.GetComponent<CinemachineConfiner2D>();
            if (confiner != null)
            {
                originalConfinerBounds = confiner.m_BoundingShape2D;
            }
        }
    }

    private void Update()
    {
        if (virtualCamera == null) return;

        float targetSize = isPlayerInTrigger ? targetOrthoSize : originalOrthoSize;

        float newSize = Mathf.Lerp(
            virtualCamera.m_Lens.OrthographicSize,
            targetSize,
            transitionSpeed * Time.deltaTime
        );

        virtualCamera.m_Lens.OrthographicSize = newSize;

        // Update confiner bounds based on current camera size
        UpdateConfinerBounds(newSize);
    }

    private void UpdateConfinerBounds(float currentOrthoSize)
    {
        if (confiner != null && useDynamicConfinement)
        {
            // Switch to zoomed bounds when camera is significantly larger than original
            if (currentOrthoSize > originalOrthoSize * 1.2f && zoomedConfinementBounds != null)
            {
                confiner.m_BoundingShape2D = zoomedConfinementBounds;
            }
            else
            {
                confiner.m_BoundingShape2D = originalConfinerBounds;
            }

            // Force the confiner to recalculate
            confiner.InvalidateCache();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = false;
        }
    }

    private void OnDisable()
    {
        ResetCameraSettings();
    }

    private void ResetCameraSettings()
    {
        if (virtualCamera != null)
        {
            virtualCamera.m_Lens.OrthographicSize = originalOrthoSize;
        }

        if (confiner != null && useDynamicConfinement)
        {
            confiner.m_BoundingShape2D = originalConfinerBounds;
            confiner.InvalidateCache();
        }
    }

    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        if (zoomedConfinementBounds != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(zoomedConfinementBounds.bounds.center, zoomedConfinementBounds.bounds.size);
        }
    }
}