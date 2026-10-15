using System.Collections.Generic;
using UnityEngine;

namespace CursedHarvest
{
    public interface IPoolable
    {
        void OnSpawnFromPool();
        void OnReturnToPool();
    }

    public class ObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int initialCapacity = 20;

        private readonly Queue<GameObject> pool = new Queue<GameObject>();
        private readonly HashSet<GameObject> activeObjects = new HashSet<GameObject>();

        public GameObject Prefab => prefab;
        public int ActiveCount => activeObjects.Count;

        public void Initialize(GameObject prefabToPool, int capacity)
        {
            prefab = prefabToPool;
            initialCapacity = capacity;
            Prewarm(initialCapacity);
        }

        private void Awake()
        {
            if (prefab != null && pool.Count == 0)
            {
                Prewarm(initialCapacity);
            }
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject obj = CreateNewObject();
                obj.SetActive(false);
                pool.Enqueue(obj);
            }
        }

        private GameObject CreateNewObject()
        {
            GameObject obj = Instantiate(prefab, transform);
            PooledObjectTracker tracker = obj.GetComponent<PooledObjectTracker>();
            if (tracker == null)
            {
                tracker = obj.AddComponent<PooledObjectTracker>();
            }
            tracker.OriginPool = this;
            return obj;
        }

        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject obj = null;
            while (pool.Count > 0 && obj == null)
            {
                obj = pool.Dequeue();
            }

            if (obj == null)
            {
                obj = CreateNewObject();
            }

            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
            activeObjects.Add(obj);

            IPoolable[] poolables = obj.GetComponentsInChildren<IPoolable>(true);
            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i].OnSpawnFromPool();
            }

            return obj;
        }

        public void Return(GameObject obj)
        {
            if (obj == null) return;

            if (activeObjects.Remove(obj))
            {
                IPoolable[] poolables = obj.GetComponentsInChildren<IPoolable>(true);
                for (int i = 0; i < poolables.Length; i++)
                {
                    poolables[i].OnReturnToPool();
                }

                obj.SetActive(false);
                obj.transform.SetParent(transform);
                pool.Enqueue(obj);
            }
        }

        public void ReturnAllActive()
        {
            List<GameObject> activeList = new List<GameObject>(activeObjects);
            for (int i = 0; i < activeList.Count; i++)
            {
                Return(activeList[i]);
            }
            activeObjects.Clear();
        }
    }

    public class PooledObjectTracker : MonoBehaviour
    {
        public ObjectPool OriginPool { get; set; }

        public void ReturnToPool()
        {
            if (OriginPool != null)
            {
                OriginPool.Return(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
