using System.Collections.Generic;

namespace Game.Systems
{
    public static class CollectableIds
    {
        public const string Coin = "coin";
        public const string Coffee = "coffee";
        public const string Matcha = "matcha";
        public const string Cookie = "cookie";

        public static List<string> GetCollectableIds()
        {
            return new List<string>
            {
                Coin,
                Coffee,
                Matcha,
                Cookie
            };
        }
    }
}
