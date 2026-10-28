using System;
using System.Collections.Generic;
using UnityEngine;

namespace CursedHarvest
{
    [Serializable]
    public class BossEncounterConfig
    {
        public int waveNumber = 5;
        public bool isFinalBoss = true;
        public string bossName = "THE LAST HARVEST";
        public string bossTitle = "Harvester of the Blighted Soil";
        public int bossAddCount = 16;
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
    public class ArenaConfig
    {
        public int arenaId;            // 1..15
        public int worldId;            // 1..3
        public int arenaIndexInWorld;  // 1..5
        public string arenaName;
        public string subtitle;
        public string description;
        public string difficultyTier;
        public int totalWaves = 5;
        public int unlockRequirementArenaId = 0; // Arena that must be beaten to unlock (0 = unlocked by default)

        public float enemyDamageMultiplier = 1.0f;
        public float bossDamageMultiplier = 1.0f;
        public float enemySpeedMultiplier = 1.0f;
        public float enemyHealthMultiplier = 1.0f;

        public float hitInvulnerabilityDuration = 0.60f;
        public float dashCooldown = 1.90f;

        public float healthDropChance = 0.04f;
        public float eliteHealthDropChance = 0.35f;
        public float luckyLastHealthDropChance = 0.08f;
        public float luckyLastEliteHealthDropChance = 0.50f;
        public float normalHealAmount = 20f;
        public float luckyLastHealAmount = 25f;
        public float luckyLastMinimumHP = 15f;

        public int[] waveEnemyCounts = new int[] { 8, 12, 16, 22, 16 };
        public int maxConcurrentEnemies = 70;
        public float spawnInterval = 0.55f;
        public float intermissionDuration = 10.0f;
        public int[] supplyDropWaves = new int[] { 2, 4 };
        public int preBossWave = 4;

        public int[] bossWaves = new int[] { 5 };
        public BossEncounterConfig[] bossEncounters;

        public string environmentIdentifier = "Env_World1_Farm";
        public string musicIdentifier = "Music_World1_Echoes";
        public Color themeColor = new Color(0.95f, 0.65f, 0.20f);
        public int environmentVisualIndex = 1; // 1..5 for WorldEnvironmentController profiles
    }

    [Serializable]
    public class WorldConfig
    {
        [Header("World Identity")]
        public int worldId = 1;
        public string displayName = "THE CURSED REALMS";
        public string subtitle = "World 1 — The Ancient Blight";
        public string description = "A blighted expanse of cursed homesteads, orchards and dark mires. 5 distinct arenas culminating in the harvest guardian.";
        public string difficultyTier = "NORMAL";
        public int totalWaves = 5; // Standard 5 waves per arena
        public int unlockRequirement = 0; // Previous World ID required (0 for World 1)
        public Color themeColor = new Color(0.95f, 0.65f, 0.20f);

        [Header("Arena Configuration")]
        public int activeArenaIndex = 1; // 1..5
        public ArenaConfig activeArena;

