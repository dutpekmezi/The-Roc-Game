using System.Collections.Generic;

namespace Utils.Scene
{
    public static class SceneKeys
    {
        public const string InitialScene = "initial_scene";
        public const string SplashScene = "splash_scene";
        public const string MenuScene = "menu_scene";
        public const string MenuBaseScene = "menu_base_scene";
        public const string StoreScene = "store_scene";
        public const string GameScene = "game_scene";
        public const string RunGameScene = "run_game_scene";

        private static List<string> values = null;

        public static List<string> GetValues()
        {
            if (values == null)
            {
                values = new List<string>()
                {
                    InitialScene, SplashScene, MenuScene, MenuBaseScene, GameScene, RunGameScene, StoreScene
                };
            }

            return values;
        }
    }
}
