using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ParryChargeSystem : MonoBehaviour
{
    [Header("Charge Images")]
    [SerializeField] private Image[] chargeImages = new Image[5];
    [SerializeField] private Image fullChargeIndicator;

    [Header("Gradient Backgrounds")]
    [SerializeField] private Image[] gradientBackgrounds = new Image[5]; // Same length as chargeImages
    [SerializeField] private Color gradientActiveColor = new Color(1f, 1f, 1f, 1f); // Full alpha white
    [SerializeField] private Color gradientFadeColor = new Color(1f, 1f, 1f, 0.39f); // ~100 alpha (0.39 of 255)
    [SerializeField] private float gradientPauseDuration = 0.5f; // Pause at full opacity before fading
    [SerializeField] private float gradientFadeDuration = 2.5f; // Time to fade from 255 to 100 alpha
    [SerializeField] private AnimationCurve gradientFadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0.39f);

    [Header("Colors")]
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color activeColor = Color.yellow;
    [SerializeField] private Color fullChargeColor = Color.green;
    [SerializeField] private Color partialChargeIndicatorColor = Color.blue;

    [Header("Full Charge Objects - Device Specific")]
    [SerializeField] private GameObject fullChargeKeyboardMouseObject;
    [SerializeField] private GameObject fullChargeGamepadObject;
    [SerializeField] private GameObject fullChargeTouchObject;

    [Header("Shake Settings")]
    [SerializeField] private bool enableShake = true;
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeIntensity = 5f;

    private int currentCharges = 0;
    private const int maxCharges = 5;
    private InputManager.CurrentDevice currentDevice;
    private Coroutine[] gradientCoroutines = new Coroutine[5]; // To track gradient fade coroutines

    private void Start()
    {
        ResetAllCharges();

        // Initialize gradient backgrounds to transparent
        foreach (var gradient in gradientBackgrounds)
        {
            if (gradient != null)
            {
                Color transparent = gradient.color;
                transparent.a = 0f;
                gradient.color = transparent;
            }
        }

        // Subscribe to device change events
        if (InputManager.instance != null)
        {
            InputManager.instance.onDeviceChanged += OnInputDeviceChanged;
            currentDevice = InputManager.instance.currentDevice;
        }

        UpdateFullChargeObjects();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (InputManager.instance != null)
        {
            InputManager.instance.onDeviceChanged -= OnInputDeviceChanged;
        }

        // Stop all gradient coroutines
        foreach (var coroutine in gradientCoroutines)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
    }

    private void Update()
    {
        // Optional: Continuously check for device changes if needed
        if (InputManager.instance != null && InputManager.instance.currentDevice != currentDevice)
        {
            currentDevice = InputManager.instance.currentDevice;
            UpdateFullChargeObjects();
        }
    }

    private void OnInputDeviceChanged(InputManager.CurrentDevice newDevice)
    {
        currentDevice = newDevice;
        UpdateFullChargeObjects();
        Debug.Log($"Input device changed to: {newDevice}");
    }

    public void AddCharge()
    {
        if (currentCharges >= maxCharges) return;

        currentCharges++;

        // Get the index of the charge that just became active
        int newlyActiveIndex = currentCharges - 1;

        // Update display
        UpdateChargeDisplay();

        // Shake the newly active charge
        if (enableShake && newlyActiveIndex < chargeImages.Length && chargeImages[newlyActiveIndex] != null)
        {
            StartCoroutine(ShakeImage(chargeImages[newlyActiveIndex].rectTransform));
        }

        // Activate gradient background for the new charge
        if (newlyActiveIndex < gradientBackgrounds.Length && gradientBackgrounds[newlyActiveIndex] != null)
        {
            StartGradientFade(newlyActiveIndex);
        }

        // Update full charge indicator
        fullChargeIndicator.color = (currentCharges == maxCharges) ? fullChargeColor : partialChargeIndicatorColor;

        // Update full charge objects
        UpdateFullChargeObjects();
    }

    public void ResetAllCharges()
    {
        currentCharges = 0;
        UpdateChargeDisplay();
        fullChargeIndicator.color = partialChargeIndicatorColor;

        // Reset all gradient backgrounds to transparent
        for (int i = 0; i < gradientBackgrounds.Length; i++)
        {
            if (gradientBackgrounds[i] != null)
            {
                // Stop any running fade coroutine
                if (gradientCoroutines[i] != null)
                {
                    StopCoroutine(gradientCoroutines[i]);
                    gradientCoroutines[i] = null;
                }

                // Set to transparent
                Color transparent = gradientBackgrounds[i].color;
                transparent.a = 0f;
                gradientBackgrounds[i].color = transparent;
            }
        }

        // Update full charge objects
        UpdateFullChargeObjects();
    }

    private void StartGradientFade(int gradientIndex)
    {
        // Stop existing coroutine if any
        if (gradientCoroutines[gradientIndex] != null)
        {
            StopCoroutine(gradientCoroutines[gradientIndex]);
        }

        // Start new fade coroutine with pause
        gradientCoroutines[gradientIndex] = StartCoroutine(AnimateGradientFadeWithPause(gradientIndex));
    }

    private IEnumerator AnimateGradientFadeWithPause(int gradientIndex)
    {
        if (gradientIndex >= gradientBackgrounds.Length || gradientBackgrounds[gradientIndex] == null)
            yield break;

        Image gradient = gradientBackgrounds[gradientIndex];

        // Step 1: Instantly set to full alpha (255)
        Color startColor = gradientActiveColor;
        gradient.color = startColor;

        // Step 2: Pause at full opacity
        yield return new WaitForSeconds(gradientPauseDuration);

        // Step 3: Fade down to 100 alpha over gradientFadeDuration
        float elapsed = 0f;
        Color endColor = gradientFadeColor;

        while (elapsed < gradientFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / gradientFadeDuration;
            float curveT = gradientFadeCurve.Evaluate(t);

            // Lerp between start and end colors
            gradient.color = Color.Lerp(startColor, endColor, curveT);

            yield return null;
        }

        // Ensure final color is set exactly
        gradient.color = endColor;
        gradientCoroutines[gradientIndex] = null;
    }

    // Alternative: Separate coroutines for pause and fade (more modular)
    private IEnumerator AnimateGradientFadeWithSeparatePause(int gradientIndex)
    {
        if (gradientIndex >= gradientBackgrounds.Length || gradientBackgrounds[gradientIndex] == null)
            yield break;

        Image gradient = gradientBackgrounds[gradientIndex];

        // Phase 1: Instant full opacity
        gradient.color = gradientActiveColor;

        // Phase 2: Hold at full opacity
        yield return new WaitForSeconds(gradientPauseDuration);

        // Phase 3: Fade to final opacity
        yield return StartCoroutine(FadeGradient(gradient, gradientActiveColor, gradientFadeColor, gradientFadeDuration));

        gradientCoroutines[gradientIndex] = null;
    }

    private IEnumerator FadeGradient(Image gradient, Color fromColor, Color toColor, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curveT = gradientFadeCurve.Evaluate(t);

            gradient.color = Color.Lerp(fromColor, toColor, curveT);
            yield return null;
        }

        gradient.color = toColor;
    }

    private void UpdateChargeDisplay()
    {
        for (int i = 0; i < chargeImages.Length; i++)
        {
            if (chargeImages[i] != null)
            {
                // Change color based on whether this charge is active
                chargeImages[i].color = (i < currentCharges) ? activeColor : inactiveColor;
            }
        }
    }

    private IEnumerator ShakeImage(RectTransform imageTransform)
    {
        if (imageTransform == null) yield break;

        Vector3 originalPosition = imageTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;

            // Calculate shake offset using sine wave
            float shakeX = Mathf.Sin(Time.time * 50f) * shakeIntensity * (1f - elapsed / shakeDuration);
            float shakeY = Mathf.Cos(Time.time * 50f * 0.7f) * shakeIntensity * (1f - elapsed / shakeDuration);

            // Apply shake to position
            imageTransform.localPosition = originalPosition + new Vector3(shakeX, shakeY, 0);

            yield return null;
        }

        // Reset to original position
        imageTransform.localPosition = originalPosition;
    }

    private void UpdateFullChargeObjects()
    {
        bool hasFullCharge = HasFullCharge();

        // Disable all objects first
        if (fullChargeKeyboardMouseObject != null)
            fullChargeKeyboardMouseObject.SetActive(false);
        if (fullChargeGamepadObject != null)
            fullChargeGamepadObject.SetActive(false);
        if (fullChargeTouchObject != null)
            fullChargeTouchObject.SetActive(false);

        // Enable the appropriate object based on current device and charge state
        if (hasFullCharge)
        {
            switch (currentDevice)
            {
                case InputManager.CurrentDevice.KeyboardMouse:
                    if (fullChargeKeyboardMouseObject != null)
                        fullChargeKeyboardMouseObject.SetActive(true);
                    break;
                case InputManager.CurrentDevice.Gamepad:
                    if (fullChargeGamepadObject != null)
                        fullChargeGamepadObject.SetActive(true);
                    break;
                case InputManager.CurrentDevice.Touch:
                    if (fullChargeTouchObject != null)
                        fullChargeTouchObject.SetActive(true);
                    break;
            }
        }

        Debug.Log($"Updated full charge objects - Device: {currentDevice}, Full Charge: {hasFullCharge}");
    }

    public bool HasFullCharge()
    {
        return currentCharges >= maxCharges;
    }

    // Method to manually refresh the display (useful if device changes externally)
    public void RefreshDisplay()
    {
        if (InputManager.instance != null)
        {
            currentDevice = InputManager.instance.currentDevice;
        }
        UpdateFullChargeObjects();
    }

    // Public method to manually trigger gradient fade for a specific charge
    public void TriggerGradientFade(int chargeIndex)
    {
        if (chargeIndex >= 0 && chargeIndex < gradientBackgrounds.Length && gradientBackgrounds[chargeIndex] != null)
        {
            StartGradientFade(chargeIndex);
        }
    }

    // Public method to set gradient pause duration
    public void SetGradientPauseDuration(float pauseDuration)
    {
        gradientPauseDuration = pauseDuration;
    }

    // Public method to set gradient fade duration
    public void SetGradientFadeDuration(float fadeDuration)
    {
        gradientFadeDuration = fadeDuration;
    }

    // For testing in editor without parrying
    [ContextMenu("Add Test Charge")]
    private void AddTestCharge()
    {
        AddCharge();
    }

    [ContextMenu("Reset Charges")]
    private void ResetTestCharges()
    {
        ResetAllCharges();
    }

    [ContextMenu("Test Gradient Fade for Charge 0")]
    private void TestGradientFade0()
    {
        TriggerGradientFade(0);
    }

    [ContextMenu("Test Gradient Fade for Charge 2")]
    private void TestGradientFade2()
    {
        TriggerGradientFade(2);
    }

    [ContextMenu("Test Gradient Fade for All Active")]
    private void TestGradientFadeAllActive()
    {
        for (int i = 0; i < currentCharges; i++)
        {
            TriggerGradientFade(i);
        }
    }

    [ContextMenu("Simulate Keyboard Input")]
    private void SimulateKeyboardInput()
    {
        if (InputManager.instance != null)
        {
            currentDevice = InputManager.CurrentDevice.KeyboardMouse;
            UpdateFullChargeObjects();
        }
    }

    [ContextMenu("Simulate Gamepad Input")]
    private void SimulateGamepadInput()
    {
        if (InputManager.instance != null)
        {
            currentDevice = InputManager.CurrentDevice.Gamepad;
            UpdateFullChargeObjects();
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Print Gradient Alphas")]
    private void PrintGradientAlphas()
    {
        for (int i = 0; i < gradientBackgrounds.Length; i++)
        {
            if (gradientBackgrounds[i] != null)
            {
                Debug.Log($"Gradient {i} alpha: {gradientBackgrounds[i].color.a}");
            }
        }
    }

    [ContextMenu("Test Different Pause Times")]
    private void TestPauseTimes()
    {
        StartCoroutine(TestPauseSequence());
    }

    private IEnumerator TestPauseSequence()
    {
        float originalPause = gradientPauseDuration;

        Debug.Log("Testing pause 0.2s");
        gradientPauseDuration = 0.2f;
        TriggerGradientFade(0);
        yield return new WaitForSeconds(3f);

        Debug.Log("Testing pause 0.5s");
        gradientPauseDuration = 0.5f;
        TriggerGradientFade(1);
        yield return new WaitForSeconds(3f);

        Debug.Log("Testing pause 1.0s");
        gradientPauseDuration = 1.0f;
        TriggerGradientFade(2);
        yield return new WaitForSeconds(3f);

        // Restore original
        gradientPauseDuration = originalPause;
        Debug.Log("Test complete, restored original pause");
    }
#endif
}