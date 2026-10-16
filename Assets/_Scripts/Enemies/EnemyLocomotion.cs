using UnityEngine;

namespace CursedHarvest
{
    /// <summary>
    /// Procedural humanoid locomotion for enemies.
    /// Rotates bones in character space around the transverse pitch axis,
    /// eliminating FBX-axis twisting and ground clipping while giving each enemy archetype
    /// distinct grounded personality, recognizable silhouettes, and organic idle breathing.
    /// </summary>
    public class EnemyLocomotion : MonoBehaviour
    {
        [Header("Locomotion Tuning")]
        [SerializeField] private float cycleDistance = 1.10f;   // Meters covered per 2-step cycle
        [SerializeField] private float maxStrideAngle = 10.0f;  // Forward/back leg swing
        [SerializeField] private float maxKneeBend = 5.0f;      // Near-neutral knee flexion
        [SerializeField] private float hipBobHeight = 0.005f;   // Step impact compression (downward only)
        [SerializeField] private float torsoLeanAngle = 0.0f;   // Spine forward pitch
        [SerializeField] private float lateralRollAngle = 0.0f; // Subtle lateral lean
        [SerializeField] private float shoulderTiltAngle = 0f;  // Asymmetrical posture
        [SerializeField] private float armSwingAngle = 3.0f;    // Arm swing amplitude
        [SerializeField] private float armRestPitch = 0.0f;     // Arm resting hang angle
        [SerializeField] private bool hasWeaponInHand = false;  // Keep weapon arm steady if true
        [SerializeField] private bool isAsymmetricLimp = false; // Mild shambler limp
        [SerializeField] private float breathRate = 2.0f;       // Idle breathing speed (Hz)
        [SerializeField] private float breathAmplitude = 0.003f;// Idle breathing displacement (meters)
        [SerializeField] private float idleMicroShift = 1.0f;   // Idle torso micro-rotation (degrees)

        private Transform hips;
        private Transform upperLegL;
        private Transform lowerLegL;
        private Transform upperLegR;
        private Transform lowerLegR;
        private Transform spine;
        private Transform armR;
        private Transform armL;

        private Quaternion restLegLToRoot;
        private Quaternion restKneeLToRoot;
        private Quaternion restLegRToRoot;
        private Quaternion restKneeRToRoot;
        private Quaternion restSpineToRoot;
        private Quaternion restArmRToRoot;
        private Quaternion restArmLToRoot;
        private Vector3 restHipsPos;

        private EnemyController enemyController;
        private float stridePhase;
        private float currentWeight;
        private float currentChargeWeight;
        private Vector3 lastPosition;
        private bool isInitialized;
        private float seed;
        private EnemyType type;

        private void Awake()
        {
            seed = Random.Range(0f, 100f);
            FindBones();
        }

