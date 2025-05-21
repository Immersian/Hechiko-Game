using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private CinemachineVirtualCamera virtualCamera;
    private CinemachineBasicMultiChannelPerlin perlinNoise;

    private void Awake()
    {
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        perlinNoise = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        ResetIntensity();
    }
    public void ShakeCamera(float intensity, float shakeTime)
    {
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
        perlinNoise.m_AmplitudeGain = 0.0f;
    }
}