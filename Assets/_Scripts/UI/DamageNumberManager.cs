using System.Collections.Generic;
using UnityEngine;

namespace CursedHarvest
{
    public class DamageNumberManager : MonoBehaviour
    {
        private static DamageNumberManager _instance;
        public static DamageNumberManager Instance
        {
            get
            {
                if (_instance == null || !_instance)
                {
                    _instance = FindAnyObjectByType<DamageNumberManager>();
                }
                return (_instance != null && _instance) ? _instance : null;
            }
            private set => _instance = value;
        }

        private class ActiveNumber
        {
            public GameObject gameObject;
            public TextMesh textMesh;
            public Vector3 startPos;
            public Vector3 velocity;
            public float timer;
            public float duration;
            public Color baseColor;
        }

        [SerializeField] private int poolSize = 35;
        private readonly Queue<GameObject> pool = new Queue<GameObject>();
        private readonly List<ActiveNumber> activeNumbers = new List<ActiveNumber>();
        private Transform cameraTransform;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            pool.Clear();
            activeNumbers.Clear();

            for (int i = 0; i < poolSize; i++)
            {
                GameObject obj = CreateNumberObject(i);
                obj.SetActive(false);
                pool.Enqueue(obj);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            pool.Clear();
            activeNumbers.Clear();
        }

        private void Start()
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private GameObject CreateNumberObject(int index)
        {
            GameObject obj = new GameObject($"DamageNum_{index}");
            obj.transform.SetParent(transform);

            TextMesh tm = obj.AddComponent<TextMesh>();
            tm.fontSize = 32;
            tm.characterSize = 0.08f;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.fontStyle = FontStyle.Bold;

            MeshRenderer mr = obj.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            return obj;
        }

        public void SpawnDamage(Vector3 worldPos, float amount, bool isCrit = false)
        {
            if (amount <= 0f) return;
            // Suppress small continuous tick spam if crowded
            if (amount < 12f && !isCrit && activeNumbers.Count > 6) return;
            // Cap maximum visible damage numbers to prevent screen clutter
            if (activeNumbers.Count >= 12) return;

            GameObject obj = null;
            while (pool.Count > 0)
            {
                var candidate = pool.Dequeue();
                if (candidate != null && candidate)
                {
                    obj = candidate;
                    break;
                }
            }
            if (obj == null)
            {
                obj = CreateNumberObject(activeNumbers.Count);
            }
            obj.SetActive(true);

            Vector3 jitter = new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0f, 0.2f), Random.Range(-0.25f, 0.25f));
            Vector3 pos = worldPos + jitter;
            obj.transform.position = pos;

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
            if (cameraTransform != null)
            {
                obj.transform.rotation = cameraTransform.rotation;
            }

            TextMesh tm = obj.GetComponent<TextMesh>();
            int rounded = Mathf.RoundToInt(amount);
            tm.text = rounded.ToString();

            Color color = isCrit ? new Color(1f, 0.35f, 0.1f, 1f) :
                          (amount >= 40f ? new Color(1f, 0.9f, 0.25f, 1f) : new Color(1f, 1f, 1f, 0.95f));
            tm.color = color;

            ActiveNumber entry = new ActiveNumber
            {
                gameObject = obj,
                textMesh = tm,
                startPos = pos,
                velocity = new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(1.4f, 1.9f), Random.Range(-0.25f, 0.25f)),
                timer = 0f,
                duration = 0.40f,
                baseColor = color
            };

            activeNumbers.Add(entry);
        }

        public void SpawnHealing(Vector3 worldPos, float amount)
        {
            if (amount <= 0f) return;
            int rounded = Mathf.RoundToInt(amount);
            string text = $"+{rounded} HP";
            Color healColor = new Color(0.35f, 0.95f, 0.45f, 1f); // Soft luminous healing green
            SpawnText(worldPos, text, healColor, 0.55f);
        }

        public void SpawnText(Vector3 worldPos, string text, Color color, float duration = 0.50f)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (activeNumbers.Count >= 16) return;

            GameObject obj = null;
            while (pool.Count > 0)
            {
                var candidate = pool.Dequeue();
                if (candidate != null && candidate)
                {
                    obj = candidate;
                    break;
                }
            }
            if (obj == null)
            {
                obj = CreateNumberObject(activeNumbers.Count);
            }
            obj.SetActive(true);

            Vector3 jitter = new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(0f, 0.15f), Random.Range(-0.15f, 0.15f));
            Vector3 pos = worldPos + jitter;
            obj.transform.position = pos;

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
            if (cameraTransform != null)
            {
                obj.transform.rotation = cameraTransform.rotation;
            }

            TextMesh tm = obj.GetComponent<TextMesh>();
            tm.text = text;
            tm.color = color;

            ActiveNumber entry = new ActiveNumber
            {
                gameObject = obj,
                textMesh = tm,
                startPos = pos,
                velocity = new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(1.3f, 1.7f), Random.Range(-0.15f, 0.15f)),
                timer = 0f,
                duration = duration,
                baseColor = color
            };

            activeNumbers.Add(entry);
        }

        private void Update()
        {
            if (activeNumbers.Count == 0) return;

            float dt = Time.deltaTime;
            for (int i = activeNumbers.Count - 1; i >= 0; i--)
            {
                ActiveNumber num = activeNumbers[i];
                num.timer += dt;
                float progress = num.timer / num.duration;

                if (progress >= 1f)
                {
                    num.gameObject.SetActive(false);
                    pool.Enqueue(num.gameObject);
                    activeNumbers.RemoveAt(i);
                }
                else
                {
                    num.gameObject.transform.position += num.velocity * dt;
                    num.velocity.y -= 2.2f * dt; // slight gravity deceleration

                    if (cameraTransform != null)
                    {
                        num.gameObject.transform.rotation = cameraTransform.rotation;
                    }

                    // Scale punch then fade
                    float scale = progress < 0.2f ? Mathf.Lerp(0.6f, 1.25f, progress / 0.2f) : Mathf.Lerp(1.25f, 0.9f, (progress - 0.2f) / 0.8f);
                    num.gameObject.transform.localScale = Vector3.one * scale;

                    // Fade out in last 50%
                    float alpha = progress > 0.5f ? (1f - (progress - 0.5f) / 0.5f) : 1f;
                    Color c = num.baseColor;
                    c.a = alpha;
                    num.textMesh.color = c;
                }
            }
        }

        public void ClearActiveNumbers()
        {
            foreach (var num in activeNumbers)
            {
                if (num.gameObject != null)
                {
                    num.gameObject.SetActive(false);
                    pool.Enqueue(num.gameObject);
                }
            }
            activeNumbers.Clear();
        }
    }
}
