using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class ShockWaveManager : MonoBehaviour
{
    public static ShockWaveManager Instance { get; private set; }

    [SerializeField] private float shockWaveTime = 0.75f;
    private Coroutine shockWaveCoroutine;
    private Material shockWaveMaterial;
    private static int waveDistanceFromCenter;

    public event System.Action onShockWave;
    private bool isShockwaveActive;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        waveDistanceFromCenter = Shader.PropertyToID("_Wave_Distance_From_Centre");
        shockWaveMaterial = GetComponent<SpriteRenderer>().sharedMaterial;
    }

    private void Update()
    {
        if (InputManager.instance.inputControl.Gameplay.ShockwaveTest.WasPressedThisFrame())
        {
            ToggleShockwave();
        }
    }

    public void ToggleShockwave()
    {

        shockWaveCoroutine = StartCoroutine(ShockWaveAction(-0.1f, 1f));
    }

    private IEnumerator ShockWaveAction(float startPos, float endPos)
    {
        shockWaveMaterial.SetFloat(waveDistanceFromCenter, startPos);

        float elapsedTime = 0f;

        while (elapsedTime < shockWaveTime)
        {
            elapsedTime += Time.deltaTime;
            float lerpedAmount = Mathf.Lerp(startPos, endPos, (elapsedTime / shockWaveTime));
            shockWaveMaterial.SetFloat(waveDistanceFromCenter, lerpedAmount);
            yield return null;
        }

        shockWaveMaterial.SetFloat(waveDistanceFromCenter, endPos);
    }

    private void OnDestroy()
    {
        if (shockWaveCoroutine != null)
        {
            StopCoroutine(shockWaveCoroutine);
        }
    }
}