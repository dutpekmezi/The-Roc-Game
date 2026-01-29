namespace Utils.Signal
{
    public class SignalBus
    {
        private static Bus _bus = new();

        public static TSignal Get<TSignal>() where TSignal : ISignal, new()
        {
            return _bus.Get<TSignal>();
        }

        public static void Clear()
        {
            _bus = new Bus();
        }
    }
}