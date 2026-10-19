using System;
using System.Collections.Generic;
using UnityEngine;

namespace CursedHarvest
{
    [Serializable]
    public class BossEncounterConfig
    {
        public int waveNumber = 10;
        public bool isFinalBoss = true; // True if beating this boss completes the world; False for mid-bosses (e.g. W4 W10, W5 W10)
        public string bossName = "THE LAST HARVEST";
        public string bossTitle = "Harvester of the Blighted Soil";
        public int bossAddCount = 18;
        public int bossGruntQuota = 6;
        public int bossChaserQuota = 4;
        public int bossWalkerQuota = 3;
        public int bossFarmerQuota = 3;
        public int bossChargerQuota = 2;
        public int bossMaxConcurrentAdds = 8;
        public float bossAddSpawnInterval = 2.1f;
        public float bossScale = 1.0f;
        public float bossHealthMultiplier = 1.0f;
        public string bossPrefabPath = "Assets/_Prefabs/Enemy_TheLastHarvest_Boss.prefab";
    }

    [Serializable]
    public class WorldConfig
    {
        [Header("World Identity")]
        public int worldId = 1;
        public string displayName = "THE CURSED FARM";
        public string subtitle = "World 1 — The Blighted Homestead";
        public string description = "A blighted harvest in the rural midnight. Moderate horde pressure designed to teach the sacred arts of survival.";
        public string difficultyTier = "EASY";
        public int totalWaves = 10;
        public int unlockRequirement = 0; // World ID that must be completed to unlock (0 = unlocked by default)

        [Header("Enemy & Boss Damage Scaling")]
        public float enemyDamageMultiplier = 0.82f;
        public float bossDamageMultiplier = 0.90f;
        public float enemySpeedMultiplier = 1.00f;
        public float enemyHealthMultiplier = 1.00f;

        [Header("Player Survivability & Mobility")]
        public float hitInvulnerabilityDuration = 0.60f;
        public float dashCooldown = 1.90f;

        [Header("Health Drop System")]
        public float healthDropChance = 0.04f;
        public float eliteHealthDropChance = 0.35f;
        public float luckyLastHealthDropChance = 0.08f;
        public float luckyLastEliteHealthDropChance = 0.50f;
        public float normalHealAmount = 20f;
        public float luckyLastHealAmount = 25f;
        public float luckyLastMinimumHP = 15f;

        [Header("Wave Progression")]
        public int[] waveEnemyCounts = new int[] { 8, 11, 14, 18, 22, 27, 33, 40, 48 };
        public int maxConcurrentEnemies = 70;
        public float spawnInterval = 0.55f;
        public float intermissionDuration = 12.0f;
        public int[] supplyDropWaves = new int[] { 2, 4, 6, 8, 9 };
        public int preBossWave = 9;

        [Header("Boss Encounters")]
        public int[] bossWaves = new int[] { 10 };
        public BossEncounterConfig[] bossEncounters;

        [Header("Environment & Visuals")]
        public string environmentIdentifier = "Env_World1_Farm";
        public string musicIdentifier = "Music_World1_Echoes";
        public Color themeColor = new Color(0.95f, 0.65f, 0.20f); // Warm amber harvest

        // ==================== FACTORY / REGISTRY ====================
        private static Dictionary<int, WorldConfig> _worldRegistry;

        public static WorldConfig GetConfig(int worldId)
        {
            EnsureRegistry();
            if (_worldRegistry.TryGetValue(worldId, out WorldConfig config))
            {
                return config;
            }
            return _worldRegistry[1];
        }

        public static List<WorldConfig> GetAllConfigs()
        {
            EnsureRegistry();
            List<WorldConfig> list = new List<WorldConfig>();
            for (int i = 1; i <= 5; i++)
            {
                if (_worldRegistry.TryGetValue(i, out WorldConfig c))
                {
                    list.Add(c);
                }
            }
            return list;
        }

