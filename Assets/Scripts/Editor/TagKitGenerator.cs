using System.Collections.Generic;
using System.IO;
using TapTapFirst;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TapTapFirst.EditorTools
{
    /// <summary>
    /// 文本填空套件生成器。
    ///
    /// 菜单：
    ///   Tools/Tag 填空套件/1. 生成全部资产（预制体 + 示例 Tag）
    ///   Tools/Tag 填空套件/2. 生成 Tag 显示预制体
    ///   Tools/Tag 填空套件/3. 生成 Tag 选项预制体
    ///   Tools/Tag 填空套件/4. 生成文本模版预制体
    ///   Tools/Tag 填空套件/5. 生成 Tag 列表面板预制体
    ///   Tools/Tag 填空套件/6. 把整套摆到当前场景（需要先选一个父节点）
    /// </summary>
    public static class TagKitGenerator
    {
        // ---------------- 路径 ----------------

        private const string KitRoot      = "Assets/Art/ui/UIPrefab/Tag";
        private const string TagDataDir    = "Assets/Config/Tag";
        private const string DisplayPrefab = KitRoot + "/TagDisplay.prefab";
        private const string OptionPrefab  = KitRoot + "/TagOption.prefab";
        private const string TemplatePrefab = KitRoot + "/TagTextTemplate.prefab";
        private const string ListPrefab    = KitRoot + "/TagList.prefab";

        /// <summary>套件里所有文本默认使用的字体</summary>
        private const string DefaultFontPath = "Assets/Art/Font/TerrarumSansBitmap SDF.asset";

        /// <summary>缓存字体，避免每次创建文本都去 AssetDatabase 查</summary>
        private static TMP_FontAsset sDefaultFont;

        // ---------------- 示例内容（想改就改这里） ----------------

        private static readonly string[] SampleNames = { "放大情绪", "删除细节", "冷静处理" };

        /// <summary>每个示例 Tag 在 4 种词性下显示的内容：[形容词, 名词, 量词, 胡扯]</summary>
        private static readonly string[][] SampleContents =
        {
            new[] { "放大情绪的", "放大情绪", "一次放大", "放大就完事了" },
            new[] { "删掉细节的", "删除细节", "一遍删除", "删了就删了" },
            new[] { "冷静的",     "冷静",     "一场冷静", "冷静个鬼" },
        };

        /// <summary>固定文本 + 空缺的交替顺序（string = 固定文本，SlotSetup = 空缺）</summary>
        private static readonly object[] SampleSequence =
        {
            "我真的很",
            new SlotSetup("slot_adjective", TagOfWords.adjective),
            "，因为",
            new SlotSetup("slot_noun", TagOfWords.noun),
            "。",
            new SlotSetup("slot_measure", TagOfWords.measure),
            "就好。",
        };

        private const float SlotSpacing = 320f;

        // ============================================================
        //  菜单
        // ============================================================

        [MenuItem("Tools/Tag \u586b\u7a7a\u5957\u4ef6/1. \u751f\u6210\u5168\u90e8\u8d44\u4ea7", false, 1)]
        public static void GenerateAll()
        {
            EnsureFolders();

            GameObject display = BuildDisplayPrefab();
            GameObject option = BuildOptionPrefab();
            List<Tag_SO> tags = BuildSampleTags();
            BuildTemplatePrefab(display);
            BuildListPrefab(option, tags);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TagKitGenerator] 全部资产已生成到 {KitRoot}");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(TemplatePrefab));
        }

        [MenuItem("Tools/Tag \u586b\u7a7a\u5957\u4ef6/2. \u751f\u6210 Tag \u663e\u793a\u9884\u5236\u4f53", false, 11)]
        public static void GenerateDisplay() { EnsureFolders(); BuildDisplayPrefab(); AssetDatabase.SaveAssets(); AssetDatabase.Refresh(); }

        [MenuItem("Tools/Tag \u586b\u7a7a\u5957\u4ef6/3. \u751f\u6210 Tag \u9009\u9879\u9884\u5236\u4f53", false, 12)]
        public static void GenerateOption() { EnsureFolders(); BuildOptionPrefab(); AssetDatabase.SaveAssets(); AssetDatabase.Refresh(); }

        [MenuItem("Tools/Tag \u586b\u7a7a\u5957\u4ef6/4. \u751f\u6210\u6587\u672c\u6a21\u7248\u9884\u5236\u4f53", false, 13)]
        public static void GenerateTemplate()
        {
            EnsureFolders();
            GameObject display = AssetDatabase.LoadAssetAtPath<GameObject>(DisplayPrefab);

            if (display == null)
            {
                Debug.LogWarning("[TagKitGenerator] 先执行第 2 步生成 Tag 显示预制体，或直接跑第 1 步");
                return;
            }

            BuildTemplatePrefab(display);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Tag \u586b\u7a7a\u5957\u4ef6/5. \u751f\u6210 Tag \u5217\u8868\u9762\u677f\u9884\u5236\u4f53", false, 14)]
        public static void GenerateList()
        {
            EnsureFolders();
            GameObject option = AssetDatabase.LoadAssetAtPath<GameObject>(OptionPrefab);

            if (option == null)
            {
                Debug.LogWarning("[TagKitGenerator] 先执行第 3 步生成 Tag 选项预制体，或直接跑第 1 步");
                return;
            }

            BuildListPrefab(option, BuildSampleTags());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Tag \u586b\u7a7a\u5957\u4ef6/6. \u6446\u5230\u5f53\u524d\u573a\u666f", false, 30)]
        public static void BuildIntoScene()
        {
            GameObject templatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TemplatePrefab);
            GameObject listPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ListPrefab);

            if (templatePrefab == null || listPrefab == null)
            {
                Debug.LogWarning("[TagKitGenerator] 请先执行第 1 步生成全部资产");
                return;
            }

            Transform parent = Selection.activeTransform;

            if (parent == null)
            {
                // 没选就找一个 Canvas
                Canvas canvas = Object.FindObjectOfType<Canvas>();

                if (canvas == null)
                {
                    Debug.LogWarning("[TagKitGenerator] 场景里没有 Canvas，也没有选中父节点，无法摆放");
                    return;
                }

                parent = canvas.transform;
            }

            GameObject root = new GameObject("TagKit", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            RectTransform rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = new Vector2(1200f, 600f);

            GameObject template = (GameObject)PrefabUtility.InstantiatePrefab(templatePrefab, root.transform);
            GameObject list = (GameObject)PrefabUtility.InstantiatePrefab(listPrefab, root.transform);

            // 模版拉伸到父节点上半部分，由 TagTextAdaptiveLayout 自适应排版
            RectTransform templateRect = (RectTransform)template.transform;
            templateRect.anchorMin = new Vector2(0f, 0.45f);
            templateRect.anchorMax = new Vector2(1f, 1f);
            templateRect.offsetMin = new Vector2(40f, 20f);
            templateRect.offsetMax = new Vector2(-40f, -20f);

            // 列表拉伸到父节点下半部分
            RectTransform listRect = (RectTransform)list.transform;
            listRect.anchorMin = new Vector2(0f, 0f);
            listRect.anchorMax = new Vector2(1f, 0.45f);
            listRect.offsetMin = new Vector2(40f, 20f);
            listRect.offsetMax = new Vector2(-40f, -20f);

            EnsureEventSystem();

            Undo.RegisterCreatedObjectUndo(root, "Build TagKit");
            Selection.activeGameObject = root;

            Debug.Log("[TagKitGenerator] 已把整套摆到场景里（TagKit 节点），运行即可测试");
        }

        // ============================================================
        //  预制体构建
        // ============================================================

        /// <summary>Tag 显示件：空缺里显示的内容 + 可选清空按钮。</summary>
        private static GameObject BuildDisplayPrefab()
        {
            GameObject root = new GameObject("TagDisplay", typeof(RectTransform));
            RectTransform rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(220f, 60f);

            Image bg = root.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.14f);
            bg.raycastTarget = true;

            TagDisplay display = root.AddComponent<TagDisplay>();

            // 文本
            GameObject label = new GameObject("Label", typeof(RectTransform));
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(6f, 2f);
            labelRect.offsetMax = new Vector2(-6f, -2f);

            TextMeshProUGUI labelText = label.AddComponent<TextMeshProUGUI>();
            labelText.text = "tag";
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.fontSize = 28f;
            labelText.raycastTarget = false;
            ApplyFont(labelText);

            // 清空按钮（右上角小 ×）
            GameObject clear = new GameObject("ClearButton", typeof(RectTransform));
            RectTransform clearRect = (RectTransform)clear.transform;
            clearRect.SetParent(rect, false);
            clearRect.anchorMin = new Vector2(1f, 1f);
            clearRect.anchorMax = new Vector2(1f, 1f);
            clearRect.pivot = new Vector2(1f, 1f);
            clearRect.anchoredPosition = new Vector2(2f, 2f);
            clearRect.sizeDelta = new Vector2(22f, 22f);

            Image clearBg = clear.AddComponent<Image>();
            clearBg.color = new Color(0.85f, 0.3f, 0.3f, 0.85f);
            clearBg.raycastTarget = true;

            Button clearButton = clear.AddComponent<Button>();
            clearButton.targetGraphic = clearBg;

            GameObject clearLabel = new GameObject("Label", typeof(RectTransform));
            RectTransform clearLabelRect = (RectTransform)clearLabel.transform;
            clearLabelRect.SetParent(clearRect, false);
            clearLabelRect.anchorMin = Vector2.zero;
            clearLabelRect.anchorMax = Vector2.one;
            clearLabelRect.offsetMin = Vector2.zero;
            clearLabelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI clearText = clearLabel.AddComponent<TextMeshProUGUI>();
            clearText.text = "x";
            clearText.alignment = TextAlignmentOptions.Center;
            clearText.fontSize = 16f;
            clearText.raycastTarget = false;
            ApplyFont(clearText);

            // 运行时按 Tag 内容刷新文本；这里同时把按钮接上
            display.ApplySetup(clearButton);

            return SavePrefab(root, DisplayPrefab);
        }

        /// <summary>Tag 选项：列表里可拖拽的一项（可复用，不被消耗）。</summary>
        private static GameObject BuildOptionPrefab()
        {
            GameObject root = new GameObject("TagOption", typeof(RectTransform));
            RectTransform rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(200f, 64f);

            Image bg = root.AddComponent<Image>();
            bg.color = new Color(0.24f, 0.45f, 0.85f, 0.9f);
            bg.raycastTarget = true;

            TagOption option = root.AddComponent<TagOption>();

            GameObject label = new GameObject("Label", typeof(RectTransform));
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 4f);
            labelRect.offsetMax = new Vector2(-8f, -4f);

            TextMeshProUGUI labelText = label.AddComponent<TextMeshProUGUI>();
            labelText.text = "Tag";
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.fontSize = 28f;
            labelText.color = Color.white;
            labelText.raycastTarget = false;
            ApplyFont(labelText);

            return SavePrefab(root, OptionPrefab);
        }

        /// <summary>
        /// 文本模版：固定文本 + 空缺，按 SampleSequence 排布。
        /// 子物体的位置/大小由 TagTextAdaptiveLayout 在运行时按父节点宽度自动算，
        /// 所以这里只负责建节点、定顺序。
        /// </summary>
        private static GameObject BuildTemplatePrefab(GameObject displayPrefab)
        {
            GameObject root = new GameObject("TagTextTemplate", typeof(RectTransform));
            RectTransform rootRect = (RectTransform)root.transform;

            // 拉伸铺满父节点（自适应排版以父节点矩形为可用区域）
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = new Vector2(40f, 40f);
            rootRect.offsetMax = new Vector2(-40f, -40f);

            TagTextTemplate template = root.AddComponent<TagTextTemplate>();
            root.AddComponent<TagTextAdaptiveLayout>();

            var segments = new List<TagTextSegment>();
            int slotIndex = 0;
            int textIndex = 0;

            foreach (object item in SampleSequence)
            {
                if (item is string text)
                {
                    GameObject go = new GameObject($"Text_{textIndex++}", typeof(RectTransform));
                    RectTransform rect = (RectTransform)go.transform;
                    rect.SetParent(rootRect, false);
                    rect.sizeDelta = new Vector2(300f, 64f);

                    TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
                    tmp.text = text;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.fontSize = 32f;
                    tmp.raycastTarget = false;
                    ApplyFont(tmp);

                    segments.Add(new TagTextSegment { TextObject = go, Text = text });
                }
                else if (item is SlotSetup setup)
                {
                    GameObject go = new GameObject(string.IsNullOrEmpty(setup.SlotId) ? $"Slot_{slotIndex}" : setup.SlotId,
                        typeof(RectTransform));
                    RectTransform rect = (RectTransform)go.transform;
                    rect.SetParent(rootRect, false);
                    rect.sizeDelta = new Vector2(280f, 70f);

                    Image hit = go.AddComponent<Image>();
                    hit.color = new Color(1f, 1f, 1f, 0.10f);
                    hit.raycastTarget = true;

                    GameObject placeholder = new GameObject("Placeholder", typeof(RectTransform));
                    RectTransform phRect = (RectTransform)placeholder.transform;
                    phRect.SetParent(rect, false);
                    phRect.anchorMin = Vector2.zero;
                    phRect.anchorMax = Vector2.one;
                    phRect.offsetMin = Vector2.zero;
                    phRect.offsetMax = Vector2.zero;

                    TextMeshProUGUI phText = placeholder.AddComponent<TextMeshProUGUI>();
                    phText.text = "____";
                    phText.alignment = TextAlignmentOptions.Center;
                    phText.fontSize = 32f;
                    phText.color = new Color(0.65f, 0.65f, 0.65f, 1f);
                    phText.raycastTarget = false;
                    ApplyFont(phText);

                    TagKindSlot slot = go.AddComponent<TagKindSlot>();
                    slot.ApplySetup(setup.SlotId, setup.WordType, displayPrefab);
                    slot.SetPlaceholder(placeholder);

                    segments.Add(new TagTextSegment { Slot = slot });
                    slotIndex++;
                }
            }

            template.SetSegments(segments);

            // 生成时先按一个参考尺寸把位置算好，这样预制体在 Project 里预览也是正常排版
            // （运行时 TagTextAdaptiveLayout 会按真实父节点尺寸再排一次）
            ApplyReferenceLayout(rootRect, segments);

            return SavePrefab(root, TemplatePrefab);
        }

        /// <summary>用参考尺寸跑一遍共享排版引擎，把子物体位置落好。</summary>
        private static void ApplyReferenceLayout(RectTransform rootRect, List<TagTextSegment> segments)
        {
            const float referenceWidth = 1200f;
            const float referenceHeight = 300f;

            Vector2 originalSize = rootRect.sizeDelta;
            Vector2 originalAnchorMin = rootRect.anchorMin;
            Vector2 originalAnchorMax = rootRect.anchorMax;

            // 临时固定成参考尺寸
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(referenceWidth, referenceHeight);

            TagLayoutSettings settings = new TagLayoutSettings
            {
                Padding = new Vector4(24f, 16f, 24f, 16f),
                SegmentSpacing = 8f,
                LineSpacing = 12f,
                MinFontSize = 16f,
                MaxFontSize = 32f,
            };

            if (TagSegmentLayout.Layout(rootRect, segments, settings, out string report))
            {
                Debug.Log($"[TagKitGenerator] 模版参考排版完成（参考尺寸 {referenceWidth}x{referenceHeight}）：{report}");
            }
            else
            {
                Debug.LogWarning($"[TagKitGenerator] 模版参考排版跳过：{report}");
            }

            // 还原成拉伸，等运行时按真实父节点再排
            rootRect.anchorMin = originalAnchorMin;
            rootRect.anchorMax = originalAnchorMax;
            rootRect.sizeDelta = originalSize;
        }
        /// <summary>Tag 列表面板：容器 + 一个选项实例（并留空给运行时生成）。</summary>
        private static GameObject BuildListPrefab(GameObject optionPrefab, List<Tag_SO> tags)
        {
            GameObject root = new GameObject("TagList", typeof(RectTransform));
            RectTransform rootRect = (RectTransform)root.transform;
            rootRect.sizeDelta = new Vector2(1200f, 200f);

            // 背景
            Image bg = root.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.25f);
            bg.raycastTarget = false;

            // 标题
            GameObject title = new GameObject("Title", typeof(RectTransform));
            RectTransform titleRect = (RectTransform)title.transform;
            titleRect.SetParent(rootRect, false);
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -6f);
            titleRect.sizeDelta = new Vector2(400f, 40f);

            TextMeshProUGUI titleText = title.AddComponent<TextMeshProUGUI>();
            titleText.text = "Tag 列表（拖到空缺里）";
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontSize = 24f;
            titleText.raycastTarget = false;
            ApplyFont(titleText);

            // 选项容器
            GameObject container = new GameObject("Options", typeof(RectTransform));
            RectTransform containerRect = (RectTransform)container.transform;
            containerRect.SetParent(rootRect, false);
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = new Vector2(0f, -20f);
            containerRect.sizeDelta = new Vector2(1100f, 80f);

            HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            // 用示例 Tag 摆几个选项实例
            int index = 0;

            foreach (Tag_SO tag in tags)
            {
                GameObject option = (GameObject)PrefabUtility.InstantiatePrefab(optionPrefab, containerRect);
                option.name = $"Option_{tag.tagName}";

                RectTransform optionRect = (RectTransform)option.transform;
                optionRect.sizeDelta = new Vector2(200f, 64f);

                TagOption optionComponent = option.GetComponent<TagOption>();
                optionComponent.Setup(tag);

                index++;
            }

            return SavePrefab(root, ListPrefab);
        }

        // ============================================================
        //  示例 Tag 资产
        // ============================================================

        private static List<Tag_SO> BuildSampleTags()
        {
            List<Tag_SO> result = new List<Tag_SO>();
            List<Tag_SO> all = new List<Tag_SO>();

            for (int i = 0; i < SampleNames.Length; i++)
            {
                string assetPath = $"{TagDataDir}/TagKit_{SampleNames[i]}.asset";
                Tag_SO tag = AssetDatabase.LoadAssetAtPath<Tag_SO>(assetPath);

                if (tag == null)
                {
                    tag = ScriptableObject.CreateInstance<Tag_SO>();
                    AssetDatabase.CreateAsset(tag, assetPath);
                }

                tag.tagId = 101 + i;               // 避开旧的 0/1，防止和 Amplify/TEST 撞 id
                tag.tagName = SampleNames[i];
                tag.tagOfWords = TagOfWords.adjective;

                string[] contents = i < SampleContents.Length
                    ? SampleContents[i]
                    : new[] { SampleNames[i], SampleNames[i], SampleNames[i], SampleNames[i] };

                tag.EnsureWordTypeSlots();

                for (int k = 0; k < TagOfWordsLength; k++)
                {
                    tag.SetContent((TagOfWords)k, k < contents.Length ? contents[k] : SampleNames[i]);
                }

                EditorUtility.SetDirty(tag);
                result.Add(tag);
                all.Add(tag);
            }

            // 把示例 Tag 也挂进总表 Tag_List_SO（避免重复添加）
            string listPath = $"{TagDataDir}/Tag_List_SO.asset";
            TagList_SO list = AssetDatabase.LoadAssetAtPath<TagList_SO>(listPath);

            if (list == null)
            {
                list = ScriptableObject.CreateInstance<TagList_SO>();
                AssetDatabase.CreateAsset(list, listPath);
            }

            list.allTagList ??= new List<Tag_SO>();

            foreach (Tag_SO tag in all)
            {
                if (!list.allTagList.Contains(tag))
                {
                    list.allTagList.Add(tag);
                }
            }

            EditorUtility.SetDirty(list);

            return result;
        }

        private const int TagOfWordsLength = 4;

        // ============================================================
        //  工具
        // ============================================================

        /// <summary>取默认字体（取不到返回 null，此时用 TMP 默认字体）。</summary>
        private static TMP_FontAsset GetDefaultFont()
        {
            if (sDefaultFont != null)
            {
                return sDefaultFont;
            }

            sDefaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultFontPath);

            if (sDefaultFont == null)
            {
                Debug.LogWarning($"[TagKitGenerator] 找不到字体 {DefaultFontPath}，将使用 TMP 默认字体");
            }

            return sDefaultFont;
        }

        /// <summary>给文本套上默认字体。</summary>
        private static void ApplyFont(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            TMP_FontAsset font = GetDefaultFont();

            if (font != null)
            {
                text.font = font;
            }
        }
        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Art");
            EnsureFolder("Assets/Art/ui");
            EnsureFolder("Assets/Art/ui/UIPrefab");
            EnsureFolder(KitRoot);
            EnsureFolder("Assets/Config");
            EnsureFolder(TagDataDir);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);

            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log($"[TagKitGenerator] 已生成 {path}");
            return prefab;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
            Debug.Log("[TagKitGenerator] 场景里原本没有 EventSystem，已自动创建");
        }
    }
}