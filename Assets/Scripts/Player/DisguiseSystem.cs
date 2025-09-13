using UnityEngine;
using HideAndSeek.Data;

namespace HideAndSeek.Player
{
    /// <summary>
    /// Handles player disguise mechanics
    /// Manages appearance changes and suspicion levels
    /// </summary>
    public class DisguiseSystem : MonoBehaviour
    {
        [Header("Disguise Settings")]
        [SerializeField] private Material[] availableMaterials;
        [SerializeField] private Mesh[] availableMeshes;
        [SerializeField] private Color[] availableColors;
        
        [Header("Suspicion Settings")]
        [SerializeField] private float baseSuspicionLevel = 0f;
        [SerializeField] private float maxSuspicionLevel = 100f;
        [SerializeField] private float suspicionIncreaseRate = 1f;
        [SerializeField] private float suspicionDecreaseRate = 0.5f;
        
        [Header("Detection Settings")]
        [SerializeField] private float detectionThreshold = 80f;
        [SerializeField] private float disguiseEffectDuration = 10f;
        
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip disguiseSound;
        
        // Components
        private Renderer characterRenderer;
        private MeshFilter meshFilter;
        private PlayerController playerController;
        
        // Disguise state
        private float currentSuspicionLevel;
        private float lastDisguiseTime;
        private bool isDisguised;
        private Material originalMaterial;
        private Mesh originalMesh;
        private Color originalColor;
        private int currentDisguiseIndex = -1;
        
        // Properties
        public float SuspicionLevel => currentSuspicionLevel;
        public bool IsDetectable => currentSuspicionLevel >= detectionThreshold;
        public bool IsDisguised => isDisguised;
        public float SuspicionPercentage => currentSuspicionLevel / maxSuspicionLevel;
        public float TimeSinceLastDisguise => Time.time - lastDisguiseTime;
        
        // Events
        public System.Action<float> OnSuspicionChanged;
        public System.Action OnBecameDetectable;
        public System.Action OnDisguiseChanged;
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            LoadSettingsFromGameManager();
            StoreOriginalAppearance();
        }
        
        private void Update()
        {
            UpdateSuspicion(Time.deltaTime);
        }
        
