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
                    return $"+{rank * 7} Maximum Health";
                case MetaUpgradeBranch.Power:
                    return $"+{rank * 8}% All Weapon Damage";
                case MetaUpgradeBranch.Haste:
                    return $"+{rank * 7}% Attack Speed";
                case MetaUpgradeBranch.Agility:
                    return $"+{rank * 6}% Movement Speed";
                case MetaUpgradeBranch.Fortune:
                    return $"+{rank * 15}% Essence & +{rank * 3}% Lucky Harvest";
                case MetaUpgradeBranch.Survival:
                    float healPct = rank switch { 1 => 5f, 2 => 10f, 3 => 17f, 4 => 25f, 5 => 35f, _ => 0f };
                    return $"+{healPct:0}% Heal & +{(rank * 0.5f):F1} HP/s Regen";
                case MetaUpgradeBranch.LuckyLast:
                    float dur = rank switch { 1 => 1f, 2 => 1f, 3 => 2f, 4 => 2f, 5 => 4f, _ => 0f };
                    float extraHp = rank switch { 2 => 3f, 3 => 3f, 4 => 8f, 5 => 8f, _ => 0f };
                    return extraHp > 0 ? $"+{dur:0}s Duration & +{extraHp:0} Recovery HP" : $"+{dur:0}s Lucky Last Duration";
                case MetaUpgradeBranch.HarvestMastery:
                    return $"-{rank * 4}% Channel Time ({(1f - rank * 0.04f):0.00}x duration)";
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
                if (_instance != null)
                {
                    _instance.EnsureInitialized();
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
            AddDef(MetaUpgradeBranch.Vitality, "Vitality", "+7 Max Health per rank (+35 max)", "♥");
            AddDef(MetaUpgradeBranch.Power, "Power", "+8% Weapon Damage per rank (+40% max)", "⚔");
            AddDef(MetaUpgradeBranch.Haste, "Haste", "+7% Attack Speed per rank (+35% max)", "⚡");
            AddDef(MetaUpgradeBranch.Agility, "Agility", "+6% Movement Speed per rank (+30% max)", "👟");
            AddDef(MetaUpgradeBranch.Fortune, "Fortune", "+15% Essence & +3% Lucky Harvest per rank", "✦");
            AddDef(MetaUpgradeBranch.Survival, "Survival", "+5..35% Heal & +0.5 HP/s Regen per rank", "✚");
            AddDef(MetaUpgradeBranch.LuckyLast, "Lucky Last", "+1..4s Duration & +3..8 Recovery HP", "★");
            AddDef(MetaUpgradeBranch.HarvestMastery, "Harvest", "-4% Channel Time per rank (0.80x max)", "🌾");
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

        public void EnsureInitialized()
        {
            if (branchDefs == null || branchDefs.Count == 0)
            {
                InitializeDefinitions();
            }
            if (branchRanks == null || branchRanks.Count == 0)
            {
                LoadProgression();
            }
        }

        public MetaUpgradeDef GetDef(MetaUpgradeBranch branch)
        {
            EnsureInitialized();
            return branchDefs.TryGetValue(branch, out var def) ? def : null;
        }

        public int GetRank(MetaUpgradeBranch branch)
        {
            EnsureInitialized();
            return branchRanks.TryGetValue(branch, out int rank) ? rank : 0;
        }

        public void LoadProgression()
        {
            int version = PlayerPrefs.GetInt(PrefsSaveVersionKey, CurrentSaveVersion);
            harvestEssence = PlayerPrefs.GetInt(PrefsEssenceKey, 0);

            branchRanks.Clear();
            foreach (MetaUpgradeBranch branch in Enum.GetValues(typeof(MetaUpgradeBranch)))
            {
                int r = PlayerPrefs.GetInt($"CH_Meta_{branch}", -1);
                if (r < 0) r = PlayerPrefs.GetInt($"CH_MetaUpgrade_{branch}", 0);
                branchRanks[branch] = Mathf.Clamp(r, 0, 5);
            }
        }

        public void SaveProgression()
        {
            PlayerPrefs.SetInt(PrefsSaveVersionKey, CurrentSaveVersion);
            PlayerPrefs.SetInt(PrefsEssenceKey, harvestEssence);
            foreach (var kvp in branchRanks)
            {
                int clamped = Mathf.Clamp(kvp.Value, 0, 5);
                PlayerPrefs.SetInt($"CH_Meta_{kvp.Key}", clamped);
                PlayerPrefs.SetInt($"CH_MetaUpgrade_{kvp.Key}", clamped);
            }
            PlayerPrefs.Save();
        }

        public void SetAllMaxRank()
        {
            foreach (MetaUpgradeBranch branch in Enum.GetValues(typeof(MetaUpgradeBranch)))
            {
                branchRanks[branch] = 5;
            }
            SaveProgression();
            foreach (MetaUpgradeBranch branch in Enum.GetValues(typeof(MetaUpgradeBranch)))
            {
                OnUpgradePurchased?.Invoke(branch, 5);
            }
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

            MobileSettingsAndLifecycleManager.Instance?.TriggerHaptic(HapticPattern.UpgradePurchase);
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

        // ==================== UNIFIED META STAT API ====================

        public float GetVitalityBonus()
        {
            return GetRank(MetaUpgradeBranch.Vitality) * 7f; // Lv1..5: +7, +14, +21, +28, +35 Max HP
        }

        public float GetPowerMultiplier()
        {
            return 1f + (GetRank(MetaUpgradeBranch.Power) * 0.08f); // Lv1..5: 1.08x, 1.16x, 1.24x, 1.32x, 1.40x All Damage
        }

        public float GetHasteMultiplier()
        {
            return 1f + (GetRank(MetaUpgradeBranch.Haste) * 0.07f); // Lv1..5: 1.07x, 1.14x, 1.21x, 1.28x, 1.35x Fire Rate
        }

        public float GetAgilityMultiplier()
        {
            return 1f + (GetRank(MetaUpgradeBranch.Agility) * 0.06f); // Lv1..5: 1.06x, 1.12x, 1.18x, 1.24x, 1.30x Move Speed
        }

        public float GetFortuneBonus()
        {
            return GetRank(MetaUpgradeBranch.Fortune) * 0.03f; // Lv1..5: +3%, +6%, +9%, +12%, +15% Lucky Harvest gamble boost
        }

        public float GetFortuneEssenceMultiplier()
        {
            return 1f + (GetRank(MetaUpgradeBranch.Fortune) * 0.15f); // Lv1..5: +15%..+75% Essence gain
        }

        public float GetSurvivalHealMultiplier()
        {
            int r = GetRank(MetaUpgradeBranch.Survival);
            return r switch
            {
                1 => 1.05f,
                2 => 1.10f,
                3 => 1.17f,
                4 => 1.25f,
                5 => 1.35f,
                _ => 1.00f
            };
        }

        public float GetSurvivalRegenRate()
        {
            return GetRank(MetaUpgradeBranch.Survival) * 0.5f; // Lv1..5: 0.5, 1.0, 1.5, 2.0, 2.5 HP/s Passive Regen
        }

        public float GetSurvivalBonus()
        {
            return GetSurvivalHealMultiplier() - 1f;
        }

        public float GetLuckyLastDurationBonus()
        {
            int r = GetRank(MetaUpgradeBranch.LuckyLast);
            return r switch
            {
                1 => 1.0f,
                2 => 1.0f,
                3 => 2.0f,
                4 => 2.0f,
                5 => 4.0f,
                _ => 0f
            }; // Base 12s -> 13s, 13s, 14s, 14s, 16s
        }

        public float GetLuckyLastRecoveryHPBonus()
        {
            int r = GetRank(MetaUpgradeBranch.LuckyLast);
            return r switch
            {
                2 => 3f,
                3 => 3f,
                4 => 8f,
                5 => 8f,
                _ => 0f
            }; // Starting recovery HP bonus (+3 HP at Lv2-3, +8 HP at Lv4-5)
        }

        public float GetLuckyLastBonus()
        {
            return GetLuckyLastDurationBonus();
        }

        public float GetHarvestChannelMultiplier()
        {
            int r = GetRank(MetaUpgradeBranch.HarvestMastery);
            return Mathf.Clamp(1.0f - (r * 0.04f), 0.80f, 1.00f); // Lv0..5: 1.00, 0.96, 0.92, 0.88, 0.84, 0.80
        }

        public float GetHarvestBonus()
        {
            return 1.0f - GetHarvestChannelMultiplier();
        }

        public float GetHarvestSpeedMultiplier()
        {
            float mult = GetHarvestChannelMultiplier();
            return mult > 0.01f ? (1f / mult) : 1f;
        }

        // ==================== STAT BONUSES APPLICATION ====================

        public void ApplyPermanentUpgrades(GameObject player)
        {
            if (player == null) return;

            // 1. Vitality: Apply directly to PlayerHealth component
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.ApplyMetaVitality();
            }

            // 2. Survival: Attach/configure PlayerRegenComponent
            int surRank = GetRank(MetaUpgradeBranch.Survival);
            PlayerRegenComponent regen = player.GetComponent<PlayerRegenComponent>();
            if (surRank > 0)
            {
                if (regen == null) regen = player.AddComponent<PlayerRegenComponent>();
                regen.SetRegenRate(GetSurvivalRegenRate());
            }
            else if (regen != null)
            {
                regen.SetRegenRate(0f);
            }

            // Note: Power, Haste, Agility, Fortune, Lucky Last, and Harvest Mastery
            // are dynamically queried by their authoritative systems (PlayerController,
            // WeaponManager, LuckyLastSystem, HarvestPlot, HealthRestorePickup) via the unified Meta Stat API,
            // strictly following: BASE STAT -> WORLD CONFIG -> META UPGRADE -> RUN UPGRADE -> TEMPORARY BUFF.
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
