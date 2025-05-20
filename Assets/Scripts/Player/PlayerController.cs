using UnityEngine;
using UnityEngine.InputSystem;

namespace SupanthaPaul
{
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerController : MonoBehaviour
	{
		[SerializeField] private float speed;
		[Header("Jumping")]
		[SerializeField] private float jumpForce;
		[SerializeField] private float fallMultiplier;
		[SerializeField] private Transform groundCheck;
		[SerializeField] private float groundCheckRadius;
		[SerializeField] private LayerMask whatIsGround;
		[SerializeField] private int extraJumpCount = 1;
		[SerializeField] private GameObject jumpEffect;
		[Header("Dashing")]
		[SerializeField] private float dashSpeed = 30f;
		[Tooltip("Amount of time (in seconds) the player will be in the dashing speed")]
		[SerializeField] private float startDashTime = 0.1f;
		[Tooltip("Time (in seconds) between dashes")]
		[SerializeField] private float dashCooldown = 0.2f;
		[SerializeField] private GameObject dashEffect;

		// Access needed for handling animation in Player script and other uses
		[HideInInspector] public bool isGrounded;
		[HideInInspector] public float moveInput;
		[HideInInspector] public bool canMove = true;
        [HideInInspector] public bool canFlip = true;
        [HideInInspector] public bool canJump = true;
        [HideInInspector] public bool canDash = true;
        [HideInInspector] public bool isDashing = false;
		[HideInInspector] public bool actuallyWallGrabbing = false;
		// controls whether this instance is currently playable or not
		[HideInInspector] public bool isCurrentlyPlayable = false;

		[Header("Wall grab & jump")]
		[Tooltip("Right offset of the wall detection sphere")]
		public Vector2 grabRightOffset = new Vector2(0.16f, 0f);
		public Vector2 grabLeftOffset = new Vector2(-0.16f, 0f);
		public float grabCheckRadius = 0.24f;
		public float slideSpeed = 2.5f;
		public Vector2 wallJumpForce = new Vector2(10.5f, 18f);
		public Vector2 wallClimbForce = new Vector2(4f, 14f);
        [SerializeField] private float wallSlideCoyoteTime = 0.1f;

        private Rigidbody2D m_rb;
		private ParticleSystem m_dustParticle;
		public bool m_facingRight = true;
		private readonly float m_groundedRememberTime = 0.25f;
		private float m_groundedRemember = 0f;
		private int m_extraJumps;
		private float m_extraJumpForce;
		private float m_dashTime;
		private bool m_hasDashedInAir = false;
		private bool m_onWall = false;
		private bool m_onRightWall = false;
		private bool m_onLeftWall = false;
		private bool m_wallGrabbing = false;
		private readonly float m_wallStickTime = 0.25f;
		private float m_wallStick = 0f;
		private bool m_wallJumping = false;
		private float m_dashCooldown;
        private float m_wallSlideCoyoteTimer;

        // 0 -> none, 1 -> right, -1 -> left
        private int m_onWallSide = 0;
		private int m_playerSide = 1;

        private CameraFollowObject _cameraFollowObject;
		[SerializeField] private GameObject _cameraFollowGO;

        // Input System
        private PlayerInput playerInput;
        private Controller inputControl;
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction dashAction;


        void Start()
		{
            playerInput = GetComponent<PlayerInput>();
            inputControl = new Controller(); // Creates instance of your "Control" class
            inputControl.Enable();

            // Cache actions
            moveAction = inputControl.Gameplay.Move; // Replace "Player" with your action map name
            jumpAction = inputControl.Gameplay.Jump;
            dashAction = inputControl.Gameplay.Dash;

            // create pools for particles
            PoolManager.instance.CreatePool(dashEffect, 2);
			PoolManager.instance.CreatePool(jumpEffect, 2);

			// if it's the player, make this instance currently playable
			if (transform.CompareTag("Player"))
				isCurrentlyPlayable = true;

			m_extraJumps = extraJumpCount;
			m_dashTime = startDashTime;
			m_dashCooldown = dashCooldown;
			m_extraJumpForce = jumpForce * 0.7f;

			m_rb = GetComponent<Rigidbody2D>();
            m_rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            m_dustParticle = GetComponentInChildren<ParticleSystem>();
            _cameraFollowObject = _cameraFollowGO.GetComponent<CameraFollowObject>();
        }

		private void FixedUpdate()
		{
			// check if grounded
			isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);
			var position = transform.position;
			// check if on wall
			m_onWall = Physics2D.OverlapCircle((Vector2)position + grabRightOffset, grabCheckRadius, whatIsGround)
			          || Physics2D.OverlapCircle((Vector2)position + grabLeftOffset, grabCheckRadius, whatIsGround);
			m_onRightWall = Physics2D.OverlapCircle((Vector2)position + grabRightOffset, grabCheckRadius, whatIsGround);
			m_onLeftWall = Physics2D.OverlapCircle((Vector2)position + grabLeftOffset, grabCheckRadius, whatIsGround);

			// calculate player and wall sides as integers
			CalculateSides();

			if((m_wallGrabbing || isGrounded) && m_wallJumping)
			{
				m_wallJumping = false;
			}
			// if this instance is currently playable
			if (isCurrentlyPlayable)
			{
				// horizontal movement
				if(m_wallJumping)
				{
					m_rb.velocity = Vector2.Lerp(m_rb.velocity, (new Vector2(moveInput * speed, m_rb.velocity.y)), 1.5f * Time.fixedDeltaTime);
				}
				else
				{
					if(canMove && !m_wallGrabbing)
						m_rb.velocity = new Vector2(moveInput * speed, m_rb.velocity.y);
					else if(!canMove)
						m_rb.velocity = new Vector2(0f, m_rb.velocity.y);
				}
				// better jump physics
				if (m_rb.velocity.y < 0f)
				{
					m_rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
				}

				// Flipping
				if (canFlip)
				{
					if (!m_facingRight && moveInput > 0f)
						Flip();
					else if (m_facingRight && moveInput < 0f)
						Flip();
				}

				// Dashing logic
				if (canDash && isDashing)
				{
					if (m_dashTime <= 0f)
					{
						isDashing = false;
						m_dashCooldown = dashCooldown;
						m_dashTime = startDashTime;
						m_rb.velocity = Vector2.zero;
					}
					else
					{
						m_dashTime -= Time.deltaTime;
						if(m_facingRight)
							m_rb.velocity = Vector2.right * dashSpeed;
						else
							m_rb.velocity = Vector2.left * dashSpeed;
					}
				}

                // wall grab
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
                        // Still in coyote time - continue sliding
                        m_wallSlideCoyoteTimer -= Time.fixedDeltaTime;
                        actuallyWallGrabbing = true;
                        m_wallGrabbing = true;
                        m_rb.velocity = new Vector2(0f, -slideSpeed); // No horizontal movement during coyote time
                    }
                    else
                    {
                        // Coyote time expired - start falling
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

				// enable/disable dust particles
				float playerVelocityMag = m_rb.velocity.sqrMagnitude;
				if(m_dustParticle.isPlaying && playerVelocityMag == 0f)
				{
					m_dustParticle.Stop();
				}
				else if(!m_dustParticle.isPlaying && playerVelocityMag > 0f)
				{
					m_dustParticle.Play();
				}

			}
		}

