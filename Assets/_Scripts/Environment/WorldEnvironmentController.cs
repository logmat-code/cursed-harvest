using System;
using UnityEngine;

namespace CursedHarvest
{
    /// <summary>
    /// Switches the active arena environment, lighting, fog, and particle systems
    /// based on the selected World ID (World 1 vs World 2 vs World 3) with zero leakage.
    /// </summary>
    public class WorldEnvironmentController : MonoBehaviour
    {
        private static WorldEnvironmentController _instance;
        public static WorldEnvironmentController Instance => _instance;

        [Header("World Environments")]
        [SerializeField] private GameObject environmentW1;
        [SerializeField] private GameObject environmentW2;
        [SerializeField] private GameObject environmentW3;

        [Header("Lighting & Atmosphere")]
        [SerializeField] private Light directionalMoonlight;
        [SerializeField] private ParticleSystem ashParticleSystem;
        [SerializeField] private ParticleSystem mireParticleSystem;

        [Header("World 1 Visual Profile (The Cursed Farm)")]
        [SerializeField] private Color w1FogColor = new Color(0.06f, 0.08f, 0.12f);
        [SerializeField] private float w1FogDensity = 0.022f;
        [SerializeField] private Color w1MoonlightColor = new Color(0.60f, 0.70f, 0.85f);
        [SerializeField] private float w1MoonlightIntensity = 0.60f;

        [Header("World 2 Visual Profile (The Ashen Orchards)")]
        [SerializeField] private Color w2FogColor = new Color(0.08f, 0.07f, 0.07f); // Charcoal / ash smoke grey
        [SerializeField] private float w2FogDensity = 0.026f;
        [SerializeField] private Color w2MoonlightColor = new Color(0.48f, 0.47f, 0.46f); // Dim cold muted grey
        [SerializeField] private float w2MoonlightIntensity = 0.45f;

        [Header("World 3 Visual Profile (The Blackened Mire)")]
        [SerializeField] private Color w3FogColor = new Color(0.09f, 0.12f, 0.11f); // Cool swamp peat grey
        [SerializeField] private float w3FogDensity = 0.024f;
        [SerializeField] private Color w3MoonlightColor = new Color(0.48f, 0.58f, 0.52f); // Sickly desaturated green-grey moonlight
        [SerializeField] private float w3MoonlightIntensity = 0.52f;

        private int activeWorldId = 1;
        public int ActiveWorldId => activeWorldId;
        public GameObject EnvironmentW1 => environmentW1;
        public GameObject EnvironmentW2 => environmentW2;
        public GameObject EnvironmentW3 => environmentW3;
        public ParticleSystem AshParticles => ashParticleSystem;
        public ParticleSystem MireParticles => mireParticleSystem;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;

            FindReferencesIfMissing();
        }

        private void Start()
        {
            int selectedWorld = 1;
            if (GameSession.Instance != null)
            {
                selectedWorld = GameSession.Instance.SelectedWorldId;
            }

            ApplyEnvironment(selectedWorld);
        }

        public void FindReferencesIfMissing()
        {
            if (environmentW1 == null)
            {
                Transform w1 = transform.Find("Environment_W1");
                if (w1 != null) environmentW1 = w1.gameObject;
                else
                {
                    GameObject go = GameObject.Find("Environment_W1");
                    if (go != null) environmentW1 = go;
                }
            }

            if (environmentW2 == null)
            {
                Transform w2 = transform.Find("Environment_W2");
                if (w2 != null) environmentW2 = w2.gameObject;
                else
                {
                    GameObject go = GameObject.Find("Environment_W2");
                    if (go != null) environmentW2 = go;
                }
            }

            if (environmentW3 == null)
            {
                Transform w3 = transform.Find("Environment_W3");
                if (w3 != null) environmentW3 = w3.gameObject;
                else
                {
                    GameObject go = GameObject.Find("Environment_W3");
                    if (go != null) environmentW3 = go;
                }
            }

            if (directionalMoonlight == null)
            {
                Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (Light l in lights)
                {
                    if (l.type == LightType.Directional)
                    {
                        directionalMoonlight = l;
                        break;
                    }
                }
            }

            if (ashParticleSystem == null && environmentW2 != null)
            {
                ashParticleSystem = environmentW2.GetComponentInChildren<ParticleSystem>(true);
            }

            if (mireParticleSystem == null && environmentW3 != null)
            {
                mireParticleSystem = environmentW3.GetComponentInChildren<ParticleSystem>(true);
            }
        }

