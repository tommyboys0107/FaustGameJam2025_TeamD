using UnityEngine;
using UnityEngine.Events;
using HideAndSeek.Data;

namespace HideAndSeek.Core
{
    public class GameManager : MonoBehaviour
    {
        [Header("Game Events")]
        public UnityEvent OnGameStart;
        public UnityEvent OnGameEnd;
        public UnityEvent<PlayerRole> OnGameWin;
        
        [Header("Game Settings")]
        [SerializeField] private GameSettings gameSettings;
        
        // Fallback values if no settings asset is assigned
        [SerializeField] private float defaultGameTime = 300f;
        [SerializeField] private int defaultMaxKills = 10;
        
        // Singleton instance
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        _instance = go.AddComponent<GameManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // Game State Enum
        public enum GameState 
        { 
            Menu, 
            Playing, 
            GameOver 
        }
        
        // Player Role Enum
        public enum PlayerRole 
        { 
            Killer, 
            Police 
        }

        // Current game state
        [SerializeField] private GameState currentState = GameState.Menu;
        public GameState CurrentState => currentState;

        // Game time tracking
        private float currentGameTime;
        public float CurrentGameTime => currentGameTime;
        // public float RemainingTime => Mathf.Max(0, gameTime - currentGameTime);

        // Player references
        private GameObject killerPlayer;
        private GameObject policePlayer;
        
        // Game statistics
        private int killCount = 0;
        public int KillCount => killCount;

        private void Awake()
        {
            // Ensure singleton pattern
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Initialize();
        }

        private void Initialize()
        {
            currentState = GameState.Menu;
            currentGameTime = 0f;
            killCount = 0;
        }

        private void Update()
        {
            if (currentState == GameState.Playing)
            {
                UpdateGameTime();
                CheckWinConditions();
            }
        }

        private void UpdateGameTime()
        {
            currentGameTime += Time.deltaTime;
        }

        private void CheckWinConditions()
        {
            int maxKills = gameSettings != null ? gameSettings.maxKillsToWin : defaultMaxKills;
            
            // Killer wins if kill count reaches maximum
            if (killCount >= maxKills)
            {
                EndGame(PlayerRole.Killer);
            }
        }

        public void StartGame()
        {
            if (currentState != GameState.Menu) return;
            
            currentState = GameState.Playing;
            currentGameTime = 0f;
            killCount = 0;
            
            OnGameStart?.Invoke();
            Debug.Log("Game Started!");
        }

        public void EndGame(PlayerRole winner)
        {
            if (currentState != GameState.Playing) return;
            
            currentState = GameState.GameOver;
            OnGameEnd?.Invoke();
            OnGameWin?.Invoke(winner);
            
            Debug.Log($"Game Over! Winner: {winner}");
        }

        public void RestartGame()
        {
            currentState = GameState.Menu;
            currentGameTime = 0f;
            killCount = 0;
            
            Debug.Log("Game Restarted!");
        }

        public void AssignPlayerRoles(GameObject killer, GameObject police)
        {
            killerPlayer = killer;
            policePlayer = police;
            
            Debug.Log($"Roles assigned - Killer: {killer.name}, Police: {police.name}");
        }

        public void AddKill()
        {
            if (currentState != GameState.Playing) return;
            
            killCount++;
            Debug.Log($"Kill count: {killCount}/{gameSettings.maxKillsToWin}");
        }

        public GameObject GetPlayerByRole(PlayerRole role)
        {
            return role == PlayerRole.Killer ? killerPlayer : policePlayer;
        }

        // Public getters for game settings
        public float GetMaxGameTime()
        {
            return gameSettings != null ? gameSettings.maxGameTime : defaultGameTime;
        }
        
        public int GetMaxKills()
        {
            return gameSettings != null ? gameSettings.maxKillsToWin : defaultMaxKills;
        }
        
        public GameSettings GetGameSettings()
        {
            return gameSettings;
        }
        
        public void SetGameSettings(GameSettings settings)
        {
            gameSettings = settings;
        }
    }
}