		private void Update()
		{
            // horizontal input
            moveInput = moveAction.ReadValue<Vector2>().x;

            if (isGrounded)
			{
				m_extraJumps = extraJumpCount;
			}

			// grounded remember offset (for more responsive jump)
			m_groundedRemember -= Time.deltaTime;
			if (isGrounded)
				m_groundedRemember = m_groundedRememberTime;

			if (!isCurrentlyPlayable) return;
			// if not currently dashing and hasn't already dashed in air once
			if (canDash && !isDashing && !m_hasDashedInAir && m_dashCooldown <= 0f)
			{
				// dash input (left shift)
				if (dashAction.triggered)
				{
					isDashing = true;
                    // dash effect
                    PoolManager.instance.ReuseObject(dashEffect, transform.position, Quaternion.identity);
					// if player in air while dashing
					if(!isGrounded)
					{
						m_hasDashedInAir = true;
					}
					// dash logic is in FixedUpdate
				}
			}
			m_dashCooldown -= Time.deltaTime;
			
			// if has dashed in air once but now grounded
			if (m_hasDashedInAir && isGrounded)
				m_hasDashedInAir = false;

			// Jumping
			if (canJump)
			{
				if (jumpAction.triggered && m_extraJumps > 0 && !isGrounded && !m_wallGrabbing)
				{
					m_rb.velocity = new Vector2(m_rb.velocity.x, m_extraJumpForce); ;
					m_extraJumps--;
					// jumpEffect
					PoolManager.instance.ReuseObject(jumpEffect, groundCheck.position, Quaternion.identity);
				}
				else if (jumpAction.triggered && (isGrounded || m_groundedRemember > 0f))   // normal single jumping
				{
					m_rb.velocity = new Vector2(m_rb.velocity.x, jumpForce);
					// jumpEffect
					PoolManager.instance.ReuseObject(jumpEffect, groundCheck.position, Quaternion.identity);
				}
				else if (jumpAction.triggered && m_wallGrabbing && moveInput != m_onWallSide)       // wall jumping off the wall
				{
					m_wallGrabbing = false;
					m_wallJumping = true;
					Debug.Log("Wall jumped");
					if (m_playerSide == m_onWallSide)
						Flip();
					m_rb.AddForce(new Vector2(-m_onWallSide * wallJumpForce.x, wallJumpForce.y), ForceMode2D.Impulse);
				}
				else if (jumpAction.triggered && m_wallGrabbing && moveInput != 0 && (moveInput == m_onWallSide))      // wall climbing jump
				{
					m_wallGrabbing = false;
					m_wallJumping = true;
					Debug.Log("Wall climbed");
					if (m_playerSide == m_onWallSide)
						Flip();
					m_rb.AddForce(new Vector2(-m_onWallSide * wallClimbForce.x, wallClimbForce.y), ForceMode2D.Impulse);
				}
			}

		}

