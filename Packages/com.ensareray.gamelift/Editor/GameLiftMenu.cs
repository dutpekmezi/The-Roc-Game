using UnityEditor;
using UnityEngine;

namespace GameLift.Editor
{
    public static class GameLiftMenu
    {
        [MenuItem("Game Lift/Open SaveService Folder")]
        public static void OpenSaveFolder()
        {
            // Open the Persistent Data Path where SaveService saves its data
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }
    }
}
