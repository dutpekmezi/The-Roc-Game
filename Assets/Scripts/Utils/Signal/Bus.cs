using System;
using System.Collections.Generic;

namespace Utils.Signal
{
    public class Bus
    {
        private readonly Dictionary<Type, ISignal> _signals = new();

        public T Get<T>() where T : ISignal, new()
        {
            var signalType = typeof(T);
            return (T)Get(signalType);
        }

        public ISignal Get(Type signalType)
        {
            return _signals.TryGetValue(signalType, out var signal) ? signal : Bind(signalType);
        }

        private ISignal Bind(Type signalType)
        {
            if (_signals.TryGetValue(signalType, out var signal))
            {
                return signal;
            }

            signal = (ISignal)Activator.CreateInstance(signalType);
            _signals.Add(signalType, signal);
            return signal;
        }
    }
}