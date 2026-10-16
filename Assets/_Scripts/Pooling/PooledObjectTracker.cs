using UnityEngine;

namespace CursedHarvest
{
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
