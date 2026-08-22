using UnityEngine;

namespace GameLift.Pooling
{
    public interface IPools
    {
        // Pool initialization
        Pool InitializePool(GameObject prefab, int preload);
        Pool InitializePool(GameObject prefab, int preload, int capacity);

        // Spawn methods for Components
        T Spawn<T>(T prefab) where T : Component;
        T Spawn<T>(T prefab, Transform parent) where T : Component;
        T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component;
        T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent) where T : Component;

        // Spawn methods for GameObjects
        GameObject Spawn(GameObject prefab);
        GameObject Spawn(GameObject prefab, Transform parent);
        GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation);
        GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent);

        // Despawn methods
        void Despawn(Component clone, float delay = 0.0f);
        void Despawn(GameObject clone, float delay = 0.0f);
    }
}