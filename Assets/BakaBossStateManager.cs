using UnityEngine;
using UnityEngine.Playables;
using System.Collections;

public class BakaBossStateManager : MonoBehaviour
{
    // All possible states
    public CutsceneBakaState cutsceneState = new CutsceneBakaState();
    public Phase1BakaState Phase1State = new Phase1BakaState();
    public PhaseTransitionState phasecutsceneState = new PhaseTransitionState();
    public Phase2BakaState Phase2State = new Phase2BakaState();
    public StunBakaState stunState = new StunBakaState();
    public DeathCutsceneBakaState deathCutsceneState = new DeathCutsceneBakaState();
    public DeathIdleBakaState deathIdleState = new DeathIdleBakaState();

    [Header("Phase Transition")]
    public PlayableDirector phaseTransitionCutscene;

    [Header("Death Cutscene")]
    public PlayableDirector deathCutscene;
    public GameObject objectToEnableDuringCutscene;
    public GameObject objectToDeleteAfterCutscene;

    [Header("UI Elements")]
    public GameObject borderObject; // Reference to the border object
    public CutsceneTriggerBoss cutsceneTrigger; // Reference to the cutscene trigger for health bar fading

    [Header("Animation Event References")]
    public GameObject objectToDisable; // Reference to object that will be disabled via animation event

    [Header("Animation Parameters")]
    public string isDeadParameter = "IsDead";

    [Header("Audio")]
    public AudioSource borderAudioSource; // Audio to play when border is enabled
    public AudioSource animationAudioSource; // Audio source for animation events
    public AudioClip cameraShakeSound; // Sound to play when camera shakes

    [Header("Camera Shake")]
    public CameraShake cameraShakeComponent; // Reference to CameraShake component
    public float cameraShakeIntensity = 1.0f; // Default intensity for camera shake
    public float cameraShakeDuration = 0.5f; // Default duration for camera shake

    [SerializeField]
    public string currentStateName;

    public BakaBossBaseState currentState;
    public BakaBossBaseState CurrentState => currentState;

    private Animator bossAnimator;
    private bool audioFadeStarted = false;

    void Start()
    {
        bossAnimator = GetComponent<Animator>();
        currentState = cutsceneState;
        currentState.EnterState(this);
        currentStateName = currentState.GetType().Name;

        // Ensure audio is disabled initially
        if (borderAudioSource != null)
        {
            borderAudioSource.enabled = false;
        }

        // Find CameraShake component if not assigned
        if (cameraShakeComponent == null)
        {
            cameraShakeComponent = FindObjectOfType<CameraShake>();
        }
    }

    void Update()
    {
        currentState.UpdateState(this);

        // If we're in death idle state and audio hasn't been faded yet, fade it
        if (currentState is DeathIdleBakaState && !audioFadeStarted)
        {
            FadeOutAudioAtDeath(2f);
            audioFadeStarted = true;
        }
    }

    public void SwitchState(BakaBossBaseState state)
    {
        currentState.ExitState(this);
        currentState = state;
        state.EnterState(this);
        currentStateName = currentState.GetType().Name;

        // Reset audio fade flag when leaving death idle state
        if (!(state is DeathIdleBakaState))
        {
            audioFadeStarted = false;
        }
    }

    public void TriggerStun()
    {
        if (currentState is not StunBakaState)
        {
            SwitchState(stunState);
        }
    }

    public void TriggerDeathCutscene()
    {
        if (currentState is not DeathCutsceneBakaState && currentState is not DeathIdleBakaState)
        {
            SwitchState(deathCutsceneState);
        }
    }

    public void TriggerDeathIdle()
    {
        if (currentState is not DeathIdleBakaState)
        {
            SwitchState(deathIdleState);
        }
    }

    // Animation control methods
    public void SetIsDead(bool value)
    {
        if (bossAnimator != null)
        {
            bossAnimator.SetBool(isDeadParameter, value);

            // If setting IsDead to true and we're not already in death states, start audio fade
            if (value && !(currentState is DeathCutsceneBakaState) && !(currentState is DeathIdleBakaState))
            {
                // Start fade after a short delay to ensure state transition happens
                StartCoroutine(DelayedAudioFade());
            }
        }
    }

    private IEnumerator DelayedAudioFade()
    {
        yield return new WaitForSeconds(0.5f);
        FadeOutAudioAtDeath(2f);
    }

    public void ResetAllAnimatorParameters()
    {
        if (bossAnimator != null)
        {
            // Reset common parameters you might have
            bossAnimator.SetBool(isDeadParameter, false);
            // Add any other parameters you want to reset here
        }
    }

    // NEW: Method to disable the border
    public void DisableBorder()
    {
        if (borderObject != null)
        {
            borderObject.SetActive(false);
            Debug.Log("Border disabled after death cutscene");
        }
    }

    // NEW: Method to enable the border (if needed)
    public void EnableBorder()
    {
        if (borderObject != null)
        {
            borderObject.SetActive(true);
            Debug.Log("Border enabled");

            // Enable and play border audio when enabling border
            if (borderAudioSource != null)
            {
                borderAudioSource.enabled = true;
                borderAudioSource.volume = 1f; // Ensure volume is reset when enabling
                if (!borderAudioSource.isPlaying)
                {
                    borderAudioSource.Play();
                    Debug.Log("Playing border audio");
                }
            }
        }
    }