        public void ApplyEnvironment(int worldId)
        {
            activeWorldId = worldId;
            FindReferencesIfMissing();

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;

            if (worldId == 3)
            {
                // WORLD 3: THE BLACKENED MIRE
                if (environmentW1 != null) environmentW1.SetActive(false);
                if (environmentW2 != null) environmentW2.SetActive(false);
                if (environmentW3 != null) environmentW3.SetActive(true);

                RenderSettings.fogColor = w3FogColor;
                RenderSettings.fogDensity = w3FogDensity;

                if (directionalMoonlight != null)
                {
                    directionalMoonlight.color = w3MoonlightColor;
                    directionalMoonlight.intensity = w3MoonlightIntensity;
                }

                if (ashParticleSystem != null)
                {
                    ashParticleSystem.Stop();
                    ashParticleSystem.gameObject.SetActive(false);
                }

                if (mireParticleSystem != null)
                {
                    mireParticleSystem.gameObject.SetActive(true);
                    if (!mireParticleSystem.isPlaying) mireParticleSystem.Play();
                }

                Debug.Log("[WorldEnvironmentController] Applied WORLD 3 (The Blackened Mire) environment profile.");
            }
            else if (worldId == 2)
            {
                // WORLD 2: THE ASHEN ORCHARDS
                if (environmentW1 != null) environmentW1.SetActive(false);
                if (environmentW2 != null) environmentW2.SetActive(true);
                if (environmentW3 != null) environmentW3.SetActive(false);

                RenderSettings.fogColor = w2FogColor;
                RenderSettings.fogDensity = w2FogDensity;

                if (directionalMoonlight != null)
                {
                    directionalMoonlight.color = w2MoonlightColor;
                    directionalMoonlight.intensity = w2MoonlightIntensity;
                }

                if (ashParticleSystem != null)
                {
                    ashParticleSystem.gameObject.SetActive(true);
                    if (!ashParticleSystem.isPlaying) ashParticleSystem.Play();
                }

                if (mireParticleSystem != null)
                {
                    mireParticleSystem.Stop();
                    mireParticleSystem.gameObject.SetActive(false);
                }

                Debug.Log("[WorldEnvironmentController] Applied WORLD 2 (The Ashen Orchards) environment profile.");
            }
            else
            {
                // WORLD 1: THE CURSED FARM (Default)
                if (environmentW1 != null) environmentW1.SetActive(true);
                if (environmentW2 != null) environmentW2.SetActive(false);
                if (environmentW3 != null) environmentW3.SetActive(false);

                RenderSettings.fogColor = w1FogColor;
                RenderSettings.fogDensity = w1FogDensity;

                if (directionalMoonlight != null)
                {
                    directionalMoonlight.color = w1MoonlightColor;
                    directionalMoonlight.intensity = w1MoonlightIntensity;
                }

                if (ashParticleSystem != null)
                {
                    ashParticleSystem.Stop();
                    ashParticleSystem.gameObject.SetActive(false);
                }

                if (mireParticleSystem != null)
                {
                    mireParticleSystem.Stop();
                    mireParticleSystem.gameObject.SetActive(false);
                }

                Debug.Log("[WorldEnvironmentController] Applied WORLD 1 (The Cursed Farm) environment profile.");
            }
        }

        public void SetEnvironments(GameObject w1, GameObject w2, Light light, ParticleSystem ash)
        {
            environmentW1 = w1;
            environmentW2 = w2;
            directionalMoonlight = light;
            ashParticleSystem = ash;
        }

        public void SetEnvironments(GameObject w1, GameObject w2, GameObject w3, Light light, ParticleSystem ash, ParticleSystem mire = null)
        {
            environmentW1 = w1;
            environmentW2 = w2;
            environmentW3 = w3;
            directionalMoonlight = light;
            ashParticleSystem = ash;
            mireParticleSystem = mire;
        }
    }
}
