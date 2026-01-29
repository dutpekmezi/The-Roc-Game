using System;

namespace Game.Installers
{
    public interface IInitializable : IDisposable
    {
        void Initialize();
    }
}
