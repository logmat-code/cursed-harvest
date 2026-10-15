using UnityEngine;

namespace CursedHarvest
{
    public struct DamageData
    {
        public float amount;
        public Vector3 hitPoint;
        public Vector3 hitDirection;
        public float knockbackForce;
        public GameObject source;
        public bool isCritical;

        public DamageData(float amount, GameObject source = null, Vector3 hitDirection = default, float knockbackForce = 0f, Vector3 hitPoint = default, bool isCritical = false)
        {
            this.amount = amount;
            this.source = source;
            this.hitDirection = hitDirection;
            this.knockbackForce = knockbackForce;
            this.hitPoint = hitPoint;
            this.isCritical = isCritical;
        }
    }

    public interface IDamageable
    {
        void TakeDamage(DamageData damage);
        bool IsAlive { get; }
        Transform transform { get; }
    }
}
