using System;
using System.Collections.Generic;
using UnityEngine;

namespace CursedHarvest
{
    public enum MetaUpgradeBranch
    {
        Vitality,       // +10 Max HP per rank (1..5: +10, +20, +30, +40, +50)
        Power,          // +8% All Damage per rank (1..5: +8%, +16%, +24%, +32%, +40%)
        Haste,          // +7% Attack Speed / Cooldown reduction (1..5: +7%, +14%, +21%, +28%, +35%)
        Agility,        // +6% Movement Speed per rank (1..5: +6%, +12%, +18%, +24%, +30%)
        Fortune,        // +15% Essence Gain & Drop Luck per rank (1..5: +15%, +30%, +45%, +60%, +75%)
        Survival,       // +0.5 HP/s Passive Regen per rank (1..5: +0.5, +1.0, +1.5, +2.0, +2.5 HP/s)
        LuckyLast,      // +1.5s Lucky Last Duration per rank (1..5: +1.5s, +3.0s, +4.5s, +6.0s, +7.5s)
        HarvestMastery  // +10% Harvest Channel Speed per rank (1..5: +10%, +20%, +30%, +40%, +50%)
    }

    [Serializable]
    public class MetaUpgradeDef
    {
        public MetaUpgradeBranch branch;
        public string displayName;
        public string descriptionFormat;
        public string iconGlyph;
        public int maxRank = 5;
        public int[] costs = new int[] { 50, 100, 175, 275, 400 };

        public int GetCost(int currentRank)
        {
            if (currentRank >= maxRank) return -1;
            if (currentRank < costs.Length) return costs[currentRank];
            return costs[costs.Length - 1];
        }

        public string GetDescriptionForRank(int rank)
        {
            switch (branch)
            {
                case MetaUpgradeBranch.Vitality:
                    return $"+{rank * 10} Maximum Health";
                case MetaUpgradeBranch.Power:
                    return $"+{rank * 8}% All Weapon Damage";
                case MetaUpgradeBranch.Haste:
                    return $"+{rank * 7}% Attack Speed";
                case MetaUpgradeBranch.Agility:
                    return $"+{rank * 6}% Movement Speed";
                case MetaUpgradeBranch.Fortune:
                    return $"+{rank * 15}% Essence Gain & Drop Luck";
                case MetaUpgradeBranch.Survival:
                    return $"+{(rank * 0.5f):F1} HP/sec Passive Regeneration";
                case MetaUpgradeBranch.LuckyLast:
                    return $"+{(rank * 1.5f):F1}s Lucky Last Invulnerability";
                case MetaUpgradeBranch.HarvestMastery:
                    return $"+{rank * 10}% Faster Harvest Channeling";
                default:
                    return "";
            }
        }
    }

