using System.Collections.Generic;
using UnityEngine;

namespace CursedHarvest
{
    public static class EnemyRegistry
    {
        private static readonly List<EnemyController> activeEnemies = new List<EnemyController>(256);

        public static IReadOnlyList<EnemyController> ActiveEnemies => activeEnemies;
        public static int Count => activeEnemies.Count;

        public static void Register(EnemyController enemy)
        {
            if (enemy != null && !activeEnemies.Contains(enemy))
            {
                activeEnemies.Add(enemy);
            }
        }

        public static void Unregister(EnemyController enemy)
        {
            if (enemy != null)
            {
                activeEnemies.Remove(enemy);
            }
        }

        public static void Clear()
        {
            activeEnemies.Clear();
        }

        public static EnemyController GetClosestEnemy(Vector3 position, float maxRange)
        {
            EnemyController closest = null;
            float closestSqrDist = maxRange * maxRange;

            for (int i = 0; i < activeEnemies.Count; i++)
            {
                EnemyController enemy = activeEnemies[i];
                if (enemy == null || !enemy.IsAlive) continue;

                float sqrDist = (enemy.transform.position - position).sqrMagnitude;
                if (sqrDist < closestSqrDist)
                {
                    closestSqrDist = sqrDist;
                    closest = enemy;
                }
            }

            return closest;
        }

        public static void GetEnemiesInRange(Vector3 position, float radius, List<EnemyController> results)
        {
            results.Clear();
            float sqrRadius = radius * radius;

            for (int i = 0; i < activeEnemies.Count; i++)
            {
                EnemyController enemy = activeEnemies[i];
                if (enemy == null || !enemy.IsAlive) continue;

                float sqrDist = (enemy.transform.position - position).sqrMagnitude;
                if (sqrDist <= sqrRadius)
                {
                    results.Add(enemy);
                }
            }
        }
    }
}