        public void SetupProfile(EnemyType enemyType)
        {
            type = enemyType;
            switch (type)
            {
                case EnemyType.PumpkinGrunt:
                    // Short, quick steps; slight lateral tilt (1-2°); loose arms; nervous feeling
                    cycleDistance = 0.85f;
                    maxStrideAngle = 11.0f;
                    maxKneeBend = 5.5f;
                    hipBobHeight = 0.005f;
                    torsoLeanAngle = 1.0f;
                    lateralRollAngle = 1.8f;
                    shoulderTiltAngle = 0.0f;
                    armSwingAngle = 4.0f;
                    armRestPitch = 0.0f;
                    hasWeaponInHand = false;
                    isAsymmetricLimp = false;
                    breathRate = 2.8f;
                    breathAmplitude = 0.0025f;
                    idleMicroShift = 1.2f;
                    break;

                case EnemyType.MaskedChaser:
                    // Longer sprint stride; torso lean (5-7°); arms accompanying sprint
                    cycleDistance = 1.30f;
                    maxStrideAngle = 13.0f;
                    maxKneeBend = 6.5f;
                    hipBobHeight = 0.006f;
                    torsoLeanAngle = 6.0f;
                    lateralRollAngle = 0.0f;
                    shoulderTiltAngle = 0.0f;
                    armSwingAngle = 5.0f;
                    armRestPitch = 0.0f;
                    hasWeaponInHand = false;
                    isAsymmetricLimp = false;
                    breathRate = 2.2f;
                    breathAmplitude = 0.0025f;
                    idleMicroShift = 1.0f;
                    break;

                case EnemyType.PlagueWalker:
                    // Slow step; subtle limp; one shoulder slightly lower; dead/dangling arms
                    cycleDistance = 0.95f;
                    maxStrideAngle = 9.0f;
                    maxKneeBend = 4.0f;
                    hipBobHeight = 0.004f;
                    torsoLeanAngle = 1.5f;
                    lateralRollAngle = 0.5f;
                    shoulderTiltAngle = 2.5f;
                    armSwingAngle = 2.0f;
                    armRestPitch = 10.0f;
                    hasWeaponInHand = false;
                    isAsymmetricLimp = true;
                    breathRate = 1.2f;
                    breathAmplitude = 0.003f;
                    idleMicroShift = 0.8f;
                    break;

                case EnemyType.DemonCharger:
                    // Heavy & stable walking; forward charge (6-8°) with raised firm weapon
                    cycleDistance = 1.20f;
                    maxStrideAngle = 10.0f;
                    maxKneeBend = 5.0f;
                    hipBobHeight = 0.005f;
                    torsoLeanAngle = 0.0f;
                    lateralRollAngle = 0.0f;
                    shoulderTiltAngle = 0.0f;
                    armSwingAngle = 2.0f;
                    armRestPitch = 0.0f;
                    hasWeaponInHand = true;
                    isAsymmetricLimp = false;
                    breathRate = 1.8f;
                    breathAmplitude = 0.003f;
                    idleMicroShift = 0.8f;
                    break;

                case EnemyType.CursedFarmer:
                    // Slow rustic march; tool held steady; free arm gentle swing
                    cycleDistance = 1.10f;
                    maxStrideAngle = 10.0f;
                    maxKneeBend = 5.0f;
                    hipBobHeight = 0.005f;
                    torsoLeanAngle = 0.0f;
                    lateralRollAngle = 0.0f;
                    shoulderTiltAngle = 0.0f;
                    armSwingAngle = 3.5f;
                    armRestPitch = 0.0f;
                    hasWeaponInHand = true;
                    isAsymmetricLimp = false;
                    breathRate = 1.5f;
                    breathAmplitude = 0.0025f;
                    idleMicroShift = 1.0f;
                    break;

                case EnemyType.HarvestBrute:
                case EnemyType.EliteScarecrow:
                default:
                    // Massive grounded strides; torso nearly immobile; vertical step impact
                    cycleDistance = 1.50f;
                    maxStrideAngle = 11.0f;
                    maxKneeBend = 5.0f;
                    hipBobHeight = 0.008f;
                    torsoLeanAngle = 0.0f;
                    lateralRollAngle = 0.0f;
                    shoulderTiltAngle = 0.0f;
                    armSwingAngle = 2.0f;
                    armRestPitch = 0.0f;
                    hasWeaponInHand = true;
                    isAsymmetricLimp = false;
                    breathRate = 1.0f;
                    breathAmplitude = 0.0035f;
                    idleMicroShift = 0.6f;
                    break;
            }
        }

        public void FindBones()
        {
            if (isInitialized) return;

            enemyController = GetComponent<EnemyController>();
            lastPosition = transform.position;

            Transform[] allBones = GetComponentsInChildren<Transform>(true);

            foreach (Transform t in allBones)
            {
                if (hips == null && t.name == "Hips") hips = t;
                if (upperLegL == null && t.name == "UpperLeg_L") upperLegL = t;
                if (lowerLegL == null && t.name == "LowerLeg_L") lowerLegL = t;
                if (upperLegR == null && t.name == "UpperLeg_R") upperLegR = t;
                if (lowerLegR == null && t.name == "LowerLeg_R") lowerLegR = t;
                if (spine == null && (t.name == "Spine_02" || t.name == "Spine_01")) spine = t;
                if (armR == null && t.name == "UpperArm_R") armR = t;
                if (armL == null && t.name == "UpperArm_L") armL = t;
            }

            if (armR == null)
            {
                foreach (Transform t in allBones) if (t.name.Contains("Arm_R") || t.name == "Shoulder_R") { armR = t; break; }
            }
            if (armL == null)
            {
                foreach (Transform t in allBones) if (t.name.Contains("Arm_L") || t.name == "Shoulder_L") { armL = t; break; }
            }

            // Capture rest orientation relative to character root
            Quaternion invRoot = Quaternion.Inverse(transform.rotation);
            if (upperLegL != null) restLegLToRoot = invRoot * upperLegL.rotation;
            if (lowerLegL != null) restKneeLToRoot = invRoot * lowerLegL.rotation;
            if (upperLegR != null) restLegRToRoot = invRoot * upperLegR.rotation;
            if (lowerLegR != null) restKneeRToRoot = invRoot * lowerLegR.rotation;
            if (spine != null) restSpineToRoot = invRoot * spine.rotation;
            if (armR != null) restArmRToRoot = invRoot * armR.rotation;
            if (armL != null) restArmLToRoot = invRoot * armL.rotation;
            if (hips != null) restHipsPos = hips.localPosition;

            isInitialized = true;
        }

