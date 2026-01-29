/*using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Utils.Math;
using Utils.Save;
using Utils.Signal;

namespace Utils.Level
{
    public class LevelService
    {
        private LevelSettings _settings;

        private LinearProgression _goldProgression = new LinearProgression(15f, 10f);
        private LinearProgression _expProgression = new LinearProgression(12f, 6f);

        private int _currentLevel;

        private Dictionary<int, LevelData> _loadedLevelDatas = new();

        private const string LevelSaveKey = "current_level";

        public int CurrentLevel => _currentLevel;

        public LevelData CurrentLevelData => _loadedLevelDatas[GetClampedLevel(CurrentLevel)];

        public static LevelService Instance { get; private set; }

        public LevelService(LevelSettings settings)
        {
            if (Instance != null)
                throw new System.Exception("Level Service already has an Instance");

            Instance = this;

            _currentLevel = SaveService.Instance.Raw.LoadInt(LevelSaveKey, 0);
            _settings = settings;
        }

        public async Task<LevelData> LoadLevelData(int levelIndex)
        {
            levelIndex = GetClampedLevel(levelIndex);

            if (_loadedLevelDatas.ContainsKey(levelIndex))
                return _loadedLevelDatas[levelIndex];

            AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>($"level_{levelIndex}");

            await handle.Task;

            if (handle.IsDone && handle.Status == AsyncOperationStatus.Succeeded)
            {
                LevelData levelData = JsonUtility.FromJson<LevelData>(handle.Result.text);

                _loadedLevelDatas.Add(levelIndex, levelData);

                Addressables.Release(handle);

                return levelData;
            }

            return null;
        }

        public void StartNextLevel()
        {
            SignalBus.Get<OnLevelStartedSignal>().Invoke(_currentLevel);
        }

        public void OnLevelCompleted(bool success)
        {
            SignalBus.Get<OnLevelCompletedSignal>().Invoke(_currentLevel, success);

            if (success)
            {
                _currentLevel++;

                _ = SaveService.Instance.Raw.SaveAsync(LevelSaveKey, _currentLevel.ToString(CultureInfo.InvariantCulture));

                _ = LoadLevelData(_currentLevel);
            }
        }

        public int CalculateLevelGold(int levelIndex)
        {
            return (int)_goldProgression.CalculateValueOfLevel(levelIndex);
        }

        public int CalculateLevelExp(int levelIndex)
        {
            return (int)_expProgression.CalculateValueOfLevel(levelIndex);
        }

        private int GetClampedLevel(int levelIndex)
        {
            if (levelIndex > _settings.LoopMaxLevel)
            {
                return ((levelIndex % _settings.LoopMaxLevel) + _settings.LoopMinLevel) - 1;
            }

            return levelIndex;
        }
    }

    public class OnLevelStartedSignal : Signal<int> { }
    public class OnLevelCompletedSignal : Signal<int, bool> { }
}*/