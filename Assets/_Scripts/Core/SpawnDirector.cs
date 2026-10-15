using System;
using System.Collections.Generic;
using UnityEngine;

namespace CursedHarvest
{
    public class SpawnDirector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private ObjectPool gruntPool;
        [SerializeField] private ObjectPool chaserPool;
        [SerializeField] private ObjectPool plaguePool;
        [SerializeField] private ObjectPool chargerPool;
        [SerializeField] private ObjectPool farmerPool;

        [Header("Spawn Boundaries")]
        [SerializeField] private float minSpawnRadius = 15f;
        [SerializeField] private float maxSpawnRadius = 20f;
        [SerializeField] private float arenaHalfBounds = 38f;

        [Header("Difficulty Curves")]
        [SerializeField] private float runTime;
        [SerializeField] private bool isRunning = true;
        [SerializeField] private bool isEndlessMode = false;

        private float lastSpawnTime;
        private float nextSpawnInterval = 1.8f;
        private int maxConcurrentEnemies = 15;

        public float RunTime => runTime;
        public bool IsEndless => isEndlessMode;
        public int TotalActiveEnemies => EnemyRegistry.Count;

        public static event Action<float> OnRunTimerUpdated;
        public static event Action OnBossTimeReached;

        private void Awake()
        {
            if (playerTransform == null)
            {
                GameObject p = GameObject.FindWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }
        }

        public void SetPlayer(Transform player)
        {
            playerTransform = player;
        }

        public void SetPools(ObjectPool grunt, ObjectPool chaser = null, ObjectPool plague = null, ObjectPool charger = null, ObjectPool farmer = null)
        {
            gruntPool = grunt;
            chaserPool = chaser;
            plaguePool = plague;
            chargerPool = charger;
            farmerPool = farmer;
        }

        public void ResetDirector()
        {
            runTime = 0f;
            lastSpawnTime = 0f;
            isEndlessMode = false;
            isRunning = true;
            nextSpawnInterval = 1.8f;
            maxConcurrentEnemies = 15;
        }

        public void EnterEndlessMode()
        {
            isEndlessMode = true;
            maxConcurrentEnemies = 120;
            nextSpawnInterval = 0.35f;
        }

        private void Update()
        {
            if (!isRunning) return;

            runTime += Time.deltaTime;
            OnRunTimerUpdated?.Invoke(runTime);

            UpdateDifficultyScaling();

            if (Time.time >= lastSpawnTime + nextSpawnInterval)
            {
                lastSpawnTime = Time.time;
                TrySpawnWave();
            }
        }

        private void UpdateDifficultyScaling()
        {
            if (isEndlessMode)
            {
                maxConcurrentEnemies = Mathf.Min(120, 70 + (int)((runTime - 720f) / 10f));
                nextSpawnInterval = Mathf.Max(0.25f, 0.4f - (runTime - 720f) * 0.0005f);
                return;
            }

            // Timeline based progression (00:00 to 12:00 = 720 seconds)
            float t = runTime;

            if (t < 180f) // 0-3 mins: Tutorial & early horde
            {
                float p = t / 180f;
                maxConcurrentEnemies = (int)Mathf.Lerp(12, 28, p);
                nextSpawnInterval = Mathf.Lerp(1.8f, 1.0f, p);
            }
            else if (t < 360f) // 3-6 mins: Increasing pressure
            {
                float p = (t - 180f) / 180f;
                maxConcurrentEnemies = (int)Mathf.Lerp(28, 48, p);
                nextSpawnInterval = Mathf.Lerp(1.0f, 0.7f, p);
            }
            else if (t < 540f) // 6-9 mins: Heavy horde
            {
                float p = (t - 360f) / 180f;
                maxConcurrentEnemies = (int)Mathf.Lerp(48, 70, p);
                nextSpawnInterval = Mathf.Lerp(0.7f, 0.5f, p);
            }
            else if (t < 720f) // 9-12 mins: Chaos & Boss prep
            {
                float p = (t - 540f) / 180f;
                maxConcurrentEnemies = (int)Mathf.Lerp(70, 95, p);
                nextSpawnInterval = Mathf.Lerp(0.5f, 0.35f, p);
            }
            else // 12:00 Boss spawn
            {
                isRunning = false;
                OnBossTimeReached?.Invoke();
            }
        }