    // NEW: Method to fade out health bar at the end of death cutscene
    public void FadeOutHealthBarAtDeath()
    {
        if (cutsceneTrigger != null)
        {
            cutsceneTrigger.StartFadeOutHealthBar();
            Debug.Log("Starting health bar fade out for death cutscene");
        }
        else
        {
            Debug.LogWarning("CutsceneTriggerBoss reference not set in BakaBossStateManager");
        }
    }

    // NEW: Method to fade out audio at the end of death cutscene
    public void FadeOutAudioAtDeath(float fadeDuration = 1.0f)
    {
        Debug.Log($"FadeOutAudioAtDeath called with duration: {fadeDuration}");

        if (borderAudioSource != null && borderAudioSource.enabled)
        {
            StartCoroutine(FadeOutAudioDirectly(fadeDuration));
        }
        else if (cutsceneTrigger != null)
        {
            cutsceneTrigger.StartFadeOutAudio(fadeDuration);
            Debug.Log("Starting audio fade out via cutscene trigger");
        }
        else
        {
            Debug.LogWarning("No audio source available to fade out");
        }
    }

    // Fallback coroutine for fading audio directly
    private IEnumerator FadeOutAudioDirectly(float fadeDuration = 3.0f)
    {
        if (borderAudioSource != null && borderAudioSource.enabled && borderAudioSource.isPlaying)
        {
            float startVolume = borderAudioSource.volume;
            float elapsed = 0f;

            Debug.Log($"Starting audio fade out from volume {startVolume} over {fadeDuration} seconds");

            while (elapsed < fadeDuration && borderAudioSource != null)
            {
                float newVolume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
                borderAudioSource.volume = newVolume;
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (borderAudioSource != null)
            {
                borderAudioSource.volume = 0f;
                borderAudioSource.Stop();
                borderAudioSource.enabled = false;
                Debug.Log("Border audio faded out and disabled (direct method)");
            }
        }
        else
        {
            Debug.LogWarning("Border audio source is null, not enabled, or not playing - cannot fade out");
        }
    }

    // ANIMATION EVENT: Disable referenced object
    public void DisableReferencedObject()
    {
        if (objectToDisable != null)
        {
            objectToDisable.SetActive(false);
            Debug.Log($"Animation event: Disabled object - {objectToDisable.name}");
        }
        else
        {
            Debug.LogWarning("Animation event: objectToDisable is not assigned!");
        }
    }

    // NEW: ANIMATION EVENT: Play audio and shake camera
    public void PlayAudioWithCameraShake()
    {
        PlayAudioWithCameraShake(cameraShakeIntensity, cameraShakeDuration);
    }

    // Overloaded version with custom parameters
    public void PlayAudioWithCameraShake(float intensity, float duration)
    {
        // Play audio if available
        if (cameraShakeSound != null)
        {
            if (animationAudioSource != null)
            {
                animationAudioSource.PlayOneShot(cameraShakeSound);
                Debug.Log($"Playing camera shake sound: {cameraShakeSound.name}");
            }
            else if (borderAudioSource != null)
            {
                borderAudioSource.PlayOneShot(cameraShakeSound);
                Debug.Log($"Playing camera shake sound on border audio source: {cameraShakeSound.name}");
            }
            else
            {
                Debug.LogWarning("No audio source available to play camera shake sound!");
            }
        }

        // Trigger camera shake
        if (cameraShakeComponent != null)
        {
            cameraShakeComponent.ShakeCamera(intensity, duration);
            Debug.Log($"Camera shake triggered: Intensity={intensity}, Duration={duration}");
        }
        else
        {
            Debug.LogWarning("CameraShake component not found! Assign it in the inspector or ensure it exists in the scene.");
        }
    }

    // NEW: ANIMATION EVENT: Stop camera shake
    public void StopCameraShake()
    {
        if (cameraShakeComponent != null)
        {
            // Access the ResetIntensity method through reflection since it's private
            var resetMethod = typeof(CameraShake).GetMethod("ResetIntensity",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (resetMethod != null)
            {
                resetMethod.Invoke(cameraShakeComponent, null);
                Debug.Log("Camera shake stopped via animation event");
            }
            else
            {
                // Fallback: Try to find and access the Cinemachine component directly
                var cameraShakeGameObject = cameraShakeComponent.gameObject;
                var virtualCamera = cameraShakeGameObject.GetComponent<Cinemachine.CinemachineVirtualCamera>();

                if (virtualCamera != null)
                {
                    var perlinNoise = virtualCamera.GetCinemachineComponent<Cinemachine.CinemachineBasicMultiChannelPerlin>();
                    if (perlinNoise != null)
                    {
                        perlinNoise.m_AmplitudeGain = 0f;
                        Debug.Log("Camera shake stopped by resetting amplitude gain");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("CameraShake component not found! Cannot stop camera shake.");
        }
    }

    // NEW: ANIMATION EVENT: Fade out health bar and audio (call this near the end of death cutscene)
    public void OnDeathCutsceneEnd()
    {
        Debug.Log("OnDeathCutsceneEnd called - starting fade outs");
        FadeOutHealthBarAtDeath();
        DisableBorder();
        // Don't fade audio here anymore - let it happen in death idle state
    }

    // NEW: Animation event for when IsDead parameter is set
    public void OnIsDeadSet()
    {
        Debug.Log("OnIsDeadSet animation event called");
        // Start audio fade when death animation plays
        FadeOutAudioAtDeath(3f); // Longer fade for smoother transition
    }

    // Optional: Method to enable the referenced object if needed
    public void EnableReferencedObject()
    {
        if (objectToDisable != null)
        {
            objectToDisable.SetActive(true);
            Debug.Log($"Enabled object - {objectToDisable.name}");
        }
    }
}