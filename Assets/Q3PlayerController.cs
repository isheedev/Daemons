using UnityEngine;

namespace Q3Movement
{
    [RequireComponent(typeof(CharacterController))]
    public class Q3PlayerController : MonoBehaviour
    {
        [System.Serializable]
        public class MovementSettings
        {
            public float MaxSpeed;
            public float Acceleration;
            public float Deceleration;

            public MovementSettings(float maxSpeed, float accel, float decel)
            {
                MaxSpeed = maxSpeed;
                Acceleration = accel;
                Deceleration = decel;
            }
        }

        [Header("Aiming")]
        [SerializeField] private Camera m_Camera;
        [SerializeField] private MouseGander m_MouseLook = new MouseGander();

        [Header("Movement")]
        [SerializeField] private float m_Friction = 6;
        [SerializeField] private float m_Gravity = 20;
        [SerializeField] private float m_JumpForce = 8;
        [SerializeField] private bool m_AutoBunnyHop = false;
        [SerializeField] private float m_AirControl = 0.3f;
        [SerializeField] private MovementSettings m_GroundSettings = new MovementSettings(7, 14, 10);
        [SerializeField] private MovementSettings m_AirSettings = new MovementSettings(7, 2, 2);
        [SerializeField] private MovementSettings m_StrafeSettings = new MovementSettings(1, 50, 50);

        [Header("Crouch & Slide")]
        [SerializeField] private float m_CrouchHeightMultiplier = 0.5f;
        [SerializeField] private float m_SlideHeightMultiplier = 0.35f;
        [SerializeField] private float m_CrouchSpeedMultiplier = 0.5f;
        [SerializeField] private float m_SlideSpeed = 12f;
        [SerializeField] private float m_SlideDuration = 0.8f;
        [SerializeField] private float m_SlideSpeedThreshold = 5f;
        [SerializeField] private float m_CrouchTransitionSpeed = 10f;
        [SerializeField] private float m_StandUpCheckDistance = 0.2f;
        
        private CharacterController m_Character;
        private Vector3 m_PlayerVelocity = Vector3.zero;
        private bool m_JumpQueued = false;
        private Vector3 m_MoveInput;
        private Transform m_Tran;
        private Transform m_CamTran;
        
        private float m_DefaultHeight;
        private Vector3 m_DefaultCenter;
        private float m_DefaultCameraY;
        
        private bool m_IsCrouching = false;
        private bool m_IsSliding = false;
        private float m_SlideTimer = 0f;
        private Vector3 m_SlideDirection = Vector3.zero;
        private bool m_CrouchHeld = false;
        private bool m_CrouchPressed = false;

        private void Start()
        {
            m_Tran = transform;
            m_Character = GetComponent<CharacterController>();

            if (!m_Camera)
                m_Camera = Camera.main;

            m_CamTran = m_Camera.transform;
            m_MouseLook.Init(m_Tran, m_CamTran);

            m_DefaultHeight = m_Character.height;
            m_DefaultCenter = m_Character.center;
            m_DefaultCameraY = m_CamTran.localPosition.y;
        }

        private void Update()
        {
            m_MouseLook.LookRotation(m_Tran, m_CamTran);
            m_MoveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
            
            QueueJump();
            HandleCrouchInput();
            HandleCrouchAndSlide();

            if (m_Character.isGrounded)
                GroundMove();
            else
                AirMove();

            m_Character.Move(m_PlayerVelocity * Time.deltaTime);
        }

        private void HandleCrouchInput()
        {
            m_CrouchPressed = Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C);
            m_CrouchHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
        }

        private void HandleCrouchAndSlide()
        {
            // Handle slide timer
            if (m_IsSliding)
            {
                m_SlideTimer -= Time.deltaTime;
                if (m_SlideTimer <= 0f || m_PlayerVelocity.magnitude < 1f)
                {
                    m_IsSliding = false;
                }
            }

            // Toggle crouch or initiate slide
            if (m_CrouchPressed && m_Character.isGrounded)
            {
                Vector3 horizontalVelocity = new Vector3(m_PlayerVelocity.x, 0, m_PlayerVelocity.z);
                
                // Start slide if moving fast enough
                if (!m_IsCrouching && horizontalVelocity.magnitude >= m_SlideSpeedThreshold)
                {
                    m_IsSliding = true;
                    m_IsCrouching = true;
                    m_SlideTimer = m_SlideDuration;
                    m_SlideDirection = horizontalVelocity.normalized;
                }
                else if (!m_IsSliding)
                {
                    // Toggle crouch
                    m_IsCrouching = !m_IsCrouching;
                }
            }

            // Try to stand up when crouch is released
            if (!m_CrouchHeld && !m_IsSliding && m_IsCrouching)
            {
                if (CanStandUp())
                {
                    m_IsCrouching = false;
                }
            }

            // Update height and camera
            float targetHeight;
            float targetCameraY;
            
            if (m_IsSliding)
            {
                targetHeight = m_DefaultHeight * m_SlideHeightMultiplier;
                targetCameraY = m_DefaultCameraY * m_SlideHeightMultiplier;
            }
            else if (m_IsCrouching)
            {
                targetHeight = m_DefaultHeight * m_CrouchHeightMultiplier;
                targetCameraY = m_DefaultCameraY * m_CrouchHeightMultiplier;
            }
            else
            {
                targetHeight = m_DefaultHeight;
                targetCameraY = m_DefaultCameraY;
            }

            // Smoothly transition height
            m_Character.height = Mathf.Lerp(m_Character.height, targetHeight, Time.deltaTime * m_CrouchTransitionSpeed);
            m_Character.center = new Vector3(m_DefaultCenter.x, m_Character.height / 2f, m_DefaultCenter.z);

            // Smoothly transition camera
            Vector3 cameraPos = m_CamTran.localPosition;
            cameraPos.y = Mathf.Lerp(cameraPos.y, targetCameraY, Time.deltaTime * m_CrouchTransitionSpeed);
            m_CamTran.localPosition = cameraPos;
        }

