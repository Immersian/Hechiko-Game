using SupanthaPaul;
using UnityEngine;

public class ParryScript : MonoBehaviour
{
    [Header("Animation Parameters")]
    public string parryTrigger = "Parry";
    public string blockBool = "isBlocking";

    [Header("Timing")]
    public float parryWindow = 0.3f;

    // Parry direction properties
    public bool IsParryingRight { get; private set; }
    public bool IsParryingLeft { get; private set; }

    public PlayerController playerController;
    private Animator animator;
    private float parryTimer;
    private bool isHoldingBlock;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponentInParent<PlayerController>();
    }

    private void Update()
    {
        if (InputManager.instance.inputControl.Gameplay.Parry.WasPressedThisFrame())
        {
            StartParry();
        }

        if (InputManager.instance.inputControl.Gameplay.Parry.IsPressed())
        {
            ContinueBlock();
        }
        else if (isHoldingBlock)
        {
            EndBlock();
        }

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
        IsParryingRight = playerController.m_facingRight;
        IsParryingLeft = !playerController.m_facingRight;
        animator.SetTrigger(parryTrigger);
        parryTimer = parryWindow;
        isHoldingBlock = true;
        animator.SetBool(blockBool, true);
    }

    private void ResetParryDirections()
    {
        IsParryingRight = false;
        IsParryingLeft = false;
    }

    public bool CanParryAttack(Vector2 attackDirection)
    {
        if (parryTimer <= 0) return false;

        Vector2 parryDirection = IsParryingRight ? Vector2.right : Vector2.left;
        float dot = Vector2.Dot(parryDirection, attackDirection.normalized);
        return dot > 0.5f;
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
}