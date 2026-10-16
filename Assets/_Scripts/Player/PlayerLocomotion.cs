using UnityEngine;

namespace CursedHarvest
{
    /// <summary>
    /// Procedural humanoid locomotion for the player.
    /// Drives clean, grounded lower-body step cycles synchronized with actual movement displacement,
    /// while keeping the upper body and weapon perfectly stable for combat aiming and shooting.
    /// </summary>
    public class PlayerLocomotion : MonoBehaviour
    {
        [Header("Grounded Lower Body Tuning")]
        [SerializeField] private float cycleDistance = 1.90f;   // Meters covered per 2-step cycle (natural running stride)
        [SerializeField] private float maxStrideAngle = 12.0f;  // Small, clean stride angle (10°-14°)
        [SerializeField] private float maxKneeBend = 5.0f;      // Very moderate knee bend (4°-7°)
        [SerializeField] private float hipBobHeight = 0.004f;   // Minimal downward step compression (3-8mm)
        [SerializeField] private float torsoLeanAngle = 1.5f;   // Very subtle forward lean (max 2°)

        private Transform hips;
        private Transform upperLegL;
        private Transform lowerLegL;
        private Transform upperLegR;
        private Transform lowerLegR;
        private Transform spine;

        private Quaternion restLegLToRoot;
        private Quaternion restKneeLToRoot;
        private Quaternion restLegRToRoot;
        private Quaternion restKneeRToRoot;
        private Quaternion restSpineToRoot;
        private Vector3 restHipsPos;

        private CharacterController characterController;
        private float stridePhase;
        private float currentWeight;
        private Vector3 lastPosition;
        private bool isInitialized;

        private void Awake()
        {
            FindBones();
        }

        public void FindBones()
        {
            if (isInitialized) return;

            characterController = GetComponent<CharacterController>();
            lastPosition = transform.position;

            Transform[] allBones = GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allBones)
            {
                if (hips == null && t.name == "Hips") hips = t;
                if (upperLegL == null && t.name == "UpperLeg_L") upperLegL = t;
                if (lowerLegL == null && t.name == "LowerLeg_L") lowerLegL = t;
                if (upperLegR == null && t.name == "UpperLeg_R") upperLegR = t;
                if (lowerLegR == null && t.name == "LowerLeg_R") lowerLegR = t;
                if (spine == null && (t.name == "Spine_01" || t.name == "Spine_02")) spine = t;
            }

            Quaternion invRoot = Quaternion.Inverse(transform.rotation);
            if (upperLegL != null) restLegLToRoot = invRoot * upperLegL.rotation;
            if (lowerLegL != null) restKneeLToRoot = invRoot * lowerLegL.rotation;
            if (upperLegR != null) restLegRToRoot = invRoot * upperLegR.rotation;
            if (lowerLegR != null) restKneeRToRoot = invRoot * lowerLegR.rotation;
            if (spine != null) restSpineToRoot = invRoot * spine.rotation;
            if (hips != null) restHipsPos = hips.localPosition;

            isInitialized = true;
        }

        private void LateUpdate()
        {
            if (!isInitialized) return;

            // Calculate actual horizontal displacement
            Vector3 disp = transform.position - lastPosition;
            disp.y = 0f;
            float dt = Mathf.Max(0.0001f, Time.deltaTime);
            float speed = disp.magnitude / dt;
            lastPosition = transform.position;

            bool isMoving = speed > 0.15f;
            float targetWeight = isMoving ? 1f : 0f;
            currentWeight = Mathf.MoveTowards(currentWeight, targetWeight, dt * 5.0f);

            // Project motion onto player's local axes to handle strafing and backpedaling
            float forwardComponent = 1f;
            float strafeScale = 1f;

            if (isMoving)
            {
                float fwd = Vector3.Dot(transform.forward, disp.normalized);
                float right = Vector3.Dot(transform.right, disp.normalized);

                // Backpedal: stride phase steps backward
                float moveDirectionSign = fwd < -0.1f ? -1f : 1f;
                float phaseRate = (speed / Mathf.Max(0.5f, cycleDistance)) * (Mathf.PI * 2f) * moveDirectionSign;
                stridePhase += phaseRate * dt;

                if (stridePhase > Mathf.PI * 2f) stridePhase -= Mathf.PI * 2f;
                else if (stridePhase < 0f) stridePhase += Mathf.PI * 2f;

                // When strafing heavily laterally, reduce forward pitch kick so legs don't kick forward awkwardly
                strafeScale = Mathf.Clamp(Mathf.Abs(fwd) + 0.35f, 0.35f, 1.0f);
            }

            // 1. Lower Body Strides (Alternating leg swing in Character-Space Pitch Plane)
            if (upperLegL != null && upperLegR != null)
            {
                float cycle = Mathf.Sin(stridePhase);
                float legAngleL = cycle * maxStrideAngle * currentWeight * strafeScale;
                float legAngleR = -cycle * maxStrideAngle * currentWeight * strafeScale;

                // Knee flexion: folds gently during recovery stroke
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

            // 2. Hip Bobbing (Downward step compression) & Subtle Idle Breathing (2.5mm)
            if (hips != null)
            {
                // Step compression pushes downward into planted foot, keeping sole firmly grounded
                float stepCompression = -Mathf.Abs(Mathf.Sin(stridePhase)) * hipBobHeight * currentWeight;
                // Idle breathing: very subtle 2.5mm rise/fall at calm 1.8 Hz
                float idleBreathing = Mathf.Sin(Time.time * 1.8f) * 0.0025f * (1f - currentWeight);
                hips.localPosition = restHipsPos + new Vector3(0f, stepCompression + idleBreathing, 0f);
            }

            // 3. Torso Lean into Movement (Very subtle, max 1.5°)
            if (spine != null && torsoLeanAngle > 0.01f)
            {
                float forwardLean = torsoLeanAngle * currentWeight * strafeScale;
                spine.rotation = transform.rotation * Quaternion.AngleAxis(forwardLean, Vector3.right) * restSpineToRoot;
            }
        }
    }
}
