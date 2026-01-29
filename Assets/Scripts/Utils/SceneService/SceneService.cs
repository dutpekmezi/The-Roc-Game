using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using Utils.Logger;
using Utils.Signal;

namespace Utils.Scene
{
    public class SceneService : ISceneService
    {
        private Dictionary<string, GameObject> _loadedScenes = new Dictionary<string, GameObject>();
        private Dictionary<string, SceneInstance> _loadedSceneInstances = new Dictionary<string, SceneInstance>();
        private SceneServiceSettings _settings;

        public Dictionary<string, GameObject> LoadedScenes => _loadedScenes;

        public static SceneService Instance { get; private set; }

        public SceneService(SceneServiceSettings settings)
        {
            if (Instance != null)
                throw new System.Exception("Scene Service Already Has an Instance");

            Instance = this;

            this._settings = settings;
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

            foreach (var sceneInstance in _loadedSceneInstances)
            {
                _ = Addressables.UnloadSceneAsync(sceneInstance.Value);
            }

            _loadedSceneInstances.Clear();
        }

        public async Task<GameObject> LoadScene(string sceneKey)
        {
            try
            {
                var config = _settings.GetSceneConfig(sceneKey);

                SignalBus.Get<OnSceneTransitionStarted>().Invoke(config);

                if (config.RemoveAllOtherScenes)
                {
                    Clear();
                }

                // Find prefab or load scene
                var loadResult = await LoadSceneResource(sceneKey);

                if (!loadResult.HasSceneInstance && loadResult.ScenePrefab == null)
                {
                    GameLogger.LogError($"Scene '{sceneKey}' not found!");
                    return null;
                }

                if (loadResult.HasSceneInstance)
                {
                    _loadedSceneInstances[sceneKey] = loadResult.SceneInstance;
                    SignalBus.Get<OnSceneTransitionEnded>().Invoke(config);
                    return null;
                }

                // Instantiate prefab
                var currentScene = GameObject.Instantiate(loadResult.ScenePrefab);
                _loadedScenes.Add(sceneKey, currentScene);

                ISceneObject sceneObject = currentScene.GetComponent<ISceneObject>();
                if (sceneObject != null)
                {
                    await sceneObject.Initialize();
                }

                SignalBus.Get<OnSceneTransitionEnded>().Invoke(config);

                return currentScene;
            }
            catch (System.Exception e)
            {
                GameLogger.Log(e.ToString());   
                return null;
            }
        }

        public async Task RemoveScene(string scene)
        {
            try
            {
                if (_loadedSceneInstances.TryGetValue(scene, out var sceneInstance))
                {
                    await Addressables.UnloadSceneAsync(sceneInstance).Task;
                    _loadedSceneInstances.Remove(scene);
                    return;
                }

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
                GameLogger.Log(e.Message);
            }
        }

        private async Task<SceneLoadResult> LoadSceneResource(string sceneKey)
        {
            var config = _settings.GetSceneConfig(sceneKey);

            if (config == null)
            {
                return SceneLoadResult.Empty;
            }

            try
            {
                var sceneHandle = config.SceneReference.LoadSceneAsync(LoadSceneMode.Additive, true);
                var sceneInstance = await sceneHandle.Task;
                return SceneLoadResult.FromSceneInstance(sceneInstance);
            }
            catch (System.Exception sceneException)
            {
                return await LoadScenePrefab(sceneKey, config.SceneReference, sceneException);
            }
        }

        private async Task<SceneLoadResult> LoadScenePrefab(string sceneKey, AssetReference sceneReference, System.Exception sceneException = null)
        {
            try
            {
                var prefab = await sceneReference.LoadAssetAsync<GameObject>().Task;
                return SceneLoadResult.FromPrefab(prefab);
            }
            catch (System.Exception prefabException)
            {
                if (sceneException != null)
                {
                    GameLogger.LogError(
                        $"Failed to load scene resource '{sceneKey}' as prefab or scene. " +
                        $"Prefab error: {prefabException.Message}. Scene error: {sceneException.Message}");
                }
                else
                {
                    GameLogger.LogError($"Failed to load scene resource '{sceneKey}' as prefab. {prefabException.Message}");
                }

                return SceneLoadResult.Empty;
            }
        }

        private readonly struct SceneLoadResult
        {
            public GameObject ScenePrefab { get; }
            public SceneInstance SceneInstance { get; }
            public bool HasSceneInstance { get; }

            private SceneLoadResult(GameObject scenePrefab, SceneInstance sceneInstance, bool hasSceneInstance)
            {
                ScenePrefab = scenePrefab;
                SceneInstance = sceneInstance;
                HasSceneInstance = hasSceneInstance;
            }

            public static SceneLoadResult Empty => new SceneLoadResult(null, default, false);

            public static SceneLoadResult FromPrefab(GameObject prefab)
            {
                return new SceneLoadResult(prefab, default, false);
            }

            public static SceneLoadResult FromSceneInstance(SceneInstance sceneInstance)
            {
                return new SceneLoadResult(null, sceneInstance, true);
            }
        }
    }
}
