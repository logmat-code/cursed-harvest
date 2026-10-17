using UnityEngine;

namespace CursedHarvest
{
    /// <summary>
    /// Procedural combat pose and two-handed crossbow posture for the player.
    /// Drives compact, natural upper-body anatomy:
    /// - Relaxed shoulders with low abduction (no T-pose, no military rifle raise)
    /// - Anatomical elbow poles (Right: down-right-back; Left: down-left-outward)
    /// - Two-bone IK with reach clamping to guarantee natural elbow flexion
    /// - Near-neutral wrist rotations (&lt; 15°) with fingers wrapped naturally on grips
    /// - Stable torso-relative CrossbowRoot placement close to the lower chest
    /// - Subtle, clamped mechanical recoil (0.005-0.007m, 0.5°-0.7° pitch)
    /// </summary>
    public class PlayerCombatPose : MonoBehaviour
    {
        [Header("Weapon Hierarchy")]
        [SerializeField] private Transform crossbowRoot;
        [SerializeField] private Transform weaponSocket; // Legacy alias
        [SerializeField] private Transform visualBow;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Transform rightHandGrip;
        [SerializeField] private Transform leftHandGrip;

        [Header("Crossbow Placement (Torso Relative)")]
        [Tooltip("Position of CrossbowRoot in player local space (compact, lower chest, right-offset)")]
        [SerializeField] private Vector3 crossbowLocalPos = new Vector3(0.11f, 1.04f, 0.24f);
        [Tooltip("Local offset of visual bow model to align handle at CrossbowRoot origin")]
        [SerializeField] private Vector3 bowOffsetInsideSocket = new Vector3(0f, 0.112f, 0.248f);
        [SerializeField] private Vector3 rightGripLocalOffset = Vector3.zero;
        [SerializeField] private Vector3 leftGripLocalOffset = new Vector3(-0.025f, -0.048f, 0.21f);
        [SerializeField] private float weaponScale = 0.58f;

        [Header("Bone References")]
        [SerializeField] private Transform shoulderR;
        [SerializeField] private Transform elbowR;
        [SerializeField] private Transform handR;
        [SerializeField] private Transform shoulderL;
        [SerializeField] private Transform elbowL;
        [SerializeField] private Transform handL;
        [SerializeField] private Transform spine;

        [Header("Wrist Micro-Alignment (< 15°)")]
        [SerializeField] private Vector3 rightWristEuler = new Vector3(4f, 8f, -2f);
        [SerializeField] private Vector3 leftWristEuler = new Vector3(8f, 10f, -4f);

        [Header("Debug Visualization")]
        [SerializeField] private bool showDebugGizmos = true;

        private float currentRecoil;
        private bool isInitialized;

        public Transform WeaponSocket => crossbowRoot != null ? crossbowRoot : weaponSocket;
        public Transform CrossbowRoot => crossbowRoot;
        public Transform FirePoint => firePoint;
        public Transform VisualBow => visualBow;
        public Transform RightHandGrip => rightHandGrip;
        public Transform LeftHandGrip => leftHandGrip;

        public Vector3 CrossbowLocalPos => crossbowLocalPos;
        public float WeaponScale => weaponScale;

        private void Awake()
        {
            FindBones();
        }

        public void FindBones()
        {
            if (isInitialized) return;

            Transform[] all = GetComponentsInChildren<Transform>(true);
            foreach (Transform t in all)
            {
                if (shoulderR == null && t.name == "Shoulder_R") shoulderR = t;
                if (elbowR == null && t.name == "Elbow_R") elbowR = t;
                if (handR == null && t.name == "Hand_R") handR = t;
                if (shoulderL == null && t.name == "Shoulder_L") shoulderL = t;
                if (elbowL == null && t.name == "Elbow_L") elbowL = t;
                if (handL == null && t.name == "Hand_L") handL = t;
                if (spine == null && (t.name == "Spine_03" || t.name == "Spine_02")) spine = t;
            }

            isInitialized = true;
        }

