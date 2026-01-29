using UnityEngine;

namespace Utils.Logger
{
    public static class GameLogger
    {
        //public static string LogHistory = "";
        public static void Log(string message)
        {
            //LogHistory += message + "\n";
            Debug.Log(message);
        }

        public static void LogWarning(string message)
        {
            //LogHistory += message + "\n";
            Debug.LogWarning(message);
        }

        public static void LogError(string message)
        {
            //LogHistory += message + "\n";
            Debug.LogError(message);
        }
    }
}