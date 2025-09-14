using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using HideAndSeek.Data;
using System;

namespace HideAndSeek.Core
{
    [Serializable]
    public class SpawnZoneSettings
    {
        [SerializeField] public Transform zoneObject;
        [SerializeField] public float width = 10f;
        [SerializeField] public float length = 10f;
        [SerializeField, Range(0f, 1f)] public float spawnRatio = 0.2f;
        [HideInInspector] public int calculatedNPCCount;

        public bool IsValid()
        {
            return zoneObject != null && width > 0 && length > 0 && spawnRatio > 0;
        }
    }

    public class SpawnManager : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private GameObject npcPrefab;
        [SerializeField] private Transform spawnParent;
        [SerializeField] private float minSpawnRadius = 5f;
        [SerializeField] private float maxSpawnRadius = 50f;
        [SerializeField] private float minSpawnDistance = 2f;

        [Header("Zone Spawn Settings")]
        [SerializeField] private List<SpawnZoneSettings> spawnZones = new List<SpawnZoneSettings>();
        [SerializeField] private bool useZoneSpawning = false;

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
                InitializePool();
            }
            else if (_instance != this)
            {
                Destroy(_instance);
                _instance = this;
                InitializePool();
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

        private bool ValidateSpawnZoneSettings()
        {
            if (!useZoneSpawning || spawnZones.Count == 0)
                return true;

            float totalRatio = 0f;
            foreach (var zone in spawnZones)
            {
                if (!zone.IsValid())
                {
                    return false;
                }
                totalRatio += zone.spawnRatio;
            }

            if (totalRatio > 1.0f)
            {
                return false;
            }

            return true;
        }

        private void CalculateZoneNPCCounts(int totalNPCCount)
        {
            foreach (var zone in spawnZones)
            {
                zone.calculatedNPCCount = Mathf.RoundToInt(totalNPCCount * zone.spawnRatio);
            }
        }

        public void SpawnNPCs()
        {
            if (!poolInitialized) InitializePool();

            if (!ValidateSpawnZoneSettings())
            {
                return;
            }

            GameSettings settings = GameManager.Instance.GetGameSettings();
            int npcCount = settings != null ? settings.npcCount : 30;

            OnSpawnStart?.Invoke();
            ClearSpawnedPositions();

            if (useZoneSpawning && spawnZones.Count > 0)
            {
                CalculateZoneNPCCounts(npcCount);
                SpawnNPCsInZones();
            }
            else
            {
                for (int i = 0; i < npcCount; i++)
                {
                    SpawnSingleNPC();
                }
            }

            OnSpawnComplete?.Invoke();
        }

        private void SpawnNPCsInZones()
        {
            foreach (var zone in spawnZones)
            {
                if (!zone.IsValid()) continue;

                for (int i = 0; i < zone.calculatedNPCCount; i++)
                {
                    SpawnSingleNPCInZone(zone);
                }
            }
        }

        private void SpawnSingleNPCInZone(SpawnZoneSettings zone)
        {
            if (npcPool.Count == 0)
            {
                return;
            }

            GameObject npc = npcPool.Dequeue();
            Vector3 spawnPosition = GetRandomPositionInZone(zone);
            var meshMaterialManager = MeshMaterialManager.Instance;

            npc.transform.position = spawnPosition;
            npc.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);
            npc.AddComponent<HideAndSeek.NPC.AIController>();
            meshMaterialManager.ApplyMeshMaterial(
                npc.transform.GetChild(0).GetComponent<SkinnedMeshRenderer>(),
                meshMaterialManager.GetRandomMeshMaterial()
                );

            npc.SetActive(true);

            spawnedNPCs.Add(npc);
            spawnedPositions.Add(spawnPosition);
        }

        private void SpawnSingleNPC()
        {
            if (npcPool.Count == 0)
            {
                return;
            }

            GameObject npc = npcPool.Dequeue();
            Vector3 spawnPosition = GetRandomSpawnPosition();
            var meshMaterialManager = MeshMaterialManager.Instance;

            npc.transform.position = spawnPosition;
            npc.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);
            npc.AddComponent<HideAndSeek.NPC.AIController>();
            meshMaterialManager.ApplyMeshMaterial(
                npc.transform.GetChild(0).GetComponent<SkinnedMeshRenderer>(),
                meshMaterialManager.GetRandomMeshMaterial()
                );
            npc.SetActive(true);

            spawnedNPCs.Add(npc);
            spawnedPositions.Add(spawnPosition);
        }

        private Vector3 GetRandomPositionInZone(SpawnZoneSettings zone)
        {
            Vector3 position;
            int attempts = 0;
            int maxAttempts = 100;

            Vector3 zoneCenter = zone.zoneObject.position;
            float halfWidth = zone.width * 0.5f;
            float halfLength = zone.length * 0.5f;

            do
            {
                float randomX = UnityEngine.Random.Range(-halfWidth, halfWidth);
                float randomZ = UnityEngine.Random.Range(-halfLength, halfLength);

                position = new Vector3(
                    zoneCenter.x + randomX,
                    zoneCenter.y,
                    zoneCenter.z + randomZ
                );

                attempts++;

                if (attempts >= maxAttempts)
                {
                    break;
                }

            } while (IsPositionTooClose(position));

            return position;
        }

        private Vector3 GetRandomSpawnPosition()
        {
            Vector3 position;
            int attempts = 0;
            int maxAttempts = 100;

            do
            {
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = UnityEngine.Random.Range(minSpawnRadius, maxSpawnRadius);

                position = new Vector3(
                    Mathf.Cos(angle) * distance,
                    0f,
                    Mathf.Sin(angle) * distance
                );

                attempts++;

                if (attempts >= maxAttempts)
                {
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
                int randomIndex = UnityEngine.Random.Range(0, availableNPCs.Count);
                result.Add(availableNPCs[randomIndex]);
                availableNPCs.RemoveAt(randomIndex);
            }

            return result;
        }

        public void AddSpawnZone(Transform zoneObject, float width, float length, float spawnRatio)
        {
            var newZone = new SpawnZoneSettings
            {
                zoneObject = zoneObject,
                width = width,
                length = length,
                spawnRatio = spawnRatio
            };
            spawnZones.Add(newZone);
        }

        public void RemoveSpawnZone(Transform zoneObject)
        {
            spawnZones.RemoveAll(zone => zone.zoneObject == zoneObject);
        }

        public void ClearSpawnZones()
        {
            spawnZones.Clear();
        }

        public void SetUseZoneSpawning(bool enabled)
        {
            useZoneSpawning = enabled;
        }

        public bool GetUseZoneSpawning()
        {
            return useZoneSpawning;
        }

        public List<SpawnZoneSettings> GetSpawnZones()
        {
            return new List<SpawnZoneSettings>(spawnZones);
        }

        public float GetTotalSpawnRatio()
        {
            float total = 0f;
            foreach (var zone in spawnZones)
            {
                total += zone.spawnRatio;
            }
            return total;
        }
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}