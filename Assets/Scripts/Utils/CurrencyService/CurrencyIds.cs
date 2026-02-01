using System.Collections.Generic;
using System.Drawing;

namespace Utils.Currency
{
    public static class CurrencyIds
    {
        public const string Coffee = "coffee";
        public const string Matcha = "matcha";
        public const string Coin = "coin";

        private static List<string> values = null;

        public static List<string> GetCurrencyIds()
        {
            if (values == null)
            {
                values = new List<string>()
                {
                    Coffee, Matcha, Coin
                };
            }

            return values;
        }
    }
}