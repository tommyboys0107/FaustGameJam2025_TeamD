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
        
        [Header("Detection Settings")]
        [SerializeField] private float detectionThreshold = 80f;
        
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip disguiseSound;
        
        // Components
        private Renderer characterRenderer;
        private MeshFilter meshFilter;
        private PlayerController playerController;
        
        // Disguise state
        private float lastDisguiseTime;
        private bool isDisguised;
        private Material originalMaterial;
        private Mesh originalMesh;
        private Color originalColor;
        private int currentDisguiseIndex = -1;
        
        // Properties
        public bool IsDisguised => isDisguised;
        public float TimeSinceLastDisguise => Time.time - lastDisguiseTime;
        
        // Events
        public System.Action OnDisguiseChanged;
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {

        }
        
        private void Update()
        {
            
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
            isDisguised = true;
            
            // Play disguise sound
            if (disguiseSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(disguiseSound, 0.8f);
            }
            
            // Invoke events
            OnDisguiseChanged?.Invoke();
            
            Debug.Log($"Disguise changed to index {currentDisguiseIndex}");
        }

        private bool ShouldBeDancing()
        {
            // This could check if music is playing, if NPCs are dancing, etc.
            // For now, assume players should dance 70% of the time
            return Random.value < 0.7f;
        }
        
        
        
        /// <summary>
        /// Get available disguise count
        /// </summary>
        /// <returns>Number of available disguises</returns>
        public int GetAvailableDisguiseCount()
        {
            return Mathf.Max(availableMaterials.Length, availableColors.Length, availableMeshes.Length);
        }
        
        // Debug visualization
        private void OnGUI()
        {
            if (!Application.isPlaying || !Debug.isDebugBuild) return;
            
            // Show suspicion debug info
            GUILayout.BeginArea(new Rect(10, 200, 200, 100));
            GUILayout.Label($"Disguised: {IsDisguised}");
            GUILayout.Label($"Disguise Index: {currentDisguiseIndex}");
            GUILayout.EndArea();
        }
    }
}