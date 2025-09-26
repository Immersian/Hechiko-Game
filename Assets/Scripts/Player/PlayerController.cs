using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine.UI;

namespace SupanthaPaul
{
    //[RequireComponent(typeof(PlayerInput))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float speed;
        [Header("Jumping")]
        [SerializeField] private float jumpForce;
        [SerializeField] private float fallMultiplier;
        [SerializeField] private float maxFallSpeed = -25f;
        [SerializeField] public Transform groundCheck;
        [SerializeField] private float groundCheckRadius;
        [SerializeField] private LayerMask whatIsGround;
        [SerializeField] private int extraJumpCount = 1;
        [SerializeField] private GameObject jumpEffect;

        [Header("Celeste-Style Dash")]
        [SerializeField] private float dashSpeed = 30f;
        [SerializeField] private float horizontalDashDuration = 0.15f;
        [SerializeField] private float upwardDashDuration = 0.12f;
        [SerializeField] private float downwardDashDuration = 0.1f;
        [SerializeField] private float diagonalDashDuration = 0.13f;
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
        public RectTransform staminaBar1;
        private float staminaBarFullWidth;

        [Header("Attack References")]
        [SerializeField] private PlayerAttack playerAttack;

        [Header("Sound Effects")]
        [SerializeField] private AudioClip jumpSound;
        [SerializeField] private AudioClip doubleJumpSound;
        [SerializeField] private AudioClip dashSound;
        [SerializeField] private AudioClip landSound;
        [SerializeField] private AudioClip wallGrabSound;
        [SerializeField] private AudioClip wallJumpSound;
        [SerializeField] private AudioClip footstepSound;
        [SerializeField] private AudioClip groundSlamSound;
        [SerializeField] private AudioClip staminaRefillSound;
        [SerializeField] private float footstepInterval = 0.3f;
        [SerializeField] private float footstepVolume = 0.4f;

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
        [SerializeField] private float wallJumpHorizontalForce = 15f;
        [SerializeField] private float wallJumpVerticalForce = 18f;

        [Header("Healing Settings")]
        [SerializeField] private float healAmount = 25f;
        [SerializeField] private GameObject healEffect;
        [SerializeField] private AudioClip healSound;
        private bool isHealing = false;
        public event Action OnHealStart;
        public event Action OnHealComplete;

        [Header("Potion Images")]
        [SerializeField] private Image[] potionImages = new Image[3];
        private int currentPotions = 0;
        private int maxPotions = 3;

        public PlayerHealth playerHealth;
        public Animator animator;
        public AudioSource audioSource;

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
        [SerializeField] private float groundSlamMinHeight = 3f;
        [SerializeField] public bool canGroundSlam;

        [Header("Enemy Zone Visualization")]
        [SerializeField] private bool showEnemyZones = false;
        [SerializeField] private float gizmoInnerRadius = 3f;
        [SerializeField] private float gizmoOuterRadius = 7f;
        [SerializeField] private Color gizmoInnerColor = new Color(1f, 0f, 0f, 0.3f);
        [SerializeField] private Color gizmoOuterColor = new Color(1f, 1f, 0f, 0.2f);

        [Header("Dash Refresh Settings")]
        [SerializeField] private float dashRefreshGracePeriod = 0.2f;
        private float m_lastDashRefreshTime = -10f;
        private bool m_dashRefreshedThisFrame = false;

        [Header("Visual Feedback")]
        [SerializeField] private SimpleFlash flashEffect;

        [Header("Tilemap Phasing")]
        [SerializeField] private TilemapBev tilemapBev;

        [Header("Effector Layer")]
        [SerializeField] private LayerMask whatIsEffector;

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

