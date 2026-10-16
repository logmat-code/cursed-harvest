using UnityEngine;

namespace CursedHarvest
{
    public class PlayerCombatPose : MonoBehaviour
    {
        [Header("Weapon Hierarchy")]
        [SerializeField] private Transform crossbowRoot;
        [SerializeField] private Transform weaponSocket; // Legacy alias
        [SerializeField] private Transform visualBow;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Transform rightHandGrip;
        [SerializeField] private Transform leftHandGrip;
        [SerializeField] private Vector3 rightGripOffset = new Vector3(0.01f, 0.01f, 0.02f);
        [SerializeField] private Vector3 bowOffsetInsideSocket = new Vector3(0f, -0.01f, 0.04f);

        [Header("Bone References")]
        [SerializeField] private Transform shoulderR;
        [SerializeField] private Transform elbowR;
        [SerializeField] private Transform handR;
        [SerializeField] private Transform shoulderL;
        [SerializeField] private Transform elbowL;
        [SerializeField] private Transform handL;
        [SerializeField] private Transform spine;

        [Header("Compact Upper Body Pose (Euler Offsets)")]
        [SerializeField] private Vector3 shoulderROffset = new Vector3(68f, 28f, -54f);
        [SerializeField] private Vector3 elbowROffset = new Vector3(16f, 58f, 10f);
        [SerializeField] private Vector3 handROffset = new Vector3(5f, -5f, 12f);

        [SerializeField] private Vector3 shoulderLOffset = new Vector3(-28f, 64f, 34f);
        [SerializeField] private Vector3 elbowLOffset = new Vector3(22f, -42f, -16f);
        [SerializeField] private Vector3 handLOffset = new Vector3(-5f, 0f, -8f);

        private float currentRecoil;
        private Quaternion restShoulderR;
        private Quaternion restElbowR;
        private Quaternion restHandR;
        private Quaternion restShoulderL;
        private Quaternion restElbowL;
        private Quaternion restHandL;
        private bool isInitialized;

        public Transform WeaponSocket => crossbowRoot != null ? crossbowRoot : weaponSocket;
        public Transform CrossbowRoot => crossbowRoot;
        public Transform FirePoint => firePoint;
        public Transform VisualBow => visualBow;
        public Transform RightHandGrip => rightHandGrip;
        public Transform LeftHandGrip => leftHandGrip;

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

            if (shoulderR != null) restShoulderR = shoulderR.localRotation;
            if (elbowR != null) restElbowR = elbowR.localRotation;
            if (handR != null) restHandR = handR.localRotation;

            if (shoulderL != null) restShoulderL = shoulderL.localRotation;
            if (elbowL != null) restElbowL = elbowL.localRotation;
            if (handL != null) restHandL = handL.localRotation;

            isInitialized = true;
        }

