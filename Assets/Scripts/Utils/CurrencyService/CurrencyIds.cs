using System.Collections.Generic;
using System.Drawing;

namespace Utils.Currency
{
    public static class CurrencyIds
    {
        public const string Gold = "gold";
        public const string Gem = "gem";
        public const string Bell = "bell";

        private static List<string> values = null;

        public static List<string> GetCurrencyIds()
        {
            if (values == null)
            {
                values = new List<string>()
                {
                    Gold, Gem, Bell
                };
            }

            return values;
        }
    }
}