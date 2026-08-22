using System.Threading;
using Cysharp.Threading.Tasks;
using GameLift.Levels;
using UnityEngine;
using VContainer.Unity;

namespace GameLift.Scene
{
    public class SceneFlowController
    {
        private readonly LevelService<BaseLevelData> _levelService;
        private readonly ISceneService _sceneService;
        private readonly SceneServiceSettings _settings;
        private readonly LevelList _levelList;

        public SceneFlowController(SceneServiceSettings settings, LevelList levelList, LevelService<BaseLevelData> levelService, ISceneService sceneService)
        {
            _levelService = levelService;
            _sceneService = sceneService;
            _settings = settings;
            _levelList = levelList;
        }

        public async UniTask LoadFirstScene()
        {
            if (_levelList.LoadImmediateUntil > 0 && _levelService.CurrentLevel < _levelList.LoadImmediateUntil)
            {
                await _levelService.LoadNextLevelData();
                await _sceneService.LoadScene(SceneKeys.GameScene);
            }
            else if (_settings.DefaultSceneConfig != null)
            {
                await _sceneService.LoadScene(_settings.DefaultSceneConfig.SceneKey);
            }
        }

    }
}
