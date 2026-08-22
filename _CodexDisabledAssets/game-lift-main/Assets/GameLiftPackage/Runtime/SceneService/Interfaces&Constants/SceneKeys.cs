using System.Collections.Generic;

namespace GameLift.Scene
{
    public static class SceneKeys
    {
        public const string MenuScene = "menu_scene";
        public const string GameScene = "game_scene";
        public const string LoadingScene = "loading_scene";
        public const string GameScene_1 = "game_scene_1";

        private static List<string> values = null;

        public static List<string> GetValues()
        {
            if (values == null)
            {
                values = new List<string>()
                {
                    MenuScene, GameScene, LoadingScene, GameScene_1
                };
            }

            return values;
        }
    }
}