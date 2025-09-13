using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using HideAndSeek.Data;

namespace HideAndSeek.Core
{
    public class SpawnManager : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private GameObject npcPrefab;
        [SerializeField] private Transform spawnParent;
        [SerializeField] private float minSpawnRadius = 5f;
        [SerializeField] private float maxSpawnRadius = 50f;
        [SerializeField] private float minSpawnDistance = 2f;

        [Header("Spawn Events")]
        public UnityEvent OnSpawnStart;
        public UnityEvent OnSpawnComplete;

        // Singleton instance
        private static SpawnManager _instance;
        public static SpawnManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SpawnManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SpawnManager");
                        _instance = go.AddComponent<SpawnManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // Object Pool
        private Queue<GameObject> npcPool = new Queue<GameObject>();
        private List<GameObject> spawnedNPCs = new List<GameObject>();
        private List<Vector3> spawnedPositions = new List<Vector3>();

        // Pool settings
        private int poolSize = 50;
        private bool poolInitialized = false;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePool();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void InitializePool()
        {
            if (poolInitialized || npcPrefab == null) return;

            if (spawnParent == null)
            {
                GameObject parentGO = new GameObject("NPC Pool");
                spawnParent = parentGO.transform;
                spawnParent.SetParent(transform);
            }

            for (int i = 0; i < poolSize; i++)
            {
                GameObject npc = Instantiate(npcPrefab, spawnParent);
                npc.SetActive(false);
                npcPool.Enqueue(npc);
            }

            poolInitialized = true;
        }

        public void SpawnNPCs()
        {
            if (!poolInitialized) InitializePool();

            GameSettings settings = GameManager.Instance.GetGameSettings();
            int npcCount = settings != null ? settings.npcCount : 30;

            OnSpawnStart?.Invoke();
            ClearSpawnedPositions();

            for (int i = 0; i < npcCount; i++)
            {
                SpawnSingleNPC();
            }

            OnSpawnComplete?.Invoke();
            Debug.Log($"Spawned {spawnedNPCs.Count} NPCs");
        }

        private void SpawnSingleNPC()
        {
            if (npcPool.Count == 0)
            {
                Debug.LogWarning("NPC pool is empty! Consider increasing pool size.");
                return;
            }

            GameObject npc = npcPool.Dequeue();
            Vector3 spawnPosition = GetRandomSpawnPosition();

            npc.transform.position = spawnPosition;
            npc.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            npc.AddComponent<HideAndSeek.NPC.AIController>();
            npc.SetActive(true);

            spawnedNPCs.Add(npc);
            spawnedPositions.Add(spawnPosition);
        }

        private Vector3 GetRandomSpawnPosition()
        {
            Vector3 position;
            int attempts = 0;
            int maxAttempts = 100;

            do
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = Random.Range(minSpawnRadius, maxSpawnRadius);

                position = new Vector3(
                    Mathf.Cos(angle) * distance,
                    0f,
                    Mathf.Sin(angle) * distance
                );

                attempts++;

                if (attempts >= maxAttempts)
                {
                    Debug.LogWarning("Could not find non-overlapping spawn position, using current position");
                    break;
                }

            } while (IsPositionTooClose(position));

            return position;
        }

        private bool IsPositionTooClose(Vector3 newPosition)
        {
            foreach (Vector3 existingPosition in spawnedPositions)
            {
                if (Vector3.Distance(newPosition, existingPosition) < minSpawnDistance)
                {
                    return true;
                }
            }
            return false;
        }

        public void DespawnAllNPCs()
        {
            foreach (GameObject npc in spawnedNPCs)
            {
                if (npc != null)
                {
                    npc.SetActive(false);
                    npcPool.Enqueue(npc);
                }
            }

            spawnedNPCs.Clear();
            ClearSpawnedPositions();
        }

        private void ClearSpawnedPositions()
        {
            spawnedPositions.Clear();
        }

        public void SetNPCPrefab(GameObject prefab)
        {
            npcPrefab = prefab;
        }

        public void SetSpawnParent(Transform parent)
        {
            spawnParent = parent;
        }

        public List<GameObject> GetSpawnedNPCs()
        {
            return new List<GameObject>(spawnedNPCs);
        }

        public int GetSpawnedNPCCount()
        {
            return spawnedNPCs.Count;
        }

        public List<GameObject> GetRandomNPCs(int count)
        {
            List<GameObject> result = new List<GameObject>();

            if (count <= 0 || spawnedNPCs.Count == 0)
            {
                return result;
            }

            List<GameObject> availableNPCs = new List<GameObject>(spawnedNPCs);
            int actualCount = Mathf.Min(count, availableNPCs.Count);

            for (int i = 0; i < actualCount; i++)
            {
                int randomIndex = Random.Range(0, availableNPCs.Count);
                result.Add(availableNPCs[randomIndex]);
                availableNPCs.RemoveAt(randomIndex);
            }

            return result;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStart.RemoveListener(SpawnNPCs);
                GameManager.Instance.OnGameEnd.RemoveListener(DespawnAllNPCs);
            }
        }
    }
}