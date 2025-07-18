﻿using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace SupanthaPaul
{
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float speed;
        [Header("Jumping")]
        [SerializeField] private float jumpForce;
        [SerializeField] private float fallMultiplier;
        [SerializeField] public Transform groundCheck;
        [SerializeField] private float groundCheckRadius;
        [SerializeField] private LayerMask whatIsGround;
        [SerializeField] private int extraJumpCount = 1;
        [SerializeField] private GameObject jumpEffect;

        [Header("Celeste-Style Dash")]
        [SerializeField] private float dashSpeed = 30f;
        [SerializeField] private float horizontalDashDuration = 0.15f; // Duration for horizontal dashes
        [SerializeField] private float upwardDashDuration = 0.12f;    // Shorter duration for upward dashes
        [SerializeField] private float downwardDashDuration = 0.1f;  // Shortest duration for downward dashes
        [SerializeField] private float diagonalDashDuration = 0.13f; // Duration for diagonal dashes
        [SerializeField] private float dashCooldown = 0.4f;
        [SerializeField] private float dashEndSpeedMultiplier = 0.85f;
        [SerializeField] private GameObject dashEffect;
        [SerializeField] private float dashBufferTime = 0.1f;

        [Header("Stamina Settings")]
        public float maxStamina = 100f;
        public float currentStamina;
        public float dashCost = 30f;
        public float staminaRegenRate = 15f;
        public float staminaRegenDelay = 1f;
        private float lastDashTime;

        [Header("Stamina Bar UI")]
        public RectTransform staminaBar1; // Primary stamina bar
        private float staminaBarFullWidth;

        [Header("Attack References")]
        [SerializeField] private PlayerAttack playerAttack;

        [HideInInspector] public bool isGrounded;
        [HideInInspector] public float moveInput;
        [HideInInspector] public bool canMove = true;
        [HideInInspector] public bool canFlip = true;
        [HideInInspector] public bool canJump = true;
        [HideInInspector] public bool canDash = true;
        [HideInInspector] public bool isDashing = false;
        [HideInInspector] public bool actuallyWallGrabbing = false;
        [HideInInspector] public bool isCurrentlyPlayable = false;

        [Header("Wall grab & jump")]
        public Vector2 grabRightOffset = new Vector2(0.16f, 0f);
        public Vector2 grabLeftOffset = new Vector2(-0.16f, 0f);
        public float grabCheckRadius = 0.24f;
        public float slideSpeed = 2.5f;
        public Vector2 wallJumpForce = new Vector2(10.5f, 18f);
        public Vector2 wallClimbForce = new Vector2(4f, 14f);
        [SerializeField] private float wallSlideCoyoteTime = 0.1f;

        [Header("Wall Jump Settings")]
        [SerializeField] private float wallJumpHorizontalForce = 15f; // Separate horizontal force control
        [SerializeField] private float wallJumpVerticalForce = 18f; // Separate vertical force control
        [SerializeField] private float wallStickCancelForce = 5f;

        [Header("Knockback Settings")]
        [SerializeField] private float knockbackDuration = 0.2f;
        [SerializeField] private float knockbackMovementLockDuration = 0.3f;
        private bool isKnockback = false;
        private float knockbackTimer = 0f;

        [Header("Camera Shake")]
        [SerializeField] public CameraShake cameraShake;
        [SerializeField] public float shakeIntensity = 5;
        [SerializeField] public float shakeTime = 0.1f;

        [Header("Ground Slam Detection")]
        [SerializeField] private float groundSlamMinHeight = 3f; // Minimum height difference to consider ground slam
        [SerializeField] public bool canGroundSlam;

        [Header("Enemy Zone Visualization")]
        [SerializeField] private bool showEnemyZones = false;
        [SerializeField] private float gizmoInnerRadius = 3f;
        [SerializeField] private float gizmoOuterRadius = 7f;
        [SerializeField] private Color gizmoInnerColor = new Color(1f, 0f, 0f, 0.3f); // Red with transparency
        [SerializeField] private Color gizmoOuterColor = new Color(1f, 1f, 0f, 0.2f); // Yellow with transparency

        private Rigidbody2D m_rb;
        private ParticleSystem m_dustParticle;
        public bool m_facingRight = true;
        public bool m_facingLeft = false;
        private float m_groundedRememberTime = 0.02f;
        private float m_groundedRemember = 0f;
        private int m_extraJumps;
        private float m_extraJumpForce;
        public bool m_hasDashedInAir = false;
        private bool m_onWall = false;
        private bool m_onRightWall = false;
        private bool m_onLeftWall = false;
        public bool m_wallGrabbing = false;
        private float m_wallStickTime = 0.25f;
        private float m_wallStick = 0f;
        private bool m_wallJumping = false;
        private float m_wallSlideCoyoteTimer;
        private int m_onWallSide = 0;
        private int m_playerSide = 1;

        // Dash variables
        private Vector2 m_dashDirection;
        private float m_dashTimeRemaining;
        private float m_dashCooldownRemaining;
        private float m_dashBufferTimer;
        private bool m_dashInputBuffered;
        private Vector2 m_dashEndVelocity;

        private CameraFollowObject _cameraFollowObject;
        [SerializeField] private GameObject _cameraFollowGO;

        private PlayerInput playerInput;
        private Controller inputControl;
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction dashAction;

        void Start()
        {
            playerInput = GetComponent<PlayerInput>();
            inputControl = new Controller();
            inputControl.Enable();

            moveAction = inputControl.Gameplay.Move;
            jumpAction = inputControl.Gameplay.Jump;
            dashAction = inputControl.Gameplay.Dash;

            PoolManager.instance.CreatePool(dashEffect, 2);
            PoolManager.instance.CreatePool(jumpEffect, 2);

            if (transform.CompareTag("Player"))
                isCurrentlyPlayable = true;

            currentStamina = maxStamina;
            currentStamina = maxStamina;
            if (staminaBar1 != null)
            {
                staminaBarFullWidth = staminaBar1.sizeDelta.x;
                UpdateStaminaBar();
            }
            m_facingLeft = !m_facingRight;
            m_extraJumps = extraJumpCount;
            m_extraJumpForce = jumpForce * 0.7f;
            m_rb = GetComponent<Rigidbody2D>();
            m_rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            m_dustParticle = GetComponentInChildren<ParticleSystem>();
            _cameraFollowObject = _cameraFollowGO.GetComponent<CameraFollowObject>();
        }

        private void Update()
        {
            if (!isCurrentlyPlayable) return;

            // Get input
            Vector2 moveInputVector = moveAction.ReadValue<Vector2>();
            moveInput = moveInputVector.x;

            // Grounded remember
            m_groundedRemember -= Time.deltaTime;
            if (isGrounded)
            {
                m_groundedRemember = m_groundedRememberTime;
                m_extraJumps = extraJumpCount;
                m_hasDashedInAir = false;
            }

            // Handle dash input buffering
            if (dashAction.triggered)
            {
                m_dashBufferTimer = dashBufferTime;
                m_dashInputBuffered = true;
            }
            else if (m_dashBufferTimer > 0)
            {
                m_dashBufferTimer -= Time.deltaTime;
            }
            else
            {
                m_dashInputBuffered = false;
            }

            // Stamina regen
            if (!isDashing && Time.time > lastDashTime + staminaRegenDelay)
            {
                currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
                UpdateStaminaBar();
            }

            // Dash cooldown
            if (m_dashCooldownRemaining > 0)
            {
                m_dashCooldownRemaining -= Time.deltaTime;
            }

            // Try to execute buffered dash
            if (m_dashInputBuffered && CanDash())
            {
                ExecuteDash(moveInputVector);
            }

            // Jumping
            if (canJump && !isDashing)
            {
                HandleJumping();
            }
        }

        private void FixedUpdate()
        {
            // Check grounded and wall states
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);
            var position = transform.position;
            m_onWall = Physics2D.OverlapCircle((Vector2)position + grabRightOffset, grabCheckRadius, whatIsGround)
                      || Physics2D.OverlapCircle((Vector2)position + grabLeftOffset, grabCheckRadius, whatIsGround);
            m_onRightWall = Physics2D.OverlapCircle((Vector2)position + grabRightOffset, grabCheckRadius, whatIsGround);
            m_onLeftWall = Physics2D.OverlapCircle((Vector2)position + grabLeftOffset, grabCheckRadius, whatIsGround);

            CalculateSides();
            CheckGroundSlam();
            if (isKnockback)
            {
                knockbackTimer -= Time.fixedDeltaTime;
                if (knockbackTimer <= 0)
                {
                    isKnockback = false;
                    // Movement will be re-enabled by the coroutine
                }
            }

            // Skip normal movement if in knockback
            if (isKnockback) return;
            if ((m_wallGrabbing || isGrounded) && m_wallJumping)
            {
                m_wallJumping = false;
            }

            if (!isCurrentlyPlayable) return;

            // Dashing
            if (isDashing)
            {
                if (m_dashTimeRemaining > 0)
                {
                    m_dashTimeRemaining -= Time.fixedDeltaTime;
                    m_rb.velocity = m_dashDirection * dashSpeed;
                }
                else
                {
                    // Dash ended - apply end speed
                    isDashing = false;
                    m_dashEndVelocity = m_dashDirection * dashSpeed * dashEndSpeedMultiplier;
                    m_rb.velocity = m_dashEndVelocity;
                    if (playerAttack != null)
                    {
                        playerAttack.OnDashEnd();
                    }
                }
            }
            else
            {
                // Normal movement
                if (canMove && !m_wallGrabbing)
                {
                    if (m_wallJumping)
                    {
                        m_rb.velocity = Vector2.Lerp(m_rb.velocity,
                            new Vector2(moveInput * speed, m_rb.velocity.y),
                            1.5f * Time.fixedDeltaTime);
                    }
                    else
                    {
                        m_rb.velocity = new Vector2(moveInput * speed, m_rb.velocity.y);
                    }
                }
                else if (!canMove)
                {
                    m_rb.velocity = new Vector2(0f, m_rb.velocity.y);
                }
            }

            // Better jump physics
            if (m_rb.velocity.y < 0f && !isDashing)
            {
                m_rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
            }

            // Flipping
            if (canFlip && !isDashing)
            {
                if (!m_facingRight && moveInput > 0f)
                    Flip();
                else if (m_facingRight && moveInput < 0f)
                    Flip();
            }

            // Wall grab
            if (!isDashing)
            {
                HandleWallGrabbing();
            }

            // Dust particles
            float playerVelocityMag = m_rb.velocity.sqrMagnitude;
            if (m_dustParticle.isPlaying && playerVelocityMag == 0f)
            {
                m_dustParticle.Stop();
            }
            else if (!m_dustParticle.isPlaying && playerVelocityMag > 0f)
            {
                m_dustParticle.Play();
            }
        }

        public bool CanDash()
        {
            PlayerAttack playerAttack = GetComponent<PlayerAttack>();
            bool isInUpwardRecovery = playerAttack != null && playerAttack.isInUpwardAttackRecovery;

            return canDash &&
                   !isDashing &&
                   m_dashCooldownRemaining <= 0f &&
                   (!m_hasDashedInAir || isGrounded) &&
                   currentStamina >= dashCost &&
                   !isInUpwardRecovery; // Added this check
        }

        private void ExecuteDash(Vector2 inputDirection)
        {
            float dashDuration = horizontalDashDuration; // Default to horizontal duration

            // Determine dash direction based on input
            if (inputDirection.magnitude < 0.1f)
            {
                // Default to facing direction if no input
                m_dashDirection = m_facingRight ? Vector2.right : Vector2.left;
            }
            else
            {
                // Snap to 8 directions and set appropriate duration
                float angle = Mathf.Atan2(inputDirection.y, inputDirection.x);
                float snappedAngle = Mathf.Round(angle / (Mathf.PI / 4)) * (Mathf.PI / 4);
                m_dashDirection = new Vector2(Mathf.Cos(snappedAngle), Mathf.Sin(snappedAngle)).normalized;

                // Set duration based on direction
                if (Mathf.Abs(m_dashDirection.y) > 0.9f) // Mostly vertical
                {
                    dashDuration = m_dashDirection.y > 0 ? upwardDashDuration : downwardDashDuration;
                }
                else if (Mathf.Abs(m_dashDirection.x) > 0.1f && Mathf.Abs(m_dashDirection.y) > 0.1f) // Diagonal
                {
                    dashDuration = diagonalDashDuration;
                }
            }
            if (playerAttack != null)
            {
                playerAttack.OnDashStart();
            }

            // Consume stamina
            currentStamina -= dashCost;
            lastDashTime = Time.time;
            UpdateStaminaBar();

            // Start dash with direction-specific duration
            isDashing = true;
            m_dashTimeRemaining = dashDuration;
            m_dashCooldownRemaining = dashCooldown;
            m_dashInputBuffered = false;
            m_dashBufferTimer = 0f;

            if (!isGrounded)
            {
                m_hasDashedInAir = true;
            }

            // Handle dash effect rotation for ALL directions
            GameObject dashEffectInstance = PoolManager.instance.ReuseObject(dashEffect, transform.position, Quaternion.identity);
            ParticleSystem.MainModule main = dashEffectInstance.GetComponent<ParticleSystem>().main;

            // Calculate angle (left dash will be 180 degrees, right 0 degrees, up 90, etc.)
            float rotationAngle = -Mathf.Atan2(m_dashDirection.y, m_dashDirection.x);
            main.startRotation = rotationAngle;

            // If your particles face the wrong direction, you might need to add an offset:
            // main.startRotation = rotationAngle + Mathf.PI/2; // 90 degree offset if needed

            cameraShake.ShakeCamera(shakeIntensity, shakeTime);
            RumbleManager.instance.RumblePulse(0.01f, 0f, 0.05f);
        }

        private void HandleJumping()
        {
            if (jumpAction.triggered)
            {
                // Check for wall jump (whether sliding or just touching wall)
                if ((m_onWall && !isGrounded) || m_wallGrabbing)
                {
                    PerformWallJump();
                    return;
                }
                else if (m_extraJumps > 0 && !isGrounded)
                {
                    // Extra jump (unchanged)
                    m_rb.velocity = new Vector2(m_rb.velocity.x, m_extraJumpForce);
                    m_extraJumps--;
                    PoolManager.instance.ReuseObject(jumpEffect, groundCheck.position, Quaternion.identity);
                }
                else if (isGrounded || m_groundedRemember > 0f)
                {
                    // Normal jump (unchanged)
                    m_rb.velocity = new Vector2(m_rb.velocity.x, jumpForce);
                    PoolManager.instance.ReuseObject(jumpEffect, groundCheck.position, Quaternion.identity);
                }
            }
        }

        private void PerformWallJump()
        {
            m_wallGrabbing = false;
            m_wallJumping = true;

            // First cancel any wall stick velocity
            m_rb.velocity = new Vector2(m_rb.velocity.x * 0.5f, 0);

            // Determine direction based on input
            bool jumpingAwayFromWall = moveInput != m_onWallSide;

            // Calculate forces
            float horizontalForce = jumpingAwayFromWall ?
                -m_onWallSide * wallJumpHorizontalForce :
                -m_onWallSide * wallJumpHorizontalForce * 0.7f;

            // Apply consistent force regardless of wall grab state
            m_rb.AddForce(new Vector2(
                horizontalForce,
                wallJumpVerticalForce
            ), ForceMode2D.Impulse);

            // Flip if needed
            if (jumpingAwayFromWall && m_playerSide == m_onWallSide)
            {
                Flip();
            }

            // Reset jumps and effects
            m_extraJumps = extraJumpCount;
            PoolManager.instance.ReuseObject(jumpEffect, groundCheck.position, Quaternion.identity);
        }

        private void HandleWallGrabbing()
        {
            bool shouldHoldWall = (m_onRightWall && moveInput > 0) || (m_onLeftWall && moveInput < 0);

            if (m_onWall && !isGrounded && m_rb.velocity.y <= 0f && m_playerSide == m_onWallSide)
            {
                if (shouldHoldWall)
                {
                    // Reset coyote timer while holding input
                    m_wallSlideCoyoteTimer = wallSlideCoyoteTime;
                    actuallyWallGrabbing = true;
                    m_wallGrabbing = true;
                    m_rb.velocity = new Vector2(moveInput * speed, -slideSpeed);
                    m_wallStick = m_wallStickTime;
                }
                else if (m_wallSlideCoyoteTimer > 0f)
                {
                    // Still in coyote time
                    m_wallSlideCoyoteTimer -= Time.fixedDeltaTime;
                    actuallyWallGrabbing = true;
                    m_wallGrabbing = true;
                    m_rb.velocity = new Vector2(0f, -slideSpeed);
                }
                else
                {
                    actuallyWallGrabbing = false;
                    m_wallGrabbing = false;
                }
            }
            else
            {
                m_wallStick -= Time.fixedDeltaTime;
                actuallyWallGrabbing = false;
                if (m_wallStick <= 0f)
                    m_wallGrabbing = false;
            }

            // Reset coyote time when grabbing a new wall
            if ((m_onRightWall && moveInput > 0) || (m_onLeftWall && moveInput < 0))
            {
                m_wallSlideCoyoteTimer = wallSlideCoyoteTime;
            }

            if (m_wallGrabbing && isGrounded)
                m_wallGrabbing = false;
        }
        private void CheckGroundSlam()
        {
            if (!isGrounded)
            {
                // Use RaycastHit2D with Mathf.Infinity as the distance
                RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, Mathf.Infinity, whatIsGround);

                if (hit.collider != null)
                {
                    float heightAboveGround = transform.position.y - hit.point.y;

                    if (heightAboveGround >= groundSlamMinHeight)
                    {
                        canGroundSlam = true;
                    }
                    else
                    {
                        canGroundSlam = false;
                    }
                }
                else
                {
                    // No ground detected below at all
                    canGroundSlam = false;
                }
            }
            else
            {
                // Player is grounded, can't ground slam
                canGroundSlam = false;
            }
        }


        // Add this method to handle knockback
        public void ApplyKnockback(Vector2 knockbackForce)
        {
            // Don't allow knockback during dash
            if (isDashing) return;

            // Reset velocity and apply force
            m_rb.velocity = Vector2.zero;
            m_rb.AddForce(knockbackForce, ForceMode2D.Impulse);

            // Set knockback state
            isKnockback = true;
            knockbackTimer = knockbackDuration;

            // Temporarily disable movement
            canMove = false;
            canDash = false;
            canJump = false;

            // Start coroutine to re-enable movement
            StartCoroutine(EndKnockbackAfterTime(knockbackMovementLockDuration));
        }

        private IEnumerator EndKnockbackAfterTime(float time)
        {
            yield return new WaitForSeconds(time);

            // Only re-enable if we're not still in knockback
            if (knockbackTimer <= 0)
            {
                canMove = true;
                canDash = true;
                canJump = true;
            }
        }

        void Flip()
        {
            m_facingRight = !m_facingRight;
            m_facingLeft = !m_facingRight; // New line - keeps them in sync
            Vector3 newScale = transform.localScale;
            newScale.x *= -1;
            transform.localScale = newScale;
            _cameraFollowObject.CallTurn();
        }

        void CalculateSides()
        {
            m_onWallSide = m_onRightWall ? 1 : (m_onLeftWall ? -1 : 0);
            m_playerSide = m_facingRight ? 1 : -1;
        }

        public void UpdateStaminaBar()
        {
            if (staminaBar1 == null) return;

            float staminaPercentage = currentStamina / maxStamina;
            Vector2 newSize = new Vector2(staminaBarFullWidth * staminaPercentage, staminaBar1.sizeDelta.y);
            staminaBar1.sizeDelta = newSize;
        }


        public void DisableMovement()
        {
            canMove = false;
            canFlip = false;
            canJump = false;
            canDash = false;
            m_rb.velocity = new Vector2(0f, m_rb.velocity.y);
            if (isDashing) EndDash();
        }

        public void EnableMovement()
        {
            canMove = true;
            canFlip = true;
            canJump = true;
            canDash = true;
        }

        public void FreezePlayer()
        {
            canMove = false;
            m_rb.velocity = Vector2.zero;
            if (isDashing) EndDash();
        }

        public void UnfreezePlayer()
        {
            canMove = true;
        }

        private void EndDash()
        {
            isDashing = false;
            m_dashTimeRemaining = 0f;
            m_rb.velocity = m_dashDirection * dashSpeed * dashEndSpeedMultiplier;
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }

            if (grabRightOffset != null && grabLeftOffset != null)
            {
                Gizmos.DrawWireSphere((Vector2)transform.position + grabRightOffset, grabCheckRadius);
                Gizmos.DrawWireSphere((Vector2)transform.position + grabLeftOffset, grabCheckRadius);
            }

            // Add to OnDrawGizmos:
            if (showEnemyZones)
            {
                // Draw donut zones around player
                Gizmos.color = gizmoOuterColor;
                Gizmos.DrawSphere(transform.position, gizmoOuterRadius);

                Gizmos.color = gizmoInnerColor;
                Gizmos.DrawSphere(transform.position, gizmoInnerRadius);

                // Clear the inner part of the outer sphere
                Gizmos.color = Color.clear;
                Gizmos.DrawSphere(transform.position, gizmoInnerRadius);
            }

            Gizmos.color = Color.yellow;
            Vector3 rayStart = transform.position;
            Vector3 rayEnd = rayStart + Vector3.down * 1000f; // Just draw a very long line for visualization
            Gizmos.DrawLine(rayStart, rayEnd);

            // Draw a small marker at the minimum height threshold
            if (groundSlamMinHeight > 0)
            {
                Gizmos.color = Color.cyan;
                float minHeightY = transform.position.y - groundSlamMinHeight;
                Vector3 minHeightStart = new Vector3(transform.position.x - 0.5f, minHeightY, 0);
                Vector3 minHeightEnd = new Vector3(transform.position.x + 0.5f, minHeightY, 0);
                Gizmos.DrawLine(minHeightStart, minHeightEnd);
            }

            // Draw actual hit point if in play mode
            if (Application.isPlaying && !isGrounded)
            {
                RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, Mathf.Infinity, whatIsGround);
                if (hit.collider != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(hit.point, 0.1f);
                    Gizmos.DrawLine(transform.position, hit.point);
                }
            }
        }
    }
}