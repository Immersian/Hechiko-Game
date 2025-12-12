using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine.UI;

public class CutsceneTriggerBoss : MonoBehaviour
{
    [Header("Cutscene Settings")]
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private BakaBossStateManager bossStateManager;

    [Header("Health Bar Settings")]
    [SerializeField] private Image healthBarFill; // Main health bar fill
    [SerializeField] private Image healthBarOutline; // Health bar outline as separate Image
    [SerializeField] private Image extraImage; // Your extra image that should fade with health bar
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private float fadeDelay = 0.5f;

    [Header("Border Settings")]
    [SerializeField] private GameObject borderObject; // The border that should be enabled during cutscene
    [SerializeField] private bool disableBorderAfterCutscene = true; // Whether to disable border after cutscene
    [SerializeField] private AudioSource borderAudioSource; // Audio to play when border is enabled

    [Header("Particle System Settings - Tag Based")]
    [SerializeField] private string mainParticleTag = "BossMainParticles"; // Tag for main particle system
    [SerializeField] private string secondaryParticleTag = "BossSecondaryParticles"; // Tag for secondary particle system
    [SerializeField] private float targetMinSpeed = 12.5f;
    [SerializeField] private float targetMaxSpeed = 15f;
    [SerializeField] private float speedChangeDuration = 1.5f; // How long it takes to change speed
    [SerializeField] private AnimationCurve speedChangeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // Particle system references (found by tag)
    private ParticleSystem mainParticleSystem;
    private ParticleSystem secondaryParticleSystem;

    // Store initial particle values
    private float initialMinSpeed;
    private float initialMaxSpeed;
    private bool hasStoredInitialValues = false;
    private bool areParticlesInitialized = false;

    private Color initialFillColor;
    private Color initialOutlineColor;
    private Color initialExtraImageColor;

    private void Awake()
    {
        // Store initial colors and disable visuals
        if (healthBarFill != null)
        {
            initialFillColor = healthBarFill.color;
            healthBarFill.gameObject.SetActive(false);
        }

        if (healthBarOutline != null)
        {
            initialOutlineColor = healthBarOutline.color;
            healthBarOutline.gameObject.SetActive(false);
        }

        if (extraImage != null)
        {
            initialExtraImageColor = extraImage.color;
            extraImage.gameObject.SetActive(false);
        }

        // Ensure border is disabled initially
        if (borderObject != null)
        {
            borderObject.SetActive(false);
        }

        // Ensure audio is disabled initially
        if (borderAudioSource != null)
        {
            borderAudioSource.enabled = false;
        }
    }

    private void Start()
    {
        // Initialize particle systems by tag
        InitializeParticleSystemsByTag();

        // Double-check that border is disabled on start
        if (borderObject != null && borderObject.activeSelf)
        {
            Debug.LogWarning("Border was enabled on start, disabling it");
            borderObject.SetActive(false);
        }

        // Ensure secondary particle system is disabled initially
        if (secondaryParticleSystem != null)
        {
            secondaryParticleSystem.gameObject.SetActive(false);
        }
    }

