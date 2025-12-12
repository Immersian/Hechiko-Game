using SupanthaPaul;
using UnityEngine;

public class ParryScript : MonoBehaviour
{
    [Header("Animation Parameters")]
    public string parryTrigger = "Parry";
    public string blockBool = "isBlocking";

    [Header("Timing")]
    public float parryWindow = 0.3f;
    public float parryCooldown = 0.5f;

    [Header("Direction Settings")]
    [Tooltip("If true, requires matching facing direction for parry")]
    public bool directionalParry = true;

    [Header("Charge System")]
    [SerializeField] private ParryChargeSystem parryChargeSystem;

    [Header("Camera Shake Parameters")]
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private float shakeIntensity = 5;
    [SerializeField] private float shakeTime = 1;

    [Header("Hitstop Settings")]
    [SerializeField] private float hitstopDuration = 0.1f;
    [SerializeField] private float hitstopTimeScale = 0.1f;

    [Header("Rumble Settings")]
    [SerializeField] private float parryRumbleLowFrequency = 0.5f;
    [SerializeField] private float parryRumbleHighFrequency = 0.5f;
    [SerializeField] private float parryRumbleDuration = 0.15f;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip parrySound;
    [SerializeField] private AudioClip blockSound;
    [SerializeField] private float parrySoundVolume = 0.8f;
    [SerializeField] private float blockSoundVolume = 0.6f;

    [SerializeField] private Transform parryTarget;

    // Public properties for other scripts to check
    public bool IsParryingRight { get; private set; }
    public bool IsParryingLeft { get; private set; }
    public bool IsParryActive => parryTimer > 0;
    public bool IsBlocking => isHoldingBlock;

    // References
    private PlayerController playerController;
    private Animator animator;
    private AudioSource audioSource;

    // Timing variables
    private float parryTimer;
    private float lastParryTime;
    private bool isHoldingBlock;
    private bool isInHitstop;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponentInParent<PlayerController>();
        audioSource = GetComponent<AudioSource>();

        // Ensure we have an AudioSource
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; // Make it 2D
            audioSource.playOnAwake = false;
        }
    }

    private void Update()
    {
        HandleParryInput();
        UpdateTimers();
    }

    private void StartBlockMovementRestrictions()
    {
        if (playerController != null)
        {
            playerController.canMove = false;
        }
    }

    private void EndBlockMovementRestrictions()
    {
        if (playerController != null)
        {
            playerController.canMove = true;
        }
    }

    private void HandleParryInput()
    {
        // Start parry on button press
        if (InputManager.instance.inputControl.Gameplay.Parry.WasPressedThisFrame() &&
            Time.time >= lastParryTime + parryCooldown)
        {
            StartParry();
        }

        // Continue/end block hold
        if (InputManager.instance.inputControl.Gameplay.Parry.IsPressed())
        {
            ContinueBlock();
        }
        else if (isHoldingBlock)
        {
            EndBlock();
        }
    }

    private void UpdateTimers()
    {
        if (parryTimer > 0)
        {
            parryTimer -= Time.deltaTime;
            if (parryTimer <= 0)
            {
                animator.ResetTrigger(parryTrigger);
                ResetParryDirections();
            }
        }
    }

    private void StartParry()
    {
        // Set parry direction based on player facing
        IsParryingRight = playerController.m_facingRight;
        IsParryingLeft = !playerController.m_facingRight;

        // Trigger animations and timers
        animator.SetTrigger(parryTrigger);
        parryTimer = parryWindow;
        isHoldingBlock = true;
        animator.SetBool(blockBool, true);
        lastParryTime = Time.time;

        // Apply movement restrictions
        StartBlockMovementRestrictions();

        // Play block sound
        PlaySound(blockSound, blockSoundVolume);
    }

    private void ResetParryDirections()
    {
        IsParryingRight = false;
        IsParryingLeft = false;
    }

    private void ContinueBlock()
    {
        if (!isHoldingBlock)
        {
            isHoldingBlock = true;
            animator.SetBool(blockBool, true);
            StartBlockMovementRestrictions(); // Add this to handle late block starts
        }
    }

    private void EndBlock()
    {
        isHoldingBlock = false;
        animator.SetBool(blockBool, false);
        EndBlockMovementRestrictions(); // Add this to restore movement
    }

    public void ForceEndBlock()
    {
        // Simply call EndBlock to handle everything cleanly
        if (isHoldingBlock)
        {
            EndBlock();

            // Also reset the parry timer if active
            if (parryTimer > 0)
            {
                parryTimer = 0;
                animator.ResetTrigger(parryTrigger);
                ResetParryDirections();
            }
        }
    }

    /// <summary>
    /// Checks if this parry can block an attack from a specific enemy
    /// </summary>
    public bool CanParryAttack(EnemyMovement enemy)
    {
        if (!IsParryActive) return false;
        if (enemy == null) return false;

        // If not using directional parry, any active parry counts
        if (!directionalParry) return true;

        // Directional parry check (matches DamageObject logic)
        return (IsParryingRight && enemy.EnemyFacingLeft) ||
               (IsParryingLeft && enemy.EnemyFacingRight);
    }

    // In the ParryScript class, modify the Parried() method:
    public void Parried()
    {
        // Apply hitstop effect
        StartCoroutine(ApplyHitstop());

        // Play parry sound
        PlaySound(parrySound, parrySoundVolume);

        if (cameraShake != null)
        {
            cameraShake.ShakeCamera(shakeIntensity, shakeTime);
        }

        // Add rumble feedback for successful parry
        TriggerParryRumble();

        // Add a charge when successfully parrying
        if (parryChargeSystem != null)
        {
            parryChargeSystem.AddCharge();
        }

        // Trigger small shockwave effect at parry position
        if (ShockWaveManager.Instance != null)
        {
            // Pass the parry target transform to position the shockwave
            ShockWaveManager.Instance.CallSmallShockwave(parryTarget);
        }
    }

    private void TriggerParryRumble()
    {
        // Check if RumbleManager exists and call rumble
        if (RumbleManager.instance != null)
        {
            RumbleManager.instance.RumblePulse(
                parryRumbleLowFrequency,
                parryRumbleHighFrequency,
                parryRumbleDuration
            );
        }
    }

    private System.Collections.IEnumerator ApplyHitstop()
    {
        if (isInHitstop) yield break;

        isInHitstop = true;

        // Freeze time
        Time.timeScale = hitstopTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // Wait for the hitstop duration (scaled by time scale)
        yield return new WaitForSecondsRealtime(hitstopDuration);

        // Restore normal time
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        isInHitstop = false;
    }

    private void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    // Visual debug
    private void OnDrawGizmos()
    {
        if (IsParryingRight)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, Vector2.right);
        }
        if (IsParryingLeft)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, Vector2.left);
        }
    }
}