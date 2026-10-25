using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CursedHarvest
{
    public enum UILayoutMode
    {
        Landscape,
        Portrait
    }

    public class UIManager : MonoBehaviour
    {
        private static UIManager _instance;
        public static UIManager Instance
        {
            get
            {
                if (_instance == null || !_instance)
                {
                    _instance = FindAnyObjectByType<UIManager>();
                }
                return (_instance != null && _instance) ? _instance : null;
            }
            private set => _instance = value;
        }

        [Header("Platform & Layout Architecture")]
        [SerializeField] private UILayoutMode currentLayoutMode = UILayoutMode.Portrait;
        public UILayoutMode CurrentLayoutMode => currentLayoutMode;

        [Header("Safe Area & Touch Controls")]
        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField] private VirtualJoystickUI virtualJoystick;
        [SerializeField] private DashTouchButtonUI dashTouchButton;
        [SerializeField] private GameObject touchControlsRoot;

        [Header("Top-Right HUD Elements")]
        [SerializeField] private RectTransform topRightHUDContainer;
        [SerializeField] private RectTransform seedBadgeRoot;
        [SerializeField] private RectTransform statusEffectsContainer;

        [Header("HUD References")]
        [SerializeField] private Image healthBarFill;
        [SerializeField] private Text healthText;
        [SerializeField] private Text luckyChargesText;
        [SerializeField] private GameObject luckyLastBanner;
        [SerializeField] private Text luckyLastTimerText;
        [SerializeField] private Text timerText;
        [SerializeField] private Text waveText;
        [SerializeField] private Text waveRemainingText;
        [SerializeField] private Image xpBarFill;
        [SerializeField] private Text levelText;
        [SerializeField] private Text killCountText;
        [SerializeField] private Text seedCountText;

        [Header("Level Up Modal")]
        [SerializeField] private GameObject levelUpModal;
        [SerializeField] private UpgradeCardUI[] upgradeCards;
        [SerializeField] private Button rerollButton;
        [SerializeField] private Text rerollButtonText;

        [Header("Pause Modal")]
        [SerializeField] private GameObject pauseModal;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button pauseSettingsButton;
        [SerializeField] private Button pauseWorldSelectButton;
        [SerializeField] private Button pauseHomeButton;

        [Header("Settings Modal (In-Run)")]
        [SerializeField] private GameObject settingsModal;
        [SerializeField] private Button settingsMusicToggleButton;
        [SerializeField] private Text settingsMusicToggleText;
        [SerializeField] private Button settingsSfxToggleButton;
        [SerializeField] private Text settingsSfxToggleText;
        [SerializeField] private Button settingsHapticsToggleButton;
        [SerializeField] private Text settingsHapticsToggleText;
        [SerializeField] private Button settingsCloseButton;

        [Header("Game Over Modal")]
        [SerializeField] private GameObject gameOverModal;
        [SerializeField] private Text gameOverStatsText;
        [SerializeField] private Button doubleEssenceAdButton;
        [SerializeField] private Text doubleEssenceAdButtonText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button gameOverWorldSelectButton;
        [SerializeField] private Button gameOverHomeButton;

        [Header("HUD Controls")]
        [SerializeField] private Button hudPauseButton;

        [Header("Boss HUD")]
        [SerializeField] private GameObject bossBarRoot;
        [SerializeField] private Image bossBarFill;
        [SerializeField] private Text bossNameText;

        [Header("Boss Victory / World Complete Modal")]
        [SerializeField] private GameObject victoryModal;
        [SerializeField] private Text victoryTitleText;
        [SerializeField] private Text victorySubtitleText;
        [SerializeField] private Button continueEndlessButton;
        [SerializeField] private Button victoryWorldSelectButton;

        [Header("Title / Instructions Modal")]
        [SerializeField] private GameObject titleModal;
        [SerializeField] private Button startRunButton;
        [SerializeField] private Button openCreditsButton;

        [Header("Credits Modal")]
        [SerializeField] private GameObject creditsModal;
        [SerializeField] private Button closeCreditsButton;

        private UpgradeManager upgradeManager;
        private LuckyLastSystem luckyLastSystem;
        [SerializeField] private int pendingLevelUps = 0;
        public int PendingLevelUps => pendingLevelUps;
        private List<UpgradeData> currentChoices = new List<UpgradeData>();
        private bool isPaused;

        private GameObject toastRoot;
        private Text toastTitleText;
        private Text toastSubtitleText;
        private Image toastBg;
        private Coroutine toastCoroutine;

        private float lastKnownCurrentHp = 100f;
        private float lastKnownMaxHp = 100f;
        private Coroutine hpFlashCoroutine;

        public bool IsModalOpen => (levelUpModal != null && levelUpModal.activeSelf) ||
                                  (pauseModal != null && pauseModal.activeSelf) ||
                                  (settingsModal != null && settingsModal.activeSelf) ||
                                  (gameOverModal != null && gameOverModal.activeSelf) ||
                                  (victoryModal != null && victoryModal.activeSelf) ||
                                  (titleModal != null && titleModal.activeSelf) ||
                                  (creditsModal != null && creditsModal.activeSelf);

        public bool IsGamePaused => isPaused;
        public GameObject PauseModal => pauseModal;
        public GameObject VictoryModal => victoryModal;
        public GameObject LevelUpModal => levelUpModal;
        public GameObject GameOverModal => gameOverModal;
        public GameObject SettingsModal => settingsModal;
        public Button HudPauseButton => hudPauseButton;
        public Button ResumeButton => resumeButton;
        public RectTransform TopRightHUDContainer => topRightHUDContainer;
        public RectTransform SeedBadgeRoot => seedBadgeRoot;
        public RectTransform StatusEffectsContainer => statusEffectsContainer;
        public RectTransform SafeAreaRoot => safeAreaRoot;
        public VirtualJoystickUI VirtualJoystick => virtualJoystick;
        public DashTouchButtonUI DashTouchButton => dashTouchButton;
        public GameObject TouchControlsRoot => touchControlsRoot;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _ = MobileSettingsAndLifecycleManager.Instance;
            _ = GameInputManager.Instance;

            EnsureHUDAndModals();

            WireAllButtons();
            HideAllModals();
        }

        private void WireAllButtons()
        {
            if (hudPauseButton != null) { hudPauseButton.onClick.RemoveAllListeners(); hudPauseButton.onClick.AddListener(TogglePause); }
            if (resumeButton != null) { resumeButton.onClick.RemoveAllListeners(); resumeButton.onClick.AddListener(ResumeGame); }
            if (restartButton != null) { restartButton.onClick.RemoveAllListeners(); restartButton.onClick.AddListener(RestartRun); }
            if (pauseSettingsButton != null) { pauseSettingsButton.onClick.RemoveAllListeners(); pauseSettingsButton.onClick.AddListener(OpenSettingsModal); }
            if (pauseWorldSelectButton != null) { pauseWorldSelectButton.onClick.RemoveAllListeners(); pauseWorldSelectButton.onClick.AddListener(ReturnToWorldSelect); }
            if (pauseHomeButton != null) { pauseHomeButton.onClick.RemoveAllListeners(); pauseHomeButton.onClick.AddListener(ReturnToHome); }

            if (settingsMusicToggleButton != null) { settingsMusicToggleButton.onClick.RemoveAllListeners(); settingsMusicToggleButton.onClick.AddListener(ToggleMusicSetting); }
            if (settingsSfxToggleButton != null) { settingsSfxToggleButton.onClick.RemoveAllListeners(); settingsSfxToggleButton.onClick.AddListener(ToggleSfxSetting); }
            if (settingsHapticsToggleButton != null) { settingsHapticsToggleButton.onClick.RemoveAllListeners(); settingsHapticsToggleButton.onClick.AddListener(ToggleHapticsSetting); }
            if (settingsCloseButton != null) { settingsCloseButton.onClick.RemoveAllListeners(); settingsCloseButton.onClick.AddListener(CloseSettingsModal); }

            if (retryButton != null) { retryButton.onClick.RemoveAllListeners(); retryButton.onClick.AddListener(RestartRun); }
            if (doubleEssenceAdButton != null) { doubleEssenceAdButton.onClick.RemoveAllListeners(); doubleEssenceAdButton.onClick.AddListener(OnDoubleEssenceAdClicked); }
            if (gameOverWorldSelectButton != null) { gameOverWorldSelectButton.onClick.RemoveAllListeners(); gameOverWorldSelectButton.onClick.AddListener(ReturnToWorldSelect); }
            if (gameOverHomeButton != null) { gameOverHomeButton.onClick.RemoveAllListeners(); gameOverHomeButton.onClick.AddListener(ReturnToHome); }

            if (rerollButton != null) { rerollButton.onClick.RemoveAllListeners(); rerollButton.onClick.AddListener(OnRerollClicked); }
            if (continueEndlessButton != null) { continueEndlessButton.onClick.RemoveAllListeners(); continueEndlessButton.onClick.AddListener(HandleVictoryContinue); }
            if (victoryWorldSelectButton != null) { victoryWorldSelectButton.onClick.RemoveAllListeners(); victoryWorldSelectButton.onClick.AddListener(ReturnToWorldSelect); }

            if (startRunButton != null) { startRunButton.onClick.RemoveAllListeners(); startRunButton.onClick.AddListener(StartRunFromTitle); }
            if (openCreditsButton != null) { openCreditsButton.onClick.RemoveAllListeners(); openCreditsButton.onClick.AddListener(ShowCredits); }
            if (closeCreditsButton != null) { closeCreditsButton.onClick.RemoveAllListeners(); closeCreditsButton.onClick.AddListener(CloseCredits); }
        }

        private void Start()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                upgradeManager = player.GetComponent<UpgradeManager>();

                PlayerHealth ph = player.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    ph.OnHealthChanged += UpdateHealthHUD;
                    ph.OnPlayerDied += ShowGameOver;
                    UpdateHealthHUD(ph.CurrentHealth, ph.MaxHealth);
                }

                ExperienceManager xp = player.GetComponent<ExperienceManager>();
                if (xp != null)
                {
                    xp.OnXPChanged += UpdateXpHUD;
                    xp.OnLevelUp += ShowLevelUp;
                    UpdateXpHUD(xp.CurrentXP, xp.RequiredXP, xp.ProgressPercent);
                    SetLevelText(xp.CurrentLevel);
                }
                luckyLastSystem = player.GetComponent<LuckyLastSystem>();
            }
        }

        private void OnEnable()
        {
            LuckyLastSystem.OnChargesChanged -= UpdateLuckyChargesHUD;
            LuckyLastSystem.OnChargesChanged += UpdateLuckyChargesHUD;
            LuckyLastSystem.OnLuckyLastActivated -= ShowLuckyLastBanner;
            LuckyLastSystem.OnLuckyLastActivated += ShowLuckyLastBanner;
            LuckyLastSystem.OnLuckyLastEnded -= HideLuckyLastBanner;
            LuckyLastSystem.OnLuckyLastEnded += HideLuckyLastBanner;

            GameManager.OnEnemiesKilledChanged -= UpdateKillCountHUD;
            GameManager.OnEnemiesKilledChanged += UpdateKillCountHUD;
            SpawnDirector.OnRunTimerUpdated -= UpdateTimerHUD;
            SpawnDirector.OnRunTimerUpdated += UpdateTimerHUD;
            SpawnDirector.OnWaveStarted -= HandleWaveStarted;
            SpawnDirector.OnWaveStarted += HandleWaveStarted;
            SpawnDirector.OnWaveUpdated -= HandleWaveUpdated;
            SpawnDirector.OnWaveUpdated += HandleWaveUpdated;
            SpawnDirector.OnWaveCleared -= HandleWaveCleared;
            SpawnDirector.OnWaveCleared += HandleWaveCleared;
            SpawnDirector.OnIntermissionTick -= HandleIntermissionTick;
            SpawnDirector.OnIntermissionTick += HandleIntermissionTick;
            CursedSeedPickup.OnFirstSeedCollected -= HandleFirstSeedCollected;
            CursedSeedPickup.OnFirstSeedCollected += HandleFirstSeedCollected;
        }

        private void OnDisable()
        {
            LuckyLastSystem.OnChargesChanged -= UpdateLuckyChargesHUD;
            LuckyLastSystem.OnLuckyLastActivated -= ShowLuckyLastBanner;
            LuckyLastSystem.OnLuckyLastEnded -= HideLuckyLastBanner;
            GameManager.OnEnemiesKilledChanged -= UpdateKillCountHUD;
            SpawnDirector.OnRunTimerUpdated -= UpdateTimerHUD;
            SpawnDirector.OnWaveStarted -= HandleWaveStarted;
            SpawnDirector.OnWaveUpdated -= HandleWaveUpdated;
            SpawnDirector.OnWaveCleared -= HandleWaveCleared;
            SpawnDirector.OnIntermissionTick -= HandleIntermissionTick;
            CursedSeedPickup.OnFirstSeedCollected -= HandleFirstSeedCollected;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void HandleFirstSeedCollected()
        {
            ShowToast("CURSED SEED ACQUIRED", "Walk into a glowing Harvest Circle to plant & gamble.", 4.0f, new Color(0.3f, 1f, 0.45f));
        }

        public void ShowHarvestResolutionToast(string title, string detail, bool isLucky)
        {
            Color c = isLucky ? new Color(0.2f, 1f, 0.45f) : new Color(1f, 0.25f, 0.25f);
            ShowToast(title, detail, 3.2f, c);
        }

        public void ShowToast(string title, string subtitle, float duration = 3.5f, Color? accentColor = null)
        {
            if (this == null || !this) return;
            EnsureToastUI();
            if (toastCoroutine != null) StopCoroutine(toastCoroutine);
            toastCoroutine = StartCoroutine(ToastRoutine(title, subtitle, duration, accentColor ?? new Color(1f, 0.85f, 0.2f)));
        }

        private void EnsureToastUI()
        {
            if (this == null || !this) return;
            if (toastRoot != null) return;

            Transform parentTarget = safeAreaRoot != null ? safeAreaRoot : transform;

            toastRoot = new GameObject("NotificationToast");
            toastRoot.transform.SetParent(parentTarget, false);

            RectTransform rt = toastRoot.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(820, 88);
            rt.anchoredPosition = new Vector2(0, -198);

            toastBg = toastRoot.AddComponent<Image>();
            toastBg.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);

            Font safeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (safeFont == null) safeFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            GameObject titleObj = new GameObject("ToastTitle");
            titleObj.transform.SetParent(toastRoot.transform, false);
            RectTransform rtTitle = titleObj.AddComponent<RectTransform>();
            rtTitle.anchorMin = new Vector2(0f, 0.48f);
            rtTitle.anchorMax = new Vector2(1f, 1f);
            rtTitle.offsetMin = new Vector2(18, 0);
            rtTitle.offsetMax = new Vector2(-18, -6);
            toastTitleText = titleObj.AddComponent<Text>();
            toastTitleText.font = safeFont;
            toastTitleText.fontSize = 26;
            toastTitleText.fontStyle = FontStyle.Bold;
            toastTitleText.alignment = TextAnchor.MiddleCenter;
            toastTitleText.color = new Color(1f, 0.85f, 0.2f);

            GameObject subObj = new GameObject("ToastSub");
            subObj.transform.SetParent(toastRoot.transform, false);
            RectTransform rtSub = subObj.AddComponent<RectTransform>();
            rtSub.anchorMin = new Vector2(0f, 0f);
            rtSub.anchorMax = new Vector2(1f, 0.50f);
            rtSub.offsetMin = new Vector2(18, 6);
            rtSub.offsetMax = new Vector2(-18, 0);
            toastSubtitleText = subObj.AddComponent<Text>();
            toastSubtitleText.font = safeFont;
            toastSubtitleText.fontSize = 20;
            toastSubtitleText.alignment = TextAnchor.MiddleCenter;
            toastSubtitleText.color = Color.white;

            toastRoot.SetActive(false);
        }

        private System.Collections.IEnumerator ToastRoutine(string title, string subtitle, float duration, Color color)
        {
            if (toastRoot == null) yield break;
            toastRoot.SetActive(true);
            if (toastTitleText != null)
            {
                toastTitleText.text = title;
                toastTitleText.color = color;
            }
            if (toastSubtitleText != null) toastSubtitleText.text = subtitle;

            yield return new WaitForSecondsRealtime(duration);

            if (toastRoot != null) toastRoot.SetActive(false);
            toastCoroutine = null;
        }

        public void HideToast()
        {
            if (toastCoroutine != null)
            {
                StopCoroutine(toastCoroutine);
                toastCoroutine = null;
            }
            if (toastRoot != null) toastRoot.SetActive(false);
        }

        private void Update()
        {
            UpdateTouchControlsVisibility();

            // Android Back Button / Escape Routing
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (settingsModal != null && settingsModal.activeSelf)
                {
                    CloseSettingsModal();
                    return;
                }
                if (creditsModal != null && creditsModal.activeSelf)
                {
                    CloseCredits();
                    return;
                }
                if (levelUpModal != null && levelUpModal.activeSelf)
                {
                    return; // Must pick an upgrade card
                }
                if (gameOverModal != null && gameOverModal.activeSelf)
                {
                    ReturnToWorldSelect();
                    return;
                }
                if (victoryModal != null && victoryModal.activeSelf)
                {
                    ReturnToWorldSelect();
                    return;
                }
                TogglePause();
                return;
            }

            // Editor / Desktop keyboard shortcuts for Level Up cards (1, 2, 3, R)
            if (levelUpModal != null && levelUpModal.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) SelectCard(0);
                if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) SelectCard(1);
                if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) SelectCard(2);
                if (Input.GetKeyDown(KeyCode.R)) OnRerollClicked();
            }

            // Real-time critical health pulsing
            if (hpFlashCoroutine == null && healthBarFill != null)
            {
                float pct = Mathf.Clamp01(lastKnownCurrentHp / lastKnownMaxHp);
                if (pct <= 0.25f)
                {
                    healthBarFill.color = GetBaseHealthColor(pct);
                }
            }

            // Real-time Lucky Last countdown
            if (luckyLastBanner != null && luckyLastBanner.activeSelf && luckyLastSystem != null)
            {
                if (luckyLastTimerText != null)
                {
                    luckyLastTimerText.text = $"★ LUCKY LAST: {luckyLastSystem.RemainingTime:F1}s ★";
                }
            }
        }

        private void UpdateTouchControlsVisibility()
        {
            if (touchControlsRoot == null) return;
            bool shouldShow = !IsModalOpen;
            if (touchControlsRoot.activeSelf != shouldShow)
            {
                touchControlsRoot.SetActive(shouldShow);
            }
        }

        public void StartRunFromTitle()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            if (titleModal != null) titleModal.SetActive(false);
            if (creditsModal != null) creditsModal.SetActive(false);
            Time.timeScale = 1f;
        }

        public void ShowCredits()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            if (creditsModal != null) creditsModal.SetActive(true);
        }

        public void CloseCredits()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            if (creditsModal != null) creditsModal.SetActive(false);
        }

        public void HideAllModals()
        {
            if (titleModal != null) titleModal.SetActive(false);
            if (creditsModal != null) creditsModal.SetActive(false);
            if (levelUpModal != null) levelUpModal.SetActive(false);
            if (pauseModal != null) pauseModal.SetActive(false);
            if (settingsModal != null) settingsModal.SetActive(false);
            if (gameOverModal != null) gameOverModal.SetActive(false);
            if (victoryModal != null) victoryModal.SetActive(false);
            if (luckyLastBanner != null) luckyLastBanner.SetActive(false);
            UpdateTouchControlsVisibility();
        }

        // ==================== HUD UPDATES ====================
        public void UpdateHealthHUD(float current, float max)
        {
            float prev = lastKnownCurrentHp;
            lastKnownCurrentHp = current;
            lastKnownMaxHp = Mathf.Max(1f, max);
            float pct = Mathf.Clamp01(current / lastKnownMaxHp);

            if (healthBarFill != null)
            {
                healthBarFill.fillAmount = pct;
                RectTransform rtFill = healthBarFill.rectTransform;
                if (rtFill != null)
                {
                    rtFill.anchorMin = Vector2.zero;
                    rtFill.anchorMax = new Vector2(pct, 1f);
                    rtFill.sizeDelta = Vector2.zero;
                }

                if (current < prev - 0.5f)
                {
                    if (hpFlashCoroutine != null) StopCoroutine(hpFlashCoroutine);
                    hpFlashCoroutine = StartCoroutine(HealthBarFlashRoutine(Color.white));
                }
                else if (current > prev + 0.5f)
                {
                    if (hpFlashCoroutine != null) StopCoroutine(hpFlashCoroutine);
                    hpFlashCoroutine = StartCoroutine(HealthBarFlashRoutine(new Color(0.2f, 0.95f, 0.4f, 1f)));
                }
                else if (hpFlashCoroutine == null)
                {
                    healthBarFill.color = GetBaseHealthColor(pct);
                }
            }

            if (healthText != null)
            {
                healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }
        }

        private Color GetBaseHealthColor(float pct)
        {
            if (pct <= 0.25f)
            {
                float pulse = Mathf.PingPong(Time.time * 4f, 1f);
                return Color.Lerp(new Color(0.95f, 0.15f, 0.15f), new Color(0.55f, 0.08f, 0.08f), pulse);
            }
            if (pct <= 0.50f)
            {
                return new Color(0.92f, 0.40f, 0.10f);
            }
            return new Color(0.76f, 0.14f, 0.14f);
        }

        private System.Collections.IEnumerator HealthBarFlashRoutine(Color flashColor)
        {
            if (healthBarFill != null) healthBarFill.color = flashColor;
            yield return new WaitForSecondsRealtime(0.12f);
            float elapsed = 0f;
            float dur = 0.20f;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / dur;
                float pct = Mathf.Clamp01(lastKnownCurrentHp / lastKnownMaxHp);
                if (healthBarFill != null)
                {
                    healthBarFill.color = Color.Lerp(flashColor, GetBaseHealthColor(pct), t);
                }
                yield return null;
            }
            hpFlashCoroutine = null;
        }

        public void UpdateLuckyChargesHUD(int current, int max)
        {
            if (luckyChargesText != null)
            {
                if (current > 0)
                {
                    luckyChargesText.text = $"★ LUCKY LAST: {current}";
                    luckyChargesText.color = new Color(0.96f, 0.82f, 0.28f);
                }
                else
                {
                    luckyChargesText.text = "☠ LUCKY LAST: 0";
                    luckyChargesText.color = new Color(0.88f, 0.28f, 0.28f);
                }
            }
        }

        public void ShowLuckyLastBanner(float duration)
        {
            if (luckyLastBanner != null) luckyLastBanner.SetActive(true);
            ShowToast("LUCKY LAST!", $"Death cheated! {Mathf.RoundToInt(duration)}s berserk power!", 2.5f, new Color(1f, 0.35f, 0.2f));
        }

        public void HideLuckyLastBanner()
        {
            if (luckyLastBanner != null) luckyLastBanner.SetActive(false);
        }

        public void UpdateTimerHUD(float seconds)
        {
            if (timerText != null)
            {
                int mins = (int)(seconds / 60f);
                int secs = (int)(seconds % 60f);
                timerText.text = $"{mins:00}:{secs:00}";
            }
        }

        public void UpdateXpHUD(float current, float required, float percent)
        {
            if (xpBarFill != null)
            {
                xpBarFill.fillAmount = percent;
                RectTransform rtFill = xpBarFill.rectTransform;
                if (rtFill != null)
                {
                    rtFill.anchorMin = Vector2.zero;
                    rtFill.anchorMax = new Vector2(Mathf.Clamp01(percent), 1f);
                    rtFill.sizeDelta = Vector2.zero;
                }
            }
        }

        public void SetLevelText(int level)
        {
            if (levelText != null) levelText.text = $"LVL {level}";
        }

        public void UpdateKillCountHUD(int count)
        {
            if (killCountText != null) killCountText.text = $"☠ {count}";
        }

        public void UpdateSeedCountHUD(int count)
        {
            if (seedCountText != null)
            {
                seedCountText.text = $"✦ {count}";
                seedCountText.color = count > 0 ? new Color(0.35f, 0.95f, 0.55f) : new Color(0.65f, 0.72f, 0.65f);
            }
        }

        // ==================== LEVEL UP MODAL ====================
        public void ShowLevelUp(int newLevel)
        {
            SetLevelText(newLevel);
            if (levelUpModal != null && levelUpModal.activeSelf)
            {
                pendingLevelUps++;
                return;
            }

            OpenLevelUpModal();
        }

        public void OpenLevelUpModal()
        {
            if (upgradeManager == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) upgradeManager = player.GetComponent<UpgradeManager>();
            }

            if (upgradeManager == null) return;

            currentChoices = upgradeManager.GetRandomUpgradeChoices(3);
            if (currentChoices.Count == 0)
            {
                if (levelUpModal != null) levelUpModal.SetActive(false);
                Time.timeScale = 1f;
                return;
            }

            Time.timeScale = 0f;

            if (levelUpModal != null)
            {
                levelUpModal.SetActive(true);
                UpdateTouchControlsVisibility();

                for (int i = 0; i < upgradeCards.Length; i++)
                {
                    if (i < currentChoices.Count)
                    {
                        upgradeCards[i].gameObject.SetActive(true);
                        int choiceIndex = i;
                        upgradeCards[i].Setup(currentChoices[i], () => SelectCard(choiceIndex));
                    }
                    else
                    {
                        upgradeCards[i].gameObject.SetActive(false);
                    }
                }

                UpdateRerollButton();
            }
        }

        private void UpdateRerollButton()
        {
            if (rerollButton != null && upgradeManager != null)
            {
                rerollButton.interactable = upgradeManager.RerollCharges > 0;
                if (rerollButtonText != null)
                {
                    rerollButtonText.text = $"REROLL CHOICES ({upgradeManager.RerollCharges} LEFT)";
                }
            }
        }

        private void OnRerollClicked()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            if (upgradeManager != null && upgradeManager.TryReroll())
            {
                currentChoices = upgradeManager.GetRandomUpgradeChoices(3);
                for (int i = 0; i < upgradeCards.Length; i++)
                {
                    if (i < currentChoices.Count)
                    {
                        upgradeCards[i].gameObject.SetActive(true);
                        int choiceIndex = i;
                        upgradeCards[i].Setup(currentChoices[i], () => SelectCard(choiceIndex));
                    }
                    else
                    {
                        upgradeCards[i].gameObject.SetActive(false);
                    }
                }
                UpdateRerollButton();
            }
        }

        public void SelectCard(int index)
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            if (index >= 0 && index < currentChoices.Count && upgradeManager != null)
            {
                upgradeManager.ApplyUpgrade(currentChoices[index]);
            }

            if (pendingLevelUps > 0)
            {
                pendingLevelUps--;
                OpenLevelUpModal();
                return;
            }

            if (levelUpModal != null) levelUpModal.SetActive(false);
            Time.timeScale = 1f;
            UpdateTouchControlsVisibility();
        }

        // ==================== WAVE HUD & HANDLERS ====================
        private void HandleWaveStarted(int wave)
        {
            int total = SpawnDirector.GetEnemyCountForWave(wave);
            UpdateWaveHUD(wave, total, total);
            WorldConfig cfg = (GameSession.Instance != null && GameSession.Instance.CurrentWorldConfig != null)
                ? GameSession.Instance.CurrentWorldConfig
                : WorldConfig.GetConfig(1);

            if (cfg != null && cfg.IsBossWave(wave))
            {
                var enc = cfg.GetBossEncounter(wave);
                string bName = enc != null ? enc.bossName : "THE LAST HARVEST";
                ShowToast(enc != null && !enc.isFinalBoss ? "MID-BOSS WAVE" : "BOSS WAVE", bName, 3.5f, new Color(1f, 0.25f, 0.25f));
            }
            else
            {
                ShowToast($"WAVE {wave}", "Survive the harvest horde!", 2.2f, new Color(1f, 0.65f, 0.15f));
            }
        }

        private void HandleWaveUpdated(int wave, int remaining, int total)
        {
            UpdateWaveHUD(wave, remaining, total);
        }

        private void HandleWaveCleared(int wave)
        {
            WorldConfig cfg = (GameSession.Instance != null && GameSession.Instance.CurrentWorldConfig != null)
                ? GameSession.Instance.CurrentWorldConfig
                : WorldConfig.GetConfig(1);

            if (cfg != null && cfg.IsBossWave(wave))
            {
                ShowToast("GUARDIAN VANQUISHED!", "Vacuuming boss spoils...", 2.5f, new Color(1f, 0.85f, 0.2f));
            }
            else
            {
                ShowToast($"WAVE {wave} CLEARED!", "Vacuuming spoils...", 2.0f, new Color(0.2f, 1f, 0.45f));
            }
            UpdateIntermissionHUD(12f);
        }

        private void HandleIntermissionTick(float seconds)
        {
            UpdateIntermissionHUD(seconds);
        }

        public void UpdateWaveHUD(int wave, int remaining, int total)
        {
            if (waveText != null)
            {
                waveText.text = $"WAVE {wave}";
            }
            if (waveRemainingText != null)
            {
                WorldConfig cfg = (GameSession.Instance != null && GameSession.Instance.CurrentWorldConfig != null)
                    ? GameSession.Instance.CurrentWorldConfig
                    : WorldConfig.GetConfig(1);

                if (cfg != null && cfg.IsBossWave(wave))
                {
                    var enc = cfg.GetBossEncounter(wave);
                    waveRemainingText.text = enc != null ? enc.bossName : "BOSS ENCOUNTER";
                    waveRemainingText.color = new Color(1f, 0.38f, 0.28f);
                }
                else
                {
                    waveRemainingText.text = $"{remaining} REMAINING";
                    waveRemainingText.color = new Color(0.95f, 0.94f, 0.90f);
                }
            }
        }

        public void UpdateIntermissionHUD(float remainingSeconds)
        {
            if (waveRemainingText != null)
            {
                waveRemainingText.text = $"NEXT WAVE IN {Mathf.CeilToInt(remainingSeconds)}s";
                waveRemainingText.color = new Color(0.96f, 0.76f, 0.26f);
            }
        }

        // ==================== PAUSE & SETTINGS MODALS ====================
        public void TogglePause()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            isPaused = !isPaused;
            if (pauseModal != null) pauseModal.SetActive(isPaused);
            if (!isPaused && settingsModal != null) settingsModal.SetActive(false);
            Time.timeScale = isPaused ? 0f : 1f;
            UpdateTouchControlsVisibility();
        }

        public void PauseOnAppBackground()
        {
            if (IsModalOpen) return;
            isPaused = true;
            if (pauseModal != null) pauseModal.SetActive(true);
            Time.timeScale = 0f;
            UpdateTouchControlsVisibility();
        }

        public void ResumeGame()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            isPaused = false;
            if (pauseModal != null) pauseModal.SetActive(false);
            if (settingsModal != null) settingsModal.SetActive(false);
            Time.timeScale = 1f;
            UpdateTouchControlsVisibility();
        }

        public void OpenSettingsModal()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            if (settingsModal != null)
            {
                RefreshSettingsModalLabels();
                settingsModal.SetActive(true);
            }
        }

        public void CloseSettingsModal()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            if (settingsModal != null) settingsModal.SetActive(false);
        }

        private void ToggleMusicSetting()
        {
            var mgr = MobileSettingsAndLifecycleManager.Instance;
            if (mgr != null) mgr.SetMusicEnabled(!mgr.MusicEnabled);
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            RefreshSettingsModalLabels();
        }

        private void ToggleSfxSetting()
        {
            var mgr = MobileSettingsAndLifecycleManager.Instance;
            if (mgr != null) mgr.SetSfxEnabled(!mgr.SfxEnabled);
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            RefreshSettingsModalLabels();
        }

        private void ToggleHapticsSetting()
        {
            var mgr = MobileSettingsAndLifecycleManager.Instance;
            if (mgr != null)
            {
                mgr.SetHapticsEnabled(!mgr.HapticsEnabled);
                if (mgr.HapticsEnabled) mgr.TriggerHaptic(HapticPattern.LightTick);
            }
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            RefreshSettingsModalLabels();
        }

        public void RefreshSettingsModalLabels()
        {
            var mgr = MobileSettingsAndLifecycleManager.Instance;
            bool musicOn = mgr == null || mgr.MusicEnabled;
            bool sfxOn = mgr == null || mgr.SfxEnabled;
            bool hapOn = mgr == null || mgr.HapticsEnabled;

            if (settingsMusicToggleText != null)
            {
                settingsMusicToggleText.text = musicOn ? "MUSIC: ON" : "MUSIC: OFF";
                settingsMusicToggleText.color = musicOn ? new Color(0.40f, 0.98f, 0.55f) : new Color(0.90f, 0.40f, 0.40f);
            }
            if (settingsSfxToggleText != null)
            {
                settingsSfxToggleText.text = sfxOn ? "SFX: ON" : "SFX: OFF";
                settingsSfxToggleText.color = sfxOn ? new Color(0.40f, 0.98f, 0.55f) : new Color(0.90f, 0.40f, 0.40f);
            }
            if (settingsHapticsToggleText != null)
            {
                settingsHapticsToggleText.text = hapOn ? "HAPTICS: ON" : "HAPTICS: OFF";
                settingsHapticsToggleText.color = hapOn ? new Color(0.40f, 0.98f, 0.55f) : new Color(0.90f, 0.40f, 0.40f);
            }
        }

        public void RestartRun()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            Time.timeScale = 1f;
            if (GameSession.Instance != null)
            {
                GameSession.Instance.RestartRun();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
        }

        public void ReturnToWorldSelect()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            Time.timeScale = 1f;

            // Notify AdsManager of run end and evaluate frequency-capped interstitial transition
            AdsManager.Instance?.NotifyRunCompleted();
            AdsManager.Instance?.TryShowInterstitial("RunEnd_WorldSelect");

            if (GameSession.Instance != null)
            {
                GameSession.Instance.ReturnToWorldSelect();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
        }

        public void ReturnToHome()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            Time.timeScale = 1f;

            // Notify AdsManager of run end and evaluate frequency-capped interstitial transition
            AdsManager.Instance?.NotifyRunCompleted();
            AdsManager.Instance?.TryShowInterstitial("RunEnd_Home");

            if (GameSession.Instance != null)
            {
                GameSession.Instance.ReturnToHome();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
        }

        // ==================== GAME OVER MODAL ====================
        public void ShowGameOver()
        {
            Time.timeScale = 0f;
            if (gameOverModal != null)
            {
                gameOverModal.SetActive(true);
                UpdateTouchControlsVisibility();
                RefreshGameOverStatsText();

                // Reset double essence ad button state
                if (doubleEssenceAdButton != null)
                {
                    int runEssence = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.RunEssenceEarned : 0;
                    bool canDouble = runEssence > 0;
                    doubleEssenceAdButton.gameObject.SetActive(canDouble);
                    doubleEssenceAdButton.interactable = canDouble;
                    if (doubleEssenceAdButtonText != null)
                    {
                        doubleEssenceAdButtonText.text = $"✦ DOUBLE ESSENCE (+{runEssence}) [AD] ✦";
                    }
                }
            }
        }

        private void RefreshGameOverStatsText()
        {
            if (gameOverStatsText == null) return;

            int kills = GameManager.Instance != null ? GameManager.Instance.EnemiesKilled : 0;
            float time = FindFirstObjectByType<SpawnDirector>() != null ? FindFirstObjectByType<SpawnDirector>().RunTime : 0f;
            int mins = (int)(time / 60f);
            int secs = (int)(time % 60f);
            int lvl = 1;
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null && player.GetComponent<ExperienceManager>() != null)
            {
                lvl = player.GetComponent<ExperienceManager>().CurrentLevel;
            }

            int score = kills * 100 + (int)time * 10 + lvl * 250;
            ArenaConfig arena = (GameSession.Instance != null && GameSession.Instance.CurrentArenaConfig != null)
                ? GameSession.Instance.CurrentArenaConfig
                : WorldConfig.GetArenaConfig(1);
            string arenaTitle = arena != null ? $"{arena.arenaName} (W{arena.worldId}-A{arena.arenaIndexInWorld})" : "ARENA TRIAL";
            int wave = FindFirstObjectByType<SpawnDirector>() != null ? FindFirstObjectByType<SpawnDirector>().CurrentWave : 1;

            int essenceEarned = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.RunEssenceEarned : 0;
            int totalEssence = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.HarvestEssence : 0;
            int enemiesSlain = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.RunEnemiesSlain : kills;
            int harvests = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.RunHarvestsCompleted : 0;

            gameOverStatsText.text =
                $"{arenaTitle}\n" +
                $"SURVIVED UNTIL WAVE {wave}\n\n" +
                $"TIME: {mins:00}:{secs:00}   •   KILLS: {enemiesSlain}   •   LVL {lvl}\n" +
                $"HARVESTS CONSECRATED: {harvests}\n\n" +
                $"✦ HARVEST ESSENCE +{essenceEarned} ✦\n" +
                $"TOTAL ESSENCE: {totalEssence} ✦\n\n" +
                $"SCORE: {score}";
        }

        private void OnDoubleEssenceAdClicked()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            if (AdsManager.Instance == null) return;

            if (doubleEssenceAdButton != null) doubleEssenceAdButton.interactable = false;

            AdsManager.Instance.ShowRewardedAd("GameOver_DoubleEssence", (rewardGranted) =>
            {
                if (rewardGranted)
                {
                    if (MetaProgressionManager.Instance != null)
                    {
                        MetaProgressionManager.Instance.DoubleRunEssence();
                    }
                    RefreshGameOverStatsText();
                    if (doubleEssenceAdButtonText != null)
                    {
                        doubleEssenceAdButtonText.text = "✦ ESSENCE DOUBLED! ✦";
                    }
                    if (doubleEssenceAdButton != null)
                    {
                        doubleEssenceAdButton.interactable = false;
                    }
                    ShowToast("HARVEST CONSECRATED!", "Run essence doubled via reward!", 3.0f, new Color(0.98f, 0.85f, 0.28f));
                }
                else
                {
                    if (doubleEssenceAdButton != null) doubleEssenceAdButton.interactable = true;
                    ShowToast("REWARD FAILED", "Video ad not completed or unavailable.", 2.5f, new Color(0.9f, 0.4f, 0.4f));
                }
            });
        }

        public void ShowVictoryModal()
        {
            if (victoryModal != null)
            {
                WorldConfig activeWorld = (GameSession.Instance != null && GameSession.Instance.CurrentWorldConfig != null) ? GameSession.Instance.CurrentWorldConfig : WorldConfig.GetConfig(1);
                ArenaConfig activeArena = (GameSession.Instance != null && GameSession.Instance.CurrentArenaConfig != null) ? GameSession.Instance.CurrentArenaConfig : WorldConfig.GetArenaConfig(1);

                if (victoryTitleText != null)
                {
                    if (activeArena.arenaIndexInWorld == 5)
                    {
                        if (activeWorld.worldId == 1)
                            victoryTitleText.text = "THE CONSECRATED CLEARING PURGED";
                        else if (activeWorld.worldId == 2)
                            victoryTitleText.text = "THE OSSUARY THRONE PURGED";
                        else if (activeWorld.worldId == 3)
                            victoryTitleText.text = "THE ECLIPSE IS BROKEN";
                        else
                            victoryTitleText.text = "WORLD CONQUERED!";
                    }
                    else
                    {
                        victoryTitleText.text = $"{activeArena.arenaName.ToUpperInvariant()} PURGED";
                    }
                }

                int essenceEarned = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.RunEssenceEarned : 0;
                int totalEssence = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.HarvestEssence : 0;
                int enemiesSlain = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.RunEnemiesSlain : 0;
                int harvests = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.RunHarvestsCompleted : 0;

                if (victorySubtitleText != null)
                {
                    string arenaStory;
                    if (activeArena.arenaIndexInWorld == 5)
                    {
                        if (activeWorld.worldId == 1)
                            arenaStory = "THE LAST HARVEST HAS FALLEN!\nWORLD 1 COMPLETE • WORLD 2 UNLOCKED";
                        else if (activeWorld.worldId == 2)
                            arenaStory = "THE GRAVE KING HAS FALLEN!\nWORLD 2 COMPLETE • WORLD 3 UNLOCKED";
                        else
                            arenaStory = "THE PRIMEVAL HORROR IS VANQUISHED!\n\n★ CAMPAIGN COMPLETE ★\n★ ALL 15 ARENAS CONSECRATED ★";
                    }
                    else
                    {
                        arenaStory = $"{activeArena.arenaName} (W{activeArena.worldId}-A{activeArena.arenaIndexInWorld}) CONSECRATED!\nNEXT ARENA UNLOCKED";
                    }

                    victorySubtitleText.text =
                        $"{arenaStory}\n\n" +
                        $"Enemies Slain: {enemiesSlain}   •   Harvests: {harvests}\n" +
                        $"✦ HARVEST ESSENCE +{essenceEarned} ✦\n" +
                        $"Total Essence: {totalEssence} ✦";
                }
                if (continueEndlessButton != null)
                {
                    Text btnTxt = continueEndlessButton.GetComponentInChildren<Text>();
                    if (btnTxt != null)
                    {
                        if (activeArena.arenaId < 15)
                            btnTxt.text = $"NEXT ARENA ({activeArena.arenaId + 1}/15)";
                        else
                            btnTxt.text = "ENTER ENDLESS HORDE";
                    }
                }
                if (victoryWorldSelectButton != null)
                {
                    Text btnTxt = victoryWorldSelectButton.GetComponentInChildren<Text>();
                    if (btnTxt != null) btnTxt.text = "WORLD / ARENA SELECT";
                }
                victoryModal.SetActive(true);
                UpdateTouchControlsVisibility();
            }
        }

        public void HandleVictoryContinue()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            if (victoryModal != null) victoryModal.SetActive(false);
            Time.timeScale = 1f;
            UpdateTouchControlsVisibility();

            ArenaConfig activeArena = (GameSession.Instance != null && GameSession.Instance.CurrentArenaConfig != null) ? GameSession.Instance.CurrentArenaConfig : null;
            int nextArenaId = activeArena != null ? activeArena.arenaId + 1 : 2;

            if (nextArenaId <= 15 && GameSession.Instance != null)
            {
                GameSession.Instance.StartRun(nextArenaId);
            }
            else
            {
                // Endless or completed campaign
                SpawnDirector sd = FindFirstObjectByType<SpawnDirector>();
                if (sd != null && activeArena != null && activeArena.arenaId == 15)
                {
                    sd.EnterEndlessMode();
                    sd.StartIntermissionAfterBoss();
                }
                else if (GameSession.Instance != null)
                {
                    GameSession.Instance.ReturnToWorldSelect();
                }
            }
        }

        public void CloseVictoryModal()
        {
            HandleVictoryContinue();
        }

        // ==================== BOSS HUD ====================
        public void ShowBossBar(string name, float currentHp, float maxHp)
        {
            if (bossBarRoot != null) bossBarRoot.SetActive(true);
            if (bossNameText != null) bossNameText.text = name;
            UpdateBossBar(currentHp, maxHp);
        }

        public void UpdateBossBar(float currentHp, float maxHp)
        {
            if (bossBarFill != null)
            {
                float pct = Mathf.Clamp01(currentHp / Mathf.Max(1f, maxHp));
                bossBarFill.fillAmount = pct;
                RectTransform rtFill = bossBarFill.rectTransform;
                if (rtFill != null)
                {
                    rtFill.anchorMin = Vector2.zero;
                    rtFill.anchorMax = new Vector2(pct, 1f);
                    rtFill.sizeDelta = Vector2.zero;
                }
            }
        }

        public void HideBossBar()
        {
            if (bossBarRoot != null) bossBarRoot.SetActive(false);
        }

        // ==================== DEFINITIVE PORTRAIT HUD & MODAL BUILDER ====================
        public void EnsureHUDAndModals()
        {
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Configure CanvasScaler for Mobile Portrait 1080 x 1920
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;
            }

            // If Portrait SafeAreaRoot already built and wired, just ensure references
            Transform existingSafe = transform.Find("SafeAreaRoot");
            if (existingSafe != null && levelUpModal != null && pauseModal != null && gameOverModal != null && victoryModal != null && virtualJoystick != null && dashTouchButton != null)
            {
                safeAreaRoot = existingSafe.GetComponent<RectTransform>();
                virtualJoystick.EnsureChildVisuals();
                dashTouchButton.EnsureChildVisuals();
                return;
            }

            // Rebuild clean Portrait Gameplay UI Hierarchy
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            // 1. SafeAreaRoot (Anchored to Screen.safeArea via MobileSafeAreaController)
            GameObject safeObj = new GameObject("SafeAreaRoot", typeof(RectTransform));
            safeObj.transform.SetParent(transform, false);
            safeAreaRoot = safeObj.GetComponent<RectTransform>();
            safeAreaRoot.anchorMin = Vector2.zero;
            safeAreaRoot.anchorMax = Vector2.one;
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;
            safeObj.AddComponent<MobileSafeAreaController>();

            // ==================== TOP-LEFT HUD (HP, XP, LVL, LUCKY LAST) ====================
            GameObject topLeft = CreateBox(safeAreaRoot, "TopLeftHUD",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(340, 112), new Vector2(20, -18),
                new Color(0.06f, 0.07f, 0.10f, 0.84f));
            AddBorder(topLeft.transform, new Color(0.38f, 0.30f, 0.20f, 0.70f));

            // HP Bar Bg
            GameObject hpBg = CreateBox(topLeft.transform, "HealthBg",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(312, 30), new Vector2(14, -12),
                Color.white);
            Image hpBgImg = hpBg.GetComponent<Image>();
            if (FolkHorrorUITheme.BarTrack != null)
            {
                hpBgImg.sprite = FolkHorrorUITheme.BarTrack;
                hpBgImg.type = Image.Type.Sliced;
            }

            GameObject hpFillObj = CreateBox(hpBg.transform, "HealthFill",
                Vector2.zero, Vector2.one, new Vector2(0, 0.5f),
                Vector2.zero, Vector2.zero,
                Color.white);
            healthBarFill = hpFillObj.GetComponent<Image>();
            if (FolkHorrorUITheme.BarHpFill != null)
            {
                healthBarFill.sprite = FolkHorrorUITheme.BarHpFill;
                healthBarFill.type = Image.Type.Sliced;
            }

            healthText = CreateLabel(hpBg.transform, "HealthText", "100 / 100", 19, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.98f, 0.96f, 0.92f), defaultFont);
            StretchFull(healthText.rectTransform);

            // XP Bar Bg (Right below HP bar)
            GameObject xpBg = CreateBox(topLeft.transform, "XpBg",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(312, 20), new Vector2(14, -48),
                Color.white);
            Image xpBgImg = xpBg.GetComponent<Image>();
            if (FolkHorrorUITheme.BarTrack != null)
            {
                xpBgImg.sprite = FolkHorrorUITheme.BarTrack;
                xpBgImg.type = Image.Type.Sliced;
            }

            GameObject xpFillObj = CreateBox(xpBg.transform, "XpFill",
                Vector2.zero, new Vector2(0f, 1f), new Vector2(0, 0.5f),
                Vector2.zero, Vector2.zero,
                Color.white);
            xpBarFill = xpFillObj.GetComponent<Image>();
            if (FolkHorrorUITheme.BarXpFill != null)
            {
                xpBarFill.sprite = FolkHorrorUITheme.BarXpFill;
                xpBarFill.type = Image.Type.Sliced;
            }

            levelText = CreateLabel(topLeft.transform, "LevelText", "LVL 1", 19, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.95f, 0.84f, 0.42f), defaultFont);
            SetRect(levelText.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(110, 30), new Vector2(16, 8));

            luckyChargesText = CreateLabel(topLeft.transform, "LuckyChargesText", "★ LUCKY LAST: 1", 18, FontStyle.Bold, TextAnchor.MiddleRight, new Color(0.96f, 0.82f, 0.28f), defaultFont);
            SetRect(luckyChargesText.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(195, 30), new Vector2(-14, 8));

            // ==================== TOP-CENTER HUD (WAVE, REMAINING, TIMER, KILLS) ====================
            GameObject topCenter = CreateBox(safeAreaRoot, "TopCenterHUD",
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(330, 112), new Vector2(10, -18),
                new Color(0.06f, 0.07f, 0.10f, 0.84f));
            AddBorder(topCenter.transform, new Color(0.38f, 0.30f, 0.20f, 0.70f));

            waveText = CreateLabel(topCenter.transform, "WaveText", "WAVE 1", 26, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.98f, 0.80f, 0.26f), defaultFont);
            SetRect(waveText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(300, 34), new Vector2(0, -10));

            waveRemainingText = CreateLabel(topCenter.transform, "WaveRemainingText", "8 REMAINING", 19, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.94f, 0.92f, 0.86f), defaultFont);
            SetRect(waveRemainingText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(310, 28), new Vector2(0, -44));

            timerText = CreateLabel(topCenter.transform, "TimerText", "00:00", 20, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.88f, 0.88f, 0.84f), defaultFont);
            SetRect(timerText.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(140, 28), new Vector2(24, 10));

            killCountText = CreateLabel(topCenter.transform, "KillText", "☠ 0", 20, FontStyle.Bold, TextAnchor.MiddleRight, new Color(0.92f, 0.65f, 0.55f), defaultFont);
            SetRect(killCountText.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(140, 28), new Vector2(-24, 10));

            // ==================== TOP-RIGHT HUD (PAUSE 72x72, SEED BADGE, STATUS CONTAINER) ====================
            GameObject trObj = new GameObject("TopRightHUD", typeof(RectTransform));
            trObj.transform.SetParent(safeAreaRoot, false);
            topRightHUDContainer = trObj.GetComponent<RectTransform>();
            SetRect(topRightHUDContainer, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(320, 112), new Vector2(-20, -18));

            // Pause Button (72 x 72 thumb-friendly button at top-right)
            GameObject pBtnObj = CreateSimpleButton(topRightHUDContainer, "HudPauseButton", "||",
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(72, 72), Vector2.zero,
                new Color(0.10f, 0.12f, 0.16f, 0.90f),
                new Color(0.98f, 0.85f, 0.38f), 30, defaultFont);
            AddBorder(pBtnObj.transform, new Color(0.45f, 0.36f, 0.22f, 0.85f));
            hudPauseButton = pBtnObj.GetComponent<Button>();

            // Seed Badge (128 x 54 pill to the left of Pause Button)
            GameObject sbObj = CreateBox(topRightHUDContainer, "SeedBadge",
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(128, 54), new Vector2(-84, -9),
                new Color(0.06f, 0.08f, 0.11f, 0.88f));
            AddBorder(sbObj.transform, new Color(0.32f, 0.62f, 0.38f, 0.70f));
            seedBadgeRoot = sbObj.GetComponent<RectTransform>();

            seedCountText = CreateLabel(sbObj.transform, "SeedText", "✦ 0", 24, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.35f, 0.95f, 0.55f), defaultFont);
            StretchFull(seedCountText.rectTransform);

            // Status Effects Container below Pause & Seed
            GameObject seObj = new GameObject("StatusEffectsContainer", typeof(RectTransform));
            seObj.transform.SetParent(topRightHUDContainer, false);
            statusEffectsContainer = seObj.GetComponent<RectTransform>();
            SetRect(statusEffectsContainer, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(310, 32), new Vector2(0, -78));
            HorizontalLayoutGroup hlgStatus = seObj.AddComponent<HorizontalLayoutGroup>();
            hlgStatus.childAlignment = TextAnchor.MiddleRight;
            hlgStatus.spacing = 8;
            hlgStatus.childControlWidth = false;
            hlgStatus.childControlHeight = false;

            // ==================== BOSS BAR (BELOW TOP HUD) ====================
            bossBarRoot = CreateBox(safeAreaRoot, "BossHUD",
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(920, 62), new Vector2(0, -142),
                new Color(0.07f, 0.05f, 0.07f, 0.92f));
            AddBorder(bossBarRoot.transform, new Color(0.75f, 0.22f, 0.18f, 0.85f));

            bossNameText = CreateLabel(bossBarRoot.transform, "BossName", "THE LAST HARVEST", 22, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.78f, 0.28f), defaultFont);
            SetRect(bossNameText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(880, 26), new Vector2(0, -4));

            GameObject bossBg = CreateBox(bossBarRoot.transform, "BossBg",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(888, 22), new Vector2(0, 8),
                new Color(0.14f, 0.08f, 0.10f, 0.96f));

            GameObject bossFillObj = CreateBox(bossBg.transform, "BossFill",
                Vector2.zero, Vector2.one, new Vector2(0, 0.5f),
                Vector2.zero, Vector2.zero,
                new Color(0.88f, 0.16f, 0.16f, 1f));
            bossBarFill = bossFillObj.GetComponent<Image>();
            bossBarRoot.SetActive(false);

            // Lucky Last Banner
            luckyLastBanner = CreateBox(safeAreaRoot, "LuckyLastBanner",
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(680, 56), new Vector2(0, -214),
                new Color(0.75f, 0.12f, 0.12f, 0.92f));
            luckyLastTimerText = CreateLabel(luckyLastBanner.transform, "BannerText", "★ LUCKY LAST ACTIVE! ★", 24, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.35f), defaultFont);
            StretchFull(luckyLastTimerText.rectTransform);
            luckyLastBanner.SetActive(false);

            // ==================== TOUCH CONTROLS (JOYSTICK BOTTOM-LEFT, DASH BOTTOM-RIGHT) ====================
            touchControlsRoot = new GameObject("TouchControlsRoot", typeof(RectTransform));
            touchControlsRoot.transform.SetParent(safeAreaRoot, false);
            RectTransform rtTouch = touchControlsRoot.GetComponent<RectTransform>();
            StretchFull(rtTouch);

            // Virtual Joystick (Bottom-Left: 240x240 at (195, 225))
            GameObject joyObj = new GameObject("VirtualJoystick", typeof(RectTransform));
            joyObj.transform.SetParent(touchControlsRoot.transform, false);
            RectTransform rtJoy = joyObj.GetComponent<RectTransform>();
            SetRect(rtJoy, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0.5f, 0.5f), new Vector2(240, 240), new Vector2(195, 225));
            virtualJoystick = joyObj.AddComponent<VirtualJoystickUI>();
            virtualJoystick.EnsureChildVisuals();

            // Dash Touch Button (Bottom-Right: 172x172 at (-175, 225))
            GameObject dashObj = new GameObject("DashTouchButton", typeof(RectTransform));
            dashObj.transform.SetParent(touchControlsRoot.transform, false);
            RectTransform rtDash = dashObj.GetComponent<RectTransform>();
            SetRect(rtDash, new Vector2(1, 0), new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(172, 172), new Vector2(-175, 225));
            dashTouchButton = dashObj.AddComponent<DashTouchButtonUI>();
            dashTouchButton.EnsureChildVisuals();

            // ==================== MODALS (PORTRAIT 1080 x 1920) ====================
            levelUpModal = CreatePortraitLevelUpModal(defaultFont);
            pauseModal = CreatePortraitPauseModal(defaultFont);
            settingsModal = CreatePortraitSettingsModal(defaultFont);
            gameOverModal = CreatePortraitGameOverModal(defaultFont);
            victoryModal = CreatePortraitVictoryModal(defaultFont);
        }

        private GameObject CreatePortraitLevelUpModal(Font font)
        {
            GameObject modal = CreateBox(transform, "LevelUpModal", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.03f, 0.04f, 0.06f, 0.94f));

            GameObject container = new GameObject("SafeContent", typeof(RectTransform));
            container.transform.SetParent(modal.transform, false);
            RectTransform rtCont = container.GetComponent<RectTransform>();
            StretchFull(rtCont);
            container.AddComponent<MobileSafeAreaController>();

            Text title = CreateLabel(container.transform, "Title", "CHOOSE AN UPGRADE", 48, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.98f, 0.84f, 0.32f), font);
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(920, 64), new Vector2(0, 620));

            Text sub = CreateLabel(container.transform, "Subtitle", "Tap a boon card to consecrate your harvest arsenal", 24, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.80f, 0.82f, 0.86f), font);
            SetRect(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(920, 38), new Vector2(0, 560));

            // 3 Vertical Stacked Cards (900 x 300 each, spacing 28)
            upgradeCards = new UpgradeCardUI[3];
            float[] yPositions = new float[] { 315f, -15f, -345f };
            for (int i = 0; i < 3; i++)
            {
                upgradeCards[i] = CreatePortraitUpgradeCard(container.transform, i + 1, yPositions[i], font);
            }

            // Reroll Button at bottom
            GameObject rerollObj = CreateSimpleButton(container.transform, "RerollButton", "REROLL CHOICES (1 LEFT)",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(560, 84), new Vector2(0, -595),
                new Color(0.16f, 0.19f, 0.26f, 0.98f), new Color(0.96f, 0.92f, 0.82f), 26, font);
            AddBorder(rerollObj.transform, new Color(0.48f, 0.40f, 0.25f, 0.90f));
            rerollButton = rerollObj.GetComponent<Button>();
            rerollButtonText = rerollObj.GetComponentInChildren<Text>();

            modal.SetActive(false);
            return modal;
        }

        private UpgradeCardUI CreatePortraitUpgradeCard(Transform parent, int slotIndex, float yPos, Font font)
        {
            GameObject cardObj = CreateBox(parent, $"Card_{slotIndex}",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(900, 300), new Vector2(0, yPos),
                new Color(0.45f, 0.36f, 0.22f, 1f));

            GameObject bodyObj = CreateBox(cardObj.transform, "CardBody",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                new Vector2(-6, -6), Vector2.zero,
                new Color(0.09f, 0.10f, 0.13f, 0.98f));

            // Make entire card tappable + dedicated SELECT button
            Button cardBtn = cardObj.AddComponent<Button>();
            cardBtn.targetGraphic = bodyObj.GetComponent<Image>();

            // Top-Left Category Badge
            Text cat = CreateLabel(bodyObj.transform, "Category", "★ NEW WEAPON ★", 22, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.30f, 0.96f, 0.52f), font);
            SetRect(cat.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(520, 34), new Vector2(28, -18));

            // Top-Right Level Badge
            Text lvl = CreateLabel(bodyObj.transform, "Level", "LEVEL 1 / 5", 22, FontStyle.Bold, TextAnchor.MiddleRight, new Color(0.92f, 0.78f, 0.34f), font);
            SetRect(lvl.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(300, 34), new Vector2(-28, -18));

            // Upgrade Title
            Text title = CreateLabel(bodyObj.transform, "Title", "Upgrade Name", 34, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.98f, 0.96f, 0.90f), font);
            SetRect(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(840, 46), new Vector2(28, -58));

            // Divider
            CreateBox(bodyObj.transform, "Divider",
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(844, 2), new Vector2(0, -110),
                new Color(0.35f, 0.28f, 0.18f, 0.85f));

            // Description (Left/Center area)
            Text desc = CreateLabel(bodyObj.transform, "Description", "Increases weapon damage and harvest power.", 24, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.88f, 0.86f, 0.82f), font);
            desc.horizontalOverflow = HorizontalWrapMode.Wrap;
            desc.verticalOverflow = VerticalWrapMode.Overflow;
            SetRect(desc.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(560, 150), new Vector2(28, 22));

            // Right Action Pill Button ("TAP TO SELECT")
            GameObject selPill = CreateBox(bodyObj.transform, "SelectPill",
                new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(240, 76), new Vector2(-28, 48),
                new Color(0.78f, 0.50f, 0.16f, 1f));
            Text selTxt = CreateLabel(selPill.transform, "Text", "SELECT", 26, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, font);
            StretchFull(selTxt.rectTransform);

            UpgradeCardUI card = cardObj.AddComponent<UpgradeCardUI>();
            if (Application.isPlaying)
            {
                card.ConfigureReferences(title, cat, desc, lvl, cardBtn);
            }
