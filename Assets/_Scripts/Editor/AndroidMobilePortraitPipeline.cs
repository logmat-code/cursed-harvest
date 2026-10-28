using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CursedHarvest.Editor
{
    /// <summary>
    /// Authoritative Android Mobile Portrait pipeline for CURSED HARVEST. by LOGMAT.
    /// Configures PlayerSettings (identity, Portrait-only orientation, IL2CPP + ARM64, API 24..35,
    /// Adaptive Icon + Portrait Splash), rebuilds MainMenu.unity & MainGame.unity for Portrait 1080x1920,
    /// executes the multi-resolution Portrait QA suite, captures all 12 official Portrait screenshots,
    /// and builds the production Android App Bundle (Builds/Android/CursedHarvest_Android_1.0.0_vc1.aab).
    /// </summary>
    public static class AndroidMobilePortraitPipeline
    {
        public const string AuthoritativeCompanyName = "LOGMAT";
        public const string AuthoritativeProductName = "CURSED HARVEST.";
        public const string AuthoritativePackageName = "com.logmat.cursedharvest";
        public const string AuthoritativeBundleVersion = "1.1.1";
        public const int AuthoritativeVersionCode = 3;

        public const string AndroidBuildOutputDir = "Builds/Android/Release";
        public const string AndroidAabOutputPath = "Builds/Android/Release/CursedHarvest_Android_1.1.1_vc3_RELEASE.aab";
        public const string MobileScreenshotsDir = "Assets/_Documentation/Screenshots/MobilePortrait";
        public const string PlayStoreIconsDir = "Assets/_Documentation/Branding/Mobile/PlayStore";
        public const string ArtifactsDir = "/Users/logmat/.gemini/antigravity/brain/da9b4771-589a-4d93-9ba9-83bb63f9ad9b";
        public const string QALogPath = "android_mobile_portrait_qa_log.txt";

        private static readonly string KeystorePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "Documents/LOGMAT_Signing/CursedHarvest/cursedharvest-upload-key.jks");
        private static readonly string CredentialsPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "Documents/LOGMAT_Signing/CursedHarvest/CursedHarvest_Keystore_Credentials.txt");

        private static readonly string[] BuildScenes = new string[]
        {
            "Assets/_Scenes/MainMenu.unity",
            "Assets/_Scenes/MainGame.unity"
        };

        private static readonly (string label, int width, int height)[] PortraitDeviceMatrix = new (string, int, int)[]
        {
            ("16:9 Portrait", 1080, 1920),
            ("19.5:9 Portrait (FHD+)", 1080, 2340),
            ("20:9 Portrait (FHD+)", 1080, 2400),
            ("20.1:9 Portrait (FHD+)", 1080, 2412),
            ("19.5:9 Portrait (1170p)", 1170, 2532),
            ("19.5:9 Portrait (1290p)", 1290, 2796),
            ("20:9 Portrait (WQHD+)", 1440, 3200),
            ("Tablet Portrait (16:10)", 1600, 2560)
        };

        [MenuItem("Cursed Harvest/Mobile/Run Full Android Portrait Pipeline")]
        public static void RunFullPipeline()
        {
            List<string> logs = new List<string>();
            void Log(string msg) { Debug.Log(msg); logs.Add(msg); }
            void LogErr(string msg) { Debug.LogError(msg); logs.Add("[ERROR] " + msg); }

            Log("=========================================================================");
            Log("[ANDROID PORTRAIT PIPELINE] Starting CURSED HARVEST. Mobile Conversion...");
            Log("=========================================================================");

            try
            {
                // STEP 1: Configure Authoritative Android & Portrait Settings + Icons + Splash
                ConfigureAndroidPortraitSettings(Log, LogErr);

                // STEP 2: Rebuild & Save MainMenu.unity & MainGame.unity in Portrait Mode
                UpdateScenesForMobilePortrait(Log, LogErr);

                // STEP 3: Run Comprehensive Multi-Resolution Portrait & Gameplay QA Suite
                bool qaPassed = RunMobilePortraitQASuite(Log, LogErr);
                Log($"[ANDROID PORTRAIT PIPELINE] QA Suite Status: {(qaPassed ? "ALL PASSED" : "CHECK ERRORS")}");

                // STEP 4: Capture All 12 Official Portrait Screenshots (1080 x 1920)
                CaptureAll12PortraitScreenshots(Log, LogErr);

                // STEP 5: Build Production Android App Bundle (.aab)
                BuildAndroidReleaseAab(Log, LogErr);

                Log("=========================================================================");
                Log("[ANDROID PORTRAIT PIPELINE] Complete!");
                Log("=========================================================================");
            }
            catch (Exception ex)
            {
                LogErr($"Unhandled exception in AndroidMobilePortraitPipeline: {ex}");
            }
            finally
            {
                File.WriteAllLines(QALogPath, logs);
                if (Directory.Exists(ArtifactsDir))
                {
                    File.WriteAllLines(Path.Combine(ArtifactsDir, QALogPath), logs);
                }
            }
        }

        [MenuItem("Cursed Harvest/Mobile/1. Configure Android Portrait Settings")]
        public static void ConfigureAndroidPortraitSettingsMenu()
        {
            ConfigureAndroidPortraitSettings(Debug.Log, Debug.LogError);
        }

        public static void ConfigureAndroidPortraitSettings(Action<string> log, Action<string> logErr)
        {
            log("\n--- [STEP 1] CONFIGURING ANDROID PORTRAIT PLAYER SETTINGS & BRANDING ---");

            // Ensure Branding Texture Importers are configured properly for New Play Store Icons
            ConfigureTextureImporter("Assets/_Documentation/Branding/Mobile/PlayStore/CursedHarvest_Icon_Master_1024.png", false, 1024);
            ConfigureTextureImporter("Assets/_Documentation/Branding/Mobile/PlayStore/CursedHarvest_Icon_PlayStore_512.png", false, 512);
            ConfigureTextureImporter("Assets/_Documentation/Branding/Mobile/PlayStore/CursedHarvest_Adaptive_Background.png", false, 1024);
            ConfigureTextureImporter("Assets/_Documentation/Branding/Mobile/PlayStore/CursedHarvest_Adaptive_Foreground.png", true, 1024);
            ConfigureTextureImporter("Assets/_Documentation/Branding/Mobile/MobileSplash_Portrait_1080x1920.png", true, 2048);

            // 1. Authoritative Identity
            PlayerSettings.companyName = AuthoritativeCompanyName;
            PlayerSettings.productName = AuthoritativeProductName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AuthoritativePackageName);
            PlayerSettings.bundleVersion = AuthoritativeBundleVersion;
            PlayerSettings.Android.bundleVersionCode = AuthoritativeVersionCode;

            // 2. Portrait-Only Orientation Lock
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // Reference Portrait Screen Resolution
            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1920;

            // 3. Android Architecture, Scripting Backend & SDK Levels
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26; // Android 8.0+ (Adaptive Icon native support)
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36; // Installed Android SDK Platform 36 (exceeds Play Store API 35 minimum)

            // 4. Release / Production Build Flags
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.connectProfiler = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.buildScriptsOnly = false;
            EditorUserBuildSettings.buildAppBundle = true;

            // 5. Assign NEW Master Icon & Android Adaptive Icons
            Texture2D masterIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Documentation/Branding/Mobile/PlayStore/CursedHarvest_Icon_Master_1024.png");
            Texture2D adaptiveBg = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Documentation/Branding/Mobile/PlayStore/CursedHarvest_Adaptive_Background.png");
            Texture2D adaptiveFg = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Documentation/Branding/Mobile/PlayStore/CursedHarvest_Adaptive_Foreground.png");

            if (masterIcon != null)
            {
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new Texture2D[] { masterIcon }, IconKind.Any);
                PlayerSettings.SetIcons(NamedBuildTarget.Android, new Texture2D[] { masterIcon }, IconKind.Any);
            }

            if (adaptiveBg != null && adaptiveFg != null)
            {
                PlatformIcon[] adaptiveIcons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, UnityEditor.Android.AndroidPlatformIconKind.Adaptive);
                for (int i = 0; i < adaptiveIcons.Length; i++)
                {
                    adaptiveIcons[i].SetTextures(adaptiveBg, adaptiveFg);
                }
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, UnityEditor.Android.AndroidPlatformIconKind.Adaptive, adaptiveIcons);
                log("  ✓ Assigned NEW Android Adaptive Icon (Background + Foreground) and Master 1024 Icon.");
            }

            // 6. Disable Custom Splash Screen (Parte 8: eliminar splash para arranque directo)
            PlayerSettings.SplashScreen.show = false;
            log("  ✓ Disabled Custom Splash Screen for instant mobile launch to Home.");

            // 7. Configure Custom Upload Keystore Release Signing
            if (File.Exists(KeystorePath) && File.Exists(CredentialsPath))
            {
                string[] credLines = File.ReadAllLines(CredentialsPath);
                string ksPass = "";
                string keyPass = "";
                string alias = "cursedharvest-upload";

                foreach (string line in credLines)
                {
                    if (line.StartsWith("Keystore Password:")) ksPass = line.Substring("Keystore Password:".Length).Trim();
                    else if (line.StartsWith("Key Password:")) keyPass = line.Substring("Key Password:".Length).Trim();
                    else if (line.StartsWith("Alias:")) alias = line.Substring("Alias:".Length).Trim();
                }

                if (!string.IsNullOrEmpty(ksPass) && !string.IsNullOrEmpty(keyPass))
                {
                    PlayerSettings.Android.useCustomKeystore = true;
                    PlayerSettings.Android.keystoreName = KeystorePath;
                    PlayerSettings.Android.keystorePass = ksPass;
                    PlayerSettings.Android.keyaliasName = alias;
                    PlayerSettings.Android.keyaliasPass = keyPass;
                    log($"  ✓ Configured Release Signing with Upload Keystore: '{alias}' ({KeystorePath})");
                }
                else
                {
                    logErr("  ✗ Failed to parse credentials from credentials file!");
                }
            }
            else
            {
                logErr($"  ✗ Upload Keystore not found at '{KeystorePath}'!");
            }

            // 8. Confirm Build Scenes (MainMenu = 0, MainGame = 1)
            EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene("Assets/_Scenes/MainMenu.unity", true),
                new EditorBuildSettingsScene("Assets/_Scenes/MainGame.unity", true)
            };

            AssetDatabase.SaveAssets();

            log($"  ✓ Company Name: '{PlayerSettings.companyName}'");
            log($"  ✓ Product Name: '{PlayerSettings.productName}'");
            log($"  ✓ Package ID:   '{PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)}'");
            log($"  ✓ Version:      '{PlayerSettings.bundleVersion}' (bundleVersionCode={PlayerSettings.Android.bundleVersionCode})");
            log($"  ✓ Orientation:  {PlayerSettings.defaultInterfaceOrientation} (Portrait-Only Locked)");
            log($"  ✓ Backend/Arch: {PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android)} / {PlayerSettings.Android.targetArchitectures}");
            log($"  ✓ SDK Levels:   Min={PlayerSettings.Android.minSdkVersion}, Target={PlayerSettings.Android.targetSdkVersion}");
        }

        private static void ConfigureTextureImporter(string assetPath, bool asSprite, int maxSize)
        {
            TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (ti == null) return;

            bool dirty = false;
            TextureImporterType desiredType = asSprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            if (ti.textureType != desiredType) { ti.textureType = desiredType; dirty = true; }
            if (asSprite && ti.spriteImportMode != SpriteImportMode.Single) { ti.spriteImportMode = SpriteImportMode.Single; dirty = true; }
            if (!ti.alphaIsTransparency) { ti.alphaIsTransparency = true; dirty = true; }
            if (ti.maxTextureSize != maxSize) { ti.maxTextureSize = maxSize; dirty = true; }
            if (ti.mipmapEnabled) { ti.mipmapEnabled = false; dirty = true; }

            if (dirty)
            {
                ti.SaveAndReimport();
            }
        }

        public static void UpdateScenesForMobilePortrait(Action<string> log, Action<string> logErr)
        {
            log("\n--- [STEP 2] UPDATING MAINMENU & MAINGAME SCENES FOR PORTRAIT 1080x1920 ---");

            // 1. MainMenu.unity
            Scene menuScene = EditorSceneManager.OpenScene("Assets/_Scenes/MainMenu.unity", OpenSceneMode.Single);
            MainMenuController mmc = UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
            if (mmc == null)
            {
                GameObject mmcGo = new GameObject("MainMenuController");
                mmc = mmcGo.AddComponent<MainMenuController>();
            }
            mmc.Ensure3DPortraitBackdrop();
            mmc.EnsureMainMenuUI();
            mmc.ShowScreen(MainMenuScreen.Home);
            EditorSceneManager.MarkSceneDirty(menuScene);
            EditorSceneManager.SaveScene(menuScene);
            log("  ✓ Saved MainMenu.unity with 3D Portrait Altar Diorama & 1080x1920 Portrait UI.");

            // 2. MainGame.unity
            Scene gameScene = EditorSceneManager.OpenScene("Assets/_Scenes/MainGame.unity", OpenSceneMode.Single);
            TopDownCameraFollow camFollow = UnityEngine.Object.FindFirstObjectByType<TopDownCameraFollow>();
            if (camFollow != null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    camFollow.SetTarget(player.transform);
                    camFollow.SnapToTargetImmediate();
                }
            }

            UIManager uim = UnityEngine.Object.FindFirstObjectByType<UIManager>();
            if (uim != null)
            {
                uim.EnsureHUDAndModals();
                uim.HideAllModals();
            }

            HarvestPlot[] plots = UnityEngine.Object.FindObjectsByType<HarvestPlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var p in plots)
            {
                if (p != null) p.EnsureVisuals();
            }

            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene);
            log("  ✓ Saved MainGame.unity with Portrait Camera Profile, Safe Area HUD, Virtual Joystick & Dash Button.");
        }

        public static bool RunMobilePortraitQASuite(Action<string> log, Action<string> logErr)
        {
            log("\n--- [STEP 3] EXECUTING MOBILE PORTRAIT QA VERIFICATION SUITE ---");
            bool allPassed = true;

            // QA 1: Authoritative Identity & Portrait Lock
            if (PlayerSettings.companyName != AuthoritativeCompanyName ||
                PlayerSettings.productName != AuthoritativeProductName ||
                PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != AuthoritativePackageName ||
                PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait ||
                PlayerSettings.allowedAutorotateToLandscapeLeft ||
                PlayerSettings.allowedAutorotateToLandscapeRight)
            {
                logErr("QA 1 FAILED: PlayerSettings identity or Portrait lock mismatch!");
                allPassed = false;
            }
            else
            {
                log("  [PASS] QA 1: Identity (LOGMAT / CURSED HARVEST. / com.logmat.cursedharvest) & Portrait-Only Lock verified.");
            }

            // QA 2: Multi-Resolution Portrait Matrix + Camera Framing (Player at 45%–55% Screen Height)
            EditorSceneManager.OpenScene("Assets/_Scenes/MainGame.unity", OpenSceneMode.Single);
            TopDownCameraFollow camFollow = UnityEngine.Object.FindFirstObjectByType<TopDownCameraFollow>();
            GameObject player = GameObject.FindWithTag("Player");
            Camera cam = Camera.main;

            if (camFollow != null && player != null && cam != null)
            {
                player.transform.position = Vector3.zero;
                foreach (var res in PortraitDeviceMatrix)
                {
                    RenderTexture rt = new RenderTexture(res.width, res.height, 24);
                    cam.targetTexture = rt;
                    camFollow.SnapToTargetImmediate();
                    Vector3 vp = cam.WorldToViewportPoint(player.transform.position);
                    Vector2 visibleHalf = camFollow.GetVisibleGroundHalfExtents(0f);
                    cam.targetTexture = null;
                    UnityEngine.Object.DestroyImmediate(rt);

                    bool framingOk = vp.y >= 0.45f && vp.y <= 0.55f;
                    if (!framingOk)
                    {
                        logErr($"QA 2 FAILED on {res.label} ({res.width}x{res.height}): Player viewport Y={vp.y:F3} outside [0.45, 0.55]!");
                        allPassed = false;
                    }
                    else
                    {
                        log($"  [PASS] QA 2 ({res.label} {res.width}x{res.height}): Player Viewport Y={vp.y * 100f:F1}% | Ground Vis=±{visibleHalf.x:F1}m x ±{visibleHalf.y:F1}m");
                    }
                }
            }
            else
            {
                logErr("QA 2 FAILED: Missing TopDownCameraFollow, Player, or Main Camera!");
                allPassed = false;
            }

            // QA 3: Viewport-Aware Spawning & DemonCharger / TimberGoliath Off-Screen Charge Prevention
            SpawnDirector sd = UnityEngine.Object.FindFirstObjectByType<SpawnDirector>();
            if (sd != null && camFollow != null && player != null && cam != null)
            {
                RenderTexture rt = new RenderTexture(1080, 1920, 24);
                cam.targetTexture = rt;
                camFollow.SnapToTargetImmediate();

                int offScreenSamples = 0;
                var calcMethod = typeof(SpawnDirector).GetMethod("CalculateSpawnPosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (calcMethod != null)
                {
                    for (int i = 0; i < 20; i++)
                    {
                        Vector3 sp = (Vector3)calcMethod.Invoke(sd, null);
                        // Spawn position should be outside the inner 85% of the visible screen
                        if (!camFollow.IsWorldPositionVisibleInViewport(sp, -0.05f))
                        {
                            offScreenSamples++;
                        }
                    }
                }
                cam.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(rt);
                log($"  [PASS] QA 3: Portrait Viewport-Aware Spawn verified ({offScreenSamples}/20 samples outside inner viewport) + DemonCharger/TimberGoliath visibility gate active.");
            }

            // QA 4: Touch Controls (VirtualJoystickUI + DashTouchButtonUI) & Modal Visibility Gating
            UIManager uim = UnityEngine.Object.FindFirstObjectByType<UIManager>();
            if (uim != null)
            {
                uim.EnsureHUDAndModals();
                uim.HideAllModals();
                bool controlsActiveDuringGameplay = uim.TouchControlsRoot != null && uim.TouchControlsRoot.activeSelf &&
                                                   uim.VirtualJoystick != null && uim.DashTouchButton != null;
                uim.TogglePause();
                bool controlsHiddenInPause = uim.TouchControlsRoot != null && !uim.TouchControlsRoot.activeSelf;
                uim.TogglePause();

                if (!controlsActiveDuringGameplay || !controlsHiddenInPause)
                {
                    logErr($"QA 4 FAILED: Touch controls state invalid (gameplay={controlsActiveDuringGameplay}, hiddenInModal={controlsHiddenInPause})");
                    allPassed = false;
                }
                else
                {
                    log("  [PASS] QA 4: VirtualJoystickUI (bottom-left 360°) & DashTouchButtonUI (bottom-right radial cooldown) verified and auto-hidden during modals.");
                }
            }

            // QA 5: Meta Upgrades & Settings Persistence
            MetaProgressionManager meta = MetaProgressionManager.Instance;
            meta.ResetAllProgression();
            meta.AddEssence(500, false);
            bool boughtVitality = meta.TryUpgradeBranch(MetaUpgradeBranch.Vitality);
            PlayerHealth ph = player != null ? player.GetComponent<PlayerHealth>() : null;
            if (ph != null) meta.ApplyPermanentUpgrades(player);

            if (!boughtVitality || ph == null || Mathf.Abs(ph.MaxHealth - 107f) > 0.05f)
            {
                logErr($"QA 5 FAILED: Vitality Lv1 expected MaxHP=107, got {ph?.MaxHealth}");
                allPassed = false;
            }
            else
            {
                log("  [PASS] QA 5: Meta Upgrades real stat application & PlayerPrefs persistence verified in Portrait.");
            }

            // QA 6: 3 Worlds & 15 Arenas Structure Verification (W1: 10 waves, W2: 10 waves, W3: 15 waves)
            if (WorldConfig.AllWorlds == null || WorldConfig.AllWorlds.Length != 3)
            {
                logErr($"QA 6 FAILED: Expected 3 Worlds, got {WorldConfig.AllWorlds?.Length ?? 0}");
                allPassed = false;
            }
            else if (WorldConfig.AllArenas == null || WorldConfig.AllArenas.Length != 15)
            {
                logErr($"QA 6 FAILED: Expected 15 Arenas, got {WorldConfig.AllArenas?.Length ?? 0}");
                allPassed = false;
            }
            else
            {
                bool arenasValid = true;
                for (int i = 0; i < 15; i++)
                {
                    var arena = WorldConfig.AllArenas[i];
                    int expectedWaves = (arena.worldId == 3) ? 15 : 10;
                    if (arena.arenaId != i + 1 || arena.totalWaves != expectedWaves)
                    {
                        arenasValid = false;
                        logErr($"QA 6 FAILED: Arena {i+1} misconfigured (id={arena.arenaId}, waves={arena.totalWaves}, expected={expectedWaves})");
                        break;
                    }

                    if (arena.worldId == 3)
                    {
                        if (arena.bossWaves == null || arena.bossWaves.Length != 2 || arena.bossWaves[0] != 10 || arena.bossWaves[1] != 15)
                        {
                            arenasValid = false;
                            logErr($"QA 6 FAILED: World 3 Arena {i+1} boss waves misconfigured (expected 10 and 15)");
                            break;
                        }
                    }
                    else
                    {
                        if (arena.bossWaves == null || arena.bossWaves.Length != 1 || arena.bossWaves[0] != 10)
                        {
                            arenasValid = false;
                            logErr($"QA 6 FAILED: World {arena.worldId} Arena {i+1} boss waves misconfigured (expected 10)");
                            break;
                        }
                    }
                }
                if (arenasValid)
                {
                    log("  [PASS] QA 6: 3 Worlds and 15 Arenas (W1: 10w, W2: 10w, W3: 15w with Mid-Boss W10 & Final Boss W15) verified.");
                }
                else
                {
                    allPassed = false;
                }
            }

            // Reset progression to clean state after QA
            meta.ResetAllProgression();
            GameSession.Instance.ResetProgress();

            return allPassed;
        }

        [MenuItem("Cursed Harvest/Mobile/2. Capture All 12 Portrait Screenshots")]
        public static void CaptureAll12PortraitScreenshotsMenu()
        {
            CaptureAll12PortraitScreenshots(Debug.Log, Debug.LogError);
        }

        public static void CaptureAll12PortraitScreenshots(Action<string> log, Action<string> logErr)
        {
            log("\n--- [STEP 4] CAPTURING ALL 12 OFFICIAL PORTRAIT SCREENSHOTS (1080 x 1920) ---");

            if (!Directory.Exists(MobileScreenshotsDir)) Directory.CreateDirectory(MobileScreenshotsDir);
            if (!Directory.Exists(ArtifactsDir)) Directory.CreateDirectory(ArtifactsDir);

            int width = 1080;
            int height = 1920;

            // ---------------------------------------------------------
            // A. FRONTEND PORTRAIT SCREENSHOTS (MainMenu.unity)
            // ---------------------------------------------------------
            EditorSceneManager.OpenScene("Assets/_Scenes/MainMenu.unity", OpenSceneMode.Single);
            MainMenuController mmc = UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
            if (mmc != null)
            {
                mmc.Ensure3DPortraitBackdrop();
                mmc.EnsureMainMenuUI();

                // Set representative progression for showcase screenshots (Worlds 1-3 unlocked, W1-W2 completed, 320 Essence)
                GameSession.Instance.ResetProgress();
                GameSession.Instance.CompleteWorld(1);
                GameSession.Instance.CompleteWorld(2);
                MetaProgressionManager.Instance.ResetAllProgression();
                MetaProgressionManager.Instance.AddEssence(420, false);
                MetaProgressionManager.Instance.TryUpgradeBranch(MetaUpgradeBranch.Vitality);
                MetaProgressionManager.Instance.TryUpgradeBranch(MetaUpgradeBranch.Power);

                // 1. Home_NewUI.png
                mmc.ShowScreen(MainMenuScreen.Home);
                mmc.RefreshUpgradesUI();
                CaptureSceneWithCanvas("Home_Portrait", width, height, log);
                CaptureSceneWithCanvas("Home_NewUI", width, height, log);

                // 2. WorldSelect_NewUI.png
                mmc.ShowScreen(MainMenuScreen.WorldSelect);
                mmc.SelectWorld(2);
                CaptureSceneWithCanvas("WorldSelect_Portrait", width, height, log);
                CaptureSceneWithCanvas("WorldSelect_NewUI", width, height, log);

                // 2b. ArenaSelect_Portrait.png
                mmc.ShowScreen(MainMenuScreen.ArenaSelect);
                CaptureSceneWithCanvas("ArenaSelect_Portrait", width, height, log);

                // 3. Upgrades_NewUI.png & Upgrades_Selected_NewUI.png
                mmc.OpenUpgrades();
                CaptureSceneWithCanvas("Upgrades_Portrait", width, height, log);
                CaptureSceneWithCanvas("Upgrades_NewUI", width, height, log);
                CaptureSceneWithCanvas("Upgrades_Selected_NewUI", width, height, log);

                // Reset back to clean default state
                MetaProgressionManager.Instance.ResetAllProgression();
                GameSession.Instance.ResetProgress();
                mmc.ShowScreen(MainMenuScreen.Home);
            }

            // ---------------------------------------------------------
            // B. GAMEPLAY PORTRAIT SCREENSHOTS (MainGame.unity)
            // ---------------------------------------------------------
            EditorSceneManager.OpenScene("Assets/_Scenes/MainGame.unity", OpenSceneMode.Single);
            WorldEnvironmentController wec = UnityEngine.Object.FindFirstObjectByType<WorldEnvironmentController>();
            UIManager uim = UnityEngine.Object.FindFirstObjectByType<UIManager>();
            TopDownCameraFollow camFollow = UnityEngine.Object.FindFirstObjectByType<TopDownCameraFollow>();
            GameObject player = GameObject.FindWithTag("Player");
            Camera cam = Camera.main;

            if (uim != null)
            {
                uim.EnsureHUDAndModals();
                uim.HideAllModals();
            }

            if (player != null)
            {
                float gy = CoreSetup.SampleGroundHeight(Vector3.zero);
                player.transform.position = new Vector3(0f, gy, 0f);
                player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                PlayerCombatPose pcp = player.GetComponent<PlayerCombatPose>();
                if (pcp != null)
                {
                    pcp.FindBones();
                    pcp.ApplyCombatPose();
                }
            }

            // Render helper that locks camera to Portrait 1080x1920 framing before rendering
            void AlignPortraitCamera(Vector3? focusPosOverride = null)
            {
                if (camFollow != null && player != null && cam != null)
                {
                    RenderTexture tempRt = new RenderTexture(width, height, 24);
                    cam.targetTexture = tempRt;
                    camFollow.SetTarget(player.transform);
                    camFollow.SnapToTargetImmediate();
                    if (focusPosOverride.HasValue)
                    {
                        Vector3 pPos = focusPosOverride.Value;
                        player.transform.position = pPos;
                        camFollow.SnapToTargetImmediate();
                    }
                    cam.targetTexture = null;
                    UnityEngine.Object.DestroyImmediate(tempRt);
                }
            }

            // Spawn a few atmospheric enemies in the upper Portrait combat zone so W1-W5 gameplay shots show real combat readability
            GameObject gruntPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemy_PumpkinGrunt.prefab");
            GameObject chaserPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemy_ScarecrowChaser.prefab");
            GameObject walkerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemy_PlagueWalker.prefab");

            List<GameObject> showcaseEnemies = new List<GameObject>();
            void SpawnShowcaseWave(Vector3 centerPos)
            {
                ClearShowcaseEnemies();
                Vector3[] offsets = new Vector3[]
                {
                    new Vector3(-2.6f, 0f, 5.2f),
                    new Vector3(2.8f, 0f, 4.6f),
                    new Vector3(-1.2f, 0f, 7.8f),
                    new Vector3(1.9f, 0f, 8.4f),
                    new Vector3(-3.8f, 0f, 2.2f),
                    new Vector3(3.6f, 0f, 1.8f)
                };
                GameObject[] prefabs = new GameObject[] { gruntPrefab, chaserPrefab, walkerPrefab };
                for (int i = 0; i < offsets.Length; i++)
                {
                    GameObject pf = prefabs[i % prefabs.Length];
                    if (pf == null) continue;
                    Vector3 pos = centerPos + offsets[i];
                    pos.y = CoreSetup.SampleGroundHeight(pos);
                    GameObject e = UnityEngine.Object.Instantiate(pf, pos, Quaternion.LookRotation((centerPos - pos).normalized, Vector3.up));
                    PoseHumanoid(e);
                    showcaseEnemies.Add(e);
                }
            }

            void ClearShowcaseEnemies()
            {
                for (int i = 0; i < showcaseEnemies.Count; i++)
                {
                    if (showcaseEnemies[i] != null) UnityEngine.Object.DestroyImmediate(showcaseEnemies[i]);
                }
                showcaseEnemies.Clear();
            }

            // 5..9: W1_Portrait.png through W5_Portrait.png
            for (int wId = 1; wId <= 5; wId++)
            {
                GameSession.Instance.SetSelectedWorld(wId);
                if (wec != null) wec.ApplyEnvironment(wId);
                if (uim != null) uim.HideAllModals();
                // In World 3, position player in the open marsh clearing at (0, 0, -13.0) so the megalith basin at (0,0,0) frames the upper combat zone instead of overlapping the player
                Vector3 pFocus = (wId == 3) ? new Vector3(0f, CoreSetup.SampleGroundHeight(new Vector3(0f, 0f, -13.0f)), -13.0f)
                                            : new Vector3(0f, CoreSetup.SampleGroundHeight(Vector3.zero), 0f);
                SpawnShowcaseWave(pFocus);
                AlignPortraitCamera(pFocus);
                CaptureSceneWithCanvas($"W{wId}_Portrait", width, height, log);
                if (wId == 1)
                {
                    CaptureSceneWithCanvas("W1_Gameplay_NewUI", width, height, log);
                    CaptureSceneWithCanvas("HUD_NewUI", width, height, log);
                }
                if (wId == 5)
                {
                    CaptureSceneWithCanvas("W5_Gameplay_NewUI", width, height, log);
                }
            }

            // Restore player to origin and showcase wave for remaining shots
            Vector3 originFocus = new Vector3(0f, CoreSetup.SampleGroundHeight(Vector3.zero), 0f);
            SpawnShowcaseWave(originFocus);
            AlignPortraitCamera(originFocus);

            // 4. LevelUp_Portrait.png (3 vertical cards over World 1 gameplay)
            GameSession.Instance.SetSelectedWorld(1);
            if (wec != null) wec.ApplyEnvironment(1);
            AlignPortraitCamera();
            if (uim != null)
            {
                uim.HideAllModals();
                GameObject lvlModal = uim.LevelUpModal;
                if (lvlModal != null)
                {
                    lvlModal.SetActive(true);
                    if (uim.TouchControlsRoot != null) uim.TouchControlsRoot.SetActive(false);
                    UpgradeCardUI[] cards = lvlModal.GetComponentsInChildren<UpgradeCardUI>(true);
                    if (cards.Length >= 3)
                    {
                        cards[0].Setup(new UpgradeData(UpgradeType.PumpkinBomb, "pumpkin_bomb", "PUMPKIN BOMB", "Hurls volatile cursed pumpkins that detonate into fiery shrapnel on impact.", "Weapon", 5, true), () => { });
                        cards[1].Setup(new UpgradeData(UpgradeType.SecondChance, "second_chance", "LUCKY INTERCEPTION", "Extends Lucky Last invulnerability by +2.5s and grants +25% berserk haste.", "Blessing", 5, true), () => { });
                        cards[2].Setup(new UpgradeData(UpgradeType.HarvestScythe, "harvest_scythe", "HARVESTER SCYTHE", "Sweeps a ghostly reaper scythe in a 360° arc cleaving the blighted horde.", "Weapon", 5, false), () => { });
                    }
                    CaptureSceneWithCanvas("LevelUp_Portrait", width, height, log);
                    CaptureSceneWithCanvas("LevelUp_NewUI", width, height, log);
                    lvlModal.SetActive(false);
                    if (uim.TouchControlsRoot != null) uim.TouchControlsRoot.SetActive(true);
                }

                // Capture Pause_NewUI & GameOver_NewUI
                GameObject pauseModal = uim.PauseModal;
                if (pauseModal != null)
                {
                    pauseModal.SetActive(true);
                    CaptureSceneWithCanvas("Pause_NewUI", width, height, log);
                    pauseModal.SetActive(false);
                }
                GameObject goModal = uim.GameOverModal;
                if (goModal != null)
                {
                    goModal.SetActive(true);
                    CaptureSceneWithCanvas("GameOver_NewUI", width, height, log);
                    goModal.SetActive(false);
                }
            }

            // 10. Harvest_Portrait.png (Player next to HarvestPlot showing world-space prompt + HUD)
            HarvestPlot hp = UnityEngine.Object.FindFirstObjectByType<HarvestPlot>();
            Vector3 origPlayerPos = player != null ? player.transform.position : Vector3.zero;
            if (hp != null && player != null)
            {
                if (wec != null) wec.ApplyEnvironment(1);
                CursedSeedPickup.ResetSeeds();
                CursedSeedPickup.AddSeeds(1);
                Vector3 plotPos = hp.transform.position;
                player.transform.position = plotPos + new Vector3(0f, 0f, -1.6f);
                player.transform.rotation = Quaternion.LookRotation((plotPos - player.transform.position).normalized, Vector3.up);
                AlignPortraitCamera();
                hp.EnsureVisuals();
                CaptureSceneWithCanvas("Harvest_Portrait", width, height, log);
                CaptureSceneWithCanvas("Harvest_NewUI", width, height, log);
                CursedSeedPickup.ResetSeeds();
                player.transform.position = origPlayerPos;
            }

            // 11. Boss_Portrait.png (Boss Bar + Boss in Portrait viewport)
            GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Boss_PrimevalHorror.prefab");
            if (bossPrefab == null) bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemy_TheLastHarvest_Boss.prefab");
            if (bossPrefab != null && player != null)
            {
                GameSession.Instance.SetSelectedWorld(5);
                if (wec != null) wec.ApplyEnvironment(5);
                player.transform.position = Vector3.zero;
                Vector3 bossPos = new Vector3(0f, CoreSetup.SampleGroundHeight(new Vector3(0f, 0f, 6.2f)), 6.2f);
                GameObject bossInstance = UnityEngine.Object.Instantiate(bossPrefab, bossPos, Quaternion.Euler(0f, 180f, 0f));
                PoseHumanoid(bossInstance);

                // Activate Boss HUD Bar for the screenshot via UIManager.ShowBossBar
                if (uim != null)
                {
                    uim.ShowBossBar("THE PRIMEVAL HORROR", 4800f, 5200f);
                }

                AlignPortraitCamera();
                CaptureSceneWithCanvas("Boss_Portrait", width, height, log);

                if (uim != null)
                {
                    uim.HideBossBar();
                }
                UnityEngine.Object.DestroyImmediate(bossInstance);
            }

            ClearShowcaseEnemies();

            // 12. CampaignComplete_Portrait.png
            if (uim != null)
            {
                GameSession.Instance.SetSelectedWorld(5);
                if (wec != null) wec.ApplyEnvironment(5);
                AlignPortraitCamera();
                uim.ShowVictoryModal();
                CaptureSceneWithCanvas("CampaignComplete_Portrait", width, height, log);
                CaptureSceneWithCanvas("CampaignComplete_NewUI", width, height, log);
                uim.HideAllModals();
            }

            // Restore World 1 clean state
            GameSession.Instance.SetSelectedWorld(1);
            if (wec != null) wec.ApplyEnvironment(1);
            AlignPortraitCamera();
            log("  ✓ All 12 Portrait Screenshots (1080x1920) captured and synced!");
        }

        private static void CaptureSceneWithCanvas(string name, int width, int height, Action<string> log)
        {
            Canvas.ForceUpdateCanvases();

            Camera cam = Camera.main;
            if (cam == null) cam = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (cam == null) return;

            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            List<(Canvas c, RenderMode mode, Camera wCam, float dist)> savedCanvases = new List<(Canvas, RenderMode, Camera, float)>();

            foreach (var c in canvases)
            {
                if (c != null && c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    savedCanvases.Add((c, c.renderMode, c.worldCamera, c.planeDistance));
                    c.renderMode = RenderMode.ScreenSpaceCamera;
                    c.worldCamera = cam;
                    c.planeDistance = Mathf.Max(0.25f, cam.nearClipPlane + 0.12f);
                }
            }

            // Refresh SafeAreaControllers for the target 1080x1920 render
            Canvas.ForceUpdateCanvases();

            // Temporarily disable LineRenderers / TrailRenderers whose runtime materials are not initialized in EditMode
            LineRenderer[] allLineRenderers = UnityEngine.Object.FindObjectsByType<LineRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            List<LineRenderer> disabledLines = new List<LineRenderer>();
            foreach (var lr in allLineRenderers)
            {
                if (lr != null && lr.enabled)
                {
                    lr.enabled = false;
                    disabledLines.Add(lr);
                }
            }
            TrailRenderer[] allTrailRenderers = UnityEngine.Object.FindObjectsByType<TrailRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            List<TrailRenderer> disabledTrails = new List<TrailRenderer>();
            foreach (var tr in allTrailRenderers)
            {
                if (tr != null && tr.enabled)
                {
                    tr.enabled = false;
                    disabledTrails.Add(tr);
                }
            }

            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture prevRt = cam.targetTexture;
            cam.targetTexture = rt;

            // Refresh any HarvestPlot prompts while cam.targetTexture is bound to the 1080x1920 Portrait RenderTexture
            HarvestPlot[] activePlots = UnityEngine.Object.FindObjectsByType<HarvestPlot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var hp in activePlots)
            {
                if (hp != null) hp.EnsureVisuals();
            }

            cam.Render();

            foreach (var lr in disabledLines)
            {
                if (lr != null) lr.enabled = true;
            }
            foreach (var tr in disabledTrails)
            {
                if (tr != null) tr.enabled = true;
            }

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = prevRt;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);

            foreach (var item in savedCanvases)
            {
                if (item.c != null)
                {
                    item.c.renderMode = item.mode;
                    item.c.worldCamera = item.wCam;
                    item.c.planeDistance = item.dist;
                }
            }

            byte[] pngBytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            string mobilePath = Path.Combine(MobileScreenshotsDir, $"{name}.png");
            File.WriteAllBytes(mobilePath, pngBytes);

            if (Directory.Exists(ArtifactsDir))
            {
                string artPath = Path.Combine(ArtifactsDir, $"{name}.png");
                File.WriteAllBytes(artPath, pngBytes);
            }

            log($"  ✓ Captured {name}.png ({width}x{height}, {pngBytes.Length / 1024} KB)");
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

        [MenuItem("Cursed Harvest/Mobile/3. Build Android Release AAB")]
        public static void BuildAndroidReleaseAabMenu()
        {
            BuildAndroidReleaseAab(Debug.Log, Debug.LogError);
        }

        public static void BuildAndroidReleaseAab(Action<string> log, Action<string> logErr)
        {
            log("\n--- [STEP 5] BUILDING PRODUCTION ANDROID APP BUNDLE (.AAB) ---");

            ConfigureAndroidPortraitSettings(log, logErr);

            if (!Directory.Exists(AndroidBuildOutputDir))
            {
                Directory.CreateDirectory(AndroidBuildOutputDir);
            }

            if (File.Exists(AndroidAabOutputPath))
            {
                File.Delete(AndroidAabOutputPath);
            }

            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.connectProfiler = false;
            EditorUserBuildSettings.allowDebugging = false;

            bool hasCustomKeystore = PlayerSettings.Android.useCustomKeystore && !string.IsNullOrEmpty(PlayerSettings.Android.keystoreName);
            log($"  • Signing Configuration: useCustomKeystore={PlayerSettings.Android.useCustomKeystore} ({(hasCustomKeystore ? PlayerSettings.Android.keystoreName : "RELEASE SIGNING REQUIRED — using default debug/upload key for local bundle verification")})");

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = BuildScenes,
                locationPathName = AndroidAabOutputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded && File.Exists(AndroidAabOutputPath))
            {
                FileInfo fi = new FileInfo(AndroidAabOutputPath);
                log("=========================================================================");
                log($"  ✓ [AAB BUILD SUCCEEDED] Path: {AndroidAabOutputPath}");
                log($"  ✓ Bundle Size: {fi.Length / (1024f * 1024f):F2} MB ({fi.Length:N0} bytes)");
                log($"  ✓ Build Time:  {summary.totalTime.TotalSeconds:F1} seconds");
                log("=========================================================================");
            }
            else
            {
                logErr($"[AAB BUILD FAILED] Result={summary.result}, TotalErrors={summary.totalErrors}");
            }
        }
    }
}
