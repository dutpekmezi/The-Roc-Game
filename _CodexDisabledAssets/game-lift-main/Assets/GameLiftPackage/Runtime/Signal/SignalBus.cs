using System;

namespace GameLift.Signal
{
    public class SignalBus : ISignalBus, IDisposable
    {
        private Bus _bus = new();

        public TSignal Get<TSignal>() where TSignal : ISignal, new()
        {
            return _bus.Get<TSignal>();
        }

        public void Dispose()
        {
            _bus = new Bus();
        }
    }
}