using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using Utils.Logger;
using Utils.Signal;
using VContainer.Unity;

namespace Utils.Scene
{
    public class SceneService : ISceneService
    {
        private Dictionary<string, GameObject> _loadedScenes = new Dictionary<string, GameObject>();
        private Dictionary<string, SceneInstance> _loadedSceneInstances = new Dictionary<string, SceneInstance>();
        private readonly SceneServiceSettings _settings;
        private readonly LifetimeScope _parent;

        public Dictionary<string, GameObject> LoadedScenes => _loadedScenes;

        public static SceneService Instance { get; private set; }

        public SceneService(SceneServiceSettings settings)
            : this(settings, null)
        {
        }

        public SceneService(SceneServiceSettings settings, LifetimeScope parent)
        {
            if (Instance != null)
                throw new System.Exception("Scene Service Already Has an Instance");

            Instance = this;

            _settings = settings;
            _parent = parent;
        }

        public void Clear()
        {
            foreach (var scene in _loadedScenes)
            {
                ISceneObject sceneObject = scene.Value.GetComponentInChildren<ISceneObject>(true);

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
                if (IsSceneLoaded(sceneKey))
                {
                    await RemoveScene(sceneKey);
                }

                var config = _settings.GetSceneConfig(sceneKey);
                var removeOtherScenes = config != null && config.RemoveAllOtherScenes;

                SignalBus.Get<OnSceneTransitionStarted>().Invoke(config);

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

                    if (removeOtherScenes)
                    {
                        await ClearExcept(sceneKey);
                    }

                    SignalBus.Get<OnSceneTransitionEnded>().Invoke(config);
                    return null;
                }

                GameObject currentScene;
                if (_parent != null)
                {
                    using (LifetimeScope.EnqueueParent(_parent))
                    {
                        currentScene = GameObject.Instantiate(loadResult.ScenePrefab);
                    }
                }
                else
                {
                    currentScene = GameObject.Instantiate(loadResult.ScenePrefab);
                }

                _loadedScenes.Add(sceneKey, currentScene);

                ISceneObject sceneObject = currentScene.GetComponentInChildren<ISceneObject>(true);
                if (sceneObject != null)
                {
                    await sceneObject.Initialize();
                }

                if (removeOtherScenes)
                {
                    await ClearExcept(sceneKey);
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

        public bool IsSceneLoaded(string sceneKey)
        {
            return _loadedScenes.ContainsKey(sceneKey) || _loadedSceneInstances.ContainsKey(sceneKey);
        }

        private async Task ClearExcept(string sceneToKeep)
        {
            var shouldKeepMenuBaseScene = sceneToKeep != SceneKeys.GameScene
                && sceneToKeep != SceneKeys.RunGameScene;

            var loadedSceneKeys = new List<string>(_loadedScenes.Keys);
            for (int i = 0; i < loadedSceneKeys.Count; i++)
            {
                if (loadedSceneKeys[i] == sceneToKeep)
                {
                    continue;
                }

                if (shouldKeepMenuBaseScene && loadedSceneKeys[i] == SceneKeys.MenuBaseScene)
                {
                    continue;
                }

                await RemoveScene(loadedSceneKeys[i]);
            }

            var loadedSceneInstanceKeys = new List<string>(_loadedSceneInstances.Keys);
            for (int i = 0; i < loadedSceneInstanceKeys.Count; i++)
            {
                if (loadedSceneInstanceKeys[i] == sceneToKeep)
                {
                    continue;
                }

                if (shouldKeepMenuBaseScene && loadedSceneInstanceKeys[i] == SceneKeys.MenuBaseScene)
                {
                    continue;
                }

                await RemoveScene(loadedSceneInstanceKeys[i]);
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
                    ISceneObject sceneObject = sceneGO.GetComponentInChildren<ISceneObject>(true);

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
                return await LoadScenePrefab(sceneKey, config.SceneReference);
            }
            catch (System.Exception prefabException)
            {
                try
                {
                    SceneInstance sceneInstance;
                    if (_parent != null)
                    {
                        using (LifetimeScope.EnqueueParent(_parent))
                        {
                            var sceneHandle = config.SceneReference.LoadSceneAsync(LoadSceneMode.Additive, true);
                            sceneInstance = await sceneHandle.Task;
                        }
                    }
                    else
                    {
                        var sceneHandle = config.SceneReference.LoadSceneAsync(LoadSceneMode.Additive, true);
                        sceneInstance = await sceneHandle.Task;
                    }

                    return SceneLoadResult.FromSceneInstance(sceneInstance);
                }
                catch (System.Exception sceneException)
                {
                    GameLogger.LogError(
                        $"Failed to load scene resource '{sceneKey}' as prefab or scene. " +
                        $"Prefab error: {prefabException.Message}. Scene error: {sceneException.Message}");

                    return SceneLoadResult.Empty;
                }
            }
        }

        private async Task<SceneLoadResult> LoadScenePrefab(string sceneKey, AssetReference sceneReference)
        {
            try
            {
                var prefab = await sceneReference.LoadAssetAsync<GameObject>().Task;
                return SceneLoadResult.FromPrefab(prefab);
            }
            catch (System.Exception prefabException)
            {
                GameLogger.LogError($"Failed to load scene resource '{sceneKey}' as prefab. {prefabException.Message}");
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
