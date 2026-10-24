using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CursedHarvest
{
    /// <summary>
    /// Master Mobile Portrait Frontend controller managing HOME, WORLD SELECT, UPGRADES, SETTINGS, and CREDITS screens.
    /// Built around a 1080 x 1920 Portrait reference canvas with Safe Area binding, real 3D folk-horror diorama backdrop,
    /// vertical scrollable 5-World cards with procedural biome thumbnails, and a 2x4 Meta Upgrades grid.
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
        [SerializeField] private GameObject settingsModalRoot;
        [SerializeField] private GameObject exitConfirmModalRoot;

        [Header("Home Screen UI")]
        [SerializeField] private Button homePlayButton;
        [SerializeField] private Button homeUpgradesButton;
        [SerializeField] private Button homeSettingsButton;
        [SerializeField] private Button homeCreditsButton;
        [SerializeField] private Button homeQuitButton;
        [SerializeField] private Text homeEssenceBadgeText;

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
            public Text waveAndBossSummaryText;
            public Text statusBadgeText;
            public Image highlightBorder;
            public Image background;
            public Image previewThumbnail;
        }

        private int currentlySelectedWorldId = 1;
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
            Ensure3DPortraitBackdrop();
            EnsureMainMenuUI();
            WireEventListeners();
        }

        private void EnsureAudioSetup()
        {
            if (FindFirstObjectByType<AudioListener>() == null)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    cam.gameObject.AddComponent<AudioListener>();
                }
            }
        }

        private void Start()
        {
            Time.timeScale = 1f;

            if (homeQuitButton != null)
            {
                homeQuitButton.gameObject.SetActive(false);
            }

            if (GameSession.Instance != null)
            {
                currentlySelectedWorldId = GameSession.Instance.SelectedWorldId;
            }

            MainMenuScreen startScreen = MainMenuScreen.Home;
            if (GameSession.Instance != null)
            {
                startScreen = GameSession.Instance.InitialMenuScreen;
            }

            ShowScreen(startScreen);
            RefreshWorldSelectionCards();
            UpdateWorldDetailsPanel(currentlySelectedWorldId);
            RefreshUpgradesUI();
            RefreshSettingsModalLabels();
        }

        private void Update()
        {
            // Subtle breathing/levitation animation on the 3D Portrait altar centerpiece
            if (floatingAltarPumpkin != null)
            {
                float t = Time.unscaledTime;
                floatingAltarPumpkin.localPosition = new Vector3(0f, 1.38f + Mathf.Sin(t * 1.8f) * 0.08f, 0f);
                floatingAltarPumpkin.localRotation = Quaternion.Euler(6f * Mathf.Sin(t * 0.9f), 180f + Mathf.Sin(t * 0.65f) * 11f, 0f);
            }
            if (altarPointLight != null)
            {
                altarPointLight.intensity = 3.8f + Mathf.Sin(Time.unscaledTime * 3.1f) * 0.45f;
            }

            // Android Back Button / Escape Navigation
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

            if ((worldSelectScreenRoot != null && worldSelectScreenRoot.activeSelf) ||
                (upgradesScreenRoot != null && upgradesScreenRoot.activeSelf) ||
                (creditsScreenRoot != null && creditsScreenRoot.activeSelf))
            {
                OpenHome();
                return;
            }

            if (homeScreenRoot != null && homeScreenRoot.activeSelf)
            {
                OpenExitConfirmation();
            }
        }

        private void OnEnable()
        {
            GameSession.OnWorldProgressionChanged += HandleProgressionChanged;
            MetaProgressionManager.OnEssenceChanged += HandleEssenceChanged;
            MetaProgressionManager.OnUpgradePurchased += HandleUpgradePurchased;
        }

        private void OnDisable()
        {
            GameSession.OnWorldProgressionChanged -= HandleProgressionChanged;
            MetaProgressionManager.OnEssenceChanged -= HandleEssenceChanged;
            MetaProgressionManager.OnUpgradePurchased -= HandleUpgradePurchased;
        }

        private void HandleProgressionChanged(int highestUnlocked)
        {
            RefreshWorldSelectionCards();
            UpdateWorldDetailsPanel(currentlySelectedWorldId);
        }

        private void HandleEssenceChanged(int essence)
        {
            RefreshUpgradesUI();
        }

        private void HandleUpgradePurchased(MetaUpgradeBranch branch, int rank)
        {
            RefreshUpgradesUI();
        }

        private void EnsureFont()
        {
            if (cachedFont == null)
            {
                cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (cachedFont == null)
                {
                    cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
            }
        }

        // ==================== REAL 3D PORTRAIT BACKDROP DIORAMA ====================

        public void Ensure3DPortraitBackdrop()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                cam = camObj.AddComponent<Camera>();
            }

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.035f, 0.028f, 0.048f, 1f);
            cam.orthographic = false;
            cam.fieldOfView = 46f;
            // Frame the altar & ritual clearing in the upper-middle of the Portrait 9:16 screen
            cam.transform.position = new Vector3(0f, 3.15f, -7.8f);
            cam.transform.rotation = Quaternion.Euler(14.5f, 0f, 0f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.05f, 0.04f, 0.07f, 1f);
            RenderSettings.fogDensity = 0.045f;
            RenderSettings.ambientLight = new Color(0.16f, 0.14f, 0.22f, 1f);

            GameObject existing = GameObject.Find("MenuPortraitDiorama_3D");
            if (existing != null)
            {
                backdropDioramaRoot = existing;
                Transform foundPumpkin = existing.transform.Find("AltarFocalGroup/CursedPumpkinHead");
                if (foundPumpkin != null) floatingAltarPumpkin = foundPumpkin;
                Transform foundLight = existing.transform.Find("AltarFocalGroup/AltarGlowLight");
                if (foundLight != null) altarPointLight = foundLight.GetComponent<Light>();
                return;
            }

            backdropDioramaRoot = new GameObject("MenuPortraitDiorama_3D");

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            Material soilMat = CreateDioramaMaterial(litShader, new Color(0.08f, 0.065f, 0.06f), 0.1f);
            Material stoneMat = CreateDioramaMaterial(litShader, new Color(0.20f, 0.19f, 0.23f), 0.2f);
            Material woodMat = CreateDioramaMaterial(litShader, new Color(0.15f, 0.10f, 0.08f), 0.15f);
            Material pumpkinMat = CreateDioramaMaterial(litShader, new Color(0.88f, 0.36f, 0.06f), 0.35f);
            Material glowMat = CreateDioramaEmissiveMaterial(litShader, new Color(1.0f, 0.76f, 0.18f), new Color(1.0f, 0.65f, 0.12f) * 3.2f);
            Material bladeMat = CreateDioramaMaterial(litShader, new Color(0.62f, 0.68f, 0.76f), 0.75f);

            // Ground Mound
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ground.name = "CursedClearingGround";
            ground.transform.SetParent(backdropDioramaRoot.transform, false);
            ground.transform.localPosition = new Vector3(0f, -0.45f, 1.5f);
            ground.transform.localScale = new Vector3(18f, 0.5f, 18f);
            ground.GetComponent<Renderer>().sharedMaterial = soilMat;
            DestroyCollider(ground);

            // Altar Focal Group (positioned at y=2.2..3.6 in world so it sits in the upper clearing of Portrait screen)
            GameObject altarGroup = new GameObject("AltarFocalGroup");
            altarGroup.transform.SetParent(backdropDioramaRoot.transform, false);
            altarGroup.transform.localPosition = new Vector3(0f, 1.45f, 0.6f);

            // Stepped Stone Altar Plinth
            CreateBlock(altarGroup.transform, "AltarBaseStep1", new Vector3(0f, 0.18f, 0f), new Vector3(2.5f, 0.36f, 1.8f), stoneMat);
            CreateBlock(altarGroup.transform, "AltarBaseStep2", new Vector3(0f, 0.52f, 0f), new Vector3(1.9f, 0.38f, 1.4f), stoneMat);
            CreateBlock(altarGroup.transform, "AltarPedestal", new Vector3(0f, 0.88f, 0f), new Vector3(1.35f, 0.42f, 1.1f), stoneMat);

            // Glowing Rune Trim on Altar
            CreateBlock(altarGroup.transform, "AltarRuneBand", new Vector3(0f, 0.92f, -0.56f), new Vector3(1.05f, 0.09f, 0.04f), glowMat);

            // Cursed Pumpkin Centerpiece
            GameObject pumpkinRoot = new GameObject("CursedPumpkinHead");
            pumpkinRoot.transform.SetParent(altarGroup.transform, false);
            pumpkinRoot.transform.localPosition = new Vector3(0f, 1.38f, 0f);
            pumpkinRoot.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            floatingAltarPumpkin = pumpkinRoot.transform;

            GameObject pCore = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pCore.name = "PumpkinRind";
            pCore.transform.SetParent(pumpkinRoot.transform, false);
            pCore.transform.localScale = new Vector3(1.12f, 0.92f, 1.05f);
            pCore.GetComponent<Renderer>().sharedMaterial = pumpkinMat;
            DestroyCollider(pCore);

            // Pumpkin ribs for stylized silhouette
            for (int r = 0; r < 4; r++)
            {
                GameObject rib = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rib.name = $"PumpkinRib_{r}";
                rib.transform.SetParent(pumpkinRoot.transform, false);
                rib.transform.localRotation = Quaternion.Euler(0f, r * 45f, 0f);
                rib.transform.localScale = new Vector3(1.18f, 0.90f, 0.72f);
                rib.GetComponent<Renderer>().sharedMaterial = pumpkinMat;
                DestroyCollider(rib);
            }

            // Stem
            CreateBlock(pumpkinRoot.transform, "PumpkinStem", new Vector3(0f, 0.52f, 0f), new Vector3(0.16f, 0.28f, 0.16f), woodMat, Quaternion.Euler(0f, 0f, -14f));

            // Glowing Carved Eyes & Grin (facing camera at -Z, since pumpkinRoot is rotated 180° Y, local +Z faces camera)
            CreateBlock(pumpkinRoot.transform, "LeftEyeGlow", new Vector3(-0.22f, 0.10f, 0.48f), new Vector3(0.20f, 0.16f, 0.12f), glowMat, Quaternion.Euler(0f, 0f, -12f));
            CreateBlock(pumpkinRoot.transform, "RightEyeGlow", new Vector3(0.22f, 0.10f, 0.48f), new Vector3(0.20f, 0.16f, 0.12f), glowMat, Quaternion.Euler(0f, 0f, 12f));
            CreateBlock(pumpkinRoot.transform, "MouthGlowCenter", new Vector3(0f, -0.16f, 0.49f), new Vector3(0.52f, 0.11f, 0.12f), glowMat);

            // Crossed Harvest Scythe behind the Pumpkin
            GameObject scytheGroup = new GameObject("CrossedHarvestScythe");
            scytheGroup.transform.SetParent(altarGroup.transform, false);
            scytheGroup.transform.localPosition = new Vector3(0.1f, 1.45f, 0.35f);
            scytheGroup.transform.localRotation = Quaternion.Euler(0f, 0f, -28f);

            CreateBlock(scytheGroup.transform, "ScytheShaft", new Vector3(0f, 0f, 0f), new Vector3(0.11f, 3.1f, 0.11f), woodMat);
            CreateBlock(scytheGroup.transform, "ScytheBlade", new Vector3(0.68f, 1.32f, 0f), new Vector3(1.45f, 0.22f, 0.05f), bladeMat, Quaternion.Euler(0f, 0f, -18f));
            CreateBlock(scytheGroup.transform, "ScytheRuneEdge", new Vector3(0.66f, 1.24f, -0.03f), new Vector3(1.25f, 0.05f, 0.05f), glowMat, Quaternion.Euler(0f, 0f, -18f));

            // Framing Standing Stones & Gnarled Trees on Left & Right edges of Portrait viewport
            CreateStandingMonolith(backdropDioramaRoot.transform, new Vector3(-2.35f, 1.4f, 1.8f), new Vector3(0f, 18f, -5f), 2.8f, stoneMat, glowMat);
            CreateStandingMonolith(backdropDioramaRoot.transform, new Vector3(2.35f, 1.4f, 1.8f), new Vector3(0f, -18f, 5f), 2.8f, stoneMat, glowMat);
            CreateGnarledTreeSilhouette(backdropDioramaRoot.transform, new Vector3(-3.2f, 1.8f, 3.8f), -1f, woodMat);
            CreateGnarledTreeSilhouette(backdropDioramaRoot.transform, new Vector3(3.2f, 1.8f, 3.8f), 1f, woodMat);

            // Altar Warm Point Light
            GameObject lightObj = new GameObject("AltarGlowLight");
            lightObj.transform.SetParent(altarGroup.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 1.45f, -0.85f);
            altarPointLight = lightObj.AddComponent<Light>();
            altarPointLight.type = LightType.Point;
            altarPointLight.color = new Color(1.0f, 0.62f, 0.16f);
            altarPointLight.range = 9.5f;
            altarPointLight.intensity = 3.8f;

            // Cool Rim Moonlight
            GameObject rimLightObj = new GameObject("MoonRimLight");
            rimLightObj.transform.SetParent(backdropDioramaRoot.transform, false);
            rimLightObj.transform.localPosition = new Vector3(-2.5f, 6.0f, 4.5f);
            rimLightObj.transform.rotation = Quaternion.Euler(45f, -155f, 0f);
            Light rimLight = rimLightObj.AddComponent<Light>();
            rimLight.type = LightType.Directional;
            rimLight.color = new Color(0.38f, 0.46f, 0.72f);
            rimLight.intensity = 1.15f;
        }

        private Material CreateDioramaMaterial(Shader shader, Color color, float smoothness)
        {
            Material m = new Material(shader);
            m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            return m;
        }

        private Material CreateDioramaEmissiveMaterial(Shader shader, Color baseCol, Color emissionCol)
        {
            Material m = CreateDioramaMaterial(shader, baseCol, 0.5f);
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emissionCol);
            return m;
        }

        private void CreateBlock(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat, Quaternion localRot = default)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = (localRot.x == 0f && localRot.y == 0f && localRot.z == 0f && localRot.w == 0f) ? Quaternion.identity : localRot;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            DestroyCollider(go);
        }

        private void CreateStandingMonolith(Transform parent, Vector3 pos, Vector3 euler, float height, Material stoneMat, Material glowMat)
        {
            GameObject root = new GameObject("RuneMonolith");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;
            root.transform.localRotation = Quaternion.Euler(euler);
            CreateBlock(root.transform, "StoneBody", new Vector3(0f, height * 0.5f, 0f), new Vector3(0.68f, height, 0.42f), stoneMat);
            CreateBlock(root.transform, "RuneCarving", new Vector3(0f, height * 0.55f, -0.22f), new Vector3(0.14f, height * 0.42f, 0.04f), glowMat);
        }

        private void CreateGnarledTreeSilhouette(Transform parent, Vector3 pos, float sideSign, Material woodMat)
        {
            GameObject tree = new GameObject("GnarledBloodwoodTree");
            tree.transform.SetParent(parent, false);
            tree.transform.localPosition = pos;
            CreateBlock(tree.transform, "Trunk", new Vector3(0f, 2.2f, 0f), new Vector3(0.55f, 4.4f, 0.55f), woodMat, Quaternion.Euler(0f, 0f, -sideSign * 8f));
            CreateBlock(tree.transform, "BranchInward", new Vector3(-sideSign * 0.85f, 3.8f, -0.2f), new Vector3(0.24f, 2.1f, 0.24f), woodMat, Quaternion.Euler(0f, 0f, sideSign * 48f));
            CreateBlock(tree.transform, "BranchUpper", new Vector3(-sideSign * 0.45f, 4.5f, 0.1f), new Vector3(0.18f, 1.6f, 0.18f), woodMat, Quaternion.Euler(0f, 0f, sideSign * 32f));
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

        // ==================== SCREEN NAVIGATION ====================

        public void ShowScreen(MainMenuScreen screen)
        {
            if (homeScreenRoot != null) homeScreenRoot.SetActive(screen == MainMenuScreen.Home);
            if (worldSelectScreenRoot != null) worldSelectScreenRoot.SetActive(screen == MainMenuScreen.WorldSelect);
            if (upgradesScreenRoot != null) upgradesScreenRoot.SetActive(screen == MainMenuScreen.Upgrades);
            if (creditsScreenRoot != null) creditsScreenRoot.SetActive(screen == MainMenuScreen.Credits);
            if (settingsModalRoot != null) settingsModalRoot.SetActive(false);
            if (exitConfirmModalRoot != null) exitConfirmModalRoot.SetActive(false);

            if (screen == MainMenuScreen.WorldSelect)
            {
                RefreshWorldSelectionCards();
                UpdateWorldDetailsPanel(currentlySelectedWorldId);
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
                settingsMusicToggleText.color = musicOn ? new Color(0.45f, 1f, 0.55f) : new Color(0.85f, 0.45f, 0.45f);
            }
            if (settingsSfxToggleText != null)
            {
                settingsSfxToggleText.text = sfxOn ? "SFX: ON" : "SFX: OFF";
                settingsSfxToggleText.color = sfxOn ? new Color(0.45f, 1f, 0.55f) : new Color(0.85f, 0.45f, 0.45f);
            }
            if (settingsHapticsToggleText != null)
            {
                settingsHapticsToggleText.text = hapticsOn ? "HAPTICS: ON" : "HAPTICS: OFF";
                settingsHapticsToggleText.color = hapticsOn ? new Color(0.45f, 1f, 0.55f) : new Color(0.85f, 0.45f, 0.45f);
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
            MobileSettingsAndLifecycleManager.Instance?.TriggerHaptic(HapticPattern.LightTap);
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
            MobileSettingsAndLifecycleManager.Instance?.TriggerHaptic(HapticPattern.LevelUp);
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

                if (card.worldNumberText != null)
                {
                    card.worldNumberText.text = $"WORLD {wId}";
                }

                if (card.worldNameText != null)
                {
                    card.worldNameText.text = config != null ? config.displayName : $"WORLD {wId}";
                }

                if (card.difficultyBadgeText != null)
                {
                    card.difficultyBadgeText.text = config != null ? config.difficultyTier : "NORMAL";
                    card.difficultyBadgeText.color = GetTierColor(config != null ? config.difficultyTier : "");
                }

                if (card.waveAndBossSummaryText != null && config != null)
                {
                    string bossMilestones = config.totalWaves == 10
                        ? "Final Boss: Wave 10"
                        : $"Mid-Boss: W10  •  Final Boss: W{config.totalWaves}";
                    card.waveAndBossSummaryText.text = $"{config.totalWaves} Waves  •  {bossMilestones}";
                }

                if (card.statusBadgeText != null)
                {
                    if (isCompleted)
                    {
                        card.statusBadgeText.text = "★ COMPLETED";
                        card.statusBadgeText.color = new Color(0.35f, 1.0f, 0.45f);
                    }
                    else if (isUnlocked)
                    {
                        card.statusBadgeText.text = "UNLOCKED";
                        card.statusBadgeText.color = new Color(1.0f, 0.85f, 0.25f);
                    }
                    else
                    {
                        card.statusBadgeText.text = $"LOCKED (Complete W{wId - 1})";
                        card.statusBadgeText.color = new Color(0.85f, 0.35f, 0.35f);
                    }
                }

                if (card.previewThumbnail != null)
                {
                    card.previewThumbnail.color = isUnlocked ? Color.white : new Color(0.42f, 0.42f, 0.45f, 0.85f);
                }

                if (card.highlightBorder != null)
                {
                    card.highlightBorder.color = isSelected
                        ? new Color(1f, 0.78f, 0.18f, 1f)
                        : (isUnlocked ? new Color(0.42f, 0.36f, 0.26f, 0.55f) : new Color(0.22f, 0.20f, 0.22f, 0.4f));
                }

                if (card.background != null)
                {
                    if (isSelected)
                    {
                        card.background.color = new Color(0.22f, 0.16f, 0.11f, 0.99f);
                    }
                    else if (isUnlocked)
                    {
                        card.background.color = new Color(0.11f, 0.09f, 0.12f, 0.96f);
                    }
                    else
                    {
                        card.background.color = new Color(0.06f, 0.05f, 0.07f, 0.90f);
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
                selectedWorldTitleText.text = $"WORLD {worldId} — {config.displayName}";
                selectedWorldTitleText.color = config.themeColor;
            }

            if (selectedWorldSubtitleText != null)
            {
                selectedWorldSubtitleText.text = config.subtitle;
            }

            if (selectedWorldTierText != null)
            {
                selectedWorldTierText.text = $"{config.difficultyTier}  •  {config.totalWaves} WAVES";
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
                        if (i > 0) sb.Append("   |   ");
                        string typeStr = enc.isFinalBoss ? "FINAL BOSS" : "MID-BOSS";
                        sb.Append($"W{enc.waveNumber}: {enc.bossName} ({typeStr})");
                    }
                    selectedWorldBossText.text = sb.ToString();
                }
                else
                {
                    selectedWorldBossText.text = "W10: THE LAST HARVEST (FINAL BOSS)";
                }
            }

            if (selectedWorldModifiersText != null)
            {
                selectedWorldModifiersText.text =
                    $"Enemy Dmg: {config.enemyDamageMultiplier * 100:0}%  •  Boss Dmg: {config.bossDamageMultiplier * 100:0}%  •  Speed: {config.enemySpeedMultiplier * 100:0}%";
            }

            if (selectedWorldStatusBadgeText != null)
            {
                if (isCompleted)
                {
                    selectedWorldStatusBadgeText.text = "★ WORLD COMPLETED";
                    selectedWorldStatusBadgeText.color = new Color(0.35f, 1.0f, 0.45f);
                }
                else if (isUnlocked)
                {
                    selectedWorldStatusBadgeText.text = "★ UNLOCKED & READY";
                    selectedWorldStatusBadgeText.color = new Color(1.0f, 0.85f, 0.25f);
                }
                else
                {
                    selectedWorldStatusBadgeText.text = $"☠ LOCKED — Complete World {config.unlockRequirement} first";
                    selectedWorldStatusBadgeText.color = new Color(0.95f, 0.35f, 0.35f);
                }
            }

            if (startRunButton != null)
            {
                startRunButton.interactable = isUnlocked;
            }

            if (startRunButtonText != null)
            {
                startRunButtonText.text = isUnlocked ? "START WORLD" : $"LOCKED (COMPLETE W{config.unlockRequirement})";
                startRunButtonText.color = isUnlocked ? new Color(1f, 0.95f, 0.35f) : new Color(0.6f, 0.6f, 0.6f);
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
                case "NIGHTMARE": return new Color(0.85f, 0.35f, 1.0f);
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
            if (homeSettingsButton != null) { homeSettingsButton.onClick.RemoveAllListeners(); homeSettingsButton.onClick.AddListener(OpenSettingsModal); }
            if (homeCreditsButton != null) { homeCreditsButton.onClick.RemoveAllListeners(); homeCreditsButton.onClick.AddListener(OpenCredits); }
            if (homeQuitButton != null) { homeQuitButton.onClick.RemoveAllListeners(); homeQuitButton.onClick.AddListener(QuitGame); }

            if (worldSelectBackButton != null) { worldSelectBackButton.onClick.RemoveAllListeners(); worldSelectBackButton.onClick.AddListener(OpenHome); }
            if (startRunButton != null) { startRunButton.onClick.RemoveAllListeners(); startRunButton.onClick.AddListener(StartSelectedWorld); }

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

            // Rebuild cleanly for Portrait 1080x1920
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

            // Subtle dark vignette that leaves the center-top 3D diorama visible on Home
            GameObject safeRootObj = new GameObject("SafeAreaRoot");
            safeRootObj.transform.SetParent(canvasTransform, false);
            RectTransform safeRt = safeRootObj.AddComponent<RectTransform>();
            safeRt.anchorMin = Vector2.zero;
            safeRt.anchorMax = Vector2.one;
            safeRt.offsetMin = Vector2.zero;
            safeRt.offsetMax = Vector2.zero;
            safeRootObj.AddComponent<MobileSafeAreaController>();

            Transform root = safeRootObj.transform;

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

            // 5. SETTINGS MODAL ROOT
            settingsModalRoot = CreateScreenPanel(root, "SettingsModalRoot");
            BuildSettingsModalContent(settingsModalRoot.transform);
            settingsModalRoot.SetActive(false);

            // 6. EXIT CONFIRMATION MODAL ROOT
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
            // Top Header Backdrop Banner (semi-transparent so 3D diorama shines in the middle)
            GameObject topBanner = new GameObject("TopTitleBanner");
            topBanner.transform.SetParent(parent, false);
            RectTransform rtBanner = topBanner.AddComponent<RectTransform>();
            rtBanner.anchorMin = new Vector2(0.5f, 1f);
            rtBanner.anchorMax = new Vector2(0.5f, 1f);
            rtBanner.pivot = new Vector2(0.5f, 1f);
            rtBanner.anchoredPosition = new Vector2(0f, -24f);
            rtBanner.sizeDelta = new Vector2(980f, 290f);
            Image bannerImg = topBanner.AddComponent<Image>();
            bannerImg.color = new Color(0.05f, 0.04f, 0.07f, 0.78f);

            // Developer Studio Tag
            CreateLabel(topBanner.transform, "StudioLabel", "LOGMAT PRESENTS", 22, FontStyle.Bold, new Color(0.78f, 0.72f, 0.64f), new Vector2(0f, 102f), new Vector2(880f, 34f));

            // Main Product Title (Authoritative: "CURSED HARVEST.")
            Text titleText = CreateLabel(topBanner.transform, "TitleText", "CURSED HARVEST.", 74, FontStyle.Bold, new Color(1.0f, 0.78f, 0.22f), new Vector2(0f, 32f), new Vector2(940f, 92f));
            Outline titleOutline = titleText.gameObject.AddComponent<Outline>();
            titleOutline.effectColor = new Color(0.18f, 0.06f, 0.02f, 0.95f);
            titleOutline.effectDistance = new Vector2(3f, -3f);

            // Subtitle
            CreateLabel(topBanner.transform, "SubtitleText", "DARK FOLK HORROR SURVIVOR  •  FIVE BLIGHTED REALMS", 23, FontStyle.Bold, new Color(0.90f, 0.84f, 0.74f), new Vector2(0f, -38f), new Vector2(920f, 38f));

            // Discrete Harvest Essence Pill inside Header
            GameObject essPill = new GameObject("HomeEssencePill");
            essPill.transform.SetParent(topBanner.transform, false);
            RectTransform rtPill = essPill.AddComponent<RectTransform>();
            rtPill.anchorMin = new Vector2(0.5f, 0.5f);
            rtPill.anchorMax = new Vector2(0.5f, 0.5f);
            rtPill.anchoredPosition = new Vector2(0f, -98f);
            rtPill.sizeDelta = new Vector2(440f, 52f);
            Image pillBg = essPill.AddComponent<Image>();
            pillBg.color = new Color(0.12f, 0.09f, 0.14f, 0.95f);

            int currentEssence = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.HarvestEssence : 0;
            homeEssenceBadgeText = CreateLabel(essPill.transform, "HomeEssenceText", $"✦ HARVEST ESSENCE: {currentEssence}", 24, FontStyle.Bold, new Color(1.0f, 0.86f, 0.28f), Vector2.zero, new Vector2(420f, 46f));

            // Note: Center vertical zone (y = +240 down to -120) is intentionally left open to showcase the 3D Cursed Pumpkin & Scythe Altar diorama!

            // Bottom Thumb-Zone Action Deck
            GameObject deckObj = new GameObject("BottomButtonDeck");
            deckObj.transform.SetParent(parent, false);
            RectTransform rtDeck = deckObj.AddComponent<RectTransform>();
            rtDeck.anchorMin = new Vector2(0.5f, 0f);
            rtDeck.anchorMax = new Vector2(0.5f, 0f);
            rtDeck.pivot = new Vector2(0.5f, 0f);
            rtDeck.anchoredPosition = new Vector2(0f, 88f);
            rtDeck.sizeDelta = new Vector2(860f, 560f);
            Image deckBg = deckObj.AddComponent<Image>();
            deckBg.color = new Color(0.05f, 0.04f, 0.07f, 0.84f);

            homePlayButton = CreateMenuButton(deckObj.transform, "PlayButton", "PLAY", new Vector2(0f, 185f), new Vector2(760f, 118f), new Color(0.88f, 0.54f, 0.14f), 38);
            homeUpgradesButton = CreateMenuButton(deckObj.transform, "UpgradesButton", "UPGRADES", new Vector2(0f, 48f), new Vector2(760f, 102f), new Color(0.22f, 0.18f, 0.26f), 32);
            homeSettingsButton = CreateMenuButton(deckObj.transform, "SettingsButton", "SETTINGS", new Vector2(0f, -78f), new Vector2(760f, 96f), new Color(0.18f, 0.17f, 0.22f), 30);
            homeCreditsButton = CreateMenuButton(deckObj.transform, "CreditsButton", "CREDITS", new Vector2(0f, -198f), new Vector2(760f, 92f), new Color(0.16f, 0.15f, 0.19f), 28);

            // Footer Version Info
            CreateLabel(parent, "VersionText", "LOGMAT  •  CURSED HARVEST. v1.0.0  •  ANDROID PORTRAIT EDITION", 18, FontStyle.Normal, new Color(0.58f, 0.54f, 0.50f), new Vector2(0f, -915f), new Vector2(960f, 36f));
        }

        private void BuildWorldSelectScreenContent(Transform parent)
        {
            worldCards.Clear();

            // Full dark backdrop for readability
            Image screenBg = parent.gameObject.AddComponent<Image>();
            screenBg.color = new Color(0.05f, 0.04f, 0.07f, 0.96f);

            // Top Header Bar with ← BACK button and Title
            worldSelectBackButton = CreateMenuButton(parent, "BackButton", "← BACK", new Vector2(-370f, 865f), new Vector2(220f, 84f), new Color(0.22f, 0.20f, 0.26f), 26);

            CreateLabel(parent, "WorldSelectTitle", "SELECT REALM", 44, FontStyle.Bold, new Color(1.0f, 0.80f, 0.25f), new Vector2(95f, 878f), new Vector2(660f, 56f));
            CreateLabel(parent, "WorldSelectSubtitle", "Consecrate all 5 realms to unlock Endless Harvest", 21, FontStyle.Normal, new Color(0.82f, 0.78f, 0.72f), new Vector2(95f, 834f), new Vector2(680f, 32f));

            // Vertical ScrollRect containing the 5 Portrait World Cards (y = +780 down to -340, height = 1120)
            GameObject scrollObj = new GameObject("WorldsScrollView");
            scrollObj.transform.SetParent(parent, false);
            RectTransform rtScroll = scrollObj.AddComponent<RectTransform>();
            rtScroll.anchorMin = new Vector2(0.5f, 0.5f);
            rtScroll.anchorMax = new Vector2(0.5f, 0.5f);
            rtScroll.anchoredPosition = new Vector2(0f, 220f);
            rtScroll.sizeDelta = new Vector2(980f, 1120f);
            Image scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.color = new Color(0.03f, 0.025f, 0.04f, 0.65f);
            Mask scrollMask = scrollObj.AddComponent<Mask>();
            scrollMask.showMaskGraphic = true;

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(scrollObj.transform, false);
            RectTransform rtContent = contentObj.AddComponent<RectTransform>();
            rtContent.anchorMin = new Vector2(0.5f, 1f);
            rtContent.anchorMax = new Vector2(0.5f, 1f);
            rtContent.pivot = new Vector2(0.5f, 1f);

            float cardWidth = 930f;
            float cardHeight = 204f;
            float cardSpacing = 16f;
            float totalContentHeight = 5 * cardHeight + 4 * cardSpacing + 24f;
            rtContent.sizeDelta = new Vector2(960f, totalContentHeight);
            rtContent.anchoredPosition = Vector2.zero;

            ScrollRect sr = scrollObj.AddComponent<ScrollRect>();
            sr.content = rtContent;
            sr.viewport = rtScroll;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 35f;

            for (int i = 1; i <= 5; i++)
            {
                int worldId = i;
                WorldConfig cfg = WorldConfig.GetConfig(worldId);

                GameObject cardObj = new GameObject($"WorldCard_{worldId}");
                cardObj.transform.SetParent(rtContent, false);
                RectTransform rtCard = cardObj.AddComponent<RectTransform>();
                rtCard.anchorMin = new Vector2(0.5f, 1f);
                rtCard.anchorMax = new Vector2(0.5f, 1f);
                rtCard.pivot = new Vector2(0.5f, 1f);
                rtCard.sizeDelta = new Vector2(cardWidth, cardHeight);
                float yPos = -12f - (i - 1) * (cardHeight + cardSpacing);
                rtCard.anchoredPosition = new Vector2(0f, yPos);

                Image cardBg = cardObj.AddComponent<Image>();
                cardBg.color = new Color(0.11f, 0.09f, 0.11f, 0.95f);

                // Selection Highlight Border (Outer frame)
                GameObject borderObj = new GameObject("Border");
                borderObj.transform.SetParent(cardObj.transform, false);
                RectTransform rtBorder = borderObj.AddComponent<RectTransform>();
                rtBorder.anchorMin = Vector2.zero;
                rtBorder.anchorMax = Vector2.one;
                rtBorder.sizeDelta = Vector2.zero;
                Image borderImg = borderObj.AddComponent<Image>();
                borderImg.color = new Color(0.42f, 0.36f, 0.26f, 0.55f);

                // Inner Card Fill (inset by 4px so outer border frames the card cleanly)
                GameObject innerBgObj = new GameObject("InnerCardBg");
                innerBgObj.transform.SetParent(cardObj.transform, false);
                RectTransform rtInner = innerBgObj.AddComponent<RectTransform>();
                rtInner.anchorMin = Vector2.zero;
                rtInner.anchorMax = Vector2.one;
                rtInner.offsetMin = new Vector2(4f, 4f);
                rtInner.offsetMax = new Vector2(-4f, -4f);
                Image innerBgImg = innerBgObj.AddComponent<Image>();
                innerBgImg.color = new Color(0.11f, 0.09f, 0.12f, 0.96f);
                cardBg = innerBgImg;

                Button btn = cardObj.AddComponent<Button>();
                ColorBlock cb = btn.colors;
                cb.normalColor = Color.white;
                cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
                cb.pressedColor = new Color(0.85f, 0.85f, 0.85f);
                btn.colors = cb;

                // Left Portrait Biome Thumbnail (220 x 176)
                GameObject thumbObj = new GameObject("BiomePreviewThumbnail");
                thumbObj.transform.SetParent(cardObj.transform, false);
                RectTransform rtThumb = thumbObj.AddComponent<RectTransform>();
                rtThumb.anchorMin = new Vector2(0f, 0.5f);
                rtThumb.anchorMax = new Vector2(0f, 0.5f);
                rtThumb.pivot = new Vector2(0f, 0.5f);
                rtThumb.anchoredPosition = new Vector2(16f, 0f);
                rtThumb.sizeDelta = new Vector2(220f, 172f);
                Image thumbImg = thumbObj.AddComponent<Image>();
                thumbImg.sprite = GetOrCreateWorldPreviewSprite(worldId);

                // World Number + Name
                Text tWNum = CreateLabel(cardObj.transform, "WorldNumber", $"WORLD {worldId}", 20, FontStyle.Bold, new Color(0.96f, 0.78f, 0.32f), new Vector2(-95f, 64f), new Vector2(400f, 30f), TextAnchor.MiddleLeft);
                Text tWName = CreateLabel(cardObj.transform, "WorldName", cfg != null ? cfg.displayName : $"REALM {worldId}", 30, FontStyle.Bold, Color.white, new Vector2(55f, 24f), new Vector2(650f, 40f), TextAnchor.MiddleLeft);

                // Difficulty Badge (Top-Right of card)
                Text tDiff = CreateLabel(cardObj.transform, "DifficultyBadge", cfg != null ? cfg.difficultyTier : "NORMAL", 22, FontStyle.Bold, GetTierColor(cfg != null ? cfg.difficultyTier : ""), new Vector2(310f, 64f), new Vector2(260f, 30f), TextAnchor.MiddleRight);

                // Waves & Boss Milestones Summary
                string bossMilestones = (cfg != null && cfg.totalWaves == 10)
                    ? "Final Boss: Wave 10"
                    : $"Mid-Boss: W10  •  Final Boss: W{(cfg != null ? cfg.totalWaves : 10)}";
                Text tWaveBoss = CreateLabel(cardObj.transform, "WaveBossSummary", $"{(cfg != null ? cfg.totalWaves : 10)} Waves  •  {bossMilestones}", 20, FontStyle.Normal, new Color(0.84f, 0.80f, 0.74f), new Vector2(55f, -22f), new Vector2(650f, 32f), TextAnchor.MiddleLeft);

                // Unlock / Completion Status Badge
                Text tStat = CreateLabel(cardObj.transform, "StatusBadge", worldId == 1 ? "UNLOCKED" : $"LOCKED (Complete W{worldId - 1})", 21, FontStyle.Bold, worldId == 1 ? new Color(1f, 0.85f, 0.25f) : new Color(0.85f, 0.35f, 0.35f), new Vector2(55f, -64f), new Vector2(650f, 32f), TextAnchor.MiddleLeft);

                WorldCardUIElements elem = new WorldCardUIElements
                {
                    worldId = worldId,
                    cardRoot = cardObj,
                    selectButton = btn,
                    worldNumberText = tWNum,
                    worldNameText = tWName,
                    difficultyBadgeText = tDiff,
                    waveAndBossSummaryText = tWaveBoss,
                    statusBadgeText = tStat,
                    highlightBorder = borderImg,
                    background = cardBg,
                    previewThumbnail = thumbImg
                };
                worldCards.Add(elem);
            }

            // Selected World Summary Panel (y = -520, height = 310)
            GameObject detailsObj = new GameObject("WorldDetailsPanel");
            detailsObj.transform.SetParent(parent, false);
            RectTransform rtDetails = detailsObj.AddComponent<RectTransform>();
            rtDetails.anchorMin = new Vector2(0.5f, 0.5f);
            rtDetails.anchorMax = new Vector2(0.5f, 0.5f);
            rtDetails.anchoredPosition = new Vector2(0f, -525f);
            rtDetails.sizeDelta = new Vector2(980f, 310f);
            Image detailsBg = detailsObj.AddComponent<Image>();
            detailsBg.color = new Color(0.10f, 0.08f, 0.12f, 0.96f);

            selectedWorldTitleText = CreateLabel(detailsObj.transform, "DetailTitle", "WORLD 1 — THE CURSED FARM", 30, FontStyle.Bold, new Color(0.95f, 0.65f, 0.20f), new Vector2(-120f, 112f), new Vector2(680f, 40f), TextAnchor.MiddleLeft);
            selectedWorldTierText = CreateLabel(detailsObj.transform, "DetailTier", "EASY  •  10 WAVES", 22, FontStyle.Bold, new Color(0.4f, 0.95f, 0.45f), new Vector2(310f, 112f), new Vector2(300f, 36f), TextAnchor.MiddleRight);
            selectedWorldSubtitleText = CreateLabel(detailsObj.transform, "DetailSubtitle", "World 1 — The Blighted Homestead", 20, FontStyle.Italic, new Color(0.82f, 0.76f, 0.68f), new Vector2(-150f, 72f), new Vector2(620f, 30f), TextAnchor.MiddleLeft);
            selectedWorldStatusBadgeText = CreateLabel(detailsObj.transform, "DetailStatus", "★ UNLOCKED & READY", 20, FontStyle.Bold, new Color(1.0f, 0.85f, 0.25f), new Vector2(280f, 72f), new Vector2(360f, 30f), TextAnchor.MiddleRight);
            selectedWorldDescText = CreateLabel(detailsObj.transform, "DetailDesc", "", 20, FontStyle.Normal, new Color(0.88f, 0.86f, 0.84f), new Vector2(0f, 12f), new Vector2(920f, 68f), TextAnchor.MiddleLeft);
            selectedWorldBossText = CreateLabel(detailsObj.transform, "DetailBossSummary", "", 20, FontStyle.Bold, new Color(0.98f, 0.72f, 0.48f), new Vector2(0f, -56f), new Vector2(920f, 36f), TextAnchor.MiddleLeft);
            selectedWorldModifiersText = CreateLabel(detailsObj.transform, "DetailModifiers", "", 19, FontStyle.Normal, new Color(0.75f, 0.72f, 0.68f), new Vector2(0f, -104f), new Vector2(920f, 32f), TextAnchor.MiddleLeft);

            // Primary Bottom Action Button: START WORLD
            startRunButton = CreateMenuButton(parent, "StartRunButton", "START WORLD", new Vector2(0f, -805f), new Vector2(920f, 124f), new Color(0.88f, 0.54f, 0.14f), 38);
            startRunButtonText = startRunButton.GetComponentInChildren<Text>();
        }

        private void BuildUpgradesScreenContent(Transform parent)
        {
            upgradeBranchCards.Clear();
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                    Destroy(parent.GetChild(i).gameObject);
                else
                    DestroyImmediate(parent.GetChild(i).gameObject);
            }

            Image screenBg = parent.GetComponent<Image>();
            if (screenBg == null) screenBg = parent.gameObject.AddComponent<Image>();
            screenBg.color = new Color(0.05f, 0.04f, 0.07f, 0.96f);

            // Top Header with ← BACK Button
            upgradesBackButton = CreateMenuButton(parent, "UpgradesBackButton", "← BACK", new Vector2(-370f, 865f), new Vector2(220f, 84f), new Color(0.22f, 0.20f, 0.26f), 26);

            CreateLabel(parent, "UpgradesTitle", "OCCULT UPGRADES", 42, FontStyle.Bold, new Color(1.0f, 0.80f, 0.25f), new Vector2(95f, 878f), new Vector2(660f, 54f));
            CreateLabel(parent, "UpgradesSubtitle", "Permanent blessings across all realms", 21, FontStyle.Normal, new Color(0.82f, 0.78f, 0.72f), new Vector2(95f, 834f), new Vector2(660f, 32f));

            // Harvest Essence Balance Banner
            GameObject essBadgeObj = new GameObject("EssenceBalanceBadge");
            essBadgeObj.transform.SetParent(parent, false);
            RectTransform rtEss = essBadgeObj.AddComponent<RectTransform>();
            rtEss.anchorMin = new Vector2(0.5f, 0.5f);
            rtEss.anchorMax = new Vector2(0.5f, 0.5f);
            rtEss.anchoredPosition = new Vector2(0f, 735f);
            rtEss.sizeDelta = new Vector2(960f, 76f);
            Image essBg = essBadgeObj.AddComponent<Image>();
            essBg.color = new Color(0.11f, 0.09f, 0.14f, 0.98f);

            essenceBalanceText = CreateLabel(essBadgeObj.transform, "EssenceText", "✦ HARVEST ESSENCE: 0", 30, FontStyle.Bold, new Color(1.0f, 0.86f, 0.28f), Vector2.zero, new Vector2(920f, 64f));

            // 8 Upgrades in a 2 Columns x 4 Rows Portrait Grid
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
                bg.color = new Color(0.12f, 0.10f, 0.15f, 0.96f);

                // Card Title & Glyph
                Text tN = CreateLabel(cObj.transform, "Title", $"{def.iconGlyph}  {def.displayName.ToUpper()}", 26, FontStyle.Bold, new Color(1.0f, 0.82f, 0.32f), new Vector2(0f, 118f), new Vector2(430f, 42f));

                // Rank Indicator [■ ■ □ □ □] Lv X/5
                Text tR = CreateLabel(cObj.transform, "Rank", "LEVEL: [□ □ □ □ □] 0/5", 20, FontStyle.Bold, new Color(0.42f, 0.96f, 0.56f), new Vector2(0f, 68f), new Vector2(430f, 32f));

                // Effect Description (Current + Next)
                Text tD = CreateLabel(cObj.transform, "Desc", "", 19, FontStyle.Normal, new Color(0.90f, 0.88f, 0.86f), new Vector2(0f, -6f), new Vector2(424f, 96f));

                // Large Touch-Friendly Upgrade Button
                Button uBtn = CreateMenuButton(cObj.transform, "UpgradeButton", "UPGRADE", new Vector2(0f, -112f), new Vector2(420f, 78f), new Color(0.82f, 0.50f, 0.14f), 22);
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
            if (essenceBalanceText != null)
            {
                essenceBalanceText.text = $"✦ HARVEST ESSENCE: {essence}";
            }
            if (homeEssenceBadgeText != null)
            {
                homeEssenceBadgeText.text = $"✦ HARVEST ESSENCE: {essence}";
            }

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
                if (card.rankText != null)
                {
                    card.rankText.text = $"Lv {rank}/5  [{boxes.Trim()}]";
                }

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
                        card.buttonText.color = new Color(0.65f, 0.65f, 0.65f);
                    }
                    else
                    {
                        int cost = def.GetCost(rank);
                        bool canAfford = essence >= cost;
                        card.upgradeButton.interactable = canAfford;
                        card.buttonText.text = $"UPGRADE ({cost} ✦)";
                        card.buttonText.color = canAfford ? new Color(1f, 0.95f, 0.4f) : new Color(0.85f, 0.45f, 0.45f);
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
            cardBg.color = new Color(0.10f, 0.08f, 0.13f, 0.98f);

            CreateLabel(cardObj.transform, "SettingsTitle", "SETTINGS", 48, FontStyle.Bold, new Color(1.0f, 0.80f, 0.25f), new Vector2(0f, 320f), new Vector2(760f, 64f));
            CreateLabel(cardObj.transform, "SettingsSub", "Audio & Touch Haptics Preferences", 22, FontStyle.Normal, new Color(0.80f, 0.76f, 0.72f), new Vector2(0f, 260f), new Vector2(760f, 36f));

            settingsMusicToggleButton = CreateMenuButton(cardObj.transform, "MusicToggleBtn", "MUSIC: ON", new Vector2(0f, 130f), new Vector2(680f, 108f), new Color(0.18f, 0.16f, 0.22f), 32);
            settingsMusicToggleText = settingsMusicToggleButton.GetComponentInChildren<Text>();

            settingsSfxToggleButton = CreateMenuButton(cardObj.transform, "SfxToggleBtn", "SFX: ON", new Vector2(0f, -10f), new Vector2(680f, 108f), new Color(0.18f, 0.16f, 0.22f), 32);
            settingsSfxToggleText = settingsSfxToggleButton.GetComponentInChildren<Text>();

            settingsHapticsToggleButton = CreateMenuButton(cardObj.transform, "HapticsToggleBtn", "HAPTICS: ON", new Vector2(0f, -150f), new Vector2(680f, 108f), new Color(0.18f, 0.16f, 0.22f), 32);
            settingsHapticsToggleText = settingsHapticsToggleButton.GetComponentInChildren<Text>();

            settingsCloseButton = CreateMenuButton(cardObj.transform, "CloseSettingsBtn", "DONE", new Vector2(0f, -310f), new Vector2(520f, 98f), new Color(0.85f, 0.52f, 0.14f), 32);
        }

        private void BuildExitConfirmationContent(Transform parent)
        {
            Image dimBg = parent.gameObject.AddComponent<Image>();
            dimBg.color = new Color(0.02f, 0.02f, 0.03f, 0.90f);

            GameObject cardObj = new GameObject("ExitConfirmCard");
            cardObj.transform.SetParent(parent, false);
            RectTransform rtCard = cardObj.AddComponent<RectTransform>();
            rtCard.anchorMin = new Vector2(0.5f, 0.5f);
            rtCard.anchorMax = new Vector2(0.5f, 0.5f);
            rtCard.sizeDelta = new Vector2(820f, 520f);
            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.color = new Color(0.11f, 0.08f, 0.12f, 0.98f);

            CreateLabel(cardObj.transform, "ExitTitle", "EXIT CURSED HARVEST.?", 40, FontStyle.Bold, new Color(1.0f, 0.80f, 0.25f), new Vector2(0f, 140f), new Vector2(740f, 56f));
            CreateLabel(cardObj.transform, "ExitSub", "Your Harvest Essence, unlocked realms, and consecrated upgrades are saved.", 23, FontStyle.Normal, new Color(0.85f, 0.82f, 0.78f), new Vector2(0f, 45f), new Vector2(720f, 80f));

            exitConfirmCancelButton = CreateMenuButton(cardObj.transform, "CancelExitBtn", "STAY IN REALM", new Vector2(-185f, -130f), new Vector2(340f, 98f), new Color(0.24f, 0.22f, 0.28f), 26);
            exitConfirmYesButton = CreateMenuButton(cardObj.transform, "ConfirmExitBtn", "EXIT GAME", new Vector2(185f, -130f), new Vector2(340f, 98f), new Color(0.72f, 0.20f, 0.20f), 26);
        }

        private void BuildCreditsScreenContent(Transform parent)
        {
            Image screenBg = parent.gameObject.AddComponent<Image>();
            screenBg.color = new Color(0.05f, 0.04f, 0.07f, 0.96f);

            creditsBackButton = CreateMenuButton(parent, "CreditsBackButton", "← BACK", new Vector2(-370f, 865f), new Vector2(220f, 84f), new Color(0.22f, 0.20f, 0.26f), 26);

            CreateLabel(parent, "CreditsTitle", "CREDITS", 46, FontStyle.Bold, new Color(1.0f, 0.80f, 0.25f), new Vector2(95f, 875f), new Vector2(640f, 56f));

            GameObject cardObj = new GameObject("CreditsCard");
            cardObj.transform.SetParent(parent, false);
            RectTransform rtCard = cardObj.AddComponent<RectTransform>();
            rtCard.anchorMin = new Vector2(0.5f, 0.5f);
            rtCard.anchorMax = new Vector2(0.5f, 0.5f);
            rtCard.anchoredPosition = new Vector2(0f, -20f);
            rtCard.sizeDelta = new Vector2(920f, 1320f);
            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.color = new Color(0.10f, 0.08f, 0.12f, 0.96f);

            string body =
                "CURSED HARVEST.\n" +
                "Definitive Mobile Portrait Edition\n\n" +
                "STUDIO & PUBLISHER\n" +
                "LOGMAT\n\n" +
                "GAME DESIGN & ENGINEERING\n" +
                "LOGMAT  •  Cursed Harvest Team\n\n" +
                "3D ENVIRONMENTS, CHARACTERS & PROPS\n" +
                "Synty Studios (POLYGON Modular Assets)\n\n" +
                "AUDIO & SOUNDTRACK ARCHITECTURE\n" +
                "100% Procedural C# PCM Synthesis\n\n" +
                "ANDROID PACKAGE\n" +
                "com.logmat.cursedharvest\n\n" +
                "May the Harvest grant you mercy in the Blighted Realms.";

            Text tBody = CreateLabel(cardObj.transform, "CreditsBody", body, 26, FontStyle.Normal, new Color(0.90f, 0.86f, 0.80f), Vector2.zero, new Vector2(820f, 1180f));
            tBody.lineSpacing = 1.28f;
        }

        // ==================== PROCEDURAL WORLD BIOME PREVIEW THUMBNAILS ====================

        private static Sprite GetOrCreateWorldPreviewSprite(int worldId)
        {
            if (_worldThumbnailCache.TryGetValue(worldId, out Sprite cached) && cached != null)
            {
                return cached;
            }

            int w = 160;
            int h = 124;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            Color skyTop;
            Color skyBottom;
            Color groundCol;
            Color accentGlow;

            switch (worldId)
            {
                case 1: // THE CURSED FARM
                    skyTop = new Color(0.08f, 0.09f, 0.18f);
                    skyBottom = new Color(0.24f, 0.16f, 0.12f);
                    groundCol = new Color(0.14f, 0.10f, 0.06f);
                    accentGlow = new Color(1.0f, 0.72f, 0.18f);
                    break;
                case 2: // THE ASHEN ORCHARDS
                    skyTop = new Color(0.14f, 0.06f, 0.05f);
                    skyBottom = new Color(0.38f, 0.16f, 0.08f);
                    groundCol = new Color(0.15f, 0.09f, 0.08f);
                    accentGlow = new Color(1.0f, 0.45f, 0.12f);
                    break;
                case 3: // THE BLACKENED MIRE
                    skyTop = new Color(0.04f, 0.12f, 0.10f);
                    skyBottom = new Color(0.10f, 0.26f, 0.18f);
                    groundCol = new Color(0.06f, 0.11f, 0.08f);
                    accentGlow = new Color(0.35f, 0.95f, 0.48f);
                    break;
                case 4: // THE BLOODWOOD TIMBER
                    skyTop = new Color(0.12f, 0.03f, 0.05f);
                    skyBottom = new Color(0.34f, 0.06f, 0.10f);
                    groundCol = new Color(0.11f, 0.04f, 0.05f);
                    accentGlow = new Color(0.96f, 0.20f, 0.25f);
                    break;
                default: // WORLD 5: THE ETERNAL ECLIPSE
                    skyTop = new Color(0.05f, 0.02f, 0.12f);
                    skyBottom = new Color(0.22f, 0.08f, 0.34f);
                    groundCol = new Color(0.07f, 0.04f, 0.11f);
                    accentGlow = new Color(0.88f, 0.32f, 1.0f);
                    break;
            }

            Color[] pixels = new Color[w * h];
            Vector2 moonCenter = new Vector2(w * 0.5f, h * 0.64f);
            float moonRadius = 24f;

            for (int y = 0; y < h; y++)
            {
                float ny = y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    Color c;
                    float hillHeight = 0.28f + 0.04f * Mathf.Sin(x * 0.06f + worldId);
                    if (ny < hillHeight)
                    {
                        c = Color.Lerp(groundCol * 0.7f, groundCol, ny / hillHeight);
                    }
                    else
                    {
                        float skyT = (ny - hillHeight) / (1f - hillHeight);
                        c = Color.Lerp(skyBottom, skyTop, skyT);
                    }

                    // Moon / Eclipse / Beacon silhouette
                    float distMoon = Vector2.Distance(new Vector2(x, y), moonCenter);
                    if (worldId == 5)
                    {
                        // Eclipse ring
                        if (distMoon < moonRadius && distMoon > moonRadius - 4.5f)
                            c = Color.Lerp(c, accentGlow, 0.92f);
                        else if (distMoon <= moonRadius - 4.5f)
                            c = new Color(0.03f, 0.01f, 0.05f);
                    }
                    else if (distMoon < moonRadius)
                    {
                        float glowT = 1f - (distMoon / moonRadius);
                        c = Color.Lerp(c, accentGlow, glowT * 0.85f);
                    }

                    // Stylized trees / monolith silhouettes on horizon
                    if (ny >= hillHeight && ny < hillHeight + 0.32f && ((x + worldId * 13) % 28 < 5))
                    {
                        c = groundCol * 0.65f;
                    }

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
            img.color = bgColor;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.20f, 1.20f, 1.20f);
            cb.pressedColor = new Color(0.78f, 0.78f, 0.78f);
            cb.disabledColor = new Color(0.42f, 0.42f, 0.42f, 0.55f);
            btn.colors = cb;

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
            t.color = Color.white;

            return btn;
        }
    }
}