        // Legacy compatibility properties forwarded to active arena
        public float enemyDamageMultiplier => activeArena != null ? activeArena.enemyDamageMultiplier : 0.85f;
        public float bossDamageMultiplier => activeArena != null ? activeArena.bossDamageMultiplier : 0.90f;
        public float enemySpeedMultiplier => activeArena != null ? activeArena.enemySpeedMultiplier : 1.00f;
        public float enemyHealthMultiplier => activeArena != null ? activeArena.enemyHealthMultiplier : 1.00f;
        public float hitInvulnerabilityDuration => activeArena != null ? activeArena.hitInvulnerabilityDuration : 0.60f;
        public float dashCooldown => activeArena != null ? activeArena.dashCooldown : 1.90f;
        public float healthDropChance => activeArena != null ? activeArena.healthDropChance : 0.04f;
        public float eliteHealthDropChance => activeArena != null ? activeArena.eliteHealthDropChance : 0.35f;
        public float luckyLastHealthDropChance => activeArena != null ? activeArena.luckyLastHealthDropChance : 0.08f;
        public float luckyLastEliteHealthDropChance => activeArena != null ? activeArena.luckyLastEliteHealthDropChance : 0.50f;
        public float normalHealAmount => activeArena != null ? activeArena.normalHealAmount : 20f;
        public float luckyLastHealAmount => activeArena != null ? activeArena.luckyLastHealAmount : 25f;
        public float luckyLastMinimumHP => activeArena != null ? activeArena.luckyLastMinimumHP : 15f;
        public int[] waveEnemyCounts => activeArena != null ? activeArena.waveEnemyCounts : new int[] { 8, 12, 16, 22, 16 };
        public int maxConcurrentEnemies => activeArena != null ? activeArena.maxConcurrentEnemies : 70;
        public float spawnInterval => activeArena != null ? activeArena.spawnInterval : 0.55f;
        public float intermissionDuration => activeArena != null ? activeArena.intermissionDuration : 10.0f;
        public int[] supplyDropWaves => activeArena != null ? activeArena.supplyDropWaves : new int[] { 2, 4 };
        public int preBossWave => activeArena != null ? activeArena.preBossWave : 4;
        public int[] bossWaves => activeArena != null ? activeArena.bossWaves : new int[] { 5 };
        public BossEncounterConfig[] bossEncounters => activeArena != null ? activeArena.bossEncounters : null;
        public string environmentIdentifier => activeArena != null ? activeArena.environmentIdentifier : "Env_World1_Farm";
        public string musicIdentifier => activeArena != null ? activeArena.musicIdentifier : "Music_World1_Echoes";
        public int environmentVisualIndex => activeArena != null ? activeArena.environmentVisualIndex : 1;

        // ==================== 15 ARENAS REGISTRY ====================
        private static Dictionary<int, ArenaConfig> _arenaRegistry;
        private static Dictionary<int, WorldConfig> _worldRegistry;

        public static void ResetRegistry()
        {
            _arenaRegistry = null;
            _worldRegistry = null;
        }

        public static ArenaConfig GetArenaConfig(int globalArenaId)
        {
            EnsureRegistries();
            if (_arenaRegistry.TryGetValue(globalArenaId, out ArenaConfig arena))
            {
                return arena;
            }
            return _arenaRegistry[1];
        }

        public static List<ArenaConfig> GetArenasForWorld(int worldId)
        {
            EnsureRegistries();
            List<ArenaConfig> list = new List<ArenaConfig>();
            for (int i = 1; i <= 15; i++)
            {
                if (_arenaRegistry.TryGetValue(i, out ArenaConfig a) && a.worldId == worldId)
                {
                    list.Add(a);
                }
            }
            return list;
        }

        public static WorldConfig GetConfig(int worldId)
        {
            EnsureRegistries();
            if (_worldRegistry.TryGetValue(worldId, out WorldConfig cfg))
            {
                return cfg;
            }
            // Fallback for legacy 1..5 callers: map 4 and 5 to World 2 and 3
            if (worldId == 4) return _worldRegistry[2];
            if (worldId >= 5) return _worldRegistry[3];
            return _worldRegistry[1];
        }

        public static List<WorldConfig> GetAllConfigs()
        {
            EnsureRegistries();
            return new List<WorldConfig>
            {
                _worldRegistry[1],
                _worldRegistry[2],
                _worldRegistry[3]
            };
        }

        public static WorldConfig[] AllWorlds
        {
            get
            {
                EnsureRegistries();
                return new WorldConfig[] { _worldRegistry[1], _worldRegistry[2], _worldRegistry[3] };
            }
        }

        public static ArenaConfig[] AllArenas
        {
            get
            {
                EnsureRegistries();
                ArenaConfig[] arr = new ArenaConfig[15];
                for (int i = 1; i <= 15; i++) arr[i - 1] = _arenaRegistry[i];
                return arr;
            }
        }

