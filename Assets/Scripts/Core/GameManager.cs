using HideAndSeek.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using HideAndSeek.Player;
using UnityEngine.UI;
using System.Collections;

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

        [Header("UI")]
        [SerializeField] private Text killScoreText;
        [SerializeField] private Image comboBar;
        [SerializeField] private Text gameTimeText;
        [SerializeField] private GameEndUI gameEndUI;

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
        // public float RemainingTime => Mathf.Max(0, gameTime - currentGameTime);

        // Player references
        private GameObject killerPlayer;
        private GameObject policePlayer;

        // Game statistics
        private int killCount = 0;
        private int killScore = 0;

        private float comboTime;
        private Coroutine comboCoolDown;

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
            
            ShowMeneu();
        }

        public void ShowMeneu()
        {
            currentState = GameState.Menu;
            //TODO: show menu
        }

        private void Start()
        {
            StartGame();
        }

        private void Update()
        {
            if (currentState == GameState.Playing)
            {
                UpdateGameTime(currentGameTime + Time.deltaTime);
            }
        }

        private void UpdateGameTime(float time)
        {
            currentGameTime += Time.deltaTime;
            int minutes = (int)(time / 60);
            int seconds = (int)(time % 60);
            gameTimeText.text =$"{minutes:D2}:{seconds:D2}";
        }

        private void updateKillScore(int core)
        {
            killScore = core;
            killScoreText.text = core.ToString();
        }

        public void StartGame()
        {
            if (currentState != GameState.Menu) return;

            // 生成角色
            SpawnManager.Instance.OnSpawnComplete.AddListener(OnSpawnNPCFinish);
            SpawnManager.Instance.SpawnNPCs();


            currentState = GameState.Playing;
            UpdateGameTime(0);
            killCount = 0;
            updateKillScore(0);
            comboTime = 0;
            comboBar.fillAmount = 0;

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
            if(comboCoolDown != null) StopCoroutine(comboCoolDown);
            currentState = GameState.GameOver;
            OnGameEnd?.Invoke();
            OnGameWin?.Invoke(winner);
            gameEndUI.Show(killCount, killScore, currentGameTime);            
            Debug.Log($"Game Over! Winner: {winner}");
        }

        public void RestartGame()
        {
            currentState = GameState.Menu;
            UpdateGameTime(0);
            updateKillScore(0);
            
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
            var score = killScore;
            if(comboTime > 0)
            {
                score +=(int)(gameSettings.killBaseScore * gameSettings.comboMultiplier);
            }
            else
            {
                score += gameSettings.killBaseScore;
                comboCoolDown = StartCoroutine(_comboCoolDown());
            }
            updateKillScore(score);
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
                    npcs[i].GetComponent<HideAndSeek.Player.ActionSystem>().SetPlayerController();
                }
                else if (i == 1)
                {
                    npcs[i].AddComponent<HideAndSeek.Player.PlayerController>().SetPlayerRole(PlayerRole.Police);
                    npcs[i].AddComponent<HideAndSeek.Player.PlayerInputTraditional>().SetPlayerID((int)PlayerRole.Police + 1);
                    npcs[i].GetComponent<HideAndSeek.Player.ActionSystem>().SetPlayerController();
                }
            }
        }

        private IEnumerator _comboCoolDown()
        {
            comboTime = gameSettings.comboTimeWindow;
            comboBar.fillAmount = 1f;
            do
            {
                yield return null;
                comboTime -= Time.deltaTime;
                comboBar.fillAmount = comboTime / gameSettings.comboTimeWindow;
            } while (comboTime > 0);
            comboBar.fillAmount = 0f;
        }
    }
}