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
        [SerializeField] private RectTransform _rectTransform;

        [SerializeField, Dropdown(("GetSceneKeys"))]
        private string sceneId;

        public string SceneId => sceneId;

        public override async void BaseOnClick()
        {
            base.BaseOnClick();

            if (SceneService.Instance == null)
            {
                return;
            }

            if (sceneId == SceneKeys.StoreScene)
            {
                await SceneService.Instance.RemoveScene(SceneKeys.MenuScene);
            }

            await SceneService.Instance.LoadScene(sceneId);
        }

        private List<string> GetSceneKeys()
        {
            return SceneKeys.GetValues();
        }

        public RectTransform Transform => _rectTransform;
    }
}
