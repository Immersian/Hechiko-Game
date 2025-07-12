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

    // Public properties for other scripts to check
    public bool IsParryingRight { get; private set; }
    public bool IsParryingLeft { get; private set; }
    public bool IsParryActive => parryTimer > 0;
    public bool IsBlocking => isHoldingBlock;

    // References
    private PlayerController playerController;
    private Animator animator;

    // Timing variables
    private float parryTimer;
    private float lastParryTime;
    private bool isHoldingBlock;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponentInParent<PlayerController>();
    }

    private void Update()
    {
        HandleParryInput();
        UpdateTimers();
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
        }
    }

    private void EndBlock()
    {
        isHoldingBlock = false;
        animator.SetBool(blockBool, false);
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

    public void Parried()
    {
        if (cameraShake != null)
        {
            cameraShake.ShakeCamera(shakeIntensity, shakeTime);
        }

        // Add a charge when successfully parrying
        if (parryChargeSystem != null)
        {
            parryChargeSystem.AddCharge();
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