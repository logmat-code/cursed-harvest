using UnityEngine;

namespace CursedHarvest
{
    public interface ICollectible
    {
        Transform transform { get; }
        GameObject gameObject { get; }
        bool IsActive { get; }
        void VacuumTo(Transform playerTransform, float flyDuration = 0.8f);
    }
}
