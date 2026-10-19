using UnityEditor;
using UnityEngine;

namespace CursedHarvest.Editor
{
    public static class CursedHarvestDebugMenu
    {
        [MenuItem("Cursed Harvest/Debug/Unlock All 5 Worlds")]
        public static void UnlockAllWorlds()
        {
            if (GameSession.Instance != null)
            {
                GameSession.Instance.UnlockAllWorlds();
                Debug.Log("[CursedHarvestDebugMenu] All 5 Worlds Unlocked via Editor Menu!");
            }
            else
            {
                PlayerPrefs.SetInt("CH_HighestUnlockedWorld", 5);
                PlayerPrefs.Save();
                Debug.Log("[CursedHarvestDebugMenu] PlayerPrefs CH_HighestUnlockedWorld set to 5.");
            }

            if (MainMenuController.Instance != null)
            {
                MainMenuController.Instance.SelectWorld(1);
            }
        }

        [MenuItem("Cursed Harvest/Debug/Reset World Progress to W1")]
        public static void ResetWorldProgress()
        {
            if (GameSession.Instance != null)
            {
                GameSession.Instance.ResetProgress();
                Debug.Log("[CursedHarvestDebugMenu] World Progress reset to World 1 via Editor Menu!");
            }
            else
            {
                PlayerPrefs.SetInt("CH_HighestUnlockedWorld", 1);
                for (int i = 1; i <= 5; i++)
                {
                    PlayerPrefs.SetInt($"CH_WorldCompleted_{i}", 0);
                }
                PlayerPrefs.Save();
                Debug.Log("[CursedHarvestDebugMenu] PlayerPrefs World Progress reset to World 1.");
            }

            if (MainMenuController.Instance != null)
            {
                MainMenuController.Instance.SelectWorld(1);
            }
        }

        // ==================== META PROGRESSION DEBUG TOOLS ====================

        [MenuItem("Cursed Harvest/Debug/Meta Progression/Add 1000 Essence")]
        public static void Add1000Essence()
        {
            if (MetaProgressionManager.Instance != null)
            {
                MetaProgressionManager.Instance.AddEssence(1000, false);
                Debug.Log($"[CursedHarvestDebugMenu] Added 1,000 Harvest Essence. Total: {MetaProgressionManager.Instance.HarvestEssence}");
            }
            else
            {
                int cur = PlayerPrefs.GetInt("CH_HarvestEssence", 0);
                PlayerPrefs.SetInt("CH_HarvestEssence", cur + 1000);
                PlayerPrefs.Save();
                Debug.Log($"[CursedHarvestDebugMenu] Added 1,000 Harvest Essence to PlayerPrefs. Total: {cur + 1000}");
            }

            if (MainMenuController.Instance != null)
            {
                MainMenuController.Instance.RefreshUpgradesUI();
            }
        }

        [MenuItem("Cursed Harvest/Debug/Meta Progression/Reset Meta Upgrades")]
        public static void ResetMetaUpgrades()
        {
            if (MetaProgressionManager.Instance != null)
            {
                MetaProgressionManager.Instance.ResetAllProgression();
                Debug.Log("[CursedHarvestDebugMenu] All Meta Upgrades and Essence reset!");
            }
            else
            {
                PlayerPrefs.SetInt("CH_HarvestEssence", 0);
                foreach (MetaUpgradeBranch branch in System.Enum.GetValues(typeof(MetaUpgradeBranch)))
                {
                    PlayerPrefs.SetInt($"CH_Meta_{branch}", 0);
                    PlayerPrefs.SetInt($"CH_MetaUpgrade_{branch}", 0);
                }
                PlayerPrefs.Save();
                Debug.Log("[CursedHarvestDebugMenu] Meta Upgrades & Essence reset in PlayerPrefs.");
            }

            if (MainMenuController.Instance != null)
            {
                MainMenuController.Instance.RefreshUpgradesUI();
            }
        }

