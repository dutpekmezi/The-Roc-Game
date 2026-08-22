using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace GameLift.File
{
    public static class FileUtilities
    {
        public static void SaveFile(string savePath, string saveData)
        {
            try
            {
                // create the directory the file will be written to if it doesn't exist
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));

                using (FileStream stream = new FileStream(savePath, FileMode.Create))
                {
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        writer.Write(saveData);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error occured when trying to save event data: " + e);
            }
        }

        public static string ReadFile(string savePath)
        {
            string dataToLoad = "";

            if (System.IO.File.Exists(savePath))
            {
                try
                {
                    using (FileStream stream = new FileStream(savePath, FileMode.Open))
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            dataToLoad = reader.ReadToEnd();
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error occured when trying to save event data: " + e);
                }
            }

            return dataToLoad;
        }


        public static async Task SaveFileAsync(string savePath, string saveData)
        {
            // create the directory the file will be written to if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(savePath));

            await System.IO.File.WriteAllTextAsync(savePath, saveData);
        }

        public static async Task<string> ReadFileAsync(string savePath)
        {
            if (System.IO.File.Exists(savePath))
            {
                string data = await System.IO.File.ReadAllTextAsync(savePath);
                return data;
            }

            return "";
        }

        public static bool CheckFileExist(string savePath)
        {
            return System.IO.File.Exists(savePath);
        }
    }
}