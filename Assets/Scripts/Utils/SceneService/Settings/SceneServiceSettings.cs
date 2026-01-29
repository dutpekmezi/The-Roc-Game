using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utils.Scene
{
    [CreateAssetMenu(fileName = "SceneServiceSettings", menuName = "Game/Scenes/Scene Service Settings", order = 0)]
    public sealed class SceneServiceSettings : ScriptableObject
    {
        public List<SceneConfig> SceneConfigs;

        public SceneConfig GetSceneConfig(string sceneType)
        {
            return SceneConfigs.FirstOrDefault(s => s.SceneKey == sceneType);
        }
    }
}