    private void InitializeParticleSystemsByTag()
    {
        // Find main particle system by tag
        if (!string.IsNullOrEmpty(mainParticleTag))
        {
            GameObject mainParticleObj = GameObject.FindWithTag(mainParticleTag);
            if (mainParticleObj != null)
            {
                mainParticleSystem = mainParticleObj.GetComponent<ParticleSystem>();
                if (mainParticleSystem != null)
                {
                    // Store initial speed values
                    var main = mainParticleSystem.main;
                    initialMinSpeed = main.startSpeed.constantMin;
                    initialMaxSpeed = main.startSpeed.constantMax;
                    hasStoredInitialValues = true;

                    Debug.Log($"Found main particle system with tag '{mainParticleTag}': {mainParticleObj.name}");
                }
                else
                {
                    Debug.LogError($"Found GameObject with tag '{mainParticleTag}' but it has no ParticleSystem component!");
                }
            }
            else
            {
                Debug.LogWarning($"No GameObject found with tag '{mainParticleTag}'");
            }
        }
        else
        {
            Debug.LogWarning("Main particle tag is not set!");
        }

        // Find secondary particle system by tag
        if (!string.IsNullOrEmpty(secondaryParticleTag))
        {
            GameObject secondaryParticleObj = GameObject.FindWithTag(secondaryParticleTag);
            if (secondaryParticleObj != null)
            {
                secondaryParticleSystem = secondaryParticleObj.GetComponent<ParticleSystem>();
                if (secondaryParticleSystem != null)
                {
                    Debug.Log($"Found secondary particle system with tag '{secondaryParticleTag}': {secondaryParticleObj.name}");
                }
                else
                {
                    Debug.LogError($"Found GameObject with tag '{secondaryParticleTag}' but it has no ParticleSystem component!");
                }
            }
            else
            {
                Debug.LogWarning($"No GameObject found with tag '{secondaryParticleTag}'");
            }
        }
        else
        {
            Debug.Log("Secondary particle tag is not set, skipping.");
        }

        areParticlesInitialized = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Initialize particles if not already done
            if (!areParticlesInitialized)
            {
                InitializeParticleSystemsByTag();
            }

            // Enable border before cutscene starts
            if (borderObject != null)
            {
                borderObject.SetActive(true);
                Debug.Log("Border enabled for cutscene");

                // Enable and play border audio if assigned
                if (borderAudioSource != null)
                {
                    borderAudioSource.enabled = true;
                    if (!borderAudioSource.isPlaying)
                    {
                        borderAudioSource.Play();
                        Debug.Log("Playing border audio");
                    }
                }
            }

            // Activate with full transparency
            if (healthBarFill != null)
            {
                healthBarFill.gameObject.SetActive(true);
                healthBarFill.color = new Color(initialFillColor.r, initialFillColor.g, initialFillColor.b, 0f);
            }

            if (healthBarOutline != null)
            {
                healthBarOutline.gameObject.SetActive(true);
                healthBarOutline.color = new Color(
                    initialOutlineColor.r,
                    initialOutlineColor.g,
                    initialOutlineColor.b,
                    0f
                );
            }

            if (extraImage != null)
            {
                extraImage.gameObject.SetActive(true);
                extraImage.color = new Color(
                    initialExtraImageColor.r,
                    initialExtraImageColor.g,
                    initialExtraImageColor.b,
                    0f
                );
            }

            // Start particle system modifications
            if (mainParticleSystem != null && hasStoredInitialValues)
            {
                StartCoroutine(ModifyParticleSpeed());
            }
            else if (mainParticleSystem == null)
            {
                Debug.LogWarning("Main particle system not found, cannot modify speed");
            }

            // Enable secondary particle system
            if (secondaryParticleSystem != null)
            {
                secondaryParticleSystem.gameObject.SetActive(true);
                secondaryParticleSystem.Play();
                Debug.Log("Secondary particle system enabled");
            }
            else
            {
                Debug.LogWarning("Secondary particle system not found");
            }

            playableDirector.Play();
            GetComponent<BoxCollider2D>().enabled = false;

            StartCoroutine(FadeInHealthBar());
            playableDirector.stopped += OnCutsceneFinished;
        }
    }

    private IEnumerator ModifyParticleSpeed()
    {
        if (mainParticleSystem == null || !hasStoredInitialValues) yield break;

        Debug.Log($"Starting particle speed modification from {initialMinSpeed}-{initialMaxSpeed} to {targetMinSpeed}-{targetMaxSpeed}");

        // Ensure particle system is playing
        if (!mainParticleSystem.isPlaying)
        {
            mainParticleSystem.Play();
        }

        float elapsed = 0f;

        while (elapsed < speedChangeDuration)
        {
            float t = elapsed / speedChangeDuration;
            float curveT = speedChangeCurve.Evaluate(t);

            // Interpolate between initial and target speeds
            float currentMinSpeed = Mathf.Lerp(initialMinSpeed, targetMinSpeed, curveT);
            float currentMaxSpeed = Mathf.Lerp(initialMaxSpeed, targetMaxSpeed, curveT);

            // Apply the new speed to the particle system
            var main = mainParticleSystem.main;
            var startSpeed = main.startSpeed;
            startSpeed.mode = ParticleSystemCurveMode.TwoConstants;
            startSpeed.constantMin = currentMinSpeed;
            startSpeed.constantMax = currentMaxSpeed;
            main.startSpeed = startSpeed;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure final values are set exactly
        var finalMain = mainParticleSystem.main;
        var finalStartSpeed = finalMain.startSpeed;
        finalStartSpeed.mode = ParticleSystemCurveMode.TwoConstants;
        finalStartSpeed.constantMin = targetMinSpeed;
        finalStartSpeed.constantMax = targetMaxSpeed;
        finalMain.startSpeed = finalStartSpeed;

        Debug.Log($"Particle speed set to {targetMinSpeed}-{targetMaxSpeed}");
    }

    private IEnumerator FadeInHealthBar()
    {
        if (healthBarFill == null && healthBarOutline == null && extraImage == null)
            yield break;

        yield return new WaitForSeconds(fadeDelay);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);

            // Fade in fill
            if (healthBarFill != null)
            {
                healthBarFill.color = new Color(
                    initialFillColor.r,
                    initialFillColor.g,
                    initialFillColor.b,
                    alpha * initialFillColor.a
                );
            }

            // Fade in outline image
            if (healthBarOutline != null)
            {
                healthBarOutline.color = new Color(
                    initialOutlineColor.r,
                    initialOutlineColor.g,
                    initialOutlineColor.b,
                    alpha * initialOutlineColor.a
                );
            }

            // Fade in extra image
            if (extraImage != null)
            {
                extraImage.color = new Color(
                    initialExtraImageColor.r,
                    initialExtraImageColor.g,
                    initialExtraImageColor.b,
                    alpha * initialExtraImageColor.a
                );
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure full visibility
        if (healthBarFill != null)
        {
            healthBarFill.color = initialFillColor;
        }

        if (healthBarOutline != null)
        {
            healthBarOutline.color = initialOutlineColor;
        }

        if (extraImage != null)
        {
            extraImage.color = initialExtraImageColor;
        }
    }

    // CHANGED: Make this public so it can be called from other scripts
    public IEnumerator FadeOutHealthBar()
    {
        if (healthBarFill == null && healthBarOutline == null && extraImage == null)
            yield break;

        float elapsed = 0f;
        Color currentFillColor = healthBarFill != null ? healthBarFill.color : Color.clear;
        Color currentOutlineColor = healthBarOutline != null ? healthBarOutline.color : Color.clear;
        Color currentExtraColor = extraImage != null ? extraImage.color : Color.clear;

        while (elapsed < fadeDuration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);

            if (healthBarFill != null)
            {
                healthBarFill.color = new Color(
                    currentFillColor.r,
                    currentFillColor.g,
                    currentFillColor.b,
                    alpha * currentFillColor.a
                );
            }

            if (healthBarOutline != null)
            {
                healthBarOutline.color = new Color(
                    currentOutlineColor.r,
                    currentOutlineColor.g,
                    currentOutlineColor.b,
                    alpha * currentOutlineColor.a
                );
            }

            if (extraImage != null)
            {
                extraImage.color = new Color(
                    currentExtraColor.r,
                    currentExtraColor.g,
                    currentExtraColor.b,
                    alpha * currentExtraColor.a
                );
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Disable objects after fading out
        if (healthBarFill != null) healthBarFill.gameObject.SetActive(false);
        if (healthBarOutline != null) healthBarOutline.gameObject.SetActive(false);
        if (extraImage != null) extraImage.gameObject.SetActive(false);

        Debug.Log("Health bar faded out");
    }

    // NEW: Public method to fade out audio
    public IEnumerator FadeOutAudio(float fadeDuration = 1.0f)
    {
        if (borderAudioSource != null && borderAudioSource.enabled)
        {
            float startVolume = borderAudioSource.volume;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                borderAudioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            borderAudioSource.volume = 0f;
            borderAudioSource.Stop();
            borderAudioSource.enabled = false;
            Debug.Log("Border audio faded out and disabled");
        }
    }

    // NEW: Method to reset particle systems
    public IEnumerator ResetParticleSystems(float resetDuration = 2.0f)
    {
        if (mainParticleSystem != null && hasStoredInitialValues)
        {
            Debug.Log($"Resetting particle speed to {initialMinSpeed}-{initialMaxSpeed}");

            float elapsed = 0f;

            while (elapsed < resetDuration)
            {
                float t = elapsed / resetDuration;

                // Interpolate back to initial values
                float currentMinSpeed = Mathf.Lerp(targetMinSpeed, initialMinSpeed, t);
                float currentMaxSpeed = Mathf.Lerp(targetMaxSpeed, initialMaxSpeed, t);

                // Apply the reset speed to the particle system
                var main = mainParticleSystem.main;
                var startSpeed = main.startSpeed;
                startSpeed.mode = ParticleSystemCurveMode.TwoConstants;
                startSpeed.constantMin = currentMinSpeed;
                startSpeed.constantMax = currentMaxSpeed;
                main.startSpeed = startSpeed;

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Ensure final values are set exactly
            var finalMain = mainParticleSystem.main;
            var finalStartSpeed = finalMain.startSpeed;
            finalStartSpeed.mode = ParticleSystemCurveMode.TwoConstants;
            finalStartSpeed.constantMin = initialMinSpeed;
            finalStartSpeed.constantMax = initialMaxSpeed;
            finalMain.startSpeed = finalStartSpeed;
        }

        // Disable secondary particle system
        if (secondaryParticleSystem != null && secondaryParticleSystem.gameObject.activeSelf)
        {
            secondaryParticleSystem.Stop();
            secondaryParticleSystem.gameObject.SetActive(false);
            Debug.Log("Secondary particle system disabled");
        }
    }

    // NEW: Public method to start the fade out coroutine from other scripts
    public void StartFadeOutHealthBar()
    {
        StartCoroutine(FadeOutHealthBar());
    }

    // NEW: Public method to start audio fade out
    public void StartFadeOutAudio(float fadeDuration = 1.0f)
    {
        StartCoroutine(FadeOutAudio(fadeDuration));
    }

    // NEW: Public method to start particle system reset
    public void StartResetParticleSystems(float resetDuration = 2.0f)
    {
        StartCoroutine(ResetParticleSystems(resetDuration));
    }

    // NEW: Public method to set particle speed directly
    public void SetParticleSpeed(float minSpeed, float maxSpeed)
    {
        if (mainParticleSystem != null)
        {
            var main = mainParticleSystem.main;
            var startSpeed = main.startSpeed;
            startSpeed.mode = ParticleSystemCurveMode.TwoConstants;
            startSpeed.constantMin = minSpeed;
            startSpeed.constantMax = maxSpeed;
            main.startSpeed = startSpeed;
        }
    }

    // NEW: Public method to enable/disable secondary particle system
    public void SetSecondaryParticleSystemActive(bool active)
    {
        if (secondaryParticleSystem != null)
        {
            if (active && !secondaryParticleSystem.gameObject.activeSelf)
            {
                secondaryParticleSystem.gameObject.SetActive(true);
                secondaryParticleSystem.Play();
            }
            else if (!active && secondaryParticleSystem.gameObject.activeSelf)
            {
                secondaryParticleSystem.Stop();
                secondaryParticleSystem.gameObject.SetActive(false);
            }
        }
    }

    // NEW: Method to refresh particle system references
    public void RefreshParticleSystemReferences()
    {
        InitializeParticleSystemsByTag();
    }

    // NEW: Method to set particle system tags at runtime
    public void SetParticleSystemTags(string mainTag, string secondaryTag)
    {
        mainParticleTag = mainTag;
        secondaryParticleTag = secondaryTag;
        RefreshParticleSystemReferences();
    }

    private void OnCutsceneFinished(PlayableDirector director)
    {
        playableDirector.stopped -= OnCutsceneFinished;

        // Disable border after cutscene if configured to do so
        if (disableBorderAfterCutscene && borderObject != null)
        {
            borderObject.SetActive(false);
            Debug.Log("Border disabled after cutscene");
        }

        if (bossStateManager != null)
        {
            bossStateManager.SwitchState(bossStateManager.Phase1State);
        }
    }

    // Optional: Public method to manually disable border if needed
    public void DisableBorder()
    {
        if (borderObject != null && borderObject.activeSelf)
        {
            borderObject.SetActive(false);
        }
    }

    // Optional: Public method to manually enable border if needed
    public void EnableBorder()
    {
        if (borderObject != null && !borderObject.activeSelf)
        {
            borderObject.SetActive(true);

            // Enable and play border audio when enabling border
            if (borderAudioSource != null)
            {
                borderAudioSource.enabled = true;
                if (!borderAudioSource.isPlaying)
                {
                    borderAudioSource.Play();
                    Debug.Log("Playing border audio");
                }
            }
        }
    }

#if UNITY_EDITOR
    // Editor helper to test particle speed changes
    [ContextMenu("Test Particle Speed Change")]
    private void TestParticleSpeedChange()
    {
        if (mainParticleSystem != null)
        {
            StartCoroutine(ModifyParticleSpeed());
        }
        else
        {
            Debug.LogWarning("Main particle system not found. Make sure tags are set correctly.");
        }
    }

    [ContextMenu("Reset Particle Speed")]
    private void ResetParticleSpeed()
    {
        if (mainParticleSystem != null && hasStoredInitialValues)
        {
            var main = mainParticleSystem.main;
            var startSpeed = main.startSpeed;
            startSpeed.mode = ParticleSystemCurveMode.TwoConstants;
            startSpeed.constantMin = initialMinSpeed;
            startSpeed.constantMax = initialMaxSpeed;
            main.startSpeed = startSpeed;
            Debug.Log($"Reset particle speed to {initialMinSpeed}-{initialMaxSpeed}");
        }
    }

    [ContextMenu("Find Particle Systems by Tag")]
    private void FindParticleSystemsByTag()
    {
        RefreshParticleSystemReferences();
    }
#endif
}