        private void LateUpdate()
        {
            if (!isInitialized) return;

            // Compute actual horizontal speed
            Vector3 disp = transform.position - lastPosition;
            disp.y = 0f;
            float dt = Mathf.Max(0.0001f, Time.deltaTime);
            float speed = disp.magnitude / dt;
            lastPosition = transform.position;

            bool isMoving = speed > 0.08f;
            float targetWeight = isMoving ? 1f : 0f;
            currentWeight = Mathf.MoveTowards(currentWeight, targetWeight, dt * 5.0f);

            // Smooth charge transition
            bool isCharging = enemyController != null && enemyController.IsCharging;
            float targetCharge = isCharging ? 1f : 0f;
            currentChargeWeight = Mathf.MoveTowards(currentChargeWeight, targetCharge, dt * 4.0f);

            float effCycleDist = Mathf.Lerp(cycleDistance, 1.45f, currentChargeWeight);
            float effStride = Mathf.Lerp(maxStrideAngle, 14.0f, currentChargeWeight);
            float effLean = Mathf.Lerp(torsoLeanAngle, 7.0f, currentChargeWeight);

            // Stride phase synchronized with displacement distance
            if (isMoving)
            {
                float phaseRate = (speed / Mathf.Max(0.5f, effCycleDist)) * (Mathf.PI * 2f);
                stridePhase += phaseRate * dt;
                if (stridePhase > Mathf.PI * 2f) stridePhase -= Mathf.PI * 2f;
            }

            // 1. Lower Body Strides (Leg Alternation in Character-Space Pitch Plane)
            if (upperLegL != null && upperLegR != null)
            {
                float cycle = Mathf.Sin(stridePhase);
                float legAngleL = cycle * effStride * currentWeight;
                float legAngleR = -cycle * effStride * currentWeight;

                if (isAsymmetricLimp)
                {
                    legAngleR *= 0.85f; // Subtle 15% limp
                }

                // Knee flexion: folds gently backward during recovery stroke
                float kneeL = Mathf.Max(0f, -Mathf.Cos(stridePhase)) * maxKneeBend * currentWeight;
                float kneeR = Mathf.Max(0f, Mathf.Cos(stridePhase)) * maxKneeBend * currentWeight;

                upperLegL.rotation = transform.rotation * Quaternion.AngleAxis(legAngleL, Vector3.right) * restLegLToRoot;
                upperLegR.rotation = transform.rotation * Quaternion.AngleAxis(legAngleR, Vector3.right) * restLegRToRoot;

                if (lowerLegL != null)
                {
                    lowerLegL.rotation = transform.rotation * Quaternion.AngleAxis(legAngleL - kneeL, Vector3.right) * restKneeLToRoot;
                }
                if (lowerLegR != null)
                {
                    lowerLegR.rotation = transform.rotation * Quaternion.AngleAxis(legAngleR - kneeR, Vector3.right) * restKneeRToRoot;
                }
            }

            // 2. Hip Bobbing (Step Compression) & Idle Breathing
            if (hips != null)
            {
                float stepCompression = -Mathf.Abs(Mathf.Sin(stridePhase)) * hipBobHeight * currentWeight;
                float idleBreathing = Mathf.Sin(Time.time * breathRate + seed) * breathAmplitude * (1f - currentWeight);
                hips.localPosition = restHipsPos + new Vector3(0f, stepCompression + idleBreathing, 0f);
            }

            // 3. Torso Posture & Micro-Shift
            if (spine != null)
            {
                float forwardLean = effLean * currentWeight;
                float walkRoll = Mathf.Sin(stridePhase) * lateralRollAngle * currentWeight;
                float idlePitch = Mathf.Sin(Time.time * breathRate + seed) * 1.0f * (1f - currentWeight);
                float idleYaw = Mathf.Sin(Time.time * 0.7f + seed) * idleMicroShift * (1f - currentWeight);

                Quaternion posture = Quaternion.Euler(forwardLean + idlePitch, idleYaw, walkRoll + shoulderTiltAngle);
                spine.rotation = transform.rotation * posture * restSpineToRoot;
            }

            // 4. Arms (Character space with stable weapons and natural sway)
            if (armL != null)
            {
                float swingL = -Mathf.Sin(stridePhase) * armSwingAngle * currentWeight;
                float idleHangL = Mathf.Sin(Time.time * breathRate * 0.5f + seed) * 1.0f * (1f - currentWeight);
                Quaternion rotL = Quaternion.Euler(armRestPitch + swingL + idleHangL, 0f, 0f);
                armL.rotation = transform.rotation * rotL * restArmLToRoot;
            }

            if (armR != null)
            {
                if (hasWeaponInHand)
                {
                    // Weapon held firmly in front; raised slightly during charge
                    float readyTilt = Mathf.Lerp(0f, 8.0f, currentChargeWeight);
                    Quaternion rotR = Quaternion.Euler(readyTilt, 0f, 0f);
                    armR.rotation = transform.rotation * rotR * restArmRToRoot;
                }
                else
                {
                    float swingR = Mathf.Sin(stridePhase) * armSwingAngle * currentWeight;
                    float idleHangR = Mathf.Sin(Time.time * breathRate * 0.5f + seed + 1f) * 1.0f * (1f - currentWeight);
                    Quaternion rotR = Quaternion.Euler(armRestPitch + swingR + idleHangR, 0f, 0f);
                    armR.rotation = transform.rotation * rotR * restArmRToRoot;
                }
            }
        }
    }
}
