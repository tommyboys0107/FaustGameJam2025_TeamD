using HideAndSeek.Data;
using HideAndSeek.Player;
using UnityEngine;
using static HideAndSeek.Player.PlayerController;

namespace HideAndSeek.NPC
{
    public class AIController : MonoBehaviour
    {
        private enum NPCState { Idle, Move, Dance };

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 720f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float deceleration = 10f;

        [Header("State Settings")]
        [Range(0, 1)]
        [SerializeField] private float dancingProb = 0f;
        [SerializeField] private float stateUpdatedMinTime = 1f;
        [SerializeField] private float stateUpdatedMaxTime = 5f;
        private float nextTime;
        private NPCState nowState = NPCState.Idle;

        // Components
        private CharacterMovement characterMovement;
        private ActionSystem actionSystem;

        // Movement 
        private Vector2 movementInput;
        private bool isPerformAction = false;


        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            LoadSettingsFromGameManager();
            nextTime = Random.Range(stateUpdatedMinTime, stateUpdatedMaxTime);
        }

        private void Update()
        {
            UpdateState();
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

        private void InitializeComponents()
        {
            characterMovement = GetComponent<CharacterMovement>();

            actionSystem = GetComponent<ActionSystem>();
            if (actionSystem == null)
                actionSystem = gameObject.AddComponent<ActionSystem>();
        }

        private void UpdateState()
        {
            nextTime -= Time.deltaTime;
            if (nextTime <= 0)
            {
                nowState = RandomState();
                Debug.Log($"{gameObject.name}: {nowState}");
                switch (nowState)
                {
                    case NPCState.Idle:
                        break;
                    case NPCState.Move:
                        HandleMovement();
                        break;
                    case NPCState.Dance:
                        PerformDance();
                        break;
                }

                nextTime = Random.Range(stateUpdatedMinTime, stateUpdatedMaxTime);
            }
        }

        private NPCState RandomState()
        {
            NPCState state = NPCState.Idle;
            int max = (int)(dancingProb * 10) + 3;
            int rnd = Random.Range(0, max);
            if (rnd == 0) state = NPCState.Idle;
            else if (rnd == 1) state = NPCState.Move;
            else state = NPCState.Dance;
            return state;
        }

        private void PerformDance()
        {
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
            nowState = NPCState.Idle;
        }

        private void HandleMovement()
        {
            if (characterMovement == null) return;

            // Read movement input based on player keys
            float horizontal = 0f;
            float vertical = 0f;

            horizontal = Random.Range(-1, 2);
            vertical = Random.Range(-1, 2);

            movementInput = new Vector2(horizontal, vertical);

            // Normalize diagonal movement
            if (movementInput.magnitude > 1f)
                movementInput.Normalize();

            // Convert to 3D movement direction
            Vector3 moveDirection = new Vector3(movementInput.x, 0, movementInput.y);

            // Instant stop on input release
            if (horizontal == 0 && vertical == 0)
            {
                moveDirection = Vector3.zero; // Or maintain vertical velocity for gravity
                characterMovement.IsMoving = false;
            }
            else
            {
                characterMovement.IsMoving = true;
            }
            actionSystem.CancelAction();

            // Handle rotation
            if (moveDirection.magnitude > 0.1f)
            {
                characterMovement.RotateTowardsDirection(moveDirection);
            }

            // Apply movement to character
            characterMovement.SetMovementInput(moveDirection);
        }
    }
}