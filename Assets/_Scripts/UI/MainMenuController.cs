using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CursedHarvest
{
    /// <summary>
    /// Master Frontend controller managing HOME, WORLD SELECT, UPGRADES, and CREDITS screens.
    /// Bridges user input to GameSession, reflects real-time unlock/completion progression,
    /// and provides robust procedural UI generation if scene references are missing.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        private static MainMenuController _instance;
        public static MainMenuController Instance => _instance;

        [Header("Screen Roots")]
        [SerializeField] private GameObject homeScreenRoot;
        [SerializeField] private GameObject worldSelectScreenRoot;
        [SerializeField] private GameObject upgradesScreenRoot;
        [SerializeField] private GameObject creditsScreenRoot;

        [Header("Home Screen UI")]
        [SerializeField] private Button homePlayButton;
        [SerializeField] private Button homeUpgradesButton;
        [SerializeField] private Button homeCreditsButton;
        [SerializeField] private Button homeQuitButton;

        [Header("World Select UI")]
        [SerializeField] private Button worldSelectBackButton;
        [SerializeField] private Button startRunButton;
        [SerializeField] private Text startRunButtonText;
        [SerializeField] private Text selectedWorldTitleText;
        [SerializeField] private Text selectedWorldSubtitleText;
        [SerializeField] private Text selectedWorldTierText;
        [SerializeField] private Text selectedWorldDescText;
        [SerializeField] private Text selectedWorldWaveCountText;
        [SerializeField] private Text selectedWorldModifiersText;
        [SerializeField] private Text selectedWorldBossText;
        [SerializeField] private Text selectedWorldStatusBadgeText;

        [Header("World Cards UI")]
        [SerializeField] private List<WorldCardUIElements> worldCards = new List<WorldCardUIElements>();

        [Header("Debug Controls")]
        [SerializeField] private Button debugUnlockAllButton;
        [SerializeField] private Button debugResetProgressButton;

        [Header("Upgrades UI")]
        [SerializeField] private Button upgradesBackButton;

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
            public Text statusBadgeText;
            public Image highlightBorder;
            public Image background;
        }

        private int currentlySelectedWorldId = 1;
        private Font cachedFont;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            Time.timeScale = 1f;

            EnsureFont();
            EnsureMainMenuUI();
            WireEventListeners();
        }

        private void Start()
        {
            // Restore proper timeScale
            Time.timeScale = 1f;

#if UNITY_WEBGL
            if (homeQuitButton != null) homeQuitButton.gameObject.SetActive(false);
#endif

            // Initialize active world from GameSession if available
            if (GameSession.Instance != null)
            {
                currentlySelectedWorldId = GameSession.Instance.SelectedWorldId;
            }

            // Determine initial screen from GameSession request
            MainMenuScreen startScreen = MainMenuScreen.Home;
            if (GameSession.Instance != null)
            {
                startScreen = GameSession.Instance.InitialMenuScreen;
            }

            ShowScreen(startScreen);
            RefreshWorldSelectionCards();
            UpdateWorldDetailsPanel(currentlySelectedWorldId);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // Escape key navigation: WorldSelect / Upgrades / Credits -> Return to Home
                if ((worldSelectScreenRoot != null && worldSelectScreenRoot.activeSelf) ||
                    (upgradesScreenRoot != null && upgradesScreenRoot.activeSelf) ||
                    (creditsScreenRoot != null && creditsScreenRoot.activeSelf))
                {
                    OpenHome();
                }
                // If on Home, do nothing (no error)
            }
        }

        private void OnEnable()
        {
            GameSession.OnWorldProgressionChanged += HandleProgressionChanged;
        }

        private void OnDisable()
        {
            GameSession.OnWorldProgressionChanged -= HandleProgressionChanged;
        }

        private void HandleProgressionChanged(int highestUnlocked)
        {
            RefreshWorldSelectionCards();
            UpdateWorldDetailsPanel(currentlySelectedWorldId);
        }

        private void EnsureFont()
        {
            if (cachedFont == null)
            {
                cachedFont = Font.CreateDynamicFontFromOSFont("Arial", 20);
                if (cachedFont == null)
                {
                    cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
            }
        }

        // ==================== SCREEN NAVIGATION ====================

        public void ShowScreen(MainMenuScreen screen)
        {
            if (homeScreenRoot != null) homeScreenRoot.SetActive(screen == MainMenuScreen.Home);
            if (worldSelectScreenRoot != null) worldSelectScreenRoot.SetActive(screen == MainMenuScreen.WorldSelect);
            if (upgradesScreenRoot != null) upgradesScreenRoot.SetActive(screen == MainMenuScreen.Upgrades);
            if (creditsScreenRoot != null) creditsScreenRoot.SetActive(screen == MainMenuScreen.Credits);

            if (screen == MainMenuScreen.WorldSelect)
            {
                RefreshWorldSelectionCards();
                UpdateWorldDetailsPanel(currentlySelectedWorldId);
            }
        }

        public void OpenHome()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            ShowScreen(MainMenuScreen.Home);
        }

        public void OpenWorldSelect()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            ShowScreen(MainMenuScreen.WorldSelect);
        }

        public void OpenUpgrades()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            ShowScreen(MainMenuScreen.Upgrades);
        }

        public void OpenCredits()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            ShowScreen(MainMenuScreen.Credits);
        }

        public void QuitGame()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            Debug.Log("[MainMenuController] Quitting application...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ==================== WORLD SELECTION ====================

        public void SelectWorld(int worldId)
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            currentlySelectedWorldId = Mathf.Clamp(worldId, 1, 5);
            RefreshWorldSelectionCards();
            UpdateWorldDetailsPanel(currentlySelectedWorldId);
        }

        public void StartSelectedWorld()
        {
            bool isUnlocked = GameSession.Instance == null || GameSession.Instance.IsWorldUnlocked(currentlySelectedWorldId);
            if (!isUnlocked)
            {
                Debug.LogWarning($"[MainMenuController] Cannot start World {currentlySelectedWorldId}: Locked!");
                return;
            }

            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            Debug.Log($"[MainMenuController] Launching World {currentlySelectedWorldId}...");

            if (GameSession.Instance != null)
            {
                GameSession.Instance.StartRun(currentlySelectedWorldId);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainGame");
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
                bool isSelected = (wId == currentlySelectedWorldId);

                if (card.worldNameText != null)
                {
                    card.worldNameText.text = config != null ? config.displayName : $"WORLD {wId}";
                }

                if (card.difficultyBadgeText != null)
                {
                    card.difficultyBadgeText.text = config != null ? config.difficultyTier : "NORMAL";
                    card.difficultyBadgeText.color = GetTierColor(config != null ? config.difficultyTier : "");
                }

                if (card.statusBadgeText != null)
                {
                    if (isCompleted)
                    {
                        card.statusBadgeText.text = "★ COMPLETED";
                        card.statusBadgeText.color = new Color(0.35f, 1.0f, 0.45f); // Consecrated green
                    }
                    else if (isUnlocked)
                    {
                        card.statusBadgeText.text = "UNLOCKED";
                        card.statusBadgeText.color = new Color(1.0f, 0.85f, 0.25f); // Amber
                    }
                    else
                    {
                        card.statusBadgeText.text = $"LOCKED (Req: W{wId - 1})";
                        card.statusBadgeText.color = new Color(0.7f, 0.25f, 0.25f); // Red
                    }
                }

                if (card.highlightBorder != null)
                {
                    card.highlightBorder.color = isSelected 
                        ? new Color(1f, 0.85f, 0.25f, 1f) 
                        : (isUnlocked ? new Color(0.4f, 0.35f, 0.25f, 0.4f) : new Color(0.2f, 0.2f, 0.2f, 0.3f));
                }

                if (card.background != null)
                {
                    if (isSelected)
                    {
                        card.background.color = new Color(0.18f, 0.14f, 0.10f, 0.95f);
                    }
                    else if (isUnlocked)
                    {
                        card.background.color = new Color(0.10f, 0.08f, 0.07f, 0.85f);
                    }
                    else
                    {
                        card.background.color = new Color(0.06f, 0.05f, 0.05f, 0.70f);
                    }
                }
            }
        }

        private void UpdateWorldDetailsPanel(int worldId)
        {
            WorldConfig config = WorldConfig.GetConfig(worldId);
            if (config == null) return;

            bool isUnlocked = GameSession.Instance == null || GameSession.Instance.IsWorldUnlocked(worldId);
            bool isCompleted = GameSession.Instance != null && GameSession.Instance.IsWorldCompleted(worldId);

            if (selectedWorldTitleText != null)
            {
                selectedWorldTitleText.text = config.displayName;
                selectedWorldTitleText.color = config.themeColor;
            }

            if (selectedWorldSubtitleText != null)
            {
                selectedWorldSubtitleText.text = config.subtitle;
            }

            if (selectedWorldTierText != null)
            {
                selectedWorldTierText.text = $"DIFFICULTY: {config.difficultyTier}";
                selectedWorldTierText.color = GetTierColor(config.difficultyTier);
            }

            if (selectedWorldDescText != null)
            {
                selectedWorldDescText.text = config.description;
            }

            if (selectedWorldWaveCountText != null)
            {
                selectedWorldWaveCountText.text = $"TOTAL WAVES: {config.totalWaves}";
            }

            if (selectedWorldBossText != null)
            {
                if (config.bossEncounters != null && config.bossEncounters.Length > 0)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int i = 0; i < config.bossEncounters.Length; i++)
                    {
                        var enc = config.bossEncounters[i];
                        if (i > 0) sb.Append("\n");
                        string typeStr = enc.isFinalBoss ? "FINAL BOSS" : "MID-BOSS";
                        sb.Append($"• Wave {enc.waveNumber}: {enc.bossName} ({typeStr})");
                    }
                    selectedWorldBossText.text = sb.ToString();
                }
                else
                {
                    selectedWorldBossText.text = "• Wave 10: THE LAST HARVEST";
                }
            }

            if (selectedWorldModifiersText != null)
            {
                selectedWorldModifiersText.text = 
                    $"Enemy Damage: {config.enemyDamageMultiplier * 100:0}%\n" +
                    $"Boss Damage: {config.bossDamageMultiplier * 100:0}%\n" +
                    $"Enemy Speed: {config.enemySpeedMultiplier * 100:0}%\n" +
                    $"Supply Drops: Waves {string.Join(", ", config.supplyDropWaves ?? new int[] { 2, 4, 6, 8, 9 })}";
            }

            if (selectedWorldStatusBadgeText != null)
            {
                if (isCompleted)
                {
                    selectedWorldStatusBadgeText.text = "★ WORLD CONSECRATED (COMPLETED) ★";
                    selectedWorldStatusBadgeText.color = new Color(0.35f, 1.0f, 0.45f);
                }
                else if (isUnlocked)
                {
                    selectedWorldStatusBadgeText.text = "★ READY FOR HARVEST (UNLOCKED) ★";
                    selectedWorldStatusBadgeText.color = new Color(1.0f, 0.85f, 0.25f);
                }
                else
                {
                    selectedWorldStatusBadgeText.text = $"☠ LOCKED — Must complete World {config.unlockRequirement} first";
                    selectedWorldStatusBadgeText.color = new Color(0.9f, 0.25f, 0.25f);
                }
            }

            if (startRunButton != null)
            {
                startRunButton.interactable = isUnlocked;
            }

            if (startRunButtonText != null)
            {
                startRunButtonText.text = isUnlocked ? "ENTER WORLD TRIAL" : "LOCKED TRIAL";
                startRunButtonText.color = isUnlocked ? new Color(1f, 0.9f, 0.3f) : new Color(0.6f, 0.6f, 0.6f);
            }
        }

        private Color GetTierColor(string tier)
        {
            switch (tier?.ToUpperInvariant())
            {
                case "EASY": return new Color(0.4f, 0.95f, 0.45f);
                case "NORMAL": return new Color(1.0f, 0.85f, 0.25f);
                case "HARD": return new Color(1.0f, 0.55f, 0.15f);
                case "VERY HARD": return new Color(0.95f, 0.25f, 0.25f);
                case "NIGHTMARE": return new Color(0.85f, 0.25f, 0.95f);
                default: return Color.white;
            }
        }

        // ==================== DEBUG TOOLS (EDITOR-ONLY) ====================
