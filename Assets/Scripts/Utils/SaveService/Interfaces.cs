using System.Threading.Tasks;

namespace Utils.Save
{
    // Lowest-level abstraction: only reads/writes strings.
    public interface ISaveHandler
    {
        Task SaveDataAsync(string key, string data);
        Task<string> LoadDataAsync(string key);

        void SaveData(string key, string data);
        string LoadData(string key);

        bool CheckKeyExist(string key);
    }

    // Anything saveable must implement this.
    public interface ISaveable
    {
        string Serialize();
        T Deserialize<T>(string data) where T : ISaveable, new();
    }
}