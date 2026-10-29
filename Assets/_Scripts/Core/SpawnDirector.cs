using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CursedHarvest
{
    public enum WaveState
    {
        Preparing,
        Spawning,
        Combat,
        Cleared,
        Vacuuming,
        Intermission
    }

    public class SpawnDirector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private ObjectPool gruntPool;
        [SerializeField] private ObjectPool chaserPool;
        [SerializeField] private ObjectPool plaguePool;
        [SerializeField] private ObjectPool chargerPool;
        [SerializeField] private ObjectPool farmerPool;

        [Header("Elites & Boss Prefabs")]
        [SerializeField] private GameObject scarecrowPrefab;
        [SerializeField] private GameObject brutePrefab;
        [SerializeField] private GameObject bossPrefab;
        [SerializeField] private GameObject ashenReaperBossPrefab;
        [SerializeField] private GameObject mireMotherBossPrefab;
        [SerializeField] private GameObject timberGoliathBossPrefab;
        [SerializeField] private GameObject bloodwoodSovereignBossPrefab;
        [SerializeField] private GameObject eclipseHeraldBossPrefab;
        [SerializeField] private GameObject primevalHorrorBossPrefab;
        [SerializeField] private GameObject healthPickupPrefab;

        [Header("Spawn Boundaries")]
        [SerializeField] private float minSpawnRadius = 18f;
        [SerializeField] private float maxSpawnRadius = 26f;
        [SerializeField] private float arenaHalfBounds = 31f;

        [Header("Wave Progression")]
        [SerializeField] private int currentWave = 1;
        [SerializeField] private WaveState currentState = WaveState.Preparing;
        [SerializeField] private float intermissionDuration = 12f;
        [SerializeField] private int maxConcurrentEnemies = 70;
        [SerializeField] private float spawnInterval = 0.55f;

        [Header("Difficulty Curves")]
        [SerializeField] private float runTime;
        [SerializeField] private bool isRunning = true;
        [SerializeField] private bool isEndlessMode = false;
        [SerializeField] private float postWave10Growth = 1.12f;

        [Header("Boss Wave 10 Configuration")]
        [SerializeField] private int bossWaveAddCount = 18;

        private int bossGruntsRemaining;
        private int bossChasersRemaining;
        private int bossWalkersRemaining;
        private int bossFarmersRemaining;
        private int bossChargersRemaining;

        private int totalEnemiesForWave;
        private int spawnedThisWave;
        private int enemiesKilledThisWave;
        private float lastSpawnTime;
        private float intermissionTimer;
        private float vacuumTimer;
        private bool bossSpawned = false;

        private bool isBossWave = false;
        private bool isBossIntro = false;
        private bool isBossAlive = false;
        private bool bossDefeatedThisWave = false;
        private GameObject activeBossObj = null;
        private EnemyHealth activeBossHealth = null;
        private Coroutine bossIntroCoroutine = null;

        public static readonly int[] WaveCounts = new int[] { 8, 11, 14, 18, 22, 27, 33, 40, 48, 18 };

        public WaveState CurrentState => currentState;
        public int CurrentWave => currentWave;
        public int TotalEnemiesForCurrentWave => totalEnemiesForWave;
        public int SpawnedThisWave => spawnedThisWave;
        public int EnemiesKilledThisWave => enemiesKilledThisWave;
        public int EnemiesRemainingThisWave => Mathf.Max(0, totalEnemiesForWave - enemiesKilledThisWave);
        public float IntermissionTimeRemaining => intermissionTimer;
        public float RunTime => runTime;
        public bool IsEndless => isEndlessMode;
        public int TotalActiveEnemies => EnemyRegistry.Count;
        public float PostWave10Growth => postWave10Growth;
        public void SetPostWave10Growth(float growth) => postWave10Growth = growth;
        public bool IsBossWave => isBossWave;
        public bool IsBossIntro => isBossIntro;
        public bool IsBossAlive => isBossAlive;
        public bool BossDefeatedThisWave => bossDefeatedThisWave;
        public bool BossSpawned => bossSpawned;
        public int BossWaveAddCount => bossWaveAddCount;
        public void SetBossWaveAddCount(int count) => bossWaveAddCount = count;
        public int BossGruntsRemaining => bossGruntsRemaining;
        public int BossChasersRemaining => bossChasersRemaining;
        public int BossWalkersRemaining => bossWalkersRemaining;
        public int BossFarmersRemaining => bossFarmersRemaining;
        public int BossChargersRemaining => bossChargersRemaining;
        public GameObject ActiveBossObject => activeBossObj;
        public GameObject HealthPickupPrefab => healthPickupPrefab;
        public WorldConfig ActiveWorldConfig => (GameSession.Instance != null && GameSession.Instance.CurrentWorldConfig != null) ? GameSession.Instance.CurrentWorldConfig : WorldConfig.GetConfig(1);

        public static event Action<float> OnRunTimerUpdated;
        public static event Action<int, int, int> OnWaveUpdated; // (wave, remaining, total)
        public static event Action<int> OnWaveStarted; // (wave)
        public static event Action<int> OnWaveCleared; // (wave)
        public static event Action<float> OnIntermissionTick; // (remainingSeconds)
        public static event Action OnBossTimeReached;
        public static event Action<int> OnMilestoneReached;

        private void Awake()
        {
            if (playerTransform == null)
            {
                GameObject p = GameObject.FindWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }
        }

        private void OnEnable()
        {
            BossController.OnBossDefeated -= HandleBossDefeated;
            BossController.OnBossDefeated += HandleBossDefeated;

            EnemyHealth.OnAnyEnemyDied -= HandleEnemyDied;
            EnemyHealth.OnAnyEnemyDied += HandleEnemyDied;
        }

        private void OnDisable()
        {
            BossController.OnBossDefeated -= HandleBossDefeated;
            EnemyHealth.OnAnyEnemyDied -= HandleEnemyDied;
        }

        public void InitializeDirector()
        {
            BossController.OnBossDefeated -= HandleBossDefeated;
            BossController.OnBossDefeated += HandleBossDefeated;

            EnemyHealth.OnAnyEnemyDied -= HandleEnemyDied;
            EnemyHealth.OnAnyEnemyDied += HandleEnemyDied;
        }

        private void HandleBossDefeated(BossController boss)
        {
            isBossAlive = false;
            bossDefeatedThisWave = true;
            AudioManager.Instance?.PlayGameplayMusic(2.0f);
            CheckWaveCompletion();
        }

        public void SetBossPrefab(GameObject prefab) => bossPrefab = prefab;
        public void SetAshenReaperBossPrefab(GameObject prefab) => ashenReaperBossPrefab = prefab;
        public GameObject AshenReaperBossPrefab => ashenReaperBossPrefab;
        public void SetMireMotherBossPrefab(GameObject prefab) => mireMotherBossPrefab = prefab;
        public GameObject MireMotherBossPrefab => mireMotherBossPrefab;
        public void SetTimberGoliathBossPrefab(GameObject prefab) => timberGoliathBossPrefab = prefab;
        public GameObject TimberGoliathBossPrefab => timberGoliathBossPrefab;
        public void SetBloodwoodSovereignBossPrefab(GameObject prefab) => bloodwoodSovereignBossPrefab = prefab;
        public GameObject BloodwoodSovereignBossPrefab => bloodwoodSovereignBossPrefab;
        public void SetElitePrefabs(GameObject scarecrow, GameObject brute)
        {
            scarecrowPrefab = scarecrow;
            brutePrefab = brute;
        }
        public void SetHealthPickupPrefab(GameObject healthPrefab) => healthPickupPrefab = healthPrefab;

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

        public static int GetEnemyCountForWave(int wave, float growth = 1.12f)
        {
            return WorldDifficultyConfig.Current.GetEnemyCountForWave(wave, growth);
        }

        public void ResetDirector()
        {
            runTime = 0f;
            lastSpawnTime = 0f;
            isEndlessMode = false;
            isRunning = true;
            bossSpawned = false;
            isBossWave = false;
            isBossIntro = false;
            isBossAlive = false;
            bossDefeatedThisWave = false;
            activeBossObj = null;
            activeBossHealth = null;
            if (bossIntroCoroutine != null)
            {
                StopCoroutine(bossIntroCoroutine);
                bossIntroCoroutine = null;
            }
            bossGruntsRemaining = WorldDifficultyConfig.Current.bossGruntQuota;
            bossChasersRemaining = WorldDifficultyConfig.Current.bossChaserQuota;
            bossWalkersRemaining = WorldDifficultyConfig.Current.bossWalkerQuota;
            bossFarmersRemaining = WorldDifficultyConfig.Current.bossFarmerQuota;
            bossChargersRemaining = WorldDifficultyConfig.Current.bossChargerQuota;
            currentWave = 1;
            currentState = WaveState.Preparing;
            intermissionTimer = 0f;
            vacuumTimer = 0f;
            spawnedThisWave = 0;
            enemiesKilledThisWave = 0;
            totalEnemiesForWave = GetEnemyCountForWave(1, postWave10Growth);
        }

        public void EnterEndlessMode()
        {
            isEndlessMode = true;
            isRunning = true;
            maxConcurrentEnemies = 100;
            spawnInterval = 0.35f;
        }

        // v1.1.2: boss prewarm - the boss is instantiated (inactive) one wave early so the boss spawn frame does not hitch.
        private GameObject prewarmedBoss;
        private GameObject prewarmedBossPrefab;

        private void PrewarmBossForWave(int bossWave)
        {
            if (prewarmedBoss != null) return;
            BossEncounterConfig enc = ActiveWorldConfig != null ? ActiveWorldConfig.GetBossEncounter(bossWave) : null;
            SpawnBoss(enc, true);
        }

        private void OnDestroy()
        {
            if (prewarmedBoss != null) Destroy(prewarmedBoss);
        }

        public GameObject SpawnBoss(BossEncounterConfig bossEnc = null, bool prewarmOnly = false)
        {
            GameObject prefabToSpawn = bossPrefab;
            if (ActiveWorldConfig != null && ActiveWorldConfig.worldId == 2)
            {
                if (ashenReaperBossPrefab != null)
                {
                    prefabToSpawn = ashenReaperBossPrefab;
                }
#if UNITY_EDITOR
                else
                {
                    GameObject loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Boss_TheAshenReaper.prefab");
                    if (loaded != null) prefabToSpawn = loaded;
                }
#endif
            }
            else if (ActiveWorldConfig != null && ActiveWorldConfig.worldId == 3)
            {
                if (mireMotherBossPrefab != null)
                {
                    prefabToSpawn = mireMotherBossPrefab;
                }
#if UNITY_EDITOR
                else
                {
                    GameObject loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Boss_TheMireMother.prefab");
                    if (loaded != null) prefabToSpawn = loaded;
                }
#endif
            }
            else if (ActiveWorldConfig != null && ActiveWorldConfig.worldId == 4)
            {
                if (bossEnc != null && bossEnc.waveNumber == 10)
                {
                    if (timberGoliathBossPrefab != null) prefabToSpawn = timberGoliathBossPrefab;
#if UNITY_EDITOR
                    else
                    {
                        GameObject loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Boss_TimberGoliath.prefab");
                        if (loaded != null) prefabToSpawn = loaded;
                    }
#endif
                }
                else if (bossEnc != null && bossEnc.waveNumber == 15)
                {
                    if (bloodwoodSovereignBossPrefab != null) prefabToSpawn = bloodwoodSovereignBossPrefab;
#if UNITY_EDITOR
                    else
                    {
                        GameObject loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Boss_BloodwoodSovereign.prefab");
                        if (loaded != null) prefabToSpawn = loaded;
                    }
#endif
                }
                else if (bossEnc != null && !string.IsNullOrEmpty(bossEnc.bossPrefabPath))
                {
#if UNITY_EDITOR
                    GameObject encPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(bossEnc.bossPrefabPath);
                    if (encPrefab != null) prefabToSpawn = encPrefab;
#endif
                }
            }
            else if (ActiveWorldConfig != null && ActiveWorldConfig.worldId == 5)
            {
                if (bossEnc != null && bossEnc.waveNumber == 10)
                {
                    if (eclipseHeraldBossPrefab != null) prefabToSpawn = eclipseHeraldBossPrefab;
#if UNITY_EDITOR
                    else
                    {
                        GameObject loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Boss_EclipseHerald.prefab");
                        if (loaded != null) prefabToSpawn = loaded;
                    }
#endif
                }
                else if (bossEnc != null && bossEnc.waveNumber == 20)
                {
                    if (primevalHorrorBossPrefab != null) prefabToSpawn = primevalHorrorBossPrefab;
#if UNITY_EDITOR
                    else
                    {
                        GameObject loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Boss_PrimevalHorror.prefab");
                        if (loaded != null) prefabToSpawn = loaded;
                    }
#endif
                }
                else if (bossEnc != null && !string.IsNullOrEmpty(bossEnc.bossPrefabPath))
                {
#if UNITY_EDITOR
                    GameObject encPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(bossEnc.bossPrefabPath);
                    if (encPrefab != null) prefabToSpawn = encPrefab;
#endif
                }
            }
            else if (bossEnc != null && !string.IsNullOrEmpty(bossEnc.bossPrefabPath))
            {
#if UNITY_EDITOR
                GameObject encPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(bossEnc.bossPrefabPath);
                if (encPrefab != null) prefabToSpawn = encPrefab;
#endif
            }

            if (prefabToSpawn == null) return null;
            bool isPortrait = TopDownCameraFollow.Instance != null ? TopDownCameraFollow.Instance.IsPortrait : (Screen.height >= Screen.width);
            float forwardDist = isPortrait ? 11.2f : 16f;
            Vector3 spawnPos = playerTransform != null ? playerTransform.position + Vector3.forward * forwardDist : new Vector3(0f, 0.5f, forwardDist);
            spawnPos.x = Mathf.Clamp(spawnPos.x, -arenaHalfBounds + 4f, arenaHalfBounds - 4f);
            spawnPos.z = Mathf.Clamp(spawnPos.z, -arenaHalfBounds + 4f, arenaHalfBounds - 4f);
            spawnPos.y = 0.5f;

            GameObject bossObj;
            if (prewarmOnly)
            {
                prewarmedBoss = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
                prewarmedBoss.SetActive(false);
                prewarmedBossPrefab = prefabToSpawn;
                return null;
            }

            if (prewarmedBoss != null && prewarmedBossPrefab == prefabToSpawn)
            {
                bossObj = prewarmedBoss;
                bossObj.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
                bossObj.SetActive(true);
                prewarmedBoss = null;
                prewarmedBossPrefab = null;
            }
            else
            {
                if (prewarmedBoss != null) { Destroy(prewarmedBoss); prewarmedBoss = null; prewarmedBossPrefab = null; }
                bossObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
            }
            if (bossEnc != null)
            {
                if (bossEnc.bossScale != 1.0f) bossObj.transform.localScale = Vector3.one * bossEnc.bossScale;
                BossController bossCtrl = bossObj.GetComponent<BossController>();
                if (bossCtrl != null) bossCtrl.Initialize();
                EnemyHealth eh = bossObj.GetComponent<EnemyHealth>();
                if (eh != null && bossEnc.bossHealthMultiplier != 1.0f)
                {
                    eh.SetMaxHealth(eh.MaxHealth * bossEnc.bossHealthMultiplier);
                }
            }
            else
            {
                BossController boss = bossObj.GetComponent<BossController>();
                if (boss != null) boss.Initialize();
            }
            return bossObj;
        }

        public void StartWave(int wave)
        {
            currentWave = wave;
            WorldConfig cfg = ActiveWorldConfig;
            isBossWave = cfg.IsBossWave(wave);
            BossEncounterConfig bossEnc = isBossWave ? cfg.GetBossEncounter(wave) : null;

            if (isBossWave && bossEnc != null)
            {
                bossWaveAddCount = bossEnc.bossAddCount;
                totalEnemiesForWave = bossWaveAddCount;
            }
            else
            {
                totalEnemiesForWave = cfg.GetEnemyCountForWave(wave);
            }

            spawnedThisWave = 0;
            enemiesKilledThisWave = 0;
            lastSpawnTime = Time.time;
            currentState = WaveState.Spawning;

            OnWaveStarted?.Invoke(currentWave);
            OnWaveUpdated?.Invoke(currentWave, EnemiesRemainingThisWave, totalEnemiesForWave);

            // Prewarm the next boss during the wave before it (Wave 9 -> W1/W2 boss, Wave 9 -> W3 mid-boss, Wave 14 -> W3 final)
            if (!isBossWave && cfg.IsBossWave(wave + 1)) PrewarmBossForWave(wave + 1);

            if (isBossWave)
            {
                StartBossWave(bossEnc);
            }
            else
            {
                isBossIntro = false;
                isBossAlive = false;
                bossDefeatedThisWave = false;
                SpawnMilestoneEnemy(currentWave);
            }
        }

        private void StartBossWave(BossEncounterConfig bossEnc = null)
        {
            isBossIntro = true;
            isBossAlive = false;
            bossDefeatedThisWave = false;
            lastSpawnTime = Time.time;

            if (bossEnc != null)
            {
                bossGruntsRemaining = bossEnc.bossGruntQuota;
                bossChasersRemaining = bossEnc.bossChaserQuota;
                bossWalkersRemaining = bossEnc.bossWalkerQuota;
                bossFarmersRemaining = bossEnc.bossFarmerQuota;
                bossChargersRemaining = bossEnc.bossChargerQuota;

                string badge = bossEnc.isFinalBoss ? "FINAL BOSS INCOMING" : "MID-BOSS INCOMING";
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowToast(badge, bossEnc.bossName, 3.5f, new Color(1f, 0.25f, 0.25f));
                }
            }
            else
            {
                bossGruntsRemaining = WorldDifficultyConfig.Current.bossGruntQuota;
                bossChasersRemaining = WorldDifficultyConfig.Current.bossChaserQuota;
                bossWalkersRemaining = WorldDifficultyConfig.Current.bossWalkerQuota;
                bossFarmersRemaining = WorldDifficultyConfig.Current.bossFarmerQuota;
                bossChargersRemaining = WorldDifficultyConfig.Current.bossChargerQuota;

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowToast("FINAL BOSS INCOMING", "THE LAST HARVEST", 3.5f, new Color(1f, 0.25f, 0.25f));
                }
            }

            AudioManager.Instance?.PlayBossMusic(1.5f);

            if (bossIntroCoroutine != null) StopCoroutine(bossIntroCoroutine);
            bossIntroCoroutine = StartCoroutine(BossIntroRoutine(1.2f, bossEnc));
        }

        private IEnumerator BossIntroRoutine(float duration, BossEncounterConfig bossEnc = null)
        {
            yield return new WaitForSeconds(duration);
            isBossIntro = false;

            if (!bossSpawned)
            {
                bossSpawned = true;
                activeBossObj = SpawnBoss(bossEnc);
                if (activeBossObj != null)
                {
                    activeBossHealth = activeBossObj.GetComponent<EnemyHealth>();
                    isBossAlive = true;
                }
                OnBossTimeReached?.Invoke();
            }
        }

        private void SpawnMilestoneEnemy(int wave)
        {
            // Milestone exception: NO automatic extra elites on any configured boss wave!
            if (ActiveWorldConfig.IsBossWave(wave)) return;

            if (wave % 10 == 0)
            {
                // Waves 20, 30, etc.
                GameObject elitePrefab = brutePrefab != null ? brutePrefab : scarecrowPrefab;
                if (elitePrefab != null)
                {
                    Vector3 spawnPos = CalculateSpawnPosition();
                    GameObject eliteObj = Instantiate(elitePrefab, spawnPos, Quaternion.identity);
                    EnemyController enemy = eliteObj.GetComponent<EnemyController>();
                    if (enemy != null)
                    {
                        enemy.SetTarget(playerTransform);
                        ApplyStatScaling(enemy);
                    }
                }
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowToast($"WAVE {wave}: CURSED SURGE", "Elite horde approaching!", 3.5f, new Color(1f, 0.35f, 0.2f));
                }
                OnMilestoneReached?.Invoke(wave);
            }
            else if (wave % 5 == 0)
            {
                GameObject elitePrefab = (wave % 10 == 5) ? (scarecrowPrefab != null ? scarecrowPrefab : brutePrefab) : brutePrefab;
                if (elitePrefab != null)
                {
                    Vector3 spawnPos = CalculateSpawnPosition();
                    GameObject eliteObj = Instantiate(elitePrefab, spawnPos, Quaternion.identity);
                    EnemyController enemy = eliteObj.GetComponent<EnemyController>();
                    if (enemy != null)
                    {
                        enemy.SetTarget(playerTransform);
                        ApplyStatScaling(enemy);
                    }
                }
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowToast("ELITE APPROACHING", "An elite fiend has arisen!", 3.0f, new Color(1f, 0.8f, 0.2f));
                }
                OnMilestoneReached?.Invoke(wave);
            }
        }

        private void HandleEnemyDied(EnemyHealth enemyHealth, DamageData damage)
        {
            if (currentState == WaveState.Spawning || currentState == WaveState.Combat)
            {
                enemiesKilledThisWave++;
                OnWaveUpdated?.Invoke(currentWave, EnemiesRemainingThisWave, totalEnemiesForWave);

                CheckWaveCompletion();
            }
        }

        private void CheckWaveCompletion()
        {
            if (currentState != WaveState.Spawning && currentState != WaveState.Combat) return;

            // In boss wave: requires boss dead, all wave adds spawned, all adds killed, and zero active enemies left
            if (isBossWave)
            {
                if (bossDefeatedThisWave && spawnedThisWave >= totalEnemiesForWave && enemiesKilledThisWave >= totalEnemiesForWave && EnemyRegistry.Count == 0)
                {
                    CompleteWave();
                }
            }
            else
            {
                if (spawnedThisWave >= totalEnemiesForWave && enemiesKilledThisWave >= totalEnemiesForWave && EnemyRegistry.Count == 0)
                {
                    CompleteWave();
                }
            }
        }

        private void CompleteWave()
        {
            currentState = WaveState.Cleared;
            OnWaveCleared?.Invoke(currentWave);

            currentState = WaveState.Vacuuming;
            vacuumTimer = 0.8f;
            TriggerWaveVacuum();
        }

        private void TriggerWaveVacuum()
        {
            if (playerTransform != null)
            {
                CollectibleRegistry.TriggerVacuumAll(playerTransform, 0.8f);
            }
        }

        private void HandleBossWaveCleared()
        {
            isBossWave = false;
            WorldConfig activeConfig = ActiveWorldConfig;
            BossEncounterConfig currentBossEnc = activeConfig.GetBossEncounter(currentWave);
            bool isFinal = currentBossEnc != null ? currentBossEnc.isFinalBoss : (currentWave >= activeConfig.totalWaves);

            if (isFinal)
            {
                // Final Boss defeated! Complete arena & world!
                if (GameSession.Instance != null)
                {
                    GameSession.Instance.CompleteArena(GameSession.Instance.SelectedArenaId);
                }

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowVictoryModal();
                }
                else
                {
                    EnterEndlessMode();
                    StartIntermission();
                }
            }
            else
            {
                // Mid-Boss defeated! Transition to second phase (Waves 11–15)
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.HideBossBar();
                    UIManager.Instance.ShowToast("PHASE II: CATACLYSM AWAKENS", "The Mid-Boss has fallen! Waves 11-15 incoming!", 4.0f, new Color(0.95f, 0.35f, 0.90f));
                }
                AudioManager.Instance?.PlaySound(SoundType.BombExplode);
                StartIntermission();
            }
        }

        public void StartIntermissionAfterBoss()
        {
            StartIntermission();
        }

        private void StartIntermission()
        {
            currentState = WaveState.Intermission;
            intermissionTimer = intermissionDuration;

            // Supply Drop & Pre-Boss Recovery logic
            float playerHpPct = 1f;
            if (playerTransform != null)
            {
                PlayerHealth ph = playerTransform.GetComponent<PlayerHealth>();
                if (ph != null && ph.MaxHealth > 0f)
                {
                    playerHpPct = ph.CurrentHealth / ph.MaxHealth;
                }
            }

            int supplyCount = WorldDifficultyConfig.Current.GetSupplyDropCount(currentWave, playerHpPct);
            if (supplyCount > 0)
            {
                SpawnGuaranteedHealthDrop(supplyCount);
            }

            OnIntermissionTick?.Invoke(intermissionTimer);
        }

        public List<GameObject> SpawnGuaranteedHealthDrop(int count = 1)
        {
            List<GameObject> spawned = new List<GameObject>();
            if (playerTransform == null) return spawned;

            for (int i = 0; i < count; i++)
            {
                Vector2 dir2D = UnityEngine.Random.insideUnitCircle.normalized;
                if (dir2D.sqrMagnitude < 0.01f) dir2D = Vector2.up;
                float dist = UnityEngine.Random.Range(2.5f, 3.8f) + (i * 0.8f);
                Vector3 spawnPos = playerTransform.position + new Vector3(dir2D.x * dist, 0f, dir2D.y * dist);

                // Ground raycast
                Ray ray = new Ray(spawnPos + Vector3.up * 8f, Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hit, 16f))
                {
                    spawnPos.y = hit.point.y + 0.35f;
                }
                else
                {
                    spawnPos.y = 0.45f;
                }

                GameObject healthObj = null;
                if (GameManager.Instance != null && GameManager.Instance.HealthPool != null)
                {
                    healthObj = GameManager.Instance.HealthPool.Get(spawnPos, Quaternion.identity);
                }
                else if (healthPickupPrefab != null)
                {
                    healthObj = Instantiate(healthPickupPrefab, spawnPos, Quaternion.identity);
                }

                if (healthObj != null)
                {
                    HealthRestorePickup hr = healthObj.GetComponent<HealthRestorePickup>();
                    if (hr != null) hr.Initialize(20f);
                    spawned.Add(healthObj);
                }
            }

            if (UIManager.Instance != null)
            {
                if (currentWave == WorldDifficultyConfig.Current.preBossWave && count > 1)
                {
                    UIManager.Instance.ShowToast("PRE-BOSS RECOVERY", "2x Health Restores dropped for the Boss fight!", 3.5f, new Color(0.35f, 0.95f, 0.45f));
                }
                else
                {
                    UIManager.Instance.ShowToast("SUPPLY DROP", "Guaranteed Health Restore (+20 HP)", 3.0f, new Color(0.35f, 0.95f, 0.45f));
                }
            }

            return spawned;
        }

        private float lastBroadcastTimerSecond = -1f;

        private void Update()
        {
            if (!isRunning) return;

            runTime += Time.deltaTime;
            int currentSecond = (int)runTime;
            if (currentSecond != (int)lastBroadcastTimerSecond)
            {
                lastBroadcastTimerSecond = currentSecond;
                OnRunTimerUpdated?.Invoke(runTime);
            }

            switch (currentState)
            {
                case WaveState.Preparing:
                    StartWave(currentWave);
                    break;

                case WaveState.Spawning:
                    float effectiveInterval = isBossWave ? WorldDifficultyConfig.Current.bossAddSpawnInterval : spawnInterval;
                    if (Time.time >= lastSpawnTime + effectiveInterval)
                    {
                        lastSpawnTime = Time.time;
                        TrySpawnWaveBatch();
                    }
                    if (spawnedThisWave >= totalEnemiesForWave)
                    {
                        currentState = WaveState.Combat;
                    }
                    break;

                case WaveState.Combat:
                    CheckWaveCompletion();
                    break;

                case WaveState.Cleared:
                    currentState = WaveState.Vacuuming;
                    vacuumTimer = 0.8f;
                    TriggerWaveVacuum();
                    break;

                case WaveState.Vacuuming:
                    vacuumTimer -= Time.deltaTime;
                    if (vacuumTimer <= 0f)
                    {
                        if (isBossWave)
                        {
                            HandleBossWaveCleared();
                        }
                        else
                        {
                            StartIntermission();
                        }
                    }
                    break;

                case WaveState.Intermission:
                    // Modal pause check: If a modal (Level Up, Pause, etc.) is open, intermission timer freezes
                    if (UIManager.Instance != null && UIManager.Instance.IsModalOpen)
                    {
                        return;
                    }

                    intermissionTimer -= Time.deltaTime;
                    OnIntermissionTick?.Invoke(intermissionTimer);

                    if (intermissionTimer <= 0f)
                    {
                        intermissionTimer = 0f;
                        currentWave++;
                        currentState = WaveState.Preparing;
                    }
                    break;
            }
        }

        private void TrySpawnWaveBatch()
        {
            if (playerTransform == null) return;
            if (isBossIntro) return; // Wait during dramatic boss intro
            if (spawnedThisWave >= totalEnemiesForWave) return;

            int currentConcurrentLimit = isBossWave ? WorldDifficultyConfig.Current.bossMaxConcurrentAdds : maxConcurrentEnemies;
            if (EnemyRegistry.Count >= currentConcurrentLimit) return;

            int remainingToSpawn = totalEnemiesForWave - spawnedThisWave;
            int roomInConcurrent = currentConcurrentLimit - EnemyRegistry.Count;
            int batchSize = isBossWave ? 1 : Mathf.Min(UnityEngine.Random.Range(2, 4), Mathf.Min(remainingToSpawn, roomInConcurrent));

            for (int i = 0; i < batchSize; i++)
            {
                if (spawnedThisWave >= totalEnemiesForWave) break;

                ObjectPool chosenPool = isBossWave ? SelectBossWavePool() : SelectPoolForCurrentWave();
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

                if (ActiveWorldConfig != null && ActiveWorldConfig.worldId == 2)
                {
                    ApplyAshenVisualVariant(enemyObj);
                }
                else if (ActiveWorldConfig != null && ActiveWorldConfig.worldId == 3)
                {
                    ApplyMireVisualVariant(enemyObj);
                }
                else if (ActiveWorldConfig != null && ActiveWorldConfig.worldId == 4)
                {
                    ApplyTimberVisualVariant(enemyObj);
                }
                else if (ActiveWorldConfig != null && ActiveWorldConfig.worldId == 5)
                {
                    ApplyEclipseVisualVariant(enemyObj);
                }

                spawnedThisWave++;
            }

            OnWaveUpdated?.Invoke(currentWave, EnemiesRemainingThisWave, totalEnemiesForWave);
        }

        private ObjectPool SelectBossWavePool()
        {
            float hpPct = 1f;
            if (activeBossHealth != null && activeBossHealth.MaxHealth > 0f)
            {
                hpPct = activeBossHealth.CurrentHealth / activeBossHealth.MaxHealth;
            }

            ObjectPool selectedPool = null;

            // Phase 1: Boss > 70% HP -> Grunts (quota 6) & Chasers (quota 4)
            if (hpPct > 0.70f)
            {
                if (bossGruntsRemaining > 0 && bossChasersRemaining > 0)
                {
                    selectedPool = (UnityEngine.Random.value < 0.65f && gruntPool != null) ? gruntPool : (chaserPool != null ? chaserPool : gruntPool);
                }
                else if (bossGruntsRemaining > 0 && gruntPool != null)
                {
                    selectedPool = gruntPool;
                }
                else if (bossChasersRemaining > 0 && chaserPool != null)
                {
                    selectedPool = chaserPool;
                }
            }
            // Phase 2: 70% - 40% HP -> Chasers, Walkers, Farmers
            else if (hpPct > 0.40f)
            {
                List<ObjectPool> p2 = new List<ObjectPool>();
                if (bossWalkersRemaining > 0 && plaguePool != null) p2.Add(plaguePool);
                if (bossFarmersRemaining > 0 && farmerPool != null) p2.Add(farmerPool);
                if (bossChasersRemaining > 0 && chaserPool != null) p2.Add(chaserPool);

                if (p2.Count > 0)
                {
                    selectedPool = p2[UnityEngine.Random.Range(0, p2.Count)];
                }
            }
            // Phase 3: < 40% HP -> Chargers, Farmers, Walkers, Chasers
            else
            {
                List<ObjectPool> p3 = new List<ObjectPool>();
                if (bossChargersRemaining > 0 && chargerPool != null) p3.Add(chargerPool);
                if (bossFarmersRemaining > 0 && farmerPool != null) p3.Add(farmerPool);
                if (bossWalkersRemaining > 0 && plaguePool != null) p3.Add(plaguePool);
                if (bossChasersRemaining > 0 && chaserPool != null) p3.Add(chaserPool);

                if (p3.Count > 0)
                {
                    selectedPool = p3[UnityEngine.Random.Range(0, p3.Count)];
                }
            }

            // Fallback: If phase-preferred quotas are exhausted, select any pool that still has remaining quota
            if (selectedPool == null)
            {
                if (bossGruntsRemaining > 0 && gruntPool != null) selectedPool = gruntPool;
                else if (bossChasersRemaining > 0 && chaserPool != null) selectedPool = chaserPool;
                else if (bossWalkersRemaining > 0 && plaguePool != null) selectedPool = plaguePool;
                else if (bossFarmersRemaining > 0 && farmerPool != null) selectedPool = farmerPool;
                else if (bossChargersRemaining > 0 && chargerPool != null) selectedPool = chargerPool;
                else selectedPool = gruntPool;
            }

            // Decrement the selected quota
            if (selectedPool == gruntPool && bossGruntsRemaining > 0) bossGruntsRemaining--;
            else if (selectedPool == chaserPool && bossChasersRemaining > 0) bossChasersRemaining--;
            else if (selectedPool == plaguePool && bossWalkersRemaining > 0) bossWalkersRemaining--;
            else if (selectedPool == farmerPool && bossFarmersRemaining > 0) bossFarmersRemaining--;
            else if (selectedPool == chargerPool && bossChargersRemaining > 0) bossChargersRemaining--;

            return selectedPool;
        }

        private static MaterialPropertyBlock _ashenPropBlock;
        private static readonly Color AshenSootColor = new Color(0.48f, 0.45f, 0.43f);

        private void ApplyAshenVisualVariant(GameObject enemyObj)
        {
            if (enemyObj == null) return;
            if (_ashenPropBlock == null) _ashenPropBlock = new MaterialPropertyBlock();

            Renderer[] renderers = enemyObj.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] is MeshRenderer || renderers[i] is SkinnedMeshRenderer)
                {
                    renderers[i].GetPropertyBlock(_ashenPropBlock);
                    _ashenPropBlock.SetColor("_BaseColor", AshenSootColor);
                    _ashenPropBlock.SetColor("_Color", AshenSootColor);
                    renderers[i].SetPropertyBlock(_ashenPropBlock);
                }
            }
        }

        private static MaterialPropertyBlock _mirePropBlock;
        private static readonly Color MireSootColor = new Color(0.32f, 0.44f, 0.35f);

        private void ApplyMireVisualVariant(GameObject enemyObj)
        {
            if (enemyObj == null) return;
            if (_mirePropBlock == null) _mirePropBlock = new MaterialPropertyBlock();

            Renderer[] renderers = enemyObj.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] is MeshRenderer || renderers[i] is SkinnedMeshRenderer)
                {
                    renderers[i].GetPropertyBlock(_mirePropBlock);
                    _mirePropBlock.SetColor("_BaseColor", MireSootColor);
                    _mirePropBlock.SetColor("_Color", MireSootColor);
                    renderers[i].SetPropertyBlock(_mirePropBlock);
                }
            }
        }

        private static MaterialPropertyBlock _timberPropBlock;
        private static readonly Color TimberSootColor = new Color(0.42f, 0.28f, 0.22f);

        private void ApplyTimberVisualVariant(GameObject enemyObj)
        {
            if (enemyObj == null) return;
            if (_timberPropBlock == null) _timberPropBlock = new MaterialPropertyBlock();

            Renderer[] renderers = enemyObj.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] is MeshRenderer || renderers[i] is SkinnedMeshRenderer)
                {
                    renderers[i].GetPropertyBlock(_timberPropBlock);
                    _timberPropBlock.SetColor("_BaseColor", TimberSootColor);
                    _timberPropBlock.SetColor("_Color", TimberSootColor);
                    renderers[i].SetPropertyBlock(_timberPropBlock);
                }
            }
        }

        private static MaterialPropertyBlock _eclipsePropBlock;
        private static readonly Color EclipseSootColor = new Color(0.24f, 0.16f, 0.32f);

        private void ApplyEclipseVisualVariant(GameObject enemyObj)
        {
            if (enemyObj == null) return;
            if (_eclipsePropBlock == null) _eclipsePropBlock = new MaterialPropertyBlock();

            Renderer[] renderers = enemyObj.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] is MeshRenderer || renderers[i] is SkinnedMeshRenderer)
                {
                    renderers[i].GetPropertyBlock(_eclipsePropBlock);
                    _eclipsePropBlock.SetColor("_BaseColor", EclipseSootColor);
                    _eclipsePropBlock.SetColor("_Color", EclipseSootColor);
                    renderers[i].SetPropertyBlock(_eclipsePropBlock);
                }
            }
        }

        private ObjectPool SelectPoolForCurrentWave()
        {
            if (ActiveWorldConfig != null && ActiveWorldConfig.worldId == 2)
            {
                return SelectPoolForWorld2();
            }
            if (ActiveWorldConfig != null && ActiveWorldConfig.worldId == 3)
            {
                return SelectPoolForWorld3();
            }
            if (ActiveWorldConfig != null && ActiveWorldConfig.worldId == 4)
            {
                return SelectPoolForWorld4();
            }
            if (ActiveWorldConfig != null && ActiveWorldConfig.worldId == 5)
            {
                return SelectPoolForWorld5();
            }

            if (isEndlessMode || currentWave > 10)
            {
                ObjectPool[] all = new ObjectPool[] { gruntPool, chaserPool, plaguePool, chargerPool, farmerPool };
                List<ObjectPool> valid = new List<ObjectPool>();
                for (int i = 0; i < all.Length; i++) if (all[i] != null) valid.Add(all[i]);
                return valid.Count > 0 ? valid[UnityEngine.Random.Range(0, valid.Count)] : gruntPool;
            }

            if (currentWave <= 1)
            {
                return gruntPool;
            }
            else if (currentWave == 2)
            {
                if (chaserPool != null && UnityEngine.Random.value < 0.30f) return chaserPool;
                return gruntPool;
            }
            else if (currentWave == 3)
            {
                float roll = UnityEngine.Random.value;
                if (plaguePool != null && roll < 0.20f) return plaguePool;
                if (chaserPool != null && roll < 0.50f) return chaserPool;
                return gruntPool;
            }
            else if (currentWave == 4)
            {
                float roll = UnityEngine.Random.value;
                if (chargerPool != null && roll < 0.15f) return chargerPool;
                if (plaguePool != null && roll < 0.35f) return plaguePool;
                if (chaserPool != null && roll < 0.65f) return chaserPool;
                return gruntPool;
            }
            else if (currentWave == 5)
            {
                float roll = UnityEngine.Random.value;
                if (farmerPool != null && roll < 0.10f) return farmerPool;
                if (chargerPool != null && roll < 0.25f) return chargerPool;
                if (plaguePool != null && roll < 0.45f) return plaguePool;
                if (chaserPool != null && roll < 0.70f) return chaserPool;
                return gruntPool;
            }
            else // Waves 6-10
            {
                float roll = UnityEngine.Random.value;
                if (farmerPool != null && roll < 0.20f) return farmerPool;
                if (chargerPool != null && roll < 0.40f) return chargerPool;
                if (plaguePool != null && roll < 0.60f) return plaguePool;
                if (chaserPool != null && roll < 0.80f) return chaserPool;
                return gruntPool;
            }
        }

        private ObjectPool SelectPoolForWorld2()
        {
            if (isEndlessMode || currentWave > 10)
            {
                ObjectPool[] all = new ObjectPool[] { gruntPool, chaserPool, plaguePool, chargerPool, farmerPool };
                List<ObjectPool> valid = new List<ObjectPool>();
                for (int i = 0; i < all.Length; i++) if (all[i] != null) valid.Add(all[i]);
                return valid.Count > 0 ? valid[UnityEngine.Random.Range(0, valid.Count)] : gruntPool;
            }

            if (currentWave <= 1)
            {
                if (chaserPool != null && UnityEngine.Random.value < 0.35f) return chaserPool;
                return gruntPool;
            }
            else if (currentWave == 2)
            {
                if (chaserPool != null && UnityEngine.Random.value < 0.50f) return chaserPool;
                return gruntPool;
            }
            else if (currentWave == 3)
            {
                float roll = UnityEngine.Random.value;
                if (farmerPool != null && roll < 0.25f) return farmerPool;
                if (chaserPool != null && roll < 0.65f) return chaserPool;
                return gruntPool;
            }
            else if (currentWave == 4)
            {
                float roll = UnityEngine.Random.value;
                if (chargerPool != null && roll < 0.25f) return chargerPool;
                if (farmerPool != null && roll < 0.55f) return farmerPool;
                if (chaserPool != null && roll < 0.85f) return chaserPool;
                return gruntPool;
            }
            else if (currentWave == 5)
            {
                float roll = UnityEngine.Random.value;
                if (chargerPool != null && roll < 0.30f) return chargerPool;
                if (farmerPool != null && roll < 0.60f) return farmerPool;
                if (chaserPool != null && roll < 0.85f) return chaserPool;
                return gruntPool;
            }
            else // Waves 6-9
            {
                float roll = UnityEngine.Random.value;
                if (farmerPool != null && roll < 0.35f) return farmerPool;
                if (chargerPool != null && roll < 0.65f) return chargerPool;
                if (chaserPool != null && roll < 0.85f) return chaserPool;
                if (plaguePool != null && roll < 0.95f) return plaguePool;
                return gruntPool;
            }
        }

        private ObjectPool SelectPoolForWorld3()
        {
            if (isEndlessMode || currentWave > 10)
            {
                ObjectPool[] all = new ObjectPool[] { gruntPool, chaserPool, plaguePool, chargerPool, farmerPool };
                List<ObjectPool> valid = new List<ObjectPool>();
                for (int i = 0; i < all.Length; i++) if (all[i] != null) valid.Add(all[i]);
                return valid.Count > 0 ? valid[UnityEngine.Random.Range(0, valid.Count)] : plaguePool;
            }

            if (currentWave <= 1)
            {
                if (plaguePool != null && UnityEngine.Random.value < 0.40f) return plaguePool;
                return gruntPool;
            }
            else if (currentWave == 2)
            {
                float roll = UnityEngine.Random.value;
                if (plaguePool != null && roll < 0.45f) return plaguePool;
                if (chaserPool != null && roll < 0.75f) return chaserPool;
                return gruntPool;
            }
            else if (currentWave == 3)
            {
                float roll = UnityEngine.Random.value;
                if (plaguePool != null && roll < 0.40f) return plaguePool;
                if (chargerPool != null && roll < 0.65f) return chargerPool;
                if (chaserPool != null && roll < 0.85f) return chaserPool;
                return gruntPool;
            }
            else if (currentWave == 4)
            {
                float roll = UnityEngine.Random.value;
                if (plaguePool != null && roll < 0.35f) return plaguePool;
                if (chargerPool != null && roll < 0.65f) return chargerPool;
                if (farmerPool != null && roll < 0.85f) return farmerPool;
                return chaserPool != null ? chaserPool : gruntPool;
            }
            else // Waves 5-9
            {
                float roll = UnityEngine.Random.value;
                if (plaguePool != null && roll < 0.35f) return plaguePool;
                if (chargerPool != null && roll < 0.60f) return chargerPool;
                if (farmerPool != null && roll < 0.80f) return farmerPool;
                if (chaserPool != null && roll < 0.95f) return chaserPool;
                return gruntPool;
            }
        }

        private ObjectPool SelectPoolForWorld4()
        {
            if (isEndlessMode || currentWave > 15)
            {
                ObjectPool[] all = new ObjectPool[] { gruntPool, chaserPool, plaguePool, chargerPool, farmerPool };
                List<ObjectPool> valid = new List<ObjectPool>();
                for (int i = 0; i < all.Length; i++) if (all[i] != null) valid.Add(all[i]);
                return valid.Count > 0 ? valid[UnityEngine.Random.Range(0, valid.Count)] : gruntPool;
            }

            if (currentWave <= 2)
            {
                // Waves 1-2: Grunts and fast Chasers
                if (chaserPool != null && UnityEngine.Random.value < 0.45f) return chaserPool;
                return gruntPool;
            }
            else if (currentWave <= 4)
            {
                // Waves 3-4: Chargers arrive early with Chasers
                float roll = UnityEngine.Random.value;
                if (chargerPool != null && roll < 0.30f) return chargerPool;
                if (chaserPool != null && roll < 0.65f) return chaserPool;
                return gruntPool;
            }
            else if (currentWave <= 7)
            {
                // Waves 5-7: Farmers (axe throwers) and Chargers dominate
                float roll = UnityEngine.Random.value;
                if (farmerPool != null && roll < 0.35f) return farmerPool;
                if (chargerPool != null && roll < 0.65f) return chargerPool;
                if (chaserPool != null && roll < 0.85f) return chaserPool;
                return gruntPool;
            }
            else if (currentWave <= 9)
            {
                // Waves 8-9: Pre-Mid-Boss grueling wave
                float roll = UnityEngine.Random.value;
                if (farmerPool != null && roll < 0.30f) return farmerPool;
                if (chargerPool != null && roll < 0.55f) return chargerPool;
                if (plaguePool != null && roll < 0.80f) return plaguePool;
                return chaserPool != null ? chaserPool : gruntPool;
            }
            else if (currentWave <= 12)
            {
                // Waves 11-12: Post-Mid-Boss high pressure
                float roll = UnityEngine.Random.value;
                if (chargerPool != null && roll < 0.35f) return chargerPool;
                if (farmerPool != null && roll < 0.65f) return farmerPool;
                if (chaserPool != null && roll < 0.85f) return chaserPool;
                return plaguePool != null ? plaguePool : gruntPool;
            }
            else
            {
                // Waves 13-14: Apex timber horde
                float roll = UnityEngine.Random.value;
                if (chargerPool != null && roll < 0.30f) return chargerPool;
                if (farmerPool != null && roll < 0.60f) return farmerPool;
                if (plaguePool != null && roll < 0.80f) return plaguePool;
                return chaserPool != null ? chaserPool : gruntPool;
            }
        }

        private ObjectPool SelectPoolForWorld5()
        {
            if (isEndlessMode || currentWave > 20)
            {
                ObjectPool[] all = new ObjectPool[] { gruntPool, chaserPool, plaguePool, chargerPool, farmerPool };
                List<ObjectPool> valid = new List<ObjectPool>();
                for (int i = 0; i < all.Length; i++) if (all[i] != null) valid.Add(all[i]);
                return valid.Count > 0 ? valid[UnityEngine.Random.Range(0, valid.Count)] : gruntPool;
            }

            if (currentWave <= 3)
            {
                // Waves 1-3: Fast shadow stalkers and grunts
                if (chaserPool != null && UnityEngine.Random.value < 0.50f) return chaserPool;
                return gruntPool;
            }
            else if (currentWave <= 6)
            {
                // Waves 4-6: Walkers emerge with Chasers and Grunts
                float roll = UnityEngine.Random.value;
                if (plaguePool != null && roll < 0.35f) return plaguePool;
                if (chaserPool != null && roll < 0.70f) return chaserPool;
                return gruntPool;
            }
            else if (currentWave <= 9)
            {
                // Waves 7-9: Heavy chargers and throwing cultists before Mid-Boss
                float roll = UnityEngine.Random.value;
                if (chargerPool != null && roll < 0.35f) return chargerPool;
                if (farmerPool != null && roll < 0.65f) return farmerPool;
                if (chaserPool != null && roll < 0.85f) return chaserPool;
                return plaguePool != null ? plaguePool : gruntPool;
            }
            else if (currentWave <= 14)
            {
                // Waves 11-14: Post-Mid-Boss grueling escalation
                float roll = UnityEngine.Random.value;
                if (chargerPool != null && roll < 0.30f) return chargerPool;
                if (farmerPool != null && roll < 0.60f) return farmerPool;
                if (plaguePool != null && roll < 0.80f) return plaguePool;
                return chaserPool != null ? chaserPool : gruntPool;
            }
            else if (currentWave <= 18)
            {
                // Waves 15-18: High intensity nightmare horde
                float roll = UnityEngine.Random.value;
                if (chargerPool != null && roll < 0.35f) return chargerPool;
                if (farmerPool != null && roll < 0.65f) return farmerPool;
                if (plaguePool != null && roll < 0.85f) return plaguePool;
                return chaserPool != null ? chaserPool : gruntPool;
            }
            else
            {
                // Wave 19: Pre-Final Boss apocalypse swarm
                float roll = UnityEngine.Random.value;
                if (chargerPool != null && roll < 0.40f) return chargerPool;
                if (farmerPool != null && roll < 0.70f) return farmerPool;
                if (plaguePool != null && roll < 0.90f) return plaguePool;
                return chaserPool != null ? chaserPool : gruntPool;
            }
        }

        private Vector3 CalculateSpawnPosition()
        {
            Vector3 basePos = playerTransform != null ? playerTransform.position : Vector3.zero;

            if (TopDownCameraFollow.Instance != null && TopDownCameraFollow.Instance.IsPortrait)
            {
                Vector2 halfExtents = TopDownCameraFollow.Instance.GetVisibleGroundHalfExtents(2.2f);
                float halfW = Mathf.Max(8.5f, halfExtents.x);
                float halfH = Mathf.Max(13.5f, halfExtents.y);

                for (int attempt = 0; attempt < 6; attempt++)
                {
                    int side = UnityEngine.Random.Range(0, 4); // 0=Top, 1=Bottom, 2=Left, 3=Right
                    float margin = UnityEngine.Random.Range(0.4f, 3.2f);
                    float dx = 0f;
                    float dz = 0f;

                    switch (side)
                    {
                        case 0: // North (ahead)
                            dx = UnityEngine.Random.Range(-halfW - 1.5f, halfW + 1.5f);
                            dz = halfH + margin;
                            break;
                        case 1: // South (behind)
                            dx = UnityEngine.Random.Range(-halfW - 1.5f, halfW + 1.5f);
                            dz = -(halfH * 0.88f + margin);
                            break;
                        case 2: // West (left)
                            dx = -(halfW + margin);
                            dz = UnityEngine.Random.Range(-halfH * 0.85f, halfH + 1.0f);
                            break;
                        default: // East (right)
                            dx = halfW + margin;
                            dz = UnityEngine.Random.Range(-halfH * 0.85f, halfH + 1.0f);
                            break;
                    }

                    Vector3 candidate = basePos + new Vector3(dx, 0f, dz);
                    candidate.x = Mathf.Clamp(candidate.x, -arenaHalfBounds, arenaHalfBounds);
                    candidate.z = Mathf.Clamp(candidate.z, -arenaHalfBounds, arenaHalfBounds);
                    candidate.y = 0.5f;

                    // If clamping pulled the spawn point inside the camera viewport, pick an unclamped flank if possible
                    if (!TopDownCameraFollow.Instance.IsWorldPositionVisibleInViewport(candidate, -0.02f) || attempt == 5)
                    {
                        return candidate;
                    }
                }
            }

            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float radius = UnityEngine.Random.Range(minSpawnRadius, maxSpawnRadius);
            Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

            Vector3 rawPos = basePos + offset;
            rawPos.x = Mathf.Clamp(rawPos.x, -arenaHalfBounds, arenaHalfBounds);
            rawPos.z = Mathf.Clamp(rawPos.z, -arenaHalfBounds, arenaHalfBounds);
            rawPos.y = 0.5f;

            return rawPos;
        }

        private void ApplyStatScaling(EnemyController enemy)
        {
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health == null) return;

            float hpMult = 1f + (currentWave - 1) * 0.08f;
            if (isEndlessMode)
            {
                hpMult += (currentWave - 10) * 0.15f;
            }

            health.SetMaxHealth(health.MaxHealth * hpMult);
        }

        // ==================== TEST & VERIFICATION METHODS ====================
        public void StartAtWave(int waveNumber)
        {
            currentWave = Mathf.Max(1, waveNumber);
            var active = new List<EnemyController>(EnemyRegistry.ActiveEnemies);
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i] != null)
                {
                    var tracker = active[i].GetComponent<PooledObjectTracker>();
                    if (tracker != null) tracker.ReturnToPool();
                    else active[i].gameObject.SetActive(false);
                }
            }
            StartWave(currentWave);
        }

        public void ForceClearCurrentWave()
        {
            spawnedThisWave = totalEnemiesForWave;
            var active = new List<EnemyController>(EnemyRegistry.ActiveEnemies);
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i] != null)
                {
                    var health = active[i].GetComponent<EnemyHealth>();
                    if (health != null && health.IsAlive)
                    {
                        health.TakeDamage(new DamageData(9999f, null, Vector3.zero, 0f, active[i].transform.position));
                    }
                }
            }
            CompleteWave();
        }

        public void FastForwardIntermission()
        {
            if (currentState == WaveState.Intermission)
            {
                intermissionTimer = 0.05f;
            }
        }
    }
}
