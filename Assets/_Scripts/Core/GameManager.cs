using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CursedHarvest
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager instance;
        public static GameManager Instance
        {
            get
            {
                if (instance == null) instance = FindFirstObjectByType<GameManager>();
                return instance;
            }
        }

        [Header("Scene References")]
        [SerializeField] private PlayerController player;
        [SerializeField] private ObjectPool boltPool;
        [SerializeField] private ObjectPool enemyPool;
        [SerializeField] private ObjectPool xpPool;
        [SerializeField] private ObjectPool seedPool;
        [SerializeField] private TopDownCameraFollow cameraFollow;

        [Header("Phase 1 Test Spawner")]
        [SerializeField] private bool autoSpawnEnemies = true;
        [SerializeField] private int maxTestEnemies = 8;
        [SerializeField] private float spawnInterval = 1.5f;
        [SerializeField] private float spawnRadius = 14f;

        private int enemiesKilled;
        private float lastSpawnTime;

        public PlayerController Player => player;
        public PlayerHealth PlayerHealth => player != null ? player.GetComponent<PlayerHealth>() : null;
        public int EnemiesKilled => enemiesKilled;
        public ObjectPool BoltPool => boltPool;
        public ObjectPool EnemyPool => enemyPool;
        public ObjectPool XpPool => xpPool;
        public ObjectPool SeedPool => seedPool;

        public static event Action<int> OnEnemiesKilledChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
            }

            if (cameraFollow == null)
            {
                cameraFollow = FindFirstObjectByType<TopDownCameraFollow>();
            }

            if (cameraFollow != null && player != null)
            {
                cameraFollow.SetTarget(player.transform);
            }
        }

        private void OnEnable()
        {
            EnemyHealth.OnAnyEnemyDied += HandleEnemyDied;
        }

        private void OnDisable()
        {
            EnemyHealth.OnAnyEnemyDied -= HandleEnemyDied;
        }

        private void Start()
        {
            // Initial burst of test enemies
            if (autoSpawnEnemies && enemyPool != null && player != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    SpawnTestEnemy();
                }
            }
        }

        private void Update()
        {
            if (autoSpawnEnemies && enemyPool != null && player != null)
            {
                if (enemyPool.ActiveCount < maxTestEnemies && Time.time >= lastSpawnTime + spawnInterval)
                {
                    lastSpawnTime = Time.time;
                    SpawnTestEnemy();
                }
            }
        }

        public void SpawnTestEnemy()
        {
            if (enemyPool == null || player == null) return;

            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle.normalized * spawnRadius;
            Vector3 spawnPos = player.transform.position + new Vector3(randomCircle.x, 0.5f, randomCircle.y);

            GameObject enemyObj = enemyPool.Get(spawnPos, Quaternion.identity);
            EnemyController enemy = enemyObj.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.SetTarget(player.transform);
            }
        }

        private void HandleEnemyDied(EnemyHealth enemyHealth, DamageData damage)
        {
            enemiesKilled++;
            OnEnemiesKilledChanged?.Invoke(enemiesKilled);
        }
    }
}
