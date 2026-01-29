using System.Collections.Generic;
using UnityEngine;
using Utils.Logger;

namespace Utils.Pools
{
    public class Pool
    {
        public GameObject Prefab;

        public int Capacity;

        // All the currently cached prefab instances
        public List<GameObject> Cache = new List<GameObject>();

        // All the delayed destruction objects
        public List<DelayedDestruction> DelayedDestructions = new List<DelayedDestruction>();

        // The total amount of created prefabs
        private int _total;

        public Transform PoolParent;

        // Returns the total amount of spawned clones
        public int Total
        {
            get
            {
                return _total;
            }
        }

        // Returns the amount of cached clones
        public int Cached
        {
            get
            {
                return Cache.Count;
            }
        }

        public Pool(GameObject prefab, Transform poolParent)
        {
            this.Prefab = prefab;
            this.PoolParent = poolParent;
        }

        public Pool(GameObject prefab, Transform poolParent, int capacity)
        {
            this.Prefab = prefab;
            this.PoolParent = poolParent;
            this.Capacity = capacity;
        }

        public Pool(GameObject prefab, Transform poolParent, int capacity, int preload)
        {
            this.Prefab = prefab;
            this.PoolParent = poolParent;
            this.Capacity = capacity;

            if (Prefab != null)
            {
                for (var i = _total; i < preload; i++)
                {
                    FastPreload();
                }
            }
        }

        // This will return a clone from the cache, or create a new instance
        public GameObject FastSpawn(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (Prefab != null)
            {
                // Attempt to spawn from the cache
                while (Cache.Count > 0)
                {
                    // Get last cache entry
                    var index = Cache.Count - 1;
                    var clone = Cache[index];

                    // Remove cache entry
                    Cache.RemoveAt(index);

                    if (clone != null)
                    {
                        // Update transform of clone
                        var cloneTransform = clone.transform;

                        cloneTransform.localPosition = position;

                        cloneTransform.localRotation = rotation;

                        cloneTransform.SetParent(parent, false);

                        // Activate clone
                        clone.SetActive(true);

                        // Messages?
                        SendNotification(clone, "OnSpawn");

                        return clone;
                    }
                    else
                    {
                        GameLogger.Log("The " + PoolParent.name + " contained a null cache entry");
                    }
                }

                // Make a new clone?
                if (Capacity <= 0 || _total < Capacity)
                {
                    var clone = FastClone(position, rotation, parent);

                    // Messages?
                    SendNotification(clone, "OnSpawn");

                    return clone;
                }
            }
            else
            {
                GameLogger.Log("Attempting to spawn null");
            }

            return null;
        }

        // Returns a clone of the prefab and increments the total
        // NOTE: Prefab is assumed to exist
        private GameObject FastClone(Vector3 position, Quaternion rotation, Transform parent)
        {
            var clone = GameObject.Instantiate(Prefab, position, rotation);

            _total += 1;

            clone.name = Prefab.name + " " + _total;

            clone.transform.SetParent(parent, false);

            return clone;
        }

        // This will despawn a clone and add it to the cache
        public void FastDespawn(GameObject clone, float delay = 0.0f)
        {
            if (clone != null)
            {
                // Delay the despawn?
                if (delay > 0.0f)
                {
                    // Make sure we only add it to the marked object list once
                    if (DelayedDestructions.Exists(m => m.Clone == clone) == false)
                    {
                        var delayedDestruction = ClassPool<DelayedDestruction>.Spawn() ?? new DelayedDestruction();

                        delayedDestruction.Clone = clone;
                        delayedDestruction.Life = delay;

                        DelayedDestructions.Add(delayedDestruction);
                    }
                }
                // Despawn now?
                else
                {
                    // Add it to the cache
                    Cache.Add(clone);

                    // Messages?
                    SendNotification(clone, "OnDespawn");

                    // Deactivate it
                    clone.SetActive(false);

                    // Move it under this GO
                    clone.transform.SetParent(PoolParent, false);
                }
            }
            else
            {
                //GameLogger.Log("Attempting to despawn a null clone");
            }
        }

        // This allows you to make another clone and add it to the cache
        public void FastPreload()
        {
            if (Prefab != null)
            {
                // Create clone
                var clone = FastClone(Vector3.zero, Quaternion.identity, null);

                // Add it to the cache
                Cache.Add(clone);

                // Deactivate it
                clone.SetActive(false);

                // Move it under this GO
                clone.transform.SetParent(PoolParent, false);
            }
        }

        // Sends messages to clones
        // NOTE: clone is assumed to exist
        private void SendNotification(GameObject clone, string messageName)
        {
            clone.SendMessage(messageName, SendMessageOptions.DontRequireReceiver);
        }
    }
}