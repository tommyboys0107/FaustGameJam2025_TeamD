using UnityEngine;
using UnityEngine.InputSystem;
using HideAndSeek.Core;
using HideAndSeek.Data;

namespace HideAndSeek.Player
{
    /// <summary>
    /// Base controller for player characters (Killer and Police)
    /// Handles movement, input, actions, and state management
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 720f;
        
        [Header("Action Settings")]
        [SerializeField] private float interactionCooldown = 2f; // Will be overridden by role
        [SerializeField] private float disguiseCooldown = 5f;
        
        [Header("References")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Animator animator;
        
        // Player role and state
        private GameManager.PlayerRole playerRole;
        private PlayerState currentState = PlayerState.Idle;
        

        private Vector2 movementInput;
        private Vector3 velocity;
        private bool isGrounded;
        
        // Action cooldowns
        private float lastInteractionTime;
        private float lastDisguiseTime;
        
        // Components
        private CharacterMovement movement;
        private ActionSystem actionSystem;
        private DisguiseSystem disguiseSystem;
        
        // Events
        public System.Action<GameManager.PlayerRole> OnPlayerRoleChanged;
        public System.Action<PlayerState> OnPlayerStateChanged;
        
        public enum PlayerState
        {
            Idle,
            Moving,
            Dancing,
            Interacting,
            Disguising,
            Stunned
        }
        
        // Properties
        public GameManager.PlayerRole PlayerRole 
        { 
            get => playerRole; 
            private set 
            { 
                playerRole = value; 
                OnPlayerRoleChanged?.Invoke(value);
                UpdateCooldownsForRole();
            } 
        }
        
        public PlayerState CurrentState 
        { 
            get => currentState; 
            private set 
            { 
                currentState = value; 
                OnPlayerStateChanged?.Invoke(value);
            } 
        }
        

        
        // Input compatibility methods for traditional input
        public void SetMovementInput(Vector2 input) { movementInput = input; }
        public Vector2 GetMovementInput() { return movementInput; }
        
        // Public action methods for traditional input
        public void TriggerInteract() 
        {
            Debug.Log($"[Interact] {CanInteract}, Remain={InteractionCooldownRemaining}, currentState={currentState}");
            if (CanInteract && currentState != PlayerState.Stunned)
                PerformInteraction();
        }
        
        public void TriggerDisguise() 
        { 
            if (CanDisguise && currentState != PlayerState.Stunned)
                PerformDisguise();
        }
        
        public void TriggerDance() 
        { 
            if (currentState != PlayerState.Stunned)
                PerformDance();
        }
        public bool CanInteract => Time.time >= lastInteractionTime + interactionCooldown;
        public bool CanDisguise => Time.time >= lastDisguiseTime + disguiseCooldown;
        public float InteractionCooldownRemaining => Mathf.Max(0, (lastInteractionTime + interactionCooldown) - Time.time);
        public float DisguiseCooldownRemaining => Mathf.Max(0, (lastDisguiseTime + disguiseCooldown) - Time.time);
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            InitializeFromGameSettings();
        }
        
        private void Update()
        {
            UpdateGroundCheck();
            HandleMovement();
            UpdateAnimations();
        }
        
        private void InitializeComponents()
        {
            // Get or add required components
            if (characterController == null)
                characterController = GetComponent<CharacterController>();
            
            if (characterController == null)
                characterController = gameObject.AddComponent<CharacterController>();
            
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
                
            // Initialize subsystems
            movement = GetComponent<CharacterMovement>();
            if (movement == null)
                movement = gameObject.AddComponent<CharacterMovement>();
                
            actionSystem = GetComponent<ActionSystem>();
            if (actionSystem == null)
                actionSystem = gameObject.AddComponent<ActionSystem>();
                
            disguiseSystem = GetComponent<DisguiseSystem>();
            if (disguiseSystem == null)
                disguiseSystem = gameObject.AddComponent<DisguiseSystem>();
        }
        
        private void InitializeFromGameSettings()
        {
            var gameSettings = GameManager.Instance.GetGameSettings();
            if (gameSettings != null)
            {
                moveSpeed = gameSettings.playerMoveSpeed;
                rotationSpeed = gameSettings.playerRotationSpeed;
                disguiseCooldown = gameSettings.disguiseCooldown;
            }
        }
        
