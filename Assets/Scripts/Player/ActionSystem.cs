using UnityEngine;
using HideAndSeek.Core;

namespace HideAndSeek.Player
{
    /// <summary>
    /// Handles player actions like dancing, killing, and arresting
    /// Manages animations and action execution
    /// </summary>
    public class ActionSystem : MonoBehaviour
    {
        [Header("Action Settings")]
        [SerializeField] private AnimationClip[] danceAnimations;
        [SerializeField] private AnimationClip killAnimation;
        [SerializeField] private AnimationClip arrestAnimation;
        
        [Header("Action Detection")]
        [SerializeField] private float interactionRange = 2f;
        [SerializeField] private LayerMask npcLayer = 1;
        [SerializeField] private LayerMask playerLayer = 1;
        
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip killSound;
        [SerializeField] private AudioClip arrestSound;
        [SerializeField] private AudioClip[] danceSounds;
        
        // Components
        private Animator animator;
        private PlayerController playerController;
        
        // Action state
        private float lastActionTime;
        private bool isPerformingAction;
        
        // Properties
        public bool IsPerformingAction => isPerformingAction;
        public float LastActionTime => lastActionTime;
        
        // Events
        public System.Action<GameObject> OnKillPerformed;
        public System.Action<GameObject> OnArrestPerformed;
        public System.Action<int> OnDancePerformed;
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void InitializeComponents()
        {
            animator = GetComponentInChildren<Animator>();
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
        /// Perform a dance action
        /// </summary>
        /// <param name="danceType">Type of dance (0-3 based on direction)</param>
        public void PerformDance(int danceType = 0)
        {
            if (isPerformingAction) return;
            
            StartCoroutine(PerformDanceCoroutine(danceType));
        }
        
        /// <summary>
        /// Perform a kill action (Killer role only)
        /// </summary>
        public void PerformKill()
        {
            if (isPerformingAction) return;
            if (playerController == null) return;
            if (playerController.PlayerRole != GameManager.PlayerRole.Killer) return;
            
            GameObject target = FindNearestTarget(npcLayer);
            if (target != null)
            {
                StartCoroutine(PerformKillCoroutine(target));
            }
            else
            {
                Debug.Log("No target found for kill action");
            }
        }
        
        /// <summary>
        /// Perform an arrest action (Police role only)
        /// </summary>
        public void PerformArrest()
        {
            if (isPerformingAction) return;
            if (playerController == null) return;
            if (playerController.PlayerRole != GameManager.PlayerRole.Police) return;
            
            // Police can arrest the killer player
            GameObject killerPlayer = GameManager.Instance.GetPlayerByRole(GameManager.PlayerRole.Killer);
            if (killerPlayer != null && Vector3.Distance(transform.position, killerPlayer.transform.position) <= interactionRange)
            {
                StartCoroutine(PerformArrestCoroutine(killerPlayer));
            }
            else
            {
                Debug.Log("Killer not in range for arrest");
            }
        }
        
        private System.Collections.IEnumerator PerformDanceCoroutine(int danceType)
        {
            isPerformingAction = true;
            lastActionTime = Time.time;
            
            // Play dance animation
            if (animator != null)
            {
                string danceTrigger = $"Dance{Mathf.Clamp(danceType, 0, 3)}";
                animator.SetTrigger(danceTrigger);
            }
            
            // Play dance sound
            if (danceSounds.Length > 0 && audioSource != null)
            {
                AudioClip danceSound = danceSounds[Random.Range(0, danceSounds.Length)];
                audioSource.PlayOneShot(danceSound, 0.8f);
            }
            
            // Invoke event
            OnDancePerformed?.Invoke(danceType);
            
            Debug.Log($"Player dancing: Type {danceType}");
            
            // Wait for dance duration
            yield return new WaitForSeconds(3f);
            
            isPerformingAction = false;
        }
        
        private System.Collections.IEnumerator PerformKillCoroutine(GameObject target)
        {
            isPerformingAction = true;
            lastActionTime = Time.time;
            
            // Face the target
            Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
            directionToTarget.y = 0;
            if (directionToTarget != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(directionToTarget);
            }
            
            // Play kill animation
            if (animator != null)
            {
                animator.SetTrigger("Kill");
            }
            
            // Play kill sound
            if (killSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(killSound, 1f);
            }
            
            // Wait for animation timing
            yield return new WaitForSeconds(1f);
            
            // Execute kill
            ExecuteKill(target);
            
            // Invoke event
            OnKillPerformed?.Invoke(target);
            
            Debug.Log($"Player killed: {target.name}");
            
            // Update game manager
            GameManager.Instance.AddKill();
            
            yield return new WaitForSeconds(0.5f);
            
            isPerformingAction = false;
        }
        
        private System.Collections.IEnumerator PerformArrestCoroutine(GameObject target)
        {
            isPerformingAction = true;
            lastActionTime = Time.time;
            
            // Face the target
            Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
            directionToTarget.y = 0;
            if (directionToTarget != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(directionToTarget);
            }
            
            // Play arrest animation
            if (animator != null)
            {
                animator.SetTrigger("Arrest");
            }
            
            // Play arrest sound
            if (arrestSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(arrestSound, 1f);
            }
            
            // Wait for animation timing
            yield return new WaitForSeconds(1.5f);
            
            // Execute arrest
            ExecuteArrest(target);
            
            // Invoke event
            OnArrestPerformed?.Invoke(target);
            
            Debug.Log($"Player arrested: {target.name}");
            
            // Police wins by arresting killer
            GameManager.Instance.EndGame(GameManager.PlayerRole.Police);
            
            yield return new WaitForSeconds(0.5f);
            
            isPerformingAction = false;
        }
        
        private GameObject FindNearestTarget(LayerMask targetLayer)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, interactionRange, targetLayer);
            
