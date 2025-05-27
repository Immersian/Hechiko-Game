using UnityEngine;

namespace SupanthaPaul
{
    public class PlayerAnimator : MonoBehaviour
    {
        private Rigidbody2D m_rb;
        private PlayerController m_controller;
        private PlayerHealth m_health;
        private Animator m_anim;
        private static readonly int Move = Animator.StringToHash("Move");
        private static readonly int JumpState = Animator.StringToHash("JumpState");
        private static readonly int IsJumping = Animator.StringToHash("IsJumping");
        private static readonly int WallGrabbing = Animator.StringToHash("WallGrabbing");
        private static readonly int IsDashing = Animator.StringToHash("IsDashing");
        private static readonly int IsHurt = Animator.StringToHash("IsHurt");

        private void Start()
        {
            m_anim = GetComponentInChildren<Animator>();
            m_controller = GetComponent<PlayerController>();
            m_rb = GetComponent<Rigidbody2D>();
            m_health = GetComponent<PlayerHealth>();

            // Subscribe to the damage event
            m_health.OnTakeDamage += TriggerHurtAnimation;
        }

        private void OnDestroy()
        {
            // Unsubscribe to prevent memory leaks
            if (m_health != null)
            {
                m_health.OnTakeDamage -= TriggerHurtAnimation;
            }
        }

        private void TriggerHurtAnimation(int damageAmount)
        {
            // Trigger the hurt animation
            m_anim.SetTrigger(IsHurt);
        }

        private void Update()
        {
            // Existing animation code remains the same...
            m_anim.SetFloat(Move, Mathf.Abs(m_rb.velocity.x));

            float verticalVelocity = m_rb.velocity.y;
            m_anim.SetFloat(JumpState, verticalVelocity);

            if (!m_controller.isGrounded && !m_controller.actuallyWallGrabbing)
            {
                m_anim.SetBool(IsJumping, true);
            }
            else
            {
                m_anim.SetBool(IsJumping, false);
            }

            if (!m_controller.isGrounded && m_controller.actuallyWallGrabbing)
            {
                m_anim.SetBool(WallGrabbing, true);
            }
            else
            {
                m_anim.SetBool(WallGrabbing, false);
            }

            m_anim.SetBool(IsDashing, m_controller.isDashing);
        }
    }
}