        // Legacy Factory Methods for test scripts
        public static WorldConfig CreateWorld1() => GetConfig(1);
        public static WorldConfig CreateWorld2() => GetConfig(2);
        public static WorldConfig CreateWorld3() => GetConfig(3);
        public static WorldConfig CreateWorld4() => GetConfig(2);
        public static WorldConfig CreateWorld5() => GetConfig(3);

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
                if (bossEncounters[i].waveNumber == wave)
                {
                    return bossEncounters[i];
                }
            }
            return null;
        }

        public int GetEnemyCountForWave(int wave)
        {
            if (waveEnemyCounts == null || waveEnemyCounts.Length == 0) return 10;
            int idx = Mathf.Clamp(wave - 1, 0, waveEnemyCounts.Length - 1);
            return waveEnemyCounts[idx];
        }

        private static void EnsureRegistries()
        {
            if (_arenaRegistry != null && _worldRegistry != null) return;

            _arenaRegistry = new Dictionary<int, ArenaConfig>();
            _worldRegistry = new Dictionary<int, WorldConfig>();

            // Build All 15 Arenas
            // WORLD 1: THE CURSED REALMS (Arenas 1–5 from existing environments)
            _arenaRegistry[1] = CreateArena(1, 1, 1, "THE BLIGHTED HOMESTEAD", "Arena 1-1", "A moonlit rural farm under eerie quietude. Learn the arts of survival and reaping.", "NORMAL", 0, 0.80f, 0.85f, 1.0f, 1.0f, 1, "THE LAST HARVEST", "Assets/_Prefabs/Enemy_TheLastHarvest_Boss.prefab", new Color(0.95f, 0.65f, 0.20f));
            _arenaRegistry[2] = CreateArena(2, 1, 2, "ASHEN ORCHARD", "Arena 1-2", "Scorched apple orchards enveloped in choking ember ash. Watch for charger ambushes.", "NORMAL", 1, 0.85f, 0.90f, 1.02f, 1.05f, 2, "THE ASHEN REAPER", "Assets/_Prefabs/Boss_TheAshenReaper.prefab", new Color(0.90f, 0.50f, 0.20f));
            _arenaRegistry[3] = CreateArena(3, 1, 3, "BLACKENED MIRE", "Arena 1-3", "Stagnant bog waters and rotting trees. Pestilent miasma clouds test your footing.", "NORMAL", 2, 0.90f, 0.95f, 1.03f, 1.10f, 3, "THE MIRE MOTHER", "Assets/_Prefabs/Boss_TheMireMother.prefab", new Color(0.45f, 0.70f, 0.40f));
            _arenaRegistry[4] = CreateArena(4, 1, 4, "BLOODWOOD GROVE", "Arena 1-4", "Crimson timberland dripping with cursed sap. Heavy timber goliaths guard the paths.", "HARD", 3, 0.95f, 1.00f, 1.05f, 1.15f, 4, "TIMBER GOLIATH", "Assets/_Prefabs/Boss_TimberGoliath.prefab", new Color(0.85f, 0.28f, 0.25f));
            _arenaRegistry[5] = CreateArena(5, 1, 5, "THE CONSECRATED CLEARING", "Arena 1-5", "The ancient heart of the Cursed Realms. Face the Sovereign of the Bleeding Bark.", "HARD", 4, 1.00f, 1.05f, 1.06f, 1.20f, 4, "BLOODWOOD SOVEREIGN", "Assets/_Prefabs/Boss_BloodwoodSovereign.prefab", new Color(0.88f, 0.22f, 0.20f));

            // WORLD 2: GRAVEBORN KINGDOM (Arenas 6–10)
            _arenaRegistry[6] = CreateArena(6, 2, 1, "THE FORSAKEN GRAVEYARD", "Arena 2-1", "Weathered tombstones and iron railings. Restless souls emerge from the cold earth.", "HARD", 5, 1.05f, 1.08f, 1.08f, 1.25f, 2, "GRAVE WARDEN", "Assets/_Prefabs/Boss_TheAshenReaper.prefab", new Color(0.60f, 0.68f, 0.75f));
            _arenaRegistry[7] = CreateArena(7, 2, 2, "THE PLAGUE VILLAGE", "Arena 2-2", "Abandoned timber cottages fallen to pestilence. Rotting shamblers swarm the alleys.", "HARD", 6, 1.08f, 1.10f, 1.09f, 1.30f, 3, "PLAGUE MATRON", "Assets/_Prefabs/Boss_TheMireMother.prefab", new Color(0.52f, 0.65f, 0.58f));
            _arenaRegistry[8] = CreateArena(8, 2, 3, "THE SUNKEN CRYPT", "Arena 2-3", "Flooded subterranean stone crypts. Deep pools of blood sap make maneuvering lethal.", "VERY HARD", 7, 1.12f, 1.15f, 1.10f, 1.35f, 3, "CRYPT BEAST", "Assets/_Prefabs/Boss_TimberGoliath.prefab", new Color(0.48f, 0.60f, 0.70f));
            _arenaRegistry[9] = CreateArena(9, 2, 4, "THE BLACK CHAPEL", "Arena 2-4", "Desecrated temple ruins illuminated by dim moonbeams. High-density elite fiends.", "VERY HARD", 8, 1.15f, 1.20f, 1.11f, 1.40f, 5, "HERALD OF BONES", "Assets/_Prefabs/Boss_EclipseHerald.prefab", new Color(0.55f, 0.50f, 0.75f));
            _arenaRegistry[10] = CreateArena(10, 2, 5, "THE OSSUARY THRONE", "Arena 2-5", "The throne of bleached bone. Face THE GRAVE KING to shatter his royal curse.", "VERY HARD", 9, 1.18f, 1.25f, 1.12f, 1.45f, 2, "THE GRAVE KING", "Assets/_Prefabs/Boss_TheAshenReaper.prefab", new Color(0.72f, 0.78f, 0.85f));

            // WORLD 3: THE INFERNAL ECLIPSE (Arenas 11–15)
            _arenaRegistry[11] = CreateArena(11, 3, 1, "ASHEN GATE", "Arena 3-1", "The towering obsidian gate leading to the eternal eclipse. Burning embers fall like rain.", "NIGHTMARE", 10, 1.22f, 1.25f, 1.14f, 1.50f, 2, "GATE GUARDIAN", "Assets/_Prefabs/Boss_TimberGoliath.prefab", new Color(0.85f, 0.40f, 0.20f));
            _arenaRegistry[12] = CreateArena(12, 3, 2, "THE BURNING GROVE", "Arena 3-2", "Trees of molten bark and blazing thorns. Aggressive fire demons close distance rapidly.", "NIGHTMARE", 11, 1.25f, 1.28f, 1.15f, 1.55f, 4, "INFERNAL SOVEREIGN", "Assets/_Prefabs/Boss_BloodwoodSovereign.prefab", new Color(0.92f, 0.32f, 0.18f));
            _arenaRegistry[13] = CreateArena(13, 3, 3, "THE VOID SANCTUM", "Arena 3-3", "An abyssal sanctum where gravity weakens. Void rifts warp space and pull unwary harvesters.", "NIGHTMARE", 12, 1.28f, 1.30f, 1.16f, 1.60f, 5, "ECLIPSE HERALD", "Assets/_Prefabs/Boss_EclipseHerald.prefab", new Color(0.68f, 0.35f, 0.85f));
            _arenaRegistry[14] = CreateArena(14, 3, 4, "THE ECLIPSE TEMPLE", "Arena 3-4", "The inner ritual sanctum beneath the bleeding sun. Surging waves of masked abominations.", "NIGHTMARE", 13, 1.32f, 1.35f, 1.18f, 1.65f, 5, "HERALD OF THE END", "Assets/_Prefabs/Boss_EclipseHerald.prefab", new Color(0.75f, 0.30f, 0.90f));
            _arenaRegistry[15] = CreateArena(15, 3, 5, "THE LAST ECLIPSE", "Arena 3-5", "The cataclysmic summit of perpetual night. Vanquish THE PRIMEVAL HORROR to save all realms.", "NIGHTMARE", 14, 1.35f, 1.40f, 1.20f, 1.70f, 5, "THE PRIMEVAL HORROR", "Assets/_Prefabs/Boss_PrimevalHorror.prefab", new Color(0.82f, 0.28f, 0.95f));

            // World Configs
            _worldRegistry[1] = new WorldConfig
            {
                worldId = 1,
                displayName = "THE CURSED REALMS",
                subtitle = "World 1 — The Ancient Blight",
                description = "5 perilous rural arenas. Reclaim the blighted farm, ash orchards, and murky mires.",
                difficultyTier = "NORMAL",
                totalWaves = 10,
                unlockRequirement = 0,
                themeColor = new Color(0.95f, 0.65f, 0.20f),
                activeArenaIndex = 1,
                activeArena = _arenaRegistry[1]
            };

            _worldRegistry[2] = new WorldConfig
            {
                worldId = 2,
                displayName = "GRAVEBORN KINGDOM",
                subtitle = "World 2 — Dominion of Bones",
                description = "5 grim cemetery and chapel arenas. Overthrow The Grave King in his ossuary throne.",
                difficultyTier = "HARD",
                totalWaves = 10,
                unlockRequirement = 1,
                themeColor = new Color(0.60f, 0.68f, 0.78f),
                activeArenaIndex = 1,
                activeArena = _arenaRegistry[6]
            };

            _worldRegistry[3] = new WorldConfig
            {
                worldId = 3,
                displayName = "THE INFERNAL ECLIPSE",
                subtitle = "World 3 — The Sunless Abyss",
                description = "5 nightmare void arenas. Conquer the Ashen Gate and defeat The Primeval Horror.",
                difficultyTier = "NIGHTMARE",
                totalWaves = 15,
                unlockRequirement = 2,
                themeColor = new Color(0.75f, 0.30f, 0.90f),
                activeArenaIndex = 1,
                activeArena = _arenaRegistry[11]
            };
        }

