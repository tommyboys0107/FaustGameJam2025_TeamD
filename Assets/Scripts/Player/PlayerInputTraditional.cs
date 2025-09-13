using UnityEngine;

namespace HideAndSeek.Player
{
    /// <summary>
    /// Handles traditional Unity Input Manager for player controls
    /// Supports two players: P1 (WASD) and P2 (Arrow Keys)
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInputTraditional : MonoBehaviour
    {
        [Header("Player Settings")]
        [SerializeField] private int playerID = 1; // 1 for P1 (WASD), 2 for P2 (Arrow Keys)
        
        [Header("Input Keys")]
        [SerializeField] private KeyCode interactKey = KeyCode.Space;
        [SerializeField] private KeyCode disguiseKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode danceKey = KeyCode.Tab;
        
        // Component references
        private PlayerController playerController;
        private CharacterMovement characterMovement;
        
        // Input state
        private Vector2 movementInput;
        private bool inputEnabled = true;
        
        // Input key mappings for different players
        private struct InputKeys
        {
            public KeyCode up, down, left, right;
            public KeyCode interact, disguise, dance;
        }
        
        private InputKeys currentKeys;
        
        public bool InputEnabled 
        { 
            get => inputEnabled; 
            set => inputEnabled = value; 
        }
        
        private void Awake()
        {
            InitializeComponents();
            SetupInputKeys();
        }
        
        private void Update()
        {
            if (inputEnabled)
            {
                HandleMovementInput();
                HandleActionInput();
            }
        }
        
        private void InitializeComponents()
        {
            playerController = GetComponent<PlayerController>();
            characterMovement = GetComponent<CharacterMovement>();
            
            if (playerController == null)
            {
                Debug.LogError("PlayerInputTraditional requires a PlayerController component!");
            }
        }
        
        private void SetupInputKeys()
        {
            switch (playerID)
            {
                case 1: // P1 - WASD
                    currentKeys = new InputKeys
                    {
                        up = KeyCode.W,
                        down = KeyCode.S,
                        left = KeyCode.A,
                        right = KeyCode.D,
                        interact = KeyCode.Space,
                        disguise = KeyCode.LeftShift,
                        dance = KeyCode.Tab
                    };
                    break;
                    
                case 2: // P2 - Arrow Keys
                    currentKeys = new InputKeys
                    {
                        up = KeyCode.UpArrow,
                        down = KeyCode.DownArrow,
                        left = KeyCode.LeftArrow,
                        right = KeyCode.RightArrow,
                        interact = KeyCode.Return,
                        disguise = KeyCode.RightShift,
                        dance = KeyCode.RightControl
                    };
                    break;
                    
                default:
                    Debug.LogWarning($"Unsupported player ID: {playerID}. Using P1 defaults.");
                    playerID = 1;
                    SetupInputKeys();
                    break;
            }
        }
        
        private void HandleMovementInput()
        {
            if (characterMovement == null) return;
            
            // Read movement input based on player keys
            float horizontal = 0f;
            float vertical = 0f;
            
            if (Input.GetKey(currentKeys.left)) horizontal -= 1f;
            if (Input.GetKey(currentKeys.right)) horizontal += 1f;
            if (Input.GetKey(currentKeys.down)) vertical -= 1f;
            if (Input.GetKey(currentKeys.up)) vertical += 1f;
            
            movementInput = new Vector2(horizontal, vertical);
            
            // Normalize diagonal movement
            if (movementInput.magnitude > 1f)
                movementInput.Normalize();
            
            // Convert to 3D movement direction
            Vector3 moveDirection = new Vector3(movementInput.x, 0, movementInput.y);
            
            // Apply movement to character
            characterMovement.SetMovementInput(moveDirection);
            
            // Handle rotation
            if (moveDirection.magnitude > 0.1f)
            {
                characterMovement.RotateTowardsDirection(moveDirection);
            }
            
            // Update PlayerController movement input for compatibility
            if (playerController != null)
            {
                playerController.SetMovementInput(movementInput);
            }
        }
        
        private void HandleActionInput()
        {
            if (playerController == null) return;
            
            // Handle interact input
            if (Input.GetKeyDown(currentKeys.interact))
            {
                OnInteract();
            }
            
            // Handle disguise input
            if (Input.GetKeyDown(currentKeys.disguise))
            {
                OnDisguise();
            }
            
            // Handle dance input
            if (Input.GetKeyDown(currentKeys.dance))
            {
                OnDance();
            }
        }
        
        #region Input Action Methods
        
        private void OnInteract()
        {
            if (!inputEnabled || playerController == null) return;
            
            playerController.TriggerInteract();
        }
        
        private void OnDisguise()
        {
            if (!inputEnabled || playerController == null) return;
            
            playerController.TriggerDisguise();
        }
        
        private void OnDance()
        {
            if (!inputEnabled || playerController == null) return;
            
            playerController.TriggerDance();
        }
        
        #endregion
        
        /// <summary>
        /// Set the player ID (1 for P1/WASD, 2 for P2/Arrow Keys)
        /// </summary>
        /// <param name="id">Player ID</param>
        public void SetPlayerID(int id)
        {
            playerID = id;
            SetupInputKeys();
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
                   Input.GetKey(currentKeys.interact) ||
                   Input.GetKey(currentKeys.disguise) ||
                   Input.GetKey(currentKeys.dance);
        }
        
        /// <summary>
        /// Get the current input key configuration
        /// </summary>
        /// <returns>String describing the key layout</returns>
        public string GetInputDescription()
        {
            return playerID == 1 ? 
                "P1: WASD移動, Space互動, LeftShift偽裝, Tab舞蹈" : 
                "P2: 方向鍵移動, Enter互動, RightShift偽裝, RightCtrl舞蹈";
        }
        
        #region Debug
        
        private void OnGUI()
        {
            if (!Application.isPlaying || !Debug.isDebugBuild) return;
            
            // Show input debug info in debug builds
            int yOffset = playerID == 1 ? 10 : 180;
            GUILayout.BeginArea(new Rect(10, yOffset, 250, 150));
            GUILayout.Label($"Player {playerID} Input:");
            GUILayout.Label($"Movement: {movementInput}");
            GUILayout.Label($"Input Enabled: {inputEnabled}");
            GUILayout.Label($"Has Any Input: {HasAnyInput()}");
            GUILayout.Label(GetInputDescription());
            GUILayout.EndArea();
        }
        
        #endregion
    }
}