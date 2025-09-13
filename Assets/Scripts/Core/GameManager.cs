using HideAndSeek.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using HideAndSeek.Player;

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

        private void Start()
        {
            StartGame();
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
            //TODO
        }

        public void StartGame()
        {
            if (currentState != GameState.Menu) return;

            // 生成角色
            SpawnManager.Instance.OnSpawnComplete.AddListener(OnSpawnNPCFinish);
            SpawnManager.Instance.SpawnNPCs();


            currentState = GameState.Playing;
            currentGameTime = 0f;
            killCount = 0;
            
            OnGameStart?.Invoke();
            Debug.Log("Game Started!");
        }

        public void EndGame(PlayerRole winner)
        {
            if (currentState != GameState.Playing) return;

            var players = GameObject.FindObjectsByType<PlayerInputTraditional>(FindObjectsSortMode.None);
            foreach(var p in players)
            {
                p.enabled = false;
            }
            var npcs = GameObject.FindObjectsOfType<HideAndSeek.NPC.AIController>();
            foreach (var npc in npcs)
            {
                npc.enabled = false;
            }
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
            if (killer != null)
            {
                killerPlayer = killer;
            }

            if (police != null)
            {
                policePlayer = police;
            }
        }

        public void AddKill()
        {
            if (currentState != GameState.Playing) return;
            
            killCount++;
        }

        public GameObject GetPlayerByRole(PlayerRole role)
        {
            return role == PlayerRole.Killer ? killerPlayer : policePlayer;
        }
        
        public GameSettings GetGameSettings()
        {
            return gameSettings;
        }
        
        public void SetGameSettings(GameSettings settings)
        {
            gameSettings = settings;
        }

        private void OnSpawnNPCFinish()
        {
            // 設定玩家角色
            List<GameObject> npcs = SpawnManager.Instance.GetRandomNPCs(2);
            AssignPlayerRoles(npcs[0], npcs[1]);
            for (int i = 0; i < npcs.Count; i++)
            {
                Destroy(npcs[i].GetComponent<HideAndSeek.NPC.AIController>());
                if (i == 0)
                {
                    npcs[i].AddComponent<HideAndSeek.Player.PlayerController>().SetPlayerRole(PlayerRole.Killer);
                    npcs[i].AddComponent<HideAndSeek.Player.PlayerInputTraditional>().SetPlayerID((int)PlayerRole.Killer + 1);
                }
                else if (i == 1)
                {
                    npcs[i].AddComponent<HideAndSeek.Player.PlayerController>().SetPlayerRole(PlayerRole.Police);
                    npcs[i].AddComponent<HideAndSeek.Player.PlayerInputTraditional>().SetPlayerID((int)PlayerRole.Police + 1);
                }
            }
        }
    }
}