#if UNITY_EDITOR
        public void DebugUnlockAll()
        {
            if (GameSession.Instance != null)
            {
                GameSession.Instance.UnlockAllWorlds();
            }
            RefreshWorldSelectionCards();
            UpdateWorldDetailsPanel(currentlySelectedWorldId);
        }

        public void DebugResetProgress()
        {
            if (GameSession.Instance != null)
            {
                GameSession.Instance.ResetProgress();
            }
            RefreshWorldSelectionCards();
            UpdateWorldDetailsPanel(currentlySelectedWorldId);
        }
#endif

        // ==================== EVENT LISTENER WIRING ====================

        private void WireEventListeners()
        {
            if (homePlayButton != null) { homePlayButton.onClick.RemoveAllListeners(); homePlayButton.onClick.AddListener(OpenWorldSelect); }
            if (homeUpgradesButton != null) { homeUpgradesButton.onClick.RemoveAllListeners(); homeUpgradesButton.onClick.AddListener(OpenUpgrades); }
            if (homeCreditsButton != null) { homeCreditsButton.onClick.RemoveAllListeners(); homeCreditsButton.onClick.AddListener(OpenCredits); }
            if (homeQuitButton != null) { homeQuitButton.onClick.RemoveAllListeners(); homeQuitButton.onClick.AddListener(QuitGame); }

            if (worldSelectBackButton != null) { worldSelectBackButton.onClick.RemoveAllListeners(); worldSelectBackButton.onClick.AddListener(OpenHome); }
            if (startRunButton != null) { startRunButton.onClick.RemoveAllListeners(); startRunButton.onClick.AddListener(StartSelectedWorld); }

#if UNITY_EDITOR
            if (debugUnlockAllButton != null) { debugUnlockAllButton.onClick.RemoveAllListeners(); debugUnlockAllButton.onClick.AddListener(DebugUnlockAll); }
            if (debugResetProgressButton != null) { debugResetProgressButton.onClick.RemoveAllListeners(); debugResetProgressButton.onClick.AddListener(DebugResetProgress); }
#else
            if (debugUnlockAllButton != null) debugUnlockAllButton.gameObject.SetActive(false);
            if (debugResetProgressButton != null) debugResetProgressButton.gameObject.SetActive(false);
#endif

            if (upgradesBackButton != null) { upgradesBackButton.onClick.RemoveAllListeners(); upgradesBackButton.onClick.AddListener(OpenHome); }
            if (creditsBackButton != null) { creditsBackButton.onClick.RemoveAllListeners(); creditsBackButton.onClick.AddListener(OpenHome); }

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
        }

        // ==================== PROCEDURAL UI BUILDER ====================

        public void EnsureMainMenuUI()
        {
            EnsureFont();

            // If already set up, skip
            if (homeScreenRoot != null && worldSelectScreenRoot != null) return;

            // Ensure EventSystem
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            // Ensure Main Camera
            if (Camera.main == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                Camera cam = camObj.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.04f, 0.03f, 0.05f); // Deep gothic black-violet
                camObj.transform.position = new Vector3(0, 1, -10);
            }

            // Ensure Canvas
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
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                canvasObj.AddComponent<GraphicRaycaster>();
                transform.SetParent(canvasObj.transform, false);
            }

            Transform root = canvas.transform;

            // Background Atmospheric Vignette
            GameObject bgObj = new GameObject("BackgroundVignette");
            bgObj.transform.SetParent(root, false);
            RectTransform bgRt = bgObj.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.06f, 0.04f, 0.05f, 1f);

            // 1. HOME SCREEN ROOT
            homeScreenRoot = CreateScreenPanel(root, "HomeScreenRoot");
            BuildHomeScreenContent(homeScreenRoot.transform);

            // 2. WORLD SELECT SCREEN ROOT
            worldSelectScreenRoot = CreateScreenPanel(root, "WorldSelectScreenRoot");
            BuildWorldSelectScreenContent(worldSelectScreenRoot.transform);

            // 3. UPGRADES SCREEN ROOT
            upgradesScreenRoot = CreateScreenPanel(root, "UpgradesScreenRoot");
            BuildUpgradesScreenContent(upgradesScreenRoot.transform);

            // 4. CREDITS SCREEN ROOT
            creditsScreenRoot = CreateScreenPanel(root, "CreditsScreenRoot");
            BuildCreditsScreenContent(creditsScreenRoot.transform);

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
            // Title Container
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(parent, false);
            RectTransform rtTitle = titleObj.AddComponent<RectTransform>();
            rtTitle.anchorMin = new Vector2(0.5f, 0.72f);
            rtTitle.anchorMax = new Vector2(0.5f, 0.72f);
            rtTitle.sizeDelta = new Vector2(900, 100);
            Text tTitle = titleObj.AddComponent<Text>();
            tTitle.font = cachedFont;
            tTitle.fontSize = 68;
            tTitle.fontStyle = FontStyle.Bold;
            tTitle.alignment = TextAnchor.MiddleCenter;
            tTitle.text = "CURSED HARVEST";
            tTitle.color = new Color(0.98f, 0.75f, 0.22f); // Golden amber

            // Subtitle
            GameObject subObj = new GameObject("SubtitleText");
            subObj.transform.SetParent(parent, false);
            RectTransform rtSub = subObj.AddComponent<RectTransform>();
            rtSub.anchorMin = new Vector2(0.5f, 0.63f);
            rtSub.anchorMax = new Vector2(0.5f, 0.63f);
            rtSub.sizeDelta = new Vector2(800, 45);
            Text tSub = subObj.AddComponent<Text>();
            tSub.font = cachedFont;
            tSub.fontSize = 26;
            tSub.alignment = TextAnchor.MiddleCenter;
            tSub.text = "A Folk Horror Survivor  •  The Five Blighted Realms";
            tSub.color = new Color(0.85f, 0.80f, 0.72f);

            // Buttons Container
            float startY = -40f;
            float spacing = 65f;

            homePlayButton = CreateMenuButton(parent, "PlayButton", "PLAY HARVEST TRIAL", new Vector2(0, startY), new Vector2(380, 54), new Color(0.85f, 0.55f, 0.15f));
            homeUpgradesButton = CreateMenuButton(parent, "UpgradesButton", "OCCULT UPGRADES", new Vector2(0, startY - spacing), new Vector2(380, 54), new Color(0.25f, 0.25f, 0.28f));
            homeCreditsButton = CreateMenuButton(parent, "CreditsButton", "CREDITS & FOLKLORE", new Vector2(0, startY - spacing * 2), new Vector2(380, 54), new Color(0.25f, 0.25f, 0.28f));
