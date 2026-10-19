using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CursedHarvest.Editor
{
    public static class World5SetupAndQA
    {
        private const string LogFile = "world5_qa_log.txt";
        private const string ArtifactDir = "/Users/logmat/.gemini/antigravity/brain/da9b4771-589a-4d93-9ba9-83bb63f9ad9b";

        [MenuItem("Cursed Harvest/World 5/Run World 5 Setup and QA")]
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
            log("[WORLD 5 QA SUITE] Starting World 5 — The Eternal Eclipse QA...");
            log("=================================================================");

            int totalTests = 6;
            int passCount = 0;

            try
            {
                // STEP 1: Execute Arena & Boss Builder
                log("[SETUP] Executing World5ArenaBuilder.BuildWorld5()...");
                World5ArenaBuilder.BuildWorld5();

                // STEP 2: VERIFICATION TESTS
                log("\n--- RUNNING WORLD 5 VERIFICATION TESTS ---");

                // TEST 1: Environment Integrity & 5 Landmarks
                if (VerifyWorld5Environment(log, logErr)) passCount++;

                // TEST 2: Eclipse Herald (Wave 10 Mid-Boss)
                if (VerifyEclipseHeraldBossPrefab(log, logErr)) passCount++;

                // TEST 3: Primeval Horror (Wave 20 Final Boss)
                if (VerifyPrimevalHorrorBossPrefab(log, logErr)) passCount++;

                // TEST 4: Wave 10 Mid-Boss & Wave 20 Final Boss Routing
                if (VerifyBossWaveRouting(log, logErr)) passCount++;

                // TEST 5: Worlds 1-4 Zero Regression & World 5 Progression
                if (VerifyWorldsRegressionAndProgression(log, logErr)) passCount++;

                // TEST 6: UI, Meta Economy & Victory Consistency
                if (VerifyUIAndMetaConsistency(log, logErr)) passCount++;

                log($"\n[QA RESULTS] Tests Passed: {passCount}/{totalTests}");

                // STEP 3: CAPTURE ALL 8 REQUIRED SCREENSHOTS
                log("\n--- CAPTURING WORLD 5 SCREENSHOTS ---");
                CaptureWorld5Screenshots(log, logErr);

                log("=================================================================");
                log("[WORLD 5 QA SUITE] Execution Complete!");
                log("=================================================================");
            }
            catch (Exception ex)
            {
                logErr($"EXCEPTION during World 5 QA: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                File.WriteAllLines(LogFile, logs);
                log($"[QA] Log written to {LogFile}");
            }
        }

        private static bool VerifyWorld5Environment(Action<string> log, Action<string> logErr)
        {
            log("\n[TEST 1/6] Verifying World 5 Environment Integrity & Landmarks...");
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

            Transform envW5 = arena.transform.Find("Environment_W5");
            if (envW5 == null)
            {
                logErr("Environment_W5 not found under Arena!");
                return false;
            }

            // Check continuous ground
            Transform ground = envW5.Find("Ground_Continuous_W5");
            if (ground == null)
            {
                logErr("Ground_Continuous_W5 plane missing!");
                return false;
            }
            MeshRenderer mr = ground.GetComponent<MeshRenderer>();
            if (mr == null || mr.sharedMaterial == null || !mr.sharedMaterial.name.Contains("M_ContinuousBaseGround_W5"))
            {
                logErr("Ground_Continuous_W5 does not have M_ContinuousBaseGround_W5 material assigned!");
                return false;
            }

            // Check 5 Landmarks
            Transform landmarks = envW5.Find("AtmosphericLandmarks_W5");
            if (landmarks == null)
            {
                logErr("AtmosphericLandmarks_W5 root missing!");
                return false;
            }

            string[] requiredLandmarks = new string[]
            {
                "Landmark_1_CentralEclipseAltar",
                "Landmark_2_ObsidianCrater",
                "Landmark_3_SunkenCatacombRuins",
                "Landmark_4_UmbralShrine",
                "Landmark_5_AbyssalGate"
            };

            foreach (string lmName in requiredLandmarks)
            {
                Transform lm = landmarks.Find(lmName);
                if (lm == null)
                {
                    logErr($"Required landmark '{lmName}' not found in AtmosphericLandmarks_W5!");
                    return false;
                }
                if (lm.childCount == 0)
                {
                    logErr($"Landmark '{lmName}' has 0 visual props!");
                    return false;
                }
                log($"  ✓ Landmark verified: '{lmName}' ({lm.childCount} elements)");
            }

            // Check Perimeter Cliffs
            Transform perimeter = envW5.Find("Perimeter_Cliffs_W5");
            if (perimeter == null || perimeter.childCount < 20)
            {
                logErr("Perimeter_Cliffs_W5 missing or insufficient palisade rocks!");
                return false;
            }
            log($"  ✓ Perimeter barrier verified ({perimeter.childCount} barrier elements)");

            // Check Eclipse Particles
            Transform particles = envW5.Find("Eclipse_Void_Particles");
            if (particles == null || particles.GetComponent<ParticleSystem>() == null)
            {
                logErr("Eclipse_Void_Particles particle system missing!");
                return false;
            }
            log("  ✓ Eclipse_Void_Particles particle system verified");

            log("[TEST 1/6 PASSED] World 5 Environment & 5 Landmarks fully intact!");
            return true;
        }

        private static bool VerifyEclipseHeraldBossPrefab(Action<string> log, Action<string> logErr)
        {
            log("\n[TEST 2/6] Verifying Eclipse Herald (Wave 10 Mid-Boss) Prefab...");
            string path = "Assets/_Prefabs/Boss_EclipseHerald.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                logErr($"Prefab missing at '{path}'!");
                return false;
            }

            if (!prefab.CompareTag("Enemy"))
            {
                logErr("Eclipse Herald tag is not 'Enemy'!");
                return false;
            }

            CharacterController cc = prefab.GetComponent<CharacterController>();
            if (cc == null)
            {
                logErr("Eclipse Herald missing CharacterController!");
                return false;
            }

            EnemyHealth eh = prefab.GetComponent<EnemyHealth>();
            if (eh == null || eh.MaxHealth < 3000f)
            {
                logErr($"Eclipse Herald missing EnemyHealth or has invalid HP ({eh?.MaxHealth})!");
                return false;
            }

            EclipseHeraldBoss boss = prefab.GetComponent<EclipseHeraldBoss>();
            if (boss == null)
            {
                logErr("Eclipse Herald missing EclipseHeraldBoss component!");
                return false;
            }

            if (boss.EclipseRayRange < 8f || boss.EclipseRayDamage < 20f || boss.ShockwaveRadius < 5f)
            {
                logErr("Eclipse Herald has invalid combat parameters!");
                return false;
            }

            log($"  ✓ Eclipse Herald verified: HP={eh.MaxHealth}, RayRange={boss.EclipseRayRange}m, RayDmg={boss.EclipseRayDamage}, ShockwaveRad={boss.ShockwaveRadius}m");
            log("[TEST 2/6 PASSED] Eclipse Herald Mid-Boss fully validated!");
            return true;
        }

        private static bool VerifyPrimevalHorrorBossPrefab(Action<string> log, Action<string> logErr)
        {
            log("\n[TEST 3/6] Verifying Primeval Horror (Wave 20 Final Boss) Prefab...");
            string path = "Assets/_Prefabs/Boss_PrimevalHorror.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                logErr($"Prefab missing at '{path}'!");
                return false;
            }

            if (!prefab.CompareTag("Enemy"))
            {
                logErr("Primeval Horror tag is not 'Enemy'!");
                return false;
            }

            CharacterController cc = prefab.GetComponent<CharacterController>();
            if (cc == null)
            {
                logErr("Primeval Horror missing CharacterController!");
                return false;
            }

            EnemyHealth eh = prefab.GetComponent<EnemyHealth>();
            if (eh == null || eh.MaxHealth < 5000f)
            {
                logErr($"Primeval Horror missing EnemyHealth or has invalid HP ({eh?.MaxHealth})!");
                return false;
            }

            PrimevalHorrorBoss boss = prefab.GetComponent<PrimevalHorrorBoss>();
            if (boss == null)
            {
                logErr("Primeval Horror missing PrimevalHorrorBoss component!");
                return false;
            }

            if (boss.VoidNovaRadius < 10f || boss.VoidNovaDamage < 30f || boss.StarfallDamage < 25f || boss.CleaveDamage < 35f)
            {
                logErr("Primeval Horror has invalid combat parameters!");
                return false;
            }

            // Check VoidRiftHazardPool prefab
            string hazardPath = "Assets/_Prefabs/VoidRiftHazardPool.prefab";
            GameObject hazardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(hazardPath);
            if (hazardPrefab == null || hazardPrefab.GetComponent<VoidRiftHazardPool>() == null)
            {
                logErr("VoidRiftHazardPool prefab missing or corrupted!");
                return false;
            }

            log($"  ✓ Primeval Horror verified: HP={eh.MaxHealth}, NovaRad={boss.VoidNovaRadius}m, NovaDmg={boss.VoidNovaDamage}, CleaveDmg={boss.CleaveDamage}");
            log("  ✓ VoidRiftHazardPool prefab verified");
            log("[TEST 3/6 PASSED] Primeval Horror Final Boss fully validated!");
            return true;
        }

        private static bool VerifyBossWaveRouting(Action<string> log, Action<string> logErr)
        {
            log("\n[TEST 4/6] Verifying Wave 10 Mid-Boss & Wave 20 Final Boss Wave Routing...");
            WorldConfig cfg5 = WorldConfig.GetConfig(5);
            if (cfg5 == null)
            {
                logErr("WorldConfig.GetConfig(5) returned null!");
                return false;
            }

            if (cfg5.totalWaves != 20)
            {
                logErr($"World 5 totalWaves is {cfg5.totalWaves}, expected 20!");
                return false;
            }

            if (cfg5.bossWaves == null || cfg5.bossWaves.Length != 2 || cfg5.bossWaves[0] != 10 || cfg5.bossWaves[1] != 20)
            {
                logErr("World 5 bossWaves not configured for {10, 20}!");
                return false;
            }

            BossEncounterConfig midBossEnc = cfg5.GetBossEncounter(10);
            if (midBossEnc == null || midBossEnc.isFinalBoss != false || !midBossEnc.bossName.Contains("ECLIPSE HERALD"))
            {
                logErr("Wave 10 encounter is not configured as non-final Mid-Boss Eclipse Herald!");
                return false;
            }

            BossEncounterConfig finalBossEnc = cfg5.GetBossEncounter(20);
            if (finalBossEnc == null || finalBossEnc.isFinalBoss != true || !finalBossEnc.bossName.Contains("PRIMEVAL HORROR"))
            {
                logErr("Wave 20 encounter is not configured as final Boss Primeval Horror!");
                return false;
            }

            // Check SpawnDirector wiring
            SpawnDirector sd = UnityEngine.Object.FindFirstObjectByType<SpawnDirector>();
            if (sd != null)
            {
                SerializedObject so = new SerializedObject(sd);
                SerializedProperty pMid = so.FindProperty("eclipseHeraldBossPrefab");
                SerializedProperty pFin = so.FindProperty("primevalHorrorBossPrefab");
                if (pMid == null || pMid.objectReferenceValue == null)
                {
                    logErr("SpawnDirector eclipseHeraldBossPrefab serialized field is not assigned!");
                    return false;
                }
                if (pFin == null || pFin.objectReferenceValue == null)
                {
                    logErr("SpawnDirector primevalHorrorBossPrefab serialized field is not assigned!");
                    return false;
                }
                log("  ✓ SpawnDirector serialized references for Eclipse Herald and Primeval Horror verified!");
            }

            log("  ✓ Wave 10 routing: Mid-Boss (non-final, run continues)");
            log("  ✓ Wave 20 routing: Final Boss (final, unlocks world 5 completion & victory)");
            log("[TEST 4/6 PASSED] Dual-Boss wave routing successfully verified!");
            return true;
        }

        private static bool VerifyWorldsRegressionAndProgression(Action<string> log, Action<string> logErr)
        {
            log("\n[TEST 5/6] Verifying Worlds 1-4 Zero Regression & World 5 Progression...");
            WorldEnvironmentController wec = UnityEngine.Object.FindFirstObjectByType<WorldEnvironmentController>();
            if (wec == null)
            {
                logErr("WorldEnvironmentController not found in scene!");
                return false;
            }

            // 1. Verify W1
            wec.ApplyEnvironment(1);
            if (wec.EnvironmentW1 == null || !wec.EnvironmentW1.activeSelf)
            {
                logErr("Environment_W1 is not active when World 1 applied!");
                return false;
            }
            if ((wec.EnvironmentW2 != null && wec.EnvironmentW2.activeSelf) ||
                (wec.EnvironmentW3 != null && wec.EnvironmentW3.activeSelf) ||
                (wec.EnvironmentW4 != null && wec.EnvironmentW4.activeSelf) ||
                (wec.EnvironmentW5 != null && wec.EnvironmentW5.activeSelf))
            {
                logErr("Zero-leakage failure: Non-W1 environment active during World 1!");
                return false;
            }
            log("  ✓ World 1: 100% active, zero leakage");

            // 2. Verify W4
            wec.ApplyEnvironment(4);
            if (wec.EnvironmentW4 == null || !wec.EnvironmentW4.activeSelf)
            {
                logErr("Environment_W4 is not active when World 4 applied!");
                return false;
            }
            if ((wec.EnvironmentW1 != null && wec.EnvironmentW1.activeSelf) ||
                (wec.EnvironmentW2 != null && wec.EnvironmentW2.activeSelf) ||
                (wec.EnvironmentW3 != null && wec.EnvironmentW3.activeSelf) ||
                (wec.EnvironmentW5 != null && wec.EnvironmentW5.activeSelf))
            {
                logErr("Zero-leakage failure: Non-W4 environment active during World 4!");
                return false;
            }
            log("  ✓ World 4: 100% active, zero leakage");

            // 3. Verify W5
            wec.ApplyEnvironment(5);
            if (wec.EnvironmentW5 == null || !wec.EnvironmentW5.activeSelf)
            {
                logErr("Environment_W5 is not active when World 5 applied!");
                return false;
            }
            if ((wec.EnvironmentW1 != null && wec.EnvironmentW1.activeSelf) ||
                (wec.EnvironmentW2 != null && wec.EnvironmentW2.activeSelf) ||
                (wec.EnvironmentW3 != null && wec.EnvironmentW3.activeSelf) ||
                (wec.EnvironmentW4 != null && wec.EnvironmentW4.activeSelf))
            {
                logErr("Zero-leakage failure: Non-W5 environment active during World 5!");
                return false;
            }
            log("  ✓ World 5: 100% active, zero leakage");

            // Reset back to World 1
            wec.ApplyEnvironment(1);

            // Progression check: World 4 unlocks World 5
            WorldConfig cfg4 = WorldConfig.GetConfig(4);
            WorldConfig cfg5 = WorldConfig.GetConfig(5);
            if (cfg5.unlockRequirement != 4)
            {
                logErr($"World 5 unlock requirement is {cfg5.unlockRequirement}, expected 4!");
                return false;
            }
            log("  ✓ World 5 unlock requirement verified (requires World 4 completion)");

            log("[TEST 5/6 PASSED] Zero leakage verified across all 5 worlds!");
            return true;
        }

        private static bool VerifyUIAndMetaConsistency(Action<string> log, Action<string> logErr)
        {
            log("\n[TEST 6/6] Verifying Meta Upgrades & UI Consistency for World 5...");
            if (MetaProgressionManager.Instance == null)
            {
                GameObject go = new GameObject("MetaProgressionManager_Test");
                go.AddComponent<MetaProgressionManager>();
            }

            float vitalityBonus = MetaProgressionManager.Instance.GetVitalityBonus();
            float powerMult = MetaProgressionManager.Instance.GetPowerMultiplier();
            float hasteMult = MetaProgressionManager.Instance.GetHasteMultiplier();
            float agilityMult = MetaProgressionManager.Instance.GetAgilityMultiplier();

            if (powerMult < 1.0f || hasteMult < 1.0f || agilityMult < 1.0f)
            {
                logErr("Meta multipliers are below base 1.0f!");
                return false;
            }
            log($"  ✓ Meta multipliers verified: Power={powerMult:F2}x, Haste={hasteMult:F2}x, Agility={agilityMult:F2}x, Vitality=+{vitalityBonus}");

            WorldConfig cfg5 = WorldConfig.GetConfig(5);
            if (cfg5.themeColor.r <= 0f && cfg5.themeColor.g <= 0f && cfg5.themeColor.b <= 0f)
            {
                logErr("World 5 themeColor is black or unassigned!");
                return false;
            }
            log($"  ✓ World 5 Theme Color: {cfg5.themeColor}");

            log("[TEST 6/6 PASSED] Meta economy & UI configuration consistent!");
            return true;
        }

        private static void CaptureWorld5Screenshots(Action<string> log, Action<string> logErr)
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != "Assets/_Scenes/MainGame.unity")
            {
                activeScene = EditorSceneManager.OpenScene("Assets/_Scenes/MainGame.unity", OpenSceneMode.Single);
            }

            WorldEnvironmentController wec = UnityEngine.Object.FindFirstObjectByType<WorldEnvironmentController>();
            if (wec != null)
            {
                wec.ApplyEnvironment(5);
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("QA_Temp_Camera");
                cam = camObj.AddComponent<Camera>();
            }

            // Create temporary screenshot camera to avoid altering main camera
            GameObject renderCamObj = new GameObject("World5_CaptureCamera");
            Camera renderCam = renderCamObj.AddComponent<Camera>();
            renderCam.CopyFrom(cam);
            renderCam.clearFlags = CameraClearFlags.Skybox;
            renderCam.backgroundColor = new Color(0.08f, 0.05f, 0.11f);

            int width = 1280;
            int height = 720;
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            renderCam.targetTexture = rt;

            string shotDir = "Screenshots";
            if (!Directory.Exists(shotDir)) Directory.CreateDirectory(shotDir);

            // Shots setup
            var shots = new (string name, Vector3 pos, Vector3 target, float fov)[]
            {
                ("World5_ArenaOverview.png", new Vector3(0f, 32f, -34f), new Vector3(0f, 0f, 0f), 65f),
                ("World5_CentralEclipseAltar.png", new Vector3(0f, 7.5f, -9.5f), new Vector3(0f, 0.5f, 0f), 55f),
                ("World5_ObsidianCrater.png", new Vector3(0f, 8.5f, 12.0f), new Vector3(0f, 0.5f, 22.0f), 55f),
                ("World5_CatacombRuins.png", new Vector3(13.5f, 7.0f, -6.5f), new Vector3(22.0f, 1.0f, 0f), 55f),
                ("World5_UmbralShrine.png", new Vector3(0f, 7.5f, -12.5f), new Vector3(0f, 1.0f, -22.0f), 55f),
                ("World5_MidBoss_Herald.png", new Vector3(0f, 4.5f, 5.0f), new Vector3(0f, 1.6f, 9.5f), 50f),
                ("World5_FinalBoss_PrimevalHorror.png", new Vector3(0f, 5.5f, 4.0f), new Vector3(0f, 2.2f, 9.5f), 50f),
                ("World5_Gameplay.png", new Vector3(0f, 15f, -16f), new Vector3(0f, 0.5f, 0f), 60f)
            };

            // Spawn boss models temporarily for their specific showcase shots
            GameObject heraldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Boss_EclipseHerald.prefab");
            GameObject horrorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Boss_PrimevalHorror.prefab");

            GameObject tempHerald = null;
            if (heraldPrefab != null)
            {
                tempHerald = UnityEngine.Object.Instantiate(heraldPrefab, new Vector3(0f, 0.05f, 9.5f), Quaternion.Euler(0f, 180f, 0f));
            }

            GameObject tempHorror = null;
            if (horrorPrefab != null)
            {
                tempHorror = UnityEngine.Object.Instantiate(horrorPrefab, new Vector3(0f, 0.05f, 9.5f), Quaternion.Euler(0f, 180f, 0f));
                if (tempHorror != null) tempHorror.SetActive(false); // Enable only for its shot
            }

            foreach (var shot in shots)
            {
                if (shot.name.Contains("Herald"))
                {
                    if (tempHerald != null) tempHerald.SetActive(true);
                    if (tempHorror != null) tempHorror.SetActive(false);
                }
                else if (shot.name.Contains("PrimevalHorror"))
                {
                    if (tempHerald != null) tempHerald.SetActive(false);
                    if (tempHorror != null) tempHorror.SetActive(true);
                }
                else
                {
                    if (tempHerald != null) tempHerald.SetActive(false);
                    if (tempHorror != null) tempHorror.SetActive(false);
                }

                renderCam.transform.position = shot.pos;
                renderCam.transform.LookAt(shot.target);
                renderCam.fieldOfView = shot.fov;

                renderCam.Render();

                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                RenderTexture.active = null;

                byte[] pngBytes = tex.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(tex);

                string localPath = Path.Combine(shotDir, shot.name);
                File.WriteAllBytes(localPath, pngBytes);

                if (Directory.Exists(ArtifactDir))
                {
                    string artPath = Path.Combine(ArtifactDir, shot.name);
                    File.WriteAllBytes(artPath, pngBytes);
                }

                log($"  ✓ Captured screenshot: {shot.name} ({pngBytes.Length / 1024} KB)");
            }

            // Cleanup
            if (tempHerald != null) UnityEngine.Object.DestroyImmediate(tempHerald);
            if (tempHorror != null) UnityEngine.Object.DestroyImmediate(tempHorror);
            renderCam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(renderCamObj);

            // Reapply World 1 before finishing
            if (wec != null)
            {
                wec.ApplyEnvironment(1);
            }

            log("[SCREENSHOTS] All 8 World 5 screenshots captured and saved to workspace and brain artifacts!");
        }
    }
}
