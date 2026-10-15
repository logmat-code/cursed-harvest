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

        private UpgradeManager upgradeManager;
        private List<UpgradeData> currentChoices = new List<UpgradeData>();
        private bool isPaused;

        public bool IsModalOpen => (levelUpModal != null && levelUpModal.activeSelf) ||
                                  (pauseModal != null && pauseModal.activeSelf) ||
                                  (gameOverModal != null && gameOverModal.activeSelf) ||
                                  (titleModal != null && titleModal.activeSelf);

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

            if (titleModal != null)
            {
                titleModal.SetActive(true);
                Time.timeScale = 0f;
            }
        }

        private void OnDestroy()
        {
            LuckyLastSystem.OnChargesChanged -= UpdateLuckyChargesHUD;
            LuckyLastSystem.OnLuckyLastActivated -= ShowLuckyLastBanner;
            LuckyLastSystem.OnLuckyLastEnded -= HideLuckyLastBanner;
            GameManager.OnEnemiesKilledChanged -= UpdateKillCountHUD;
            SpawnDirector.OnRunTimerUpdated -= UpdateTimerHUD;
        }

        private void Update()
        {
            if (titleModal != null && titleModal.activeSelf)
            {
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
        }

        public void StartRunFromTitle()
        {
            AudioManager.Instance?.PlaySound(SoundType.ButtonClick);
            if (titleModal != null) titleModal.SetActive(false);
            Time.timeScale = 1f;
        }

        public void HideAllModals()
        {
            if (titleModal != null) titleModal.SetActive(false);
            if (levelUpModal != null) levelUpModal.SetActive(false);
            if (pauseModal != null) pauseModal.SetActive(false);
            if (gameOverModal != null) gameOverModal.SetActive(false);
            if (victoryModal != null) victoryModal.SetActive(false);
            if (luckyLastBanner != null) luckyLastBanner.SetActive(false);
        }

        // ==================== HUD UPDATES ====================
        public void UpdateHealthHUD(float current, float max)
        {
            if (healthBarFill != null) healthBarFill.fillAmount = Mathf.Clamp01(current / max);
            if (healthText != null) healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        public void UpdateLuckyChargesHUD(int current, int max)
        {
            if (luckyChargesText != null) luckyChargesText.text = $"LUCKY LAST: {current}/{max}";
        }

        public void ShowLuckyLastBanner(float duration)
        {
            if (luckyLastBanner != null) luckyLastBanner.SetActive(true);
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
            if (killCountText != null) killCountText.text = $"KILLS: {count}";
        }

        public void UpdateSeedCountHUD(int count)
        {
            if (seedCountText != null) seedCountText.text = $"SEEDS: {count}";
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
                    rerollButtonText.text = $"REROLL ({upgradeManager.RerollCharges}) [R]";
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
