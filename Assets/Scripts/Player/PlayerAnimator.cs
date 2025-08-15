using UnityEngine;

namespace SupanthaPaul
{
    public class PlayerAnimator : MonoBehaviour
    {
        private Rigidbody2D m_rb;
        private PlayerController m_controller;
        private PlayerHealth m_health;
        private Animator m_anim;

        // Animation hashes
        private static readonly int Move = Animator.StringToHash("Move");
        private static readonly int JumpState = Animator.StringToHash("JumpState");
        private static readonly int IsJumping = Animator.StringToHash("IsJumping");
        private static readonly int WallGrabbing = Animator.StringToHash("WallGrabbing");
        private static readonly int IsDashing = Animator.StringToHash("IsDashing");
        private static readonly int IsHurt = Animator.StringToHash("IsHurt");
        private static readonly int HealTrigger = Animator.StringToHash("Heal"); // Changed to trigger

        private void Start()
        {
            m_anim = GetComponentInChildren<Animator>();
            m_controller = GetComponent<PlayerController>();
            m_rb = GetComponent<Rigidbody2D>();
            m_health = GetComponent<PlayerHealth>();

            // Subscribe to healing events
            m_controller.OnHealStart += TriggerHealAnimation;
            m_controller.OnHealComplete += ResetHealTrigger;
        }

        private void OnDestroy()
        {
            if (m_controller != null)
            {
                m_controller.OnHealStart -= TriggerHealAnimation;
                m_controller.OnHealComplete -= ResetHealTrigger;
            }
        }

        private void TriggerHealAnimation()
        {
            if (m_anim == null)
            {
                Debug.LogError("Animator reference is null!");
                return;
            }

            if (!HasParameter("Heal", m_anim))
            {
                Debug.LogError("Animator is missing 'Heal' trigger parameter!");
                return;
            }

            m_anim.SetTrigger(HealTrigger);
            Debug.Log("Heal trigger activated");
        }

        private void ResetHealTrigger()
        {
            if (m_anim != null && HasParameter("Heal", m_anim))
            {
                m_anim.ResetTrigger(HealTrigger);
            }
        }

        // Helper method to check if parameter exists
        private bool HasParameter(string paramName, Animator animator)
        {
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == paramName) return true;
            }
            return false;
        }

        private void Update()
        {
            // Existing animation updates
            m_anim.SetFloat(Move, Mathf.Abs(m_rb.velocity.x));
            m_anim.SetFloat(JumpState, m_rb.velocity.y);

            m_anim.SetBool(IsJumping, !m_controller.isGrounded && !m_controller.actuallyWallGrabbing);
            m_anim.SetBool(WallGrabbing, !m_controller.isGrounded && m_controller.actuallyWallGrabbing);
            m_anim.SetBool(IsDashing, m_controller.isDashing);
        }
    }
}