        private static void EnsureRegistry()
        {
            if (_worldRegistry == null)
            {
                _worldRegistry = new Dictionary<int, WorldConfig>
                {
                    { 1, CreateWorld1() },
                    { 2, CreateWorld2() },
                    { 3, CreateWorld3() },
                    { 4, CreateWorld4() },
                    { 5, CreateWorld5() }
                };
            }
        }

        public bool IsBossWave(int wave)
        {
            if (bossWaves == null) return false;
            for (int i = 0; i < bossWaves.Length; i++)
            {
                if (bossWaves[i] == wave) return true;
            }
            return false;
        }

        public BossEncounterConfig GetBossEncounter(int wave)
        {
            if (bossEncounters == null) return null;
            for (int i = 0; i < bossEncounters.Length; i++)
            {
                if (bossEncounters[i].waveNumber == wave) return bossEncounters[i];
            }
            return null;
        }

        public int GetEnemyCountForWave(int wave)
        {
            if (wave <= 0) return waveEnemyCounts != null && waveEnemyCounts.Length > 0 ? waveEnemyCounts[0] : 8;

            // Check if it's a boss wave
            if (IsBossWave(wave))
            {
                BossEncounterConfig bossEnc = GetBossEncounter(wave);
                if (bossEnc != null) return bossEnc.bossAddCount;
            }

            if (waveEnemyCounts != null && wave - 1 < waveEnemyCounts.Length)
            {
                return waveEnemyCounts[wave - 1];
            }

            // Post-configured scaling
            int lastKnown = waveEnemyCounts != null && waveEnemyCounts.Length > 0 ? waveEnemyCounts[waveEnemyCounts.Length - 1] : 75;
            int excess = wave - (waveEnemyCounts != null ? waveEnemyCounts.Length : 9);
            return Mathf.RoundToInt(lastKnown * Mathf.Pow(1.12f, excess));
        }

        // ==================== WORLD DEFINITIONS ====================

        public static WorldConfig CreateWorld1()
        {
            return new WorldConfig
            {
                worldId = 1,
                displayName = "THE CURSED FARM",
                subtitle = "World 1 — The Blighted Homestead",
                description = "A blighted harvest in the rural midnight. Mild horde pressure designed to teach the sacred arts of survival.",
                difficultyTier = "EASY",
                totalWaves = 10,
                unlockRequirement = 0,
                enemyDamageMultiplier = 0.82f,
                bossDamageMultiplier = 0.90f,
                enemySpeedMultiplier = 1.00f,
                enemyHealthMultiplier = 1.00f,
                hitInvulnerabilityDuration = 0.60f,
                dashCooldown = 1.90f,
                healthDropChance = 0.04f,
                eliteHealthDropChance = 0.35f,
                luckyLastHealthDropChance = 0.08f,
                luckyLastEliteHealthDropChance = 0.50f,
                normalHealAmount = 20f,
                luckyLastHealAmount = 25f,
                luckyLastMinimumHP = 15f,
                waveEnemyCounts = new int[] { 8, 11, 14, 18, 22, 27, 33, 40, 48 },
                maxConcurrentEnemies = 70,
                spawnInterval = 0.55f,
                intermissionDuration = 12.0f,
                supplyDropWaves = new int[] { 2, 4, 6, 8, 9 },
                preBossWave = 9,
                bossWaves = new int[] { 10 },
                bossEncounters = new BossEncounterConfig[]
                {
                    new BossEncounterConfig
                    {
                        waveNumber = 10,
                        isFinalBoss = true,
                        bossName = "THE LAST HARVEST",
                        bossTitle = "Lord of the Blighted Cornfield",
                        bossAddCount = 18,
                        bossGruntQuota = 6,
                        bossChaserQuota = 4,
                        bossWalkerQuota = 3,
                        bossFarmerQuota = 3,
                        bossChargerQuota = 2,
                        bossMaxConcurrentAdds = 8,
                        bossAddSpawnInterval = 2.1f,
                        bossScale = 1.0f,
                        bossHealthMultiplier = 1.0f
                    }
                },
                themeColor = new Color(0.95f, 0.65f, 0.20f),
                environmentIdentifier = "Env_World1_Farm",
                musicIdentifier = "Music_World1_Echoes"
            };
        }

