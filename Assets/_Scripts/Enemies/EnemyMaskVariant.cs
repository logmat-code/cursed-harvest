using UnityEngine;

namespace CursedHarvest
{
    public class EnemyMaskVariant : MonoBehaviour, IPoolable
    {
        [Header("Mask Configuration")]
        [SerializeField] private GameObject[] maskVariants;
        [SerializeField] [Range(0f, 1f)] private float maskChance = 1.0f;

        public GameObject[] MaskVariants => maskVariants;
        public float MaskChance => maskChance;
        public int VariantsCount => maskVariants != null ? maskVariants.Length : 0;
        public bool IsMaskActive
        {
            get
            {
                if (maskVariants == null) return false;
                for (int i = 0; i < maskVariants.Length; i++)
                {
                    if (maskVariants[i] != null && maskVariants[i].activeSelf) return true;
                }
                return false;
            }
        }
        public int ActiveMaskIndex
        {
            get
            {
                if (maskVariants == null) return -1;
                for (int i = 0; i < maskVariants.Length; i++)
                {
                    if (maskVariants[i] != null && maskVariants[i].activeSelf) return i;
                }
                return -1;
            }
        }

        public void SetMaskVariants(GameObject[] variants, float chance)
        {
            maskVariants = variants;
            maskChance = Mathf.Clamp01(chance);
            ApplyRandomMask();
        }

        private void Awake()
        {
            ApplyRandomMask();
        }

        public void OnSpawnFromPool()
        {
            ApplyRandomMask();
        }

        public void OnReturnToPool()
        {
            DeactivateAllMasks();
        }

        public void ApplyRandomMask(int forcedIndex = -1)
        {
            if (maskVariants == null || maskVariants.Length == 0) return;

            DeactivateAllMasks();

            if (forcedIndex >= 0)
            {
                if (forcedIndex < maskVariants.Length && maskVariants[forcedIndex] != null)
                {
                    maskVariants[forcedIndex].SetActive(true);
                }
                return;
            }

            if (UnityEngine.Random.value <= maskChance)
            {
                int chosen = UnityEngine.Random.Range(0, maskVariants.Length);
                if (maskVariants[chosen] != null)
                {
                    maskVariants[chosen].SetActive(true);
                }
            }
        }

        public void DeactivateAllMasks()
        {
            if (maskVariants == null) return;
            for (int i = 0; i < maskVariants.Length; i++)
            {
                if (maskVariants[i] != null)
                {
                    maskVariants[i].SetActive(false);
                }
            }
        }
    }
}
