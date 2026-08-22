using System.Collections.Generic;
using System.IO;
using GameLift.Levels;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace GameLift.Editor
{
    [CustomEditor(typeof(LevelList))]
    public class LevelListEditor : UnityEditor.Editor
    {
        private SerializedProperty _folderPathProp;
        private SerializedProperty _levelsProp;

        private void OnEnable()
        {
            _folderPathProp = serializedObject.FindProperty("<LevelsFolderPath>k__BackingField");
            _levelsProp = serializedObject.FindProperty("<Levels>k__BackingField");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Auto-Construct", EditorStyles.boldLabel);

            DefaultAsset currentFolder = string.IsNullOrEmpty(_folderPathProp.stringValue)
                ? null
                : AssetDatabase.LoadAssetAtPath<DefaultAsset>(_folderPathProp.stringValue);

            EditorGUI.BeginChangeCheck();
            DefaultAsset newFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                "Levels Folder", currentFolder, typeof(DefaultAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                string path = newFolder == null ? string.Empty : AssetDatabase.GetAssetPath(newFolder);
                if (!string.IsNullOrEmpty(path) && !AssetDatabase.IsValidFolder(path))
                {
                    Debug.LogWarning($"Selected asset is not a folder: {path}");
                }
                else
                {
                    _folderPathProp.stringValue = path;
                }
            }

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_folderPathProp.stringValue)))
            {
                if (GUILayout.Button("Construct Levels"))
                    ConstructLevels(_folderPathProp.stringValue);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void ConstructLevels(string folderPath)
        {
            string[] guids = AssetDatabase.FindAssets("t:BaseLevelData", new[] { folderPath });

            Dictionary<string, (bool isLoopable, LevelDifficulty difficulty)> existing = new();
            for (int i = 0; i < _levelsProp.arraySize; i++)
            {
                SerializedProperty element = _levelsProp.GetArrayElementAtIndex(i);
                SerializedProperty levelRef = element.FindPropertyRelative("<Level>k__BackingField");
                SerializedProperty guidProp = levelRef.FindPropertyRelative("m_AssetGUID");
                if (guidProp == null || string.IsNullOrEmpty(guidProp.stringValue))
                    continue;

                bool isLoopable = element.FindPropertyRelative("<IsLoopable>k__BackingField").boolValue;
                LevelDifficulty difficulty = (LevelDifficulty)element
                    .FindPropertyRelative("<Difficulty>k__BackingField").enumValueIndex;
                existing[guidProp.stringValue] = (isLoopable, difficulty);
            }

            System.Array.Sort(guids, (a, b) =>
            {
                string nameA = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(a));
                string nameB = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(b));
                return EditorUtility.NaturalCompare(nameA, nameB);
            });

            _levelsProp.arraySize = guids.Length;

            for (int i = 0; i < guids.Length; i++)
            {
                string guid = guids[i];
                SerializedProperty element = _levelsProp.GetArrayElementAtIndex(i);
                SerializedProperty levelRef = element.FindPropertyRelative("<Level>k__BackingField");
                SerializedProperty guidProp = levelRef.FindPropertyRelative("m_AssetGUID");
                guidProp.stringValue = guid;

                SerializedProperty subRef = levelRef.FindPropertyRelative("m_SubObjectName");
                if (subRef != null) subRef.stringValue = string.Empty;

                if (existing.TryGetValue(guid, out var prev))
                {
                    element.FindPropertyRelative("<IsLoopable>k__BackingField").boolValue = prev.isLoopable;
                    element.FindPropertyRelative("<Difficulty>k__BackingField").enumValueIndex =
                        (int)prev.difficulty;
                }
                else
                {
                    element.FindPropertyRelative("<IsLoopable>k__BackingField").boolValue = false;
                    element.FindPropertyRelative("<Difficulty>k__BackingField").enumValueIndex =
                        (int)LevelDifficulty.Easy;
                }
            }

            Debug.Log($"Constructed {guids.Length} level(s) from {folderPath}.");
        }
    }
}