        public static WorldConfig CreateWorld2()
        {
            return new WorldConfig
            {
                worldId = 2,
                displayName = "THE ASHEN ORCHARDS",
                subtitle = "World 2 — The Scorched Fruit",
                description = "Charred trees and denser swarms. Fewer respite drops and persistent stalkers prowling the dying grove.",
                difficultyTier = "NORMAL",
                totalWaves = 10,
                unlockRequirement = 1,
                enemyDamageMultiplier = 0.94f,
                bossDamageMultiplier = 0.95f,
                enemySpeedMultiplier = 1.03f,
                enemyHealthMultiplier = 1.08f,
                hitInvulnerabilityDuration = 0.55f,
                dashCooldown = 2.00f,
                healthDropChance = 0.035f,
                eliteHealthDropChance = 0.30f,
                luckyLastHealthDropChance = 0.07f,
                luckyLastEliteHealthDropChance = 0.45f,
                normalHealAmount = 20f,
                luckyLastHealAmount = 25f,
                luckyLastMinimumHP = 12f,
                waveEnemyCounts = new int[] { 10, 14, 18, 23, 29, 35, 42, 50, 59 },
                maxConcurrentEnemies = 70,
                spawnInterval = 0.52f,
                intermissionDuration = 12.0f,
                supplyDropWaves = new int[] { 3, 6, 9 },
                preBossWave = 9,
                bossWaves = new int[] { 10 },
                bossEncounters = new BossEncounterConfig[]
                {
                    new BossEncounterConfig
                    {
                        waveNumber = 10,
                        isFinalBoss = true,
                        bossName = "THE ASHEN REAPER",
                        bossTitle = "Scourge of the Scorched Grove",
                        bossAddCount = 20,
                        bossGruntQuota = 6,
                        bossChaserQuota = 5,
                        bossWalkerQuota = 4,
                        bossFarmerQuota = 3,
                        bossChargerQuota = 2,
                        bossMaxConcurrentAdds = 8,
                        bossAddSpawnInterval = 1.9f,
                        bossScale = 1.05f,
                        bossHealthMultiplier = 1.15f,
                        bossPrefabPath = "Assets/_Prefabs/Boss_TheAshenReaper.prefab"
                    }
                },
                themeColor = new Color(0.85f, 0.45f, 0.15f),
                environmentIdentifier = "Env_World2_Orchard",
                musicIdentifier = "Music_World2_Ashen"
            };
        }

        public static WorldConfig CreateWorld3()
        {
            return new WorldConfig
            {
                worldId = 3,
                displayName = "THE BLACKENED MIRE",
                subtitle = "World 3 — The Plague Marsh",
                description = "Putrid swamps crawling with plague walkers and charging fiends. Healing is sparse and toxic puddles abound.",
                difficultyTier = "HARD",
                totalWaves = 10,
                unlockRequirement = 2,
                enemyDamageMultiplier = 1.00f,
                bossDamageMultiplier = 1.00f,
                enemySpeedMultiplier = 1.06f,
                enemyHealthMultiplier = 1.18f,
                hitInvulnerabilityDuration = 0.50f,
                dashCooldown = 2.10f,
                healthDropChance = 0.030f,
                eliteHealthDropChance = 0.25f,
                luckyLastHealthDropChance = 0.06f,
                luckyLastEliteHealthDropChance = 0.40f,
                normalHealAmount = 20f,
                luckyLastHealAmount = 22f,
                luckyLastMinimumHP = 10f,
                waveEnemyCounts = new int[] { 12, 17, 23, 30, 38, 47, 57, 68, 80 },
                maxConcurrentEnemies = 70,
                spawnInterval = 0.48f,
                intermissionDuration = 12.0f,
                supplyDropWaves = new int[] { 4, 8 },
                preBossWave = 9,
                bossWaves = new int[] { 10 },
                bossEncounters = new BossEncounterConfig[]
                {
                    new BossEncounterConfig
                    {
                        waveNumber = 10,
                        isFinalBoss = true,
                        bossName = "THE MIRE MOTHER",
                        bossTitle = "Queen of Pestilence",
                        bossAddCount = 24,
                        bossGruntQuota = 6,
                        bossChaserQuota = 5,
                        bossWalkerQuota = 5,
                        bossFarmerQuota = 4,
                        bossChargerQuota = 4,
                        bossMaxConcurrentAdds = 9,
                        bossAddSpawnInterval = 1.8f,
                        bossScale = 1.35f,
                        bossHealthMultiplier = 1.30f,
                        bossPrefabPath = "Assets/_Prefabs/Boss_TheMireMother.prefab"
                    }
                },
                themeColor = new Color(0.20f, 0.75f, 0.45f),
                environmentIdentifier = "Env_World3_Mire",
                musicIdentifier = "Music_World3_Mire"
            };
        }