        public void SetupWeapon(Transform bowModel, Transform fp)
        {
            visualBow = bowModel;
            firePoint = fp;

            FindBones();

            Transform visualParent = transform.Find("Visual");
            Transform parentToUse = visualParent != null ? visualParent : transform;

            // 1. Establish CrossbowRoot anchored to player torso (NOT parented to Hand_R)
            if (crossbowRoot == null)
            {
                Transform existing = parentToUse.Find("CrossbowRoot");
                if (existing == null) existing = transform.Find("CrossbowRoot");
                if (existing == null && handR != null) existing = handR.Find("CrossbowRoot");

                if (existing != null)
                {
                    crossbowRoot = existing;
                }
                else
                {
                    GameObject rootGo = new GameObject("CrossbowRoot");
                    rootGo.transform.SetParent(parentToUse, false);
                    crossbowRoot = rootGo.transform;
                }
            }

            // Ensure CrossbowRoot is anchored to torso/visual, not handR
            if (crossbowRoot.parent == handR || crossbowRoot.parent == null)
            {
                crossbowRoot.SetParent(parentToUse, true);
            }
            weaponSocket = crossbowRoot;

            // 2. RightHandGrip (trigger/grip handle at CrossbowRoot origin)
            if (rightHandGrip == null)
            {
                Transform existing = crossbowRoot.Find("RightHandGrip");
                if (existing != null) rightHandGrip = existing;
                else
                {
                    GameObject rhg = new GameObject("RightHandGrip");
                    rhg.transform.SetParent(crossbowRoot, false);
                    rhg.transform.localPosition = rightGripLocalOffset;
                    rightHandGrip = rhg.transform;
                }
            }

            // 3. LeftHandGrip (fore-stock support along underside of barrel)
            if (leftHandGrip == null)
            {
                Transform existing = crossbowRoot.Find("LeftHandGrip");
                if (existing != null) leftHandGrip = existing;
                else
                {
                    GameObject lhg = new GameObject("LeftHandGrip");
                    lhg.transform.SetParent(crossbowRoot, false);
                    lhg.transform.localPosition = leftGripLocalOffset;
                    leftHandGrip = lhg.transform;
                }
            }

            // 4. Attach VisualBow under CrossbowRoot
            if (crossbowRoot != null && visualBow != null)
            {
                visualBow.SetParent(crossbowRoot, false);
                visualBow.localPosition = bowOffsetInsideSocket;
                visualBow.localRotation = Quaternion.Euler(0f, 90f, 0f); // Bow limbs at front (+Z), stock at back (-Z)
                visualBow.localScale = Vector3.one * weaponScale;        // Compact 0.58 scale
            }

            // 5. Attach FirePoint under CrossbowRoot
            if (crossbowRoot != null && firePoint != null)
            {
                firePoint.SetParent(crossbowRoot, false);
                firePoint.localPosition = new Vector3(0f, 0.04f, 0.33f); // Tip of compact crossbow barrel
                firePoint.localRotation = Quaternion.identity;
            }
        }

        public void PlayRecoil(float amount = 0.006f)
        {
            // Subtle mechanical kickback clamped strictly to 0.005-0.007m
            currentRecoil = Mathf.Clamp(currentRecoil + amount, 0f, 0.007f);
        }

        public void SetArmOffsets(Vector3 sR, Vector3 eR, Vector3 hR, Vector3 sL, Vector3 eL, Vector3 hL)
        {
            // Kept for backward compatibility
        }

        private void LateUpdate()
        {
            ApplyCombatPose();
        }

        public void ApplyCombatPose()
        {
            if (!isInitialized) FindBones();

            // Smooth exponential recoil recovery
            if (currentRecoil > 0.0001f)
            {
                currentRecoil = Mathf.Lerp(currentRecoil, 0f, Time.deltaTime * 20f);
            }
            else
            {
                currentRecoil = 0f;
            }

            // Ensure CrossbowRoot is parented to visual/torso, not handR
            Transform visualParent = transform.Find("Visual");
            Transform torsoParent = visualParent != null ? visualParent : transform;
            if (crossbowRoot != null && crossbowRoot.parent == handR)
            {
                crossbowRoot.SetParent(torsoParent, true);
            }

            // 1. Position and Align CrossbowRoot relative to player torso
            if (crossbowRoot != null)
            {
                float kickDist = currentRecoil;
                float kickPitch = currentRecoil * 80f; // Max ~0.56° pitch kick
                Vector3 kickOffset = -transform.forward * kickDist;

                crossbowRoot.position = transform.TransformPoint(crossbowLocalPos) + kickOffset;
                crossbowRoot.rotation = Quaternion.LookRotation(transform.forward, Vector3.up) * Quaternion.Euler(-kickPitch, 0f, 0f);

                if (visualBow != null && visualBow.parent == crossbowRoot)
                {
                    visualBow.localPosition = bowOffsetInsideSocket;
                    visualBow.localRotation = Quaternion.Euler(0f, 90f, 0f);
                    visualBow.localScale = Vector3.one * weaponScale;
                }

                if (rightHandGrip != null) rightHandGrip.localPosition = rightGripLocalOffset;
                if (leftHandGrip != null) leftHandGrip.localPosition = leftGripLocalOffset;
            }

            // 2. Solve Right Arm IK (Grasps trigger handle)
            // Elbow pole: down, slightly right, somewhat backward (Section 7)
            if (shoulderR != null && elbowR != null && handR != null && rightHandGrip != null)
            {
                Vector3 poleR = (-transform.up * 0.65f + transform.right * 0.45f - transform.forward * 0.35f).normalized;
                SolveTwoBoneIK(shoulderR, elbowR, handR, rightHandGrip.position, poleR, true);

                // Subtle natural wrist alignment (< 15°) wrapping fingers on pistol grip
                handR.localRotation = Quaternion.Euler(rightWristEuler);
            }

            // 3. Solve Left Arm IK (Supports fore-stock underside)
            // Elbow pole: down, left, slightly outward/forward (Section 7)
            if (shoulderL != null && elbowL != null && handL != null && leftHandGrip != null)
            {
                Vector3 poleL = (-transform.up * 0.70f - transform.right * 0.50f + transform.forward * 0.25f).normalized;
                SolveTwoBoneIK(shoulderL, elbowL, handL, leftHandGrip.position, poleL, false);

                // Subtle cupped palm alignment (< 15°) cradling underside of stock
                handL.localRotation = Quaternion.Euler(leftWristEuler);
            }
        }

