using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CursedHarvest
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("HUD References")]
        [SerializeField] private Image healthBarFill;
        [SerializeField] private Text healthText;
        [SerializeField] private Text luckyChargesText;
        [SerializeField] private GameObject luckyLastBanner;
        [SerializeField] private Text luckyLastTimerText;
        [SerializeField] private Text timerText;
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

        [Header("Game Over Modal")]
        [SerializeField] private GameObject gameOverModal;
        [SerializeField] private Text gameOverStatsText;
        [SerializeField] private Button retryButton;

        [Header("Boss HUD")]
        [SerializeField] private GameObject bossBarRoot;
        [SerializeField] private Image bossBarFill;
        [SerializeField] private Text bossNameText;

        [Header("Boss Victory / Endless Modal")]
        [SerializeField] private GameObject victoryModal;
        [SerializeField] private Text victoryTitleText;
        [SerializeField] private Button continueEndlessButton;

        [Header("Title / Instructions Modal")]
        [SerializeField] private GameObject titleModal;
        [SerializeField] private Button startRunButton;
        [SerializeField] private Button openCreditsButton;

        [Header("Credits Modal")]
        [SerializeField] private GameObject creditsModal;
        [SerializeField] private Button closeCreditsButton;

        private UpgradeManager upgradeManager;
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

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
            if (restartButton != null) restartButton.onClick.AddListener(RestartRun);
            if (retryButton != null) retryButton.onClick.AddListener(RestartRun);
            if (rerollButton != null) rerollButton.onClick.AddListener(OnRerollClicked);
            if (continueEndlessButton != null) continueEndlessButton.onClick.AddListener(CloseVictoryModal);
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
            }

            LuckyLastSystem.OnChargesChanged += UpdateLuckyChargesHUD;
            LuckyLastSystem.OnLuckyLastActivated += ShowLuckyLastBanner;
            LuckyLastSystem.OnLuckyLastEnded += HideLuckyLastBanner;

            GameManager.OnEnemiesKilledChanged += UpdateKillCountHUD;
            SpawnDirector.OnRunTimerUpdated += UpdateTimerHUD;
            CursedSeedPickup.OnFirstSeedCollected += HandleFirstSeedCollected;

            // Start game immediately without blocking text modal
            if (titleModal != null)
            {
                titleModal.SetActive(false);
            }
            Time.timeScale = 1f;

            // Onboarding movement toast (Paso 10)
            ShowToast("WASD — MOVE  •  SPACE — DASH", "Weapons fire automatically.", 4.5f, new Color(1f, 0.85f, 0.2f));
        }

        private void OnDestroy()
        {
            LuckyLastSystem.OnChargesChanged -= UpdateLuckyChargesHUD;
            LuckyLastSystem.OnLuckyLastActivated -= ShowLuckyLastBanner;
            LuckyLastSystem.OnLuckyLastEnded -= HideLuckyLastBanner;
            GameManager.OnEnemiesKilledChanged -= UpdateKillCountHUD;
            SpawnDirector.OnRunTimerUpdated -= UpdateTimerHUD;
            CursedSeedPickup.OnFirstSeedCollected -= HandleFirstSeedCollected;
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
            EnsureToastUI();
            if (toastCoroutine != null) StopCoroutine(toastCoroutine);
            toastCoroutine = StartCoroutine(ToastRoutine(title, subtitle, duration, accentColor ?? new Color(1f, 0.85f, 0.2f)));
        }

        private void EnsureToastUI()
        {
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
            ShowToast("LUCKY LAST!", "Death cheated. 8 seconds of boosted power.", 2.5f, new Color(1f, 0.35f, 0.2f));
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
            if (upgradeManager == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) upgradeManager = player.GetComponent<UpgradeManager>();
            }

            if (upgradeManager == null) return;

            currentChoices = upgradeManager.GetRandomUpgradeChoices(3);
            if (currentChoices.Count == 0) return; // All maxed out

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

            if (levelUpModal != null) levelUpModal.SetActive(false);
            Time.timeScale = 1f; // Resume
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
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
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
                    gameOverStatsText.text = $"TIME: {mins:00}:{secs:00}\nKILLS: {kills}\nLEVEL: {lvl}\n\nFINAL SCORE: {score}";
                }
            }
        }

        public void ShowVictoryModal()
        {
            if (victoryModal != null)
            {
                victoryModal.SetActive(true);
            }
        }

        public void CloseVictoryModal()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            if (victoryModal != null) victoryModal.SetActive(false);
            Time.timeScale = 1f;
            SpawnDirector sd = FindFirstObjectByType<SpawnDirector>();
            if (sd != null) sd.EnterEndlessMode();
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
    }
}
