using SupanthaPaul;
using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [Header("Bounce Settings")]
    [SerializeField] private float bounceForce = 20f;
    [SerializeField] private Animator animator;

    [Header("Sound Settings")]
    [SerializeField] private AudioClip bounceSound;
    [SerializeField] private float bounceSoundVolume = 0.8f;
    [SerializeField] private AudioSource audioSource;

    [Header("Pitch Shift Settings")]
    [SerializeField] private bool enablePitchShift = true;
    [SerializeField] private float minPitch = 0.8f;
    [SerializeField] private float maxPitch = 1.2f;
    [SerializeField] private float pitchChangeSpeed = 5f;
    [SerializeField] private bool randomizePitch = true;

    [Header("Camera Shake")]
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private float shakeIntensity = 2f;
    [SerializeField] private float shakeDuration = 0.2f;

    [Header("Dash Cooldown")]
    [SerializeField] private float dashCooldownAfterBounce = 0.2f;

    private static readonly int BounceTrigger = Animator.StringToHash("Bounce");
    private float currentPitch = 1f;
    private float targetPitch = 1f;

    private void Awake()
    {
        // Auto-get components if not set in inspector
        if (animator == null)
            animator = GetComponent<Animator>();

        if (cameraShake == null)
            cameraShake = FindObjectOfType<CameraShake>();

        // Try to get AudioSource if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            // Create AudioSource if not found
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D sound
            }
        }

        // Initialize pitch
        currentPitch = 1f;
        targetPitch = 1f;
        ApplyCurrentPitch();
    }

    private void Update()
    {
        // Smoothly interpolate pitch if needed
        if (enablePitchShift && Mathf.Abs(currentPitch - targetPitch) > 0.01f)
        {
            currentPitch = Mathf.Lerp(currentPitch, targetPitch, pitchChangeSpeed * Time.deltaTime);
            ApplyCurrentPitch();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();

            if (rb == null)
            {
                Debug.LogWarning("No Rigidbody2D found on player!", this);
                return;
            }

            // Interrupt any active dash first
            if (player != null && player.isDashing)
            {
                player.InterruptDash();

                // Reset vertical velocity to ensure consistent bounce
                rb.velocity = new Vector2(rb.velocity.x, 0f);
            }

            // Play bounce sound with optional pitch shift
            PlayBounceSound();

            // Apply bounce force
            rb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);

            // Trigger dash cooldown on player
            if (player != null)
            {
                player.ForceDashCooldown(dashCooldownAfterBounce);
            }

            // Trigger animation
            if (animator != null)
            {
                animator.SetTrigger(BounceTrigger);
            }
            else
            {
                Debug.LogWarning("No Animator found on JumpPad!", this);
            }

            // Trigger camera shake
            if (cameraShake != null)
            {
                cameraShake.ShakeCamera(shakeIntensity, shakeDuration);
            }
            else
            {
                Debug.LogWarning("No CameraShake reference found!", this);
            }
        }
    }

    private void PlayBounceSound()
    {
        if (bounceSound != null && audioSource != null)
        {
            // Apply pitch shift if enabled
            if (enablePitchShift)
            {
                if (randomizePitch)
                {
                    // Random pitch between min and max
                    targetPitch = Random.Range(minPitch, maxPitch);
                }
                else
                {
                    // Oscillating pitch (alternates between min and max)
                    targetPitch = Mathf.PingPong(Time.time, maxPitch - minPitch) + minPitch;
                }

                // Apply pitch immediately for this sound
                currentPitch = targetPitch;
                ApplyCurrentPitch();
            }

            audioSource.PlayOneShot(bounceSound, bounceSoundVolume);
        }
        else if (bounceSound == null)
        {
            Debug.LogWarning("No bounce sound assigned to JumpPad!", this);
        }
        else if (audioSource == null)
        {
            Debug.LogWarning("No AudioSource found on JumpPad!", this);
        }
    }

    private void ApplyCurrentPitch()
    {
        if (audioSource != null)
        {
            audioSource.pitch = Mathf.Clamp(currentPitch, 0.01f, 3f); // Clamp to reasonable values
        }
    }

    // Set a specific pitch (can be called from animation events)
    public void SetPitch(float pitch)
    {
        if (enablePitchShift)
        {
            targetPitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
    }

    // Set random pitch within range
    public void SetRandomPitch()
    {
        if (enablePitchShift)
        {
            targetPitch = Random.Range(minPitch, maxPitch);
        }
    }

    // Optional: Animation Event method for precise sound timing
    public void PlayBounceSoundEvent()
    {
        PlayBounceSound();
    }

    // Optional: Animation Event method with specific pitch
    public void PlayBounceSoundWithPitch(float pitch)
    {
        if (enablePitchShift)
        {
            targetPitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            currentPitch = targetPitch;
            ApplyCurrentPitch();
        }

        PlayBounceSound();
    }

    // Optional: Public method to test sound
    public void TestBounceSound()
    {
        PlayBounceSound();
    }

    // Optional: Public method to change bounce sound at runtime
    public void SetBounceSound(AudioClip newSound, float newVolume = 0.8f)
    {
        bounceSound = newSound;
        bounceSoundVolume = newVolume;
    }

    // Optional: Public method to configure pitch settings at runtime
    public void ConfigurePitchSettings(bool enable, float newMinPitch = 0.8f, float newMaxPitch = 1.2f, bool useRandom = true)
    {
        enablePitchShift = enable;
        minPitch = newMinPitch;
        maxPitch = newMaxPitch;
        randomizePitch = useRandom;

        // Clamp current pitch to new range
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
        ApplyCurrentPitch();
    }

    // Optional: Reset pitch to default
    public void ResetPitch()
    {
        currentPitch = 1f;
        targetPitch = 1f;
        ApplyCurrentPitch();
    }
}