        [MenuItem("Cursed Harvest/Debug/Meta Progression/Set All Meta Lv5")]
        public static void SetAllMetaLv5()
        {
            if (MetaProgressionManager.Instance != null)
            {
                MetaProgressionManager.Instance.SetAllMaxRank();
                Debug.Log("[CursedHarvestDebugMenu] All 8 Meta Upgrades set to Level 5 (MAX)!");
            }
            else
            {
                foreach (MetaUpgradeBranch branch in System.Enum.GetValues(typeof(MetaUpgradeBranch)))
                {
                    PlayerPrefs.SetInt($"CH_Meta_{branch}", 5);
                    PlayerPrefs.SetInt($"CH_MetaUpgrade_{branch}", 5);
                }
                PlayerPrefs.Save();
                Debug.Log("[CursedHarvestDebugMenu] Set All Meta Upgrades to Level 5 in PlayerPrefs.");
            }

            if (MainMenuController.Instance != null)
            {
                MainMenuController.Instance.RefreshUpgradesUI();
            }
        }

        [MenuItem("Cursed Harvest/Debug/Print Effective Stats")]
        [MenuItem("Cursed Harvest/Debug/Meta Progression/Print Effective Player Stats")]
        public static void PrintEffectiveStats()
        {
            int worldId = GameSession.Instance != null ? GameSession.Instance.SelectedWorldId : 1;
            WorldConfig wc = WorldConfig.GetConfig(worldId);
            string worldName = wc != null ? wc.displayName : $"World {worldId}";
            string tier = wc != null ? wc.difficultyTier : "NORMAL";

            GameObject player = GameObject.FindWithTag("Player");
            PlayerHealth ph = player != null ? player.GetComponent<PlayerHealth>() : null;
            PlayerController pc = player != null ? player.GetComponent<PlayerController>() : null;
            WeaponManager wm = player != null ? player.GetComponent<WeaponManager>() : null;
            LuckyLastSystem lls = player != null ? player.GetComponent<LuckyLastSystem>() : null;
            UpgradeManager um = player != null ? player.GetComponent<UpgradeManager>() : null;
            HarvestPlot hp = Object.FindFirstObjectByType<HarvestPlot>();

            // 1. Max HP
            float baseHp = ph != null ? ph.BaseMaxHealth : 100f;
            float worldHpMod = 0f;
            float metaVit = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.GetVitalityBonus() : 0f;
            float runHp = (um != null) ? (ph != null ? ph.MaxHealth - baseHp - metaVit : 0f) : 0f;
            float tempHp = 0f;
            float finalMaxHp = ph != null ? ph.MaxHealth : (baseHp + metaVit + runHp);

            // 2. Damage Multiplier
            float baseDmg = 1.00f;
            float worldDmg = 1.00f;
            float metaPower = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.GetPowerMultiplier() : 1f;
            float runDmg = wm != null ? wm.RunDamageMultiplier : 1f;
            float tempDmg = 0f;
            float finalDmg = wm != null ? wm.GlobalDamageMultiplier : (metaPower * runDmg + tempDmg);

            // 3. Attack Speed / Cooldown
            float baseAtkSpd = 1.00f;
            float worldAtkSpd = 1.00f;
            float metaHaste = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.GetHasteMultiplier() : 1f;
            float runCdMult = wm != null ? wm.RunCooldownMultiplier : 1f;
            float tempCdMult = 1.00f;
            float finalCdMult = wm != null ? wm.GlobalCooldownMultiplier : (runCdMult / metaHaste);
            float finalAtkSpd = finalCdMult > 0.01f ? (1f / finalCdMult) : 1f;

            // 4. Move Speed
            float baseSpeed = 6.80f;
            float worldSpeed = 1.00f;
            float metaAgi = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.GetAgilityMultiplier() : 1f;
            float runSpeedMult = pc != null ? pc.SpeedMultiplier : 1f;
            float tempSpeedBonus = 0f;
            float finalSpeed = pc != null ? pc.MoveSpeed : (baseSpeed * metaAgi * runSpeedMult);

            // 5. Fortune
            float baseLucky = 0.70f;
            float worldLucky = 0f;
            float metaFortune = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.GetFortuneBonus() : 0f;
            float runLucky = um != null ? um.BetterHarvestBonus : 0f;
            float tempLucky = (ph != null && ph.HealthPercent <= 0.25f) ? (0.25f - ph.HealthPercent) * 0.60f : 0f;
            float finalLucky = hp != null ? hp.CalculateLuckyChance() : Mathf.Clamp(baseLucky + metaFortune + runLucky + tempLucky, 0.10f, 0.85f);
            float metaEssenceMult = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.GetFortuneEssenceMultiplier() : 1f;

            // 6. Health Restore Multiplier
            float baseHeal = 20.0f;
            float worldHeal = WorldDifficultyConfig.Current != null ? (LuckyLastSystem.IsAnyActive ? WorldDifficultyConfig.Current.luckyLastHealAmount : WorldDifficultyConfig.Current.normalHealAmount) : 20f;
            float metaSurvivalMult = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.GetSurvivalHealMultiplier() : 1f;
            float metaRegen = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.GetSurvivalRegenRate() : 0f;
            float runHeal = 1.00f;
            float tempHeal = 1.00f;
            float finalHeal = WorldDifficultyConfig.Current != null ? WorldDifficultyConfig.Current.GetEffectiveHealAmount(LuckyLastSystem.IsAnyActive) : (worldHeal * metaSurvivalMult);

            // 7. Lucky Last Duration
            float baseLuckyDur = 12.0f;
            float worldLuckyDur = 0f;
            float metaLuckyDur = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.GetLuckyLastDurationBonus() : 0f;
            float runLuckyDur = 0f;
            float tempLuckyDur = 0f;
            float finalLuckyDur = lls != null ? lls.TotalDuration : (baseLuckyDur + metaLuckyDur);

            // 8. Lucky Last Recovery HP
            float baseRecoveryHp = 1f;
            float worldRecoveryHp = WorldDifficultyConfig.Current != null ? WorldDifficultyConfig.Current.luckyLastMinimumHP : 15f;
            float metaRecoveryHp = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.GetLuckyLastRecoveryHPBonus() : 0f;
            float runRecoveryHp = 0f;
            float tempRecoveryHp = 0f;
            float finalRecoveryHp = worldRecoveryHp + metaRecoveryHp;

            // 9. Harvest Channel Multiplier
            float baseChannelTime = 3.50f;
            float worldChannelMult = 1.00f;
            float metaChannelMult = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.GetHarvestChannelMultiplier() : 1f;
            float runChannelMult = 1.00f;
            float tempChannelMult = 1.00f;
            float finalChannelTime = hp != null ? hp.EffectiveGrowDuration : (baseChannelTime * metaChannelMult);

            string report = 
                "====================================================================================================\n" +
                $"[EFFECTIVE STATS AUDIT] WORLD: {worldId} — {worldName} ({tier})\n" +
                "====================================================================================================\n" +
                $"1. Max HP:\n" +
                $"   Base={baseHp:F1} | World=+{worldHpMod:F1} | Meta=+{metaVit:F1} | Run=+{runHp:F1} | Temporary=+{tempHp:F1} -> FINAL={finalMaxHp:F1} HP\n" +
                $"2. Damage Multiplier:\n" +
                $"   Base={baseDmg:F2}x | World={worldDmg:F2}x | Meta={metaPower:F2}x | Run={runDmg:F2}x | Temporary=+{tempDmg:F2} -> FINAL={finalDmg:F2}x\n" +
                $"3. Attack Speed:\n" +
                $"   Base={baseAtkSpd:F2}x | World={worldAtkSpd:F2}x | Meta={metaHaste:F2}x | Run={runCdMult:F2}x | Temporary={tempCdMult:F2}x -> FINAL={finalAtkSpd:F2}x (CD Factor: {finalCdMult:F2}x)\n" +
                $"4. Move Speed:\n" +
                $"   Base={baseSpeed:F2} m/s | World={worldSpeed:F2}x | Meta={metaAgi:F2}x | Run={runSpeedMult:F2}x | Temporary=+{tempSpeedBonus:F2} -> FINAL={finalSpeed:F2} m/s\n" +
                $"5. Fortune:\n" +
                $"   Base={baseLucky:P0} | World=+{worldLucky:P0} | Meta=+{metaFortune:P0} | Run=+{runLucky:P0} | Temporary=+{tempLucky:P0} -> FINAL={finalLucky:P1} (Cap 85%) [Essence: {metaEssenceMult:F2}x]\n" +
                $"6. Health Restore Multiplier:\n" +
                $"   Base={baseHeal:F1} HP | World={worldHeal:F1} HP | Meta={metaSurvivalMult:F2}x (+{metaRegen:F1} HP/s) | Run={runHeal:F2}x | Temporary={tempHeal:F2}x -> FINAL={finalHeal:F1} HP\n" +
                $"7. Lucky Last Duration:\n" +
                $"   Base={baseLuckyDur:F1}s | World=+{worldLuckyDur:F1}s | Meta=+{metaLuckyDur:F1}s | Run=+{runLuckyDur:F1}s | Temporary=+{tempLuckyDur:F1}s -> FINAL={finalLuckyDur:F1}s\n" +
                $"8. Lucky Last Recovery HP:\n" +
                $"   Base={baseRecoveryHp:F0} HP | World={worldRecoveryHp:F0} HP | Meta=+{metaRecoveryHp:F0} HP | Run=+{runRecoveryHp:F0} HP | Temporary=+{tempRecoveryHp:F0} HP -> FINAL={finalRecoveryHp:F0} HP\n" +
                $"9. Harvest Channel Multiplier:\n" +
                $"   Base={baseChannelTime:F2}s | World={worldChannelMult:F2}x | Meta={metaChannelMult:F2}x | Run={runChannelMult:F2}x | Temporary={tempChannelMult:F2}x -> FINAL={finalChannelTime:F2}s (Speed: {(1f/metaChannelMult):F2}x)\n" +
                "====================================================================================================";

            Debug.Log(report);
        }

