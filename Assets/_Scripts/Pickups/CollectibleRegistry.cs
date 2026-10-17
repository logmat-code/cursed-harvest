using System.Collections.Generic;
using UnityEngine;

namespace CursedHarvest
{
    public static class CollectibleRegistry
    {
        private static readonly List<ICollectible> activeCollectibles = new List<ICollectible>(256);

        public static IReadOnlyList<ICollectible> ActiveCollectibles => activeCollectibles;
        public static int Count => activeCollectibles.Count;

        public static void Register(ICollectible item)
        {
            if (item != null && !activeCollectibles.Contains(item))
            {
                activeCollectibles.Add(item);
            }
        }

        public static void Unregister(ICollectible item)
        {
            if (item != null)
            {
                activeCollectibles.Remove(item);
            }
        }

        public static void Clear()
        {
            activeCollectibles.Clear();
        }

        public static void TriggerVacuumAll(Transform playerTransform, float flyDuration = 0.8f)
        {
            if (playerTransform == null) return;
            List<ICollectible> items = new List<ICollectible>(activeCollectibles);
            for (int i = 0; i < items.Count; i++)
            {
                ICollectible item = items[i];
                if (item != null && item.IsActive)
                {
                    item.VacuumTo(playerTransform, flyDuration);
                }
            }
        }
    }
}
