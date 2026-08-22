using System;

namespace GameLift.Signal
{
    public interface ISignal
    {
        void Subscribe(Action<ISignal> action);
        void Unsubscribe(Action<ISignal> action); 
    }
}