        public void SetupWeapon(Transform bowModel, Transform fp)
        {
            visualBow = bowModel;
            firePoint = fp;

            FindBones();

            Transform parentToUse = handR != null ? handR : transform;

            // 1. Establish CrossbowRoot wrapper hierarchy
            if (crossbowRoot == null)
            {
                Transform existing = parentToUse.Find("CrossbowRoot");
                if (existing == null) existing = transform.Find("Visual/CrossbowRoot");
                if (existing == null) existing = transform.Find("Visual/WeaponSocket");

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
            weaponSocket = crossbowRoot;

            // 2. RightHandGrip (trigger/grip handle)
            if (rightHandGrip == null)
            {
                Transform existing = crossbowRoot.Find("RightHandGrip");
                if (existing != null) rightHandGrip = existing;
                else
                {
                    GameObject rhg = new GameObject("RightHandGrip");
                    rhg.transform.SetParent(crossbowRoot, false);
                    rhg.transform.localPosition = Vector3.zero;
                    rightHandGrip = rhg.transform;
                }
            }

            // 3. LeftHandGrip (fore-stock support along bottom of barrel)
            if (leftHandGrip == null)
            {
                Transform existing = crossbowRoot.Find("LeftHandGrip");
                if (existing != null) leftHandGrip = existing;
                else
                {
                    GameObject lhg = new GameObject("LeftHandGrip");
                    lhg.transform.SetParent(crossbowRoot, false);
                    lhg.transform.localPosition = new Vector3(0.01f, -0.025f, 0.20f);
                    leftHandGrip = lhg.transform;
                }
            }

            // 4. Attach VisualBow under CrossbowRoot
            if (crossbowRoot != null && visualBow != null)
            {
                visualBow.SetParent(crossbowRoot, false);
                visualBow.localPosition = bowOffsetInsideSocket;
                visualBow.localRotation = Quaternion.identity; // Forward along +Z
                visualBow.localScale = Vector3.one * 0.64f;   // Calibrated compact 0.64 scale
            }

            // 5. Attach FirePoint under CrossbowRoot
            if (crossbowRoot != null && firePoint != null)
            {
                firePoint.SetParent(crossbowRoot, false);
                firePoint.localPosition = new Vector3(0f, 0.025f, 0.38f); // Front tip of compact crossbow
                firePoint.localRotation = Quaternion.identity;           // Exact forward direction
            }
        }

        public void PlayRecoil(float amount = 0.015f)
        {
            currentRecoil = Mathf.Clamp(currentRecoil + amount, 0f, 0.020f);
        }

        public void SetArmOffsets(Vector3 sR, Vector3 eR, Vector3 hR, Vector3 sL, Vector3 eL, Vector3 hL)
        {
            shoulderROffset = sR;
            elbowROffset = eR;
            handROffset = hR;
            shoulderLOffset = sL;
            elbowLOffset = eL;
            handLOffset = hL;
        }

        private void LateUpdate()
        {
            ApplyCombatPose();
        }

        private void ApplyCombatPose()
        {
            // Smoothly recover recoil
            if (currentRecoil > 0.0001f)
            {
                currentRecoil = Mathf.Lerp(currentRecoil, 0f, Time.deltaTime * 16f);
            }
            else
            {
                currentRecoil = 0f;
            }

            // 1. Pose Right Arm (Brings hand near right chest)
            if (shoulderR != null) shoulderR.localRotation = restShoulderR * Quaternion.Euler(shoulderROffset);
            if (elbowR != null) elbowR.localRotation = restElbowR * Quaternion.Euler(elbowROffset);
            if (handR != null) handR.localRotation = restHandR * Quaternion.Euler(handROffset);

            // 2. Pose Left Arm (Compact inward bend, elbow flexed)
            if (shoulderL != null) shoulderL.localRotation = restShoulderL * Quaternion.Euler(shoulderLOffset);
            if (elbowL != null) elbowL.localRotation = restElbowL * Quaternion.Euler(elbowLOffset);
            if (handL != null) handL.localRotation = restHandL * Quaternion.Euler(handLOffset);

            // 3. Align Crossbow with player aim forward + recoil kickback
            if (crossbowRoot != null && handR != null)
            {
                float kickPitch = currentRecoil * 35f; // ~0.5 degree subtle pitch kick
                crossbowRoot.position = handR.position - transform.forward * currentRecoil + transform.TransformDirection(rightGripOffset);
                crossbowRoot.rotation = Quaternion.LookRotation(transform.forward, Vector3.up) * Quaternion.Euler(-kickPitch, 0f, 0f);

                // Right hand grip wraps naturally around the handle
                handR.rotation = crossbowRoot.rotation * Quaternion.Euler(12f, 0f, 15f);
            }

            // 4. Support Left Hand under fore-stock (LeftHandGrip)
            if (handL != null && leftHandGrip != null && crossbowRoot != null)
            {
                handL.position = leftHandGrip.position;
                handL.rotation = crossbowRoot.rotation * Quaternion.Euler(15f, 25f, -35f); // Cupped upward supporting stock
            }
        }
    }
}
