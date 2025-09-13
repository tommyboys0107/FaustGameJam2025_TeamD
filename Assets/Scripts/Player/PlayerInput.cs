using UnityEngine;
using UnityEngine.InputSystem;

namespace HideAndSeek.Player
{
    /// <summary>
    /// Handles Unity Input System integration for player controls
    /// Manages input actions and connects them to PlayerController
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInput : MonoBehaviour
    {
        [Header("Input Actions")]
        [SerializeField] private InputActionAsset inputActions;
        
        // Input action references
        private InputAction moveAction;
        private InputAction interactAction;
        private InputAction disguiseAction;
        private InputAction danceAction;
        
        // Component references
        private PlayerController playerController;
        private CharacterMovement characterMovement;
        
        // Input state
        private Vector2 movementInput;
        private bool inputEnabled = true;
        
        public bool InputEnabled 
        { 
            get => inputEnabled; 
            set 
            { 
                inputEnabled = value; 
                if (inputEnabled) 
                    EnableInput(); 
                else 
                    DisableInput(); 
            } 
        }
        
        private void Awake()
        {
            InitializeComponents();
            SetupInputActions();
        }
        
        private void OnEnable()
        {
            EnableInput();
        }
        
        private void OnDisable()
        {
            DisableInput();
        }
        
        private void Update()
        {
            if (inputEnabled)
            {
                HandleMovementInput();
            }
        }
        
        private void InitializeComponents()
        {
            playerController = GetComponent<PlayerController>();
            characterMovement = GetComponent<CharacterMovement>();
            
            if (playerController == null)
            {
                Debug.LogError("PlayerInput requires a PlayerController component!");
            }
        }
        
        private void SetupInputActions()
        {
            if (inputActions == null)
            {
                Debug.LogError("Input Actions asset is not assigned!");
                return;
            }
            
            // Get input actions from the action map
            var playerMap = inputActions.FindActionMap("Player");
            if (playerMap == null)
            {
                Debug.LogError("Player action map not found in Input Actions asset!");
                return;
            }
            
            moveAction = playerMap.FindAction("Move");
            interactAction = playerMap.FindAction("Interact");
            disguiseAction = playerMap.FindAction("Disguise");
            danceAction = playerMap.FindAction("Dance");
            
            // Subscribe to input events
            if (interactAction != null)
                interactAction.performed += OnInteract;
                
            if (disguiseAction != null)
                disguiseAction.performed += OnDisguise;
                
            if (danceAction != null)
                danceAction.performed += OnDance;
        }
        
        private void EnableInput()
        {
            if (inputActions != null)
            {
                inputActions.Enable();
            }
        }
        
        private void DisableInput()
        {
            if (inputActions != null)
            {
                inputActions.Disable();
            }
        }
        
        private void HandleMovementInput()
        {
            if (moveAction == null || characterMovement == null) return;
            
            // Read movement input
            movementInput = moveAction.ReadValue<Vector2>();
            
            // Convert to 3D movement direction
            Vector3 moveDirection = new Vector3(movementInput.x, 0, movementInput.y);
            
            // Apply movement to character
            characterMovement.SetMovementInput(moveDirection);
            
            // Handle rotation
            if (moveDirection.magnitude > 0.1f)
            {
                characterMovement.RotateTowardsDirection(moveDirection);
            }
        }
        
        #region Input Action Callbacks
        
        private void OnInteract(InputAction.CallbackContext context)
        {
            if (!inputEnabled || playerController == null) return;
            
            playerController.OnInteract(context);
        }
        
        private void OnDisguise(InputAction.CallbackContext context)
        {
            if (!inputEnabled || playerController == null) return;
            
            playerController.OnDisguise(context);
        }
        
        private void OnDance(InputAction.CallbackContext context)
        {
            if (!inputEnabled || playerController == null) return;
            
            // For dance, we can modify behavior based on movement input
            Vector2 danceInput = movementInput;
            
            // Pass dance direction to controller (different dance based on direction)
            if (danceInput.magnitude > 0.1f)
            {
                // Dance in the direction of movement input
                int danceType = GetDanceTypeFromDirection(danceInput);
                playerController.OnDance(context);
            }
            else
            {
                // Default dance
                playerController.OnDance(context);
            }
        }
        
        #endregion
        
        private int GetDanceTypeFromDirection(Vector2 direction)
        {
            // Convert movement direction to dance type (0-3)
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360;
            
            // Divide into 4 quadrants for different dance types
            if (angle >= 315 || angle < 45) return 0;      // Right
            else if (angle >= 45 && angle < 135) return 1;  // Up
            else if (angle >= 135 && angle < 225) return 2; // Left
            else return 3;                                   // Down
        }
        
        /// <summary>
        /// Set the input actions asset
        /// </summary>
        /// <param name="actions">Input actions asset to use</param>
        public void SetInputActions(InputActionAsset actions)
        {
            if (inputActions != null)
            {
                DisableInput();
            }
            
            inputActions = actions;
            SetupInputActions();
            
            if (gameObject.activeInHierarchy)
            {
                EnableInput();
            }
        }
        
        /// <summary>
        /// Temporarily disable input for a duration
        /// </summary>
        /// <param name="duration">Duration to disable input</param>
        public void DisableInputTemporarily(float duration)
        {
            InputEnabled = false;
            Invoke(nameof(ReEnableInput), duration);
        }
        
        private void ReEnableInput()
        {
            InputEnabled = true;
        }
        
        /// <summary>
        /// Get current movement input
        /// </summary>
        /// <returns>Current movement input vector</returns>
        public Vector2 GetMovementInput()
        {
            return movementInput;
        }
        
        /// <summary>
        /// Check if any input is currently being pressed
        /// </summary>
        /// <returns>True if any input is active</returns>
        public bool HasAnyInput()
        {
            return movementInput.magnitude > 0.1f ||
                   (interactAction?.IsPressed() ?? false) ||
                   (disguiseAction?.IsPressed() ?? false) ||
                   (danceAction?.IsPressed() ?? false);
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (interactAction != null)
                interactAction.performed -= OnInteract;
                
            if (disguiseAction != null)
                disguiseAction.performed -= OnDisguise;
                
            if (danceAction != null)
                danceAction.performed -= OnDance;
        }
        
        #region Debug
        
        private void OnGUI()
        {
            if (!Application.isPlaying || !Debug.isDebugBuild) return;
            
            // Show input debug info in debug builds
            GUILayout.BeginArea(new Rect(10, 10, 200, 150));
            GUILayout.Label($"Movement: {movementInput}");
            GUILayout.Label($"Input Enabled: {inputEnabled}");
            GUILayout.Label($"Has Any Input: {HasAnyInput()}");
            
            if (interactAction != null)
                GUILayout.Label($"Interact: {interactAction.IsPressed()}");
            if (disguiseAction != null)
                GUILayout.Label($"Disguise: {disguiseAction.IsPressed()}");
            if (danceAction != null)
                GUILayout.Label($"Dance: {danceAction.IsPressed()}");
                
            GUILayout.EndArea();
        }
        
        #endregion
    }
}