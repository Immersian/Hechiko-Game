using Cinemachine;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CameraShake : MonoBehaviour
{
    private CinemachineVirtualCamera virtualCamera;
    private CinemachineBasicMultiChannelPerlin perlinNoise;

    private void Awake()
    {
        virtualCamera = GetComponent<CinemachineVirtualCamera>();

        if (virtualCamera == null)
        {
            Debug.LogError("No CinemachineVirtualCamera found on this GameObject!", this);
            return;
        }

        perlinNoise = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

        if (perlinNoise == null)
        {
            Debug.LogError("No CinemachineBasicMultiChannelPerlin component found on the virtual camera! " +
                         "Add Noise to your CinemachineVirtualCamera.", this);
            enabled = false; // Disable this script if setup is invalid
            return;
        }

        ResetIntensity();
    }

    public void ShakeCamera(float intensity, float shakeTime)
    {
        if (perlinNoise == null) return;

        perlinNoise.m_AmplitudeGain = intensity;
        StartCoroutine(WaitTime(shakeTime));
    }

    IEnumerator WaitTime(float shakeTime)
    {
        yield return new WaitForSeconds(shakeTime);
        ResetIntensity();
    }

    void ResetIntensity()
    {
        if (perlinNoise != null)
            perlinNoise.m_AmplitudeGain = 0.0f;
    }
}