using System.Collections.Generic;

namespace Game.Systems
{
    public static class CollectableIds
    {
        public const string Coffee = "coffee";
        public const string Matcha = "matcha";
        public const string Coin = "coin";
        public const string Cookie = "cookie";

        private static List<string> values = null;

        public static List<string> GetCollectableIds()
        {
            if (values == null)
            {
                values = new List<string>()
                {
                    Coffee, Matcha, Coin, Cookie
                };
            }

            return values;
        }
    }
}
