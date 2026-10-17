using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CursedHarvest
{
    public enum PlotState
    {
        Ready,
        Planting,
        Growing,
        Resolved,
        Cooldown
    }

    public enum HarvestResult
    {
        None,
        Lucky,
        Cursed
    }

    public class HarvestPlot : MonoBehaviour
    {
        [Header("Plot Configuration")]
        [SerializeField] private float interactionRadius = 2.5f;
        [SerializeField] private float growDuration = 3.5f;
        [SerializeField] private float cooldownDuration = 6.0f;
        [SerializeField] private float baseLuckyChance = 0.70f;

        [Header("Visual References")]
        [SerializeField] private Transform plantSocket;
        [SerializeField] private GameObject growingPumpkin;
        [SerializeField] private Light plotLight;
        [SerializeField] private GameObject luckyVfxPrefab;
        [SerializeField] private GameObject cursedVfxPrefab;

        private PlotState currentState = PlotState.Ready;
        private Transform playerTransform;
        private PlayerHealth playerHealth;
        private PlayerController playerController;
        private WeaponManager weaponManager;
        private UpgradeManager upgradeManager;
        private LuckyLastSystem luckySystem;

        private float plantingTimer;
        private const float PlantingDuration = 0.85f;
        private GameObject plantingSeedVisual;
        private float growTimer;
        private float cooldownTimer;
        private TextMesh promptTextMesh;
        private Transform cameraTransform;

        public PlotState State => currentState;
        public float BaseLuckyChance => baseLuckyChance;

        public static event Action<HarvestResult> OnHarvestGambleResolved;

        private void Awake()
        {
            if (plantSocket == null) plantSocket = transform;
            if (growingPumpkin != null) growingPumpkin.SetActive(false);
            if (plotLight != null)
            {
                // IDLE: Soft atmospheric warm amber light
                plotLight.color = new Color(1.0f, 0.65f, 0.25f);
                plotLight.intensity = 1.2f;
            }

            CreatePromptTextMesh();
        }

        private void CreatePromptTextMesh()
        {
            GameObject promptObj = new GameObject("HarvestPromptText");
            promptObj.transform.SetParent(transform, false);
            promptObj.transform.localPosition = new Vector3(0f, 2.3f, 0f);

            promptTextMesh = promptObj.AddComponent<TextMesh>();
            promptTextMesh.fontSize = 28;
            promptTextMesh.characterSize = 0.08f;
            promptTextMesh.alignment = TextAlignment.Center;
            promptTextMesh.anchor = TextAnchor.MiddleCenter;
            promptTextMesh.fontStyle = FontStyle.Bold;
            promptTextMesh.text = "";
        }

        private void Start()
        {
            FindPlayer();
            if (Camera.main != null) cameraTransform = Camera.main.transform;
        }

        public void Initialize(Transform targetPlayer)
        {
            if (targetPlayer != null)
            {
                playerTransform = targetPlayer;
                playerHealth = targetPlayer.GetComponent<PlayerHealth>();
                playerController = targetPlayer.GetComponent<PlayerController>();
                weaponManager = targetPlayer.GetComponent<WeaponManager>();
                upgradeManager = targetPlayer.GetComponent<UpgradeManager>();
                luckySystem = targetPlayer.GetComponent<LuckyLastSystem>();
            }
        }

        private void FindPlayer()
        {
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    Initialize(player.transform);
                }
            }
        }

        private void Update()
        {
            FindPlayer();
            UpdateBillboardRotation();
            UpdatePromptText();

            if (currentState == PlotState.Ready)
            {
                // Dynamic ambient lighting for Idle vs Player Has Seed
                if (plotLight != null)
                {
                    float dist = playerTransform != null ? Vector3.Distance(transform.position, playerTransform.position) : 99f;
                    if (dist <= interactionRadius + 2.0f && CursedSeedPickup.PlayerSeedCount > 0)
                    {
                        // PLAYER HAS SEED: Subtle emerald pulse / rune brightening
                        float pulse = Mathf.PingPong(Time.time * 2.8f, 1f);
                        plotLight.color = Color.Lerp(new Color(0.25f, 0.95f, 0.45f), new Color(0.5f, 1f, 0.7f), pulse);
                        plotLight.intensity = 1.6f + pulse * 0.8f;
                    }
                    else
                    {
                        // IDLE: Soft warm atmospheric amber light
                        float pulse = Mathf.PingPong(Time.time * 0.8f, 1f);
                        plotLight.color = new Color(1.0f, 0.65f, 0.25f);
                        plotLight.intensity = 1.1f + pulse * 0.35f;
                    }
                }

                CheckPlayerInteraction();
            }
            else if (currentState == PlotState.Planting)
            {
                UpdatePlanting();
            }
            else if (currentState == PlotState.Growing)
            {
                UpdateGrowth();
            }
            else if (currentState == PlotState.Cooldown)
            {
                cooldownTimer -= Time.deltaTime;
                if (cooldownTimer <= 0f)
                {
                    ResetPlot();
                }
            }
        }

        private void UpdateBillboardRotation()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
            if (cameraTransform != null && promptTextMesh != null)
            {
                promptTextMesh.transform.rotation = cameraTransform.rotation;
            }
        }

        private void UpdatePromptText()
        {
            if (promptTextMesh == null) return;
            if (playerTransform == null)
            {
                promptTextMesh.text = "";
                return;
            }

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist > interactionRadius + 2.8f)
            {
                promptTextMesh.text = "";
                return;
            }

            switch (currentState)
            {
                case PlotState.Ready:
                    if (CursedSeedPickup.PlayerSeedCount > 0)
                    {
                        promptTextMesh.text = "[E] PLANT CURSED SEED";
                        promptTextMesh.color = new Color(1f, 0.85f, 0.2f);
                    }
                    else
                    {
                        promptTextMesh.text = "YOU NEED A CURSED SEED";
                        promptTextMesh.color = new Color(0.95f, 0.35f, 0.3f);
                    }
                    break;
                case PlotState.Planting:
                    promptTextMesh.text = "★ PLANTING SEED... ★";
                    promptTextMesh.color = new Color(1f, 0.85f, 0.25f);
                    break;
                case PlotState.Growing:
                    promptTextMesh.text = "HARVESTING...";
                    promptTextMesh.color = new Color(1f, 0.65f, 0.1f);
                    break;
                case PlotState.Cooldown:
                    promptTextMesh.text = $"RECHARGING ({cooldownTimer:0.0}s)";
                    promptTextMesh.color = new Color(0.7f, 0.7f, 0.7f);
                    break;
                default:
                    promptTextMesh.text = "";
                    break;
            }
        }

        private void CheckPlayerInteraction()
        {
            if (playerTransform == null) return;

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            bool inProximity = dist <= interactionRadius;
            bool pressedE = Input.GetKeyDown(KeyCode.E);

            if ((inProximity || (dist <= interactionRadius + 1.2f && pressedE)) && CursedSeedPickup.PlayerSeedCount > 0)
            {
                // Plant seed sequence!
                if (CursedSeedPickup.TryConsumeSeed())
                {
                    StartPlanting();
                }
            }
        }

        public void StartPlanting()
        {
            currentState = PlotState.Planting;
            plantingTimer = 0f;

            if (plantingSeedVisual == null)
            {
                plantingSeedVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                plantingSeedVisual.name = "PlantingSeedVisual";
                plantingSeedVisual.transform.SetParent(transform, false);
                Collider c = plantingSeedVisual.GetComponent<Collider>();
                if (c != null)
                {
                    if (Application.isPlaying) Destroy(c);
                    else DestroyImmediate(c);
                }
                plantingSeedVisual.transform.localScale = Vector3.one * 0.28f;

                Renderer r = plantingSeedVisual.GetComponent<Renderer>();
                if (r != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    mat.color = new Color(0.95f, 0.7f, 0.2f);
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(1.0f, 0.75f, 0.25f) * 2.5f);
                    r.material = mat;
                }
            }

            plantingSeedVisual.SetActive(true);
            plantingSeedVisual.transform.localPosition = new Vector3(0f, 0.85f, 0f);

            if (plotLight != null)
            {
                plotLight.color = new Color(1f, 0.75f, 0.25f);
                plotLight.intensity = 2.2f;
            }
        }

        private void UpdatePlanting()
        {
            plantingTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(plantingTimer / PlantingDuration);

            // Sinks smoothly downward into tilled dirt with gathering light
            if (plantingSeedVisual != null)
            {
                float y = Mathf.Lerp(0.85f, 0.05f, progress);
                plantingSeedVisual.transform.localPosition = new Vector3(0f, y, 0f);
                plantingSeedVisual.transform.localScale = Vector3.one * Mathf.Lerp(0.28f, 0.10f, progress);
            }

            if (plotLight != null)
            {
                plotLight.intensity = Mathf.Lerp(2.2f, 3.0f, progress);
            }

            if (plantingTimer >= PlantingDuration)
            {
                if (plantingSeedVisual != null) plantingSeedVisual.SetActive(false);
                StartGrowing();
            }
        }

        public void StartGrowing()
        {
            currentState = PlotState.Growing;
            growTimer = 0f;

            if (plantingSeedVisual != null) plantingSeedVisual.SetActive(false);

            if (growingPumpkin != null)
            {
                growingPumpkin.SetActive(true);
                growingPumpkin.transform.localScale = Vector3.zero;
            }

            if (plotLight != null)
            {
                plotLight.color = new Color(1f, 0.6f, 0.1f);
                plotLight.intensity = 2f;
            }
        }

        private void UpdateGrowth()
        {
            growTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(growTimer / growDuration);

            // Scale pumpkin with pulsing anticipation
            if (growingPumpkin != null)
            {
                float pulse = 1f + Mathf.Sin(progress * Mathf.PI * 8f) * 0.15f * progress;
                growingPumpkin.transform.localScale = Vector3.one * (progress * pulse);
            }

            // Pulse light with corrupt ritual energy
            if (plotLight != null)
            {
                plotLight.color = new Color(1.0f, 0.55f, 0.15f);
                plotLight.intensity = 1.6f + Mathf.PingPong(Time.time * 4f, 1.4f);
            }

            if (growTimer >= growDuration)
            {
                ResolveHarvest();
            }
        }

        public float CalculateLuckyChance()
        {
            if (playerTransform == null) FindPlayer();
            float luckyChance = baseLuckyChance;

            if (upgradeManager != null)
            {
                luckyChance += upgradeManager.BetterHarvestBonus;
            }

            if (playerHealth != null && playerHealth.HealthPercent <= 0.25f)
            {
                float desperationBonus = (0.25f - playerHealth.HealthPercent) * 0.60f;
                luckyChance += desperationBonus;
            }

            return Mathf.Clamp(luckyChance, 0.10f, 0.95f);
        }

        public HarvestResult ResolveHarvest(float forcedRoll = -1f)
        {
            if (playerTransform == null) FindPlayer();
            float luckyChance = CalculateLuckyChance();

            float roll = forcedRoll >= 0f ? forcedRoll : UnityEngine.Random.value;
            HarvestResult result = (roll <= luckyChance) ? HarvestResult.Lucky : HarvestResult.Cursed;

            ExecuteResult(result);
            return result;
        }

        private void ExecuteResult(HarvestResult result)
        {
            currentState = PlotState.Resolved;

            if (growingPumpkin != null)
            {
                growingPumpkin.SetActive(false);
            }

            string outcomeDetail = "";
            if (result == HarvestResult.Lucky)
            {
                outcomeDetail = ExecuteLuckyReward();
                string title = (outcomeDetail.Contains("RESTORED") || outcomeDetail.Contains("FULL HP"))
                    ? "★ RESTORATIVE HARVEST! ★"
                    : "★ LUCKY HARVEST! ★";
                UIManager.Instance?.ShowHarvestResolutionToast(title, outcomeDetail, true);
            }
            else
            {
                outcomeDetail = ExecuteCursedConsequence();
                UIManager.Instance?.ShowHarvestResolutionToast("☠ CURSED HARVEST! ☠", outcomeDetail, false);
            }

            OnHarvestGambleResolved?.Invoke(result);

            // Enter Cooldown
            currentState = PlotState.Cooldown;
            cooldownTimer = cooldownDuration;
            if (plotLight != null)
            {
                plotLight.color = Color.gray;
                plotLight.intensity = 0.5f;
            }
        }

        private string ExecuteLuckyReward()
        {
            if (plotLight != null) plotLight.color = new Color(0.35f, 1f, 0.45f);
            if (luckyVfxPrefab != null)
            {
                GameObject vfx = Instantiate(luckyVfxPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                if (vfx.GetComponent<VfxAutoDestroy>() == null)
                {
                    if (Application.isPlaying) Destroy(vfx, 1.2f);
                    else DestroyImmediate(vfx);
                }
            }
            AudioManager.Instance?.PlaySound(SoundType.HarvestLucky, 0.95f, 0f);

            float roll = UnityEngine.Random.value;
            if (roll < 0.35f)
            {
                // 1. XP Burst (Drop 6-8 XP pickups)
                if (GameManager.Instance != null && GameManager.Instance.XpPool != null)
                {
                    for (int i = 0; i < 7; i++)
                    {
                        Vector2 circle = UnityEngine.Random.insideUnitCircle * 2.5f;
                        Vector3 pos = transform.position + new Vector3(circle.x, 0.3f, circle.y);
                        GameManager.Instance.XpPool.Get(pos, Quaternion.identity);
                    }
                }
                return "+7 XP Crystals Dropped!";
            }
            else if (roll < 0.65f)
            {
                // 2. Restorative Harvest (Heal 20 HP normally, +25 HP during Lucky Last, or bonus if Full HP)
                float targetHeal = WorldDifficultyConfig.Current.GetEffectiveHealAmount(LuckyLastSystem.IsAnyActive);
                if (playerHealth != null)
                {
                    if (playerHealth.CurrentHealth < playerHealth.MaxHealth)
                    {
                        float healed = playerHealth.Heal(targetHeal);
                        return $"HARVEST RESTORED +{Mathf.RoundToInt(healed)} HP";
                    }
                    else
                    {
                        // Player at full HP: provide speed boost so Lucky reward is never wasted
                        if (playerController != null)
                        {
                            StartCoroutine(TemporarySpeedBuffRoutine(0.12f, 6.0f));
                            return "FULL HP BONUS: +12% SPEED (6s)!";
                        }
                        else if (GameManager.Instance != null && GameManager.Instance.XpPool != null)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                Vector2 circle = UnityEngine.Random.insideUnitCircle * 2.0f;
                                Vector3 pos = transform.position + new Vector3(circle.x, 0.3f, circle.y);
                                GameManager.Instance.XpPool.Get(pos, Quaternion.identity);
                            }
                            return "FULL HP BONUS: +4 XP CRYSTALS!";
                        }
                    }
                }
                return $"HARVEST RESTORED +{Mathf.RoundToInt(targetHeal)} HP";
            }
            else if (roll < 0.85f)
            {
                // 3. Temporary Damage Buff (+35% for 15s)
                if (weaponManager != null)
                {
                    StartCoroutine(TemporaryDamageBuffRoutine(0.35f, 15f));
                }
                return "+35% Damage Boost for 15s!";
            }
            else
            {
                // 4. Lucky Last Recovery
                if (luckySystem != null)
                {
                    luckySystem.AddCharge();
                }
                return "+1 Lucky Last Life Restored!";
            }
        }

        private string ExecuteCursedConsequence()
        {
            if (plotLight != null) plotLight.color = new Color(1f, 0.1f, 0.1f);
            if (cursedVfxPrefab != null)
            {
                GameObject vfx = Instantiate(cursedVfxPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                if (vfx.GetComponent<VfxAutoDestroy>() == null)
                {
                    if (Application.isPlaying) Destroy(vfx, 0.55f);
                    else DestroyImmediate(vfx);
                }
            }
            AudioManager.Instance?.PlaySound(SoundType.HarvestCursed, 0.95f, 0f);
            TopDownCameraFollow.Instance?.TriggerShake(0.14f, 0.10f);

            float roll = UnityEngine.Random.value;
            if (roll < 0.50f)
            {
                // 1. Ambush: spawn 4 enemies around plot
                if (GameManager.Instance != null && GameManager.Instance.EnemyPool != null)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 circle = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(2.5f, 4.5f);
                        Vector3 pos = transform.position + new Vector3(circle.x, 0.5f, circle.y);
                        GameObject eObj = GameManager.Instance.EnemyPool.Get(pos, Quaternion.identity);
                        EnemyController ec = eObj.GetComponent<EnemyController>();
                        if (ec != null && playerTransform != null) ec.SetTarget(playerTransform);
                    }
                }
                return "Monster Ambush Spawned!";
            }
            else if (roll < 0.80f)
            {
                // 2. Damaging / Toxic Zone around plot
                StartCoroutine(ToxicZoneRoutine(6f, 15f));
                return "Toxic Hazard Zone Spawned!";
            }
            else
            {
                // 3. Movement Penalty (Slow 25% for 5s)
                if (playerController != null)
                {
                    StartCoroutine(TemporarySlowRoutine(0.25f, 5f));
                }
                return "-25% Slow for 5s!";
            }
        }

        private IEnumerator TemporaryDamageBuffRoutine(float amount, float duration)
        {
            if (weaponManager != null)
            {
                weaponManager.SetDamageMultiplier(weaponManager.GlobalDamageMultiplier + amount);
                yield return new WaitForSeconds(duration);
                weaponManager.SetDamageMultiplier(Mathf.Max(1f, weaponManager.GlobalDamageMultiplier - amount));
            }
        }

        private IEnumerator TemporarySlowRoutine(float penalty, float duration)
        {
            if (playerController != null)
            {
                playerController.SetSpeedMultiplier(Mathf.Max(0.5f, playerController.MoveSpeed / 6.8f - penalty));
                yield return new WaitForSeconds(duration);
                playerController.SetSpeedMultiplier(playerController.MoveSpeed / 6.8f + penalty);
            }
        }

        private IEnumerator TemporarySpeedBuffRoutine(float speedBonus, float duration)
        {
            if (playerController == null) yield break;
            playerController.SetSpeedMultiplier(1f + speedBonus);
            yield return new WaitForSeconds(duration);
            if (playerController != null)
            {
                playerController.SetSpeedMultiplier(1f);
            }
        }

        private IEnumerator ToxicZoneRoutine(float duration, float damagePerSec)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += 0.5f;
                yield return new WaitForSeconds(0.5f);

                if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) <= interactionRadius + 1f)
                {
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(new DamageData(damagePerSec * 0.5f, gameObject));
                    }
                }
            }
        }

        public void ResetPlot()
        {
            currentState = PlotState.Ready;
            growTimer = 0f;
            plantingTimer = 0f;
            cooldownTimer = 0f;
            if (plantingSeedVisual != null) plantingSeedVisual.SetActive(false);
            if (growingPumpkin != null) growingPumpkin.SetActive(false);
            if (plotLight != null)
            {
                plotLight.color = new Color(1.0f, 0.65f, 0.25f);
                plotLight.intensity = 1.2f;
            }
        }
    }
}
