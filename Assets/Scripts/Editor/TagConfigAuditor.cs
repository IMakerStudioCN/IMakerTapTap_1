using System.Collections.Generic;
using System.Text;
using TapTapFirst;
using UnityEditor;
using UnityEngine;

namespace TapTapFirst.EditorTools
{
    /// <summary>
    /// Tag 配置体检：查重复 tagId、缺内容、没进总表等问题。
    /// 菜单：Tools/Tag 填空套件/7. 体检 Tag 配置
    /// </summary>
    public static class TagConfigAuditor
    {
        private const string TagDataDir = "Assets/Config/Tag";

        [MenuItem("Tools/Tag \u586b\u7a7a\u5957\u4ef6/7. \u4f53\u68c0 Tag \u914d\u7f6e", false, 40)]
        public static void Audit()
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("===== Tag 配置体检 =====");

            string[] guids = AssetDatabase.FindAssets("t:Tag_SO", new[] { TagDataDir });
            List<Tag_SO> tags = new List<Tag_SO>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Tag_SO tag = AssetDatabase.LoadAssetAtPath<Tag_SO>(path);

                if (tag != null)
                {
                    tags.Add(tag);
                }
            }

            report.AppendLine($"共 {tags.Count} 个 Tag_SO");

            // 1) 重复 tagId
            Dictionary<int, List<string>> byId = new Dictionary<int, List<string>>();

            foreach (Tag_SO tag in tags)
            {
                if (!byId.TryGetValue(tag.tagId, out List<string> names))
                {
                    names = new List<string>();
                    byId[tag.tagId] = names;
                }

                names.Add(tag.name);
            }

            int duplicateCount = 0;

            foreach (KeyValuePair<int, List<string>> pair in byId)
            {
                if (pair.Value.Count > 1)
                {
                    duplicateCount++;
                    report.AppendLine($"  [X] tagId={pair.Key} 重复：{string.Join(", ", pair.Value)}");
                }
            }

            if (duplicateCount == 0)
            {
                report.AppendLine("  [OK] 没有重复 tagId");
            }

            // 2) 每个 Tag 的 4 个词性内容
            foreach (Tag_SO tag in tags)
            {
                if (tag.theTypeToContent == null || tag.theTypeToContent.Count < Tag_SO.WordTypeCount)
                {
                    report.AppendLine($"  [!] {tag.name} 的 theTypeToContent 少于 4 项（会回退到 tagName）");
                    continue;
                }

                List<string> missing = new List<string>();

                for (int i = 0; i < Tag_SO.WordTypeCount; i++)
                {
                    if (string.IsNullOrEmpty(tag.theTypeToContent[i]))
                    {
                        missing.Add(Tag_SO.WordTypeNames[i]);
                    }
                }

                if (missing.Count > 0)
                {
                    report.AppendLine($"  [!] {tag.name} 缺内容：{string.Join(", ", missing)}（没填的词性会显示 tagName）");
                }
            }

            // 3) 是否都进了总表
            string listPath = $"{TagDataDir}/Tag_List_SO.asset";
            TagList_SO list = AssetDatabase.LoadAssetAtPath<TagList_SO>(listPath);

            if (list == null)
            {
                report.AppendLine($"  [X] 找不到 {listPath}（TagSystem 靠它查 tagId）");
            }
            else
            {
                int notInList = 0;

                foreach (Tag_SO tag in tags)
                {
                    if (!list.allTagList.Contains(tag))
                    {
                        notInList++;
                        report.AppendLine($"  [!] {tag.name} 不在 Tag_List_SO 的总表里");
                    }
                }

                if (notInList == 0)
                {
                    report.AppendLine($"  [OK] {tags.Count} 个 Tag 都已在总表里");
                }
            }

            report.AppendLine("===== 体检结束 =====");
            Debug.Log(report.ToString());
        }
    }
}