        private float lastFootstepTime;
        private bool wasGrounded;
        private bool wasWallGrabbing;

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
            if (staminaBar1 != null)
            {
                staminaBarFullWidth = staminaBar1.sizeDelta.x;
                UpdateStaminaBar();
            }
            // Add this to your PlayerController's Start() method
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.spatialize = false;
                audioSource.spatialBlend = 0; // Makes it fully 2D (non-directional)
            }
            currentPotions = maxPotions;
            UpdatePotionImages();
            m_facingLeft = !m_facingRight;
            m_extraJumps = extraJumpCount;
            m_extraJumpForce = jumpForce * 0.7f;
            m_rb = GetComponent<Rigidbody2D>();
            m_rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            m_dustParticle = GetComponentInChildren<ParticleSystem>();
            _cameraFollowObject = _cameraFollowGO.GetComponent<CameraFollowObject>();
            playerHealth = GetComponent<PlayerHealth>();
            animator = GetComponentInChildren<Animator>();
            audioSource = GetComponent<AudioSource>();

            wasGrounded = isGrounded;
            wasWallGrabbing = m_wallGrabbing;
        }

        private void Update()
        {
            if (!isCurrentlyPlayable) return;

            Vector2 moveInputVector = moveAction.ReadValue<Vector2>();
            moveInput = moveInputVector.x;

            ParryScript parryScript = GetComponentInChildren<ParryScript>();
            if (parryScript != null && parryScript.IsBlocking)
            {
                moveInput = 0f; // Zero out movement input while blocking
            }

            m_groundedRemember -= Time.deltaTime;
            if (isGrounded)
            {
                m_groundedRemember = m_groundedRememberTime;
                m_extraJumps = extraJumpCount;
                m_hasDashedInAir = false;
                flashEffect.RegularColour();
            }

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

            if (!isDashing && Time.time > lastDashTime + staminaRegenDelay)
            {
                currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
                UpdateStaminaBar();
            }

            if (m_dashCooldownRemaining > 0)
            {
                m_dashCooldownRemaining -= Time.deltaTime;
            }

            if (m_dashInputBuffered && CanDash())
            {
                ExecuteDash(moveInputVector);
            }

            if (canJump && !isDashing)
            {
                HandleJumping();
            }

            if (InputManager.instance.inputControl.Gameplay.Heal.WasPressedThisFrame())
            {
                TryHeal();
            }

            // Sound effects
            if (isGrounded && !wasGrounded && m_rb.velocity.y <= 0f)
            {
                PlaySound(landSound, 0.7f);
            }
            wasGrounded = isGrounded;

            if (m_wallGrabbing && !wasWallGrabbing)
            {
                PlaySound(wallGrabSound, 0.6f);
            }
            wasWallGrabbing = m_wallGrabbing;

            if (isGrounded && Mathf.Abs(moveInput) > 0.1f && Time.time - lastFootstepTime > footstepInterval)
            {
                PlaySound(footstepSound, footstepVolume);
                lastFootstepTime = Time.time;
            }
        }

        private void FixedUpdate()
        {
            // Check if player is on either ground or effector
            bool onGround = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);
            bool onEffector = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsEffector);
            isGrounded = onGround || onEffector;

            // Check for walls (excluding effectors)
            var position = transform.position;
            m_onRightWall = Physics2D.OverlapCircle((Vector2)position + grabRightOffset, grabCheckRadius, whatIsGround) &&
                           !Physics2D.OverlapCircle((Vector2)position + grabRightOffset, grabCheckRadius, whatIsEffector);

            m_onLeftWall = Physics2D.OverlapCircle((Vector2)position + grabLeftOffset, grabCheckRadius, whatIsGround) &&
                          !Physics2D.OverlapCircle((Vector2)position + grabLeftOffset, grabCheckRadius, whatIsEffector);

            m_onWall = m_onRightWall || m_onLeftWall;

            CalculateSides();
            CheckGroundSlam();

            if (isKnockback)
            {
                knockbackTimer -= Time.fixedDeltaTime;
                if (knockbackTimer <= 0)
                {
                    isKnockback = false;
                }
            }

            if (isKnockback) return;
            if ((m_wallGrabbing || isGrounded) && m_wallJumping)
            {
                m_wallJumping = false;
            }

            if (!isCurrentlyPlayable) return;

            ParryScript parryScript = GetComponentInChildren<ParryScript>();
            bool isBlocking = parryScript != null && parryScript.IsBlocking;

            if (isDashing)
            {
                if (m_dashTimeRemaining > 0)
                {
                    m_dashTimeRemaining -= Time.fixedDeltaTime;
                    m_rb.velocity = m_dashDirection * dashSpeed;
                    flashEffect.DashingTrans();
                }
                else
                {
                    isDashing = false;
                    m_dashEndVelocity = m_dashDirection * dashSpeed * dashEndSpeedMultiplier;
                    m_rb.velocity = m_dashEndVelocity;

                    if (!isGrounded && CanSetDashUsed() && !m_dashRefreshedThisFrame)
                    {
                        m_hasDashedInAir = true;
                        flashEffect.NoDash();
                    }

                    if (playerAttack != null)
                    {
                        playerAttack.OnDashEnd();
                    }

                    m_dashRefreshedThisFrame = false;
                }
            }
            else
            {
                if (canMove && !m_wallGrabbing && !isBlocking)
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
                else if (!canMove || isBlocking) // Added isBlocking check
                {
                    m_rb.velocity = new Vector2(0f, m_rb.velocity.y);
                }
            }

            if (m_rb.velocity.y < 0f && !isDashing)
            {
                m_rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;

                if (m_rb.velocity.y < maxFallSpeed)
                {
                    m_rb.velocity = new Vector2(m_rb.velocity.x, maxFallSpeed);
                }
            }

            if (canFlip && !isDashing)
            {
                if (!m_facingRight && moveInput > 0f)
                    Flip();
                else if (m_facingRight && moveInput < 0f)
                    Flip();
            }

            if (!isDashing)
            {
                HandleWallGrabbing();
            }

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
            PlayerAttack playerAttack = GetComponentInChildren<PlayerAttack>();
            bool isInUpwardRecovery = playerAttack != null && playerAttack.isInUpwardAttackRecovery;
            bool isInPostAttackCooldown = playerAttack != null && playerAttack.IsInPostAttackDashCooldown;

            return canDash &&
                   !isDashing &&
                   m_dashCooldownRemaining <= 0f &&
                   (!m_hasDashedInAir || isGrounded) &&
                   currentStamina >= dashCost &&
                   !isInUpwardRecovery &&
                   !isInPostAttackCooldown;
        }

        private void ExecuteDash(Vector2 inputDirection)
        {
            ParryScript parryScript = GetComponentInChildren<ParryScript>();
            if (parryScript != null && parryScript.IsBlocking)
            {
                parryScript.ForceEndBlock();
            }

            float dashDuration = horizontalDashDuration;
            flashEffect.DashingTrans();

            if (inputDirection.magnitude < 0.1f)
            {
                m_dashDirection = m_facingRight ? Vector2.right : Vector2.left;
            }
            else
            {
                float angle = Mathf.Atan2(inputDirection.y, inputDirection.x);
                float snappedAngle = Mathf.Round(angle / (Mathf.PI / 4)) * (Mathf.PI / 4);
                m_dashDirection = new Vector2(Mathf.Cos(snappedAngle), Mathf.Sin(snappedAngle)).normalized;

                if (Mathf.Abs(m_dashDirection.y) > 0.9f)
                {
                    dashDuration = m_dashDirection.y > 0 ? upwardDashDuration : downwardDashDuration;
                }
                else if (Mathf.Abs(m_dashDirection.x) > 0.1f && Mathf.Abs(m_dashDirection.y) > 0.1f)
                {
                    dashDuration = diagonalDashDuration;
                }
            }

            if (tilemapBev != null)
            {
                tilemapBev.ToggleTilemaps();
            }

            if (playerAttack != null)
            {
                playerAttack.OnDashStart();
            }

            currentStamina -= dashCost;
            lastDashTime = Time.time;
            UpdateStaminaBar();

            isDashing = true;
            m_dashTimeRemaining = dashDuration;
            m_dashCooldownRemaining = dashCooldown;
            m_dashInputBuffered = false;
            m_dashBufferTimer = 0f;

            PlaySound(dashSound, 0.8f);

            GameObject dashEffectInstance = PoolManager.instance.ReuseObject(dashEffect, transform.position, Quaternion.identity);
            ParticleSystem.MainModule main = dashEffectInstance.GetComponent<ParticleSystem>().main;
            float rotationAngle = -Mathf.Atan2(m_dashDirection.y, m_dashDirection.x);
            main.startRotation = rotationAngle;

            cameraShake.ShakeCamera(shakeIntensity, shakeTime);
            RumbleManager.instance.RumblePulse(0.5f, 0.5f, 0.15f);
        }

        private void TryHeal()
        {
            if (CanHeal() && currentPotions > 0)
            {
                currentPotions--;
                UpdatePotionImages();
                StartCoroutine(PerformHeal());
            }
        }

        private bool CanHeal()
        {
            return isGrounded &&
                   !isHealing &&
                   !isDashing &&
                   !isKnockback;
        }

        private IEnumerator PerformHeal()
        {
            isHealing = true;
            OnHealStart?.Invoke();

            canMove = false;
            canDash = false;
            canFlip = false;

            if (healSound != null && audioSource != null)
                audioSource.PlayOneShot(healSound);

            if (healEffect != null)
                Instantiate(healEffect, transform.position, Quaternion.identity);

            yield return null;

            float animLength = animator.GetCurrentAnimatorStateInfo(0).length;
            float elapsedTime = 0f;
            while (elapsedTime < animLength && isHealing)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if (isHealing)
            {
                playerHealth.Heal(Mathf.RoundToInt(healAmount));
            }

            isHealing = false;
            OnHealComplete?.Invoke();

            canMove = true;
            canDash = true;
            canFlip = true;
        }

        public void InterruptHealing()
        {
            if (isHealing)
            {
                isHealing = false;
                OnHealComplete?.Invoke();
                canMove = true;
                canDash = true;
                canFlip = true;
            }
        }

        private void UpdatePotionImages()
        {
            for (int i = 0; i < potionImages.Length; i++)
            {
                int reverseIndex = potionImages.Length - 1 - i;
                potionImages[i].enabled = (reverseIndex < currentPotions);
            }
        }

        public void AddPotion()
        {
            if (currentPotions < maxPotions)
            {
                currentPotions++;
                UpdatePotionImages();
            }
        }

        public void RefillAllPotions()
        {
            currentPotions = maxPotions;
            UpdatePotionImages();
        }

        private void HandleJumping()
        {
            if (jumpAction.triggered)
            {
                if ((m_onWall && !isGrounded) || m_wallGrabbing)
                {
                    PerformWallJump();
                    return;
                }
                else if (m_extraJumps > 0 && !isGrounded)
                {
                    m_rb.velocity = new Vector2(m_rb.velocity.x, m_extraJumpForce);
                    m_extraJumps--;
                    PoolManager.instance.ReuseObject(jumpEffect, groundCheck.position, Quaternion.identity);
                    PlaySound(doubleJumpSound, 0.7f);
                }
                else if (isGrounded || m_groundedRemember > 0f)
                {
                    m_rb.velocity = new Vector2(m_rb.velocity.x, jumpForce);
                    PoolManager.instance.ReuseObject(jumpEffect, groundCheck.position, Quaternion.identity);
                    PlaySound(jumpSound, 0.7f);
                }
            }
        }

        private void PerformWallJump()
        {
            m_wallGrabbing = false;
            m_wallJumping = true;

            m_rb.velocity = new Vector2(m_rb.velocity.x * 0.5f, 0);

            bool jumpingAwayFromWall = moveInput != m_onWallSide;

            float horizontalForce = jumpingAwayFromWall ?
                -m_onWallSide * wallJumpHorizontalForce :
                -m_onWallSide * wallJumpHorizontalForce * 0.7f;

            m_rb.AddForce(new Vector2(
                horizontalForce,
                wallJumpVerticalForce
            ), ForceMode2D.Impulse);

            if (jumpingAwayFromWall && m_playerSide == m_onWallSide)
            {
                Flip();
            }

            m_extraJumps = extraJumpCount;
            PoolManager.instance.ReuseObject(jumpEffect, groundCheck.position, Quaternion.identity);
            PlaySound(wallJumpSound, 0.8f);
        }

        private void HandleWallGrabbing()
        {
            bool shouldHoldWall = (m_onRightWall && moveInput > 0) || (m_onLeftWall && moveInput < 0);

            if (m_onWall && !isGrounded && m_rb.velocity.y <= 0f && m_playerSide == m_onWallSide)
            {
                if (shouldHoldWall)
                {
                    m_wallSlideCoyoteTimer = wallSlideCoyoteTime;
                    actuallyWallGrabbing = true;
                    m_wallGrabbing = true;
                    m_rb.velocity = new Vector2(moveInput * speed, -slideSpeed);
                    m_wallStick = m_wallStickTime;
                }
                else if (m_wallSlideCoyoteTimer > 0f)
                {
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
                // Check for both ground and effector layers
                int combinedLayers = whatIsGround | whatIsEffector;
                RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, Mathf.Infinity, combinedLayers);

                if (hit.collider != null)
                {
                    float heightAboveGround = transform.position.y - hit.point.y;
                    canGroundSlam = heightAboveGround >= groundSlamMinHeight;
                }
                else
                {
                    canGroundSlam = false;
                }
            }
            else
            {
                canGroundSlam = false;
            }
        }

        public void ApplyKnockback(Vector2 knockbackForce)
        {
            if (isDashing)
            {
                EndDash();
            }

            m_rb.velocity = Vector2.zero;
            m_rb.AddForce(knockbackForce, ForceMode2D.Impulse);

            isKnockback = true;
            knockbackTimer = knockbackDuration;

            canMove = false;
            canDash = false;
            canJump = false;

            StartCoroutine(EndKnockbackAfterTime(knockbackMovementLockDuration));
        }

        private IEnumerator EndKnockbackAfterTime(float time)
        {
            yield return new WaitForSeconds(time);

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
            m_facingLeft = !m_facingRight;
            Vector3 newScale = transform.localScale;
            newScale.x *= -1;
            transform.localScale = newScale;
            _cameraFollowObject.CallTurn();
        }
        private bool IsEffectorWall(Vector2 checkPosition)
        {
            return Physics2D.OverlapCircle(checkPosition, grabCheckRadius, whatIsEffector);
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

        public void RefreshDash()
        {
            m_hasDashedInAir = false;
            m_dashCooldownRemaining = 0f;
            currentStamina = maxStamina;
            UpdateStaminaBar();
            m_lastDashRefreshTime = Time.time;
            m_dashRefreshedThisFrame = true;
            flashEffect.RegularColour();
            PlaySound(staminaRefillSound, 0.6f);
        }

        private bool CanSetDashUsed()
        {
            return Time.time > m_lastDashRefreshTime + dashRefreshGracePeriod;
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
            if (!isDashing) return;

            isDashing = false;
            m_dashTimeRemaining = 0f;
            m_rb.velocity = m_dashDirection * dashSpeed * dashEndSpeedMultiplier;
            flashEffect.RegularColour();

            if (playerAttack != null)
            {
                playerAttack.OnDashEnd();
            }
        }

        public void InterruptDash()
        {
            if (isDashing)
            {
                isDashing = false;
                m_dashTimeRemaining = 0f;
                m_rb.velocity = new Vector2(
                    m_dashDirection.x * dashSpeed * dashEndSpeedMultiplier,
                    0f
                );
                flashEffect.RegularColour();

                if (playerAttack != null)
                {
                    playerAttack.OnDashEnd();
                }

                m_dashCooldownRemaining = dashCooldown * 0.5f;
            }
        }

        public void ForceDashCooldown(float duration)
        {
            m_dashCooldownRemaining = duration;
            m_hasDashedInAir = true;
        }

        public void PlayGroundSlamSound()
        {
            PlaySound(groundSlamSound, 1f);
        }

        private void PlaySound(AudioClip clip, float volume = 1f)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip, volume);
            }
        }
        public void ResetControllerState()
        {
            // Reset all movement flags to default
            canMove = true;
            canFlip = true;
            canJump = true;
            canDash = true;
            isDashing = false;
            isKnockback = false;

            // Reset any timers or state variables
            m_dashCooldownRemaining = 0f;
            m_hasDashedInAir = false;

            // Ensure Rigidbody is in correct state
            if (m_rb != null)
            {
                m_rb.velocity = Vector2.zero;
            }
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

            if (showEnemyZones)
            {
                Gizmos.color = gizmoOuterColor;
                Gizmos.DrawSphere(transform.position, gizmoOuterRadius);

                Gizmos.color = gizmoInnerColor;
                Gizmos.DrawSphere(transform.position, gizmoInnerRadius);

                Gizmos.color = Color.clear;
                Gizmos.DrawSphere(transform.position, gizmoInnerRadius);
            }

            Gizmos.color = Color.yellow;
            Vector3 rayStart = transform.position;
            Vector3 rayEnd = rayStart + Vector3.down * 1000f;
            Gizmos.DrawLine(rayStart, rayEnd);

            if (groundSlamMinHeight > 0)
            {
                Gizmos.color = Color.cyan;
                float minHeightY = transform.position.y - groundSlamMinHeight;
                Vector3 minHeightStart = new Vector3(transform.position.x - 0.5f, minHeightY, 0);
                Vector3 minHeightEnd = new Vector3(transform.position.x + 0.5f, minHeightY, 0);
                Gizmos.DrawLine(minHeightStart, minHeightEnd);
            }

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