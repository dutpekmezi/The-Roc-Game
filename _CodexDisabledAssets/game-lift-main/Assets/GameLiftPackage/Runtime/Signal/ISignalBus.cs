using UnityEngine;

namespace GameLift.Signal
{
    public interface ISignalBus
    {
        TSignal Get<TSignal>() where TSignal : ISignal, new();
    }
}