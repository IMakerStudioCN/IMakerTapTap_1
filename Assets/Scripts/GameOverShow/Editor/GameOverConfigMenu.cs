using TMPro;
using UnityEditor;
using UnityEngine;

namespace TapTapFirst.GameOverShow.EditorTools
{
    public static class GameOverConfigMenu
    {
        const string ResourcesFolder = "Assets/Resources";
        const string ConfigFolder = "Assets/Resources/GameOverShow";
        const string ConfigPath = ConfigFolder + "/GameOverConfig.asset";
        const string FontPath = "Assets/Art/Font/TerrarumSansBitmap SDF.asset";

        [MenuItem("TapTap/GameOver/创建或选中结局配置", false, 1)]
        public static void CreateOrSelect()
        {
            EnsureFolders();

            var config = AssetDatabase.LoadAssetAtPath<GameOverConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GameOverConfig>();
                config.EnsureDefaults();

                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
                if (font != null) config.font = font;

                AssetDatabase.CreateAsset(config, ConfigPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[GameOverShow] 已创建结局配置：" + ConfigPath);
            }

            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }

        [MenuItem("TapTap/GameOver/预览结局/0 信任归零", false, 20)]
        static void Preview0() => Preview(0);

        [MenuItem("TapTap/GameOver/预览结局/1 道德归零", false, 21)]
        static void Preview1() => Preview(1);

        [MenuItem("TapTap/GameOver/预览结局/2 资金不足", false, 22)]
        static void Preview2() => Preview(2);

        [MenuItem("TapTap/GameOver/预览结局/3 普通结局", false, 23)]
        static void Preview3() => Preview(3);

        [MenuItem("TapTap/GameOver/预览结局/4 NB结局", false, 24)]
        static void Preview4() => Preview(4);

        static void Preview(int endingId)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[GameOverShow] 需要先进入 Play 模式再预览结局。");
                return;
            }

            TapTap.Interface.SendEvent(new GameOverEvent { EndingId = endingId });
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(ResourcesFolder)) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(ConfigFolder)) AssetDatabase.CreateFolder(ResourcesFolder, "GameOverShow");
        }
    }
}
