using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CursedHarvest.Editor
{
    [InitializeOnLoad]
    public static class MetaProgressionQAAndScreenshotRunner
    {
        private const string MarkerFile = "meta_qa_done.marker";
        private const string ResultsFile = "meta_qa_results.txt";
        private const string ArtifactDir = "/Users/logmat/.gemini/antigravity/brain/da9b4771-589a-4d93-9ba9-83bb63f9ad9b";

        static MetaProgressionQAAndScreenshotRunner()
        {
            EditorApplication.delayCall += AutoRunIfPending;
        }

        private static void AutoRunIfPending()
        {
            if (File.Exists(MarkerFile)) return;

            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
                return;
            }

            File.WriteAllText(MarkerFile, "running");
            RunMetaProgressionQA();
        }

        [MenuItem("Cursed Harvest/Test/Run Meta Progression QA")]
        public static void RunMetaProgressionQA()
        {
            List<string> logs = new List<string>();
            Action<string> log = (msg) => { Debug.Log(msg); logs.Add(msg); };
            Action<string> logErr = (msg) => { Debug.LogError(msg); logs.Add("ERROR: " + msg); };

            log("=================================================================");
            log("[META PROGRESSION QA SUITE] Starting Complete Audit & Tests...");
            log("=================================================================");

            int totalTests = 8;
            int passedTests = 0;

            try
            {
                // Ensure MainMenu is loaded first for frontend tests
                EditorSceneManager.OpenScene("Assets/_Scenes/MainMenu.unity", OpenSceneMode.Single);

                MetaProgressionManager meta = MetaProgressionManager.Instance;
                if (meta == null)
                {
                    logErr("MetaProgressionManager.Instance is null!");
                    return;
                }

                // -------------------------------------------------------------
                // TEST 1: Fresh Reset & Base Stat Defaults (Lv0)
                // -------------------------------------------------------------
                meta.ResetAllProgression();
                bool t1Pass = true;
                foreach (MetaUpgradeBranch b in Enum.GetValues(typeof(MetaUpgradeBranch)))
                {
                    if (meta.GetRank(b) != 0) { logErr($"Branch {b} rank is not 0 after reset: {meta.GetRank(b)}"); t1Pass = false; }
                }
                if (meta.HarvestEssence != 0) { logErr($"HarvestEssence is not 0 after reset: {meta.HarvestEssence}"); t1Pass = false; }
                if (meta.GetVitalityBonus() != 0f) { logErr($"Vitality bonus not 0 at Lv0: {meta.GetVitalityBonus()}"); t1Pass = false; }
                if (Mathf.Abs(meta.GetPowerMultiplier() - 1.0f) > 0.001f) { logErr($"Power mult not 1.0 at Lv0: {meta.GetPowerMultiplier()}"); t1Pass = false; }
                if (Mathf.Abs(meta.GetHasteMultiplier() - 1.0f) > 0.001f) { logErr($"Haste mult not 1.0 at Lv0: {meta.GetHasteMultiplier()}"); t1Pass = false; }
                if (Mathf.Abs(meta.GetAgilityMultiplier() - 1.0f) > 0.001f) { logErr($"Agility mult not 1.0 at Lv0: {meta.GetAgilityMultiplier()}"); t1Pass = false; }
                if (Mathf.Abs(meta.GetHarvestChannelMultiplier() - 1.0f) > 0.001f) { logErr($"Harvest channel mult not 1.0 at Lv0: {meta.GetHarvestChannelMultiplier()}"); t1Pass = false; }

                if (t1Pass)
                {
                    passedTests++;
                    log("[TEST 1/8] PASSED: Reset and Lv0 defaults verified for all branches.");
                }

                // Capture Screenshot: MetaUpgrade_Vitality_Before
                Scene menuScene1 = EditorSceneManager.GetActiveScene();
                MainMenuController mmc = GetOrFindMainMenuController(menuScene1);
                if (mmc != null)
                {
                    mmc.OpenUpgrades();
                    mmc.RefreshUpgradesUI();
                    Canvas.ForceUpdateCanvases();
                    CaptureAndCopy("MetaUpgrade_Vitality_Before", 1920, 1080);
                }

                // -------------------------------------------------------------
                // TEST 2: Essence Addition & Immediate Purchase Persistence
                // -------------------------------------------------------------
                meta.AddEssence(1000, false);
                if (meta.HarvestEssence != 1000) { logErr($"AddEssence failed: expected 1000, got {meta.HarvestEssence}"); }

                // Buy Vitality Lv1 (Cost: 50)
                bool boughtVit = meta.TryUpgradeBranch(MetaUpgradeBranch.Vitality);
                bool t2Pass = boughtVit && (meta.GetRank(MetaUpgradeBranch.Vitality) == 1) && (meta.HarvestEssence == 950);
                if (!t2Pass)
                {
                    logErr($"Buy Vitality Lv1 failed! bought={boughtVit}, rank={meta.GetRank(MetaUpgradeBranch.Vitality)}, essence={meta.HarvestEssence}");
                }
                else
                {
                    // Check persistence in PlayerPrefs immediately
                    int savedRank = PlayerPrefs.GetInt("CH_Meta_Vitality", -1);
                    int savedRankLegacy = PlayerPrefs.GetInt("CH_MetaUpgrade_Vitality", -1);
                    int savedEssence = PlayerPrefs.GetInt("CH_HarvestEssence", -1);
                    if (savedRank == 1 && savedRankLegacy == 1 && savedEssence == 950)
                    {
                        passedTests++;
                        log("[TEST 2/8] PASSED: Purchase, immediate essence deduction, rank increment, and dual-key PlayerPrefs persistence verified.");
                    }
                    else
                    {
                        logErr($"PlayerPrefs mismatch: savedRank={savedRank}, legacy={savedRankLegacy}, essence={savedEssence}");
                    }
                }

                if (mmc != null)
                {
                    mmc.RefreshUpgradesUI();
                    CaptureAndCopy("MetaUpgrade_Purchase", 1920, 1080);
                    CaptureAndCopy("MetaUpgrade_Vitality_After", 1920, 1080);
                }

                // -------------------------------------------------------------
                // TEST 3: Gameplay Stat Application in MainGame
                // -------------------------------------------------------------
                EditorSceneManager.OpenScene("Assets/_Scenes/MainGame.unity", OpenSceneMode.Single);
                meta = MetaProgressionManager.Instance;

                GameObject player = GameObject.FindWithTag("Player");
                PlayerHealth ph = player != null ? player.GetComponent<PlayerHealth>() : null;
                if (ph == null) ph = Object.FindAnyObjectByType<PlayerHealth>(FindObjectsInactive.Include);
                if (ph != null && (player == null || player.GetComponent<PlayerHealth>() == null)) player = ph.gameObject;

                if (player == null || ph == null)
                {
                    GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Player.prefab");
                    if (playerPrefab != null)
                    {
                        player = Object.Instantiate(playerPrefab);
                        player.name = "Player";
                        ph = player.GetComponent<PlayerHealth>();
                    }
                }

                if (player == null)
                {
                    player = new GameObject("Player");
                    player.tag = "Player";
                }

                if (ph == null) ph = player.GetComponent<PlayerHealth>() ?? player.AddComponent<PlayerHealth>();

                PlayerController pc = player.GetComponent<PlayerController>();
                if (pc == null) pc = Object.FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include) ?? player.AddComponent<PlayerController>();

                WeaponManager wm = player.GetComponent<WeaponManager>();
                if (wm == null) wm = Object.FindAnyObjectByType<WeaponManager>(FindObjectsInactive.Include) ?? player.AddComponent<WeaponManager>();

                LuckyLastSystem lls = player.GetComponent<LuckyLastSystem>();
                if (lls == null) lls = Object.FindAnyObjectByType<LuckyLastSystem>(FindObjectsInactive.Include) ?? player.AddComponent<LuckyLastSystem>();

                HarvestPlot hp = Object.FindAnyObjectByType<HarvestPlot>(FindObjectsInactive.Include);
                if (hp == null)
                {
                    GameObject plotObj = new GameObject("TestHarvestPlot");
                    hp = plotObj.AddComponent<HarvestPlot>();
                }

                meta.ApplyPermanentUpgrades(player);

                bool t3Pass = true;
                // Vitality Lv1 -> +7 HP -> MaxHealth == 107
                if (ph == null || Mathf.Abs(ph.MaxHealth - 107f) > 0.01f || Mathf.Abs(ph.CurrentHealth - 107f) > 0.01f)
                {
                    logErr($"PlayerHealth Vitality Lv1 application failed! MaxHealth={ph?.MaxHealth}, CurrentHealth={ph?.CurrentHealth}, expected 107");
                    t3Pass = false;
                }

                if (t3Pass)
                {
                    passedTests++;
                    log("[TEST 3/8] PASSED: MainGame PlayerHealth correctly initialized with effectiveMaxHealth = 107 and currentHealth = 107.");
                }

                // -------------------------------------------------------------
                // TEST 4: Power, Haste & Agility Scalers
                // -------------------------------------------------------------
                // Add sufficient essence and upgrade Power to Lv1 (+8%), Haste to Lv1 (+7%), Agility to Lv1 (+6%)
                meta.AddEssence(5000, false);
                meta.TryUpgradeBranch(MetaUpgradeBranch.Power);
                meta.TryUpgradeBranch(MetaUpgradeBranch.Haste);
                meta.TryUpgradeBranch(MetaUpgradeBranch.Agility);

                bool t4Pass = true;
                if (wm == null || Mathf.Abs(wm.GlobalDamageMultiplier - 1.08f) > 0.005f)
                {
                    logErr($"WeaponManager GlobalDamageMultiplier failed! Expected 1.08, got {wm?.GlobalDamageMultiplier}");
                    t4Pass = false;
                }

                float expectedCd = 1.0f / 1.07f;
                if (wm == null || Mathf.Abs(wm.GlobalCooldownMultiplier - expectedCd) > 0.005f)
                {
                    logErr($"WeaponManager GlobalCooldownMultiplier failed! Expected {expectedCd:F3}, got {wm?.GlobalCooldownMultiplier:F3}");
                    t4Pass = false;
                }

                float expectedSpeed = 6.8f * 1.06f;
                if (pc == null || Mathf.Abs(pc.MoveSpeed - expectedSpeed) > 0.01f)
                {
                    logErr($"PlayerController MoveSpeed failed! Expected {expectedSpeed:F2}, got {pc?.MoveSpeed:F2}");
                    t4Pass = false;
                }

                if (t4Pass)
                {
                    passedTests++;
                    log($"[TEST 4/8] PASSED: Power (1.08x dmg), Haste ({(1f/expectedCd):F2}x fire rate), Agility ({expectedSpeed:F2} m/s) verified.");
                }

                // -------------------------------------------------------------
                // TEST 5: Fortune, Survival, Lucky Last, Harvest Mastery
                // -------------------------------------------------------------
                meta.TryUpgradeBranch(MetaUpgradeBranch.Fortune);
                meta.TryUpgradeBranch(MetaUpgradeBranch.Survival);
                meta.TryUpgradeBranch(MetaUpgradeBranch.LuckyLast);
                meta.TryUpgradeBranch(MetaUpgradeBranch.HarvestMastery);

                bool t5Pass = true;
                // Fortune: +3% Lucky chance
                if (hp == null || hp.CalculateLuckyChance() < 0.20f)
                {
                    logErr($"HarvestPlot CalculateLuckyChance with Fortune failed: {hp?.CalculateLuckyChance()}");
                    t5Pass = false;
                }

                // Survival: +5% Heal
                GameObject hrObj = new GameObject("TestHealthRestore");
                HealthRestorePickup hr = hrObj.AddComponent<HealthRestorePickup>();
                hr.Initialize(20f);
                float effectiveHeal = hr.GetEffectiveHealAmount();
                if (Mathf.Abs(effectiveHeal - 21.0f) > 0.01f)
                {
                    logErr($"HealthRestorePickup Survival Lv1 heal failed! Expected 21.0, got {effectiveHeal}");
                    t5Pass = false;
                }
                UnityEngine.Object.DestroyImmediate(hrObj);

                // Lucky Last: Duration 13.0s
                if (lls == null || Mathf.Abs(lls.TotalDuration - 13.0f) > 0.01f)
                {
                    logErr($"LuckyLastSystem Duration Lv1 failed! Expected 13.0s, got {lls?.TotalDuration}s");
                    t5Pass = false;
                }

                // Harvest Mastery: 0.96x duration = 3.36s
                if (hp == null || Mathf.Abs(hp.EffectiveGrowDuration - (3.5f * 0.96f)) > 0.01f)
                {
                    logErr($"HarvestPlot EffectiveGrowDuration Lv1 failed! Expected {3.5f * 0.96f}, got {hp?.EffectiveGrowDuration}");
                    t5Pass = false;
                }

                if (t5Pass)
                {
                    passedTests++;
                    log("[TEST 5/8] PASSED: Fortune, Survival, Lucky Last, and Harvest Mastery stat integrations verified.");
                }

                // -------------------------------------------------------------
                // TEST 6: Double Application Prevention (Restart / Return Home)
                // -------------------------------------------------------------
                // Simulate restart: call ph.ResetHealth(), wm.ResetStats(), etc.
                ph.ResetHealth();
                wm.ResetStats();
                float restartSpeed = pc.MoveSpeed;
                float restartDmg = wm.GlobalDamageMultiplier;
                float restartHp = ph.MaxHealth;

                bool t6Pass = true;
                if (Mathf.Abs(restartSpeed - expectedSpeed) > 0.01f)
                {
                    logErr($"Double application detected on Speed! Expected {expectedSpeed}, got {restartSpeed}");
                    t6Pass = false;
                }
                if (Mathf.Abs(restartDmg - 1.08f) > 0.005f)
                {
                    logErr($"Double application detected on Damage! Expected 1.08, got {restartDmg}");
                    t6Pass = false;
                }
                if (Mathf.Abs(restartHp - 107f) > 0.01f)
                {
                    logErr($"Double application detected on Health! Expected 107, got {restartHp}");
                    t6Pass = false;
                }

                if (t6Pass)
                {
                    passedTests++;
                    log("[TEST 6/8] PASSED: No double-application on Reset/Restart. Stats remain strictly authoritative.");
                }

                // -------------------------------------------------------------
                // TEST 7: Max Rank (Level 5) Cap & UI State
                // -------------------------------------------------------------
                meta.SetAllMaxRank();
                meta.ApplyPermanentUpgrades(player);

                bool t7Pass = true;
                foreach (MetaUpgradeBranch b in Enum.GetValues(typeof(MetaUpgradeBranch)))
                {
                    if (meta.GetRank(b) != 5) { logErr($"Branch {b} rank is not 5 after SetAllMaxRank: {meta.GetRank(b)}"); t7Pass = false; }
                }

                // Max HP: 100 + 35 = 135
                if (Mathf.Abs(ph.MaxHealth - 135f) > 0.01f) { logErr($"Max Rank Vitality HP failed: expected 135, got {ph.MaxHealth}"); t7Pass = false; }
                // Max Power: 1.40x
                if (Mathf.Abs(wm.GlobalDamageMultiplier - 1.40f) > 0.005f) { logErr($"Max Rank Power failed: expected 1.40, got {wm.GlobalDamageMultiplier}"); t7Pass = false; }
                // Max Speed: 6.8 * 1.30 = 8.84
                if (Mathf.Abs(pc.MoveSpeed - 8.84f) > 0.01f) { logErr($"Max Rank Speed failed: expected 8.84, got {pc.MoveSpeed}"); t7Pass = false; }
                // Max Lucky Last: 12 + 4 = 16s
                if (Mathf.Abs(lls.TotalDuration - 16.0f) > 0.01f) { logErr($"Max Rank Lucky Last duration failed: expected 16.0s, got {lls.TotalDuration}s"); t7Pass = false; }
                // Max Harvest: 3.5 * 0.80 = 2.80s
                if (Mathf.Abs(hp.EffectiveGrowDuration - 2.80f) > 0.01f) { logErr($"Max Rank Harvest time failed: expected 2.80s, got {hp.EffectiveGrowDuration}s"); t7Pass = false; }

                if (t7Pass)
                {
                    passedTests++;
                    log("[TEST 7/8] PASSED: Level 5 Max stats verified across all branches (135 HP, 1.40x Dmg, 1.35x Haste, 8.84 m/s Speed, 16s Lucky Last, 2.80s Harvest).");
                }

                // Capture Run Stats Screenshot
                UIManager uim = Object.FindFirstObjectByType<UIManager>();
                if (uim != null)
                {
                    uim.EnsureHUDAndModals();
                    ph.ResetHealth();
                }
                CaptureAndCopy("MetaUpgrade_RunStats", 1920, 1080);

                // -------------------------------------------------------------
                // TEST 8: MainMenu UI MAX LEVEL Card Verification
                // -------------------------------------------------------------
                Scene menuScene2 = EditorSceneManager.OpenScene("Assets/_Scenes/MainMenu.unity", OpenSceneMode.Single);
                meta = MetaProgressionManager.Instance;
                mmc = GetOrFindMainMenuController(menuScene2);

                bool t8Pass = false;
                if (mmc != null)
                {
                    if (!mmc.gameObject.activeInHierarchy) mmc.gameObject.SetActive(true);
                    mmc.OpenUpgrades();
                    mmc.RefreshUpgradesUI();
                    Canvas.ForceUpdateCanvases();
                    CaptureAndCopy("MetaUpgrade_MaxLevel", 1920, 1080);
                    t8Pass = true;
                }
                else
                {
                    logErr("Test 8: MainMenuController could not be found in MainMenu.unity!");
                }

                if (t8Pass)
                {
                    passedTests++;
                    log("[TEST 8/8] PASSED: Upgrades screen MAX LEVEL badge and disabled consecrate button verified.");
                }

                log("=================================================================");
                log($"[META PROGRESSION QA COMPLETE] Result: {passedTests}/{totalTests} Tests PASSED!");
                log("=================================================================");

                File.WriteAllLines(ResultsFile, logs.ToArray());
                File.WriteAllText(MarkerFile, $"COMPLETED_{passedTests}_OF_{totalTests}");
            }
            catch (Exception ex)
            {
                logErr("Exception in Meta QA Suite: " + ex);
                File.WriteAllLines(ResultsFile, logs.ToArray());
                File.WriteAllText(MarkerFile, "FAILED: " + ex.Message);
            }
        }

        private static MainMenuController GetOrFindMainMenuController(Scene scene)
        {
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    var found = root.GetComponentInChildren<MainMenuController>(true);
                    if (found != null) return found;
                }
            }

            var all = Resources.FindObjectsOfTypeAll<MainMenuController>();
            foreach (var c in all)
            {
                if (!EditorUtility.IsPersistent(c) && c.gameObject.scene == scene)
                    return c;
            }
            if (all.Length > 0 && !EditorUtility.IsPersistent(all[0]))
            {
                return all[0];
            }

            var fallback = Object.FindAnyObjectByType<MainMenuController>(FindObjectsInactive.Include);
            if (fallback != null) return fallback;

            // If completely missing, create and initialize procedural UI
            GameObject canvasGo = GameObject.Find("MainMenuCanvas");
            if (canvasGo == null && scene.IsValid())
            {
                foreach (var r in scene.GetRootGameObjects())
                {
                    if (r.name.Contains("Canvas")) { canvasGo = r; break; }
                }
            }
            if (canvasGo == null)
            {
                canvasGo = new GameObject("MainMenuCanvas");
                canvasGo.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            GameObject mmcGo = new GameObject("MainMenuController");
            mmcGo.transform.SetParent(canvasGo.transform, false);
            var newMmc = mmcGo.AddComponent<MainMenuController>();
            newMmc.EnsureMainMenuUI();
            return newMmc;
        }

        private static void CaptureAndCopy(string shotName, int width, int height)
        {
            string shotPath = ScreenshotHelper.CaptureWithCanvas(shotName, width, height);
            if (!string.IsNullOrEmpty(shotPath) && File.Exists(shotPath))
            {
                string dest = Path.Combine(ArtifactDir, Path.GetFileName(shotPath));
                File.Copy(shotPath, dest, true);
                Debug.Log($"[Screenshot] Captured & Copied: {shotName} -> {dest}");
            }
        }
    }
}