#if !UNITY_WEBGL
            homeQuitButton = CreateMenuButton(parent, "QuitButton", "QUIT GAME", new Vector2(0, startY - spacing * 3), new Vector2(380, 54), new Color(0.35f, 0.15f, 0.15f));
#endif

            // Version info at bottom
            GameObject verObj = new GameObject("VersionText");
            verObj.transform.SetParent(parent, false);
            RectTransform rtVer = verObj.AddComponent<RectTransform>();
            rtVer.anchorMin = new Vector2(0.5f, 0.05f);
            rtVer.anchorMax = new Vector2(0.5f, 0.05f);
            rtVer.sizeDelta = new Vector2(600, 30);
            Text tVer = verObj.AddComponent<Text>();
            tVer.font = cachedFont;
            tVer.fontSize = 16;
            tVer.alignment = TextAnchor.MiddleCenter;
            tVer.text = "Cursed Harvest v0.9.0 — Unity 6000 • 5 Worlds Architecture";
            tVer.color = new Color(0.5f, 0.45f, 0.4f);
        }

        private void BuildWorldSelectScreenContent(Transform parent)
        {
            worldCards.Clear();

            // Header Title
            GameObject titleObj = new GameObject("WorldSelectTitle");
            titleObj.transform.SetParent(parent, false);
            RectTransform rtTitle = titleObj.AddComponent<RectTransform>();
            rtTitle.anchorMin = new Vector2(0.5f, 0.92f);
            rtTitle.anchorMax = new Vector2(0.5f, 0.92f);
            rtTitle.sizeDelta = new Vector2(800, 60);
            Text tTitle = titleObj.AddComponent<Text>();
            tTitle.font = cachedFont;
            tTitle.fontSize = 38;
            tTitle.fontStyle = FontStyle.Bold;
            tTitle.alignment = TextAnchor.MiddleCenter;
            tTitle.text = "SELECT WORLD DESTINATION";
            tTitle.color = new Color(0.98f, 0.78f, 0.25f);

            // Subtitle
            GameObject subObj = new GameObject("WorldSelectSubtitle");
            subObj.transform.SetParent(parent, false);
            RectTransform rtSub = subObj.AddComponent<RectTransform>();
            rtSub.anchorMin = new Vector2(0.5f, 0.86f);
            rtSub.anchorMax = new Vector2(0.5f, 0.86f);
            rtSub.sizeDelta = new Vector2(800, 35);
            Text tSub = subObj.AddComponent<Text>();
            tSub.font = cachedFont;
            tSub.fontSize = 20;
            tSub.alignment = TextAnchor.MiddleCenter;
            tSub.text = "Consecrate each realm by slaying its guardian before the blighted night ends";
            tSub.color = new Color(0.8f, 0.75f, 0.7f);

            // World Cards Horizontal Bar (Top half: y = 0.68)
            float totalWidth = 1450f;
            float cardWidth = 270f;
            float cardHeight = 160f;
            float spacing = (totalWidth - (cardWidth * 5)) / 4f;
            float startX = -totalWidth * 0.5f + cardWidth * 0.5f;

            for (int i = 1; i <= 5; i++)
            {
                int worldId = i;
                WorldConfig cfg = WorldConfig.GetConfig(worldId);

                GameObject cardObj = new GameObject($"WorldCard_{worldId}");
                cardObj.transform.SetParent(parent, false);
                RectTransform rtCard = cardObj.AddComponent<RectTransform>();
                rtCard.anchorMin = new Vector2(0.5f, 0.68f);
                rtCard.anchorMax = new Vector2(0.5f, 0.68f);
                rtCard.sizeDelta = new Vector2(cardWidth, cardHeight);
                rtCard.anchoredPosition = new Vector2(startX + (i - 1) * (cardWidth + spacing), 0);

                Image cardBg = cardObj.AddComponent<Image>();
                cardBg.color = new Color(0.12f, 0.10f, 0.09f, 0.90f);

                // Highlight border
                GameObject borderObj = new GameObject("Border");
                borderObj.transform.SetParent(cardObj.transform, false);
                RectTransform rtBorder = borderObj.AddComponent<RectTransform>();
                rtBorder.anchorMin = Vector2.zero;
                rtBorder.anchorMax = Vector2.one;
                rtBorder.sizeDelta = new Vector2(4, 4);
                Image borderImg = borderObj.AddComponent<Image>();
                borderImg.color = new Color(0.4f, 0.35f, 0.25f, 0.5f);
                borderObj.transform.SetAsFirstSibling();

                Button btn = cardObj.AddComponent<Button>();
                ColorBlock cb = btn.colors;
                cb.normalColor = Color.white;
                cb.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
                cb.pressedColor = new Color(0.8f, 0.8f, 0.8f);
                btn.colors = cb;

                // Card Texts
                GameObject wNumObj = new GameObject("WorldNumber");
                wNumObj.transform.SetParent(cardObj.transform, false);
                RectTransform rtWNum = wNumObj.AddComponent<RectTransform>();
                rtWNum.anchorMin = new Vector2(0, 0.72f);
                rtWNum.anchorMax = new Vector2(1, 0.95f);
                rtWNum.offsetMin = rtWNum.offsetMax = Vector2.zero;
                Text tWNum = wNumObj.AddComponent<Text>();
                tWNum.font = cachedFont;
                tWNum.fontSize = 18;
                tWNum.fontStyle = FontStyle.Bold;
                tWNum.alignment = TextAnchor.MiddleCenter;
                tWNum.text = $"WORLD {worldId}";
                tWNum.color = new Color(0.95f, 0.8f, 0.35f);

                GameObject wNameObj = new GameObject("WorldName");
                wNameObj.transform.SetParent(cardObj.transform, false);
                RectTransform rtWName = wNameObj.AddComponent<RectTransform>();
                rtWName.anchorMin = new Vector2(0.05f, 0.40f);
                rtWName.anchorMax = new Vector2(0.95f, 0.72f);
                rtWName.offsetMin = rtWName.offsetMax = Vector2.zero;
                Text tWName = wNameObj.AddComponent<Text>();
                tWName.font = cachedFont;
                tWName.fontSize = 16;
                tWName.fontStyle = FontStyle.Bold;
                tWName.alignment = TextAnchor.MiddleCenter;
                tWName.text = cfg != null ? cfg.displayName : $"Realm {worldId}";
                tWName.color = Color.white;

                GameObject diffObj = new GameObject("DifficultyBadge");
                diffObj.transform.SetParent(cardObj.transform, false);
                RectTransform rtDiff = diffObj.AddComponent<RectTransform>();
                rtDiff.anchorMin = new Vector2(0.05f, 0.22f);
                rtDiff.anchorMax = new Vector2(0.95f, 0.40f);
                rtDiff.offsetMin = rtDiff.offsetMax = Vector2.zero;
                Text tDiff = diffObj.AddComponent<Text>();
                tDiff.font = cachedFont;
                tDiff.fontSize = 14;
                tDiff.alignment = TextAnchor.MiddleCenter;
                tDiff.text = cfg != null ? cfg.difficultyTier : "NORMAL";
                tDiff.color = GetTierColor(cfg != null ? cfg.difficultyTier : "");

                GameObject statObj = new GameObject("StatusBadge");
                statObj.transform.SetParent(cardObj.transform, false);
                RectTransform rtStat = statObj.AddComponent<RectTransform>();
                rtStat.anchorMin = new Vector2(0.05f, 0.05f);
                rtStat.anchorMax = new Vector2(0.95f, 0.22f);
                rtStat.offsetMin = rtStat.offsetMax = Vector2.zero;
                Text tStat = statObj.AddComponent<Text>();
                tStat.font = cachedFont;
                tStat.fontSize = 13;
                tStat.alignment = TextAnchor.MiddleCenter;
                tStat.text = worldId == 1 ? "UNLOCKED" : $"LOCKED";
                tStat.color = worldId == 1 ? new Color(1f, 0.85f, 0.25f) : new Color(0.7f, 0.25f, 0.25f);

                WorldCardUIElements elem = new WorldCardUIElements
                {
                    worldId = worldId,
                    cardRoot = cardObj,
                    selectButton = btn,
                    worldNumberText = tWNum,
                    worldNameText = tWName,
                    difficultyBadgeText = tDiff,
                    statusBadgeText = tStat,
                    highlightBorder = borderImg,
                    background = cardBg
                };
                worldCards.Add(elem);
            }

            // DETAILS PANEL (Center: y = 0.36)
            GameObject detailsObj = new GameObject("WorldDetailsPanel");
            detailsObj.transform.SetParent(parent, false);
            RectTransform rtDetails = detailsObj.AddComponent<RectTransform>();
            rtDetails.anchorMin = new Vector2(0.5f, 0.36f);
            rtDetails.anchorMax = new Vector2(0.5f, 0.36f);
            rtDetails.sizeDelta = new Vector2(1450, 240);
            Image detailsBg = detailsObj.AddComponent<Image>();
            detailsBg.color = new Color(0.09f, 0.08f, 0.09f, 0.92f);

            // Details title
            GameObject dTitleObj = new GameObject("DetailTitle");
            dTitleObj.transform.SetParent(detailsObj.transform, false);
            RectTransform rtDTitle = dTitleObj.AddComponent<RectTransform>();
            rtDTitle.anchorMin = new Vector2(0.03f, 0.78f);
            rtDTitle.anchorMax = new Vector2(0.70f, 0.95f);
            rtDTitle.offsetMin = rtDTitle.offsetMax = Vector2.zero;
            selectedWorldTitleText = dTitleObj.AddComponent<Text>();
            selectedWorldTitleText.font = cachedFont;
            selectedWorldTitleText.fontSize = 28;
            selectedWorldTitleText.fontStyle = FontStyle.Bold;
            selectedWorldTitleText.alignment = TextAnchor.MiddleLeft;

            // Details subtitle
            GameObject dSubObj = new GameObject("DetailSubtitle");
            dSubObj.transform.SetParent(detailsObj.transform, false);
            RectTransform rtDSub = dSubObj.AddComponent<RectTransform>();
            rtDSub.anchorMin = new Vector2(0.03f, 0.62f);
            rtDSub.anchorMax = new Vector2(0.70f, 0.78f);
            rtDSub.offsetMin = rtDSub.offsetMax = Vector2.zero;
            selectedWorldSubtitleText = dSubObj.AddComponent<Text>();
            selectedWorldSubtitleText.font = cachedFont;
            selectedWorldSubtitleText.fontSize = 18;
            selectedWorldSubtitleText.color = new Color(0.8f, 0.75f, 0.65f);
            selectedWorldSubtitleText.alignment = TextAnchor.MiddleLeft;

            // Details Tier
            GameObject dTierObj = new GameObject("DetailTier");
            dTierObj.transform.SetParent(detailsObj.transform, false);
            RectTransform rtDTier = dTierObj.AddComponent<RectTransform>();
            rtDTier.anchorMin = new Vector2(0.70f, 0.78f);
            rtDTier.anchorMax = new Vector2(0.97f, 0.95f);
            rtDTier.offsetMin = rtDTier.offsetMax = Vector2.zero;
            selectedWorldTierText = dTierObj.AddComponent<Text>();
            selectedWorldTierText.font = cachedFont;
            selectedWorldTierText.fontSize = 22;
            selectedWorldTierText.fontStyle = FontStyle.Bold;
            selectedWorldTierText.alignment = TextAnchor.MiddleRight;

            // Status Badge in details panel
            GameObject dStatObj = new GameObject("DetailStatus");
            dStatObj.transform.SetParent(detailsObj.transform, false);
            RectTransform rtDStat = dStatObj.AddComponent<RectTransform>();
            rtDStat.anchorMin = new Vector2(0.70f, 0.62f);
            rtDStat.anchorMax = new Vector2(0.97f, 0.78f);
            rtDStat.offsetMin = rtDStat.offsetMax = Vector2.zero;
            selectedWorldStatusBadgeText = dStatObj.AddComponent<Text>();
            selectedWorldStatusBadgeText.font = cachedFont;
            selectedWorldStatusBadgeText.fontSize = 18;
            selectedWorldStatusBadgeText.fontStyle = FontStyle.Bold;
            selectedWorldStatusBadgeText.alignment = TextAnchor.MiddleRight;

            // Description Lore text
            GameObject dDescObj = new GameObject("DetailDesc");
            dDescObj.transform.SetParent(detailsObj.transform, false);
            RectTransform rtDDesc = dDescObj.AddComponent<RectTransform>();
            rtDDesc.anchorMin = new Vector2(0.03f, 0.35f);
            rtDDesc.anchorMax = new Vector2(0.55f, 0.60f);
            rtDDesc.offsetMin = rtDDesc.offsetMax = Vector2.zero;
            selectedWorldDescText = dDescObj.AddComponent<Text>();
            selectedWorldDescText.font = cachedFont;
            selectedWorldDescText.fontSize = 16;
            selectedWorldDescText.color = new Color(0.85f, 0.85f, 0.85f);
            selectedWorldDescText.alignment = TextAnchor.UpperLeft;

            // Wave count text
            GameObject dWaveObj = new GameObject("DetailWaveCount");
            dWaveObj.transform.SetParent(detailsObj.transform, false);
            RectTransform rtDWave = dWaveObj.AddComponent<RectTransform>();
            rtDWave.anchorMin = new Vector2(0.03f, 0.10f);
            rtDWave.anchorMax = new Vector2(0.55f, 0.32f);
            rtDWave.offsetMin = rtDWave.offsetMax = Vector2.zero;
            selectedWorldWaveCountText = dWaveObj.AddComponent<Text>();
            selectedWorldWaveCountText.font = cachedFont;
            selectedWorldWaveCountText.fontSize = 17;
            selectedWorldWaveCountText.fontStyle = FontStyle.Bold;
            selectedWorldWaveCountText.color = new Color(0.95f, 0.8f, 0.3f);
            selectedWorldWaveCountText.alignment = TextAnchor.MiddleLeft;

            // Boss Encounter summary
            GameObject dBossObj = new GameObject("DetailBossSummary");
            dBossObj.transform.SetParent(detailsObj.transform, false);
            RectTransform rtDBoss = dBossObj.AddComponent<RectTransform>();
            rtDBoss.anchorMin = new Vector2(0.58f, 0.10f);
            rtDBoss.anchorMax = new Vector2(0.97f, 0.58f);
            rtDBoss.offsetMin = rtDBoss.offsetMax = Vector2.zero;
            selectedWorldBossText = dBossObj.AddComponent<Text>();
            selectedWorldBossText.font = cachedFont;
            selectedWorldBossText.fontSize = 15;
            selectedWorldBossText.color = new Color(0.9f, 0.7f, 0.6f);
            selectedWorldBossText.alignment = TextAnchor.UpperLeft;

            // BOTTOM ACTION BAR (y = 0.12)
            worldSelectBackButton = CreateMenuButton(parent, "BackButton", "← RETURN TO HOME", new Vector2(-280, -420), new Vector2(280, 52), new Color(0.25f, 0.25f, 0.25f));
            startRunButton = CreateMenuButton(parent, "StartRunButton", "ENTER WORLD TRIAL", new Vector2(280, -420), new Vector2(360, 56), new Color(0.85f, 0.55f, 0.15f));
            startRunButtonText = startRunButton.GetComponentInChildren<Text>();

#if UNITY_EDITOR
            // Small Debug Controls in bottom corner (Editor-Only)
            debugUnlockAllButton = CreateMenuButton(parent, "DebugUnlockAll", "[DEBUG] UNLOCK ALL 5", new Vector2(720, -500), new Vector2(200, 32), new Color(0.2f, 0.4f, 0.2f));
            Text tD1 = debugUnlockAllButton.GetComponentInChildren<Text>();
            if (tD1 != null) tD1.fontSize = 12;

            debugResetProgressButton = CreateMenuButton(parent, "DebugResetProgress", "[DEBUG] RESET TO W1", new Vector2(500, -500), new Vector2(200, 32), new Color(0.4f, 0.2f, 0.2f));
            Text tD2 = debugResetProgressButton.GetComponentInChildren<Text>();
            if (tD2 != null) tD2.fontSize = 12;
#endif
        }

        private void BuildUpgradesScreenContent(Transform parent)
        {
            // Title
            GameObject titleObj = new GameObject("UpgradesTitle");
            titleObj.transform.SetParent(parent, false);
            RectTransform rtTitle = titleObj.AddComponent<RectTransform>();
            rtTitle.anchorMin = new Vector2(0.5f, 0.90f);
            rtTitle.anchorMax = new Vector2(0.5f, 0.90f);
            rtTitle.sizeDelta = new Vector2(900, 60);
            Text tTitle = titleObj.AddComponent<Text>();
            tTitle.font = cachedFont;
            tTitle.fontSize = 38;
            tTitle.fontStyle = FontStyle.Bold;
            tTitle.alignment = TextAnchor.MiddleCenter;
            tTitle.text = "ANCIENT RELICS & OCCULT UPGRADES";
            tTitle.color = new Color(0.98f, 0.78f, 0.25f);

            // Subtitle
            GameObject subObj = new GameObject("UpgradesSubtitle");
            subObj.transform.SetParent(parent, false);
            RectTransform rtSub = subObj.AddComponent<RectTransform>();
            rtSub.anchorMin = new Vector2(0.5f, 0.83f);
            rtSub.anchorMax = new Vector2(0.5f, 0.83f);
            rtSub.sizeDelta = new Vector2(900, 35);
            Text tSub = subObj.AddComponent<Text>();
            tSub.font = cachedFont;
            tSub.fontSize = 20;
            tSub.alignment = TextAnchor.MiddleCenter;
            tSub.text = "Permanent boons consecrated from blighted seeds across the five realms";
            tSub.color = new Color(0.8f, 0.75f, 0.7f);

            // 4 Placeholder Relic Cards Grid
            string[] relicNames = new string[] { "Heart of the Soil", "Quicksilver Quiver", "Witchbane Talisman", "Reaper's Fortune" };
            string[] relicDescs = new string[] {
                "+20 Maximum Health\n+15% Health Drop Frequency",
                "+12% Crossbow Fire Rate\n+20% Bolt Velocity",
                "+25% Area of Effect\n+30% Knockback Resistance",
                "+3.0s Lucky Last Duration\n+10% Seed Harvest Yield"
            };

            float gridWidth = 1100f;
            float cWidth = 500f;
            float cHeight = 150f;

            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0) ? -280f : 280f;
                float y = (i < 2) ? 90f : -90f;

                GameObject cObj = new GameObject($"RelicCard_{i}");
                cObj.transform.SetParent(parent, false);
                RectTransform rt = cObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.50f);
                rt.anchorMax = new Vector2(0.5f, 0.50f);
                rt.sizeDelta = new Vector2(cWidth, cHeight);
                rt.anchoredPosition = new Vector2(x, y);

                Image bg = cObj.AddComponent<Image>();
                bg.color = new Color(0.12f, 0.10f, 0.12f, 0.90f);

                // Relic Name
                GameObject nObj = new GameObject("Name");
                nObj.transform.SetParent(cObj.transform, false);
                RectTransform rtN = nObj.AddComponent<RectTransform>();
                rtN.anchorMin = new Vector2(0.06f, 0.65f);
                rtN.anchorMax = new Vector2(0.94f, 0.92f);
                rtN.offsetMin = rtN.offsetMax = Vector2.zero;
                Text tN = nObj.AddComponent<Text>();
                tN.font = cachedFont;
                tN.fontSize = 20;
                tN.fontStyle = FontStyle.Bold;
                tN.text = relicNames[i];
                tN.color = new Color(0.95f, 0.8f, 0.35f);

                // Relic stats
                GameObject sObj = new GameObject("Stats");
                sObj.transform.SetParent(cObj.transform, false);
                RectTransform rtS = sObj.AddComponent<RectTransform>();
                rtS.anchorMin = new Vector2(0.06f, 0.25f);
                rtS.anchorMax = new Vector2(0.94f, 0.65f);
                rtS.offsetMin = rtS.offsetMax = Vector2.zero;
                Text tS = sObj.AddComponent<Text>();
                tS.font = cachedFont;
                tS.fontSize = 15;
                tS.text = relicDescs[i];
                tS.color = new Color(0.8f, 0.8f, 0.8f);

                // Consecration status
                GameObject statObj = new GameObject("Status");
                statObj.transform.SetParent(cObj.transform, false);
                RectTransform rtStat = statObj.AddComponent<RectTransform>();
                rtStat.anchorMin = new Vector2(0.06f, 0.05f);
                rtStat.anchorMax = new Vector2(0.94f, 0.25f);
                rtStat.offsetMin = rtStat.offsetMax = Vector2.zero;
                Text tStat = statObj.AddComponent<Text>();
                tStat.font = cachedFont;
                tStat.fontSize = 13;
                tStat.text = "UNDER CONSECRATION (COMING SOON)";
                tStat.color = new Color(0.7f, 0.6f, 0.3f);
            }

            // Back button
            upgradesBackButton = CreateMenuButton(parent, "UpgradesBackButton", "← RETURN TO HOME", new Vector2(0, -420), new Vector2(280, 52), new Color(0.25f, 0.25f, 0.25f));
        }

        private void BuildCreditsScreenContent(Transform parent)
        {
            // Title
            GameObject titleObj = new GameObject("CreditsTitle");
            titleObj.transform.SetParent(parent, false);
            RectTransform rtTitle = titleObj.AddComponent<RectTransform>();
            rtTitle.anchorMin = new Vector2(0.5f, 0.88f);
            rtTitle.anchorMax = new Vector2(0.5f, 0.88f);
            rtTitle.sizeDelta = new Vector2(800, 60);
            Text tTitle = titleObj.AddComponent<Text>();
            tTitle.font = cachedFont;
            tTitle.fontSize = 38;
            tTitle.fontStyle = FontStyle.Bold;
            tTitle.alignment = TextAnchor.MiddleCenter;
            tTitle.text = "CURSED HARVEST — CREDITS";
            tTitle.color = new Color(0.98f, 0.78f, 0.25f);

            // Credits Body
            GameObject bodyObj = new GameObject("CreditsBody");
            bodyObj.transform.SetParent(parent, false);
            RectTransform rtBody = bodyObj.AddComponent<RectTransform>();
            rtBody.anchorMin = new Vector2(0.5f, 0.50f);
            rtBody.anchorMax = new Vector2(0.5f, 0.50f);
            rtBody.sizeDelta = new Vector2(900, 360);
            Text tBody = bodyObj.AddComponent<Text>();
            tBody.font = cachedFont;
            tBody.fontSize = 18;
            tBody.lineSpacing = 1.3f;
            tBody.alignment = TextAnchor.MiddleCenter;
            tBody.text = 
                "CURSED HARVEST\nA Folk Horror Roguelite Survival Experience\n\n" +
                "Core Gameplay & Systems: Antigravity Agentic Pair Programming\n" +
                "Art Assets & Modular Environments: Synty Studios / POLYGON Series\n" +
                "Horror Masks & Props: POLYGON Halloween Horror Collection\n" +
                "Audio & Folk Atmosphere: Folk Acoustic & Ambient Synthesis\n\n" +
                "Special thanks to the brave souls who brave the Blighted Soil.\n" +
                "May the Harvest treat you with mercy.";
            tBody.color = new Color(0.85f, 0.82f, 0.78f);

            // Back button
            creditsBackButton = CreateMenuButton(parent, "CreditsBackButton", "← RETURN TO HOME", new Vector2(0, -420), new Vector2(280, 52), new Color(0.25f, 0.25f, 0.25f));
        }

        private Button CreateMenuButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color bgColor)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;

            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.25f, 1.25f, 1.25f);
            cb.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            cb.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
            btn.colors = cb;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform rtT = textObj.AddComponent<RectTransform>();
            rtT.anchorMin = Vector2.zero;
            rtT.anchorMax = Vector2.one;
            rtT.sizeDelta = Vector2.zero;

            Text t = textObj.AddComponent<Text>();
            t.font = cachedFont;
            t.fontSize = 20;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.text = label;
            t.color = Color.white;

            return btn;
        }
    }
}
