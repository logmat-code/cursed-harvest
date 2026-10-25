using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CursedHarvest
{
    /// <summary>
    /// Master Mobile Portrait Frontend controller for CURSED HARVEST. v1.1.0.
    /// Manages HOME, WORLD SELECT, ARENA SELECT, UPGRADES, SETTINGS, and CREDITS screens.
    /// Hierarchy flow: HOME -> WORLD SELECT (Worlds 1..3) -> ARENA SELECT (Arenas 1..5) -> START RUN.
    /// Coherent folk-horror aesthetic palette (#121510 background, #1B211A dark panels, #D59A38 harvest gold, #74884B cursed green).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        private static MainMenuController _instance;
        public static MainMenuController Instance => _instance;

        // Palette Constants
        public static readonly Color ColBackground = new Color(0.07f, 0.08f, 0.06f, 0.98f);    // #121510
        public static readonly Color ColDarkPanel = new Color(0.11f, 0.13f, 0.10f, 0.96f);     // #1B211A
        public static readonly Color ColElevated = new Color(0.15f, 0.17f, 0.13f, 0.98f);      // #252C22
        public static readonly Color ColTextPrimary = new Color(0.94f, 0.91f, 0.84f, 1.0f);    // #EFE7D5
        public static readonly Color ColTextMuted = new Color(0.69f, 0.65f, 0.58f, 1.0f);      // #AFA795
        public static readonly Color ColHarvestGold = new Color(0.84f, 0.60f, 0.22f, 1.0f);    // #D59A38
        public static readonly Color ColCursedGreen = new Color(0.45f, 0.53f, 0.29f, 1.0f);    // #74884B
        public static readonly Color ColDanger = new Color(0.64f, 0.28f, 0.24f, 1.0f);         // #A4473D

        [Header("Screen Roots")]
        [SerializeField] private GameObject homeScreenRoot;
        [SerializeField] private GameObject worldSelectScreenRoot;
        [SerializeField] private GameObject arenaSelectScreenRoot;
        [SerializeField] private GameObject upgradesScreenRoot;
        [SerializeField] private GameObject creditsScreenRoot;
        [SerializeField] private GameObject settingsModalRoot;
        [SerializeField] private GameObject exitConfirmModalRoot;

        [Header("Home Screen UI")]
        [SerializeField] private Button homePlayButton;
        [SerializeField] private Button homeUpgradesButton;
        [SerializeField] private Button homeSettingsButton;
        [SerializeField] private Button homeCreditsButton;
        [SerializeField] private Button homeQuitButton;
        [SerializeField] private Text homeEssenceBadgeText;

        [Header("World Select UI (3 Worlds)")]
        [SerializeField] private Button worldSelectBackButton;
        [SerializeField] private List<WorldCardUIElements> worldCards = new List<WorldCardUIElements>();

        [Header("Arena Select UI (5 Arenas per World)")]
        [SerializeField] private Button arenaSelectBackButton;
        [SerializeField] private Text arenaSelectTitleText;
        [SerializeField] private Text arenaSelectSubtitleText;
        [SerializeField] private List<ArenaCardUIElements> arenaCards = new List<ArenaCardUIElements>();
        [SerializeField] private Button startRunButton;
        [SerializeField] private Text startRunButtonText;
        [SerializeField] private Text selectedArenaDescText;
        [SerializeField] private Text selectedArenaBossSummaryText;

        [Header("Upgrades UI")]
        [SerializeField] private Button upgradesBackButton;
        [SerializeField] private Text essenceBalanceText;
        [SerializeField] private List<MetaUpgradeCardUIElements> upgradeBranchCards = new List<MetaUpgradeCardUIElements>();

        [Serializable]
        public class MetaUpgradeCardUIElements
        {
            public MetaUpgradeBranch branch;
            public Text titleText;
            public Text rankText;
            public Text descText;
            public Button upgradeButton;
            public Text buttonText;
        }

        [Header("Settings Modal UI")]
        [SerializeField] private Button settingsMusicToggleButton;
        [SerializeField] private Text settingsMusicToggleText;
        [SerializeField] private Button settingsSfxToggleButton;
        [SerializeField] private Text settingsSfxToggleText;
        [SerializeField] private Button settingsHapticsToggleButton;
        [SerializeField] private Text settingsHapticsToggleText;
        [SerializeField] private Button settingsCloseButton;

        [Header("Exit Confirmation UI")]
        [SerializeField] private Button exitConfirmYesButton;
        [SerializeField] private Button exitConfirmCancelButton;

        [Header("Credits UI")]
        [SerializeField] private Button creditsBackButton;

        [Serializable]
        public class WorldCardUIElements
        {
            public int worldId;
            public GameObject cardRoot;
            public Button selectButton;
            public Text worldNumberText;
            public Text worldNameText;
            public Text difficultyBadgeText;
            public Text arenaSummaryText;
            public Text statusBadgeText;
            public Image highlightBorder;
            public Image background;
            public Image previewThumbnail;
        }

        [Serializable]
        public class ArenaCardUIElements
        {
            public int globalArenaId;
            public int arenaIndexInWorld;
            public GameObject cardRoot;
            public Button selectButton;
            public Text arenaNumberText;
            public Text arenaNameText;
            public Text difficultyBadgeText;
            public Text statusBadgeText;
            public Image highlightBorder;
            public Image background;
        }

        private int currentlySelectedWorldId = 1;
        private int currentlySelectedArenaId = 1;
        private Font cachedFont;
        private float lastUpgradeClickTime = -1f;
        private GameObject backdropDioramaRoot;
        private Transform floatingAltarPumpkin;
        private Light altarPointLight;
        private static readonly Dictionary<int, Sprite> _worldThumbnailCache = new Dictionary<int, Sprite>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            Time.timeScale = 1f;

            var _ = MobileSettingsAndLifecycleManager.Instance;
            EnsureAudioSetup();
            EnsureFont();
        }

        private void Start()
        {
            EnsureAudioSetup();
            EnsureFont();
            Ensure3DPortraitBackdrop();
            EnsureMainMenuUI();

            MainMenuScreen startScreen = MainMenuScreen.Home;
            if (GameSession.Instance != null)
            {
                startScreen = GameSession.Instance.InitialMenuScreen;
                currentlySelectedWorldId = GameSession.Instance.SelectedWorldId;
                currentlySelectedArenaId = GameSession.Instance.SelectedArenaId;
            }

            ShowScreen(startScreen);
        }

        private void EnsureFont()
        {
            if (cachedFont == null)
            {
                cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (cachedFont == null) cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

        private void EnsureAudioSetup()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayGameplayMusic(1.0f);
            }
        }

        public void Ensure3DPortraitBackdrop()
        {
            if (backdropDioramaRoot != null) return;
            Transform existing = transform.Find("3D_Portrait_Altar_Backdrop");
            if (existing != null)
            {
                backdropDioramaRoot = existing.gameObject;
                floatingAltarPumpkin = backdropDioramaRoot.transform.Find("FloatingPumpkinCenterpiece");
                Transform lightTr = backdropDioramaRoot.transform.Find("AltarPointLight");
                if (lightTr != null) altarPointLight = lightTr.GetComponent<Light>();
                return;
            }

            backdropDioramaRoot = new GameObject("3D_Portrait_Altar_Backdrop");
            backdropDioramaRoot.transform.SetParent(transform, false);

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material bloodwoodMat = new Material(litShader);
            bloodwoodMat.color = new Color(0.18f, 0.08f, 0.06f);

            Material stoneMat = new Material(litShader);
            stoneMat.color = new Color(0.24f, 0.22f, 0.20f);

            Material goldRuneMat = new Material(Shader.Find("Sprites/Default") ?? litShader);
            goldRuneMat.color = new Color(1.0f, 0.76f, 0.20f, 0.95f);

            GameObject groundObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundObj.name = "AltarGroundMound";
            groundObj.transform.SetParent(backdropDioramaRoot.transform, false);
            groundObj.transform.localPosition = new Vector3(0f, -0.6f, 5.5f);
            groundObj.transform.localScale = new Vector3(1.4f, 1f, 1.4f);
            Renderer gRend = groundObj.GetComponent<Renderer>();
            if (gRend != null) gRend.material = stoneMat;
            DestroyCollider(groundObj);

            GameObject baseBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseBlock.name = "AltarStoneBase";
            baseBlock.transform.SetParent(backdropDioramaRoot.transform, false);
            baseBlock.transform.localPosition = new Vector3(0f, 0.15f, 5.2f);
            baseBlock.transform.localScale = new Vector3(1.8f, 0.45f, 1.4f);
            Renderer bRend = baseBlock.GetComponent<Renderer>();
            if (bRend != null) bRend.material = stoneMat;
            DestroyCollider(baseBlock);

            GameObject pumpkinObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pumpkinObj.name = "FloatingPumpkinCenterpiece";
            pumpkinObj.transform.SetParent(backdropDioramaRoot.transform, false);
            pumpkinObj.transform.localPosition = new Vector3(0f, 1.38f, 5.0f);
            pumpkinObj.transform.localScale = new Vector3(1.35f, 1.15f, 1.35f);
            Renderer pRend = pumpkinObj.GetComponent<Renderer>();
            if (pRend != null)
            {
                Material pMat = new Material(litShader);
                pMat.color = new Color(0.92f, 0.44f, 0.08f);
                pRend.material = pMat;
            }
            DestroyCollider(pumpkinObj);
            floatingAltarPumpkin = pumpkinObj.transform;

            GameObject lightObj = new GameObject("AltarPointLight");
            lightObj.transform.SetParent(backdropDioramaRoot.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 1.55f, 4.6f);
            altarPointLight = lightObj.AddComponent<Light>();
            altarPointLight.type = LightType.Point;
            altarPointLight.color = new Color(1.0f, 0.65f, 0.18f);
            altarPointLight.range = 7.5f;
            altarPointLight.intensity = 3.8f;
        }

        private void DestroyCollider(GameObject go)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }
        }

        private void Update()
        {
            if (floatingAltarPumpkin != null)
            {
                float t = Time.unscaledTime;
                floatingAltarPumpkin.localPosition = new Vector3(0f, 1.38f + Mathf.Sin(t * 1.8f) * 0.08f, 5.0f);
                floatingAltarPumpkin.localRotation = Quaternion.Euler(6f * Mathf.Sin(t * 0.9f), 180f + Mathf.Sin(t * 0.65f) * 11f, 0f);
            }
            if (altarPointLight != null)
            {
                altarPointLight.intensity = 3.8f + Mathf.Sin(Time.unscaledTime * 3.1f) * 0.45f;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleBackNavigation();
            }
        }

        public void HandleBackNavigation()
        {
            if (settingsModalRoot != null && settingsModalRoot.activeSelf)
            {
                CloseSettingsModal();
                return;
            }
            if (exitConfirmModalRoot != null && exitConfirmModalRoot.activeSelf)
            {
                CloseExitConfirmation();
                return;
            }
            if (arenaSelectScreenRoot != null && arenaSelectScreenRoot.activeSelf)
            {
                OpenWorldSelect();
                return;
            }
            if (worldSelectScreenRoot != null && worldSelectScreenRoot.activeSelf)
            {
                OpenHome();
                return;
            }
            if (upgradesScreenRoot != null && upgradesScreenRoot.activeSelf)
            {
                OpenHome();
                return;
            }
            if (creditsScreenRoot != null && creditsScreenRoot.activeSelf)
            {
                OpenHome();
                return;
            }

            OpenExitConfirmation();
        }

        // ==================== SCREEN NAVIGATION ====================

        public void ShowScreen(MainMenuScreen screen)
        {
            if (homeScreenRoot != null) homeScreenRoot.SetActive(screen == MainMenuScreen.Home);
            if (worldSelectScreenRoot != null) worldSelectScreenRoot.SetActive(screen == MainMenuScreen.WorldSelect);
            if (arenaSelectScreenRoot != null) arenaSelectScreenRoot.SetActive(screen == MainMenuScreen.ArenaSelect);
            if (upgradesScreenRoot != null) upgradesScreenRoot.SetActive(screen == MainMenuScreen.Upgrades);
            if (creditsScreenRoot != null) creditsScreenRoot.SetActive(screen == MainMenuScreen.Credits);
            if (settingsModalRoot != null) settingsModalRoot.SetActive(false);
            if (exitConfirmModalRoot != null) exitConfirmModalRoot.SetActive(false);

            if (backdropDioramaRoot != null)
            {
                backdropDioramaRoot.SetActive(screen == MainMenuScreen.Home);
            }

            if (screen == MainMenuScreen.WorldSelect)
            {
                RefreshWorldSelectionCards();
            }
            else if (screen == MainMenuScreen.ArenaSelect)
            {
                RefreshArenaSelectionCards();
            }
            else if (screen == MainMenuScreen.Upgrades || screen == MainMenuScreen.Home)
            {
                RefreshUpgradesUI();
            }
        }

        public void OpenHome()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            MobileSettingsAndLifecycleManager.Instance?.TriggerHaptic(HapticPattern.LightTap);
            ShowScreen(MainMenuScreen.Home);
        }

        public void OpenWorldSelect()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            MobileSettingsAndLifecycleManager.Instance?.TriggerHaptic(HapticPattern.LightTap);
            ShowScreen(MainMenuScreen.WorldSelect);
        }

        public void OpenArenaSelect(int worldId)
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            MobileSettingsAndLifecycleManager.Instance?.TriggerHaptic(HapticPattern.LightTap);
            currentlySelectedWorldId = Mathf.Clamp(worldId, 1, 3);
            currentlySelectedArenaId = (currentlySelectedWorldId - 1) * 5 + 1;
            ShowScreen(MainMenuScreen.ArenaSelect);
        }

        public void OpenUpgrades()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            MobileSettingsAndLifecycleManager.Instance?.TriggerHaptic(HapticPattern.LightTap);
            EnsureUpgradesScreenGrid();
            RefreshUpgradesUI();
            ShowScreen(MainMenuScreen.Upgrades);
        }

        public void EnsureUpgradesScreenGrid()
        {
            EnsureFont();
            if (upgradesScreenRoot == null) return;
            if (upgradeBranchCards == null || upgradeBranchCards.Count < 8)
            {
                BuildUpgradesScreenContent(upgradesScreenRoot.transform);
                if (upgradesBackButton != null)
                {
                    upgradesBackButton.onClick.RemoveAllListeners();
                    upgradesBackButton.onClick.AddListener(OpenHome);
                }
            }
        }

        public void OpenCredits()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            MobileSettingsAndLifecycleManager.Instance?.TriggerHaptic(HapticPattern.LightTap);
            ShowScreen(MainMenuScreen.Credits);
        }

        public void OpenSettingsModal()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            MobileSettingsAndLifecycleManager.Instance?.TriggerHaptic(HapticPattern.LightTap);
            RefreshSettingsModalLabels();
            if (settingsModalRoot != null) settingsModalRoot.SetActive(true);
        }

        public void CloseSettingsModal()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            MobileSettingsAndLifecycleManager.Instance?.TriggerHaptic(HapticPattern.LightTap);
            if (settingsModalRoot != null) settingsModalRoot.SetActive(false);
        }

        public void ToggleMusicSetting()
        {
            if (MobileSettingsAndLifecycleManager.Instance != null)
            {
                MobileSettingsAndLifecycleManager.Instance.MusicEnabled = !MobileSettingsAndLifecycleManager.Instance.MusicEnabled;
            }
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            RefreshSettingsModalLabels();
        }

        public void ToggleSfxSetting()
        {
            if (MobileSettingsAndLifecycleManager.Instance != null)
            {
                MobileSettingsAndLifecycleManager.Instance.SfxEnabled = !MobileSettingsAndLifecycleManager.Instance.SfxEnabled;
            }
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            RefreshSettingsModalLabels();
        }

        public void ToggleHapticsSetting()
        {
            if (MobileSettingsAndLifecycleManager.Instance != null)
            {
                MobileSettingsAndLifecycleManager.Instance.HapticsEnabled = !MobileSettingsAndLifecycleManager.Instance.HapticsEnabled;
                if (MobileSettingsAndLifecycleManager.Instance.HapticsEnabled)
                {
                    MobileSettingsAndLifecycleManager.Instance.TriggerHaptic(HapticPattern.LightTap);
                }
            }
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            RefreshSettingsModalLabels();
        }

        private void RefreshSettingsModalLabels()
        {
            bool musicOn = MobileSettingsAndLifecycleManager.Instance == null || MobileSettingsAndLifecycleManager.Instance.MusicEnabled;
            bool sfxOn = MobileSettingsAndLifecycleManager.Instance == null || MobileSettingsAndLifecycleManager.Instance.SfxEnabled;
            bool hapticsOn = MobileSettingsAndLifecycleManager.Instance == null || MobileSettingsAndLifecycleManager.Instance.HapticsEnabled;

            if (settingsMusicToggleText != null)
            {
                settingsMusicToggleText.text = musicOn ? "MUSIC: ON" : "MUSIC: OFF";
                settingsMusicToggleText.color = musicOn ? ColCursedGreen : ColDanger;
            }
            if (settingsSfxToggleText != null)
            {
                settingsSfxToggleText.text = sfxOn ? "SFX: ON" : "SFX: OFF";
                settingsSfxToggleText.color = sfxOn ? ColCursedGreen : ColDanger;
            }
            if (settingsHapticsToggleText != null)
            {
                settingsHapticsToggleText.text = hapticsOn ? "HAPTICS: ON" : "HAPTICS: OFF";
                settingsHapticsToggleText.color = hapticsOn ? ColCursedGreen : ColDanger;
            }
        }

        public void OpenExitConfirmation()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            if (exitConfirmModalRoot != null) exitConfirmModalRoot.SetActive(true);
        }

        public void CloseExitConfirmation()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            if (exitConfirmModalRoot != null) exitConfirmModalRoot.SetActive(false);
        }

        public void QuitGame()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ==================== WORLD SELECTION ====================

        public void SelectWorld(int worldId)
        {
            currentlySelectedWorldId = Mathf.Clamp(worldId, 1, 3);
            bool isUnlocked = GameSession.Instance == null || GameSession.Instance.IsWorldUnlocked(currentlySelectedWorldId);
            if (isUnlocked)
            {
                OpenArenaSelect(currentlySelectedWorldId);
            }
            else
            {
                AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
                RefreshWorldSelectionCards();
            }
        }

        private void RefreshWorldSelectionCards()
        {
            if (worldCards == null) return;

            for (int i = 0; i < worldCards.Count; i++)
            {
                var card = worldCards[i];
                if (card == null || card.cardRoot == null) continue;

                int wId = card.worldId;
                WorldConfig config = WorldConfig.GetConfig(wId);
                bool isUnlocked = GameSession.Instance == null || GameSession.Instance.IsWorldUnlocked(wId);
                bool isCompleted = GameSession.Instance != null && GameSession.Instance.IsWorldCompleted(wId);

                if (card.worldNumberText != null) card.worldNumberText.text = $"WORLD {wId}";
                if (card.worldNameText != null) card.worldNameText.text = config != null ? config.displayName : $"WORLD {wId}";
                if (card.difficultyBadgeText != null)
                {
                    card.difficultyBadgeText.text = config != null ? config.difficultyTier : "NORMAL";
                    card.difficultyBadgeText.color = GetTierColor(config != null ? config.difficultyTier : "");
                }
                if (card.arenaSummaryText != null) card.arenaSummaryText.text = "5 Unique Arenas  •  Wave 5 Boss Encounter";

                if (card.statusBadgeText != null)
                {
                    if (isCompleted)
                    {
                        card.statusBadgeText.text = "★ WORLD COMPLETED";
                        card.statusBadgeText.color = ColCursedGreen;
                    }
                    else if (isUnlocked)
                    {
                        card.statusBadgeText.text = "★ UNLOCKED — TAP TO CHOOSE ARENA";
                        card.statusBadgeText.color = ColHarvestGold;
                    }
                    else
                    {
                        card.statusBadgeText.text = $"LOCKED (Complete World {wId - 1})";
                        card.statusBadgeText.color = ColDanger;
                    }
                }

                if (card.previewThumbnail != null)
                {
                    card.previewThumbnail.color = isUnlocked ? Color.white : new Color(0.42f, 0.42f, 0.45f, 0.85f);
                }

                if (card.highlightBorder != null)
                {
                    card.highlightBorder.color = (wId == currentlySelectedWorldId) ? ColHarvestGold : (isUnlocked ? new Color(0.42f, 0.36f, 0.26f, 0.55f) : new Color(0.22f, 0.20f, 0.22f, 0.4f));
                }

                if (card.background != null)
                {
                    card.background.color = isUnlocked ? ColDarkPanel : new Color(0.06f, 0.05f, 0.07f, 0.90f);
                }
            }
        }

        // ==================== ARENA SELECTION ====================

        public void SelectArena(int globalArenaId)
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            MobileSettingsAndLifecycleManager.Instance?.TriggerHaptic(HapticPattern.LightTap);
            currentlySelectedArenaId = Mathf.Clamp(globalArenaId, 1, 15);
            RefreshArenaSelectionCards();
        }

        public void StartSelectedArena()
        {
            bool isUnlocked = GameSession.Instance == null || GameSession.Instance.IsArenaUnlocked(currentlySelectedArenaId);
            if (!isUnlocked)
            {
                Debug.LogWarning($"[MainMenuController] Cannot start Arena {currentlySelectedArenaId}: Locked!");
                return;
            }

            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            MobileSettingsAndLifecycleManager.Instance?.TriggerHaptic(HapticPattern.LevelUp);
            Debug.Log($"[MainMenuController] Launching Arena {currentlySelectedArenaId}...");

            if (GameSession.Instance != null)
            {
                GameSession.Instance.StartRun(currentlySelectedArenaId);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainGame");
            }
        }

        private void RefreshArenaSelectionCards()
        {
            WorldConfig worldCfg = WorldConfig.GetConfig(currentlySelectedWorldId);
            if (arenaSelectTitleText != null && worldCfg != null)
            {
                arenaSelectTitleText.text = $"{worldCfg.displayName}";
                arenaSelectTitleText.color = worldCfg.themeColor;
            }
            if (arenaSelectSubtitleText != null && worldCfg != null)
            {
                arenaSelectSubtitleText.text = $"WORLD {currentlySelectedWorldId} ARENAS  •  CHOOSE YOUR BATTLEGROUND";
            }

            List<ArenaConfig> arenas = WorldConfig.GetArenasForWorld(currentlySelectedWorldId);
            for (int i = 0; i < arenaCards.Count && i < arenas.Count; i++)
            {
                var card = arenaCards[i];
                var arena = arenas[i];
                if (card == null || card.cardRoot == null) continue;

                card.globalArenaId = arena.arenaId;
                card.arenaIndexInWorld = arena.arenaIndexInWorld;

                bool isUnlocked = GameSession.Instance == null || GameSession.Instance.IsArenaUnlocked(arena.arenaId);
                bool isCompleted = GameSession.Instance != null && GameSession.Instance.IsArenaCompleted(arena.arenaId);
                bool isSelected = (arena.arenaId == currentlySelectedArenaId);

                if (card.arenaNumberText != null) card.arenaNumberText.text = $"ARENA {arena.worldId}-{arena.arenaIndexInWorld}";
                if (card.arenaNameText != null) card.arenaNameText.text = arena.arenaName;
                if (card.difficultyBadgeText != null)
                {
                    card.difficultyBadgeText.text = arena.difficultyTier;
                    card.difficultyBadgeText.color = GetTierColor(arena.difficultyTier);
                }

                if (card.statusBadgeText != null)
                {
                    if (isCompleted)
                    {
                        card.statusBadgeText.text = "★ COMPLETED";
                        card.statusBadgeText.color = ColCursedGreen;
                    }
                    else if (isUnlocked)
                    {
                        card.statusBadgeText.text = "UNLOCKED";
                        card.statusBadgeText.color = ColHarvestGold;
                    }
                    else
                    {
                        card.statusBadgeText.text = $"LOCKED";
                        card.statusBadgeText.color = ColDanger;
                    }
                }

                if (card.highlightBorder != null)
                {
                    card.highlightBorder.color = isSelected ? ColHarvestGold : (isUnlocked ? new Color(0.35f, 0.42f, 0.28f, 0.6f) : new Color(0.2f, 0.2f, 0.2f, 0.4f));
                }

                if (card.background != null)
                {
                    card.background.color = isSelected ? ColElevated : (isUnlocked ? ColDarkPanel : new Color(0.06f, 0.05f, 0.07f, 0.90f));
                }
            }

            // Update details panel
            ArenaConfig selectedArena = WorldConfig.GetArenaConfig(currentlySelectedArenaId);
            bool arenaUnlocked = GameSession.Instance == null || GameSession.Instance.IsArenaUnlocked(currentlySelectedArenaId);
            if (selectedArenaDescText != null && selectedArena != null)
            {
                selectedArenaDescText.text = selectedArena.description;
            }
            if (selectedArenaBossSummaryText != null && selectedArena != null && selectedArena.bossEncounters != null && selectedArena.bossEncounters.Length > 0)
            {
                selectedArenaBossSummaryText.text = $"WAVE 5 BOSS: {selectedArena.bossEncounters[0].bossName} ({selectedArena.bossEncounters[0].bossTitle})";
            }
            if (startRunButton != null)
            {
                startRunButton.interactable = arenaUnlocked;
            }
            if (startRunButtonText != null)
            {
                startRunButtonText.text = arenaUnlocked ? $"ENTER ARENA {selectedArena.worldId}-{selectedArena.arenaIndexInWorld}" : "ARENA LOCKED";
                startRunButtonText.color = arenaUnlocked ? ColHarvestGold : ColTextMuted;
            }
        }

        private Color GetTierColor(string tier)
        {
            switch (tier?.ToUpperInvariant())
            {
                case "NORMAL": return ColCursedGreen;
                case "HARD": return ColHarvestGold;
                case "VERY HARD": return new Color(0.95f, 0.45f, 0.22f);
                case "NIGHTMARE": return new Color(0.85f, 0.35f, 1.0f);
                default: return ColTextPrimary;
            }
        }

        // ==================== EVENT LISTENER WIRING ====================

        private void WireEventListeners()
        {
            if (homePlayButton != null) { homePlayButton.onClick.RemoveAllListeners(); homePlayButton.onClick.AddListener(OpenWorldSelect); }
            if (homeUpgradesButton != null) { homeUpgradesButton.onClick.RemoveAllListeners(); homeUpgradesButton.onClick.AddListener(OpenUpgrades); }
            if (homeSettingsButton != null) { homeSettingsButton.onClick.RemoveAllListeners(); homeSettingsButton.onClick.AddListener(OpenSettingsModal); }
            if (homeCreditsButton != null) { homeCreditsButton.onClick.RemoveAllListeners(); homeCreditsButton.onClick.AddListener(OpenCredits); }
            if (homeQuitButton != null) { homeQuitButton.onClick.RemoveAllListeners(); homeQuitButton.onClick.AddListener(QuitGame); }

            if (worldSelectBackButton != null) { worldSelectBackButton.onClick.RemoveAllListeners(); worldSelectBackButton.onClick.AddListener(OpenHome); }
            if (arenaSelectBackButton != null) { arenaSelectBackButton.onClick.RemoveAllListeners(); arenaSelectBackButton.onClick.AddListener(OpenWorldSelect); }
            if (startRunButton != null) { startRunButton.onClick.RemoveAllListeners(); startRunButton.onClick.AddListener(StartSelectedArena); }

            if (upgradesBackButton != null) { upgradesBackButton.onClick.RemoveAllListeners(); upgradesBackButton.onClick.AddListener(OpenHome); }
            if (creditsBackButton != null) { creditsBackButton.onClick.RemoveAllListeners(); creditsBackButton.onClick.AddListener(OpenHome); }

            if (settingsMusicToggleButton != null) { settingsMusicToggleButton.onClick.RemoveAllListeners(); settingsMusicToggleButton.onClick.AddListener(ToggleMusicSetting); }
            if (settingsSfxToggleButton != null) { settingsSfxToggleButton.onClick.RemoveAllListeners(); settingsSfxToggleButton.onClick.AddListener(ToggleSfxSetting); }
            if (settingsHapticsToggleButton != null) { settingsHapticsToggleButton.onClick.RemoveAllListeners(); settingsHapticsToggleButton.onClick.AddListener(ToggleHapticsSetting); }
            if (settingsCloseButton != null) { settingsCloseButton.onClick.RemoveAllListeners(); settingsCloseButton.onClick.AddListener(CloseSettingsModal); }

            if (exitConfirmYesButton != null) { exitConfirmYesButton.onClick.RemoveAllListeners(); exitConfirmYesButton.onClick.AddListener(QuitGame); }
            if (exitConfirmCancelButton != null) { exitConfirmCancelButton.onClick.RemoveAllListeners(); exitConfirmCancelButton.onClick.AddListener(CloseExitConfirmation); }

            for (int i = 0; i < worldCards.Count; i++)
            {
                var card = worldCards[i];
                if (card != null && card.selectButton != null)
                {
                    int wId = card.worldId;
                    card.selectButton.onClick.RemoveAllListeners();
                    card.selectButton.onClick.AddListener(() => SelectWorld(wId));
                }
            }

            for (int i = 0; i < arenaCards.Count; i++)
            {
                var card = arenaCards[i];
                if (card != null && card.selectButton != null)
                {
                    card.selectButton.onClick.RemoveAllListeners();
                    int capturedIndex = i;
                    card.selectButton.onClick.AddListener(() =>
                    {
                        if (capturedIndex < arenaCards.Count)
                        {
                            SelectArena(arenaCards[capturedIndex].globalArenaId);
                        }
                    });
                }
            }
        }

        // ==================== PROCEDURAL PORTRAIT UI BUILDER ====================

        public void EnsureMainMenuUI()
        {
            EnsureFont();

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("MainMenuCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                transform.SetParent(canvasObj.transform, false);
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Transform canvasTransform = canvas.transform;
            if (transform.parent == canvasTransform)
            {
                transform.SetParent(null, false);
            }
            for (int i = canvasTransform.childCount - 1; i >= 0; i--)
            {
                GameObject child = canvasTransform.GetChild(i).gameObject;
                if (child == gameObject) continue;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
            transform.SetParent(canvasTransform, false);

            GameObject safeRootObj = new GameObject("SafeAreaRoot");
            safeRootObj.transform.SetParent(canvasTransform, false);
            RectTransform safeRt = safeRootObj.AddComponent<RectTransform>();
            safeRt.anchorMin = Vector2.zero;
            safeRt.anchorMax = Vector2.one;
            safeRt.offsetMin = Vector2.zero;
            safeRt.offsetMax = Vector2.zero;
            safeRootObj.AddComponent<MobileSafeAreaController>();

            Transform root = safeRootObj.transform;

            homeScreenRoot = CreateScreenPanel(root, "HomeScreenRoot");
            BuildHomeScreenContent(homeScreenRoot.transform);

            worldSelectScreenRoot = CreateScreenPanel(root, "WorldSelectScreenRoot");
            BuildWorldSelectScreenContent(worldSelectScreenRoot.transform);

            arenaSelectScreenRoot = CreateScreenPanel(root, "ArenaSelectScreenRoot");
            BuildArenaSelectScreenContent(arenaSelectScreenRoot.transform);

            upgradesScreenRoot = CreateScreenPanel(root, "UpgradesScreenRoot");
            BuildUpgradesScreenContent(upgradesScreenRoot.transform);

            creditsScreenRoot = CreateScreenPanel(root, "CreditsScreenRoot");
            BuildCreditsScreenContent(creditsScreenRoot.transform);

            settingsModalRoot = CreateScreenPanel(root, "SettingsModalRoot");
            BuildSettingsModalContent(settingsModalRoot.transform);
            settingsModalRoot.SetActive(false);

            exitConfirmModalRoot = CreateScreenPanel(root, "ExitConfirmModalRoot");
            BuildExitConfirmationContent(exitConfirmModalRoot.transform);
            exitConfirmModalRoot.SetActive(false);

            WireEventListeners();
        }

        private GameObject CreateScreenPanel(Transform parent, string name)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            return obj;
        }

        private void BuildHomeScreenContent(Transform parent)
        {
            // Top Header Backdrop Banner
            GameObject topBanner = new GameObject("TopTitleBanner");
            topBanner.transform.SetParent(parent, false);
            RectTransform rtBanner = topBanner.AddComponent<RectTransform>();
            rtBanner.anchorMin = new Vector2(0.5f, 1f);
            rtBanner.anchorMax = new Vector2(0.5f, 1f);
            rtBanner.pivot = new Vector2(0.5f, 1f);
            rtBanner.anchoredPosition = new Vector2(0f, -24f);
            rtBanner.sizeDelta = new Vector2(980f, 300f);
            Image bannerImg = topBanner.AddComponent<Image>();
            FolkHorrorUITheme.StylePanel(bannerImg, true);

            CreateLabel(topBanner.transform, "StudioLabel", "LOGMAT PRESENTS", 22, FontStyle.Bold, ColTextMuted, new Vector2(0f, 106f), new Vector2(880f, 34f));

            Text titleText = CreateLabel(topBanner.transform, "TitleText", "CURSED HARVEST.", 74, FontStyle.Bold, ColHarvestGold, new Vector2(0f, 38f), new Vector2(940f, 92f));
            Outline titleOutline = titleText.gameObject.AddComponent<Outline>();
            titleOutline.effectColor = new Color(0.12f, 0.05f, 0.02f, 0.95f);
            titleOutline.effectDistance = new Vector2(3f, -3f);

            CreateLabel(topBanner.transform, "SubtitleText", "DARK FOLK HORROR SURVIVOR  •  3 WORLDS  •  15 ARENAS", 23, FontStyle.Bold, ColTextPrimary, new Vector2(0f, -32f), new Vector2(920f, 38f));

            GameObject essPill = new GameObject("HomeEssencePill");
            essPill.transform.SetParent(topBanner.transform, false);
            RectTransform rtPill = essPill.AddComponent<RectTransform>();
            rtPill.anchorMin = new Vector2(0.5f, 0.5f);
            rtPill.anchorMax = new Vector2(0.5f, 0.5f);
            rtPill.anchoredPosition = new Vector2(0f, -96f);
            rtPill.sizeDelta = new Vector2(460f, 54f);
            Image pillBg = essPill.AddComponent<Image>();
            FolkHorrorUITheme.StylePanel(pillBg, false);

            int currentEssence = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.HarvestEssence : 0;
            homeEssenceBadgeText = CreateLabel(essPill.transform, "HomeEssenceText", $"✦ HARVEST ESSENCE: {currentEssence}", 24, FontStyle.Bold, ColHarvestGold, Vector2.zero, new Vector2(440f, 46f));

            // Bottom Thumb-Zone Action Deck
            GameObject deckObj = new GameObject("BottomButtonDeck");
            deckObj.transform.SetParent(parent, false);
            RectTransform rtDeck = deckObj.AddComponent<RectTransform>();
            rtDeck.anchorMin = new Vector2(0.5f, 0f);
            rtDeck.anchorMax = new Vector2(0.5f, 0f);
            rtDeck.pivot = new Vector2(0.5f, 0f);
            rtDeck.anchoredPosition = new Vector2(0f, 88f);
            rtDeck.sizeDelta = new Vector2(880f, 580f);
            Image deckBg = deckObj.AddComponent<Image>();
            FolkHorrorUITheme.StylePanel(deckBg, true);

            homePlayButton = CreateMenuButton(deckObj.transform, "PlayButton", "ENTER THE HARVEST", new Vector2(0f, 190f), new Vector2(780f, 120f), ColHarvestGold, 38);
            homeUpgradesButton = CreateMenuButton(deckObj.transform, "UpgradesButton", "OCCULT UPGRADES", new Vector2(0f, 52f), new Vector2(780f, 104f), ColElevated, 32);
            homeSettingsButton = CreateMenuButton(deckObj.transform, "SettingsButton", "SETTINGS", new Vector2(0f, -74f), new Vector2(780f, 96f), ColElevated, 30);
            homeCreditsButton = CreateMenuButton(deckObj.transform, "CreditsButton", "CREDITS", new Vector2(0f, -192f), new Vector2(780f, 92f), ColElevated, 28);

            CreateLabel(parent, "VersionText", "LOGMAT  •  CURSED HARVEST. v1.1.0  •  ANDROID PORTRAIT EDITION", 18, FontStyle.Normal, ColTextMuted, new Vector2(0f, -915f), new Vector2(960f, 36f));
        }

        private void BuildWorldSelectScreenContent(Transform parent)
        {
            worldCards.Clear();

            Image screenBg = parent.gameObject.AddComponent<Image>();
            screenBg.color = ColBackground;

            worldSelectBackButton = CreateMenuButton(parent, "BackButton", "← BACK", new Vector2(-370f, 865f), new Vector2(220f, 84f), ColElevated, 26);

            CreateLabel(parent, "WorldSelectTitle", "SELECT WORLD", 44, FontStyle.Bold, ColHarvestGold, new Vector2(95f, 878f), new Vector2(660f, 56f));
            CreateLabel(parent, "WorldSelectSubtitle", "Each World contains 5 unique Arenas and Bosses", 21, FontStyle.Normal, ColTextMuted, new Vector2(95f, 834f), new Vector2(680f, 32f));

            float cardWidth = 960f;
            float cardHeight = 310f;
            float cardSpacing = 28f;
            float startY = 560f;

            for (int i = 1; i <= 3; i++)
            {
                int worldId = i;
                WorldConfig cfg = WorldConfig.GetConfig(worldId);

                GameObject cardObj = new GameObject($"WorldCard_{worldId}");
                cardObj.transform.SetParent(parent, false);
                RectTransform rtCard = cardObj.AddComponent<RectTransform>();
                rtCard.anchorMin = new Vector2(0.5f, 0.5f);
                rtCard.anchorMax = new Vector2(0.5f, 0.5f);
                rtCard.sizeDelta = new Vector2(cardWidth, cardHeight);
                float yPos = startY - (i - 1) * (cardHeight + cardSpacing);
                rtCard.anchoredPosition = new Vector2(0f, yPos);

                Image cardBg = cardObj.AddComponent<Image>();
                FolkHorrorUITheme.StylePanel(cardBg, true);

                GameObject borderObj = new GameObject("Border");
                borderObj.transform.SetParent(cardObj.transform, false);
                RectTransform rtBorder = borderObj.AddComponent<RectTransform>();
                rtBorder.anchorMin = Vector2.zero;
                rtBorder.anchorMax = Vector2.one;
                rtBorder.sizeDelta = Vector2.zero;
                Image borderImg = borderObj.AddComponent<Image>();
                FolkHorrorUITheme.StylePanel(borderImg, false);

                GameObject innerBgObj = new GameObject("InnerCardBg");
                innerBgObj.transform.SetParent(cardObj.transform, false);
                RectTransform rtInner = innerBgObj.AddComponent<RectTransform>();
                rtInner.anchorMin = Vector2.zero;
                rtInner.anchorMax = Vector2.one;
                rtInner.offsetMin = new Vector2(6f, 6f);
                rtInner.offsetMax = new Vector2(-6f, -6f);
                Image innerBgImg = innerBgObj.AddComponent<Image>();
                FolkHorrorUITheme.StylePanel(innerBgImg, false);
                cardBg = innerBgImg;

                Button btn = cardObj.AddComponent<Button>();

                // Biome Thumbnail (240 x 260)
                GameObject thumbObj = new GameObject("BiomePreviewThumbnail");
                thumbObj.transform.SetParent(cardObj.transform, false);
                RectTransform rtThumb = thumbObj.AddComponent<RectTransform>();
                rtThumb.anchorMin = new Vector2(0f, 0.5f);
                rtThumb.anchorMax = new Vector2(0f, 0.5f);
                rtThumb.pivot = new Vector2(0f, 0.5f);
                rtThumb.anchoredPosition = new Vector2(20f, 0f);
                rtThumb.sizeDelta = new Vector2(240f, 260f);
                Image thumbImg = thumbObj.AddComponent<Image>();
                thumbImg.sprite = GetOrCreateWorldPreviewSprite(worldId);

                // World details labels
                Text tWNum = CreateLabel(cardObj.transform, "WorldNumber", $"WORLD {worldId}", 24, FontStyle.Bold, ColHarvestGold, new Vector2(0f, 96f), new Vector2(400f, 32f), TextAnchor.MiddleLeft);
                Text tWName = CreateLabel(cardObj.transform, "WorldName", cfg != null ? cfg.displayName : $"WORLD {worldId}", 34, FontStyle.Bold, ColTextPrimary, new Vector2(100f, 44f), new Vector2(600f, 44f), TextAnchor.MiddleLeft);
                Text tDiff = CreateLabel(cardObj.transform, "DifficultyBadge", cfg != null ? cfg.difficultyTier : "NORMAL", 24, FontStyle.Bold, GetTierColor(cfg != null ? cfg.difficultyTier : ""), new Vector2(340f, 96f), new Vector2(240f, 32f), TextAnchor.MiddleRight);
                Text tArenas = CreateLabel(cardObj.transform, "ArenaSummary", "5 Unique Arenas  •  Wave 5 Boss Encounter", 22, FontStyle.Normal, ColTextMuted, new Vector2(100f, -14f), new Vector2(600f, 34f), TextAnchor.MiddleLeft);
                Text tStat = CreateLabel(cardObj.transform, "StatusBadge", worldId == 1 ? "★ UNLOCKED — TAP TO CHOOSE ARENA" : $"LOCKED (Complete World {worldId - 1})", 22, FontStyle.Bold, worldId == 1 ? ColHarvestGold : ColDanger, new Vector2(100f, -76f), new Vector2(600f, 34f), TextAnchor.MiddleLeft);

                WorldCardUIElements elem = new WorldCardUIElements
                {
                    worldId = worldId,
                    cardRoot = cardObj,
                    selectButton = btn,
                    worldNumberText = tWNum,
                    worldNameText = tWName,
                    difficultyBadgeText = tDiff,
                    arenaSummaryText = tArenas,
                    statusBadgeText = tStat,
                    highlightBorder = borderImg,
                    background = cardBg,
                    previewThumbnail = thumbImg
                };
                worldCards.Add(elem);
            }
        }

        private void BuildArenaSelectScreenContent(Transform parent)
        {
            arenaCards.Clear();

            Image screenBg = parent.gameObject.AddComponent<Image>();
            screenBg.color = ColBackground;

            arenaSelectBackButton = CreateMenuButton(parent, "BackButton", "← WORLDS", new Vector2(-360f, 865f), new Vector2(240f, 84f), ColElevated, 26);

            arenaSelectTitleText = CreateLabel(parent, "ArenaSelectTitle", "THE CURSED REALMS", 44, FontStyle.Bold, ColHarvestGold, new Vector2(95f, 878f), new Vector2(660f, 56f));
            arenaSelectSubtitleText = CreateLabel(parent, "ArenaSelectSubtitle", "WORLD 1 ARENAS  •  CHOOSE YOUR BATTLEGROUND", 21, FontStyle.Normal, ColTextMuted, new Vector2(95f, 834f), new Vector2(680f, 32f));

            float cardWidth = 960f;
            float cardHeight = 160f;
            float cardSpacing = 16f;
            float startY = 460f;

            for (int i = 0; i < 5; i++)
            {
                int arenaSlot = i + 1;
                GameObject cardObj = new GameObject($"ArenaCard_{arenaSlot}");
                cardObj.transform.SetParent(parent, false);
                RectTransform rtCard = cardObj.AddComponent<RectTransform>();
                rtCard.anchorMin = new Vector2(0.5f, 0.5f);
                rtCard.anchorMax = new Vector2(0.5f, 0.5f);
                rtCard.sizeDelta = new Vector2(cardWidth, cardHeight);
                float yPos = startY - i * (cardHeight + cardSpacing);
                rtCard.anchoredPosition = new Vector2(0f, yPos);

                Image cardBg = cardObj.AddComponent<Image>();
                FolkHorrorUITheme.StylePanel(cardBg, false);

                GameObject borderObj = new GameObject("Border");
                borderObj.transform.SetParent(cardObj.transform, false);
                RectTransform rtBorder = borderObj.AddComponent<RectTransform>();
                rtBorder.anchorMin = Vector2.zero;
                rtBorder.anchorMax = Vector2.one;
                rtBorder.sizeDelta = Vector2.zero;
                Image borderImg = borderObj.AddComponent<Image>();
                FolkHorrorUITheme.StylePanel(borderImg, true);

                GameObject innerBgObj = new GameObject("InnerCardBg");
                innerBgObj.transform.SetParent(cardObj.transform, false);
                RectTransform rtInner = innerBgObj.AddComponent<RectTransform>();
                rtInner.anchorMin = Vector2.zero;
                rtInner.anchorMax = Vector2.one;
                rtInner.offsetMin = new Vector2(4f, 4f);
                rtInner.offsetMax = new Vector2(-4f, -4f);
                Image innerBgImg = innerBgObj.AddComponent<Image>();
                FolkHorrorUITheme.StylePanel(innerBgImg, false);
                cardBg = innerBgImg;

                Button btn = cardObj.AddComponent<Button>();

                Text tANum = CreateLabel(cardObj.transform, "ArenaNumber", $"ARENA 1-{arenaSlot}", 22, FontStyle.Bold, ColHarvestGold, new Vector2(-280f, 36f), new Vector2(340f, 32f), TextAnchor.MiddleLeft);
                Text tAName = CreateLabel(cardObj.transform, "ArenaName", "Arena Name", 30, FontStyle.Bold, ColTextPrimary, new Vector2(-120f, -14f), new Vector2(660f, 40f), TextAnchor.MiddleLeft);
                Text tDiff = CreateLabel(cardObj.transform, "DifficultyBadge", "NORMAL", 22, FontStyle.Bold, ColCursedGreen, new Vector2(340f, 36f), new Vector2(220f, 32f), TextAnchor.MiddleRight);
                Text tStat = CreateLabel(cardObj.transform, "StatusBadge", "UNLOCKED", 20, FontStyle.Bold, ColHarvestGold, new Vector2(340f, -18f), new Vector2(220f, 32f), TextAnchor.MiddleRight);

                ArenaCardUIElements elem = new ArenaCardUIElements
                {
                    globalArenaId = arenaSlot,
                    arenaIndexInWorld = arenaSlot,
                    cardRoot = cardObj,
                    selectButton = btn,
                    arenaNumberText = tANum,
                    arenaNameText = tAName,
                    difficultyBadgeText = tDiff,
                    statusBadgeText = tStat,
                    highlightBorder = borderImg,
                    background = cardBg
                };
                arenaCards.Add(elem);
            }

            // Bottom Details Panel & Launch Button
            GameObject detailsObj = new GameObject("ArenaDetailsPanel");
            detailsObj.transform.SetParent(parent, false);
            RectTransform rtDetails = detailsObj.AddComponent<RectTransform>();
            rtDetails.anchorMin = new Vector2(0.5f, 0.5f);
            rtDetails.anchorMax = new Vector2(0.5f, 0.5f);
            rtDetails.anchoredPosition = new Vector2(0f, -460f);
            rtDetails.sizeDelta = new Vector2(960f, 180f);
            Image detailsBg = detailsObj.AddComponent<Image>();
            FolkHorrorUITheme.StylePanel(detailsBg, true);

            selectedArenaDescText = CreateLabel(detailsObj.transform, "ArenaDesc", "A moonlit rural farm under eerie quietude.", 22, FontStyle.Normal, ColTextPrimary, new Vector2(0f, 38f), new Vector2(900f, 60f), TextAnchor.MiddleLeft);
            selectedArenaBossSummaryText = CreateLabel(detailsObj.transform, "BossSummary", "WAVE 5 BOSS: THE LAST HARVEST", 24, FontStyle.Bold, ColDanger, new Vector2(0f, -32f), new Vector2(900f, 40f), TextAnchor.MiddleLeft);

            startRunButton = CreateMenuButton(parent, "StartRunButton", "ENTER ARENA", new Vector2(0f, -720f), new Vector2(940f, 126f), ColHarvestGold, 38);
            startRunButtonText = startRunButton.GetComponentInChildren<Text>();
        }

        private void BuildUpgradesScreenContent(Transform parent)
        {
            upgradeBranchCards.Clear();
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying) Destroy(parent.GetChild(i).gameObject);
                else DestroyImmediate(parent.GetChild(i).gameObject);
            }

            Image screenBg = parent.GetComponent<Image>();
            if (screenBg == null) screenBg = parent.gameObject.AddComponent<Image>();
            screenBg.color = ColBackground;

            upgradesBackButton = CreateMenuButton(parent, "UpgradesBackButton", "← BACK", new Vector2(-370f, 865f), new Vector2(220f, 84f), ColElevated, 26);

            CreateLabel(parent, "UpgradesTitle", "OCCULT UPGRADES", 42, FontStyle.Bold, ColHarvestGold, new Vector2(95f, 878f), new Vector2(660f, 54f));
            CreateLabel(parent, "UpgradesSubtitle", "Permanent blessings across all realms", 21, FontStyle.Normal, ColTextMuted, new Vector2(95f, 834f), new Vector2(660f, 32f));

            GameObject essBadgeObj = new GameObject("EssenceBalanceBadge");
            essBadgeObj.transform.SetParent(parent, false);
            RectTransform rtEss = essBadgeObj.AddComponent<RectTransform>();
            rtEss.anchorMin = new Vector2(0.5f, 0.5f);
            rtEss.anchorMax = new Vector2(0.5f, 0.5f);
            rtEss.anchoredPosition = new Vector2(0f, 735f);
            rtEss.sizeDelta = new Vector2(960f, 76f);
            Image essBg = essBadgeObj.AddComponent<Image>();
            FolkHorrorUITheme.StylePanel(essBg, false);

            essenceBalanceText = CreateLabel(essBadgeObj.transform, "EssenceText", "✦ HARVEST ESSENCE: 0", 30, FontStyle.Bold, ColHarvestGold, Vector2.zero, new Vector2(920f, 64f));

            MetaUpgradeBranch[] branches = (MetaUpgradeBranch[])Enum.GetValues(typeof(MetaUpgradeBranch));
            float cardWidth = 468f;
            float cardHeight = 340f;
            float[] colX = new float[] { -248f, 248f };
            float[] rowY = new float[] { 475f, 115f, -245f, -605f };

            for (int i = 0; i < branches.Length; i++)
            {
                MetaUpgradeBranch branch = branches[i];
                MetaUpgradeDef def = MetaProgressionManager.Instance.GetDef(branch);
                int col = i % 2;
                int row = i / 2;

                GameObject cObj = new GameObject($"UpgradeCard_{branch}");
                cObj.transform.SetParent(parent, false);
                RectTransform rt = cObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(cardWidth, cardHeight);
                rt.anchoredPosition = new Vector2(colX[col], rowY[row]);

                Image bg = cObj.AddComponent<Image>();
                FolkHorrorUITheme.StylePanel(bg, true);

                // Add icon image if available
                Sprite iconSp = null;
#if UNITY_EDITOR
                iconSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/_Documentation/Branding/Mobile/MetaIcon_{branch}.png");
#endif
                if (iconSp != null)
                {
                    GameObject icoObj = new GameObject("CardIcon");
                    icoObj.transform.SetParent(cObj.transform, false);
                    RectTransform rtIco = icoObj.AddComponent<RectTransform>();
                    rtIco.anchorMin = new Vector2(0.5f, 1f);
                    rtIco.anchorMax = new Vector2(0.5f, 1f);
                    rtIco.pivot = new Vector2(0.5f, 1f);
                    rtIco.anchoredPosition = new Vector2(0f, -14f);
                    rtIco.sizeDelta = new Vector2(64f, 64f);
                    Image icoImg = icoObj.AddComponent<Image>();
                    icoImg.sprite = iconSp;
                }

                Text tN = CreateLabel(cObj.transform, "Title", $"{def.displayName.ToUpper()}", 25, FontStyle.Bold, ColHarvestGold, new Vector2(0f, 76f), new Vector2(430f, 38f));
                Text tR = CreateLabel(cObj.transform, "Rank", "LEVEL: [□ □ □ □ □] 0/5", 20, FontStyle.Bold, ColCursedGreen, new Vector2(0f, 38f), new Vector2(430f, 30f));
                Text tD = CreateLabel(cObj.transform, "Desc", "", 19, FontStyle.Normal, ColTextPrimary, new Vector2(0f, -22f), new Vector2(424f, 82f));

                Button uBtn = CreateMenuButton(cObj.transform, "UpgradeButton", "UPGRADE", new Vector2(0f, -112f), new Vector2(420f, 76f), ColHarvestGold, 22);
                Text btnText = uBtn.GetComponentInChildren<Text>();

                uBtn.onClick.RemoveAllListeners();
                MetaUpgradeBranch capturedBranch = branch;
                uBtn.onClick.AddListener(() =>
                {
                    if (Time.unscaledTime - lastUpgradeClickTime < 0.25f) return;
                    lastUpgradeClickTime = Time.unscaledTime;
                    AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
                    MetaProgressionManager.Instance.TryUpgradeBranch(capturedBranch);
                    RefreshUpgradesUI();
                });

                MetaUpgradeCardUIElements elem = new MetaUpgradeCardUIElements
                {
                    branch = branch,
                    titleText = tN,
                    rankText = tR,
                    descText = tD,
                    upgradeButton = uBtn,
                    buttonText = btnText
                };
                upgradeBranchCards.Add(elem);
            }

            RefreshUpgradesUI();
        }

        public void RefreshUpgradesUI()
        {
            if (MetaProgressionManager.Instance == null) return;

            int essence = MetaProgressionManager.Instance.HarvestEssence;
            if (essenceBalanceText != null) essenceBalanceText.text = $"✦ HARVEST ESSENCE: {essence}";
            if (homeEssenceBadgeText != null) homeEssenceBadgeText.text = $"✦ HARVEST ESSENCE: {essence}";

            for (int i = 0; i < upgradeBranchCards.Count; i++)
            {
                var card = upgradeBranchCards[i];
                if (card == null) continue;

                MetaUpgradeDef def = MetaProgressionManager.Instance.GetDef(card.branch);
                int rank = MetaProgressionManager.Instance.GetRank(card.branch);

                string boxes = "";
                for (int b = 0; b < 5; b++)
                {
                    boxes += (b < rank) ? "■ " : "□ ";
                }
                if (card.rankText != null) card.rankText.text = $"Lv {rank}/5  [{boxes.Trim()}]";

                if (card.descText != null)
                {
                    if (rank == 0)
                        card.descText.text = $"Current: Base\nNext: {def.GetDescriptionForRank(1)}";
                    else if (rank < 5)
                        card.descText.text = $"Active: {def.GetDescriptionForRank(rank)}\nNext: {def.GetDescriptionForRank(rank + 1)}";
                    else
                        card.descText.text = $"{def.GetDescriptionForRank(rank)}\n[MAX RANK CONSECRATED]";
                }

                if (card.upgradeButton != null && card.buttonText != null)
                {
                    if (rank >= 5)
                    {
                        card.upgradeButton.interactable = false;
                        card.buttonText.text = "MAX LEVEL";
                        card.buttonText.color = ColTextMuted;
                    }
                    else
                    {
                        int cost = def.GetCost(rank);
                        bool canAfford = essence >= cost;
                        card.upgradeButton.interactable = canAfford;
                        card.buttonText.text = $"UPGRADE ({cost} ✦)";
                        card.buttonText.color = canAfford ? ColTextPrimary : ColDanger;
                    }
                }
            }
        }

        private void BuildSettingsModalContent(Transform parent)
        {
            Image dimBg = parent.gameObject.AddComponent<Image>();
            dimBg.color = new Color(0.02f, 0.02f, 0.03f, 0.90f);

            GameObject cardObj = new GameObject("SettingsCard");
            cardObj.transform.SetParent(parent, false);
            RectTransform rtCard = cardObj.AddComponent<RectTransform>();
            rtCard.anchorMin = new Vector2(0.5f, 0.5f);
            rtCard.anchorMax = new Vector2(0.5f, 0.5f);
            rtCard.sizeDelta = new Vector2(860f, 860f);
            Image cardBg = cardObj.AddComponent<Image>();
            FolkHorrorUITheme.StylePanel(cardBg, true);

            CreateLabel(cardObj.transform, "SettingsTitle", "SETTINGS", 48, FontStyle.Bold, ColHarvestGold, new Vector2(0f, 320f), new Vector2(760f, 64f));
            CreateLabel(cardObj.transform, "SettingsSub", "Audio & Touch Haptics Preferences", 22, FontStyle.Normal, ColTextMuted, new Vector2(0f, 260f), new Vector2(760f, 36f));

            settingsMusicToggleButton = CreateMenuButton(cardObj.transform, "MusicToggleBtn", "MUSIC: ON", new Vector2(0f, 130f), new Vector2(680f, 108f), ColElevated, 32);
            settingsMusicToggleText = settingsMusicToggleButton.GetComponentInChildren<Text>();

            settingsSfxToggleButton = CreateMenuButton(cardObj.transform, "SfxToggleBtn", "SFX: ON", new Vector2(0f, -10f), new Vector2(680f, 108f), ColElevated, 32);
            settingsSfxToggleText = settingsSfxToggleButton.GetComponentInChildren<Text>();

            settingsHapticsToggleButton = CreateMenuButton(cardObj.transform, "HapticsToggleBtn", "HAPTICS: ON", new Vector2(0f, -150f), new Vector2(680f, 108f), ColElevated, 32);
            settingsHapticsToggleText = settingsHapticsToggleButton.GetComponentInChildren<Text>();

            settingsCloseButton = CreateMenuButton(cardObj.transform, "CloseSettingsBtn", "CLOSE", new Vector2(0f, -300f), new Vector2(460f, 92f), ColHarvestGold, 30);
        }

        private void BuildExitConfirmationContent(Transform parent)
        {
            Image dimBg = parent.gameObject.AddComponent<Image>();
            dimBg.color = new Color(0.02f, 0.02f, 0.03f, 0.90f);

            GameObject cardObj = new GameObject("ExitCard");
            cardObj.transform.SetParent(parent, false);
            RectTransform rtCard = cardObj.AddComponent<RectTransform>();
            rtCard.anchorMin = new Vector2(0.5f, 0.5f);
            rtCard.anchorMax = new Vector2(0.5f, 0.5f);
            rtCard.sizeDelta = new Vector2(800f, 520f);
            Image cardBg = cardObj.AddComponent<Image>();
            FolkHorrorUITheme.StylePanel(cardBg, false);

            CreateLabel(cardObj.transform, "ExitTitle", "ABANDON HARVEST?", 44, FontStyle.Bold, ColDanger, new Vector2(0f, 160f), new Vector2(720f, 60f));
            CreateLabel(cardObj.transform, "ExitMessage", "Are you sure you wish to exit to device?", 24, FontStyle.Normal, ColTextPrimary, new Vector2(0f, 75f), new Vector2(720f, 50f));

            exitConfirmYesButton = CreateMenuButton(cardObj.transform, "YesButton", "QUIT TO DEVICE", new Vector2(0f, -30f), new Vector2(640f, 96f), ColDanger, 30);
            exitConfirmCancelButton = CreateMenuButton(cardObj.transform, "CancelButton", "STAY IN HARVEST", new Vector2(0f, -145f), new Vector2(640f, 96f), ColElevated, 30);
        }

        private void BuildCreditsScreenContent(Transform parent)
        {
            Image screenBg = parent.gameObject.AddComponent<Image>();
            screenBg.color = ColBackground;

            creditsBackButton = CreateMenuButton(parent, "CreditsBackButton", "← BACK", new Vector2(-370f, 865f), new Vector2(220f, 84f), ColElevated, 26);

            CreateLabel(parent, "CreditsTitle", "CREDITS", 48, FontStyle.Bold, ColHarvestGold, new Vector2(95f, 878f), new Vector2(660f, 56f));
            CreateLabel(parent, "CreditsSubtitle", "The artisans behind the cursed harvest", 22, FontStyle.Normal, ColTextMuted, new Vector2(95f, 834f), new Vector2(680f, 32f));

            GameObject cardObj = new GameObject("CreditsCard");
            cardObj.transform.SetParent(parent, false);
            RectTransform rtCard = cardObj.AddComponent<RectTransform>();
            rtCard.anchorMin = new Vector2(0.5f, 0.5f);
            rtCard.anchorMax = new Vector2(0.5f, 0.5f);
            rtCard.sizeDelta = new Vector2(960f, 1400f);
            rtCard.anchoredPosition = new Vector2(0f, -60f);
            Image cardBg = cardObj.AddComponent<Image>();
            FolkHorrorUITheme.StylePanel(cardBg, true);

            string creditsText =
                "CURSED HARVEST. — MOBILE v1.1.0\n" +
                "RELEASE EDITION\n\n" +
                "DEVELOPED & PUBLISHED BY\n" +
                "LOGMAT\n\n" +
                "CORE GAME ARCHITECTURE\n" +
                "LOGMAT Interactive Systems\n\n" +
                "3D ASSETS & ARTISTRY\n" +
                "Synty Studios (Polygon Generic, Halloween Masks)\n" +
                "Original Dark Folk-Horror Mobile Branding\n\n" +
                "AUDIO & ATMOSPHERE\n" +
                "Folk Horror Drone & Procedural Synthesizer\n\n" +
                "MOBILE ARCHITECTURE\n" +
                "Native Android Portrait Pipeline\n" +
                "Touch-First Mobile Navigation & Controls\n\n" +
                "DEDICATED TO ALL OCCULT ROGUELIKE HARVESTERS";

            Text tCredits = CreateLabel(cardObj.transform, "CreditsBody", creditsText, 24, FontStyle.Normal, ColTextPrimary, Vector2.zero, new Vector2(880f, 1300f), TextAnchor.MiddleCenter);
            tCredits.lineSpacing = 1.35f;
        }

        private Sprite GetOrCreateWorldPreviewSprite(int worldId)
        {
            if (_worldThumbnailCache.TryGetValue(worldId, out Sprite cached) && cached != null) return cached;

            int w = 240;
            int h = 180;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            Color skyTop = new Color(0.05f, 0.05f, 0.08f);
            Color skyBottom = new Color(0.12f, 0.10f, 0.14f);
            Color groundCol = new Color(0.16f, 0.14f, 0.12f);
            Color accentGlow = ColHarvestGold;

            if (worldId == 2)
            {
                skyBottom = new Color(0.14f, 0.16f, 0.18f);
                groundCol = new Color(0.12f, 0.14f, 0.16f);
                accentGlow = new Color(0.60f, 0.70f, 0.85f);
            }
            else if (worldId == 3)
            {
                skyBottom = new Color(0.18f, 0.08f, 0.22f);
                groundCol = new Color(0.10f, 0.06f, 0.14f);
                accentGlow = new Color(0.85f, 0.35f, 0.95f);
            }

            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float ny = y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    Color c = ny < 0.35f ? groundCol : Color.Lerp(skyBottom, skyTop, (ny - 0.35f) / 0.65f);
                    pixels[y * w + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            _worldThumbnailCache[worldId] = sp;
            return sp;
        }

        private Text CreateLabel(Transform parent, string name, string content, int fontSize, FontStyle style, Color color, Vector2 anchoredPos, Vector2 size, TextAnchor align = TextAnchor.MiddleCenter)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Text t = obj.AddComponent<Text>();
            t.font = cachedFont;
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.alignment = align;
            t.color = color;
            t.text = content;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private Button CreateMenuButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color bgColor, int fontSize = 26)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;

            Image img = btnObj.AddComponent<Image>();
            Button btn = btnObj.AddComponent<Button>();

            string style = (bgColor == ColHarvestGold) ? "gold" : ((bgColor == ColDanger) ? "danger" : "dark");
            FolkHorrorUITheme.StyleButton(btn, img, style);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform rtT = textObj.AddComponent<RectTransform>();
            rtT.anchorMin = Vector2.zero;
            rtT.anchorMax = Vector2.one;
            rtT.sizeDelta = Vector2.zero;

            Text t = textObj.AddComponent<Text>();
            t.font = cachedFont;
            t.fontSize = fontSize;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.text = label;
            t.color = (style == "gold") ? new Color(0.12f, 0.08f, 0.04f) : ColTextPrimary;

            return btn;
        }
    }
}