            GameObject nearestTarget = null;
            float nearestDistance = float.MaxValue;
            
            foreach (Collider collider in colliders)
            {
                if (collider.gameObject == gameObject) continue; // Skip self
                
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestTarget = collider.gameObject;
                }
            }
            
            return nearestTarget;
        }
        
        private void ExecuteKill(GameObject target)
        {
            // For NPCs, disable them or mark as killed
            var npcController = target.GetComponent<MonoBehaviour>(); // Will be NPCController when implemented
            if (npcController != null)
            {
                // Disable the NPC (or call a "Die" method when NPCController is implemented)
                target.SetActive(false);
            }
        }
        
        private void ExecuteArrest(GameObject target)
        {
            // Stun the target player
            var targetPlayerController = target.GetComponent<PlayerController>();
            if (targetPlayerController != null)
            {
                targetPlayerController.SetStunned(true, 5f); // Stun for 5 seconds
            }
        }
        
        /// <summary>
        /// Check if there are valid targets in range
        /// </summary>
        /// <returns>True if targets are available</returns>
        public bool HasTargetsInRange()
        {
            if (playerController.PlayerRole == GameManager.PlayerRole.Killer)
            {
                return FindNearestTarget(npcLayer) != null;
            }
            else
            {
                GameObject killerPlayer = GameManager.Instance.GetPlayerByRole(GameManager.PlayerRole.Killer);
                return killerPlayer != null && Vector3.Distance(transform.position, killerPlayer.transform.position) <= interactionRange;
            }
        }
        
        /// <summary>
        /// Get the current target if any
        /// </summary>
        /// <returns>Current target or null</returns>
        public GameObject GetCurrentTarget()
        {
            if (playerController.PlayerRole == GameManager.PlayerRole.Killer)
            {
                return FindNearestTarget(npcLayer);
            }
            else
            {
                GameObject killerPlayer = GameManager.Instance.GetPlayerByRole(GameManager.PlayerRole.Killer);
                if (killerPlayer != null && Vector3.Distance(transform.position, killerPlayer.transform.position) <= interactionRange)
                {
                    return killerPlayer;
                }
                return null;
            }
        }
        
        /// <summary>
        /// Cancel current action if possible
        /// </summary>
        public void CancelAction()
        {
            if (isPerformingAction)
            {
                StopAllCoroutines();
                isPerformingAction = false;
                
                if (animator != null)
                {
                    animator.SetTrigger("Idle");
                }
            }
        }
        
        // Debug visualization
        //private void OnDrawGizmosSelected()
        //{
        //    // Draw interaction range
        //    Gizmos.color = Color.yellow;
        //    Gizmos.DrawWireSphere(transform.position, interactionRange);
            
        //    // Draw line to current target
        //    GameObject target = GetCurrentTarget();
        //    if (target != null)
        //    {
        //        Gizmos.color = Color.red;
        //        Gizmos.DrawLine(transform.position, target.transform.position);
        //    }
        //}
        
        private void OnValidate()
        {
            // Ensure positive values
            interactionRange = Mathf.Max(0.1f, interactionRange);
        }
    }
}