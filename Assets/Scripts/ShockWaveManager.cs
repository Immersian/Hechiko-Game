using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class ShockWaveManager : MonoBehaviour
{
    public static ShockWaveManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float shockWaveTime = 0.75f;
    [SerializeField] private float startPosition = -0.1f;
    [SerializeField] private float endPosition = 1f;
    [SerializeField] private float specialAttackCooldown = 2f;
    [SerializeField] private float startSize = 0.05f;
    [SerializeField] private float endSize = 0f;
    [SerializeField] private float maxColliderRadius = 5.56f;

    [Header("Shockwave Position")]
    [SerializeField] private Transform specialAttackTarget; // Assign your empty object here
    [SerializeField] private Transform parryTarget; // Add this for parry position

    private CircleCollider2D shockWaveCollider;
    private Coroutine shockWaveCoroutine;
    private Material shockWaveMaterial;
    private static int waveDistanceFromCenter;
    private static int waveSize;
    private bool isShockwaveActive;
    private float lastSpecialAttackTime;
    private bool canSpecialAttack = true;
    private Animator playerAnimator;
    private Vector3 originalPosition; // Store original position

    public event System.Action onShockWave;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Get components first
        shockWaveCollider = GetComponent<CircleCollider2D>();
        shockWaveMaterial = GetComponent<SpriteRenderer>().material;

        // Store original position
        originalPosition = transform.position;

        // DISABLE COLLIDER IMMEDIATELY
        if (shockWaveCollider != null)
        {
            shockWaveCollider.enabled = false;
            shockWaveCollider.radius = 0f;
        }

        waveDistanceFromCenter = Shader.PropertyToID("_Wave_Distance_From_Centre");
        waveSize = Shader.PropertyToID("_Size");

        // Find player animator automatically
        playerAnimator = GameObject.FindGameObjectWithTag("Player Attack")?.GetComponent<Animator>();
        if (playerAnimator == null)
        {
            Debug.LogWarning("Player Animator not found! Make sure player has 'Player' tag.");
        }
    }

    private void Start()
    {
        // Double-check that collider is disabled at start
        if (shockWaveCollider != null && shockWaveCollider.enabled)
        {
            Debug.LogWarning("Shockwave collider was still enabled in Start(), disabling now.");
            shockWaveCollider.enabled = false;
            shockWaveCollider.radius = 0f;
        }
    }

    private void Update()
    {
        // Existing test input
        if (InputManager.instance.inputControl.Gameplay.ShockwaveTest.WasPressedThisFrame())
        {
            TrySpecialAttack();
        }
    }

    public void TrySpecialAttack()
    {
        ParryChargeSystem parryChargeSystem = FindObjectOfType<ParryChargeSystem>();

        if (parryChargeSystem != null && parryChargeSystem.HasFullCharge())
        {
            // We have charges, trigger the animation which will call CallShockwave
            if (playerAnimator != null)
            {
                playerAnimator.SetTrigger("SpecialAttack");
                canSpecialAttack = false;
                lastSpecialAttackTime = Time.time;
                StartCoroutine(ResetSpecialAttackCooldown());
            }
        }
        else
        {
            // Provide feedback that attack can't be used
            Debug.Log("Special attack not available - need full charges!");
            // You could play a sound, show UI feedback, etc.
        }
    }

    private bool CanPerformSpecialAttack()
    {
        bool isGrounded = playerAnimator != null && playerAnimator.GetBool("IsGrounded");
        return canSpecialAttack && isGrounded && !isShockwaveActive;
    }

    private IEnumerator ResetSpecialAttackCooldown()
    {
        yield return new WaitForSeconds(specialAttackCooldown);
        canSpecialAttack = true;
    }

    // Call this method from the animation event
    public void CallShockwave(bool fromAnimation = false)
    {
        // Get reference to ParryChargeSystem
        ParryChargeSystem parryChargeSystem = FindObjectOfType<ParryChargeSystem>();

        // Check if we have enough charges
        if (parryChargeSystem == null || !parryChargeSystem.HasFullCharge())
        {
            Debug.Log("Not enough parry charges for shockwave!");

            if (fromAnimation && playerAnimator != null)
            {
                playerAnimator.SetTrigger("SpecialAttackFailed");
            }
            return;
        }

        if (shockWaveCoroutine != null)
        {
            StopCoroutine(shockWaveCoroutine);
        }

        // Reset parry charges
        parryChargeSystem.ResetAllCharges();

        shockWaveCoroutine = StartCoroutine(ShockWaveAction());
        onShockWave?.Invoke();

        if (!fromAnimation)
        {
            canSpecialAttack = false;
            lastSpecialAttackTime = Time.time;
            StartCoroutine(ResetSpecialAttackCooldown());
        }
    }

    // Modified to accept a position parameter
    public void CallSmallShockwave(Transform targetPosition = null)
    {
        if (shockWaveCoroutine != null)
        {
            StopCoroutine(shockWaveCoroutine);
        }
        shockWaveCoroutine = StartCoroutine(SmallShockwaveAction(targetPosition));
    }

    private IEnumerator SmallShockwaveAction(Transform targetPosition = null)
    {
        isShockwaveActive = true;

        // Store current position
        Vector3 previousPosition = transform.position;

        // Move to target position if provided
        if (targetPosition != null)
        {
            transform.position = targetPosition.position;
        }

        // Set initial values for a smaller effect
        float smallStartPos = 0f;
        float smallEndPos = 0.10f;
        float smallStartSize = 0.03f;
        float smallEndSize = 0f;
        float duration = 0.4f;

        shockWaveMaterial.SetFloat(waveDistanceFromCenter, smallStartPos);
        shockWaveMaterial.SetFloat(waveSize, smallStartSize);

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            float distance = Mathf.Lerp(smallStartPos, smallEndPos, progress);
            float size = Mathf.Lerp(smallStartSize, smallEndSize, progress);
            shockWaveMaterial.SetFloat(waveDistanceFromCenter, distance);
            shockWaveMaterial.SetFloat(waveSize, size);

            yield return null;
        }

        shockWaveMaterial.SetFloat(waveDistanceFromCenter, endPosition);
        shockWaveMaterial.SetFloat(waveSize, endSize);

        // Return to original position
        transform.position = previousPosition;

        isShockwaveActive = false;
    }

    private IEnumerator ShockWaveAction()
    {
        isShockwaveActive = true;

        // Store current position
        Vector3 previousPosition = transform.position;

        // Move to special attack target position if available
        if (specialAttackTarget != null)
        {
            transform.position = specialAttackTarget.position;
        }

        // Initialize visual properties
        shockWaveMaterial.SetFloat(waveDistanceFromCenter, startPosition);
        shockWaveMaterial.SetFloat(waveSize, startSize);

        // Initialize and enable collider
        if (shockWaveCollider != null)
        {
            shockWaveCollider.enabled = true;
            shockWaveCollider.radius = 0f;
        }

        float elapsedTime = 0f;
        while (elapsedTime < shockWaveTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / shockWaveTime;

            // Animate visual properties
            float distance = Mathf.Lerp(startPosition, endPosition, progress);
            float size = Mathf.Lerp(startSize, endSize, progress);
            shockWaveMaterial.SetFloat(waveDistanceFromCenter, distance);
            shockWaveMaterial.SetFloat(waveSize, size);

            // Animate collider radius
            if (shockWaveCollider != null)
            {
                shockWaveCollider.radius = Mathf.Lerp(0f, maxColliderRadius, progress);
            }

            yield return null;
        }

        // Ensure final values are set
        shockWaveMaterial.SetFloat(waveDistanceFromCenter, endPosition);
        shockWaveMaterial.SetFloat(waveSize, endSize);

        // Disable collider at the end
        if (shockWaveCollider != null)
        {
            shockWaveCollider.radius = maxColliderRadius;
            shockWaveCollider.enabled = false;
        }

        // Return to original position
        transform.position = previousPosition;

        isShockwaveActive = false;
    }

    private void OnDestroy()
    {
        if (shockWaveMaterial != null)
        {
            Destroy(shockWaveMaterial);
        }
    }

    public bool IsShockwaveActive() => isShockwaveActive;
}