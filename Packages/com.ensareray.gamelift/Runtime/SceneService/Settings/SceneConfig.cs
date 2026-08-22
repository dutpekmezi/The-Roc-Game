using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace GameLift.Scene
{
    [CreateAssetMenu(fileName = "SceneConfig", menuName = "Game Lift/Scenes/Scene Config", order = 1)]
    public class SceneConfig : ScriptableObject
    {
        [field: SerializeField, Dropdown("GetSceneKeys")] public string SceneKey { get; set; }
        [field: SerializeField] public AssetReference SceneReference { get; set; }
        [field: SerializeField] public bool RemoveAllOtherScenes { get; set; } = false;
        [field: SerializeField] public bool ShowSceneTransition { get; set; }

        private List<string> GetSceneKeys()
        {
            return SceneKeys.GetValues();
        }
    }
}
