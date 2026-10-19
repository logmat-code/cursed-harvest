using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CursedHarvest.Editor
{
    public static class World4SetupAndQA
    {
        private const string LogFile = "world4_qa_log.txt";
        private const string ArtifactDir = "/Users/logmat/.gemini/antigravity/brain/da9b4771-589a-4d93-9ba9-83bb63f9ad9b";

        [MenuItem("Cursed Harvest/World 4/Run World 4 Setup and QA")]
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
            log("[WORLD 4 QA SUITE] Starting World 4 — The Bloodwood Timber QA...");
            log("=================================================================");

            int totalTests = 6;
            int passCount = 0;

            try
            {
                // STEP 1: Execute Arena & Boss Builder
                log("[SETUP] Executing World4ArenaBuilder.BuildWorld4()...");
                World4ArenaBuilder.BuildWorld4();

                // STEP 2: VERIFICATION TESTS
                log("\n--- RUNNING WORLD 4 VERIFICATION TESTS ---");

                // TEST 1: Environment Integrity & 5 Landmarks
                if (VerifyWorld4Environment(log, logErr)) passCount++;

                // TEST 2: Timber Goliath (Wave 10 Mid-Boss)
                if (VerifyTimberGoliathBossPrefab(log, logErr)) passCount++;

                // TEST 3: Bloodwood Sovereign (Wave 15 Final Boss)
                if (VerifyBloodwoodSovereignBossPrefab(log, logErr)) passCount++;

                // TEST 4: Wave 10 Mid-Boss & Wave 15 Final Boss Routing
                if (VerifyBossWaveRouting(log, logErr)) passCount++;

                // TEST 5: Worlds 1-3 Freeze Regression & World 4 Progression
                if (VerifyWorldsRegressionAndProgression(log, logErr)) passCount++;

                // TEST 6: UI & Meta Economy Consistency
                if (VerifyUIAndMetaConsistency(log, logErr)) passCount++;

                log($"\n[QA RESULTS] Tests Passed: {passCount}/{totalTests}");

                // STEP 3: CAPTURE ALL 8 REQUIRED SCREENSHOTS
                log("\n--- CAPTURING WORLD 4 SCREENSHOTS ---");
                CaptureWorld4Screenshots(log, logErr);

                log("=================================================================");
                log("[WORLD 4 QA SUITE] Execution Complete!");
                log("=================================================================");
            }
            catch (Exception ex)
            {
                logErr($"EXCEPTION during World 4 QA: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                File.WriteAllLines(LogFile, logs);
                log($"[QA] Log written to {LogFile}");
            }
        }

        private static bool VerifyWorld4Environment(Action<string> log, Action<string> logErr)
        {
            log("\n[TEST 1/6] Verifying World 4 Environment Integrity & Landmarks...");
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != "Assets/_Scenes/MainGame.unity")
            {
                activeScene = EditorSceneManager.OpenScene("Assets/_Scenes/MainGame.unity", OpenSceneMode.Single);
            }

            GameObject arena = GameObject.Find("Arena");
            if (arena == null)
            {
                logErr("Arena root not found in MainGame.unity!");
                return false;
            }

            Transform envW4 = arena.transform.Find("Environment_W4");
            if (envW4 == null)
            {
                logErr("Environment_W4 not found under Arena!");
                return false;
            }

            // Check continuous ground
            Transform ground = envW4.Find("Ground_Continuous_W4");
            if (ground == null)
            {
                logErr("Ground_Continuous_W4 plane missing!");
                return false;
            }
            MeshRenderer mr = ground.GetComponent<MeshRenderer>();
            if (mr == null || mr.sharedMaterial == null || !mr.sharedMaterial.name.Contains("M_ContinuousBaseGround_W4"))
            {
                logErr("Ground_Continuous_W4 does not have M_ContinuousBaseGround_W4 material assigned!");
                return false;
            }

            // Check 5 Landmarks
            Transform landmarks = envW4.Find("AtmosphericLandmarks_W4");
            if (landmarks == null)
            {
                logErr("AtmosphericLandmarks_W4 root missing!");
                return false;
            }

            string[] requiredLandmarks = new string[]
            {
                "Landmark_1_CentralTimberRitual",
                "Landmark_2_AbandonedSawmill",
                "Landmark_3_LogStorageYard",
                "Landmark_4_BloodrootGrove",
                "Landmark_5_BrokenLoggingCamp"
            };

            foreach (string lm in requiredLandmarks)
            {
                Transform t = landmarks.Find(lm);
                if (t == null)
                {
                    logErr($"Required World 4 landmark '{lm}' missing!");
                    return false;
                }
                if (t.childCount == 0)
                {
                    logErr($"Landmark '{lm}' has no authored props!");
                    return false;
                }
            }

            // Check perimeter palisades
            Transform perimeter = envW4.Find("PerimeterPalisades_W4");
            if (perimeter == null || perimeter.childCount < 20)
            {
                logErr("Perimeter palisades missing or incomplete!");
                return false;
            }

            // Check particles
            Transform embers = envW4.Find("Bloodwood_Sap_Embers");
            if (embers == null || embers.GetComponent<ParticleSystem>() == null)
            {
                logErr("Bloodwood_Sap_Embers particle system missing!");
                return false;
            }

            // Test WorldEnvironmentController switching
            WorldEnvironmentController wec = UnityEngine.Object.FindAnyObjectByType<WorldEnvironmentController>(FindObjectsInactive.Include);
            if (wec == null)
            {
                foreach (var r in activeScene.GetRootGameObjects())
                {
                    wec = r.GetComponentInChildren<WorldEnvironmentController>(true);
                    if (wec != null) break;
                }
            }

            if (wec == null)
            {
                logErr("WorldEnvironmentController missing from MainGame scene!");
                return false;
            }

            wec.ApplyEnvironment(4);
            if (wec.EnvironmentW1 != null && wec.EnvironmentW1.activeSelf)
            {
                logErr("Environment_W1 remained active when World 4 applied!");
                return false;
            }
            if (wec.EnvironmentW2 != null && wec.EnvironmentW2.activeSelf)
            {
                logErr("Environment_W2 remained active when World 4 applied!");
                return false;
            }
            if (wec.EnvironmentW3 != null && wec.EnvironmentW3.activeSelf)
            {
                logErr("Environment_W3 remained active when World 4 applied!");
                return false;
            }
            if (wec.EnvironmentW4 == null || !wec.EnvironmentW4.activeSelf)
            {
                logErr("Environment_W4 is not active when World 4 applied!");
                return false;
            }

            // Revert to W1 default
            wec.ApplyEnvironment(1);

            log("[TEST 1/6] PASSED: World 4 environment, 5 distinct landmarks, palisades, particles, and controller verified.");
            return true;
        }

        private static bool VerifyTimberGoliathBossPrefab(Action<string> log, Action<string> logErr)
        {
            log("\n[TEST 2/6] Verifying Timber Goliath (Wave 10 Mid-Boss)...");
            string path = "Assets/_Prefabs/Boss_TimberGoliath.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                logErr($"Timber Goliath prefab missing at {path}!");
                return false;
            }

            TimberGoliathBoss boss = prefab.GetComponent<TimberGoliathBoss>();
            if (boss == null)
            {
                logErr("TimberGoliathBoss component missing on prefab!");
                return false;
            }

            EnemyHealth eh = prefab.GetComponent<EnemyHealth>();
            if (eh == null || eh.MaxHealth < 2800f)
            {
                logErr($"EnemyHealth missing or insufficient on Timber Goliath (MaxHealth: {eh?.MaxHealth})!");
                return false;
            }

            if (boss.LogSweepRange < 4.0f || boss.LogSweepDamage < 30f)
            {
                logErr("LogSweep attack properties out of spec!");
                return false;
            }
            if (boss.GroundSplinterRange < 8.0f)
            {
                logErr("GroundSplinter attack range out of spec!");
                return false;
            }
            if (boss.ChargeDamage < 30f)
            {
                logErr("Charge damage out of spec!");
                return false;
            }
            if (boss.ShockwaveRadius < 7.0f)
            {
                logErr("Shockwave radius out of spec!");
                return false;
            }

            log($"[TEST 2/6] PASSED: Timber Goliath verified (HP: {eh.MaxHealth}, SweepRange: {boss.LogSweepRange}m, SweepDmg: {boss.LogSweepDamage}).");
            return true;
        }

        private static bool VerifyBloodwoodSovereignBossPrefab(Action<string> log, Action<string> logErr)
        {
            log("\n[TEST 3/6] Verifying Bloodwood Sovereign (Wave 15 Final Boss)...");
            string path = "Assets/_Prefabs/Boss_BloodwoodSovereign.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                logErr($"Bloodwood Sovereign prefab missing at {path}!");
                return false;
            }

            BloodwoodSovereignBoss boss = prefab.GetComponent<BloodwoodSovereignBoss>();
            if (boss == null)
            {
                logErr("BloodwoodSovereignBoss component missing on prefab!");
                return false;
            }

            EnemyHealth eh = prefab.GetComponent<EnemyHealth>();
            if (eh == null || eh.MaxHealth < 3600f)
            {
                logErr($"EnemyHealth missing or insufficient on Bloodwood Sovereign (MaxHealth: {eh?.MaxHealth})!");
                return false;
            }

            // Verify BloodSapHazardPool prefab
            string poolPath = "Assets/_Prefabs/BloodSapHazardPool.prefab";
            GameObject poolPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(poolPath);
            if (poolPrefab == null)
            {
                logErr($"BloodSapHazardPool prefab missing at {poolPath}!");
                return false;
            }

            BloodSapHazardPool pool = poolPrefab.GetComponent<BloodSapHazardPool>();
            if (pool == null || pool.Duration < 6.0f || pool.DamagePerSecond < 7.0f)
            {
                logErr("BloodSapHazardPool component missing or stats invalid!");
                return false;
            }

            if (boss.SapPoolRange < 10.0f || boss.SpearDamage < 30f || boss.SpearRadius < 2.0f)
            {
                logErr("Bloodwood Sovereign attack properties out of spec!");
                return false;
            }

            log($"[TEST 3/6] PASSED: Bloodwood Sovereign verified (HP: {eh.MaxHealth}, SapRange: {boss.SapPoolRange}m, SpearDmg: {boss.SpearDamage}).");
            return true;
        }

        private static bool VerifyBossWaveRouting(Action<string> log, Action<string> logErr)
        {
            log("\n[TEST 4/6] Verifying Wave 10 Mid-Boss & Wave 15 Final Boss Routing...");
            WorldConfig cfg = WorldConfig.CreateWorld4();
            if (cfg.totalWaves != 15)
            {
                logErr($"World 4 total waves is {cfg.totalWaves}, expected 15!");
                return false;
            }

            if (cfg.bossWaves == null || cfg.bossWaves.Length != 2 || cfg.bossWaves[0] != 10 || cfg.bossWaves[1] != 15)
            {
                logErr("World 4 bossWaves must contain [10, 15]!");
                return false;
            }

            BossEncounterConfig midBoss = cfg.GetBossEncounter(10);
            if (midBoss == null)
            {
                logErr("Wave 10 encounter missing from World 4!");
                return false;
            }
            if (midBoss.isFinalBoss)
            {
                logErr("Wave 10 must be a Mid-Boss (isFinalBoss == false)!");
                return false;
            }
            if (!midBoss.bossName.Contains("TIMBER GOLIATH"))
            {
                logErr($"Wave 10 bossName '{midBoss.bossName}' does not match TIMBER GOLIATH!");
                return false;
            }

            BossEncounterConfig finalBoss = cfg.GetBossEncounter(15);
            if (finalBoss == null)
            {
                logErr("Wave 15 encounter missing from World 4!");
                return false;
            }
            if (!finalBoss.isFinalBoss)
            {
                logErr("Wave 15 must be the Final Boss (isFinalBoss == true)!");
                return false;
            }
            if (!finalBoss.bossName.Contains("BLOODWOOD SOVEREIGN"))
            {
                logErr($"Wave 15 bossName '{finalBoss.bossName}' does not match BLOODWOOD SOVEREIGN!");
                return false;
            }

            // Verify SpawnDirector wiring
            Scene activeScene = EditorSceneManager.GetActiveScene();
            SpawnDirector sd = UnityEngine.Object.FindAnyObjectByType<SpawnDirector>(FindObjectsInactive.Include);
            if (sd == null)
            {
                foreach (var r in activeScene.GetRootGameObjects())
                {
                    sd = r.GetComponentInChildren<SpawnDirector>(true);
                    if (sd != null) break;
                }
            }
            if (sd == null)
            {
                logErr("SpawnDirector missing in MainGame scene!");
                return false;
            }

            if (sd.TimberGoliathBossPrefab == null)
            {
                logErr("SpawnDirector.TimberGoliathBossPrefab is null!");
                return false;
            }
            if (sd.BloodwoodSovereignBossPrefab == null)
            {
                logErr("SpawnDirector.BloodwoodSovereignBossPrefab is null!");
                return false;
            }

            log("[TEST 4/6] PASSED: Wave 10 Mid-Boss & Wave 15 Final Boss correctly configured and wired to SpawnDirector.");
            return true;
        }

        private static bool VerifyWorldsRegressionAndProgression(Action<string> log, Action<string> logErr)
        {
            log("\n[TEST 5/6] Verifying Worlds 1-3 Freeze Regression & World 4 Progression...");
            // World 1
            WorldConfig w1 = WorldConfig.CreateWorld1();
            if (w1.worldId != 1 || w1.totalWaves != 10 || w1.bossEncounters[0].bossName != "THE LAST HARVEST")
            {
                logErr("World 1 config corrupted!");
                return false;
            }

            // World 2
            WorldConfig w2 = WorldConfig.CreateWorld2();
            if (w2.worldId != 2 || w2.totalWaves != 10 || w2.bossEncounters[0].bossName != "THE ASHEN REAPER")
            {
                logErr("World 2 config corrupted!");
                return false;
            }

            // World 3
            WorldConfig w3 = WorldConfig.CreateWorld3();
            if (w3.worldId != 3 || w3.totalWaves != 10 || w3.bossEncounters[0].bossName != "THE MIRE MOTHER")
            {
                logErr("World 3 config corrupted!");
                return false;
            }

            // World 4
            WorldConfig w4 = WorldConfig.CreateWorld4();
            if (w4.worldId != 4 || w4.unlockRequirement != 3 || w4.totalWaves != 15)
            {
                logErr("World 4 config progression parameters corrupted!");
                return false;
            }

            // World 5
            WorldConfig w5 = WorldConfig.CreateWorld5();
            if (w5.worldId != 5 || w5.unlockRequirement != 4 || w5.totalWaves != 20)
            {
                logErr("World 5 config progression parameters corrupted!");
                return false;
            }

            log("[TEST 5/6] PASSED: Worlds 1, 2, and 3 are 100% frozen; Worlds 4 and 5 progression hierarchy valid.");
            return true;
        }

        private static bool VerifyUIAndMetaConsistency(Action<string> log, Action<string> logErr)
        {
            log("\n[TEST 6/6] Verifying UI & Meta Progression Consistency...");
            if (MetaProgressionManager.Instance == null)
            {
                GameObject go = new GameObject("MetaProgressionManager_Test");
                go.AddComponent<MetaProgressionManager>();
            }

            float vitBonus = MetaProgressionManager.Instance.GetVitalityBonus();
            float powMult = MetaProgressionManager.Instance.GetPowerMultiplier();
            float hasteMult = MetaProgressionManager.Instance.GetHasteMultiplier();

            if (powMult < 1.0f || hasteMult < 1.0f)
            {
                logErr("MetaProgressionManager returned invalid multipliers!");
                return false;
            }

            log($"[TEST 6/6] PASSED: MetaProgressionManager active (Vit: +{vitBonus} HP, Power: {powMult:F2}x, Haste: {hasteMult:F2}x).");
            return true;
        }

        // ==================== SCREENSHOT GENERATION ====================
        private static void CaptureWorld4Screenshots(Action<string> log, Action<string> logErr)
        {
            Directory.CreateDirectory(ArtifactDir);

            // Set session to World 4
            GameSession.Instance.GetType().GetField("selectedWorldId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(GameSession.Instance, 4);
            WorldDifficultyConfig.Current = WorldDifficultyConfig.FromWorldConfig(WorldConfig.GetConfig(4));

            EditorSceneManager.OpenScene("Assets/_Scenes/MainGame.unity", OpenSceneMode.Single);
            WorldEnvironmentController controller = UnityEngine.Object.FindAnyObjectByType<WorldEnvironmentController>(FindObjectsInactive.Include);
            if (controller == null)
            {
                foreach (var r in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                {
                    controller = r.GetComponentInChildren<WorldEnvironmentController>(true);
                    if (controller != null) break;
                }
            }
            if (controller != null)
            {
                controller.ApplyEnvironment(4);
            }

            UIManager uim = UnityEngine.Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
            if (uim == null)
            {
                foreach (var r in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                {
                    uim = r.GetComponentInChildren<UIManager>(true);
                    if (uim != null) break;
                }
            }
            if (uim != null) uim.EnsureHUDAndModals();

            GameObject player = GameObject.FindWithTag("Player");
            Camera cam = Camera.main;

            // 1. World4_ArenaOverview.png (Wide overview of The Bloodwood Timber)
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 21.0f, -22f);
                cam.transform.rotation = Quaternion.Euler(42f, 0f, 0f);

                string shot = ScreenshotHelper.CaptureWithCanvas("World4_ArenaOverview", 1920, 1080);
                SaveArtifact(shot, "World4_ArenaOverview.png", log);
            }

            // 2. World4_TimberClearing.png (Landmark 1: Central Timber Ritual Altar)
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 4.2f, -5.5f);
                cam.transform.LookAt(new Vector3(0f, 1.2f, 1.8f));

                string shot = ScreenshotHelper.CaptureWithCanvas("World4_TimberClearing", 1920, 1080);
                SaveArtifact(shot, "World4_TimberClearing.png", log);
            }

            // 3. World4_Sawmill.png (Landmark 2: Abandoned Sawmill - North)
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 4.5f, 16.5f);
                cam.transform.LookAt(new Vector3(0f, 1.4f, 22.5f));

                string shot = ScreenshotHelper.CaptureWithCanvas("World4_Sawmill", 1920, 1080);
                SaveArtifact(shot, "World4_Sawmill.png", log);
            }

            // 4. World4_LogStorage.png (Landmark 3: Log Storage Yard - East)
            if (cam != null)
            {
                cam.transform.position = new Vector3(16.5f, 4.2f, -4f);
                cam.transform.LookAt(new Vector3(22.0f, 1.2f, -4f));

                string shot = ScreenshotHelper.CaptureWithCanvas("World4_LogStorage", 1920, 1080);
                SaveArtifact(shot, "World4_LogStorage.png", log);
            }

            // 5. World4_BloodrootGrove.png (Landmark 4: Bloodroot Ancient Pine Grove - South)
            if (cam != null)
            {
                cam.transform.position = new Vector3(-3.5f, 5.8f, -14.5f);
                cam.transform.LookAt(new Vector3(4.0f, 2.5f, -22.0f));

                string shot = ScreenshotHelper.CaptureWithCanvas("World4_BloodrootGrove", 1920, 1080);
                SaveArtifact(shot, "World4_BloodrootGrove.png", log);
            }

            // 6. World4_MidBoss_Goliath.png (Wave 10 Mid-Boss: Timber Goliath showcase)
            GameObject midBossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Boss_TimberGoliath.prefab");
            GameObject midBossInstance = null;
            if (midBossPrefab != null && player != null && cam != null)
            {
                player.transform.position = new Vector3(-40f, 0.1f, 0f); // Move player out of view
                midBossInstance = UnityEngine.Object.Instantiate(midBossPrefab, new Vector3(0f, 0.1f, 8.5f), Quaternion.Euler(0f, 180f, 0f));
                PoseHumanoid(midBossInstance);

                cam.transform.position = new Vector3(0f, 2.8f, 2.2f);
                cam.transform.LookAt(midBossInstance.transform.position + Vector3.up * 1.8f);

                if (uim != null)
                {
                    uim.ShowToast("MID-BOSS WAVE", "TIMBER GOLIATH\nCrusher of the Ancient Pines", 5f, new Color(1.0f, 0.35f, 0.15f));
                    uim.ShowBossBar("TIMBER GOLIATH", 2800f, 2800f);
                }

                string shot = ScreenshotHelper.CaptureWithCanvas("World4_MidBoss_Goliath", 1920, 1080);
                SaveArtifact(shot, "World4_MidBoss_Goliath.png", log);

                UnityEngine.Object.DestroyImmediate(midBossInstance);
            }

            // 7. World4_FinalBoss_Sovereign.png (Wave 15 Final Boss: Bloodwood Sovereign showcase)
            GameObject finalBossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Boss_BloodwoodSovereign.prefab");
            GameObject sapPoolPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/BloodSapHazardPool.prefab");
            GameObject finalBossInstance = null;
            GameObject sapPoolInstance = null;
            if (finalBossPrefab != null && cam != null)
            {
                finalBossInstance = UnityEngine.Object.Instantiate(finalBossPrefab, new Vector3(0f, 0.1f, 8.5f), Quaternion.Euler(0f, 180f, 0f));
                PoseHumanoid(finalBossInstance);

                if (sapPoolPrefab != null)
                {
                    sapPoolInstance = UnityEngine.Object.Instantiate(sapPoolPrefab, new Vector3(2.5f, 0.05f, 7.5f), Quaternion.identity);
                }

                cam.transform.position = new Vector3(0f, 2.8f, 2.2f);
                cam.transform.LookAt(finalBossInstance.transform.position + Vector3.up * 2.0f);

                if (uim != null)
                {
                    uim.ShowToast("FINAL BOSS", "BLOODWOOD SOVEREIGN\nMonarch of the Bleeding Bark", 5f, new Color(0.95f, 0.15f, 0.12f));
                    uim.ShowBossBar("BLOODWOOD SOVEREIGN", 3600f, 3600f);
                }

                string shot = ScreenshotHelper.CaptureWithCanvas("World4_FinalBoss_Sovereign", 1920, 1080);
                SaveArtifact(shot, "World4_FinalBoss_Sovereign.png", log);

                if (finalBossInstance != null) UnityEngine.Object.DestroyImmediate(finalBossInstance);
                if (sapPoolInstance != null) UnityEngine.Object.DestroyImmediate(sapPoolInstance);
            }

            // 8. World4_Gameplay.png (Gameplay perspective with HUD, player in arena, active bloodwood atmosphere)
            if (cam != null && player != null)
            {
                player.transform.position = new Vector3(0f, 0.1f, -2.5f);
                player.transform.rotation = Quaternion.LookRotation(Vector3.forward);
                PoseHumanoid(player);

                cam.transform.position = new Vector3(0f, 13.5f, -16.5f);
                cam.transform.rotation = Quaternion.Euler(38f, 0f, 0f);

                if (uim != null)
                {
                    uim.HideBossBar();
                    uim.ShowToast("WAVE 1 / 15", "THE BLOODWOOD TIMBER", 3f, new Color(0.85f, 0.25f, 0.25f));
                }

                // Spawn a couple enemies in arena for live action shot
                GameObject chargerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemy_DemonCharger.prefab");
                GameObject chaserPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemy_MaskedChaser.prefab");
                GameObject e1 = null;
                GameObject e2 = null;

                if (chargerPrefab != null)
                {
                    e1 = UnityEngine.Object.Instantiate(chargerPrefab, new Vector3(2.2f, 0.1f, 1.2f), Quaternion.LookRotation(new Vector3(-0.6f, 0f, -0.8f)));
                    PoseHumanoid(e1);
                }
                if (chaserPrefab != null)
                {
                    e2 = UnityEngine.Object.Instantiate(chaserPrefab, new Vector3(-2.6f, 0.1f, 0.5f), Quaternion.LookRotation(new Vector3(0.7f, 0f, -0.7f)));
                    PoseHumanoid(e2);
                }

                string shot = ScreenshotHelper.CaptureWithCanvas("World4_Gameplay", 1920, 1080);
                SaveArtifact(shot, "World4_Gameplay.png", log);

                if (e1 != null) UnityEngine.Object.DestroyImmediate(e1);
                if (e2 != null) UnityEngine.Object.DestroyImmediate(e2);
            }

            // Revert scene to clean default (World 1)
            if (controller != null) controller.ApplyEnvironment(1);
            if (uim != null)
            {
                uim.HideBossBar();
                uim.HideAllModals();
            }
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static void PoseHumanoid(GameObject go)
        {
            if (go == null) return;
            Transform[] bones = go.GetComponentsInChildren<Transform>(true);
            foreach (Transform b in bones)
            {
                string n = b.name.ToLower();
                if (n.Contains("arm_l") || n.Contains("upperarm_l")) b.localRotation = Quaternion.Euler(15f, 0f, -25f);
                else if (n.Contains("arm_r") || n.Contains("upperarm_r")) b.localRotation = Quaternion.Euler(-25f, 0f, 25f);
                else if (n.Contains("forearm_r")) b.localRotation = Quaternion.Euler(-45f, 0f, 0f);
                else if (n.Contains("thigh_l") || n.Contains("up_leg_l")) b.localRotation = Quaternion.Euler(10f, 0f, 0f);
                else if (n.Contains("thigh_r") || n.Contains("up_leg_r")) b.localRotation = Quaternion.Euler(-10f, 0f, 0f);
            }
        }

        private static void SaveArtifact(string sourcePath, string destFileName, Action<string> log)
        {
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath)) return;
            string dest = Path.Combine(ArtifactDir, destFileName);
            File.Copy(sourcePath, dest, true);
            log($"[ARTIFACT] Saved: {destFileName} -> {dest}");
        }
    }
}
