using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TapTapFirst.EditorTools
{
    /// <summary>
    /// 把 [TagFontPresetIndex] 标注的 int 字段画成「预设字体」下拉。
    ///
    /// 选项来自同一个组件上的 mFontPresets 列表：
    ///   第 0 项 = 不使用预设（-1，落回 mFont），之后每一项对应 mFontPresets[i]。
    /// 列表为空时退化成普通 int 输入框，不会把 Inspector 画坏。
    /// </summary>
    [CustomPropertyDrawer(typeof(TagFontPresetIndexAttribute))]
    public class TagFontPresetIndexDrawer : PropertyDrawer
    {
        private const string PresetsPropertyName = "mFontPresets";
        private const string NoneLabel = "不使用预设（落回 mFont）";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Integer)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            if (label == null)
            {
                label = GUIContent.none;
            }

            SerializedProperty presets = property.serializedObject.FindProperty(PresetsPropertyName);

            if (presets == null || !presets.isArray || presets.arraySize == 0)
            {
                EditorGUI.BeginChangeCheck();
                int typed = EditorGUI.IntField(position, label, property.intValue);

                if (EditorGUI.EndChangeCheck())
                {
                    property.intValue = typed;
                }

                return;
            }

            List<string> options = new List<string>(presets.arraySize + 1) { NoneLabel };

            for (int i = 0; i < presets.arraySize; i++)
            {
                SerializedProperty item = presets.GetArrayElementAtIndex(i);
                SerializedProperty presetName = item.FindPropertyRelative("name");
                SerializedProperty presetFont = item.FindPropertyRelative("font");

                string display = presetName != null && !string.IsNullOrEmpty(presetName.stringValue)
                    ? presetName.stringValue
                    : "预设 " + i;

                string fontName = presetFont != null && presetFont.objectReferenceValue != null
                    ? presetFont.objectReferenceValue.name
                    : "未指定字体";

                options.Add(display + " (" + fontName + ")");
            }

            // -1 => 第 0 项；越界（预设被删短了）也当没选，避免显示成别的字体
            int current = property.intValue + 1;

            if (current < 0 || current >= options.Count)
            {
                current = 0;
            }

            bool previousMixed = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            int picked = EditorGUI.Popup(position, label.text, current, options.ToArray());
            EditorGUI.showMixedValue = previousMixed;

            if (EditorGUI.EndChangeCheck())
            {
                property.intValue = picked - 1;
            }
        }
    }
}
