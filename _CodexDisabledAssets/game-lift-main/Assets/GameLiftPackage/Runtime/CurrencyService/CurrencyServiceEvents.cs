using GameLift.Signal;

namespace GameLift.Currency
{
    public class OnCurrencyChangedSignal : Signal<string, int> { }

    public class OnCurrencyChangedUISignal : Signal<string, int> { }
}
