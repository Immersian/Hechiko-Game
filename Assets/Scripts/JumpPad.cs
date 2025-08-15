using SupanthaPaul;
using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [Header("Bounce Settings")]
    [SerializeField] private float bounceForce = 20f;
    [SerializeField] private Animator animator;

    [Header("Camera Shake")]
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private float shakeIntensity = 2f;
    [SerializeField] private float shakeDuration = 0.2f;

    private static readonly int BounceTrigger = Animator.StringToHash("Bounce");

    [Header("Dash Cooldown")]
    [SerializeField] private float dashCooldownAfterBounce = 0.2f;

    private void Awake()
    {
        // Auto-get components if not set in inspector
        if (animator == null)
            animator = GetComponent<Animator>();

        if (cameraShake == null)
            cameraShake = FindObjectOfType<CameraShake>();
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();

            // Interrupt any active dash first
            if (player != null && player.isDashing)
            {
                player.InterruptDash();

                // Reset vertical velocity to ensure consistent bounce
                rb.velocity = new Vector2(rb.velocity.x, 0f);
            }

            // Apply bounce force
            rb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);

            // Trigger dash cooldown on player
            if (player != null)
            {
                player.ForceDashCooldown(dashCooldownAfterBounce);
            }

            // Rest of your existing code...
            if (animator != null)
            {
                animator.SetTrigger(BounceTrigger);
            }
            else
            {
                Debug.LogWarning("No Animator found on JumpPad!", this);
            }

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
}