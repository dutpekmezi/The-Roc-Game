using Utils.Signal;

namespace Utils.Currency
{
    public class OnCurrencyChangedSignal : Signal<string, int> { }

    public class OnCurrencyChangedUISignal : Signal<string, int> { }
}
