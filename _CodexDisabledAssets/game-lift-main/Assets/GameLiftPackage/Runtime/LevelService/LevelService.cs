using GameLift.Save;
using GameLift.Signal;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GameLift.Levels
{
    public class LevelService<T> where T : BaseLevelData
    {
        private const string LevelIndexKey = "level_index";
        private LevelList _levelList;
        private readonly ISignalBus _signalBus;
        private readonly ISaveService _saveService;
        private int _currentLevel;

        private Dictionary<int, T> _loadedLevelDatas = new();


        public int CurrentLevel => _currentLevel;

        public T CurrentLevelData => _loadedLevelDatas[GetClampedLevel(CurrentLevel)];
        public LevelDataContainer CurrentLevelDataContainer => _levelList.Levels[GetClampedLevel(CurrentLevel)];


        public LevelService(LevelList levelList, ISignalBus signalBus, ISaveService saveService)
        {
            _levelList = levelList;
            _signalBus = signalBus;
            _saveService = saveService;
            _currentLevel = _saveService.Raw.LoadInt(LevelIndexKey, 0);
        }

        public async Task<T> LoadNextLevelData()
        {
            return await LoadLevelData(_currentLevel);
        }

        public async Task<T> LoadLevelData(int levelIndex)
        {
            levelIndex = GetClampedLevel(levelIndex);

            if (_loadedLevelDatas.ContainsKey(levelIndex))
                return _loadedLevelDatas[levelIndex];

            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(_levelList.Levels[levelIndex].Level);

            await handle.Task;

            if (handle.IsDone && handle.Status == AsyncOperationStatus.Succeeded)
            {
                T levelData = handle.Result;

                _loadedLevelDatas.Add(levelIndex, levelData);

                return levelData;
            }

            return null;
        }

        public void OnLevelStarted()
        {
            _signalBus.Get<OnLevelStartedSignal>().Invoke(_currentLevel);
        }

        public void OnLevelCompleted(bool success)
        {
            _signalBus.Get<OnLevelCompletedSignal>().Invoke(_currentLevel, success);

            if (success)
            {
                _currentLevel++;

                _saveService.Raw.Save(LevelIndexKey, _currentLevel.ToString(CultureInfo.InvariantCulture));

                _ = LoadLevelData(_currentLevel);
            }
        }

        private int GetClampedLevel(int levelIndex)
        {
#if UNITY_EDITOR
            if (_levelList.TestLevel != null
                && !string.IsNullOrEmpty(_levelList.TestLevel.AssetGUID))
            {
                string testGuid = _levelList.TestLevel.AssetGUID;
                int testIndex = _levelList.Levels.FindIndex(l => l != null && l.Level.AssetGUID == testGuid);
                if (testIndex >= 0)
                    return testIndex;
            }
#endif

            if (levelIndex > _levelList.Levels.Count - 1)
            {
                List<int> loopableIndices = new();
                for (int i = 0; i < _levelList.Levels.Count; i++)
                {
                    if (_levelList.Levels[i] != null && _levelList.Levels[i].IsLoopable)
                        loopableIndices.Add(i);
                }

                if (loopableIndices.Count == 0)
                    return levelIndex % _levelList.Levels.Count;

                int loopOffset = levelIndex - _levelList.Levels.Count;
                return loopableIndices[loopOffset % loopableIndices.Count];
            }

            return levelIndex;
        }

        public void ClearLoadedLevel()
        {
            _loadedLevelDatas.Clear();
        }

        public void SetLevelIndex(int index)
        {
            _currentLevel = index;
        }
    }



    public class OnLevelStartedSignal : Signal<int> { }
    public class OnLevelCompletedSignal : Signal<int, bool> { }
}