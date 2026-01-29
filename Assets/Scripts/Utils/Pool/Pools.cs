using System.Collections.Generic;
using UnityEngine;
using Utils.Logger;
using Utils.Singleton;

namespace Utils.Pools
{
    public class Pools : Singleton<Pools>
    {
        // All the currently active pools in the scene
        public List<Pool> AllPools = new List<Pool>();

        // The reference between a spawned GameObject and its pool
        public Dictionary<GameObject, Pool> AllLinks = new Dictionary<GameObject, Pool>();

        private Transform _poolsTransform = null;

        private const int DefaultPoolCapacity = 1000;

        protected override void Awake()
        {
            base.Awake();
            _poolsTransform = new GameObject("Pools").transform;
        }

        private void Update()
        {
            foreach (var pool in AllPools)
            {
                // Go through all marked objects
                for (var i = pool.DelayedDestructions.Count - 1; i >= 0; i--)
                {
                    var markedObject = pool.DelayedDestructions[i];

                    // Is it still valid?
                    if (markedObject.Clone != null)
                    {
                        // Age it
                        markedObject.Life -= Time.deltaTime;

                        // Dead?
                        if (markedObject.Life <= 0.0f)
                        {
                            RemoveDelayedDestruction(pool, i);

                            // Despawn it
                            Despawn(markedObject.Clone);
                        }
                    }
                    else
                    {
                        RemoveDelayedDestruction(pool, i);
                    }
                }
            }
        }

        private void RemoveDelayedDestruction(Pool pool, int index)
        {
            var delayedDestruction = pool.DelayedDestructions[index];

            pool.DelayedDestructions.RemoveAt(index);

            ClassPool<DelayedDestruction>.Despawn(delayedDestruction);
        }

        public Pool InitializePool(GameObject prefab, int preload)
        {
            return InitializePool(prefab, preload, DefaultPoolCapacity);
        }

        public Pool InitializePool(GameObject prefab, int preload, int capacity)
        {
            // Find the pool that handles this prefab
            var pool = AllPools.Find(p => p.Prefab == prefab);

            // Create a new pool for this prefab?
            if (pool == null)
            {
                pool = new Pool(prefab, new GameObject(prefab.name + " Pool").transform, capacity, preload);

                pool.PoolParent.transform.SetParent(this._poolsTransform);

                // Add new pool to AllPools list
                AllPools.Add(pool);
            }

            return pool;
        }

        // These methods allows you to spawn prefabs via Component with varying levels of transform data
        public T Spawn<T>(T prefab)
            where T : Component
        {
            return Spawn(prefab, Vector3.zero, Quaternion.identity, null);
        }

        public T Spawn<T>(T prefab, Transform parent)
            where T : Component
        {
            return Spawn(prefab, Vector3.zero, Quaternion.identity, parent);
        }

        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation)
            where T : Component
        {
            return Spawn(prefab, position, rotation, null);
        }

        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent)
            where T : Component
        {
            // Clone this prefabs's GameObject
            var gameObject = prefab != null ? prefab.gameObject : null;
            var clone = Spawn(gameObject, position, rotation, parent);

            // Return the same component from the clone
            return clone != null ? clone.GetComponent<T>() : null;
        }

        // These methods allows you to spawn prefabs via GameObject with varying levels of transform data
        public GameObject Spawn(GameObject prefab)
        {
            return Spawn(prefab, Vector3.zero, Quaternion.identity, null);
        }

        public GameObject Spawn(GameObject prefab, Transform parent)
        {
            return Spawn(prefab, Vector3.zero, Quaternion.identity, parent);
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return Spawn(prefab, position, rotation, null);
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (prefab != null)
            {
                var pool = InitializePool(prefab, 0);

                // Spawn a clone from this pool
                var clone = pool.FastSpawn(position, rotation, parent);

                // Was a clone created?
                // NOTE: This will be null if the pool's capacity has been reached
                if (clone != null)
                {
                    // Associate this clone with this pool
                    AllLinks.Add(clone, pool);

                    // Return the clone
                    return clone.gameObject;
                }
            }
            else
            {
                GameLogger.LogWarning("Attempting to spawn a null prefab");
            }

            return null;
        }

        // This allows you to despawn a clone via Component, with optional delay
        public void Despawn(Component clone, float delay = 0.0f)
        {
            if (clone != null)
                Despawn(clone.gameObject, delay);
        }

        // This allows you to despawn a clone via GameObject, with optional delay
        public void Despawn(GameObject clone, float delay = 0.0f)
        {
            if (clone != null)
            {
                Pool pool;

                // Try and find the pool associated with this clone
                if (AllLinks.TryGetValue(clone, out pool) == true)
                {
                    if (delay == 0)
                    {
                        // Remove the association
                        AllLinks.Remove(clone);
                    }

                    // Despawn it
                    pool.FastDespawn(clone, delay);
                }
                else
                {
                    GameLogger.LogWarning("Attempting to despawn " + clone.name + ", but failed to find pool for it! Make sure you created it using PoolService.Spawn!");

                    // Fall back to normal destroying
                    GameObject.Destroy(clone);
                }
            }
            else
            {
                //GameLogger.LogError("Attempting to despawn a null clone");
            }
        }
    }

    public class DelayedDestruction
    {
        public GameObject Clone;

        public float Life;
    }
}
