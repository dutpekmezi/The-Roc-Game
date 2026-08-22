using System.Threading.Tasks;
using UnityEngine;

namespace Utils.Save
{
    public class WebGLPlayerPrefsSaveHandler : ISaveHandler
    {
        private const string KeyPrefix = "TheRoc.Save.";

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

        public void DeleteData(string key)
        {
            PlayerPrefs.DeleteKey(GetStorageKey(key));
            PlayerPrefs.Save();
        }

        private static string GetStorageKey(string key)
        {
            return KeyPrefix + key;
        }
    }
}