        [MenuItem("Cursed Harvest/Debug/Complete Current Wave")]
        public static void CompleteCurrentWave()
        {
            SpawnDirector sd = Object.FindFirstObjectByType<SpawnDirector>();
            if (sd != null)
            {
                sd.ForceClearCurrentWave();
                Debug.Log($"[CursedHarvestDebugMenu] Force cleared Wave {sd.CurrentWave}!");
            }
            else
            {
                Debug.LogWarning("[CursedHarvestDebugMenu] No active SpawnDirector found in scene.");
            }
        }

        [MenuItem("Cursed Harvest/Debug/Spawn The Mire Mother")]
        public static void SpawnMireMother()
        {
            SpawnDirector sd = Object.FindFirstObjectByType<SpawnDirector>();
            if (sd != null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Boss_TheMireMother.prefab");
                if (prefab != null)
                {
                    Transform player = GameObject.FindWithTag("Player")?.transform;
                    Vector3 pos = player != null ? player.position + Vector3.forward * 14f : new Vector3(0, 0.5f, 14f);
                    GameObject boss = Object.Instantiate(prefab, pos, Quaternion.identity);
                    BossController bc = boss.GetComponent<BossController>();
                    if (bc != null) bc.Initialize();
                    Debug.Log("[CursedHarvestDebugMenu] Spawned The Mire Mother!");
                }
                else
                {
                    Debug.LogWarning("[CursedHarvestDebugMenu] Boss_TheMireMother.prefab not found at Assets/_Prefabs/Boss_TheMireMother.prefab");
                }
            }
            else
            {
                Debug.LogWarning("[CursedHarvestDebugMenu] No active SpawnDirector found in scene.");
            }
        }
    }
}