        private void UpdateCooldownsForRole()
        {
            var gameSettings = GameManager.Instance.GetGameSettings();
            if (gameSettings != null)
            {
                interactionCooldown = gameSettings.GetActionCooldown(playerRole);
            }
            else
            {
                // Fallback values
                interactionCooldown = playerRole == GameManager.PlayerRole.Killer ? 2f : 5f;
            }
        }
        
        private void UpdateGroundCheck()
        {
            isGrounded = characterController.isGrounded;
        }
        
        private void HandleMovement()
        {
            if (currentState == PlayerState.Stunned) return;
            
            // Convert input to world space movement
            Vector3 moveDirection = new Vector3(movementInput.x, 0, movementInput.y);
            
            // Update state based on movement
            if (moveDirection.magnitude > 0.1f)
            {
                if (currentState == PlayerState.Idle)
                    CurrentState = PlayerState.Moving;
                    
                // Rotate to face movement direction
                if (moveDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }
            else if (currentState == PlayerState.Moving)
            {
                CurrentState = PlayerState.Idle;
            }
            
            // Apply movement
            velocity = moveDirection * moveSpeed;
            
            // Apply gravity
            if (!isGrounded)
                velocity.y -= 9.81f * Time.deltaTime;
            else
                velocity.y = 0;
                
            characterController.Move(velocity * Time.deltaTime);
        }
        
        private void UpdateAnimations()
        {
            if (animator == null) return;
            
            // Update animator parameters
            animator.SetFloat("Speed", velocity.magnitude);
        }
        
        // Input System callbacks (to be connected by Input System)
        public void OnMove(InputAction.CallbackContext context)
        {
            movementInput = context.ReadValue<Vector2>();
        }
        
        public void OnInteract(InputAction.CallbackContext context)
        {
            if (context.performed && CanInteract && currentState != PlayerState.Stunned)
            {
                PerformInteraction();
            }
        }
        
        public void OnDisguise(InputAction.CallbackContext context)
        {
            if (context.performed && CanDisguise && currentState != PlayerState.Stunned)
            {
                PerformDisguise();
            }
        }
        
        public void OnDance(InputAction.CallbackContext context)
        {
            if (context.performed && currentState != PlayerState.Stunned)
            {
                PerformDance();
            }
        }
        
        // Action methods
        private void PerformInteraction()
        {
            CurrentState = PlayerState.Interacting;
            lastInteractionTime = Time.time;
            
            if (actionSystem != null)
            {
                if (playerRole == GameManager.PlayerRole.Killer)
                    actionSystem.PerformKill();
                else
                    actionSystem.PerformArrest();
            }
            
            // Return to idle after action completes
            Invoke(nameof(ReturnToIdle), 1f);
        }
        
        private void PerformDisguise()
        {
            CurrentState = PlayerState.Disguising;
            lastDisguiseTime = Time.time;
            
            if (disguiseSystem != null)
            {
                disguiseSystem.ChangeAppearance();
            }
            
            // Return to idle after disguise completes
            Invoke(nameof(ReturnToIdle), 2f);
        }
        
        private void PerformDance()
        {
            CurrentState = PlayerState.Dancing;
            
            if (actionSystem != null)
            {
                int danceType = Random.Range(0, 4); // 4 different dance types based on input
                actionSystem.PerformDance(danceType);
            }
            
            // Return to idle after dance completes
            Invoke(nameof(ReturnToIdle), 3f);
        }
        
        private void ReturnToIdle()
        {
            CurrentState = PlayerState.Idle;
        }
        
        // Public methods for external control
        public void SetPlayerRole(GameManager.PlayerRole role)
        {
            PlayerRole = role;
            Debug.Log($"Player role set to: {role}");
        }
        
        public void SetStunned(bool stunned, float duration = 0f)
        {
            if (stunned)
            {
                CurrentState = PlayerState.Stunned;
                if (duration > 0)
                    Invoke(nameof(ReturnToIdle), duration);
            }
            else
            {
                ReturnToIdle();
            }
        }
        
        public void ResetPlayer()
        {
            CurrentState = PlayerState.Idle;
            movementInput = Vector2.zero;
            velocity = Vector3.zero;
            lastInteractionTime = 0;
            lastDisguiseTime = 0;
        }
        
        private void OnValidate()
        {
            // Ensure positive values
            moveSpeed = Mathf.Max(0.1f, moveSpeed);
            rotationSpeed = Mathf.Max(1f, rotationSpeed);
            interactionCooldown = Mathf.Max(0.1f, interactionCooldown);
            disguiseCooldown = Mathf.Max(0.1f, disguiseCooldown);
        }
    }
}