    public class MetaProgressionManager : MonoBehaviour
    {
        private static MetaProgressionManager _instance;
        public static MetaProgressionManager Instance
        {
            get
            {
                if (_instance == null || !_instance)
                {
                    _instance = FindFirstObjectByType<MetaProgressionManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("MetaProgressionManager");
                        _instance = go.AddComponent<MetaProgressionManager>();
                        if (Application.isPlaying) DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private const string PrefsEssenceKey = "CH_HarvestEssence";
        private const string PrefsSaveVersionKey = "CH_SaveVersion";
        private const int CurrentSaveVersion = 3;

        private int harvestEssence = 0;
        private readonly Dictionary<MetaUpgradeBranch, int> branchRanks = new Dictionary<MetaUpgradeBranch, int>();
        private readonly Dictionary<MetaUpgradeBranch, MetaUpgradeDef> branchDefs = new Dictionary<MetaUpgradeBranch, MetaUpgradeDef>();

        // Current Run Summary Trackers
        private int runEssenceEarned = 0;
        private int runEnemiesSlain = 0;
        private int runBossesDefeated = 0;
        private int runHarvestsCompleted = 0;

        public int HarvestEssence => harvestEssence;
        public int RunEssenceEarned => runEssenceEarned;
        public int RunEnemiesSlain => runEnemiesSlain;
        public int RunBossesDefeated => runBossesDefeated;
        public int RunHarvestsCompleted => runHarvestsCompleted;

        public static event Action<int> OnEssenceChanged;
        public static event Action<MetaUpgradeBranch, int> OnUpgradePurchased;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            InitializeDefinitions();
            LoadProgression();
        }

        private void OnEnable()
        {
            EnemyHealth.OnAnyEnemyDied -= HandleEnemyDied;
            EnemyHealth.OnAnyEnemyDied += HandleEnemyDied;

            BossController.OnBossDefeated -= HandleBossDefeated;
            BossController.OnBossDefeated += HandleBossDefeated;

            HarvestPlot.OnHarvestGambleResolved -= HandleHarvestResolved;
            HarvestPlot.OnHarvestGambleResolved += HandleHarvestResolved;
        }

        private void OnDisable()
        {
            EnemyHealth.OnAnyEnemyDied -= HandleEnemyDied;
            BossController.OnBossDefeated -= HandleBossDefeated;
            HarvestPlot.OnHarvestGambleResolved -= HandleHarvestResolved;
        }

        private void InitializeDefinitions()
        {
            branchDefs.Clear();
            AddDef(MetaUpgradeBranch.Vitality, "Vitality", "+10 Max Health per rank", "♥");
            AddDef(MetaUpgradeBranch.Power, "Power", "+8% Weapon Damage per rank", "⚔");
            AddDef(MetaUpgradeBranch.Haste, "Haste", "+7% Attack Speed per rank", "⚡");
            AddDef(MetaUpgradeBranch.Agility, "Agility", "+6% Movement Speed per rank", "👟");
            AddDef(MetaUpgradeBranch.Fortune, "Fortune", "+15% Essence Gain & Drop Luck", "✦");
            AddDef(MetaUpgradeBranch.Survival, "Survival", "+0.5 HP/s Passive Health Regen", "✚");
            AddDef(MetaUpgradeBranch.LuckyLast, "Lucky Last", "+1.5s Lucky Last Duration", "★");
            AddDef(MetaUpgradeBranch.HarvestMastery, "Harvest", "+10% Harvest Channel Speed", "🌾");
        }

        private void AddDef(MetaUpgradeBranch branch, string name, string descFormat, string glyph)
        {
            branchDefs[branch] = new MetaUpgradeDef
            {
                branch = branch,
                displayName = name,
                descriptionFormat = descFormat,
                iconGlyph = glyph,
                maxRank = 5,
                costs = new int[] { 50, 100, 175, 275, 400 }
            };
        }

        public MetaUpgradeDef GetDef(MetaUpgradeBranch branch)
        {
            if (branchDefs.Count == 0) InitializeDefinitions();
            return branchDefs.TryGetValue(branch, out var def) ? def : null;
        }

        public int GetRank(MetaUpgradeBranch branch)
        {
            return branchRanks.TryGetValue(branch, out int rank) ? rank : 0;
        }

        public void LoadProgression()
        {
            int version = PlayerPrefs.GetInt(PrefsSaveVersionKey, CurrentSaveVersion);
            harvestEssence = PlayerPrefs.GetInt(PrefsEssenceKey, 0);

            branchRanks.Clear();
            foreach (MetaUpgradeBranch branch in Enum.GetValues(typeof(MetaUpgradeBranch)))
            {
                int r = PlayerPrefs.GetInt($"CH_MetaUpgrade_{branch}", 0);
                branchRanks[branch] = Mathf.Clamp(r, 0, 5);
            }
        }

        public void SaveProgression()
        {
            PlayerPrefs.SetInt(PrefsSaveVersionKey, CurrentSaveVersion);
            PlayerPrefs.SetInt(PrefsEssenceKey, harvestEssence);
            foreach (var kvp in branchRanks)
            {
                PlayerPrefs.SetInt($"CH_MetaUpgrade_{kvp.Key}", kvp.Value);
            }
            PlayerPrefs.Save();
        }

        public void ResetAllProgression()
        {
            harvestEssence = 0;
            branchRanks.Clear();
            foreach (MetaUpgradeBranch branch in Enum.GetValues(typeof(MetaUpgradeBranch)))
            {
                branchRanks[branch] = 0;
            }
            SaveProgression();
            OnEssenceChanged?.Invoke(harvestEssence);
        }

        public void ResetRunStats()
        {
            runEssenceEarned = 0;
            runEnemiesSlain = 0;
            runBossesDefeated = 0;
            runHarvestsCompleted = 0;
        }

        public bool TryUpgradeBranch(MetaUpgradeBranch branch)
        {
            MetaUpgradeDef def = GetDef(branch);
            if (def == null) return false;

            int currentRank = GetRank(branch);
            if (currentRank >= def.maxRank) return false;

            int cost = def.GetCost(currentRank);
            if (harvestEssence < cost) return false;

            harvestEssence -= cost;
            branchRanks[branch] = currentRank + 1;
            SaveProgression();

            OnEssenceChanged?.Invoke(harvestEssence);
            OnUpgradePurchased?.Invoke(branch, branchRanks[branch]);
            return true;
        }

        public void AddEssence(int amount, bool applyFortune = true)
        {
            if (amount <= 0) return;

            int finalAmount = amount;
            if (applyFortune)
            {
                int fortuneRank = GetRank(MetaUpgradeBranch.Fortune);
                float bonusPct = fortuneRank * 0.15f;
                finalAmount = Mathf.RoundToInt(amount * (1f + bonusPct));
            }

            harvestEssence += finalAmount;
            runEssenceEarned += finalAmount;
            SaveProgression();

            OnEssenceChanged?.Invoke(harvestEssence);
        }

        public bool SpendEssence(int amount)
        {
            if (amount <= 0) return true;
            if (harvestEssence < amount) return false;
            harvestEssence -= amount;
            SaveProgression();
            OnEssenceChanged?.Invoke(harvestEssence);
            return true;
        }

        // ==================== EVENT HANDLERS ====================

        private void HandleEnemyDied(EnemyHealth enemyHealth, DamageData damage)
        {
            runEnemiesSlain++;

            if (enemyHealth != null && enemyHealth.IsEliteEnemy)
            {
                // Elite fiend killed: 5 Essence
                AddEssence(5, true);
            }
            else
            {
                // Common enemy killed: 25% chance of 1 Essence (boosted by Fortune)
                float dropChance = 0.25f * (1f + GetRank(MetaUpgradeBranch.Fortune) * 0.15f);
                if (UnityEngine.Random.value < dropChance)
                {
                    AddEssence(1, false);
                }
            }
        }

        private void HandleBossDefeated(BossController boss)
        {
            runBossesDefeated++;

            int worldId = 1;
            if (GameSession.Instance != null && GameSession.Instance.CurrentWorldConfig != null)
            {
                worldId = GameSession.Instance.CurrentWorldConfig.worldId;
            }

            int bossEssence = 100;
            if (worldId == 2) bossEssence = 150;
            else if (worldId >= 3) bossEssence = 225;

            AddEssence(bossEssence, true);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast("BOSS ESSENCE CONSECRATED", $"+{bossEssence} Harvest Essence gained!", 3.2f, new Color(1f, 0.85f, 0.3f));
            }
        }

        private void HandleHarvestResolved(HarvestResult result)
        {
            if (result == HarvestResult.Lucky)
            {
                runHarvestsCompleted++;
                AddEssence(25, true);
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowToast("LUCKY HARVEST", "+25 Harvest Essence harvested!", 2.5f, new Color(0.35f, 0.95f, 0.55f));
                }
            }
        }