        public static WorldConfig CreateWorld4()
        {
            return new WorldConfig
            {
                worldId = 4,
                displayName = "THE BLOODWOOD TIMBER",
                subtitle = "World 4 — The Crimson Pines",
                description = "15 waves of grueling endurance. A brutal Mid-Boss at Wave 10, culminated by the Bloodwood Sovereign at Wave 15.",
                difficultyTier = "VERY HARD",
                totalWaves = 15,
                unlockRequirement = 3,
                enemyDamageMultiplier = 1.08f,
                bossDamageMultiplier = 1.05f,
                enemySpeedMultiplier = 1.08f,
                enemyHealthMultiplier = 1.28f,
                hitInvulnerabilityDuration = 0.45f,
                dashCooldown = 2.15f,
                healthDropChance = 0.025f,
                eliteHealthDropChance = 0.22f,
                luckyLastHealthDropChance = 0.05f,
                luckyLastEliteHealthDropChance = 0.35f,
                normalHealAmount = 18f,
                luckyLastHealAmount = 20f,
                luckyLastMinimumHP = 8f,
                waveEnemyCounts = new int[] { 12, 16, 21, 27, 34, 42, 51, 61, 72, 20, 76, 85, 96, 108, 24 },
                maxConcurrentEnemies = 70,
                spawnInterval = 0.45f,
                intermissionDuration = 12.0f,
                supplyDropWaves = new int[] { 4, 8, 13 },
                preBossWave = 14,
                bossWaves = new int[] { 10, 15 },
                bossEncounters = new BossEncounterConfig[]
                {
                    new BossEncounterConfig
                    {
                        waveNumber = 10,
                        isFinalBoss = false, // Mid-Boss: does not complete the world!
                        bossName = "TIMBER GOLIATH",
                        bossTitle = "Crusher of the Ancient Pines",
                        bossAddCount = 20,
                        bossGruntQuota = 6,
                        bossChaserQuota = 5,
                        bossWalkerQuota = 4,
                        bossFarmerQuota = 3,
                        bossChargerQuota = 2,
                        bossMaxConcurrentAdds = 8,
                        bossAddSpawnInterval = 1.9f,
                        bossScale = 1.15f,
                        bossHealthMultiplier = 1.25f,
                        bossPrefabPath = "Assets/_Prefabs/Boss_TimberGoliath.prefab"
                    },
                    new BossEncounterConfig
                    {
                        waveNumber = 15,
                        isFinalBoss = true, // Final Boss of World 4: completes the world!
                        bossName = "BLOODWOOD SOVEREIGN",
                        bossTitle = "Monarch of the Bleeding Bark",
                        bossAddCount = 24,
                        bossGruntQuota = 6,
                        bossChaserQuota = 6,
                        bossWalkerQuota = 4,
                        bossFarmerQuota = 4,
                        bossChargerQuota = 4,
                        bossMaxConcurrentAdds = 9,
                        bossAddSpawnInterval = 1.7f,
                        bossScale = 1.20f,
                        bossHealthMultiplier = 1.45f,
                        bossPrefabPath = "Assets/_Prefabs/Boss_BloodwoodSovereign.prefab"
                    }
                },
                themeColor = new Color(0.85f, 0.25f, 0.25f),
                environmentIdentifier = "Env_World4_Timber",
                musicIdentifier = "Music_World4_Bloodwood"
            };
        }

