using GameLift.Levels;
using UnityEditor;
using UnityEngine;

namespace GameLift.Editor
{
    [CustomPropertyDrawer(typeof(LevelDataContainer))]
    public class LevelDataContainerDrawer : PropertyDrawer
    {
        private const float Spacing = 4f;
        private const float LoopableWidth = 60f;
        private const float DifficultyWidth = 80f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty loopableProp = property.FindPropertyRelative("<IsLoopable>k__BackingField");
            SerializedProperty difficultyProp = property.FindPropertyRelative("<Difficulty>k__BackingField");
            SerializedProperty levelProp = property.FindPropertyRelative("<Level>k__BackingField");

            int previousIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float levelWidth = position.width - LoopableWidth - DifficultyWidth - Spacing * 2f;

            Rect loopableRect = new(position.x, position.y, LoopableWidth, position.height);
            Rect difficultyRect = new(loopableRect.xMax + Spacing, position.y, DifficultyWidth, position.height);
            Rect levelRect = new(difficultyRect.xMax + Spacing, position.y, levelWidth, position.height);

            loopableProp.boolValue = EditorGUI.ToggleLeft(loopableRect, "Loop", loopableProp.boolValue);
            EditorGUI.PropertyField(difficultyRect, difficultyProp, GUIContent.none);
            EditorGUI.PropertyField(levelRect, levelProp, GUIContent.none);

            EditorGUI.indentLevel = previousIndent;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
