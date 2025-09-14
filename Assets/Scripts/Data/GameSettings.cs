using UnityEngine;

namespace HideAndSeek.Data
{
    [CreateAssetMenu(fileName = "GameSettings", menuName = "Hide&Seek/Game Settings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Player Settings")]
        [Tooltip("Cooldown time for killer's kill action")]
        public float killerCooldown = 2f;
        
        [Tooltip("Cooldown time for police's arrest action")]
        public float policeCooldown = 5f;
        
        [Tooltip("Cooldown time for disguise action")]
        public float disguiseCooldown = 5f;

        [Header("Player Movement")]
        [Tooltip("Player movement speed")]
        public float playerMoveSpeed = 5f;
        
        [Tooltip("Player rotation speed")]
        public float playerRotationSpeed = 720f;

        [Header("NPC Settings")]
        [Tooltip("Number of NPCs to spawn")]
        public int npcCount = 30;
                
        [Tooltip("Probability of NPC dancing (0-1)")]
        [Range(0f, 1f)]
        public float npcDanceFrequency = 0.7f;

        [Header("Score Settings")]
        [Tooltip("Base score for each kill")]
        public int killBaseScore = 100;
        
        [Tooltip("Combo multiplier for consecutive kills")]
        public float comboMultiplier = 1.5f;
        
        [Tooltip("Time window for combo in seconds")]
        public float comboTimeWindow = 3f;

        public float failedArrestPenalty = 5f;

        [Header("Audio Settings")]
        [Tooltip("Master volume level")]
        [Range(0f, 1f)]
        public float masterVolume = 1f;
        
        [Tooltip("Music volume level")]
        [Range(0f, 1f)]
        public float musicVolume = 0.8f;
        
        [Tooltip("SFX volume level")]
        [Range(0f, 1f)]
        public float sfxVolume = 1f;

        [Header("Lighting Settings")]
        [Tooltip("Light rotation speed for disco lights")]
        public float lightRotationSpeed = 30f;
        
        [Tooltip("Beat detection threshold for light sync")]
        [Range(0f, 1f)]
        public float beatThreshold = 0.8f;

        // Validation method to ensure settings are within reasonable ranges
        private void OnValidate()
        {
            // Ensure positive values
            killerCooldown = Mathf.Max(0.1f, killerCooldown);
            policeCooldown = Mathf.Max(0.1f, policeCooldown);
            disguiseCooldown = Mathf.Max(0.1f, disguiseCooldown);
            playerMoveSpeed = Mathf.Max(0.1f, playerMoveSpeed);
            npcCount = Mathf.Max(1, npcCount);
            killBaseScore = Mathf.Max(1, killBaseScore);
            
            // Ensure combo multiplier is at least 1
            comboMultiplier = Mathf.Max(1f, comboMultiplier);
            comboTimeWindow = Mathf.Max(0.1f, comboTimeWindow);
        }

        // Helper methods for easy access to common settings
        public float GetActionCooldown(HideAndSeek.Core.GameManager.PlayerRole role)
        {
            return role == HideAndSeek.Core.GameManager.PlayerRole.Killer ? killerCooldown : policeCooldown;
        }
    }
}