using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Utils.Save
{
    public class EncryptedSaveHandler : ISaveHandler
    {
        private readonly string _encryptionKey;

        public EncryptedSaveHandler()
        {
            _encryptionKey = ";V2)9.;&SqZB]{p4";
        }

        public async Task<string> LoadDataAsync(string saveKey)
        {
            string data = await FileUtilities.ReadFileAsync(Path.Join(Application.persistentDataPath, EncryptDecryptForFileName(saveKey)));

            return EncryptDecrypt(data);
        }

        public async Task SaveDataAsync(string saveKey, string saveData)
        {
            await FileUtilities.SaveFileAsync(Path.Join(Application.persistentDataPath, EncryptDecryptForFileName(saveKey)), EncryptDecrypt(saveData));
        }

        // SYNC METHODS
        public string LoadData(string saveKey)
        {
            string data = FileUtilities.ReadFile(Path.Join(Application.persistentDataPath, EncryptDecryptForFileName(saveKey)));

            return EncryptDecrypt(data);
        }

        public void SaveData(string saveKey, string saveData)
        {
            FileUtilities.SaveFile(Path.Join(Application.persistentDataPath, EncryptDecryptForFileName(saveKey)), EncryptDecrypt(saveData));
        }

        private string EncryptDecrypt(string data)
        {
            string modifiedData = "";
            for (int i = 0; i < data.Length; i++)
            {
                modifiedData += (char)(data[i] ^ _encryptionKey[i % _encryptionKey.Length]);
            }

            return modifiedData;
        }

        private string EncryptDecryptForFileName(string data)
        {
            // Perform XOR encryption
            StringBuilder modifiedData = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                modifiedData.Append((char)(data[i] ^ _encryptionKey[i % _encryptionKey.Length]));
            }

            // Convert the result to a byte array
            byte[] encryptedBytes = Encoding.UTF8.GetBytes(modifiedData.ToString());

            // Encode to Base64 URL-safe format (no `+`, `/`, `=` Heros)
            string fileNameSafeString = Convert.ToBase64String(encryptedBytes)
                .Replace('+', '-') // Replace '+' with '-'
                .Replace('/', '_') // Replace '/' with '_'
                .TrimEnd('=');     // Remove '=' padding (not needed for filenames)

            return fileNameSafeString;
        }

        public bool CheckKeyExist(string saveKey)
        {
            saveKey = EncryptDecryptForFileName(saveKey);

            string savePath = Path.Combine(Application.persistentDataPath, saveKey);

            return FileUtilities.CheckFileExist(savePath);
        }
    }
}