        private void InitializeComponents()
        {
            characterRenderer = GetComponentInChildren<Renderer>();
            meshFilter = GetComponentInChildren<MeshFilter>();
            playerController = GetComponent<PlayerController>();
            
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
                suspicionIncreaseRate = gameSettings.suspicionIncreaseRate;
                suspicionDecreaseRate = gameSettings.suspicionDecreaseRate;
                maxSuspicionLevel = gameSettings.maxSuspicionLevel;
            }
        }
        
        private void StoreOriginalAppearance()
        {
            if (characterRenderer != null)
            {
                originalMaterial = characterRenderer.material;
                originalColor = characterRenderer.material.color;
            }
            
            if (meshFilter != null)
            {
                originalMesh = meshFilter.mesh;
            }
        }
        
        /// <summary>
        /// Change the character's appearance (disguise)
        /// </summary>
        public void ChangeAppearance()
        {
            if (Time.time < lastDisguiseTime + Core.GameManager.Instance.GetGameSettings()?.disguiseCooldown)
            {
                Debug.Log("Disguise is on cooldown");
                return;
            }
            
            lastDisguiseTime = Time.time;
            
            // Choose a random disguise
            int newDisguiseIndex;
            do
            {
                newDisguiseIndex = Random.Range(0, Mathf.Max(availableMaterials.Length, availableColors.Length));
            } while (newDisguiseIndex == currentDisguiseIndex && (availableMaterials.Length > 1 || availableColors.Length > 1));
            
            currentDisguiseIndex = newDisguiseIndex;
            
            // Apply material change
            if (availableMaterials.Length > 0 && characterRenderer != null)
            {
                Material newMaterial = availableMaterials[newDisguiseIndex % availableMaterials.Length];
                characterRenderer.material = newMaterial;
            }
            
            // Apply color change
            if (availableColors.Length > 0 && characterRenderer != null)
            {
                Color newColor = availableColors[newDisguiseIndex % availableColors.Length];
                characterRenderer.material.color = newColor;
            }
            
            // Apply mesh change
            if (availableMeshes.Length > 0 && meshFilter != null)
            {
                Mesh newMesh = availableMeshes[newDisguiseIndex % availableMeshes.Length];
                meshFilter.mesh = newMesh;
            }
            
            // Reset suspicion after disguise
            currentSuspicionLevel = baseSuspicionLevel;
            isDisguised = true;
            
            // Play disguise sound
            if (disguiseSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(disguiseSound, 0.8f);
            }
            
            // Invoke events
            OnDisguiseChanged?.Invoke();
            OnSuspicionChanged?.Invoke(currentSuspicionLevel);
            
            Debug.Log($"Disguise changed to index {currentDisguiseIndex}");
            
            // Schedule return to normal after duration
            Invoke(nameof(RevertToOriginal), disguiseEffectDuration);
        }
        
        /// <summary>
        /// Revert to original appearance
        /// </summary>
        public void RevertToOriginal()
        {
            if (!isDisguised) return;
            
            // Restore original appearance
            if (characterRenderer != null && originalMaterial != null)
            {
                characterRenderer.material = originalMaterial;
                characterRenderer.material.color = originalColor;
            }
            
            if (meshFilter != null && originalMesh != null)
            {
                meshFilter.mesh = originalMesh;
            }
            
            isDisguised = false;
            currentDisguiseIndex = -1;
            
            OnDisguiseChanged?.Invoke();
            
            Debug.Log("Reverted to original appearance");
        }
        
        /// <summary>
        /// Update suspicion level over time
        /// </summary>
        /// <param name="deltaTime">Time since last update</param>
        public void UpdateSuspicion(float deltaTime)
        {
            float previousSuspicion = currentSuspicionLevel;
            bool wasDetectable = IsDetectable;
            
            // Increase suspicion when performing suspicious actions
            if (IsBehavingSuspiciously())
            {
                IncreaseSuspicion(suspicionIncreaseRate * deltaTime);
            }
            else
            {
                // Decrease suspicion when not being suspicious
                DecreaseSuspicion(suspicionDecreaseRate * deltaTime);
            }
            
            // Check if suspicion level changed significantly
            if (Mathf.Abs(currentSuspicionLevel - previousSuspicion) > 0.1f)
            {
                OnSuspicionChanged?.Invoke(currentSuspicionLevel);
            }
            
            // Check if became detectable
            if (!wasDetectable && IsDetectable)
            {
                OnBecameDetectable?.Invoke();
                Debug.Log("Player became detectable!");
            }
        }
        
        /// <summary>
        /// Increase suspicion level
        /// </summary>
        /// <param name="amount">Amount to increase</param>
        public void IncreaseSuspicion(float amount)
        {
            currentSuspicionLevel = Mathf.Min(currentSuspicionLevel + amount, maxSuspicionLevel);
        }
        
        /// <summary>
        /// Decrease suspicion level
        /// </summary>
        /// <param name="amount">Amount to decrease</param>
        public void DecreaseSuspicion(float amount)
        {
            currentSuspicionLevel = Mathf.Max(currentSuspicionLevel - amount, 0f);
        }
        
        /// <summary>
        /// Check if the player is currently behaving suspiciously
        /// </summary>
        /// <returns>True if behaving suspiciously</returns>
        private bool IsBehavingSuspiciously()
        {
            if (playerController == null) return false;
            
            // Increase suspicion when:
            // - Moving too fast
            // - Not dancing when others are dancing
            // - Just performed an action
            
            bool movingTooFast = playerController.GetComponent<CharacterMovement>()?.Speed > 6f;
            bool notDancing = playerController.CurrentState != PlayerController.PlayerState.Dancing;
            bool recentAction = Time.time < playerController.GetComponent<ActionSystem>()?.LastActionTime + 2f;
            
            return movingTooFast || (notDancing && ShouldBeDancing()) || recentAction;
        }
        
        /// <summary>
        /// Check if the player should be dancing based on environment
        /// </summary>
        /// <returns>True if should be dancing</returns>
        private bool ShouldBeDancing()
        {
            // This could check if music is playing, if NPCs are dancing, etc.
            // For now, assume players should dance 70% of the time
            return Random.value < 0.7f;
        }
        
        /// <summary>
        /// Reset suspicion to base level
        /// </summary>
        public void ResetSuspicion()
        {
            currentSuspicionLevel = baseSuspicionLevel;
            OnSuspicionChanged?.Invoke(currentSuspicionLevel);
        }
        
        /// <summary>
        /// Set suspicion level directly
        /// </summary>
        /// <param name="level">New suspicion level</param>
        public void SetSuspicionLevel(float level)
        {
            currentSuspicionLevel = Mathf.Clamp(level, 0f, maxSuspicionLevel);
            OnSuspicionChanged?.Invoke(currentSuspicionLevel);
        }
        
        /// <summary>
        /// Force detection state
        /// </summary>
        /// <param name="detected">Whether to set as detected</param>
        public void SetDetected(bool detected)
        {
            if (detected)
            {
                currentSuspicionLevel = maxSuspicionLevel;
                OnBecameDetectable?.Invoke();
            }
            else
            {
                currentSuspicionLevel = 0f;
            }
            OnSuspicionChanged?.Invoke(currentSuspicionLevel);
        }
        
        /// <summary>
        /// Get available disguise count
        /// </summary>
        /// <returns>Number of available disguises</returns>
        public int GetAvailableDisguiseCount()
        {
            return Mathf.Max(availableMaterials.Length, availableColors.Length, availableMeshes.Length);
        }
        
        private void OnValidate()
        {
            // Ensure valid ranges
            maxSuspicionLevel = Mathf.Max(1f, maxSuspicionLevel);
            detectionThreshold = Mathf.Clamp(detectionThreshold, 0f, maxSuspicionLevel);
            suspicionIncreaseRate = Mathf.Max(0f, suspicionIncreaseRate);
            suspicionDecreaseRate = Mathf.Max(0f, suspicionDecreaseRate);
            disguiseEffectDuration = Mathf.Max(1f, disguiseEffectDuration);
            currentSuspicionLevel = Mathf.Clamp(currentSuspicionLevel, 0f, maxSuspicionLevel);
        }
        
        // Debug visualization
        private void OnGUI()
        {
            if (!Application.isPlaying || !Debug.isDebugBuild) return;
            
            // Show suspicion debug info
            GUILayout.BeginArea(new Rect(10, 200, 200, 100));
            GUILayout.Label($"Suspicion: {currentSuspicionLevel:F1}/{maxSuspicionLevel}");
            GUILayout.Label($"Detectable: {IsDetectable}");
            GUILayout.Label($"Disguised: {IsDisguised}");
            GUILayout.Label($"Disguise Index: {currentDisguiseIndex}");
            GUILayout.EndArea();
        }
    }
}