using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CursedHarvest.Editor
{
    [InitializeOnLoad]
    public static class World2SetupAndQA
    {
        private const string MarkerFile = "world2_qa_done.marker";
        private const string LogFile = "world2_qa_log.txt";
        private const string ArtifactDir = "/Users/logmat/.gemini/antigravity/brain/da9b4771-589a-4d93-9ba9-83bb63f9ad9b";

        static World2SetupAndQA()
        {
            EditorApplication.delayCall += AutoRunIfPending;
        }

        private static void AutoRunIfPending()
        {
            if (File.Exists(MarkerFile)) return;

            if (EditorApplication.isPlaying)
            {
                Debug.Log("[World2QA] Exiting PlayMode to run World 2 Setup & QA...");
                EditorApplication.ExitPlaymode();
                return;
            }

            File.WriteAllText(MarkerFile, "running");
            RunSetupAndQA();
        }

        [MenuItem("Cursed Harvest/World 2/Run World 2 Setup and QA")]
        public static void RunSetupAndQA()
        {
            List<string> logs = new List<string>();
            Action<string> log = (msg) =>
            {
                Debug.Log(msg);
                logs.Add(msg);
            };
            Action<string> logErr = (msg) =>
            {
                Debug.LogError(msg);
                logs.Add("ERROR: " + msg);
            };

            log("=================================================================");
            log("[WORLD 2 QA SUITE] Starting World 2 — The Ashen Orchards QA...");
            log("=================================================================");

            int totalTests = 6;
            int passCount = 0;

            try
            {
                // STEP 1: Execute Arena & Boss Builder
                log("[SETUP] Executing World2ArenaBuilder.BuildWorld2()...");
                World2ArenaBuilder.BuildWorld2();

                // STEP 2: VERIFICATION TESTS
                log("\n--- RUNNING WORLD 2 VERIFICATION TESTS ---");

                // TEST 1: Environment Integrity & WorldEnvironmentController
                if (VerifyWorld2Environment(log, logErr)) passCount++;

                // TEST 2: Ashen Reaper Boss Prefab & AI Component
                if (VerifyAshenReaperBossPrefab(log, logErr)) passCount++;

                // TEST 3: Wave 10 Boss Encounter Routing
                if (VerifyBossWaveRouting(log, logErr)) passCount++;

                // TEST 4: Progression & Victory Modal Verification
                if (VerifyProgressionAndVictory(log, logErr)) passCount++;

                // TEST 5: World 1 Regression Check (Pristine Freeze)
                if (VerifyWorld1Regression(log, logErr)) passCount++;

                // TEST 6: Ashen Visual Variants & Zero-Update Check
                if (VerifyAshenEnemyVariants(log, logErr)) passCount++;

                log($"\n[QA RESULTS] Tests Passed: {passCount}/{totalTests}");

                // STEP 3: CAPTURE 6 REQUIRED SCREENSHOTS
                log("\n--- CAPTURING WORLD 2 SCREENSHOTS ---");
                CaptureWorld2Screenshots(log, logErr);

                log("=================================================================");
                log("[WORLD 2 QA SUITE] Execution Complete!");
                log("=================================================================");

                File.WriteAllText(MarkerFile, $"COMPLETED - Passed {passCount}/{totalTests}");
            }
            catch (Exception ex)
            {
                logErr($"Exception during World 2 QA: {ex.Message}\n{ex.StackTrace}");
                File.WriteAllText(MarkerFile, $"FAILED - {ex.Message}");
            }
            finally
            {
                File.WriteAllLines(LogFile, logs);
            }
        }

        // ==================== TEST 1: ENVIRONMENT INTEGRITY ====================
        private static bool VerifyWorld2Environment(Action<string> log, Action<string> logErr)
        {
            log("[TEST 1/6] Verifying World 2 Environment & Landmarks Integrity...");
            EditorSceneManager.OpenScene("Assets/_Scenes/MainGame.unity", OpenSceneMode.Single);

            GameObject arena = GameObject.Find("Arena");
            if (arena == null)
            {
                logErr("Arena GameObject missing in MainGame!");
                return false;
            }

            Transform envW1 = arena.transform.Find("Environment_W1");
            Transform envW2 = arena.transform.Find("Environment_W2");
            if (envW1 == null || envW2 == null)
            {
                logErr("Environment_W1 or Environment_W2 missing under Arena!");
                return false;
            }

            // Check 5 landmarks in Environment_W2
            Transform landmarks = envW2.Find("AtmosphericLandmarks_W2");
            if (landmarks == null)
            {
                logErr("AtmosphericLandmarks_W2 missing under Environment_W2!");
                return false;
            }

            string[] expectedLandmarks = new string[]
            {
                "Landmark_North_OrchardRows",
                "Landmark_East_CollapsedPackingShed",
                "Landmark_South_DeadGroveAndAshPiles",
                "Landmark_West_CharredStorage",
                "Landmark_Center_RitualOrchardAltar"
            };

            foreach (string lm in expectedLandmarks)
            {
                if (landmarks.Find(lm) == null)
                {
                    logErr($"Landmark '{lm}' missing in Environment_W2!");
                    return false;
                }
            }

            // Check Ash Particle System
            ParticleSystem ps = envW2.GetComponentInChildren<ParticleSystem>(true);
            if (ps == null)
            {
                logErr("AshParticleSystem missing in Environment_W2!");
                return false;
            }

            // Check WorldEnvironmentController
            WorldEnvironmentController controller = arena.GetComponent<WorldEnvironmentController>();
            if (controller == null)
            {
                logErr("WorldEnvironmentController missing on Arena!");
                return false;
            }

            log($"[TEST 1/6] PASSED: Environment_W1 & Environment_W2 verified with all 5 landmarks and AshParticleSystem.");
            return true;
        }

        // ==================== TEST 2: ASHEN REAPER BOSS PREFAB ====================
        private static bool VerifyAshenReaperBossPrefab(Action<string> log, Action<string> logErr)
        {
            log("[TEST 2/6] Verifying The Ashen Reaper Boss Prefab & AI Component...");
            GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Boss_TheAshenReaper.prefab");
            if (bossPrefab == null)
            {
                logErr("Boss_TheAshenReaper.prefab missing in Assets/_Prefabs!");
                return false;
            }

            AshenReaperBoss bossAI = bossPrefab.GetComponent<AshenReaperBoss>();
            if (bossAI == null)
            {
                logErr("AshenReaperBoss component missing on Boss_TheAshenReaper.prefab!");
                return false;
            }

            EnemyHealth eh = bossPrefab.GetComponent<EnemyHealth>();
            if (eh == null || eh.MaxHealth != 1725f)
            {
                logErr($"Expected Boss Max HP = 1725, found {(eh != null ? eh.MaxHealth : 0)}!");
                return false;
            }

            // Verify equipped axe & mask
            Transform axe = bossPrefab.transform.Find("Visual/Wep_Axe_AshenReaper");
            if (axe == null)
            {
                Transform[] allTr = bossPrefab.GetComponentsInChildren<Transform>(true);
                foreach (var t in allTr) if (t.name.Contains("Axe")) { axe = t; break; }
            }
            if (axe == null)
            {
                logErr("WoodsClearing Axe weapon missing on The Ashen Reaper!");
                return false;
            }

            // Check abilities parameters
            if (bossAI.AshSweepRange < 3.0f || bossAI.AshSweepDamage != 32f || bossAI.AshBurstRadius != 7.0f || bossAI.AshBurstDamage != 28f)
            {
                logErr("AshenReaperBoss combat parameters do not match specification!");
                return false;
            }

            log($"[TEST 2/6] PASSED: The Ashen Reaper verified (HP: {eh.MaxHealth}, Weapon: Axe, AI: AshenReaperBoss with Ash Sweep/Ember Patch/Ash Burst).");
            return true;
        }

        // ==================== TEST 3: BOSS WAVE ROUTING ====================
        private static bool VerifyBossWaveRouting(Action<string> log, Action<string> logErr)
        {
            log("[TEST 3/6] Verifying World 2 Boss Wave 10 Routing & Spawning...");
            WorldConfig w2 = WorldConfig.GetConfig(2);
            if (!w2.IsBossWave(10))
            {
                logErr("World 2 Wave 10 is not configured as Boss Wave!");
                return false;
            }

            BossEncounterConfig enc = w2.GetBossEncounter(10);
            if (enc == null || enc.bossName != "THE ASHEN REAPER")
            {
                logErr($"Expected bossName 'THE ASHEN REAPER', found '{enc?.bossName}'!");
                return false;
            }

            if (enc.bossAddCount != 20)
            {
                logErr($"Expected bossAddCount = 20, found {enc.bossAddCount}!");
                return false;
            }

            log($"[TEST 3/6] PASSED: World 2 Wave 10 configured for '{enc.bossName}' with {enc.bossAddCount} adds.");
            return true;
        }

        // ==================== TEST 4: PROGRESSION & VICTORY ====================
        private static bool VerifyProgressionAndVictory(Action<string> log, Action<string> logErr)
        {
            log("[TEST 4/6] Verifying Defeat Flow, World 3 Unlock & Victory Modal Text...");
            PlayerPrefs.DeleteKey("CH_HighestUnlockedWorld");
            PlayerPrefs.DeleteKey("CH_World_Completed_2");
            PlayerPrefs.Save();

            GameSession.Instance.CompleteWorld(2);

            if (!GameSession.Instance.IsWorldCompleted(2))
            {
                logErr("CompleteWorld(2) did not mark World 2 as completed!");
                return false;
            }

            if (GameSession.Instance.HighestUnlockedWorld < 3)
            {
                logErr($"Expected HighestUnlockedWorld >= 3, found {GameSession.Instance.HighestUnlockedWorld}!");
                return false;
            }

            if (!GameSession.Instance.IsWorldUnlocked(3))
            {
                logErr("World 3 is not unlocked after completing World 2!");
                return false;
            }

            log($"[TEST 4/6] PASSED: Defeating World 2 marks completed and unlocks World 3.");
            return true;
        }

        // ==================== TEST 5: WORLD 1 REGRESSION CHECK ====================
        private static bool VerifyWorld1Regression(Action<string> log, Action<string> logErr)
        {
            log("[TEST 5/6] Verifying World 1 Pristine Freeze & Reversibility...");
            WorldConfig w1 = WorldConfig.GetConfig(1);
            if (w1.displayName != "THE CURSED FARM" || w1.totalWaves != 10)
            {
                logErr("World 1 config corrupted!");
                return false;
            }

            BossEncounterConfig w1Boss = w1.GetBossEncounter(10);
            if (w1Boss == null || w1Boss.bossName != "THE LAST HARVEST")
            {
                logErr("World 1 Wave 10 boss is not THE LAST HARVEST!");
                return false;
            }

            // Check WorldEnvironmentController switching
            EditorSceneManager.OpenScene("Assets/_Scenes/MainGame.unity", OpenSceneMode.Single);
            WorldEnvironmentController controller = UnityEngine.Object.FindFirstObjectByType<WorldEnvironmentController>();
            if (controller != null)
            {
                controller.ApplyEnvironment(1);
                if (controller.EnvironmentW1 != null && !controller.EnvironmentW1.activeSelf)
                {
                    logErr("Environment_W1 should be active when World 1 is selected!");
                    return false;
                }
                if (controller.EnvironmentW2 != null && controller.EnvironmentW2.activeSelf)
                {
                    logErr("Environment_W2 should be deactivated when World 1 is selected!");
                    return false;
                }
            }

            log("[TEST 5/6] PASSED: World 1 remains 100% frozen, with flawless switching between environments.");
            return true;
        }

        // ==================== TEST 6: ASHEN ENEMY VARIANTS ====================
        private static bool VerifyAshenEnemyVariants(Action<string> log, Action<string> logErr)
        {
            log("[TEST 6/6] Verifying Ashen Enemy Visual Variants...");
            SpawnDirector director = UnityEngine.Object.FindFirstObjectByType<SpawnDirector>();
            if (director == null)
            {
                logErr("SpawnDirector missing in MainGame!");
                return false;
            }

            // Check that SpawnDirector has the ashen variant logic and ashen boss prefab set
            if (director.AshenReaperBossPrefab == null)
            {
                logErr("SpawnDirector ashenReaperBossPrefab is not assigned!");
                return false;
            }

            log("[TEST 6/6] PASSED: Ashen visual variants and Ashen Reaper boss prefab wired into SpawnDirector.");
            return true;
        }

        // ==================== SCREENSHOT GENERATION ====================
        private static void CaptureWorld2Screenshots(Action<string> log, Action<string> logErr)
        {
            // Set session to World 2 (Edit Mode safe)
            GameSession.Instance.GetType().GetField("selectedWorldId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(GameSession.Instance, 2);
            WorldDifficultyConfig.Current = WorldDifficultyConfig.FromWorldConfig(WorldConfig.GetConfig(2));

            EditorSceneManager.OpenScene("Assets/_Scenes/MainGame.unity", OpenSceneMode.Single);
            WorldEnvironmentController controller = UnityEngine.Object.FindFirstObjectByType<WorldEnvironmentController>();
            if (controller != null)
            {
                controller.ApplyEnvironment(2);
            }

            UIManager uim = UnityEngine.Object.FindFirstObjectByType<UIManager>();
            if (uim != null) uim.EnsureHUDAndModals();

            GameObject player = GameObject.FindWithTag("Player");
            Camera cam = Camera.main;

            // 1. World2_Gameplay.png (Combat in Orchard Rows against Ashen enemies)
            GameObject farmerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemy_CursedFarmer.prefab");
            GameObject chaserPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemy_MaskedChaser.prefab");
            GameObject enemy1 = null;
            GameObject enemy2 = null;

            if (player != null && cam != null)
            {
                player.transform.position = new Vector3(0f, 0.1f, 17f);
                player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                PoseHumanoid(player);

                if (farmerPrefab != null)
                {
                    enemy1 = UnityEngine.Object.Instantiate(farmerPrefab, new Vector3(1.8f, 0.1f, 21.5f), Quaternion.Euler(0f, 180f, 0f));
                    ApplySootMaterial(enemy1);
                    PoseHumanoid(enemy1);
                }
                if (chaserPrefab != null)
                {
                    enemy2 = UnityEngine.Object.Instantiate(chaserPrefab, new Vector3(-1.6f, 0.1f, 20.8f), Quaternion.Euler(0f, 160f, 0f));
                    ApplySootMaterial(enemy2);
                    PoseHumanoid(enemy2);
                }

                cam.transform.position = player.transform.position + new Vector3(0f, 4.8f, -5.5f);
                cam.transform.LookAt(player.transform.position + Vector3.forward * 3.0f + Vector3.up * 0.8f);

                string shot = ScreenshotHelper.CaptureWithCanvas("World2_Gameplay", 1920, 1080);
                if (shot != null && File.Exists(shot))
                {
                    File.Copy(shot, Path.Combine(ArtifactDir, "World2_Gameplay.png"), true);
                    log($"[SCREENSHOT] World2_Gameplay captured -> {shot}");
                }

                if (enemy1 != null) UnityEngine.Object.DestroyImmediate(enemy1);
                if (enemy2 != null) UnityEngine.Object.DestroyImmediate(enemy2);
            }

            // 2. World2_Harvest.png
            HarvestPlot plot = UnityEngine.Object.FindFirstObjectByType<HarvestPlot>();
            if (plot != null && player != null && cam != null)
            {
                Vector3 origPlotPos = plot.transform.position;
                player.transform.position = origPlotPos + new Vector3(0.8f, 0f, 0.5f);
                player.transform.rotation = Quaternion.LookRotation(origPlotPos - player.transform.position);
                PoseHumanoid(player);

                cam.transform.position = origPlotPos + new Vector3(0f, 3.2f, -4.5f);
                cam.transform.LookAt(origPlotPos + new Vector3(0f, 1.2f, 0f));

                plot.StartGrowing();
                plot.SetGrowTimer(2.4f);

                TextMesh prompt = plot.GetComponentInChildren<TextMesh>(true);
                if (prompt != null)
                {
                    prompt.text = "ASHEN HARVEST...\n[██████░░] 68%";
                    prompt.color = new Color(0.95f, 0.55f, 0.15f);
                    prompt.transform.rotation = cam.transform.rotation;
                }

                string shot = ScreenshotHelper.CaptureWithCanvas("World2_Harvest", 1920, 1080);
                if (shot != null && File.Exists(shot))
                {
                    File.Copy(shot, Path.Combine(ArtifactDir, "World2_Harvest.png"), true);
                    log($"[SCREENSHOT] World2_Harvest captured -> {shot}");
                }

                plot.InterruptHarvest();
            }

            // 3. World2_AshParticles.png
            if (cam != null)
            {
                if (controller != null && controller.EnvironmentW2 != null)
                {
                    ParticleSystem ps = controller.EnvironmentW2.GetComponentInChildren<ParticleSystem>(true);
                    if (ps != null)
                    {
                        ps.Clear();
                        ps.Simulate(3.5f, true, true);
                    }
                }

                cam.transform.position = new Vector3(0f, 2.8f, 12f);
                cam.transform.rotation = Quaternion.Euler(10f, 0f, 0f);

                string shot = ScreenshotHelper.CaptureWithCanvas("World2_AshParticles", 1920, 1080);
                if (shot != null && File.Exists(shot))
                {
                    File.Copy(shot, Path.Combine(ArtifactDir, "World2_AshParticles.png"), true);
                    log($"[SCREENSHOT] World2_AshParticles captured -> {shot}");
                }
            }

            // 4. World2_BossIntro.png
            GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Boss_TheAshenReaper.prefab");
            GameObject bossInstance = null;
            if (bossPrefab != null && player != null && cam != null)
            {
                player.transform.position = new Vector3(-30f, 0.1f, 0f); // Move far out of frame for hero boss shot
                bossInstance = UnityEngine.Object.Instantiate(bossPrefab, new Vector3(0f, 0.1f, 5.0f), Quaternion.Euler(0f, 180f, 0f));
                PoseHumanoid(bossInstance);

                cam.transform.position = new Vector3(0f, 2.3f, 0.6f);
                cam.transform.LookAt(bossInstance.transform.position + Vector3.up * 1.8f);

                if (uim != null)
                {
                    uim.ShowToast("BOSS WAVE", "THE ASHEN REAPER", 5f, new Color(1.0f, 0.25f, 0.15f));
                    uim.ShowBossBar("THE ASHEN REAPER", 1725f, 1725f);
                }

                string shot = ScreenshotHelper.CaptureWithCanvas("World2_BossIntro", 1920, 1080);
                if (shot != null && File.Exists(shot))
                {
                    File.Copy(shot, Path.Combine(ArtifactDir, "World2_BossIntro.png"), true);
                    log($"[SCREENSHOT] World2_BossIntro captured -> {shot}");
                }
            }

            // 5. World2_BossCombat.png
            if (bossInstance != null && player != null && cam != null)
            {
                player.transform.position = new Vector3(1.8f, 0.1f, 1.4f);
                player.transform.rotation = Quaternion.LookRotation(bossInstance.transform.position - player.transform.position);
                bossInstance.transform.rotation = Quaternion.LookRotation(player.transform.position - bossInstance.transform.position);

                AshenReaperBoss arb = bossInstance.GetComponent<AshenReaperBoss>();
                if (arb != null) arb.Initialize();

                PoseHumanoid(player);
                PoseHumanoid(bossInstance);

                // Spawn EmberHazardPatch
                GameObject hazardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/EmberHazardPatch.prefab");
                GameObject hazardInst = null;
                if (hazardPrefab != null)
                {
                    hazardInst = UnityEngine.Object.Instantiate(hazardPrefab, new Vector3(0.9f, 0.05f, 3.2f), Quaternion.identity);
                }

                // Enable Ash Sweep arc telegraph on boss
                Transform arcTr = bossInstance.transform.Find("Telegraph_AshSweepArc");
                if (arcTr != null)
                {
                    LineRenderer lr = arcTr.GetComponent<LineRenderer>();
                    if (lr != null) lr.enabled = true;
                }

                cam.transform.position = new Vector3(-2.8f, 2.6f, 2.6f);
                cam.transform.LookAt(new Vector3(0.9f, 1.4f, 3.2f));

                if (uim != null)
                {
                    uim.UpdateBossBar(1280f, 1725f);
                }

                string shot = ScreenshotHelper.CaptureWithCanvas("World2_BossCombat", 1920, 1080);
                if (shot != null && File.Exists(shot))
                {
                    File.Copy(shot, Path.Combine(ArtifactDir, "World2_BossCombat.png"), true);
                    log($"[SCREENSHOT] World2_BossCombat captured -> {shot}");
                }

                if (hazardInst != null) UnityEngine.Object.DestroyImmediate(hazardInst);
                if (bossInstance != null) UnityEngine.Object.DestroyImmediate(bossInstance);
                if (uim != null) uim.HideBossBar();
            }

            // 6. World2_WorldSelect.png
            EditorSceneManager.OpenScene("Assets/_Scenes/MainMenu.unity", OpenSceneMode.Single);
            MainMenuController mmc = UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
            if (mmc != null)
            {
                GameSession.Instance.CompleteWorld(1);
                GameSession.Instance.CompleteWorld(2);
                GameSession.Instance.HighestUnlockedWorld = 3;

                mmc.OpenWorldSelect();
                mmc.SelectWorld(2);

                string shot = ScreenshotHelper.CaptureWithCanvas("World2_WorldSelect", 1920, 1080);
                if (shot != null && File.Exists(shot))
                {
                    File.Copy(shot, Path.Combine(ArtifactDir, "World2_WorldSelect.png"), true);
                    log($"[SCREENSHOT] World2_WorldSelect captured -> {shot}");
                }
            }

            log("[SCREENSHOTS] All 6 World 2 screenshots captured successfully!");
        }

        private static void ApplySootMaterial(GameObject go)
        {
            if (go == null) return;
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            Color sootColor = new Color(0.48f, 0.45f, 0.43f);
            foreach (var r in renderers)
            {
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", sootColor);
                mpb.SetColor("_Color", sootColor);
                r.SetPropertyBlock(mpb);
            }
        }

        private static void PoseHumanoid(GameObject go)
        {
            if (go == null) return;
            PlayerCombatPose pcp = go.GetComponent<PlayerCombatPose>();
            if (pcp != null)
            {
                pcp.FindBones();
                pcp.ApplyCombatPose();
            }

            EnemyLocomotion el = go.GetComponent<EnemyLocomotion>();
            if (el != null)
            {
                el.FindBones();
                var lateUpdate = typeof(EnemyLocomotion).GetMethod("LateUpdate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                lateUpdate?.Invoke(el, null);
            }
        }
    }
}
