using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Utils.Save
{
    public class PrimitiveSaveHelper
    {
        private readonly ISaveHandler _saveHandler;

        public PrimitiveSaveHelper(ISaveHandler saveHandler)
        {
            this._saveHandler = saveHandler;
        }
        #region ASYNC METHODS

        public async Task<int> LoadIntAsync(string key)
        {
            string data = await _saveHandler.LoadDataAsync(key);
            return int.TryParse(data, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : 0;
        }

        public async Task<float> LoadFloatAsync(string key)
        {
            string data = await _saveHandler.LoadDataAsync(key);
            return float.TryParse(data, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : 0f;
        }

        public async Task<bool> LoadBoolAsync(string key)
        {
            string data = await _saveHandler.LoadDataAsync(key);
            return bool.TryParse(data, out bool value) && value;
        }

        public async Task<string> LoadDataAsync(string key)
        {
            return await _saveHandler.LoadDataAsync(key);
        }

        public async Task SaveAsync(string key, string data) => await _saveHandler.SaveDataAsync(key, data);

        #endregion ASYNC METHODS

        #region SYNC METHODS
        public int LoadInt(string key, int defaultValue)
        {
            string data = _saveHandler.LoadData(key);
            return int.TryParse(data, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : defaultValue;
        }

        public float LoadFloat(string key, float defaultValue)
        {
            string data = _saveHandler.LoadData(key);
            return float.TryParse(data, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : defaultValue;
        }

        public bool LoadBool(string key)
        {
            string data = _saveHandler.LoadData(key);
            return bool.TryParse(data, out bool value) && value;
        }

        public string LoadData(string key)
        {
            return _saveHandler.LoadData(key);
        }

        public void Save(string key, string data) => _saveHandler.SaveData(key, data);
        #endregion SYNC METHODS
    }
}