			void Flip()
        {
            m_facingRight = !m_facingRight;

            // Flip using scale instead of rotation
            Vector3 newScale = transform.localScale;
            newScale.x *= -1;
            transform.localScale = newScale;

            // Trigger camera animation
            _cameraFollowObject.CallTurn();
        }

        void CalculateSides()
		{
			if (m_onRightWall)
				m_onWallSide = 1;
			else if (m_onLeftWall)
				m_onWallSide = -1;
			else
				m_onWallSide = 0;

			if (m_facingRight)
				m_playerSide = 1;
			else
				m_playerSide = -1;
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
			Gizmos.DrawWireSphere((Vector2)transform.position + grabRightOffset, grabCheckRadius);
			Gizmos.DrawWireSphere((Vector2)transform.position + grabLeftOffset, grabCheckRadius);
		}

        /// <summary>
        /// Disables player movement while maintaining gravity and falling
        /// </summary>
        public void DisableMovement()
        {
            canMove = false;
            canFlip = false;
            canJump = false;
            canDash = false;

            // Preserve only vertical velocity (falling/jumping)
            m_rb.velocity = new Vector2(0f, m_rb.velocity.y);

            // Cancel any active movement abilities
            if (isDashing)
            {
                isDashing = false;
                m_dashTime = startDashTime;
            }
            m_wallGrabbing = false;
            actuallyWallGrabbing = false;
        }

        /// <summary>
        /// Enables player movement
        /// </summary>
        public void EnableMovement()
        {
            canMove = true;
            canFlip = true;
            canJump = true;
            canDash = true;
        }

        /// <summary>
        /// Fully freezes the player in place (including vertical movement)
        /// </summary>
        public void FreezePlayer()
        {
            canMove = false;
            m_rb.velocity = Vector2.zero;
            isDashing = false;
            m_wallGrabbing = false;
            actuallyWallGrabbing = false;
        }

        /// <summary>
        /// Unfreezes the player and enables movement
        /// </summary>
        public void UnfreezePlayer()
        {
            canMove = true;
        }
    }
}