        public static WorldConfig CreateWorld5()
        {
            return new WorldConfig
            {
                worldId = 5,
                displayName = "THE ETERNAL ECLIPSE",
                subtitle = "World 5 — The Sunless Abyss",
                description = "20 waves of unrelenting nightmare. Mid-Boss at Wave 10, culminated by the Primeval Horror at Wave 20.",
                difficultyTier = "NIGHTMARE",
                totalWaves = 20,
                unlockRequirement = 4,
                enemyDamageMultiplier = 1.15f,
                bossDamageMultiplier = 1.10f,
                enemySpeedMultiplier = 1.12f,
                enemyHealthMultiplier = 1.40f,
                hitInvulnerabilityDuration = 0.40f,
                dashCooldown = 2.20f,
                healthDropChance = 0.020f,
                eliteHealthDropChance = 0.20f,
                luckyLastHealthDropChance = 0.04f,
                luckyLastEliteHealthDropChance = 0.30f,
                normalHealAmount = 16f,
                luckyLastHealAmount = 18f,
                luckyLastMinimumHP = 5f,
                waveEnemyCounts = new int[] { 14, 19, 25, 32, 40, 49, 59, 70, 82, 22, 84, 93, 103, 114, 126, 139, 153, 168, 185, 26 },
                maxConcurrentEnemies = 75,
                spawnInterval = 0.40f,
                intermissionDuration = 12.0f,
                supplyDropWaves = new int[] { 5, 9, 14, 18 },
                preBossWave = 19,
                bossWaves = new int[] { 10, 20 },
                bossEncounters = new BossEncounterConfig[]
                {
                    new BossEncounterConfig
                    {
                        waveNumber = 10,
                        isFinalBoss = false, // Mid-Boss: does not complete the world!
                        bossName = "ECLIPSE HERALD",
                        bossTitle = "Shadow of the Dying Sun",
                        bossAddCount = 22,
                        bossGruntQuota = 6,
                        bossChaserQuota = 5,
                        bossWalkerQuota = 4,
                        bossFarmerQuota = 4,
                        bossChargerQuota = 3,
                        bossMaxConcurrentAdds = 9,
                        bossAddSpawnInterval = 1.8f,
                        bossScale = 1.15f,
                        bossHealthMultiplier = 1.35f,
                        bossPrefabPath = "Assets/_Prefabs/Boss_EclipseHerald.prefab"
                    },
                    new BossEncounterConfig
                    {
                        waveNumber = 20,
                        isFinalBoss = true, // Final Boss of World 5: completes the entire journey!
                        bossName = "THE PRIMEVAL HORROR",
                        bossTitle = "Ancient Entity of Perpetual Night",
                        bossAddCount = 26,
                        bossGruntQuota = 6,
                        bossChaserQuota = 6,
                        bossWalkerQuota = 5,
                        bossFarmerQuota = 5,
                        bossChargerQuota = 4,
                        bossMaxConcurrentAdds = 10,
                        bossAddSpawnInterval = 1.6f,
                        bossScale = 1.30f,
                        bossHealthMultiplier = 1.60f,
                        bossPrefabPath = "Assets/_Prefabs/Boss_PrimevalHorror.prefab"
                    }
                },
                themeColor = new Color(0.65f, 0.25f, 0.85f),
                environmentIdentifier = "Env_World5_Eclipse",
                musicIdentifier = "Music_World5_Eclipse"
            };
        }
    }
}
