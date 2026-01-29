using System;

namespace Utils.Signal
{
    public interface ISignal
    {
        void Subscribe(Action<ISignal> action);
        void Unsubscribe(Action<ISignal> action); 
    }
}