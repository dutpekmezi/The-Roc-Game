using System.Threading.Tasks;

namespace Utils.Save
{
    public class SaveRepository<T> where T : ISaveable, new()
    {
        private readonly ISaveHandler _saveHandler;
        private readonly string _saveKey;
        private T _cachedEntity;

        public SaveRepository(ISaveHandler saveHandler, string saveKey)
        {
            this._saveHandler = saveHandler;
            this._saveKey = saveKey;
            _cachedEntity = new T();
        }

        public T Get() => _cachedEntity;

        public async Task<T> LoadAsync()
        {
            string data = await _saveHandler.LoadDataAsync(_saveKey);
            if (string.IsNullOrEmpty(data))
                return _cachedEntity;

            _cachedEntity = new T().Deserialize<T>(data);
            return _cachedEntity;
        }

        public T Load()
        {
            string data = _saveHandler.LoadData(_saveKey);
            if (string.IsNullOrEmpty(data))
                return _cachedEntity;

            _cachedEntity = new T().Deserialize<T>(data);
            return _cachedEntity;
        }

        public async Task SaveAsync(T entity)
        {
            _cachedEntity = entity;
            await _saveHandler.SaveDataAsync(_saveKey, entity.Serialize());
        }

        public void Save(T entity)
        {
            _cachedEntity = entity;
            _saveHandler.SaveData(_saveKey, entity.Serialize());
        }
    }
}