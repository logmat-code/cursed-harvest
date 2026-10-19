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
        [SerializeField] private UILayoutMode currentLayoutMode = UILayoutMode.Landscape;
        public UILayoutMode CurrentLayoutMode => currentLayoutMode;

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
        [SerializeField] private Button pauseWorldSelectButton;
        [SerializeField] private Button pauseHomeButton;

        [Header("Game Over Modal")]
        [SerializeField] private GameObject gameOverModal;
        [SerializeField] private Text gameOverStatsText;
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
                                  (gameOverModal != null && gameOverModal.activeSelf) ||
                                  (titleModal != null && titleModal.activeSelf) ||
                                  (creditsModal != null && creditsModal.activeSelf);

        public bool IsGamePaused => isPaused;
        public GameObject PauseModal => pauseModal;
        public GameObject VictoryModal => victoryModal;
        public Button HudPauseButton => hudPauseButton;
        public Button ResumeButton => resumeButton;
        public RectTransform TopRightHUDContainer => topRightHUDContainer;
        public RectTransform SeedBadgeRoot => seedBadgeRoot;
        public RectTransform StatusEffectsContainer => statusEffectsContainer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureHUDAndModals();

            if (hudPauseButton != null) hudPauseButton.onClick.AddListener(TogglePause);
            if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
            if (restartButton != null) restartButton.onClick.AddListener(RestartRun);
            if (pauseWorldSelectButton != null) pauseWorldSelectButton.onClick.AddListener(ReturnToWorldSelect);
            if (pauseHomeButton != null) pauseHomeButton.onClick.AddListener(ReturnToHome);

            if (retryButton != null) retryButton.onClick.AddListener(RestartRun);
            if (gameOverWorldSelectButton != null) gameOverWorldSelectButton.onClick.AddListener(ReturnToWorldSelect);
            if (gameOverHomeButton != null) gameOverHomeButton.onClick.AddListener(ReturnToHome);

            if (rerollButton != null) rerollButton.onClick.AddListener(OnRerollClicked);
            if (continueEndlessButton != null) continueEndlessButton.onClick.AddListener(HandleVictoryContinue);
            if (victoryWorldSelectButton != null) victoryWorldSelectButton.onClick.AddListener(ReturnToWorldSelect);

            if (startRunButton != null) startRunButton.onClick.AddListener(StartRunFromTitle);
            if (openCreditsButton != null) openCreditsButton.onClick.AddListener(ShowCredits);
            if (closeCreditsButton != null) closeCreditsButton.onClick.AddListener(CloseCredits);

            HideAllModals();
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
            ShowToast("CURSED SEED ACQUIRED", "Take it to a Harvest Plot gravestone to gamble.", 4.5f, new Color(0.3f, 1f, 0.45f));
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

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            toastRoot = new GameObject("NotificationToast");
            toastRoot.transform.SetParent(canvas.transform, false);

            RectTransform rt = toastRoot.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.82f);
            rt.anchorMax = new Vector2(0.5f, 0.82f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(620, 72);
            rt.anchoredPosition = Vector2.zero;

            toastBg = toastRoot.AddComponent<Image>();
            toastBg.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);

            GameObject titleObj = new GameObject("ToastTitle");
            titleObj.transform.SetParent(toastRoot.transform, false);
            RectTransform rtTitle = titleObj.AddComponent<RectTransform>();
            rtTitle.anchorMin = new Vector2(0f, 0.5f);
            rtTitle.anchorMax = new Vector2(1f, 1f);
            rtTitle.offsetMin = new Vector2(15, 0);
            rtTitle.offsetMax = new Vector2(-15, -4);
            toastTitleText = titleObj.AddComponent<Text>();
            Font safeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (safeFont == null) safeFont = Font.CreateDynamicFontFromOSFont("Arial", 22);
            toastTitleText.font = safeFont;
            toastTitleText.fontSize = 22;
            toastTitleText.fontStyle = FontStyle.Bold;
            toastTitleText.alignment = TextAnchor.MiddleCenter;
            toastTitleText.color = new Color(1f, 0.85f, 0.2f);

            GameObject subObj = new GameObject("ToastSub");
            subObj.transform.SetParent(toastRoot.transform, false);
            RectTransform rtSub = subObj.AddComponent<RectTransform>();
            rtSub.anchorMin = new Vector2(0f, 0f);
            rtSub.anchorMax = new Vector2(1f, 0.5f);
            rtSub.offsetMin = new Vector2(15, 4);
            rtSub.offsetMax = new Vector2(-15, 0);
            toastSubtitleText = subObj.AddComponent<Text>();
            toastSubtitleText.font = toastTitleText.font;
            toastSubtitleText.fontSize = 16;
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
            if (creditsModal != null && creditsModal.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                {
                    CloseCredits();
                }
                return;
            }

            if (titleModal != null && titleModal.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.C))
                {
                    ShowCredits();
                    return;
                }
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                {
                    StartRunFromTitle();
                }
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (levelUpModal != null && levelUpModal.activeSelf) return; // Don't pause during level up
                TogglePause();
            }

            // Keyboard selection for level-up cards (1, 2, 3 or R)
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
            if (gameOverModal != null) gameOverModal.SetActive(false);
            if (victoryModal != null) victoryModal.SetActive(false);
            if (luckyLastBanner != null) luckyLastBanner.SetActive(false);
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
                // Set both fillAmount and anchorMax so fill works under all sprite/non-sprite modes
                healthBarFill.fillAmount = pct;
                RectTransform rtFill = healthBarFill.rectTransform;
                if (rtFill != null)
                {
                    rtFill.anchorMin = Vector2.zero;
                    rtFill.anchorMax = new Vector2(pct, 1f);
                    rtFill.sizeDelta = Vector2.zero;
                }

                // Flash on hit or heal
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
                return new Color(0.92f, 0.40f, 0.10f); // Cautionary amber-red
            }
            return new Color(0.72f, 0.12f, 0.12f); // Deep blood crimson
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
                    luckyChargesText.text = $"★ {current}";
                    luckyChargesText.color = new Color(0.95f, 0.82f, 0.25f);
                }
                else
                {
                    luckyChargesText.text = "☠ 0";
                    luckyChargesText.color = new Color(0.85f, 0.25f, 0.25f);
                }
            }
        }

        public void ShowLuckyLastBanner(float duration)
        {
            if (luckyLastBanner != null) luckyLastBanner.SetActive(true);
            ShowToast("LUCKY LAST!", $"Death cheated. {Mathf.RoundToInt(duration)} seconds of boosted power.", 2.5f, new Color(1f, 0.35f, 0.2f));
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
            if (xpBarFill != null) xpBarFill.fillAmount = percent;
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
                seedCountText.color = count > 0 ? new Color(0.35f, 0.95f, 0.55f) : new Color(0.55f, 0.65f, 0.55f);
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

        private void OpenLevelUpModal()
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
                return; // All maxed out
            }

            Time.timeScale = 0f; // Pause game

            if (levelUpModal != null)
            {
                levelUpModal.SetActive(true);

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
                    rerollButtonText.text = $"REROLL — {upgradeManager.RerollCharges} LEFT [R]";
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
            Time.timeScale = 1f; // Resume
        }

        // ==================== WAVE HUD & HANDLERS ====================
        private void HandleWaveStarted(int wave)
        {
            int total = SpawnDirector.GetEnemyCountForWave(wave);
            UpdateWaveHUD(wave, total, total);
            if (wave == 10)
            {
                ShowToast("BOSS WAVE", "THE LAST HARVEST", 3.5f, new Color(1f, 0.25f, 0.25f));
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
            if (wave == 10)
            {
                ShowToast("THE LAST HARVEST FALLEN!", "Vacuuming boss spoils...", 2.5f, new Color(1f, 0.85f, 0.2f));
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
                if (wave == 10)
                {
                    waveRemainingText.text = "THE LAST HARVEST";
                    waveRemainingText.color = new Color(1f, 0.35f, 0.25f);
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

        // ==================== PAUSE MODAL ====================
        public void TogglePause()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            isPaused = !isPaused;
            if (pauseModal != null) pauseModal.SetActive(isPaused);
            Time.timeScale = isPaused ? 0f : 1f;
        }

        public void ResumeGame()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            isPaused = false;
            if (pauseModal != null) pauseModal.SetActive(false);
            Time.timeScale = 1f;
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
                if (gameOverStatsText != null)
                {
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
                    WorldConfig activeConfig = (GameSession.Instance != null && GameSession.Instance.CurrentWorldConfig != null) ? GameSession.Instance.CurrentWorldConfig : WorldConfig.GetConfig(1);
                    int wave = FindFirstObjectByType<SpawnDirector>() != null ? FindFirstObjectByType<SpawnDirector>().CurrentWave : 1;

                    int essenceEarned = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.RunEssenceEarned : 0;
                    int totalEssence = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.HarvestEssence : 0;
                    int enemiesSlain = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.RunEnemiesSlain : kills;
                    int harvests = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.RunHarvestsCompleted : 0;

                    gameOverStatsText.text = $"{activeConfig.displayName}\nSURVIVED UNTIL WAVE {wave}\nTIME: {mins:00}:{secs:00} | KILLS: {enemiesSlain} | LEVEL: {lvl}\nHARVESTS CONSECRATED: {harvests}\n\n✦ HARVEST ESSENCE EARNED: +{essenceEarned} ✦\nTOTAL BALANCE: {totalEssence} ✦\n\nSCORE: {score}";
                }
            }
        }

        public void ShowVictoryModal()
        {
            if (victoryModal != null)
            {
                WorldConfig activeConfig = (GameSession.Instance != null && GameSession.Instance.CurrentWorldConfig != null) ? GameSession.Instance.CurrentWorldConfig : WorldConfig.GetConfig(1);

                if (victoryTitleText != null)
                {
                    if (activeConfig.worldId == 1)
                        victoryTitleText.text = "YOU GOT LUCKY.";
                    else if (activeConfig.worldId == 2)
                        victoryTitleText.text = "THE ASHEN ORCHARDS CLEANSED";
                    else if (activeConfig.worldId == 3)
                        victoryTitleText.text = "THE BLACKENED MIRE PURGED";
                    else if (activeConfig.worldId == 4)
                        victoryTitleText.text = "THE BLOODWOOD TIMBER PURGED";
                    else if (activeConfig.worldId == 5)
                        victoryTitleText.text = "THE ECLIPSE IS BROKEN!";
                    else
                        victoryTitleText.text = "WORLD COMPLETE!";
                }

                int essenceEarned = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.RunEssenceEarned : 0;
                int totalEssence = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.HarvestEssence : 0;
                int enemiesSlain = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.RunEnemiesSlain : 0;
                int harvests = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.RunHarvestsCompleted : 0;

                if (victorySubtitleText != null)
                {
                    string worldStory = "";
                    if (activeConfig.worldId == 1)
                        worldStory = "THE LAST HARVEST HAS FALLEN!\nWORLD 1 COMPLETE!\nWORLD 2 — THE ASHEN ORCHARDS UNLOCKED.";
                    else if (activeConfig.worldId == 2)
                        worldStory = "THE ASHEN REAPER HAS FALLEN!\nWORLD 2 COMPLETE!\nWORLD 3 — THE BLACKENED MIRE UNLOCKED.";
                    else if (activeConfig.worldId == 3)
                        worldStory = "THE MIRE MOTHER HAS PERISHED!\nWORLD 3 COMPLETE!\nWORLD 4 — THE BLOODWOOD TIMBER UNLOCKED.";
                    else if (activeConfig.worldId == 4)
                        worldStory = "THE BLOODWOOD SOVEREIGN HAS FALLEN!\nWORLD 4 COMPLETE!\nWORLD 5 — THE ETERNAL ECLIPSE UNLOCKED.";
                    else if (activeConfig.worldId == 5)
                        worldStory = "THE PRIMEVAL HORROR IS VANQUISHED!\n\n★ CAMPAIGN COMPLETE ★\n★ ENDLESS MODE UNLOCKED ★\n\nThe ancient blight recedes, but the night remains hungry.";
                    else
                        worldStory = $"{activeConfig.displayName} PURGED!\nTHE HARVEST RECEDES FROM THIS REALM.";

                    victorySubtitleText.text = $"{worldStory}\n\n--- RUN REWARDS ---\nEnemies Slain: {enemiesSlain} | Harvests: {harvests}\n✦ Essence Consecrated: +{essenceEarned} ✦\nTotal Essence Balance: {totalEssence} ✦";
                }
                if (continueEndlessButton != null)
                {
                    Text btnTxt = continueEndlessButton.GetComponentInChildren<Text>();
                    if (btnTxt != null)
                    {
                        if (activeConfig.worldId == 1)
                            btnTxt.text = "CONTINUE: ONE LAST NIGHT [ENTER]";
                        else if (activeConfig.worldId < 5)
                            btnTxt.text = $"ENTER WORLD {activeConfig.worldId + 1} [ENTER]";
                        else
                            btnTxt.text = "ENTER ENDLESS HORDE [ENTER]";
                    }
                }
                if (victoryWorldSelectButton != null)
                {
                    Text btnTxt = victoryWorldSelectButton.GetComponentInChildren<Text>();
                    if (btnTxt != null) btnTxt.text = "RETURN TO WORLD SELECT";
                }
                victoryModal.SetActive(true);
            }
        }

        public void HandleVictoryContinue()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            if (victoryModal != null) victoryModal.SetActive(false);
            Time.timeScale = 1f;

            WorldConfig activeConfig = (GameSession.Instance != null && GameSession.Instance.CurrentWorldConfig != null) ? GameSession.Instance.CurrentWorldConfig : WorldConfig.GetConfig(1);
            if (activeConfig.worldId == 1 || activeConfig.worldId == 5)
            {
                SpawnDirector sd = FindFirstObjectByType<SpawnDirector>();
                if (sd != null)
                {
                    sd.EnterEndlessMode();
                    sd.StartIntermissionAfterBoss();
                }
                else if (GameSession.Instance != null)
                {
                    GameSession.Instance.ReturnToWorldSelect();
                }
            }
            else
            {
                if (activeConfig.worldId < 5 && GameSession.Instance != null)
                {
                    GameSession.Instance.StartRun(activeConfig.worldId + 1);
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
                bossBarFill.fillAmount = Mathf.Clamp01(currentHp / Mathf.Max(1f, maxHp));
            }
        }

        public void HideBossBar()
        {
            if (bossBarRoot != null) bossBarRoot.SetActive(false);
        }

        // ==================== DYNAMIC HUD & MODAL FALLBACKS ====================
        public void EnsureHUDAndModals()
        {
            Font defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 20);

            // 1. Top-Right HUD Dedicated Container (SafeArea Margin: -28, -24)
            Transform trTrans = transform.Find("TopRightHUD");
            GameObject trObj;
            if (trTrans == null)
            {
                trObj = new GameObject("TopRightHUD");
                trObj.transform.SetParent(transform, false);
                RectTransform rtTr = trObj.AddComponent<RectTransform>();
                rtTr.anchorMin = new Vector2(1, 1);
                rtTr.anchorMax = new Vector2(1, 1);
                rtTr.pivot = new Vector2(1, 1);
                rtTr.sizeDelta = new Vector2(180, 90);
                rtTr.anchoredPosition = new Vector2(-28, -24);
                topRightHUDContainer = rtTr;
            }
            else
            {
                trObj = trTrans.gameObject;
                topRightHUDContainer = trObj.GetComponent<RectTransform>();
                if (topRightHUDContainer != null)
                {
                    topRightHUDContainer.anchorMin = new Vector2(1, 1);
                    topRightHUDContainer.anchorMax = new Vector2(1, 1);
                    topRightHUDContainer.pivot = new Vector2(1, 1);
                    topRightHUDContainer.sizeDelta = new Vector2(180, 90);
                    topRightHUDContainer.anchoredPosition = new Vector2(-28, -24);
                }
            }

            // Ensure NO large opaque background box on the container
            Image trImg = trObj.GetComponent<Image>();
            if (trImg != null) trImg.color = Color.clear;

            // 1a. Isolated HUD Pause Button (44x44 circular/pill button at top-right corner, local (0, 0))
            if (hudPauseButton == null)
            {
                Transform existingBtn = trObj.transform.Find("HudPauseButton") ?? transform.Find("HudPauseButton");
                if (existingBtn != null)
                {
                    existingBtn.SetParent(trObj.transform, false);
                    RectTransform rtBtn = existingBtn.GetComponent<RectTransform>();
                    rtBtn.anchorMin = new Vector2(1, 1);
                    rtBtn.anchorMax = new Vector2(1, 1);
                    rtBtn.pivot = new Vector2(1, 1);
                    rtBtn.sizeDelta = new Vector2(44, 44);
                    rtBtn.anchoredPosition = Vector2.zero;
                    hudPauseButton = existingBtn.GetComponent<Button>();
                }
                else
                {
                    GameObject pBtnObj = CreateSimpleButton(trObj.transform, "HudPauseButton", "||",
                        new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                        new Vector2(44, 44), Vector2.zero,
                        new Color(0.08f, 0.09f, 0.12f, 0.65f),
                        new Color(0.95f, 0.85f, 0.40f), 20, defaultFont);
                    hudPauseButton = pBtnObj.GetComponent<Button>();
                }
            }
            else
            {
                hudPauseButton.transform.SetParent(trObj.transform, false);
                RectTransform rtBtn = hudPauseButton.GetComponent<RectTransform>();
                rtBtn.anchorMin = new Vector2(1, 1);
                rtBtn.anchorMax = new Vector2(1, 1);
                rtBtn.pivot = new Vector2(1, 1);
                rtBtn.sizeDelta = new Vector2(44, 44);
                rtBtn.anchoredPosition = Vector2.zero;
            }

            if (hudPauseButton != null)
            {
                Image btnImg = hudPauseButton.GetComponent<Image>();
                if (btnImg != null) btnImg.color = new Color(0.08f, 0.09f, 0.12f, 0.65f);
                Text btnTxt = hudPauseButton.GetComponentInChildren<Text>();
                if (btnTxt != null) btnTxt.color = new Color(0.95f, 0.85f, 0.40f);
            }

            // 1b. Cursed Seeds Badge (86x34 rounded pill to the left of Pause Button: local (-52, -5))
            Transform sbTrans = trObj.transform.Find("SeedBadge");
            if (sbTrans == null)
            {
                GameObject sbObj = new GameObject("SeedBadge");
                sbObj.transform.SetParent(trObj.transform, false);
                seedBadgeRoot = sbObj.AddComponent<RectTransform>();
                seedBadgeRoot.anchorMin = new Vector2(1, 1);
                seedBadgeRoot.anchorMax = new Vector2(1, 1);
                seedBadgeRoot.pivot = new Vector2(1, 1);
                seedBadgeRoot.sizeDelta = new Vector2(86, 34);
                seedBadgeRoot.anchoredPosition = new Vector2(-52, -5);

                Image sbImg = sbObj.AddComponent<Image>();
                sbImg.color = new Color(0.08f, 0.09f, 0.12f, 0.65f);

                if (seedCountText == null)
                {
                    GameObject txtObj = new GameObject("SeedText");
                    txtObj.transform.SetParent(sbObj.transform, false);
                    RectTransform rtTxt = txtObj.AddComponent<RectTransform>();
                    rtTxt.anchorMin = Vector2.zero;
                    rtTxt.anchorMax = Vector2.one;
                    rtTxt.offsetMin = Vector2.zero;
                    rtTxt.offsetMax = Vector2.zero;
                    Text t = txtObj.AddComponent<Text>();
                    t.font = defaultFont;
                    t.fontSize = 15;
                    t.fontStyle = FontStyle.Bold;
                    t.alignment = TextAnchor.MiddleCenter;
                    t.text = "✦ 0";
                    t.color = new Color(0.95f, 0.85f, 0.35f);
                    seedCountText = t;
                }
                else
                {
                    seedCountText.transform.SetParent(sbObj.transform, false);
                    RectTransform rtTxt = seedCountText.GetComponent<RectTransform>();
                    rtTxt.anchorMin = Vector2.zero;
                    rtTxt.anchorMax = Vector2.one;
                    rtTxt.offsetMin = Vector2.zero;
                    rtTxt.offsetMax = Vector2.zero;
                }
            }
            else
            {
                seedBadgeRoot = sbTrans.GetComponent<RectTransform>();
                seedBadgeRoot.anchorMin = new Vector2(1, 1);
                seedBadgeRoot.anchorMax = new Vector2(1, 1);
                seedBadgeRoot.pivot = new Vector2(1, 1);
                seedBadgeRoot.sizeDelta = new Vector2(86, 34);
                seedBadgeRoot.anchoredPosition = new Vector2(-52, -5);

                Image sbImg = sbTrans.GetComponent<Image>();
                if (sbImg != null) sbImg.color = new Color(0.08f, 0.09f, 0.12f, 0.65f);
            }

            // 1c. Status Effects Container (180x32 below buttons at local (0, -52), right aligned)
            Transform seTrans = trObj.transform.Find("StatusEffectsContainer");
            if (seTrans == null)
            {
                GameObject seObj = new GameObject("StatusEffectsContainer");
                seObj.transform.SetParent(trObj.transform, false);
                statusEffectsContainer = seObj.AddComponent<RectTransform>();
                statusEffectsContainer.anchorMin = new Vector2(1, 1);
                statusEffectsContainer.anchorMax = new Vector2(1, 1);
                statusEffectsContainer.pivot = new Vector2(1, 1);
                statusEffectsContainer.sizeDelta = new Vector2(180, 32);
                statusEffectsContainer.anchoredPosition = new Vector2(0, -52);

                HorizontalLayoutGroup hlg = seObj.AddComponent<HorizontalLayoutGroup>();
                hlg.childAlignment = TextAnchor.MiddleRight;
                hlg.spacing = 6;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;
            }
            else
            {
                statusEffectsContainer = seTrans.GetComponent<RectTransform>();
            }

            // 2. Pause Modal
            if (pauseModal == null)
            {
                Transform existingPause = transform.Find("PauseModal");
                if (existingPause != null)
                {
                    pauseModal = existingPause.gameObject;
                    resumeButton = existingPause.Find("Panel/Body/ResumeButton")?.GetComponent<Button>() ?? existingPause.Find("Panel/ResumeButton")?.GetComponent<Button>();
                    restartButton = existingPause.Find("Panel/Body/RestartButton")?.GetComponent<Button>() ?? existingPause.Find("Panel/RestartButton")?.GetComponent<Button>();
                    pauseWorldSelectButton = existingPause.Find("Panel/Body/WorldSelectButton")?.GetComponent<Button>() ?? existingPause.Find("Panel/WorldSelectButton")?.GetComponent<Button>();
                    pauseHomeButton = existingPause.Find("Panel/Body/HomeButton")?.GetComponent<Button>() ?? existingPause.Find("Panel/HomeButton")?.GetComponent<Button>();
                }
                else
                {
                    pauseModal = CreatePauseModalObject(defaultFont);
                }
            }

            // 3. Game Over Modal World Select & Home buttons
            if (gameOverModal != null)
            {
                if (gameOverWorldSelectButton == null)
                {
                    Transform existingWs = gameOverModal.transform.Find("GameOverWorldSelectButton");
                    if (existingWs != null) gameOverWorldSelectButton = existingWs.GetComponent<Button>();
                    else
                    {
                        GameObject wsBtn = CreateSimpleButton(gameOverModal.transform, "GameOverWorldSelectButton", "WORLD SELECT",
                            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                            new Vector2(230, 46), new Vector2(-125, -195),
                            new Color(0.24f, 0.32f, 0.48f), Color.white, 17, defaultFont);
                        gameOverWorldSelectButton = wsBtn.GetComponent<Button>();
                    }
                }

                if (gameOverHomeButton == null)
                {
                    Transform existingHome = gameOverModal.transform.Find("GameOverHomeButton");
                    if (existingHome != null) gameOverHomeButton = existingHome.GetComponent<Button>();
                    else
                    {
                        GameObject hmBtn = CreateSimpleButton(gameOverModal.transform, "GameOverHomeButton", "HOME",
                            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                            new Vector2(230, 46), new Vector2(125, -195),
                            new Color(0.50f, 0.20f, 0.20f), Color.white, 17, defaultFont);
                        gameOverHomeButton = hmBtn.GetComponent<Button>();
                    }
                }
            }

            // 4. Victory Modal Return to World Select button
            if (victoryModal != null && victoryWorldSelectButton == null)
            {
                Transform existingWs = victoryModal.transform.Find("ReturnWorldSelectButton");
                if (existingWs != null) victoryWorldSelectButton = existingWs.GetComponent<Button>();
                else
                {
                    GameObject wsBtn = CreateSimpleButton(victoryModal.transform, "ReturnWorldSelectButton", "RETURN TO WORLD SELECT",
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(460, 50), new Vector2(0, -185),
                        new Color(0.24f, 0.32f, 0.48f), Color.white, 18, defaultFont);
                    victoryWorldSelectButton = wsBtn.GetComponent<Button>();
                }
            }
        }

        private GameObject CreatePauseModalObject(Font font)
        {
            GameObject modal = new GameObject("PauseModal");
            modal.transform.SetParent(transform, false);
            RectTransform rtModal = modal.AddComponent<RectTransform>();
            rtModal.anchorMin = Vector2.zero;
            rtModal.anchorMax = Vector2.one;
            rtModal.offsetMin = Vector2.zero;
            rtModal.offsetMax = Vector2.zero;

            Image darkBg = modal.AddComponent<Image>();
            darkBg.color = new Color(0.04f, 0.05f, 0.07f, 0.90f);

            // Center Panel
            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(modal.transform, false);
            RectTransform rtPanel = panel.AddComponent<RectTransform>();
            rtPanel.anchorMin = new Vector2(0.5f, 0.5f);
            rtPanel.anchorMax = new Vector2(0.5f, 0.5f);
            rtPanel.pivot = new Vector2(0.5f, 0.5f);
            rtPanel.sizeDelta = new Vector2(440, 440);
            rtPanel.anchoredPosition = Vector2.zero;

            Image panelBorder = panel.AddComponent<Image>();
            panelBorder.color = new Color(0.38f, 0.30f, 0.20f, 1f); // Antique copper border

            GameObject body = new GameObject("Body");
            body.transform.SetParent(panel.transform, false);
            RectTransform rtBody = body.AddComponent<RectTransform>();
            rtBody.anchorMin = Vector2.zero;
            rtBody.anchorMax = Vector2.one;
            rtBody.offsetMin = new Vector2(2, 2);
            rtBody.offsetMax = new Vector2(-2, -2);
            Image bodyImg = body.AddComponent<Image>();
            bodyImg.color = new Color(0.08f, 0.09f, 0.11f, 0.96f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(body.transform, false);
            RectTransform rtTitle = titleObj.AddComponent<RectTransform>();
            rtTitle.anchorMin = new Vector2(0.5f, 0.5f);
            rtTitle.anchorMax = new Vector2(0.5f, 0.5f);
            rtTitle.anchoredPosition = new Vector2(0, 150);
            rtTitle.sizeDelta = new Vector2(400, 50);
            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.font = font;
            titleTxt.text = "PAUSED";
            titleTxt.fontSize = 36;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(1f, 0.82f, 0.28f);

            // Subtitle
            GameObject subObj = new GameObject("Subtitle");
            subObj.transform.SetParent(body.transform, false);
            RectTransform rtSub = subObj.AddComponent<RectTransform>();
            rtSub.anchorMin = new Vector2(0.5f, 0.5f);
            rtSub.anchorMax = new Vector2(0.5f, 0.5f);
            rtSub.anchoredPosition = new Vector2(0, 110);
            rtSub.sizeDelta = new Vector2(400, 30);
            Text subTxt = subObj.AddComponent<Text>();
            subTxt.font = font;
            subTxt.text = "THE CURSED HARVEST";
            subTxt.fontSize = 15;
            subTxt.alignment = TextAnchor.MiddleCenter;
            subTxt.color = new Color(0.85f, 0.82f, 0.75f);

            // Buttons
            GameObject resObj = CreateSimpleButton(body.transform, "ResumeButton", "RESUME [ESC]",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(340, 48), new Vector2(0, 45),
                new Color(0.22f, 0.58f, 0.30f), Color.white, 19, font);
            resumeButton = resObj.GetComponent<Button>();

            GameObject rstObj = CreateSimpleButton(body.transform, "RestartButton", "RESTART RUN",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(340, 48), new Vector2(0, -15),
                new Color(0.58f, 0.38f, 0.18f), Color.white, 19, font);
            restartButton = rstObj.GetComponent<Button>();

            GameObject wsObj = CreateSimpleButton(body.transform, "WorldSelectButton", "WORLD SELECT",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(340, 48), new Vector2(0, -75),
                new Color(0.24f, 0.32f, 0.48f), Color.white, 19, font);
            pauseWorldSelectButton = wsObj.GetComponent<Button>();

            GameObject hmObj = CreateSimpleButton(body.transform, "HomeButton", "HOME",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(340, 48), new Vector2(0, -135),
                new Color(0.50f, 0.20f, 0.20f), Color.white, 19, font);
            pauseHomeButton = hmObj.GetComponent<Button>();

            modal.SetActive(false);
            return modal;
        }

        private static GameObject CreateSimpleButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 size, Vector2 anchoredPos,
            Color bgColor, Color textColor, int fontSize, Font font)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;

            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            ColorBlock cb = btn.colors;
            cb.highlightedColor = Color.Lerp(bgColor, Color.white, 0.25f);
            cb.pressedColor = Color.Lerp(bgColor, Color.black, 0.25f);
            btn.colors = cb;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform rtText = textObj.AddComponent<RectTransform>();
            rtText.anchorMin = Vector2.zero;
            rtText.anchorMax = Vector2.one;
            rtText.offsetMin = Vector2.zero;
            rtText.offsetMax = Vector2.zero;

            Text txt = textObj.AddComponent<Text>();
            txt.font = font;
            txt.text = label;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = textColor;

            return btnObj;
        }

        /// <summary>
        /// Architectural hook for multi-orientation support (WebGL Jam Target = Landscape).
        /// Future mobile portrait builds can leverage this to re-anchor HUD widgets.
        /// </summary>
        public void SetLayoutMode(UILayoutMode mode)
        {
            currentLayoutMode = mode;
            if (currentLayoutMode == UILayoutMode.Landscape)
            {
                if (topRightHUDContainer != null)
                {
                    topRightHUDContainer.anchoredPosition = new Vector2(-28, -24);
                }
            }
            else // Portrait
            {
                if (topRightHUDContainer != null)
                {
                    topRightHUDContainer.anchoredPosition = new Vector2(-16, -20);
                }
            }
        }
    }
}
