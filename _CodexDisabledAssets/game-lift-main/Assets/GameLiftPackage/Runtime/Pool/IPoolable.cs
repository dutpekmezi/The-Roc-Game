using UnityEngine;

namespace GameLift.Pooling
{
    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
        GameObject gameObject { get; }
        Transform transform{ get; }
    }
}