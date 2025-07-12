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
    [SerializeField] private KeyCode specialAttackKey = KeyCode.R;
    [SerializeField] private float specialAttackCooldown = 2f;

    private Coroutine shockWaveCoroutine;
    private Material shockWaveMaterial;
    private static int waveDistanceFromCenter;
    private bool isShockwaveActive;
    private float lastSpecialAttackTime;
    private bool canSpecialAttack = true;
    private Animator playerAnimator;

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

        waveDistanceFromCenter = Shader.PropertyToID("_Wave_Distance_From_Centre");
        shockWaveMaterial = GetComponent<SpriteRenderer>().material;

        // Find player animator automatically
        playerAnimator = GameObject.FindGameObjectWithTag("Player Attack")?.GetComponent<Animator>();
        if (playerAnimator == null)
        {
            Debug.LogWarning("Player Animator not found! Make sure player has 'Player' tag.");
        }
    }

    private void Update()
    {

        // Existing test input
        if (InputManager.instance.inputControl.Gameplay.ShockwaveTest.WasPressedThisFrame())
        {
            TriggerSpecialAttack();
        }
    }

    private bool CanPerformSpecialAttack()
    {
        // Check if player is grounded through the animator (assuming you have a "IsGrounded" parameter)
        bool isGrounded = playerAnimator != null && playerAnimator.GetBool("IsGrounded");
        return canSpecialAttack && isGrounded && !isShockwaveActive;
    }

    private void TriggerSpecialAttack()
    {
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger("SpecialAttack");
            canSpecialAttack = false;
            lastSpecialAttackTime = Time.time;
            StartCoroutine(ResetSpecialAttackCooldown());
        }
    }

    private IEnumerator ResetSpecialAttackCooldown()
    {
        yield return new WaitForSeconds(specialAttackCooldown);
        canSpecialAttack = true;
    }

    // Call this method from the animation event
    public void CallShockwave(bool fromAnimation = false)
    {
        if (shockWaveCoroutine != null)
        {
            StopCoroutine(shockWaveCoroutine);
        }

        shockWaveCoroutine = StartCoroutine(ShockWaveAction());
        onShockWave?.Invoke();

        // If called from animation, don't trigger the special attack cooldown
        if (!fromAnimation)
        {
            canSpecialAttack = false;
            lastSpecialAttackTime = Time.time;
            StartCoroutine(ResetSpecialAttackCooldown());
        }
    }

    private IEnumerator ShockWaveAction()
    {
        isShockwaveActive = true;
        shockWaveMaterial.SetFloat(waveDistanceFromCenter, startPosition);

        float elapsedTime = 0f;
        while (elapsedTime < shockWaveTime)
        {
            elapsedTime += Time.deltaTime;
            float lerpedAmount = Mathf.Lerp(startPosition, endPosition, elapsedTime / shockWaveTime);
            shockWaveMaterial.SetFloat(waveDistanceFromCenter, lerpedAmount);
            yield return null;
        }

        shockWaveMaterial.SetFloat(waveDistanceFromCenter, endPosition);
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