        private bool CanStandUp()
        {
            float heightDifference = m_DefaultHeight - m_Character.height;
            Vector3 startPos = m_Tran.position + Vector3.up * m_Character.height;
            float radius = m_Character.radius * 0.9f;
            
            return !Physics.SphereCast(startPos, radius, Vector3.up, out RaycastHit hit, heightDifference - m_StandUpCheckDistance);
        }

        private void QueueJump()
        {
            if (m_AutoBunnyHop)
            {
                m_JumpQueued = Input.GetButton("Jump");
                return;
            }

            if (Input.GetButtonDown("Jump") && !m_JumpQueued)
                m_JumpQueued = true;

            if (Input.GetButtonUp("Jump"))
                m_JumpQueued = false;
        }

        private void GroundMove()
        {
            if (!m_JumpQueued)
            {
                // Apply slide momentum
                if (m_IsSliding)
                {
                    float slideInfluence = m_SlideTimer / m_SlideDuration;
                    Vector3 slideVelocity = m_SlideDirection * m_SlideSpeed * slideInfluence;
                    m_PlayerVelocity.x = Mathf.Lerp(m_PlayerVelocity.x, slideVelocity.x, Time.deltaTime * 5f);
                    m_PlayerVelocity.z = Mathf.Lerp(m_PlayerVelocity.z, slideVelocity.z, Time.deltaTime * 5f);
                }
                else
                {
                    ApplyFriction(1.0f);
                }
                
                m_PlayerVelocity.y = -2f;
            }
            else
            {
                ApplyFriction(0f);
                m_PlayerVelocity.y = m_JumpForce;
                m_JumpQueued = false;
                
                // Cancel slide on jump
                if (m_IsSliding)
                {
                    m_IsSliding = false;
                    m_IsCrouching = false;
                }
            }

            // Don't allow movement input during slide
            if (!m_IsSliding)
            {
                var wishdir = m_Tran.TransformDirection(new Vector3(m_MoveInput.x, 0, m_MoveInput.z)).normalized;
                float wishspeed = wishdir.magnitude * m_GroundSettings.MaxSpeed;
                
                // Reduce speed when crouching
                if (m_IsCrouching)
                {
                    wishspeed *= m_CrouchSpeedMultiplier;
                }

                Accelerate(wishdir, wishspeed, m_GroundSettings.Acceleration);
            }
        }

        private void AirMove()
        {
            // Cancel slide in air
            if (m_IsSliding)
            {
                m_IsSliding = false;
            }

            var wishdir = m_Tran.TransformDirection(new Vector3(m_MoveInput.x, 0, m_MoveInput.z));
            float wishspeed = wishdir.magnitude * m_AirSettings.MaxSpeed;
            wishdir.Normalize();

            float accel = (Vector3.Dot(m_PlayerVelocity, wishdir) < 0) ? m_AirSettings.Deceleration : m_AirSettings.Acceleration;

            if (m_MoveInput.z == 0 && m_MoveInput.x != 0)
            {
                if (wishspeed > m_StrafeSettings.MaxSpeed)
                    wishspeed = m_StrafeSettings.MaxSpeed;
                accel = m_StrafeSettings.Acceleration;
            }

            Accelerate(wishdir, wishspeed, accel);
            if (m_AirControl > 0) AirControl(wishdir, wishspeed);

            m_PlayerVelocity.y -= m_Gravity * Time.deltaTime;
        }

        private void AirControl(Vector3 targetDir, float targetSpeed)
        {
            if (Mathf.Abs(m_MoveInput.z) < 0.001 || Mathf.Abs(targetSpeed) < 0.001) return;

            float zSpeed = m_PlayerVelocity.y;
            m_PlayerVelocity.y = 0;
            float speed = m_PlayerVelocity.magnitude;
            m_PlayerVelocity.Normalize();

            float dot = Vector3.Dot(m_PlayerVelocity, targetDir);
            float k = 32 * m_AirControl * dot * dot * Time.deltaTime;

            if (dot > 0)
            {
                m_PlayerVelocity = (m_PlayerVelocity * speed + targetDir * k).normalized;
            }

            m_PlayerVelocity *= speed;
            m_PlayerVelocity.y = zSpeed;
        }

        private void ApplyFriction(float t)
        {
            Vector3 vec = m_PlayerVelocity;
            vec.y = 0;
            float speed = vec.magnitude;
            float drop = 0;

            if (speed > 0)
            {
                float control = speed < m_GroundSettings.Deceleration ? m_GroundSettings.Deceleration : speed;
                drop = control * m_Friction * Time.deltaTime * t;
            }

            float newSpeed = Mathf.Max(0, speed - drop);
            if (speed > 0) newSpeed /= speed;

            m_PlayerVelocity.x *= newSpeed;
            m_PlayerVelocity.z *= newSpeed;
        }

        private void Accelerate(Vector3 targetDir, float targetSpeed, float accel)
        {
            float currentspeed = Vector3.Dot(m_PlayerVelocity, targetDir);
            float addspeed = targetSpeed - currentspeed;
            if (addspeed <= 0) return;

            float accelspeed = accel * Time.deltaTime * targetSpeed;
            if (accelspeed > addspeed) accelspeed = addspeed;

            m_PlayerVelocity.x += accelspeed * targetDir.x;
            m_PlayerVelocity.z += accelspeed * targetDir.z;
        }
    }
}