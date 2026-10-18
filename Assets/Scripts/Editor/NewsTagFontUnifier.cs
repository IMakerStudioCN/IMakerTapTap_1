using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace TapTapFirst.EditorTools
{
    /// <summary>
    /// 把 News / Tag 相关预制体里的文字统一成同一个 TMP 字体，并把颜色设成纯黑。
    ///
    /// 覆盖范围：
    ///   Assets/Config/Prefabs/News        （含子目录）
    ///   Assets/Config/Prefabs/Tag         （含子目录）
    ///   Assets/Art/ui/UIPrefab/Tag        （含子目录）
    ///   Assets/Art/ui/UIPrefab/word.prefab（写作面板本体）
    ///
    /// 除了 TMP 组件本身，还会把字体写进 TagTextTemplate 的 mFont / mTextColor，
    /// 因为写作区的文本 / 空缺是运行时 AddComponent 出来的，改预制体改不到它们。
    ///
    /// 菜单：Tools/News &amp; Tag/统一字体与颜色（纯黑）
    /// </summary>
    public static class NewsTagFontUnifier
    {
        private const string FontPath = "Assets/Art/Font/TerrarumSansBitmap SDF.asset";

        /// <summary>要处理的预制体目录（会递归扫子目录）。</summary>
        private static readonly string[] PrefabFolders =
        {
            "Assets/Config/Prefabs/News",
            "Assets/Config/Prefabs/Tag",
            "Assets/Art/ui/UIPrefab/Tag",
        };

        /// <summary>额外单独处理的预制体（写作面板本体）。</summary>
        private static readonly string[] ExtraPrefabs =
        {
            "Assets/Art/ui/UIPrefab/word.prefab",
        };

        [MenuItem("Tools/News & Tag/\u7edf\u4e00\u5b57\u4f53\u4e0e\u989c\u8272\uff08\u7eaf\u9ed1\uff09", false, 0)]
        public static void UnifyFontAndColor()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            if (font == null)
            {
                Debug.LogError($"[NewsTagFont] 找不到字体资产：{FontPath}");
                return;
            }

            List<string> paths = CollectPrefabPaths();

            if (paths.Count == 0)
            {
                Debug.LogWarning("[NewsTagFont] 没有找到要处理的预制体");
                return;
            }

            int changedPrefabs = 0;
            int changedTexts = 0;

            foreach (string path in paths)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);

                if (root == null)
                {
                    Debug.LogWarning($"[NewsTagFont] 加载不到 {path}，跳过");
                    continue;
                }

                try
                {
                    int changed = ApplyToPrefab(root, font);

                    if (changed > 0)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        changedPrefabs++;
                        changedTexts += changed;
                        Debug.Log($"[NewsTagFont] {path}：{changed} 处已统一");
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[NewsTagFont] 完成：{changedPrefabs}/{paths.Count} 个预制体、{changedTexts} 处文字 -> " +
                      $"{FontPath}，颜色纯黑");
        }

        private static List<string> CollectPrefabPaths()
        {
            List<string> paths = new List<string>();

            foreach (string folder in PrefabFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    Debug.LogWarning($"[NewsTagFont] 目录不存在，跳过：{folder}");
                    continue;
                }

                foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    if (!string.IsNullOrEmpty(path) && !paths.Contains(path))
                    {
                        paths.Add(path);
                    }
                }
            }

            foreach (string extra in ExtraPrefabs)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(extra) != null && !paths.Contains(extra))
                {
                    paths.Add(extra);
                }
            }

            return paths;
        }

        /// <summary>返回这个预制体里被改动的处数。</summary>
        private static int ApplyToPrefab(GameObject root, TMP_FontAsset font)
        {
            int changed = 0;

            // 1) 所有现成的 TMP 组件
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (ApplyToText(text, font))
                {
                    changed++;
                }
            }

            // 2) 运行时生成文字的模版：把字体 / 颜色写进它的序列化字段
            foreach (TagTextTemplate template in root.GetComponentsInChildren<TagTextTemplate>(true))
            {
                if (ApplyToTemplate(template, font))
                {
                    changed++;
                }
            }

            return changed;
        }

        private static bool ApplyToText(TMP_Text text, TMP_FontAsset font)
        {
            bool changed = false;

            if (text.font != font)
            {
                text.font = font;
                changed = true;
            }

            if (text.color != Color.black)
            {
                text.color = Color.black;
                changed = true;
            }

            // 材质还指着旧字体的图集时换回新字体的默认材质，否则会显示成豆腐块
            Material material = font.material;

            if (material != null &&
                (text.fontSharedMaterial == null || text.fontSharedMaterial.mainTexture != font.atlasTexture))
            {
                text.fontSharedMaterial = material;
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(text);
            }

            return changed;
        }

        private static bool ApplyToTemplate(TagTextTemplate template, TMP_FontAsset font)
        {
            SerializedObject serialized = new SerializedObject(template);

            SerializedProperty fontProperty = serialized.FindProperty("mFont");
            SerializedProperty colorProperty = serialized.FindProperty("mTextColor");

            if (fontProperty == null && colorProperty == null)
            {
                return false;
            }

            bool changed = false;

            if (fontProperty != null && fontProperty.objectReferenceValue != font)
            {
                fontProperty.objectReferenceValue = font;
                changed = true;
            }

            if (colorProperty != null && colorProperty.colorValue != Color.black)
            {
                colorProperty.colorValue = Color.black;
                changed = true;
            }

            if (changed)
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(template);
            }

            return changed;
        }
    }
}