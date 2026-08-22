using System.Collections.Generic;
using System.Drawing;

namespace GameLift.Currency
{
    public static class CurrencyIds
    {
        public const string Gold = "gold";
        public const string Gem = "gem";
        public const string Energy = "energy";

        private static List<string> values = null;

        public static List<string> GetCurrencyIds()
        {
            if (values == null)
            {
                values = new List<string>()
                {
                    Gold, Gem, Energy
                };
            }

            return values;
        }
    }
}