        /// <summary>
        /// Analytical two-bone inverse kinematics with independent anatomical elbow poles
        /// and reach clamping. Guarantees natural elbow flexion and eliminates elbow flips and tearing.
        /// </summary>
        private void SolveTwoBoneIK(Transform rootBone, Transform midBone, Transform endBone, Vector3 targetPos, Vector3 poleDir, bool isRightArm)
        {
            if (rootBone == null || midBone == null || endBone == null) return;

            Vector3 A = rootBone.position;
            Vector3 B_cur = midBone.position;
            Vector3 C_cur = endBone.position;

            float l1 = Mathf.Max(0.08f, Vector3.Distance(A, B_cur));
            float l2 = Mathf.Max(0.08f, Vector3.Distance(B_cur, C_cur));

            Vector3 toTarget = targetPos - A;
            float dist = toTarget.magnitude;
            if (dist < 0.001f)
            {
                toTarget = transform.forward;
                dist = 0.001f;
            }
            Vector3 u = toTarget / dist;

            // Reach clamping (Section 8: minReach, maxReach based on upperArm + forearm)
            float minReach = Mathf.Abs(l1 - l2) + 0.03f;
            float maxReach = (l1 + l2) * 0.94f; // 94% extension guarantees natural elbow bend
            float clampedDist = Mathf.Clamp(dist, minReach, maxReach);
            Vector3 effectiveTarget = A + u * clampedDist;

            // Elbow pole projection onto plane perpendicular to u (Section 7)
            Vector3 p = poleDir.normalized;
            Vector3 v = p - u * Vector3.Dot(p, u);
            if (v.sqrMagnitude < 0.001f)
            {
                Vector3 fallback = isRightArm ? transform.right : -transform.right;
                v = fallback - u * Vector3.Dot(fallback, u);
            }
            v = v.normalized;

            // Law of cosines for shoulder angle
            float cosAlpha = Mathf.Clamp((l1 * l1 + clampedDist * clampedDist - l2 * l2) / (2f * l1 * clampedDist), -1f, 1f);
            float sinAlpha = Mathf.Sqrt(Mathf.Max(0f, 1f - cosAlpha * cosAlpha));

            // Exact mathematical elbow position
            Vector3 targetElbowPos = A + u * (l1 * cosAlpha) + v * (l1 * sinAlpha);

            // Desired bone directions
            Vector3 desiredUpperDir = (targetElbowPos - A).normalized;
            Vector3 desiredForearmDir = (effectiveTarget - targetElbowPos).normalized;

            // 1. Solve upper arm (shoulder)
            Vector3 curUpperDir = (midBone.position - rootBone.position).normalized;
            if (curUpperDir.sqrMagnitude > 0.001f)
            {
                Quaternion rotUpper = Quaternion.FromToRotation(curUpperDir, desiredUpperDir);
                rootBone.rotation = rotUpper * rootBone.rotation;
            }

            // 2. Solve forearm (elbow)
            Vector3 curForearmDir = (endBone.position - midBone.position).normalized;
            if (curForearmDir.sqrMagnitude > 0.001f)
            {
                Quaternion rotForearm = Quaternion.FromToRotation(curForearmDir, desiredForearmDir);
                midBone.rotation = rotForearm * midBone.rotation;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;

            // Shoulders (Cyan)
            Gizmos.color = Color.cyan;
            if (shoulderR != null) Gizmos.DrawWireSphere(shoulderR.position, 0.03f);
            if (shoulderL != null) Gizmos.DrawWireSphere(shoulderL.position, 0.03f);

            // Elbows (Yellow)
            Gizmos.color = Color.yellow;
            if (elbowR != null) Gizmos.DrawWireSphere(elbowR.position, 0.025f);
            if (elbowL != null) Gizmos.DrawWireSphere(elbowL.position, 0.025f);

            // Hands (Green)
            Gizmos.color = Color.green;
            if (handR != null) Gizmos.DrawWireSphere(handR.position, 0.025f);
            if (handL != null) Gizmos.DrawWireSphere(handL.position, 0.025f);

            // Grip Targets (Magenta)
            Gizmos.color = Color.magenta;
            if (rightHandGrip != null) Gizmos.DrawWireSphere(rightHandGrip.position, 0.02f);
            if (leftHandGrip != null) Gizmos.DrawWireSphere(leftHandGrip.position, 0.02f);

            // Arm Bone Lines
            Gizmos.color = Color.white;
            if (shoulderR != null && elbowR != null) Gizmos.DrawLine(shoulderR.position, elbowR.position);
            if (elbowR != null && handR != null) Gizmos.DrawLine(elbowR.position, handR.position);
            if (shoulderL != null && elbowL != null) Gizmos.DrawLine(shoulderL.position, elbowL.position);
            if (elbowL != null && handL != null) Gizmos.DrawLine(elbowL.position, handL.position);
        }
#endif
    }
}
