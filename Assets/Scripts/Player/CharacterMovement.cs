using UnityEngine;
using HideAndSeek.Data;

namespace HideAndSeek.Player
{
    /// <summary>
    /// Handles character movement mechanics and physics
    /// Provides smooth movement control and animation integration
    /// </summary>
    public class CharacterMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 720f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float deceleration = 10f;
        
        [Header("Ground Detection")]
        [SerializeField] private float groundCheckDistance = 0.1f;
        [SerializeField] private LayerMask groundLayer = 1;
        
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] footstepSounds;
        [SerializeField] private float footstepInterval = 0.5f;
        
        // Components
        private CharacterController characterController;
        private Animator animator;
        
        // Movement state
        private Vector3 currentVelocity;
        private Vector3 targetVelocity;
        private bool isGrounded;
        private float lastFootstepTime;
        
        // Properties
        public Vector3 Velocity => currentVelocity;
        public bool IsGrounded => isGrounded;
        public bool IsMoving => currentVelocity.magnitude > 0.1f;
        public float Speed => currentVelocity.magnitude;
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            LoadSettingsFromGameManager();
        }
        
        private void Update()
        {
            UpdateGroundCheck();
            UpdateMovement();
            UpdateAudio();
            UpdateAnimations();
        }
        
        private void InitializeComponents()
        {
            characterController = GetComponent<CharacterController>();
            if (characterController == null)
            {
                characterController = gameObject.AddComponent<CharacterController>();
                Debug.LogWarning("CharacterController was missing and has been added automatically.");
            }
            
            animator = GetComponentInChildren<Animator>();
            
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
                
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // 3D sound
            }
        }
        
        private void LoadSettingsFromGameManager()
        {
            var gameSettings = Core.GameManager.Instance?.GetGameSettings();
            if (gameSettings != null)
            {
                moveSpeed = gameSettings.playerMoveSpeed;
                rotationSpeed = gameSettings.playerRotationSpeed;
            }
        }
        
        private void UpdateGroundCheck()
        {
            // Check if character controller is grounded
            isGrounded = characterController.isGrounded;
            
            // Additional ground check using raycast for more precise detection
            if (!isGrounded)
            {
                Ray ray = new Ray(transform.position + Vector3.up * 0.1f, Vector3.down);
                isGrounded = Physics.Raycast(ray, groundCheckDistance + 0.1f, groundLayer);
            }
        }
        
        private void UpdateMovement()
        {
            // Smoothly interpolate velocity
            currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, 
                (targetVelocity.magnitude > currentVelocity.magnitude ? acceleration : deceleration) * Time.deltaTime);
            
            // Apply gravity if not grounded
            if (!isGrounded)
            {
                currentVelocity.y -= 9.81f * Time.deltaTime;
            }
            else if (currentVelocity.y < 0)
            {
                currentVelocity.y = 0;
            }
            
            // Move the character
            if (characterController.enabled)
            {
                characterController.Move(currentVelocity * Time.deltaTime);
            }
        }
        
        private void UpdateAudio()
        {
            // Play footstep sounds when moving on ground
            if (IsMoving && isGrounded && Time.time >= lastFootstepTime + footstepInterval)
            {
                PlayFootstepSound();
                lastFootstepTime = Time.time;
            }
        }
        
        private void UpdateAnimations()
        {
            if (animator == null) return;
            
            // Update animator parameters for movement
            animator.SetFloat("Speed", Speed);
            animator.SetFloat("VelocityX", currentVelocity.x);
            animator.SetFloat("VelocityZ", currentVelocity.z);
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsMoving", IsMoving);
        }
        
        /// <summary>
        /// Set the target movement direction and speed
        /// </summary>
        /// <param name="direction">Normalized movement direction</param>
        /// <param name="speedMultiplier">Speed multiplier (0-1)</param>
        public void SetMovementInput(Vector3 direction, float speedMultiplier = 1f)
        {
            direction.y = 0; // Remove any Y component
            targetVelocity = direction.normalized * moveSpeed * Mathf.Clamp01(speedMultiplier);
        }
        
        /// <summary>
        /// Set movement directly with a velocity vector
        /// </summary>
        /// <param name="velocity">Target velocity</param>
        public void SetVelocity(Vector3 velocity)
        {
            velocity.y = currentVelocity.y; // Preserve Y velocity for gravity
            targetVelocity = velocity;
        }
        
        /// <summary>
        /// Rotate the character to face a direction
        /// </summary>
        /// <param name="direction">Direction to face</param>
        /// <param name="immediate">If true, rotate immediately without interpolation</param>
        public void RotateTowardsDirection(Vector3 direction, bool immediate = false)
        {
            if (direction.magnitude < 0.1f) return;
            
            direction.y = 0; // Remove Y component
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            
            if (immediate)
            {
                transform.rotation = targetRotation;
            }
            else
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, 
                    targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        
        /// <summary>
        /// Stop all movement immediately
        /// </summary>
        public void StopMovement()
        {
            targetVelocity = Vector3.zero;
            currentVelocity = new Vector3(0, currentVelocity.y, 0); // Preserve Y for gravity
        }
        
        /// <summary>
        /// Add an impulse force to the character
        /// </summary>
        /// <param name="force">Force to apply</param>
        public void AddImpulse(Vector3 force)
        {
            currentVelocity += force;
        }
        
        /// <summary>
        /// Teleport the character to a position
        /// </summary>
        /// <param name="position">Target position</param>
        public void Teleport(Vector3 position)
        {
            characterController.enabled = false;
            transform.position = position;
            characterController.enabled = true;
            currentVelocity = Vector3.zero;
            targetVelocity = Vector3.zero;
        }
        
        private void PlayFootstepSound()
        {
            if (footstepSounds.Length == 0 || audioSource == null) return;
            
            // Play a random footstep sound
            AudioClip footstep = footstepSounds[Random.Range(0, footstepSounds.Length)];
            audioSource.PlayOneShot(footstep, 0.7f);
        }
        
        /// <summary>
        /// Update movement settings from game settings
        /// </summary>
        public void UpdateMovementSettings(GameSettings gameSettings)
        {
            if (gameSettings == null) return;
            
            moveSpeed = gameSettings.playerMoveSpeed;
            rotationSpeed = gameSettings.playerRotationSpeed;
        }
        
        // Debug methods
        private void OnDrawGizmosSelected()
        {
            // Draw ground check ray
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Vector3 rayStart = transform.position + Vector3.up * 0.1f;
            Gizmos.DrawRay(rayStart, Vector3.down * (groundCheckDistance + 0.1f));
            
            // Draw velocity vector
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, currentVelocity);
            
            // Draw target velocity
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, targetVelocity);
        }
        
        private void OnValidate()
        {
            // Ensure positive values
            moveSpeed = Mathf.Max(0.1f, moveSpeed);
            rotationSpeed = Mathf.Max(1f, rotationSpeed);
            acceleration = Mathf.Max(0.1f, acceleration);
            deceleration = Mathf.Max(0.1f, deceleration);
            groundCheckDistance = Mathf.Max(0.01f, groundCheckDistance);
            footstepInterval = Mathf.Max(0.1f, footstepInterval);
        }
    }
}