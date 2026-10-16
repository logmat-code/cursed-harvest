using UnityEngine;

namespace CursedHarvest
{
    public class VfxAutoDestroy : MonoBehaviour
    {
        [SerializeField] private float lifetime = 0.5f;

        private Light attachedLight;
        private float initialIntensity;
        private float timer;

        public void SetLifetime(float time)
        {
            lifetime = Mathf.Max(0.1f, time);
        }

        private void Awake()
        {
            attachedLight = GetComponentInChildren<Light>();
            if (attachedLight != null)
            {
                initialIntensity = attachedLight.intensity;
            }
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            if (attachedLight != null && lifetime > 0.001f)
            {
                timer += Time.deltaTime;
                float progress = Mathf.Clamp01(timer / lifetime);
                attachedLight.intensity = Mathf.Lerp(initialIntensity, 0f, progress * progress);
            }
        }
    }
}