        private static ArenaConfig CreateArena(
            int arenaId, int worldId, int indexInWorld, string name, string subtitle, string desc,
            string tier, int reqId, float dmgMult, float bossDmgMult, float spdMult, float hpMult,
            int visualIdx, string bossName, string bossPrefab, Color color)
        {
            int totalWaves = (worldId == 3) ? 15 : 10;
            int[] waveCounts;
            int[] bossWaves;
            BossEncounterConfig[] encounters;

            if (worldId == 3)
            {
                // 15 Waves: Waves 1-9 progression, Wave 10 MID-BOSS, Waves 11-14 second difficulty phase, Wave 15 FINAL BOSS
                waveCounts = new int[]
                {
                    10 + indexInWorld * 2, 14 + indexInWorld * 2, 18 + indexInWorld * 3, 22 + indexInWorld * 3, 26 + indexInWorld * 4,
                    30 + indexInWorld * 4, 34 + indexInWorld * 4, 38 + indexInWorld * 5, 42 + indexInWorld * 5, 20 + indexInWorld * 2, // Wave 10 Mid-boss adds
                    32 + indexInWorld * 4, 36 + indexInWorld * 5, 42 + indexInWorld * 5, 48 + indexInWorld * 6, 24 + indexInWorld * 3  // Wave 15 Final boss adds
                };
                bossWaves = new int[] { 10, 15 };
                encounters = new BossEncounterConfig[]
                {
                    new BossEncounterConfig
                    {
                        waveNumber = 10,
                        isFinalBoss = false,
                        bossName = (indexInWorld == 5) ? "ECLIPSE HERALD" : "CORRUPTED SENTINEL",
                        bossTitle = $"Mid-Boss of {name}",
                        bossAddCount = 18 + indexInWorld * 2,
                        bossGruntQuota = 6,
                        bossChaserQuota = 5,
                        bossWalkerQuota = 4,
                        bossFarmerQuota = 3,
                        bossChargerQuota = 2,
                        bossMaxConcurrentAdds = 10,
                        bossAddSpawnInterval = 1.8f,
                        bossScale = 1.10f,
                        bossHealthMultiplier = 0.90f + indexInWorld * 0.10f,
                        bossPrefabPath = "Assets/_Prefabs/Boss_EclipseHerald.prefab"
                    },
                    new BossEncounterConfig
                    {
                        waveNumber = 15,
                        isFinalBoss = true,
                        bossName = bossName,
                        bossTitle = $"Final Guardian of {name}",
                        bossAddCount = 22 + indexInWorld * 2,
                        bossGruntQuota = 7,
                        bossChaserQuota = 6,
                        bossWalkerQuota = 5,
                        bossFarmerQuota = 4,
                        bossChargerQuota = 3,
                        bossMaxConcurrentAdds = 12,
                        bossAddSpawnInterval = 1.7f,
                        bossScale = 1.25f,
                        bossHealthMultiplier = 1.20f + indexInWorld * 0.15f,
                        bossPrefabPath = bossPrefab
                    }
                };
            }
            else
            {
                // World 1 & 2: 10 Waves (Waves 1-9 progression, Wave 10 FINAL BOSS)
                waveCounts = new int[]
                {
                    8 + indexInWorld * 2, 12 + indexInWorld * 2, 16 + indexInWorld * 3, 20 + indexInWorld * 3, 24 + indexInWorld * 4,
                    28 + indexInWorld * 4, 32 + indexInWorld * 4, 36 + indexInWorld * 5, 40 + indexInWorld * 5, 18 + indexInWorld * 2  // Wave 10 Final boss adds
                };
                bossWaves = new int[] { 10 };
                encounters = new BossEncounterConfig[]
                {
                    new BossEncounterConfig
                    {
                        waveNumber = 10,
                        isFinalBoss = true,
                        bossName = bossName,
                        bossTitle = $"Guardian of {name}",
                        bossAddCount = 18 + indexInWorld * 2,
                        bossGruntQuota = 6,
                        bossChaserQuota = 5,
                        bossWalkerQuota = 4,
                        bossFarmerQuota = 3,
                        bossChargerQuota = 2,
                        bossMaxConcurrentAdds = 10,
                        bossAddSpawnInterval = 1.8f,
                        bossScale = 1.20f,
                        bossHealthMultiplier = 1.0f + indexInWorld * 0.12f,
                        bossPrefabPath = bossPrefab
                    }
                };
            }

            return new ArenaConfig
            {
                arenaId = arenaId,
                worldId = worldId,
                arenaIndexInWorld = indexInWorld,
                arenaName = name,
                subtitle = subtitle,
                description = desc,
                difficultyTier = tier,
                totalWaves = totalWaves,
                unlockRequirementArenaId = reqId,
                enemyDamageMultiplier = dmgMult,
                bossDamageMultiplier = bossDmgMult,
                enemySpeedMultiplier = spdMult,
                enemyHealthMultiplier = hpMult,
                hitInvulnerabilityDuration = 0.55f,
                dashCooldown = 1.90f,
                healthDropChance = 0.04f,
                eliteHealthDropChance = 0.35f,
                luckyLastHealthDropChance = 0.08f,
                luckyLastEliteHealthDropChance = 0.50f,
                normalHealAmount = 20f,
                luckyLastHealAmount = 25f,
                luckyLastMinimumHP = 15f,
                waveEnemyCounts = waveCounts,
                maxConcurrentEnemies = 70,
                spawnInterval = 0.48f,
                intermissionDuration = 8.0f,
                supplyDropWaves = (worldId == 3) ? new int[] { 3, 7, 12 } : new int[] { 3, 7 },
                preBossWave = (worldId == 3) ? 14 : 9,
                bossWaves = bossWaves,
                bossEncounters = encounters,
                environmentIdentifier = $"Env_Arena_{arenaId}",
                musicIdentifier = $"Music_World{worldId}",
                themeColor = color,
                environmentVisualIndex = visualIdx
            };
        }
    }
}
