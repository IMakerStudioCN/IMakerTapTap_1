using TMPro;
using UnityEditor;
using QFramework;
using UnityEngine;

namespace TapTapFirst.DayChangeShow.EditorTools
{
    public static class DayChangeConfigMenu
    {
        const string ResourcesFolder = "Assets/Resources";
        const string ConfigFolder = "Assets/Resources/DayChangeShow";
        const string ConfigPath = ConfigFolder + "/DayChangeConfig.asset";
        const string FontPath = "Assets/Art/Font/TerrarumSansBitmap SDF.asset";

        [MenuItem("TapTap/DayChange/创建或选中天数过场配置", false, 1)]
        public static void CreateOrSelect()
        {
            EnsureFolders();

            var config = AssetDatabase.LoadAssetAtPath<DayChangeConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<DayChangeConfig>();

                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
                if (font != null) config.font = font;

                AssetDatabase.CreateAsset(config, ConfigPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[DayChangeShow] 已创建天数过场配置：" + ConfigPath);
            }

            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }

        [MenuItem("TapTap/DayChange/预览天数过场（当前天数 → 下一天）", false, 20)]
        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[DayChangeShow] 需要先进入 Play 模式再预览天数过场。");
                return;
            }

            var director = DayChangeDirector.Instance;
            if (director == null)
            {
                Debug.LogWarning("[DayChangeShow] 场景里还没有 DayChangeDirector。");
                return;
            }

            var days = TapTap.Interface.GetModel<ITimeModel>().days;
            director.Play(days, days + 1);
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(ResourcesFolder)) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(ConfigFolder)) AssetDatabase.CreateFolder(ResourcesFolder, "DayChangeShow");
        }
    }
}
