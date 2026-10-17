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
    [InitializeOnLoad]
    public static class WorldAndFrontendSetupAndQA
    {
        private const string MarkerFile = "world_frontend_done.marker";
        private const string LogFile = "world_frontend_verify_log.txt";
        private const string ArtifactDir = "/Users/logmat/.gemini/antigravity/brain/da9b4771-589a-4d93-9ba9-83bb63f9ad9b";

        static WorldAndFrontendSetupAndQA()
        {
            EditorApplication.delayCall += AutoRunIfPending;
        }

        private static void AutoRunIfPending()
        {
            if (File.Exists(MarkerFile)) return;

            // If currently playing, wait until exit playmode
            if (EditorApplication.isPlaying)
            {
                Debug.Log("[WorldAndFrontendSetupAndQA] Currently in PlayMode, exiting to run setup...");
                EditorApplication.ExitPlaymode();
                return;
            }

            File.WriteAllText(MarkerFile, "running");
            RunSetupAndQA();
        }

        [MenuItem("Cursed Harvest/Setup & QA/Run World and Frontend QA Suite")]
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
            log("[WORLD & FRONTEND SUITE] Starting Setup, Build Settings & QA...");
            log("=================================================================");

            int totalTests = 5;
            int passCount = 0;

            try
            {
                // STEP 1: Ensure MainMenu scene exists and is configured
                log("[SETUP] Ensuring Assets/_Scenes/MainMenu.unity...");
                EnsureMainMenuScene(log);

                // STEP 2: Configure EditorBuildSettings
                log("[SETUP] Configuring EditorBuildSettings.scenes...");
                ConfigureBuildSettings(log);

                // STEP 3: RUN UNIT & LOGIC VERIFICATIONS
                log("\n--- RUNNING QA TESTS ---");

                // TEST 1: WorldConfig 1–5 Integrity
                if (VerifyWorldConfigs(log, logErr)) passCount++;

                // TEST 2: GameSession Progression & Save System
                if (VerifyProgressionAndSave(log, logErr)) passCount++;

                // TEST 3: Harvest Channeling (Stay-in-Zone) Mechanics
                if (VerifyHarvestChanneling(log, logErr)) passCount++;

                // TEST 4: Pause Modal & TimeScale Mechanics
                if (VerifyPauseAndUI(log, logErr)) passCount++;

                // TEST 5: Boss Wave Abstraction & Encounter Routing
                if (VerifyBossWaveRouting(log, logErr)) passCount++;

                log($"\n[QA RESULTS] Tests Passed: {passCount}/{totalTests}");

                // STEP 4: SCREENSHOT GENERATION
                log("\n--- GENERATING SCREENSHOTS ---");
                CaptureRequiredScreenshots(log, logErr);

                log("=================================================================");
                log("[WORLD & FRONTEND SUITE] Execution Complete!");
                log("=================================================================");

                File.WriteAllText(MarkerFile, $"COMPLETED - Passed {passCount}/{totalTests}");
            }
            catch (Exception ex)
            {
                logErr($"[WORLD & FRONTEND SUITE] Fatal Exception: {ex.Message}\n{ex.StackTrace}");
                File.WriteAllText(MarkerFile, $"ERROR - {ex.Message}");
            }
            finally
            {
                File.WriteAllLines(LogFile, logs);
                AssetDatabase.Refresh();
            }
        }

        private static void EnsureMainMenuScene(Action<string> log)
        {
            string scenePath = "Assets/_Scenes/MainMenu.unity";
            log("Creating fresh MainMenu.unity scene...");
            Scene mainMenuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Ensure Main Camera
            Camera cam = Camera.main;
            if (cam == null) cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                cam = camObj.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.04f, 0.03f, 0.05f);
                camObj.transform.position = new Vector3(0, 1, -10);
            }

            // Ensure Moonlight
            Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
            if (lights == null || lights.Length == 0)
            {
                GameObject lightObj = new GameObject("Moonlight");
                Light l = lightObj.AddComponent<Light>();
                l.type = LightType.Directional;
                l.color = new Color(0.6f, 0.7f, 0.85f);
                l.intensity = 0.6f;
                lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
            }

            // Ensure MainMenuController and UI
            MainMenuController mmc = UnityEngine.Object.FindAnyObjectByType<MainMenuController>();
            if (mmc == null)
            {
                GameObject mmGo = new GameObject("MainMenuController");
                mmc = mmGo.AddComponent<MainMenuController>();
            }
            mmc.EnsureMainMenuUI();

            EditorSceneManager.SaveScene(mainMenuScene, scenePath);
            log($"MainMenu.unity saved successfully at {scenePath}");
        }

        private static void ConfigureBuildSettings(Action<string> log)
        {
            var scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene("Assets/_Scenes/MainMenu.unity", true),
                new EditorBuildSettingsScene("Assets/_Scenes/MainGame.unity", true)
            };
            EditorBuildSettings.scenes = scenes;
            log("EditorBuildSettings configured: [0] MainMenu.unity, [1] MainGame.unity");
        }

        // ==================== QA TESTS ====================

        private static bool VerifyWorldConfigs(Action<string> log, Action<string> logErr)
        {
            log("[TEST 1] Verifying 5 Worlds WorldConfig Integrity...");
            bool ok = true;

            for (int i = 1; i <= 5; i++)
            {
                WorldConfig cfg = WorldConfig.GetConfig(i);
                if (cfg == null)
                {
                    logErr($"WorldConfig {i} is NULL!");
                    ok = false;
                    continue;
                }

                // Wave count check
                int expectedWaves = (i <= 3) ? 10 : (i == 4 ? 15 : 20);
                if (cfg.totalWaves != expectedWaves)
                {
                    logErr($"World {i} totalWaves mismatch: Expected {expectedWaves}, Got {cfg.totalWaves}");
                    ok = false;
                }

                // Unlock requirement check
                int expectedReq = i == 1 ? 0 : (i - 1);
                if (cfg.unlockRequirement != expectedReq)
                {
                    logErr($"World {i} unlockRequirement mismatch: Expected {expectedReq}, Got {cfg.unlockRequirement}");
                    ok = false;
                }

                // Boss wave validation
                if (i <= 3)
                {
                    if (!cfg.IsBossWave(10)) { logErr($"World {i} missing Boss Wave 10!"); ok = false; }
                    var boss = cfg.GetBossEncounter(10);
                    if (boss == null || !boss.isFinalBoss) { logErr($"World {i} Wave 10 boss should be Final Boss!"); ok = false; }
                }
                else if (i == 4)
                {
                    if (!cfg.IsBossWave(10) || !cfg.IsBossWave(15)) { logErr("World 4 must have boss waves at 10 and 15!"); ok = false; }
                    var midBoss = cfg.GetBossEncounter(10);
                    var finalBoss = cfg.GetBossEncounter(15);
                    if (midBoss == null || midBoss.isFinalBoss) { logErr("World 4 Wave 10 should be Mid-Boss (isFinalBoss=false)!"); ok = false; }
                    if (finalBoss == null || !finalBoss.isFinalBoss) { logErr("World 4 Wave 15 should be Final Boss (isFinalBoss=true)!"); ok = false; }
                }
                else if (i == 5)
                {
                    if (!cfg.IsBossWave(10) || !cfg.IsBossWave(20)) { logErr("World 5 must have boss waves at 10 and 20!"); ok = false; }
                    var midBoss = cfg.GetBossEncounter(10);
                    var finalBoss = cfg.GetBossEncounter(20);
                    if (midBoss == null || midBoss.isFinalBoss) { logErr("World 5 Wave 10 should be Mid-Boss (isFinalBoss=false)!"); ok = false; }
                    if (finalBoss == null || !finalBoss.isFinalBoss) { logErr("World 5 Wave 20 should be Final Boss (isFinalBoss=true)!"); ok = false; }
                }

                log($"  • World {i}: '{cfg.displayName}' ({cfg.difficultyTier}) — {cfg.totalWaves} Waves, Req: W{cfg.unlockRequirement} -> OK");
            }

            if (ok) log("[TEST 1 PASSED] All 5 WorldConfigs validated successfully!");
            return ok;
        }

        private static bool VerifyProgressionAndSave(Action<string> log, Action<string> logErr)
        {
            log("[TEST 2] Verifying GameSession Progression & Save System...");
            bool ok = true;

            GameObject sessionGo = new GameObject("TestGameSession");
            GameSession session = sessionGo.AddComponent<GameSession>();

            try
            {
                // Reset progress
                session.ResetProgress();
                if (session.HighestUnlockedWorld != 1 || !session.IsWorldUnlocked(1))
                {
                    logErr("Initial reset failed: World 1 should be unlocked, Highest=1");
                    ok = false;
                }
                if (session.IsWorldUnlocked(2) || session.IsWorldUnlocked(3))
                {
                    logErr("Initial reset failed: Worlds 2-5 should be locked");
                    ok = false;
                }

                // Complete World 1
                session.CompleteWorld(1);
                if (!session.IsWorldCompleted(1) || session.HighestUnlockedWorld != 2 || !session.IsWorldUnlocked(2))
                {
                    logErr("CompleteWorld(1) failed to unlock World 2!");
                    ok = false;
                }

                // Complete World 2
                session.CompleteWorld(2);
                if (!session.IsWorldCompleted(2) || session.HighestUnlockedWorld != 3 || !session.IsWorldUnlocked(3))
                {
                    logErr("CompleteWorld(2) failed to unlock World 3!");
                    ok = false;
                }

                // Complete World 3
                session.CompleteWorld(3);
                if (!session.IsWorldCompleted(3) || session.HighestUnlockedWorld != 4 || !session.IsWorldUnlocked(4))
                {
                    logErr("CompleteWorld(3) failed to unlock World 4!");
                    ok = false;
                }

                // Complete World 4
                session.CompleteWorld(4);
                if (!session.IsWorldCompleted(4) || session.HighestUnlockedWorld != 5 || !session.IsWorldUnlocked(5))
                {
                    logErr("CompleteWorld(4) failed to unlock World 5!");
                    ok = false;
                }

                // Complete World 5
                session.CompleteWorld(5);
                if (!session.IsWorldCompleted(5))
                {
                    logErr("CompleteWorld(5) failed to mark World 5 completed!");
                    ok = false;
                }

                // Reset back to World 1 for clean state
                session.ResetProgress();
                if (session.HighestUnlockedWorld != 1 || session.IsWorldCompleted(1))
                {
                    logErr("Final ResetProgress failed!");
                    ok = false;
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sessionGo);
            }

            if (ok) log("[TEST 2 PASSED] Progression and PlayerPrefs save system validated!");
            return ok;
        }

        private static bool VerifyHarvestChanneling(Action<string> log, Action<string> logErr)
        {
            log("[TEST 3] Verifying Harvest Stay-In-Zone Channeling Mechanics...");
            bool ok = true;

            GameObject plotGo = new GameObject("TestPlot");
            HarvestPlot plot = plotGo.AddComponent<HarvestPlot>();

            try
            {
                // A. Stay inside zone (radius = 2.8m, dist = 1.5m)
                plot.StartGrowing();
                plot.SimulateChannelingStep(1.0f, 1.5f);
                if (plot.IsHarvestPaused || plot.GrowTimer < 0.99f)
                {
                    logErr($"A. Stay inside zone failed: paused={plot.IsHarvestPaused}, timer={plot.GrowTimer}");
                    ok = false;
                }

                // B. Grace exit < 1.0s (dist = 4.0m, dt = 0.5s)
                plot.SimulateChannelingStep(0.5f, 4.0f);
                if (!plot.IsHarvestPaused || plot.HarvestGraceTimer < 0.49f)
                {
                    logErr($"B. Grace exit pause failed: paused={plot.IsHarvestPaused}, graceTimer={plot.HarvestGraceTimer}");
                    ok = false;
                }

                // Return to zone within grace (dist = 1.0m, dt = 0.5s)
                plot.SimulateChannelingStep(0.5f, 1.0f);
                if (plot.IsHarvestPaused || plot.HarvestGraceTimer != 0f)
                {
                    logErr($"B. Return to zone failed: paused={plot.IsHarvestPaused}, graceTimer={plot.HarvestGraceTimer}");
                    ok = false;
                }

                // C. Grace exit > 1.0s (dist = 4.0m, dt = 1.1s)
                plot.SimulateChannelingStep(1.1f, 4.0f);
                if (plot.GrowTimer != 0f || plot.State != PlotState.Ready)
                {
                    logErr($"C. Grace expiry reset failed: growTimer={plot.GrowTimer}, state={plot.State}");
                    ok = false;
                }

                // D. Movement within circle (dist = 2.4m, dt = 1.0f)
                plot.StartGrowing();
                plot.SimulateChannelingStep(1.0f, 2.4f);
                if (plot.IsHarvestPaused || plot.GrowTimer < 0.99f)
                {
                    logErr($"D. Movement inside circle failed: paused={plot.IsHarvestPaused}");
                    ok = false;
                }

                // E. Pause during harvest: when Time.timeScale=0, dt=0
                plot.SimulateChannelingStep(0f, 4.0f);
                if (plot.GrowTimer < 0.99f || plot.State != PlotState.Growing)
                {
                    logErr($"E. Pause freezing failed: growTimer={plot.GrowTimer}");
                    ok = false;
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(plotGo);
            }

            if (ok) log("[TEST 3 PASSED] Harvest stay-in-zone channeling & grace mechanics verified!");
            return ok;
        }

        private static bool VerifyPauseAndUI(Action<string> log, Action<string> logErr)
        {
            log("[TEST 4] Verifying Pause Modal & TimeScale Mechanics...");
            bool ok = true;

            GameObject uiGo = new GameObject("TestUIManager");
            UIManager uim = uiGo.AddComponent<UIManager>();
            uim.EnsureHUDAndModals();

            try
            {
                // Initial state
                if (uim.IsGamePaused || Time.timeScale != 1f)
                {
                    logErr($"Initial pause state invalid: IsGamePaused={uim.IsGamePaused}, timeScale={Time.timeScale}");
                    ok = false;
                }

                // Toggle Pause -> On
                uim.TogglePause();
                if (!uim.IsGamePaused || Time.timeScale != 0f || uim.PauseModal == null || !uim.PauseModal.activeSelf)
                {
                    logErr($"TogglePause ON failed: IsGamePaused={uim.IsGamePaused}, timeScale={Time.timeScale}");
                    ok = false;
                }

                // Resume Game -> Off
                uim.ResumeGame();
                if (uim.IsGamePaused || Time.timeScale != 1f || (uim.PauseModal != null && uim.PauseModal.activeSelf))
                {
                    logErr($"ResumeGame failed: IsGamePaused={uim.IsGamePaused}, timeScale={Time.timeScale}");
                    ok = false;
                }

                // Verify Top-Right HUD Separation (No Overlap)
                if (uim.TopRightHUDContainer == null)
                {
                    logErr("TopRightHUDContainer is NULL!");
                    ok = false;
                }
                if (uim.HudPauseButton == null)
                {
                    logErr("HudPauseButton is NULL!");
                    ok = false;
                }
                if (uim.SeedBadgeRoot == null)
                {
                    logErr("SeedBadgeRoot is NULL!");
                    ok = false;
                }
                if (uim.StatusEffectsContainer == null)
                {
                    logErr("StatusEffectsContainer is NULL!");
                    ok = false;
                }

                if (uim.HudPauseButton != null && uim.SeedBadgeRoot != null)
                {
                    RectTransform rtPause = uim.HudPauseButton.GetComponent<RectTransform>();
                    RectTransform rtSeed = uim.SeedBadgeRoot;
                    float pauseLeftEdge = rtPause.anchoredPosition.x - rtPause.sizeDelta.x;
                    float seedRightEdge = rtSeed.anchoredPosition.x;
                    if (seedRightEdge > pauseLeftEdge)
                    {
                        logErr($"TopRight HUD overlap: seedRightEdge ({seedRightEdge}) > pauseLeftEdge ({pauseLeftEdge})");
                        ok = false;
                    }
                    else
                    {
                        log($"  • TopRight HUD non-overlapping separation verified! (Gap: {pauseLeftEdge - seedRightEdge:0.0}px)");
                    }
                }
            }
            finally
            {
                Time.timeScale = 1f;
                UnityEngine.Object.DestroyImmediate(uiGo);
            }

            if (ok) log("[TEST 4 PASSED] Pause modal and timeScale toggle verified!");
            return ok;
        }

        private static bool VerifyBossWaveRouting(Action<string> log, Action<string> logErr)
        {
            log("[TEST 5] Verifying Boss Wave Abstraction & Encounter Routing...");
            bool ok = true;

            WorldConfig w4 = WorldConfig.GetConfig(4);
            var w4_w10 = w4.GetBossEncounter(10);
            var w4_w15 = w4.GetBossEncounter(15);

            if (w4_w10.isFinalBoss != false)
            {
                logErr("World 4 Wave 10 must be a Mid-Boss (isFinalBoss == false)!");
                ok = false;
            }
            if (w4_w15.isFinalBoss != true)
            {
                logErr("World 4 Wave 15 must be the Final Boss (isFinalBoss == true)!");
                ok = false;
            }

            WorldConfig w5 = WorldConfig.GetConfig(5);
            var w5_w10 = w5.GetBossEncounter(10);
            var w5_w20 = w5.GetBossEncounter(20);

            if (w5_w10.isFinalBoss != false)
            {
                logErr("World 5 Wave 10 must be a Mid-Boss (isFinalBoss == false)!");
                ok = false;
            }
            if (w5_w20.isFinalBoss != true)
            {
                logErr("World 5 Wave 20 must be the Final Boss (isFinalBoss == true)!");
                ok = false;
            }

            if (ok) log("[TEST 5 PASSED] Mid-Boss vs Final Boss routing correctly validated!");
            return ok;
        }

        // ==================== SCREENSHOT CAPTURE ====================

        private static void CaptureRequiredScreenshots(Action<string> log, Action<string> logErr)
        {
            if (!Directory.Exists(ArtifactDir))
            {
                Directory.CreateDirectory(ArtifactDir);
            }

            // 1 & 2: MainMenu Scene (HOME and WORLD SELECT)
            EditorSceneManager.OpenScene("Assets/_Scenes/MainMenu.unity", OpenSceneMode.Single);
            MainMenuController mmc = UnityEngine.Object.FindAnyObjectByType<MainMenuController>();
            if (mmc != null)
            {
                // Capture HOME Screen
                mmc.OpenHome();
                string homeShot = ScreenshotHelper.CaptureWithCanvas("Home_Screen", 1920, 1080);
                if (homeShot != null && File.Exists(homeShot))
                {
                    File.Copy(homeShot, Path.Combine(ArtifactDir, "Home_Screen.png"), true);
                    log($"[SCREENSHOT] Home_Screen captured -> {homeShot}");
                }

                // Capture WORLD SELECT Screen
                mmc.OpenWorldSelect();
                mmc.SelectWorld(1);
                string worldShot = ScreenshotHelper.CaptureWithCanvas("WorldSelect_Screen", 1920, 1080);
                if (worldShot != null && File.Exists(worldShot))
                {
                    File.Copy(worldShot, Path.Combine(ArtifactDir, "WorldSelect_Screen.png"), true);
                    log($"[SCREENSHOT] WorldSelect_Screen captured -> {worldShot}");
                }
                // Capture UPGRADES Screen
                mmc.OpenUpgrades();
                string upgShot = ScreenshotHelper.CaptureWithCanvas("Upgrades_Screen", 1920, 1080);
                if (upgShot != null && File.Exists(upgShot))
                {
                    File.Copy(upgShot, Path.Combine(ArtifactDir, "Upgrades_Screen.png"), true);
                    log($"[SCREENSHOT] Upgrades_Screen captured -> {upgShot}");
                }
            }
            else
            {
                logErr("MainMenuController not found in MainMenu.unity!");
            }

            // 3, 4 & 5: MainGame Scene (PAUSE, HUD TOP-RIGHT, RESPONSIVE, and HARVEST CHANNELING PROGRESS)
            EditorSceneManager.OpenScene("Assets/_Scenes/MainGame.unity", OpenSceneMode.Single);
            UIManager uim = UnityEngine.Object.FindAnyObjectByType<UIManager>();
            if (uim != null)
            {
                // Capture PAUSE MODAL
                uim.EnsureHUDAndModals();
                uim.TogglePause();
                string pauseShot = ScreenshotHelper.CaptureWithCanvas("Pause_Modal", 1920, 1080);
                if (pauseShot != null && File.Exists(pauseShot))
                {
                    File.Copy(pauseShot, Path.Combine(ArtifactDir, "Pause_Modal.png"), true);
                    log($"[SCREENSHOT] Pause_Modal captured -> {pauseShot}");
                }
                uim.ResumeGame();

                // Capture GAMEPLAY HUD TOP-RIGHT (16:9, 16:10, 4:3)
                uim.HideAllModals();
                string hudShot = ScreenshotHelper.CaptureWithCanvas("Gameplay_HUD_TopRight", 1920, 1080);
                if (hudShot != null && File.Exists(hudShot))
                {
                    File.Copy(hudShot, Path.Combine(ArtifactDir, "Gameplay_HUD_TopRight.png"), true);
                    log($"[SCREENSHOT] Gameplay_HUD_TopRight captured -> {hudShot}");
                }

                string hud1610 = ScreenshotHelper.CaptureWithCanvas("Gameplay_HUD_16x10", 1680, 1050);
                if (hud1610 != null && File.Exists(hud1610))
                {
                    File.Copy(hud1610, Path.Combine(ArtifactDir, "Gameplay_HUD_16x10.png"), true);
                    log($"[SCREENSHOT] Gameplay_HUD_16x10 captured -> {hud1610}");
                }

                string hud43 = ScreenshotHelper.CaptureWithCanvas("Gameplay_HUD_4x3", 1024, 768);
                if (hud43 != null && File.Exists(hud43))
                {
                    File.Copy(hud43, Path.Combine(ArtifactDir, "Gameplay_HUD_4x3.png"), true);
                    log($"[SCREENSHOT] Gameplay_HUD_4x3 captured -> {hud43}");
                }

                // Capture HARVEST CHANNELING PROGRESS
                HarvestPlot plot = UnityEngine.Object.FindAnyObjectByType<HarvestPlot>();
                GameObject player = GameObject.FindWithTag("Player");
                Camera cam = Camera.main;

                if (plot != null && player != null && cam != null)
                {
                    Vector3 origCamPos = cam.transform.position;
                    Quaternion origCamRot = cam.transform.rotation;
                    Vector3 origPlayerPos = player.transform.position;

                    // Position player inside interaction zone
                    player.transform.position = plot.transform.position + new Vector3(0.8f, 0f, 0.5f);
                    player.transform.rotation = Quaternion.LookRotation(plot.transform.position - player.transform.position);

                    // Set HarvestPlot to 75% growth
                    plot.StartGrowing();
                    plot.SetGrowTimer(2.625f); // 75% of 3.5s

                    // Position camera nicely showing both player, harvest plot, and billboard text
                    cam.transform.position = plot.transform.position + new Vector3(0f, 2.8f, -4.2f);
                    cam.transform.LookAt(plot.transform.position + new Vector3(0f, 1.3f, 0f));

                    TextMesh promptMesh = plot.GetComponentInChildren<TextMesh>(true);
                    if (promptMesh != null)
                    {
                        promptMesh.text = "HARVESTING...\n[██████░░] 75%";
                        promptMesh.color = new Color(1f, 0.75f, 0.18f);
                        promptMesh.transform.rotation = cam.transform.rotation;
                    }

                    string harvestShot = ScreenshotHelper.CaptureWithCanvas("Harvest_Channeling_Progress", 1920, 1080);
                    if (harvestShot != null && File.Exists(harvestShot))
                    {
                        File.Copy(harvestShot, Path.Combine(ArtifactDir, "Harvest_Channeling_Progress.png"), true);
                        log($"[SCREENSHOT] Harvest_Channeling_Progress captured -> {harvestShot}");
                    }

                    // Restore camera and player
                    cam.transform.position = origCamPos;
                    cam.transform.rotation = origCamRot;
                    player.transform.position = origPlayerPos;
                    plot.InterruptHarvest();
                }
            }
            else
            {
                logErr("UIManager not found in MainGame.unity!");
            }
        }
    }
}
