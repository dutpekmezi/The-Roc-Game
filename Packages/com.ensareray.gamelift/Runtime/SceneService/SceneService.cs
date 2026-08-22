using GameLift.Signal;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace GameLift.Scene
{
    public class SceneService : ISceneService
    {
        private Dictionary<string, GameObject> _loadedScenes = new Dictionary<string, GameObject>();
        private SceneServiceSettings _settings;
        private readonly ISignalBus _signalBus;
        private readonly LifetimeScope _parent;

        public Dictionary<string, GameObject> LoadedScenes => _loadedScenes;

        public SceneService(SceneServiceSettings settings, ISignalBus signalBus, LifetimeScope lifetimeScope)
        {
            _settings = settings;
            _signalBus = signalBus;
            _parent = lifetimeScope;
        }

        public void Clear()
        {
            foreach (var scene in _loadedScenes)
            {
                ISceneObject sceneObject = scene.Value.GetComponent<ISceneObject>();

                if (sceneObject != null)
                {
                    _ = sceneObject.Clear();
                }

                GameObject.Destroy(scene.Value);

                var config = _settings.GetSceneConfig(scene.Key);

                if (config != null)
                {
                    config.SceneReference.ReleaseAsset();
                }
            }

            _loadedScenes.Clear();
        }

        public async Task<GameObject> LoadScene(string sceneKey)
        {
            try
            {
                var config = _settings.GetSceneConfig(sceneKey);

                _signalBus.Get<OnSceneTransitionStarted>().Invoke(config);

                if (config.RemoveAllOtherScenes)
                {
                    Clear();
                }

                // Find prefab
                var sceneGameobject = await LoadSceneResource(sceneKey);

                if (sceneGameobject == null)
                {
                    Debug.LogError($"Scene '{sceneGameobject.name}' not found!");
                    return null;
                }

                using (LifetimeScope.EnqueueParent(_parent))
                {
                    // Instantiate
                    var currentScene = GameObject.Instantiate(sceneGameobject);
                    _loadedScenes.Add(sceneKey, currentScene);

                    ISceneObject sceneObject = currentScene.GetComponent<ISceneObject>();
                    if (sceneObject != null)
                    {
                        await sceneObject.Initialize();
                    }

                    _signalBus.Get<OnSceneTransitionEnded>().Invoke(config);

                    return currentScene;
                }
            }
            catch (System.Exception e)
            {
                Debug.Log(e.ToString());
                return null;
            }
        }

        public async Task RemoveScene(string scene)
        {
            try
            {
                if (_loadedScenes.TryGetValue(scene, out var sceneGO))
                {
                    ISceneObject sceneObject = sceneGO.GetComponent<ISceneObject>();

                    if (sceneObject != null)
                    {
                        await sceneObject.Clear();
                    }

                    GameObject.Destroy(sceneGO);

                    var config = _settings.GetSceneConfig(scene);

                    if (config != null)
                    {
                        config.SceneReference.ReleaseAsset();
                    }

                    _loadedScenes.Remove(scene);
                }
            }
            catch (System.Exception e)
            {
                Debug.Log(e.Message);
            }
        }

        private async Task<GameObject> LoadSceneResource(string sceneKey)
        {
            var config = _settings.GetSceneConfig(sceneKey);

            if (config != null)
            {
                return await config.SceneReference.LoadAssetAsync<GameObject>().Task;
            }
            else
            {
                return null;
            }
        }
    }
}