        public void AwardWorldClearBonus(int worldId)
        {
            int clearBonus = 50 + (worldId * 25);
            AddEssence(clearBonus, true);
        }

        // ==================== STAT BONUSES APPLICATION ====================

        public void ApplyPermanentUpgrades(GameObject player)
        {
            if (player == null) return;

            // 1. Vitality: Base Max Health (+10 HP per rank)
            int vitRank = GetRank(MetaUpgradeBranch.Vitality);
            if (vitRank > 0)
            {
                PlayerHealth ph = player.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    ph.SetMaxHealth(100f + (vitRank * 10f), true);
                }
            }

            // 2. Agility: Movement Speed (+6% per rank)
            int agiRank = GetRank(MetaUpgradeBranch.Agility);
            if (agiRank > 0)
            {
                PlayerController pc = player.GetComponent<PlayerController>();
                if (pc != null)
                {
                    float speedMult = 1f + (agiRank * 0.06f);
                    pc.SetSpeedMultiplier(speedMult);
                }
            }

            // 3. Power & Haste: Damage & Attack Speed (+8% dmg, +7% atk speed per rank)
            int powRank = GetRank(MetaUpgradeBranch.Power);
            int hasteRank = GetRank(MetaUpgradeBranch.Haste);
            if (powRank > 0 || hasteRank > 0)
            {
                WeaponManager wm = player.GetComponent<WeaponManager>();
                if (wm != null)
                {
                    if (powRank > 0)
                    {
                        wm.SetDamageMultiplier(1f + (powRank * 0.08f));
                    }
                    if (hasteRank > 0)
                    {
                        float cdMult = 1f / (1f + (hasteRank * 0.07f));
                        wm.SetCooldownMultiplier(cdMult);
                    }
                }
            }

            // 4. Survival: Passive Health Regeneration (+0.5 HP/sec per rank)
            int surRank = GetRank(MetaUpgradeBranch.Survival);
            if (surRank > 0)
            {
                PlayerRegenComponent regen = player.GetComponent<PlayerRegenComponent>();
                if (regen == null) regen = player.AddComponent<PlayerRegenComponent>();
                regen.SetRegenRate(surRank * 0.5f);
            }

            // 5. Lucky Last: Extended duration (+1.5s per rank)
            int llRank = GetRank(MetaUpgradeBranch.LuckyLast);
            if (llRank > 0)
            {
                LuckyLastSystem lls = player.GetComponent<LuckyLastSystem>();
                if (lls != null)
                {
                    lls.SetDuration(12.0f + (llRank * 1.5f));
                }
            }

            // 6. Harvest Mastery: Applied via static getter in HarvestPlot
        }

        public float GetHarvestSpeedMultiplier()
        {
            int rank = GetRank(MetaUpgradeBranch.HarvestMastery);
            return 1f + (rank * 0.10f);
        }
    }

    /// <summary>
    /// Helper component attached to player when Survival meta upgrade is active.
    /// Regenerates health tick-by-tick.
    /// </summary>
    public class PlayerRegenComponent : MonoBehaviour
    {
        private PlayerHealth playerHealth;
        private float regenPerSecond = 0f;
        private float timer = 0f;

        public void SetRegenRate(float rate)
        {
            regenPerSecond = rate;
        }

        private void Awake()
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        private void Update()
        {
            if (playerHealth == null || !playerHealth.IsAlive || regenPerSecond <= 0f) return;

            timer += Time.deltaTime;
            if (timer >= 1.0f)
            {
                timer -= 1.0f;
                if (playerHealth.CurrentHealth < playerHealth.MaxHealth)
                {
                    playerHealth.Heal(regenPerSecond);
                }
            }
        }
    }
}