        private void TrySpawnWave()
        {
            if (playerTransform == null) return;
            if (EnemyRegistry.Count >= maxConcurrentEnemies) return;

            int toSpawn = Mathf.Clamp(maxConcurrentEnemies - EnemyRegistry.Count, 1, 3);
            for (int i = 0; i < toSpawn; i++)
            {
                ObjectPool chosenPool = SelectPoolForCurrentTime();
                if (chosenPool == null) chosenPool = gruntPool;
                if (chosenPool == null) return;

                Vector3 spawnPos = CalculateSpawnPosition();
                GameObject enemyObj = chosenPool.Get(spawnPos, Quaternion.identity);
                EnemyController enemy = enemyObj.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    enemy.SetTarget(playerTransform);
                    ApplyStatScaling(enemy);
                }
            }
        }

        private ObjectPool SelectPoolForCurrentTime()
        {
            float t = runTime;

            if (isEndlessMode)
            {
                // In endless mode, all pools are active
                ObjectPool[] all = new ObjectPool[] { gruntPool, chaserPool, plaguePool, chargerPool, farmerPool };
                List<ObjectPool> valid = new List<ObjectPool>();
                for (int i = 0; i < all.Length; i++) if (all[i] != null) valid.Add(all[i]);
                return valid.Count > 0 ? valid[UnityEngine.Random.Range(0, valid.Count)] : gruntPool;
            }

            if (t < 180f)
            {
                if (chaserPool != null && UnityEngine.Random.value < 0.25f) return chaserPool;
                return gruntPool;
            }
            else if (t < 360f)
            {
                float roll = UnityEngine.Random.value;
                if (plaguePool != null && roll < 0.20f) return plaguePool;
                if (chaserPool != null && roll < 0.50f) return chaserPool;
                return gruntPool;
            }
            else if (t < 540f)
            {
                float roll = UnityEngine.Random.value;
                if (chargerPool != null && roll < 0.20f) return chargerPool;
                if (plaguePool != null && roll < 0.40f) return plaguePool;
                if (chaserPool != null && roll < 0.65f) return chaserPool;
                return gruntPool;
            }
            else
            {
                float roll = UnityEngine.Random.value;
                if (farmerPool != null && roll < 0.20f) return farmerPool;
                if (chargerPool != null && roll < 0.40f) return chargerPool;
                if (plaguePool != null && roll < 0.60f) return plaguePool;
                if (chaserPool != null && roll < 0.80f) return chaserPool;
                return gruntPool;
            }
        }

        private Vector3 CalculateSpawnPosition()
        {
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float radius = UnityEngine.Random.Range(minSpawnRadius, maxSpawnRadius);
            Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

            Vector3 rawPos = playerTransform.position + offset;
            rawPos.x = Mathf.Clamp(rawPos.x, -arenaHalfBounds, arenaHalfBounds);
            rawPos.z = Mathf.Clamp(rawPos.z, -arenaHalfBounds, arenaHalfBounds);
            rawPos.y = 0.5f;

            return rawPos;
        }

        private void ApplyStatScaling(EnemyController enemy)
        {
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health == null) return;

            // Small progressive HP boost over run time
            float timeMinutes = runTime / 60f;
            float hpMult = 1f + timeMinutes * 0.12f;
            if (isEndlessMode)
            {
                hpMult += (runTime - 720f) / 60f * 0.25f;
            }

            health.SetMaxHealth(health.MaxHealth * hpMult);
        }
    }
}
