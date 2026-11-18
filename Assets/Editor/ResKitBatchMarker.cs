using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ResKitBatchMarker
{
    private static readonly string[] Roots = { "Assets/Config" };

    private static readonly HashSet<string> Skip = new HashSet<string>
    {
        "DialogSO 1",
        "11",
        "222",
        "EmailLine_SO",
    };

    private const string Suffix = "_asset";

    [MenuItem("Tools/ResKit/批量打标 Config 下的 SO", false, 1)]
    public static void MarkAll()
    {
        var log = new StringBuilder();
        var paths = CollectPaths();
        var marked = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var path in paths)
            {
                var fileName = Path.GetFileNameWithoutExtension(path);

                if (Skip.Contains(fileName))
                {
                    log.AppendLine("[跳过] " + path);
                    continue;
                }

                var importer = AssetImporter.GetAtPath(path);
                if (importer == null)
                {
                    log.AppendLine("[失败] importer 为空 " + path);
                    continue;
                }

                var bundleName = fileName.ToLower().Replace('.', '_') + Suffix;
                if (importer.assetBundleName == bundleName)
                {
                    log.AppendLine("[已是] " + path + "  ->  " + bundleName);
                    continue;
                }

                importer.assetBundleName = bundleName;
                marked++;
                log.AppendLine("[打标] " + path + "  ->  " + bundleName);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.Refresh();
        }

        Debug.Log("[ResKit] 批量打标完成，新打标 " + marked + " 个，共扫描 " + paths.Count + " 个\n" + log);
    }

    [MenuItem("Tools/ResKit/清除 Config 下的 SO 标", false, 2)]
    public static void ClearAll()
    {
        var paths = CollectPaths();
        var cleared = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var path in paths)
            {
                var importer = AssetImporter.GetAtPath(path);
                if (importer == null || string.IsNullOrEmpty(importer.assetBundleName)) continue;

                importer.assetBundleName = string.Empty;
                cleared++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.Refresh();
        }

        Debug.Log("[ResKit] 已清除 " + cleared + " 个标");
    }

    [MenuItem("Tools/ResKit/报告 Config 下 SO 的打标情况", false, 3)]
    public static void Report()
    {
        var log = new StringBuilder();
        var marked = 0;

        foreach (var path in CollectPaths())
        {
            var importer = AssetImporter.GetAtPath(path);
            var bundle = importer == null ? string.Empty : importer.assetBundleName;
            var isSkip = Skip.Contains(Path.GetFileNameWithoutExtension(path));

            if (!string.IsNullOrEmpty(bundle)) marked++;

            log.AppendLine((string.IsNullOrEmpty(bundle) ? "[未标]" : "[已标]") +
                           (isSkip ? "[测试]" : "      ") +
                           " " + path + "  ->  " + bundle);
        }

        Debug.Log("[ResKit] 打标报告：共 " + CollectPaths().Count + " 个，已标 " + marked + " 个\n" + log);
    }

    private static List<string> CollectPaths()
    {
        var result = new List<string>();
        var guids = AssetDatabase.FindAssets("t:ScriptableObject", Roots);

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;
            if (!path.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase)) continue;
            if (result.Contains(path)) continue;
            result.Add(path);
        }

        result.Sort(System.StringComparer.Ordinal);
        return result;
    }
}
