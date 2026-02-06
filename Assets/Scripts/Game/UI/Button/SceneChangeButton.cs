using Game.Installers;
using UnityEngine;
using Utils.Scene;
using Utils.Buttons;
using NaughtyAttributes;
using System.Collections.Generic;

namespace Game.UI
{
    public class SceneChangeButton : BaseButton
    {

        [SerializeField, Dropdown(("GetSceneKeys"))]
        private string sceneId;

        public string SceneId => sceneId;

        public override void BaseOnClick()
        {
            base.BaseOnClick();

            _ = SceneService.Instance.LoadScene(sceneId);
        }

        private List<string> GetSceneKeys()
        {
            return SceneKeys.GetValues();
        }
    }
}