#if UNITY_EDITOR
            else
            {
                UnityEditor.SerializedObject so = new UnityEditor.SerializedObject(card);
                so.FindProperty("titleText").objectReferenceValue = title;
                so.FindProperty("categoryText").objectReferenceValue = cat;
                so.FindProperty("descriptionText").objectReferenceValue = desc;
                so.FindProperty("levelText").objectReferenceValue = lvl;
                so.FindProperty("selectButton").objectReferenceValue = cardBtn;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
#endif
            return card;
        }

        private GameObject CreatePortraitPauseModal(Font font)
        {
            GameObject modal = CreateBox(transform, "PauseModal", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.03f, 0.04f, 0.06f, 0.92f));

            GameObject panel = CreateBox(modal.transform, "Panel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(820, 940), Vector2.zero,
                new Color(0.42f, 0.33f, 0.20f, 1f));

            GameObject body = CreateBox(panel.transform, "Body",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                new Vector2(-6, -6), Vector2.zero,
                new Color(0.08f, 0.09f, 0.12f, 0.98f));

            Text title = CreateLabel(body.transform, "Title", "PAUSED", 54, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.84f, 0.28f), font);
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(700, 70), new Vector2(0, 340));

            Text sub = CreateLabel(body.transform, "Subtitle", "CURSED HARVEST.", 24, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.82f, 0.78f, 0.70f), font);
            SetRect(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(700, 36), new Vector2(0, 280));

            resumeButton = CreateSimpleButton(body.transform, "ResumeButton", "RESUME",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(640, 92), new Vector2(0, 155),
                new Color(0.22f, 0.58f, 0.30f), Color.white, 30, font).GetComponent<Button>();

            restartButton = CreateSimpleButton(body.transform, "RestartButton", "RESTART RUN",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(640, 92), new Vector2(0, 35),
                new Color(0.62f, 0.40f, 0.16f), Color.white, 30, font).GetComponent<Button>();

            pauseSettingsButton = CreateSimpleButton(body.transform, "SettingsButton", "SETTINGS",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(640, 92), new Vector2(0, -85),
                new Color(0.26f, 0.28f, 0.35f), Color.white, 30, font).GetComponent<Button>();

            pauseWorldSelectButton = CreateSimpleButton(body.transform, "WorldSelectButton", "WORLD SELECT",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(640, 92), new Vector2(0, -205),
                new Color(0.22f, 0.32f, 0.48f), Color.white, 30, font).GetComponent<Button>();

            pauseHomeButton = CreateSimpleButton(body.transform, "HomeButton", "HOME",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(640, 92), new Vector2(0, -325),
                new Color(0.52f, 0.20f, 0.20f), Color.white, 30, font).GetComponent<Button>();

            modal.SetActive(false);
            return modal;
        }

        private GameObject CreatePortraitSettingsModal(Font font)
        {
            GameObject modal = CreateBox(transform, "SettingsModal", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.02f, 0.03f, 0.05f, 0.95f));

            GameObject panel = CreateBox(modal.transform, "Panel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(820, 780), Vector2.zero,
                new Color(0.45f, 0.35f, 0.22f, 1f));

            GameObject body = CreateBox(panel.transform, "Body",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                new Vector2(-6, -6), Vector2.zero,
                new Color(0.08f, 0.09f, 0.12f, 0.98f));

            Text title = CreateLabel(body.transform, "Title", "SETTINGS", 48, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.84f, 0.28f), font);
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(700, 64), new Vector2(0, 260));

            GameObject musBtnObj = CreateSimpleButton(body.transform, "MusicToggleBtn", "MUSIC: ON",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(620, 96), new Vector2(0, 120),
                new Color(0.16f, 0.20f, 0.26f), new Color(0.40f, 0.98f, 0.55f), 30, font);
            settingsMusicToggleButton = musBtnObj.GetComponent<Button>();
            settingsMusicToggleText = musBtnObj.GetComponentInChildren<Text>();

            GameObject sfxBtnObj = CreateSimpleButton(body.transform, "SfxToggleBtn", "SFX: ON",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(620, 96), new Vector2(0, 0),
                new Color(0.16f, 0.20f, 0.26f), new Color(0.40f, 0.98f, 0.55f), 30, font);
            settingsSfxToggleButton = sfxBtnObj.GetComponent<Button>();
            settingsSfxToggleText = sfxBtnObj.GetComponentInChildren<Text>();

            GameObject hapBtnObj = CreateSimpleButton(body.transform, "HapticsToggleBtn", "HAPTICS: ON",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(620, 96), new Vector2(0, -120),
                new Color(0.16f, 0.20f, 0.26f), new Color(0.40f, 0.98f, 0.55f), 30, font);
            settingsHapticsToggleButton = hapBtnObj.GetComponent<Button>();
            settingsHapticsToggleText = hapBtnObj.GetComponentInChildren<Text>();

            settingsCloseButton = CreateSimpleButton(body.transform, "CloseSettingsBtn", "BACK",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(460, 88), new Vector2(0, -265),
                new Color(0.55f, 0.36f, 0.16f), Color.white, 28, font).GetComponent<Button>();

            RefreshSettingsModalLabels();
            modal.SetActive(false);
            return modal;
        }

        private GameObject CreatePortraitGameOverModal(Font font)
        {
            GameObject modal = CreateBox(transform, "GameOverModal", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.12f, 0.02f, 0.03f, 0.94f));

            GameObject panel = CreateBox(modal.transform, "Panel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(900, 1140), Vector2.zero,
                new Color(0.60f, 0.18f, 0.18f, 1f));

            GameObject body = CreateBox(panel.transform, "Body",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                new Vector2(-6, -6), Vector2.zero,
                new Color(0.08f, 0.06f, 0.08f, 0.98f));

            Text title = CreateLabel(body.transform, "Title", "HARVEST CLAIMED YOU", 50, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.98f, 0.24f, 0.22f), font);
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(840, 70), new Vector2(0, 440));

            gameOverStatsText = CreateLabel(body.transform, "StatsText", "ARENA TRIAL\nSURVIVED UNTIL WAVE 1\nTIME: 00:00", 25, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.95f, 0.92f, 0.86f), font);
            gameOverStatsText.lineSpacing = 1.25f;
            SetRect(gameOverStatsText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(800, 390), new Vector2(0, 175));

            // Rewarded Ad Button: Double Essence
            GameObject adBtnObj = CreateSimpleButton(body.transform, "DoubleEssenceAdButton", "✦ DOUBLE ESSENCE (WATCH AD) ✦",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(700, 96), new Vector2(0, -90),
                new Color(0.85f, 0.60f, 0.16f), Color.white, 28, font);
            AddBorder(adBtnObj.transform, new Color(1f, 0.85f, 0.35f, 0.9f));
            doubleEssenceAdButton = adBtnObj.GetComponent<Button>();
            doubleEssenceAdButtonText = adBtnObj.GetComponentInChildren<Text>();

            retryButton = CreateSimpleButton(body.transform, "RetryButton", "RETRY TRIAL",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(700, 90), new Vector2(0, -205),
                new Color(0.22f, 0.60f, 0.30f), Color.white, 30, font).GetComponent<Button>();

            gameOverWorldSelectButton = CreateSimpleButton(body.transform, "GameOverWorldSelectButton", "WORLD SELECT",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(700, 86), new Vector2(0, -315),
                new Color(0.24f, 0.32f, 0.48f), Color.white, 28, font).GetComponent<Button>();

            gameOverHomeButton = CreateSimpleButton(body.transform, "GameOverHomeButton", "HOME",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(700, 86), new Vector2(0, -420),
                new Color(0.48f, 0.20f, 0.20f), Color.white, 28, font).GetComponent<Button>();

            modal.SetActive(false);
            return modal;
        }

        private GameObject CreatePortraitVictoryModal(Font font)
        {
            GameObject modal = CreateBox(transform, "VictoryModal", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.03f, 0.08f, 0.05f, 0.94f));

            GameObject panel = CreateBox(modal.transform, "Panel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(900, 1060), Vector2.zero,
                new Color(0.85f, 0.68f, 0.24f, 1f));

            GameObject body = CreateBox(panel.transform, "Body",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                new Vector2(-6, -6), Vector2.zero,
                new Color(0.07f, 0.09f, 0.10f, 0.98f));

            victoryTitleText = CreateLabel(body.transform, "Title", "THE ECLIPSE IS BROKEN", 48, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.28f), font);
            SetRect(victoryTitleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(840, 80), new Vector2(0, 390));

            victorySubtitleText = CreateLabel(body.transform, "Sub", "CAMPAIGN COMPLETE\nENDLESS UNLOCKED", 26, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.94f, 0.94f, 0.90f), font);
            victorySubtitleText.lineSpacing = 1.25f;
            SetRect(victorySubtitleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(800, 440), new Vector2(0, 85));

            continueEndlessButton = CreateSimpleButton(body.transform, "EndlessButton", "ENTER ENDLESS HORDE",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(680, 98), new Vector2(0, -220),
                new Color(0.22f, 0.62f, 0.32f), Color.white, 30, font).GetComponent<Button>();

            victoryWorldSelectButton = CreateSimpleButton(body.transform, "ReturnWorldSelectButton", "WORLD SELECT",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(680, 92), new Vector2(0, -345),
                new Color(0.24f, 0.34f, 0.50f), Color.white, 28, font).GetComponent<Button>();

            modal.SetActive(false);
            return modal;
        }

        private static GameObject CreateBox(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 pos, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            Image img = go.GetComponent<Image>();

            // Auto-style with authentic Folk-Horror 9-slice panel
            FolkHorrorUITheme.StylePanel(img, true);
            if (color != Color.white && color.a > 0.05f)
            {
                img.color = new Color(color.r, color.g, color.b, color.a);
            }
            return go;
        }

        private static void AddBorder(Transform parent, Color borderColor)
        {
            Outline outline = parent.GetComponent<Outline>();
            if (outline == null) outline = parent.gameObject.AddComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(2f, -2f);
        }

        private static Text CreateLabel(Transform parent, string name, string text, int fontSize, FontStyle style, TextAnchor align, Color color, Font font)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text t = go.GetComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.alignment = align;
            t.color = color;
            return t;
        }

        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static GameObject CreateSimpleButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 size, Vector2 anchoredPos,
            Color bgColor, Color textColor, int fontSize, Font font)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;

            Image img = btnObj.GetComponent<Image>();
            Button btn = btnObj.GetComponent<Button>();

            string style = (bgColor.r > 0.5f && bgColor.g < 0.35f) ? "danger" : ((bgColor.r > 0.5f && bgColor.g > 0.4f) ? "gold" : "dark");
            FolkHorrorUITheme.StyleButton(btn, img, style);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform rtText = textObj.GetComponent<RectTransform>();
            StretchFull(rtText);

            Text txt = textObj.GetComponent<Text>();
            txt.font = font;
            txt.text = label;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = (style == "gold") ? new Color(0.12f, 0.08f, 0.04f) : FolkHorrorUITheme.ColorTextCream;

            return btnObj;
        }

        public void SetLayoutMode(UILayoutMode mode)
        {
            currentLayoutMode = mode;
        }
    }
}
