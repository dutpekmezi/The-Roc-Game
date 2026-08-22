using System.Threading.Tasks;
using UnityEngine;

namespace GameLift.Save
{
    public class WebGLPlayerPrefsSaveHandler : ISaveHandler
    {
        private const string KeyPrefix = "TheRoc.GameLift.Save.";

        public Task SaveDataAsync(string key, string data)
        {
            SaveData(key, data);
            return Task.CompletedTask;
        }

        public Task<string> LoadDataAsync(string key)
        {
            return Task.FromResult(LoadData(key));
        }

        public void SaveData(string key, string data)
        {
            PlayerPrefs.SetString(GetStorageKey(key), data ?? string.Empty);
            PlayerPrefs.Save();
        }

        public string LoadData(string key)
        {
            return PlayerPrefs.GetString(GetStorageKey(key), string.Empty);
        }

        public bool CheckKeyExist(string key)
        {
            return PlayerPrefs.HasKey(GetStorageKey(key));
        }

        private static string GetStorageKey(string key)
        {
            return KeyPrefix + key;
        }
    }
}
