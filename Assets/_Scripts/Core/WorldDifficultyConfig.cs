using System;
using UnityEngine;

namespace CursedHarvest
{
    /// <summary>
    /// Centralized difficulty configuration for World and Arena progression.
    /// Adapted for 15 Arenas across 3 Worlds with 5 waves per arena and boss encounter on wave 5.
    /// </summary>
    [Serializable]
    public class WorldDifficultyConfig
    {
        [Header("World Identity")]
        public int worldId = 1;
        public string worldName = "World 1 — The Cursed Realms";

        [Header("Wave Enemy Counts")]
        public int[] waveCounts = new int[] { 8, 12, 16, 22, 16 };

        [Header("Boss Wave Configuration")]
        public int bossAddCount = 16;
        public int bossGruntQuota = 6;
        public int bossChaserQuota = 4;
        public int bossWalkerQuota = 3;
        public int bossFarmerQuota = 3;
        public int bossChargerQuota = 2;
        public int bossMaxConcurrentAdds = 8;
        public float bossAddSpawnInterval = 2.1f;

        [Header("Endless Wave (Wave 6+)")]
        public int postWave5BaseCount = 35;
        public float postWave5Growth = 1.15f;

        [Header("Damage Multipliers")]
        public float enemyDamageMultiplier = 0.85f;
        public float bossDamageMultiplier = 0.90f;

        [Header("Player Survivability & Mobility")]
        public float hitInvulnerability = 0.60f;
        public float dashBaseCooldown = 1.90f;

        [Header("Enemy Speed Adjustments")]
        public float chargerSpeedMultiplier = 1.0f;

        [Header("Health Restore Drop Chances")]
        public float commonHealthDropChance = 0.04f;
        public float eliteHealthDropChance = 0.35f;
        public float luckyLastCommonHealthDropChance = 0.08f;
        public float luckyLastEliteHealthDropChance = 0.50f;

        [Header("Healing & Lucky Last Assist")]
        public float normalHealAmount = 20f;
        public float luckyLastHealAmount = 25f;
        public float luckyLastMinimumHP = 15f;

        [Header("Intermission Supply Drops & Pre-Boss Recovery")]
        public int[] supplyDropWaves = new int[] { 2, 4 };
        public int preBossWave = 4;
        public float preBossLowHpThreshold = 0.50f;
        public int preBossLowHpDropCount = 2;
        public int preBossNormalDropCount = 1;

        private static WorldDifficultyConfig _current;

        public static WorldDifficultyConfig Current
        {
            get
            {
                if (_current == null)
                {
                    _current = CreateWorld1Default();
                }
                return _current;
            }
            set
            {
                _current = value ?? CreateWorld1Default();
            }
        }

        public static WorldDifficultyConfig CreateWorld1Default()
        {
            return new WorldDifficultyConfig();
        }

        public static WorldDifficultyConfig FromWorldConfig(WorldConfig wc)
        {
            if (wc == null) return CreateWorld1Default();
            var cfg = new WorldDifficultyConfig();
            cfg.worldId = wc.worldId;
            cfg.worldName = wc.displayName;
            cfg.waveCounts = wc.waveEnemyCounts;
            if (wc.bossEncounters != null && wc.bossEncounters.Length > 0)
            {
                var be = wc.bossEncounters[0];
                cfg.bossAddCount = be.bossAddCount;
                cfg.bossGruntQuota = be.bossGruntQuota;
                cfg.bossChaserQuota = be.bossChaserQuota;
                cfg.bossWalkerQuota = be.bossWalkerQuota;
                cfg.bossFarmerQuota = be.bossFarmerQuota;
                cfg.bossChargerQuota = be.bossChargerQuota;
                cfg.bossMaxConcurrentAdds = be.bossMaxConcurrentAdds;
                cfg.bossAddSpawnInterval = be.bossAddSpawnInterval;
            }
            cfg.enemyDamageMultiplier = wc.enemyDamageMultiplier;
            cfg.bossDamageMultiplier = wc.bossDamageMultiplier;
            cfg.hitInvulnerability = wc.hitInvulnerabilityDuration;
            cfg.dashBaseCooldown = wc.dashCooldown;
            cfg.chargerSpeedMultiplier = wc.enemySpeedMultiplier;
            cfg.commonHealthDropChance = wc.healthDropChance;
            cfg.eliteHealthDropChance = wc.eliteHealthDropChance;
            cfg.luckyLastCommonHealthDropChance = wc.luckyLastHealthDropChance;
            cfg.luckyLastEliteHealthDropChance = wc.luckyLastEliteHealthDropChance;
            cfg.normalHealAmount = wc.normalHealAmount;
            cfg.luckyLastHealAmount = wc.luckyLastHealAmount;
            cfg.luckyLastMinimumHP = wc.luckyLastMinimumHP;
            cfg.supplyDropWaves = wc.supplyDropWaves;
            cfg.preBossWave = wc.preBossWave;
            return cfg;
        }

        public static void ResetToWorld1Default()
        {
            _current = CreateWorld1Default();
        }

        public int GetEnemyCountForWave(int wave, float? customGrowth = null)
        {
            if (wave <= 0) return waveCounts != null && waveCounts.Length > 0 ? waveCounts[0] : 8;
            if (wave <= 4)
            {
                if (waveCounts != null && wave - 1 < waveCounts.Length)
                {
                    return waveCounts[wave - 1];
                }
                return 8;
            }
            if (wave == 5)
            {
                return bossAddCount;
            }

            float g = customGrowth ?? postWave5Growth;
            return Mathf.RoundToInt(postWave5BaseCount * Mathf.Pow(g, wave - 6));
        }

        public bool IsSupplyDropWave(int completedWave)
        {
            if (completedWave <= 0 || completedWave == 5) return false;

            if (completedWave <= 4)
            {
                if (supplyDropWaves != null)
                {
                    for (int i = 0; i < supplyDropWaves.Length; i++)
                    {
                        if (supplyDropWaves[i] == completedWave) return true;
                    }
                }
                return false;
            }

            return completedWave % 3 == 0;
        }

        public int GetSupplyDropCount(int completedWave, float playerHpPercent)
        {
            if (!IsSupplyDropWave(completedWave)) return 0;

            if (completedWave == preBossWave)
            {
                return (playerHpPercent < preBossLowHpThreshold) ? preBossLowHpDropCount : preBossNormalDropCount;
            }

            return 1;
        }

        public float GetEffectiveHealthDropChance(bool isElite, bool isLuckyLastActive)
        {
            if (isLuckyLastActive)
            {
                return isElite ? luckyLastEliteHealthDropChance : luckyLastCommonHealthDropChance;
            }
            return isElite ? eliteHealthDropChance : commonHealthDropChance;
        }

        public float GetEffectiveHealAmount(bool isLuckyLastActive)
        {
            float baseHeal = isLuckyLastActive ? luckyLastHealAmount : normalHealAmount;
            float survivalMult = (MetaProgressionManager.Instance != null) ? MetaProgressionManager.Instance.GetSurvivalHealMultiplier() : 1f;
            return baseHeal * survivalMult;
        }
    }
}
