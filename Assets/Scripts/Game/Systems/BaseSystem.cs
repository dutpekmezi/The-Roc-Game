using System;
using UnityEngine;
using Utils.Singleton;

namespace Game.Systems
{
    public abstract class BaseSystem : IDisposable
    {
        public BaseSystem()
        {

        }

        public abstract void Tick();

        public void OnDispose()
        {
            Dispose();
        }

        public virtual void Dispose()
        {
            OnDispose();
        }
    }
}