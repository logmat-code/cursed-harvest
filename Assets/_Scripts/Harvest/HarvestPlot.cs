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
        [SerializeField] private float interactionRadius = 2.8f;
        [SerializeField] private float growDuration = 3.5f;
        [SerializeField] private float cooldownDuration = 6.0f;
        [SerializeField] private float baseLuckyChance = 0.70f;

        [Header("Ground Boundary Visuals (2.8m Folk-Horror Decal Ring)")]
        [SerializeField] private GameObject boundaryVisualRoot;
        private LineRenderer boundaryLine;
        private Material boundaryMaterial;

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
        private float harvestGraceTimer;
        private const float HarvestGraceDuration = 1.0f;
        private bool isHarvestPaused;
        private float interruptedFeedbackTimer;
        private TextMesh promptTextMesh;
        private Transform cameraTransform;

        public PlotState State => currentState;
        public float BaseLuckyChance => baseLuckyChance;
        public float GrowProgress => growDuration > 0f ? Mathf.Clamp01(growTimer / growDuration) : 0f;
        public bool IsHarvestPaused => isHarvestPaused;
        public float HarvestGraceTimer => harvestGraceTimer;
        public float InteractionRadius => interactionRadius;
        public float GrowTimer => growTimer;
        public void SetGrowTimer(float t) => growTimer = t;

        public void SimulateChannelingStep(float dt, float playerDistance)
        {
            bool insideZone = playerDistance <= interactionRadius;
            if (insideZone)
            {
                isHarvestPaused = false;
                harvestGraceTimer = 0f;
                growTimer += dt;
            }
            else
            {
                isHarvestPaused = true;
                harvestGraceTimer += dt;
                if (harvestGraceTimer >= HarvestGraceDuration)
                {
                    InterruptHarvest();
                    return;
                }
            }
        }

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
            EnsureGroundBoundaryVisual();
        }

        private void EnsureGroundBoundaryVisual()
        {
            if (boundaryVisualRoot != null && boundaryLine != null) return;

            Transform existing = transform.Find("HarvestBoundaryVisual");
            if (existing != null)
            {
                boundaryVisualRoot = existing.gameObject;
                boundaryLine = boundaryVisualRoot.GetComponent<LineRenderer>();
                if (boundaryLine != null && boundaryLine.material != null)
                {
                    boundaryMaterial = boundaryLine.material;
                }
                return;
            }

            boundaryVisualRoot = new GameObject("HarvestBoundaryVisual");
            boundaryVisualRoot.transform.SetParent(transform, false);
            boundaryVisualRoot.transform.localPosition = new Vector3(0f, 0.03f, 0f);

            boundaryLine = boundaryVisualRoot.AddComponent<LineRenderer>();
            boundaryLine.useWorldSpace = false;
            boundaryLine.loop = true;
            boundaryLine.startWidth = 0.10f;
            boundaryLine.endWidth = 0.10f;

            int segments = 48;
            boundaryLine.positionCount = segments;
            float radius = interactionRadius; // 2.8f
            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                boundaryLine.SetPosition(i, new Vector3(x, 0f, z));
            }

            // Material: Folk-horror soft rune glow (unlit transparent amber, NOT neon)
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            if (shader != null)
            {
                boundaryMaterial = new Material(shader);
                boundaryMaterial.color = new Color(0.65f, 0.45f, 0.18f, 0.40f);
                boundaryLine.material = boundaryMaterial;
            }

            boundaryLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            boundaryLine.receiveShadows = false;

            // Add 4 subtle small weathered rune markers at Cardinal points (N, S, E, W)
            Vector3[] cardinalOffsets = new Vector3[]
            {
                new Vector3(0f, 0.04f, radius),
                new Vector3(radius, 0.04f, 0f),
                new Vector3(0f, 0.04f, -radius),
                new Vector3(-radius, 0.04f, 0f)
            };
            for (int i = 0; i < cardinalOffsets.Length; i++)
            {
                GameObject runeMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                runeMarker.name = $"RuneMarker_{i}";
                runeMarker.transform.SetParent(boundaryVisualRoot.transform, false);
                runeMarker.transform.localPosition = cardinalOffsets[i];
                runeMarker.transform.localScale = new Vector3(0.18f, 0.08f, 0.18f);
                runeMarker.transform.localRotation = Quaternion.Euler(0f, i * 90f + 45f, 0f);

                Collider col = runeMarker.GetComponent<Collider>();
                if (col != null) DestroyImmediate(col);

                Renderer r = runeMarker.GetComponent<Renderer>();
                if (r != null && boundaryMaterial != null)
                {
                    r.material = boundaryMaterial;
                }
            }
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
            UpdateBoundaryVisualFeedback();

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

            if (interruptedFeedbackTimer > 0f)
            {
                interruptedFeedbackTimer -= Time.deltaTime;
                promptTextMesh.text = "★ HARVEST INTERRUPTED ★\nStay inside the zone!";
                promptTextMesh.color = new Color(0.95f, 0.20f, 0.20f);
                return;
            }

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist > interactionRadius + 3.5f && currentState != PlotState.Growing)
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
                    float progress = Mathf.Clamp01(growTimer / growDuration);
                    string bar = GenerateProgressBar(progress, 8);
                    if (isHarvestPaused)
                    {
                        float graceRemaining = Mathf.Max(0f, HarvestGraceDuration - harvestGraceTimer);
                        promptTextMesh.text = $"HARVEST PAUSED\nRETURN TO HARVEST ({graceRemaining:0.0}s)\n[{bar}] {Mathf.RoundToInt(progress * 100)}%";
                        promptTextMesh.color = new Color(1f, 0.35f, 0.20f);
                    }
                    else
                    {
                        promptTextMesh.text = $"HARVESTING...\n[{bar}] {Mathf.RoundToInt(progress * 100)}%";
                        promptTextMesh.color = new Color(1f, 0.75f, 0.18f);
                    }
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

        private void UpdateBoundaryVisualFeedback()
        {
            if (boundaryMaterial == null) return;

            if (currentState == PlotState.Growing)
            {
                if (isHarvestPaused)
                {
                    // WARNING AMBER-RED
                    boundaryMaterial.color = new Color(0.90f, 0.28f, 0.18f, 0.70f);
                }
                else
                {
                    // CHANNELING PULSE: Warm ritual ember glow
                    float pulse = Mathf.PingPong(Time.time * 2.5f, 1f);
                    boundaryMaterial.color = Color.Lerp(new Color(0.65f, 0.45f, 0.18f, 0.45f), new Color(0.95f, 0.72f, 0.25f, 0.80f), pulse);
                }
            }
            else if (currentState == PlotState.Ready)
            {
                float pulse = Mathf.PingPong(Time.time * 0.8f, 1f);
                boundaryMaterial.color = new Color(0.55f, 0.38f, 0.16f, 0.35f + pulse * 0.12f);
            }
            else
            {
                boundaryMaterial.color = new Color(0.35f, 0.30f, 0.25f, 0.20f);
            }
        }

        private static string GenerateProgressBar(float progress, int segments = 8)
        {
            int filled = Mathf.Clamp(Mathf.RoundToInt(progress * segments), 0, segments);
            System.Text.StringBuilder sb = new System.Text.StringBuilder(segments);
            for (int i = 0; i < filled; i++) sb.Append('█');
            for (int i = filled; i < segments; i++) sb.Append('░');
            return sb.ToString();
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
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            bool insideZone = dist <= interactionRadius;

            if (insideZone)
            {
                isHarvestPaused = false;
                harvestGraceTimer = 0f;
                growTimer += Time.deltaTime;
            }
            else
            {
                // Player has exited harvest zone -> pause progress immediately
                isHarvestPaused = true;
                harvestGraceTimer += Time.deltaTime;

                if (harvestGraceTimer >= HarvestGraceDuration)
                {
                    // Grace period exceeded: reset progress to 0!
                    InterruptHarvest();
                    return;
                }
            }

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
                if (isHarvestPaused)
                {
                    plotLight.color = new Color(0.95f, 0.35f, 0.15f);
                    plotLight.intensity = 1.4f + Mathf.PingPong(Time.time * 6f, 0.8f);
                }
                else
                {
                    plotLight.color = new Color(1.0f, 0.55f, 0.15f);
                    plotLight.intensity = 1.6f + Mathf.PingPong(Time.time * 4f, 1.4f);
                }
            }

            if (growTimer >= growDuration)
            {
                ResolveHarvest();
            }
        }

        public void InterruptHarvest()
        {
            Debug.Log("[HarvestPlot] Harvest interrupted! Player remained outside zone > 1.0s. Resetting progress to 0.");
            growTimer = 0f;
            harvestGraceTimer = 0f;
            isHarvestPaused = false;
            interruptedFeedbackTimer = 1.6f;

            if (growingPumpkin != null)
            {
                growingPumpkin.transform.localScale = Vector3.zero;
                growingPumpkin.SetActive(false);
            }

            if (plotLight != null)
            {
                plotLight.color = new Color(0.95f, 0.20f, 0.20f);
                plotLight.intensity = 1.8f;
            }

            AudioManager.Instance?.PlaySound(SoundType.HarvestCursed, 0.6f);
            currentState = PlotState.Ready;
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
