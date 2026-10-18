using System.Collections.Generic;
using TapTapFirst;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TapTapFirst.EditorTools
{
    /// <summary>
    /// word.prefab（原 WritePanel.prefab）的布局工具。
    ///
    /// 菜单：
    ///   0. 重构三栏布局（ModelList / TagModel / TagList）  ← 三栏各占 ContentBox 的 1/3，屏幕自适应
    ///   1. 生成 ModelList 模板列表
    ///   2. 生成卡片与内容预制体
    ///   3. 生成测试新闻模板
    ///   4. 刷新 prefab 卡片顺序
    ///   5. 清理测试新闻
    /// </summary>
    public static class WritePanelTemplateListBuilder
    {
        private const string PrefabPath = "Assets/Art/ui/UIPrefab/word.prefab";

        private const string NewsDir = "Assets/Config/News";

        /// <summary>卡片 / 内容预制体目录</summary>
        private const string NewsPrefabDir = "Assets/Config/Prefabs/News";


        /// <summary>Tag 套件产出的 Tag 卡片（也用作空缺显示件）</summary>
        private const string TagOptionPrefabPath = "Assets/Config/Prefabs/Tag/TagOption.prefab";
        private const float ListWidth = 420f;
        private const float ListHeight = 670f;

        private const float ScrollbarWidth = 16f;
        private const float CardHeight = 110f;
        private const float CardSpacing = 12f;
        private const float Padding = 12f;

        // 标题：样式对齐 TagList/Title（字号 24、居中、高 40）
        private const float TitleHeight = 40f;
        private const float TitleGap = 4f;
        private const float TitleFontSize = 24f;
        private const string TitleText = "模板列表（点选切换）";

        [MenuItem("Tools/WritePanel/\u751f\u6210 ModelList \u6a21\u677f\u5217\u8868", false, 1)]
        public static void Build()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);

            if (prefabRoot == null)
            {
                Debug.LogError($"[WritePanelTool] 加载不到 {PrefabPath}");
                return;
            }

            try
            {
                Transform modelList = FindByName(prefabRoot.transform, "ModelList");

                if (modelList == null)
                {
                    Debug.LogError("[WritePanelTool] prefab 里找不到 ModelList 节点");
                    return;
                }

                RectTransform root = modelList as RectTransform;
                Rebuild(root);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[WritePanelTool] {PrefabPath} 的 ModelList 已重建为竖版模板列表");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        // ============================================================
        //  测试数据：生成若干 NewsTemplate_SO
        // ============================================================

        /// <summary>测试用新闻：标题 + 词性内容</summary>
        private struct SampleNews
        {
            public int Id;
            public string Title;

            public SampleNews(int id, string title)
            {
                Id = id;
                Title = title;
            }
        }

        /// <summary>测试新闻清单（想改就直接改这里）</summary>
        private static readonly SampleNews[] SampleNewsList =
        {
            new SampleNews(101, "城市晨报"),
            new SampleNews(102, "科技周刊"),
            new SampleNews(103, "体育快讯"),
            new SampleNews(104, "生活副刊"),
            new SampleNews(105, "财经观察"),
        };

        [MenuItem("Tools/WritePanel/\u751f\u6210\u6d4b\u8bd5\u65b0\u95fb\u6a21\u677f", false, 3)]
        public static void GenerateTestNews()
        {
            if (!EditorUtility.DisplayDialog("生成测试新闻模板",
                    $"将生成 {SampleNewsList.Length} 个 NewsTemplate_SO 到 {NewsDir}，\n" +
                    "并写入 NewsTemplateList 总表、刷新 prefab 里的卡片。\n\n继续？", "生成", "取消"))
            {
                return;
            }

            List<NewsTemplate_SO> created = CreateTestNewsAssets();
            List<NewsTemplate_SO> prefabOrder = AssignTemplateList(created);

            RefreshPrefabCards(prefabOrder, created);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[WritePanelTool] 测试新闻已生成：{created.Count} 个资产 + prefab 卡片已刷新");
        }
        // ============================================================
        //  卡片 / 内容预制体
        // ============================================================

        [MenuItem("Tools/WritePanel/\u751f\u6210\u5361\u7247\u4e0e\u5185\u5bb9\u9884\u5236\u4f53", false, 2)]
        public static void GenerateCardsAndContentPrefabs()
        {
            EnsureFolder("Assets/Config");
            EnsureFolder("Assets/Config/Prefabs");
            EnsureFolder(NewsPrefabDir);

            BuildTemplateCardPrefab();
            BuildNewsContentPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[WritePanelTool] 预制体已生成到 {NewsPrefabDir}");
        }

        /// <summary>模板列表里的一张新闻卡片。</summary>
        private static void BuildTemplateCardPrefab()
        {
            GameObject root = new GameObject("TemplateCard", typeof(RectTransform));
            RectTransform rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(0f, 110f);

            Image bg = root.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.14f);
            bg.raycastTarget = true;

            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.minHeight = 110f;
            layout.preferredHeight = 110f;

            TextMeshProUGUI label = CreateTextNode(rect, "Title", "模板标题", 28f, Color.black);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.offsetMin = new Vector2(16f, 8f);
            labelRect.offsetMax = new Vector2(-16f, -8f);

            root.AddComponent<TemplateCardItem>();

            SavePrefabAt(root, $"{NewsPrefabDir}/TemplateCard.prefab");
        }

        /// <summary>写作内容模版：内部就是 TagTextTemplate 的结构（Text_* + slot_*）。</summary>
        private static void BuildNewsContentPrefab()
        {
            GameObject root = new GameObject("NewsContent", typeof(RectTransform));
            RectTransform rootRect = (RectTransform)root.transform;
            rootRect.sizeDelta = new Vector2(900f, 300f);

            TagTextTemplate template = root.AddComponent<TagTextTemplate>();
            root.AddComponent<TagTextAdaptiveLayout>();

            // 空缺里显示的内容件：直接复用 Tag 套件产出的 TagOption.prefab
            GameObject displayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TagOptionPrefabPath);

            List<object> sequence = new List<object>
            {
                "我真的很",
                new SlotSetup("slot_adjective", TagOfWords.adjective),
                "，因为",
                new SlotSetup("slot_noun", TagOfWords.noun),
                "。",
                new SlotSetup("slot_measure", TagOfWords.measure),
                "就好。",
            };

            template.Configure(sequence, displayPrefab);

            SavePrefabAt(root, $"{NewsPrefabDir}/NewsContent.prefab");
        }

        /// <summary>建一个铺满父级的文本节点（统一套默认字体）。</summary>
        private static TextMeshProUGUI CreateTextNode(RectTransform parent, string name, string text,
            float fontSize, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.raycastTarget = false;

            TMP_FontAsset font = LoadDefaultFont();

            if (font != null)
            {
                tmp.font = font;
            }

            return tmp;
        }

        /// <summary>把临时物体存成预制体并销毁。</summary>
        private static void SavePrefabAt(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log($"[WritePanelTool] 已生成 {path}");
        }

        [MenuItem("Tools/WritePanel/\u5237\u65b0 prefab \u5361\u7247\u987a\u5e8f", false, 4)]
        public static void RefreshPrefabCardsMenu()
        {
            List<NewsTemplate_SO> templates = ReadTemplateList();

            if (templates == null || templates.Count == 0)
            {
                Debug.LogWarning("[WritePanelTool] NewsTemplateList 是空的，先执行「生成测试新闻模板」");
                return;
            }

            RefreshPrefabCards(templates, templates);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[WritePanelTool] prefab 卡片已按总表顺序刷新：{templates.Count} 张");
        }

        [MenuItem("Tools/WritePanel/\u6e05\u7406\u6d4b\u8bd5\u65b0\u95fb", false, 5)]
        public static void ClearTestNews()
        {
            string[] guids = AssetDatabase.FindAssets("t:NewsTemplate_SO", new[] { NewsDir });

            if (guids.Length == 0)
            {
                Debug.Log("[WritePanelTool] 没有测试新闻模板要清理");
                return;
            }

            if (!EditorUtility.DisplayDialog("清理测试新闻",
                    $"将删除 {NewsDir} 下的 {guids.Length} 个 NewsTemplate_SO，\n" +
                    "总表里指向它们的条目也会移除。\n\n继续？", "删除", "取消"))
            {
                return;
            }

            // 先从总表里摘掉，避免留下丢失引用
            NewsTemplateList_SO list = LoadOrCreateList();
            int removedRefs = 0;

            if (list != null && list.newsTemplateList != null)
            {
                for (int i = list.newsTemplateList.Count - 1; i >= 0; i--)
                {
                    NewsTemplate_SO item = list.newsTemplateList[i];

                    if (item != null && AssetDatabase.GetAssetPath(item).StartsWith(NewsDir))
                    {
                        list.newsTemplateList.RemoveAt(i);
                        removedRefs++;
                    }
                }

                EditorUtility.SetDirty(list);
            }

            foreach (string guid in guids)
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[WritePanelTool] 已删除 {guids.Length} 个测试新闻，总表移除 {removedRefs} 条引用");
        }

        /// <summary>建 / 覆盖测试新闻资产。</summary>
        private static List<NewsTemplate_SO> CreateTestNewsAssets()
        {
            EnsureFolder("Assets/Config");
            EnsureFolder(NewsDir);

            List<NewsTemplate_SO> created = new List<NewsTemplate_SO>();

            foreach (SampleNews sample in SampleNewsList)
            {
                string path = $"{NewsDir}/NewsTemplate_{sample.Id}.asset";
                NewsTemplate_SO asset = AssetDatabase.LoadAssetAtPath<NewsTemplate_SO>(path);

                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<NewsTemplate_SO>();
                    AssetDatabase.CreateAsset(asset, path);
                }

                asset.newsId = sample.Id;
                asset.newsTitle = sample.Title;

                // 真正的内容必须写进 contentSegments，否则写作区一个子物体都不会生成（整片空白）
                asset.contentSegments = BuildSampleSegments(sample.Id);

                // 倾向数值只在没配过时补默认值，别覆盖手调过的
                if (asset.effectValues == null || asset.effectValues.Count == 0)
                {
                    asset.SetEffectValue(NewsEffectType.Objective, 1f);
                    asset.SetEffectValue(NewsEffectType.Positive, 1.5f);
                    asset.SetEffectValue(NewsEffectType.Negative, 0.5f);
                }

                EditorUtility.SetDirty(asset);
                created.Add(asset);
            }

            return created;
        }

        /// <summary>
        /// 给测试新闻造一段「固定文本 + 空缺」的内容。
        /// 想改测试文案直接改这里；不配的话写作区会是空的。
        /// </summary>
        private static List<NewsContentSegment> BuildSampleSegments(int newsId)
        {
            List<NewsContentSegment> segments = new List<NewsContentSegment>();

            switch (newsId)
            {
                case 101:
                    AddText(segments, "今日");
                    AddSlot(segments, "slot_1", TagOfWords.noun);
                    AddText(segments, "席卷全城，多位");
                    AddSlot(segments, "slot_2", TagOfWords.adjective);
                    AddText(segments, "市民表示，应当");
                    AddSlot(segments, "slot_3", TagOfWords.noun);
                    AddText(segments, "。");
                    break;

                case 102:
                    AddText(segments, "本周");
                    AddSlot(segments, "slot_1", TagOfWords.noun);
                    AddText(segments, "正式发布，业内评价");
                    AddSlot(segments, "slot_2", TagOfWords.adjective);
                    AddText(segments, "，预计影响");
                    AddSlot(segments, "slot_3", TagOfWords.measure);
                    AddText(segments, "用户。");
                    break;

                case 103:
                    AddText(segments, "昨晚的比赛以");
                    AddSlot(segments, "slot_1", TagOfWords.adjective);
                    AddText(segments, "收场，主教练称这是一场");
                    AddSlot(segments, "slot_2", TagOfWords.noun);
                    AddText(segments, "。");
                    break;

                case 104:
                    AddText(segments, "入秋之后，");
                    AddSlot(segments, "slot_1", TagOfWords.noun);
                    AddText(segments, "成了热门话题，不少读者分享了");
                    AddSlot(segments, "slot_2", TagOfWords.adjective);
                    AddText(segments, "的心得。");
                    break;

                case 105:
                    AddText(segments, "本月指数");
                    AddSlot(segments, "slot_1", TagOfWords.adjective);
                    AddText(segments, "，分析师提醒关注");
                    AddSlot(segments, "slot_2", TagOfWords.noun);
                    AddText(segments, "的波动。");
                    break;

                default:
                    AddText(segments, "这是一条测试新闻：");
                    AddSlot(segments, "slot_1", TagOfWords.noun);
                    AddText(segments, "，请填入");
                    AddSlot(segments, "slot_2", TagOfWords.adjective);
                    AddText(segments, "的内容。");
                    break;
            }

            return segments;
        }

        private static void AddText(List<NewsContentSegment> segments, string text)
        {
            segments.Add(NewsContentSegment.Text(text));
        }

        private static void AddSlot(List<NewsContentSegment> segments, string slotId, TagOfWords wordType)
        {
            segments.Add(NewsContentSegment.Slot(slotId, wordType));
        }

        /// <summary>
        /// 写入总表：已有手工配置排在前面并保留，测试数据按 id 升序追加，清理重复与空引用。
        /// </summary>
        private static List<NewsTemplate_SO> AssignTemplateList(List<NewsTemplate_SO> testNews)
        {
            NewsTemplateList_SO list = LoadOrCreateList();
            List<NewsTemplate_SO> source = list.newsTemplateList ?? new List<NewsTemplate_SO>();

            List<NewsTemplate_SO> kept = new List<NewsTemplate_SO>();
            HashSet<int> usedIds = new HashSet<int>();

            // 1) 保留原有条目（去重、去空）
            foreach (NewsTemplate_SO item in source)
            {
                if (item == null)
                {
                    continue;
                }

                if (usedIds.Contains(item.newsId))
                {
                    Debug.LogWarning($"[WritePanelTool] 总表里 newsId={item.newsId} 重复（{item.name}），已去重");
                    continue;
                }

                usedIds.Add(item.newsId);
                kept.Add(item);
            }

            // 2) 追加测试数据
            foreach (NewsTemplate_SO item in testNews)
            {
                if (item == null || usedIds.Contains(item.newsId))
                {
                    continue;
                }

                usedIds.Add(item.newsId);
                kept.Add(item);
            }

            // 3) 按 id 升序，让卡片顺序可预期
            kept.Sort((a, b) => a.newsId.CompareTo(b.newsId));

            list.newsTemplateList = kept;
            EditorUtility.SetDirty(list);

            return kept;
        }

        private static NewsTemplateList_SO LoadOrCreateList()
        {
            EnsureFolder("Assets/Config");
            EnsureFolder(NewsDir);

            string path = $"{NewsDir}/NewsTemplateList.asset";
            NewsTemplateList_SO list = AssetDatabase.LoadAssetAtPath<NewsTemplateList_SO>(path);

            if (list == null)
            {
                list = ScriptableObject.CreateInstance<NewsTemplateList_SO>();
                AssetDatabase.CreateAsset(list, path);
            }

            if (list.newsTemplateList == null)
            {
                list.newsTemplateList = new List<NewsTemplate_SO>();
            }

            return list;
        }

        private static List<NewsTemplate_SO> ReadTemplateList()
        {
            NewsTemplateList_SO list = AssetDatabase.LoadAssetAtPath<NewsTemplateList_SO>($"{NewsDir}/NewsTemplateList.asset");
            return list != null ? list.newsTemplateList : null;
        }

        /// <summary>把 prefab 里 ModelList/Viewport/Content 的卡片按给定顺序改名换标题。</summary>
        private static void RefreshPrefabCards(List<NewsTemplate_SO> prefabOrder, List<NewsTemplate_SO> creationOrder)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);

            if (prefabRoot == null)
            {
                Debug.LogError($"[WritePanelTool] 加载不到 {PrefabPath}");
                return;
            }

            try
            {
                Transform modelList = FindByName(prefabRoot.transform, "ModelList");

                if (modelList == null)
                {
                    Debug.LogError("[WritePanelTool] prefab 里找不到 ModelList");
                    return;
                }

                Transform viewport = modelList.Find("Viewport");
                Transform content = viewport != null ? viewport.Find("Content") : null;

                if (content == null)
                {
                    Debug.LogError("[WritePanelTool] 找不到 ModelList/Viewport/Content，先执行「生成 ModelList 模板列表」");
                    return;
                }

                // 1) 摘出所有空状态卡片
                List<GameObject> cards = new List<GameObject>();

                for (int i = content.childCount - 1; i >= 0; i--)
                {
                    Transform child = content.GetChild(i);
                    TemplateCardItem item = child.GetComponent<TemplateCardItem>();

                    if (item != null)
                    {
                        cards.Add(child.gameObject);
                    }
                }

                cards.Reverse();   // 恢复原有顺序

                // 2) 数量不够就补卡片
                int needed = prefabOrder != null ? prefabOrder.Count : 0;

                while (cards.Count < needed && content.childCount > 0)
                {
                    GameObject clone = Object.Instantiate(content.GetChild(content.childCount - 1).gameObject, content);
                    clone.name = "TemplateCard(Clone)";
                    cards.Add(clone);
                }

                // 3) 按顺序改名 / 换标题
                for (int i = 0; i < cards.Count; i++)
                {
                    TemplateCardItem card = cards[i].GetComponent<TemplateCardItem>();
                    bool hasData = i < needed;

                    NewsTemplate_SO news = hasData ? prefabOrder[i] : null;
                    int newsId = news != null ? news.newsId : -1;
                    string title = news != null ? news.newsTitle : "模板占位";

                    cards[i].name = hasData ? $"Template_{newsId}_{title}" : "Template_Placeholder";
                    card.Setup(newsId, title);
                    cards[i].transform.SetSiblingIndex(i);
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
                Debug.Log($"[WritePanelTool] prefab 卡片已刷新：{cards.Count} 张 / 数据 {needed} 条");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);

            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }
        // ============================================================
        //  搭结构
        // ============================================================

        private static void Rebuild(RectTransform root)
        {
            // 记录 / 清空原有子物体（保留 BackGround 这类纯装饰）
            List<GameObject> keep = new List<GameObject>();
            List<GameObject> remove = new List<GameObject>();

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);

                // Title 和 BackGround 是固定装饰，重建时保留
                bool isBackGround = child.name == "BackGround";
                bool isTitle = child.name == "Title";

                if (isBackGround || isTitle)
                {
                    keep.Add(child.gameObject);

                    if (isBackGround)
                    {
                        // 背景铺满
                        RectTransform bg = child as RectTransform;

                        if (bg != null)
                        {
                            bg.anchorMin = Vector2.zero;
                            bg.anchorMax = Vector2.one;
                            bg.offsetMin = Vector2.zero;
                            bg.offsetMax = Vector2.zero;
                        }
                    }
                }
                else
                {
                    remove.Add(child.gameObject);
                }
            }

            foreach (GameObject go in remove)
            {
                Object.DestroyImmediate(go);
            }

            // ---- 标题（样式对齐 TagList/Title） ----
            RectTransform title = EnsureChild(root, "Title");
            title.SetSiblingIndex(0);          // 放在最前，避免盖住卡片
            title.anchorMin = new Vector2(0f, 1f);
            title.anchorMax = new Vector2(1f, 1f);
            title.pivot = new Vector2(0.5f, 1f);
            title.anchoredPosition = Vector2.zero;
            title.sizeDelta = new Vector2(0f, TitleHeight);

            TextMeshProUGUI titleText = title.GetComponent<TextMeshProUGUI>();

            if (titleText == null)
            {
                titleText = title.gameObject.AddComponent<TextMeshProUGUI>();
            }

            titleText.text = TitleText;
            titleText.fontSize = TitleFontSize;
            titleText.alignment = TextAlignmentOptions.Center;   // 水平居中 + 垂直居中
            titleText.color = Color.black;
            titleText.raycastTarget = false;
            titleText.enableWordWrapping = true;

            TMP_FontAsset titleFont = LoadDefaultFont();

            if (titleFont != null)
            {
                titleText.font = titleFont;
            }
            // ---- ScrollRect ----
            ScrollRect scroll = root.GetComponent<ScrollRect>();

            if (scroll == null)
            {
                scroll = root.gameObject.AddComponent<ScrollRect>();
            }

            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.135f;

            // ---- Viewport（遮罩） ----
            RectTransform viewport = EnsureChild(root, "Viewport");
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(Padding, Padding);
            // 顶部让出标题的高度
            viewport.offsetMax = new Vector2(-(ScrollbarWidth + Padding), -(Padding + TitleHeight + TitleGap));
            viewport.pivot = new Vector2(0f, 1f);

            Image viewportImage = viewport.GetComponent<Image>();

            if (viewportImage == null)
            {
                viewportImage = viewport.gameObject.AddComponent<Image>();
            }

            viewportImage.color = new Color(1f, 1f, 1f, 0.004f);
            viewportImage.raycastTarget = true;

            RectMask2D mask = viewport.GetComponent<RectMask2D>();

            if (mask == null)
            {
                viewport.gameObject.AddComponent<RectMask2D>();
            }

            // ---- Content（竖排 + 自适应高度） ----
            RectTransform content = EnsureChild(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, CardHeight);

            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();

            if (layout == null)
            {
                layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.padding = new RectOffset((int)Padding, (int)Padding, (int)Padding, (int)Padding);
            layout.spacing = CardSpacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();

            if (fitter == null)
            {
                fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ---- 垂直滚动条（固定在右侧） ----
            RectTransform scrollbar = EnsureChild(root, "Scrollbar Vertical");
            scrollbar.anchorMin = new Vector2(1f, 0f);
            scrollbar.anchorMax = new Vector2(1f, 1f);
            scrollbar.pivot = new Vector2(1f, 1f);
            scrollbar.anchoredPosition = Vector2.zero;
            scrollbar.sizeDelta = new Vector2(ScrollbarWidth, -(Padding * 2f + TitleHeight + TitleGap));

            Image barBg = scrollbar.GetComponent<Image>();

            if (barBg == null)
            {
                barBg = scrollbar.gameObject.AddComponent<Image>();
            }

            barBg.color = new Color(0f, 0f, 0f, 0.25f);
            barBg.raycastTarget = true;

            Scrollbar bar = scrollbar.GetComponent<Scrollbar>();

            if (bar == null)
            {
                bar = scrollbar.gameObject.AddComponent<Scrollbar>();
            }

            bar.direction = Scrollbar.Direction.BottomToTop;
            bar.targetGraphic = barBg;

            RectTransform slidingArea = EnsureChild(scrollbar, "Sliding Area");
            slidingArea.anchorMin = Vector2.zero;
            slidingArea.anchorMax = Vector2.one;
            slidingArea.offsetMin = Vector2.zero;
            slidingArea.offsetMax = Vector2.zero;

            RectTransform handle = EnsureChild(slidingArea, "Handle");
            handle.anchorMin = Vector2.zero;
            handle.anchorMax = Vector2.one;
            handle.offsetMin = Vector2.zero;
            handle.offsetMax = Vector2.zero;

            Image handleImage = handle.GetComponent<Image>();

            if (handleImage == null)
            {
                handleImage = handle.gameObject.AddComponent<Image>();
            }

            handleImage.color = new Color(1f, 1f, 1f, 0.7f);
            handleImage.raycastTarget = true;

            bar.handleRect = handle;
            bar.targetGraphic = handleImage;

            // 接线
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scroll.verticalScrollbarSpacing = 0f;

            // ---- 铺模板卡片 ----
            int count = SpawnTemplateCards(content);

            // 滚到顶部
            scroll.verticalNormalizedPosition = 1f;

            Debug.Log($"[WritePanelTool] 模板列表已生成：{count} 张卡片" +
                      (count == 0 ? "（NewsTemplateList_SO 里没有配置，或没找到资源）" : string.Empty));
        }

        // ============================================================
        //  卡片
        // ============================================================

        private static int SpawnTemplateCards(RectTransform content)
        {
            List<NewsTemplate_SO> templates = LoadTemplates();

            if (templates == null || templates.Count == 0)
            {
                // 至少放一张占位卡片，方便看排版
                CreateCard(content, "（没有配置新闻模板）", -1, true);
                return 0;
            }

            for (int i = 0; i < templates.Count; i++)
            {
                NewsTemplate_SO news = templates[i];
                CreateCard(content, news != null ? news.newsTitle : $"模板 {i}", news != null ? news.newsId : i, false);
            }

            return templates.Count;
        }

        private static void CreateCard(RectTransform parent, string title, int newsId, bool isPlaceholder)
        {
            GameObject card = new GameObject($"Template_{newsId}_{title}", typeof(RectTransform));
            RectTransform cardRect = (RectTransform)card.transform;
            cardRect.SetParent(parent, false);
            cardRect.sizeDelta = new Vector2(0f, CardHeight);

            LayoutElement element = card.AddComponent<LayoutElement>();
            element.minHeight = CardHeight;
            element.preferredHeight = CardHeight;

            Image bg = card.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, isPlaceholder ? 0.06f : 0.14f);
            bg.raycastTarget = true;

            // 标题
            GameObject label = new GameObject("Title", typeof(RectTransform));
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.SetParent(cardRect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(16f, 8f);
            labelRect.offsetMax = new Vector2(-16f, -8f);

            TextMeshProUGUI text = label.AddComponent<TextMeshProUGUI>();
            text.text = title;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.fontSize = 28f;
            text.color = Color.black;
            text.raycastTarget = false;

            TMP_FontAsset font = LoadDefaultFont();

            if (font != null)
            {
                text.font = font;
            }

            // 填数据
            if (!isPlaceholder)
            {
                TemplateCardItem cardItem = card.AddComponent<TemplateCardItem>();
                cardItem.Setup(newsId, title);
            }


        }

        // ============================================================
        //  资源
        // ============================================================

        private static List<NewsTemplate_SO> LoadTemplates()
        {
            string[] guids = AssetDatabase.FindAssets("t:NewsTemplateList_SO", new[] { "Assets/Config" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                NewsTemplateList_SO list = AssetDatabase.LoadAssetAtPath<NewsTemplateList_SO>(path);

                if (list != null && list.newsTemplateList != null && list.newsTemplateList.Count > 0)
                {
                    Debug.Log($"[WritePanelTool] 使用模板总表：{path}（{list.newsTemplateList.Count} 条）");
                    return list.newsTemplateList;
                }
            }

            Debug.LogWarning("[WritePanelTool] Assets/Config 下没有找到有内容的 NewsTemplateList_SO");
            return null;
        }

        private static TMP_FontAsset LoadDefaultFont()
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Font/TerrarumSansBitmap SDF.asset");
        }

        // ============================================================
        //  三栏布局：ModelList / TagModel / TagList 各占 ContentBox 的 1/3
        //    三条竖向等分栏，锚点用 0 / 1/3 / 2/3 / 1，
        //    所以窗口从普通尺寸拉到全屏（WindowKit 全屏）都自动按比例分。
        // ============================================================

        /// <summary>栏与栏之间的留白</summary>
        private const float ColumnGap = 6f;


        /// <summary>面板内部留白</summary>
        private const float ColPadding = 10f;

        /// <summary>面板标题高</summary>
        private const float ColTitleHeight = 40f;

        /// <summary>标题与内容之间的间隙</summary>
        private const float ColTitleGap = 6f;

        /// <summary>垂直滚动条宽度</summary>
        private const float ColScrollbarWidth = 16f;

        /// <summary>列表卡片高</summary>
        private const float ColCardHeight = 110f;

        /// <summary>列表卡片间距</summary>
        private const float ColCardSpacing = 10f;

        private const float ColTitleFontSize = 24f;

        [MenuItem("Tools/WritePanel/\u91cd\u6784\u4e09\u680f\u5e03\u5c40\uff08ModelList / TagModel / TagList\uff09", false, 0)]
        public static void RebuildThreeColumnsMenu()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);

            if (prefabRoot == null)
            {
                Debug.LogError($"[WritePanelTool] 加载不到 {PrefabPath}");
                return;
            }

            try
            {
                RectTransform contentBox = FindRect(prefabRoot.transform, "ContentBox");

                if (contentBox == null)
                {
                    Debug.LogError("[WritePanelTool] prefab 里找不到 ContentBox 节点");
                    return;
                }

                RectTransform context = FindRect(contentBox, "Context");

                if (context == null)
                {
                    context = EnsureChild(contentBox, "Context");
                    Debug.LogWarning("[WritePanelTool] prefab 里没有 Context，已新建一个");
                }

                if (!RebuildThreeColumns(context))
                {
                    Debug.LogError("[WritePanelTool] 三栏布局重构失败，prefab 未保存");
                    return;
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[WritePanelTool] {PrefabPath} 已重构为三栏等分布局（ModelList / TagModel / TagList）");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        /// <summary>把 Context 下的三个面板摆成等分三栏并重建各自骨架。成功返回 true。</summary>
        /// <summary>把 Context 下的三个面板摆成等分三栏并重建各自骨架。成功返回 true。</summary>
        private static bool RebuildThreeColumns(RectTransform context)
        {
            // ---- 1) Context 铺满 ContentBox ----
            StretchToParent(context, 0f, 0f, 0f, 0f);

            // ---- 2) 找到三个面板 ----
            RectTransform modelList = FindRect(context, "ModelList");
            RectTransform tagModel = FindRect(context, "TagModel");
            RectTransform tagList = FindRect(context, "TagList");

            if (modelList == null || tagModel == null || tagList == null)
            {
                Debug.LogError($"[WritePanelTool] 三栏节点不全：ModelList={modelList != null} " +
                               $"TagModel={tagModel != null} TagList={tagList != null}");
                return false;
            }

            // 三个面板统一搬成 Context 的直接子节点。
            // 之前只搬了 TagList，另外两个留在嵌套的中间节点里（比如 100x100 的 word），
            // 于是只有 TagList 比例正常、另两栏被压成 100x100。
            RectTransform[] panels = { modelList, tagModel, tagList };

            foreach (RectTransform panel in panels)
            {
                if (panel != null && panel.parent != context)
                {
                    panel.SetParent(context, false);
                }
            }

            // ---- 3) 三栏锚点：0 / 1/3 / 2/3 / 1，竖向 0 -> 1 完全占满 ----
            SetColumn(modelList, 0f, 1f / 3f, true, false);
            SetColumn(tagModel, 1f / 3f, 2f / 3f, false, false);
            SetColumn(tagList, 2f / 3f, 1f, false, true);

            // ---- 4) 各自骨架 ----
            // ModelList：面板自己就是 ScrollRect（NewsController 找 ModelList/Viewport/Content）
            BuildPanel(modelList, "模板列表", null, true);

            // TagModel：写作区本身不滚动 —— 滚动条在内容预制体（NewsContent）里，
            //           这里只保留 TagModel/Scroll View/Viewport/Content 这串占位容器。
            BuildPanel(tagModel, "写作区", "Scroll View", false);

            // TagList：保留 "Options" 这一层（NewsController 找 Options/Viewport/Content）
            BuildPanel(tagList, "Tag 列表", "Options", true);

            // ---- 5) 提交 / 清空 搬进 TagModel 下半部分，竖向居中排列 ----
            MoveButtonsToTagModel(context.root, tagModel);

            // ---- 6) 清掉被搬空的中间壳（比如嵌套的 word 节点） ----
            RemoveEmptyShells(context, panels);

            return true;
        }

        /// <summary>
        /// 把面板钉在父节点的 [xMin, xMax] 这三等分区间里，
        /// 竖向 0 -> 1 完全占满，只留栏与栏之间的水平缝（上下不留白）。
        /// </summary>
        private static void SetColumn(RectTransform panel, float xMin, float xMax, bool isFirst, bool isLast)
        {
            panel.anchorMin = new Vector2(xMin, 0f);
            panel.anchorMax = new Vector2(xMax, 1f);
            panel.pivot = new Vector2(0.5f, 0.5f);

            // 先设锚点再设 offset：offset 的 setter 会按当前锚点回算 sizeDelta
            float left = isFirst ? ColumnGap : ColumnGap * 0.5f;
            float right = isLast ? ColumnGap : ColumnGap * 0.5f;

            panel.offsetMin = new Vector2(left, 0f);
            panel.offsetMax = new Vector2(-right, 0f);
        }

        /// <summary>
        /// 搭一个面板骨架：背景 + 标题 + 滚动区（Viewport / Content / 滚动条）。
        /// scrollRootName 为空 -> 面板自己当 ScrollRect（ModelList 的做法）
        /// scrollRootName 非空 -> 面板下再套一层同名节点当 ScrollRect（TagModel / TagList 兼容现有查找路径）
        /// listContent = true -> Content 挂 VerticalLayoutGroup + ContentSizeFitter（列表）
        /// listContent = false -> Content 四边拉伸、不加布局组（写作区交给 TagTextAdaptiveLayout）
        /// </summary>
        private static void BuildPanel(RectTransform panel, string titleText, string scrollRootName, bool listContent)
        {
            // ---- 背景 ----
            EnsurePanelBackground(panel);

            // ---- 标题 ----
            EnsurePanelTitle(panel, titleText);

            // ---- 滚动区根 ----
            bool rootIsPanel = string.IsNullOrEmpty(scrollRootName);
            RectTransform scrollRoot = rootIsPanel ? panel : EnsureChild(panel, scrollRootName);

            // 清掉滚动区里的旧内容（面板自己的背景 / 标题保留）
            for (int i = scrollRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = scrollRoot.GetChild(i);

                if (rootIsPanel && IsPanelDecoration(child.name))
                {
                    continue;
                }

                Object.DestroyImmediate(child.gameObject);
            }

            // 滚动区根：套一层的情况下，让它铺满面板并让出标题区
            float topInset = 0f;
            float bottomInset = 0f;
            float sideInset = 0f;

            if (!rootIsPanel)
            {
                scrollRoot.anchorMin = Vector2.zero;
                scrollRoot.anchorMax = Vector2.one;
                scrollRoot.pivot = new Vector2(0.5f, 0.5f);
                scrollRoot.offsetMin = new Vector2(ColPadding, ColPadding);
                scrollRoot.offsetMax = new Vector2(-ColPadding, -(ColPadding + ColTitleHeight + ColTitleGap));
            }
            else
            {
                // 面板自己当滚动区：标题和留白都在面板坐标系里让出来
                topInset = ColPadding + ColTitleHeight + ColTitleGap;
                bottomInset = ColPadding;
                sideInset = ColPadding;
            }

            // ================= 列表模式：ScrollRect + Viewport(遮罩) + Content(VLG+CSF) =================
            if (listContent)
            {
                // ---- ScrollRect + 可命中的 Graphic ----
                ScrollRect scroll = scrollRoot.GetComponent<ScrollRect>();

                if (scroll == null)
                {
                    scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
                }

                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.inertia = true;
                scroll.decelerationRate = 0.135f;
                scroll.scrollSensitivity = 30f;

                EnsureImage(scrollRoot);

                // ---- Viewport（遮罩） ----
                RectTransform viewport = EnsureChild(scrollRoot, "Viewport");
                StretchToParent(viewport, sideInset, bottomInset, ColScrollbarWidth, topInset);

                Image viewportImage = EnsureImage(viewport, 0.004f);
                viewportImage.raycastTarget = true;

                if (viewport.GetComponent<RectMask2D>() == null)
                {
                    viewport.gameObject.AddComponent<RectMask2D>();
                }

                // ---- Content：竖排 + 自适应高度 ----
                RectTransform content = EnsureChild(viewport, "Content");
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(1f, 1f);
                content.pivot = new Vector2(0.5f, 1f);
                content.anchoredPosition = Vector2.zero;
                content.sizeDelta = new Vector2(0f, ColCardHeight);

                VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();

                if (layout == null)
                {
                    layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
                }

                layout.padding = new RectOffset((int)ColPadding, (int)ColPadding, (int)ColPadding, (int)ColPadding);
                layout.spacing = ColCardSpacing;
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;

                ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();

                if (fitter == null)
                {
                    fitter = content.gameObject.AddComponent<ContentSizeFitter>();
                }

                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                // ---- 垂直滚动条 ----
                // 用 offset 精确对齐 Viewport 的上下边界（只设 sizeDelta 会因为竖向拉伸锚点而居中，对不齐）
                RectTransform scrollbar = EnsureChild(scrollRoot, "Scrollbar Vertical");
                scrollbar.anchorMin = new Vector2(1f, 0f);
                scrollbar.anchorMax = new Vector2(1f, 1f);
                scrollbar.pivot = new Vector2(1f, 0.5f);
                scrollbar.offsetMin = new Vector2(-ColScrollbarWidth, bottomInset);
                scrollbar.offsetMax = new Vector2(0f, -topInset);

                Image barBg = EnsureImage(scrollbar, 0.25f);
                barBg.color = new Color(0f, 0f, 0f, 0.25f);
                barBg.raycastTarget = true;

                Scrollbar bar = scrollbar.GetComponent<Scrollbar>();

                if (bar == null)
                {
                    bar = scrollbar.gameObject.AddComponent<Scrollbar>();
                }

                bar.direction = Scrollbar.Direction.BottomToTop;

                RectTransform slidingArea = EnsureChild(scrollbar, "Sliding Area");
                StretchToParent(slidingArea, 0f, 0f, 0f, 0f);

                RectTransform handle = EnsureChild(slidingArea, "Handle");
                StretchToParent(handle, 0f, 0f, 0f, 0f);

                Image handleImage = EnsureImage(handle, 0.7f);
                handleImage.color = new Color(1f, 1f, 1f, 0.7f);
                handleImage.raycastTarget = true;

                bar.handleRect = handle;
                bar.targetGraphic = handleImage;

                // ---- 接线 ----
                scroll.viewport = viewport;
                scroll.content = content;
                scroll.verticalScrollbar = bar;
                scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
                scroll.verticalScrollbarSpacing = 0f;
                scroll.verticalNormalizedPosition = 1f;

                Debug.Log($"[WritePanelTool] {panel.name} 列表骨架已重建（ScrollRect 在 {scrollRoot.name}）");
                return;
            }

            // ================= 写作区：和列表同一套滚动结构，但 Content 由模版自己排版 =================
            // Content 自己就是 TagTextTemplate —— 生成的固定文本 / 空缺直接挂在它下面，
            // 高度由 TagTextAdaptiveLayout 写进 LayoutElement.preferredHeight，ContentSizeFitter 跟着撑开。
            // 不能挂 LayoutGroup：段落是 TagSegmentLayout 按像素绝对定位的，LayoutGroup 会把它们重排掉。
            ScrollRect writingScroll = scrollRoot.GetComponent<ScrollRect>();

            if (writingScroll == null)
            {
                writingScroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            }

            writingScroll.horizontal = false;
            writingScroll.vertical = true;
            writingScroll.movementType = ScrollRect.MovementType.Clamped;
            writingScroll.inertia = true;
            writingScroll.decelerationRate = 0.135f;
            writingScroll.scrollSensitivity = 30f;

            EnsureImage(scrollRoot);

            RectTransform writingViewport = EnsureChild(scrollRoot, "Viewport");
            StretchToParent(writingViewport, sideInset, bottomInset, ColScrollbarWidth, topInset);

            Image writingViewportImage = EnsureImage(writingViewport, 0.004f);
            writingViewportImage.raycastTarget = true;

            if (writingViewport.GetComponent<RectMask2D>() == null)
            {
                writingViewport.gameObject.AddComponent<RectMask2D>();
            }

            RectTransform writingContent = EnsureChild(writingViewport, "Content");
            writingContent.anchorMin = new Vector2(0f, 1f);
            writingContent.anchorMax = new Vector2(1f, 1f);
            writingContent.pivot = new Vector2(0.5f, 1f);
            writingContent.anchoredPosition = Vector2.zero;
            writingContent.sizeDelta = new Vector2(0f, 300f);

            // 段落由 TagSegmentLayout 绝对定位，这里不能有布局组
            RemoveLayoutComponents(writingContent);

            ContentSizeFitter writingFitter = writingContent.GetComponent<ContentSizeFitter>();

            if (writingFitter == null)
            {
                writingFitter = writingContent.gameObject.AddComponent<ContentSizeFitter>();
            }

            writingFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            writingFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement writingElement = writingContent.GetComponent<LayoutElement>();

            if (writingElement == null)
            {
                writingElement = writingContent.gameObject.AddComponent<LayoutElement>();
            }

            writingElement.flexibleWidth = 1f;

            // Content 本身就是模版：文本 / 空缺直接挂在它下面（顺序不能反：Adaptive 依赖 Template）
            if (writingContent.GetComponent<TagTextTemplate>() == null)
            {
                writingContent.gameObject.AddComponent<TagTextTemplate>();
            }

            if (writingContent.GetComponent<TagTextAdaptiveLayout>() == null)
            {
                writingContent.gameObject.AddComponent<TagTextAdaptiveLayout>();
            }

            // ---- 垂直滚动条 ----
            RectTransform writingBar = EnsureChild(scrollRoot, "Scrollbar Vertical");
            writingBar.anchorMin = new Vector2(1f, 0f);
            writingBar.anchorMax = new Vector2(1f, 1f);
            writingBar.pivot = new Vector2(1f, 0.5f);
            writingBar.offsetMin = new Vector2(-ColScrollbarWidth, bottomInset);
            writingBar.offsetMax = new Vector2(0f, -topInset);

            Image writingBarBg = EnsureImage(writingBar, 0.2f);
            writingBarBg.color = new Color(0f, 0f, 0f, 0.2f);
            writingBarBg.raycastTarget = true;

            Scrollbar writingScrollbar = writingBar.GetComponent<Scrollbar>();

            if (writingScrollbar == null)
            {
                writingScrollbar = writingBar.gameObject.AddComponent<Scrollbar>();
            }

            writingScrollbar.direction = Scrollbar.Direction.BottomToTop;

            RectTransform writingSlidingArea = EnsureChild(writingBar, "Sliding Area");
            StretchToParent(writingSlidingArea, 0f, 0f, 0f, 0f);

            RectTransform writingHandle = EnsureChild(writingSlidingArea, "Handle");
            StretchToParent(writingHandle, 0f, 0f, 0f, 0f);

            Image writingHandleImage = EnsureImage(writingHandle, 0.6f);
            writingHandleImage.color = new Color(1f, 1f, 1f, 0.6f);
            writingHandleImage.raycastTarget = true;

            writingScrollbar.handleRect = writingHandle;
            writingScrollbar.targetGraphic = writingHandleImage;

            writingScroll.viewport = writingViewport;
            writingScroll.content = writingContent;
            writingScroll.verticalScrollbar = writingScrollbar;
            writingScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            writingScroll.verticalScrollbarSpacing = 0f;
            writingScroll.verticalNormalizedPosition = 1f;

            Debug.Log($"[WritePanelTool] {panel.name} 写作区骨架已重建（Content 自带 TagTextTemplate，独立滚动）");
        }

        /// <summary>面板背景：有 BackGround / BckGround 就拉满，没有就新建一个。</summary>
        private static void EnsurePanelBackground(RectTransform panel)
        {
            RectTransform background = null;

            for (int i = 0; i < panel.childCount; i++)
            {
                Transform child = panel.GetChild(i);

                if (IsBackgroundName(child.name))
                {
                    background = child as RectTransform;
                    break;
                }
            }

            if (background == null)
            {
                background = EnsureChild(panel, "BackGround");
            }

            StretchToParent(background, 0f, 0f, 0f, 0f);

            Image image = background.GetComponent<Image>();

            if (image == null)
            {
                image = background.gameObject.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0.06f);
            }

            image.raycastTarget = true;
            background.SetSiblingIndex(0);   // 垫在最底下
        }

        /// <summary>面板标题：没有就建，样式统一。</summary>
        private static void EnsurePanelTitle(RectTransform panel, string text)
        {
            RectTransform title = EnsureChild(panel, "Title");

            title.anchorMin = new Vector2(0f, 1f);
            title.anchorMax = new Vector2(1f, 1f);
            title.pivot = new Vector2(0.5f, 1f);
            title.anchoredPosition = Vector2.zero;
            title.sizeDelta = new Vector2(0f, ColTitleHeight);

            TextMeshProUGUI tmp = title.GetComponent<TextMeshProUGUI>();

            if (tmp == null)
            {
                tmp = title.gameObject.AddComponent<TextMeshProUGUI>();
            }

            tmp.text = text;
            tmp.fontSize = ColTitleFontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.black;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;

            TMP_FontAsset font = LoadDefaultFont();

            if (font != null)
            {
                tmp.font = font;
            }
        }

        /// <summary>
        /// 把提交 / 清空两个按钮搬进 TagModel，竖向堆叠、水平居中，整组中心落在
        /// TagModel 的 1/4 高处（下半部分的中间）。
        /// 按钮引用直接从 NewsController 的序列化字段读，避免误搬别的 Button。
        /// </summary>
        private static void MoveButtonsToTagModel(Transform root, RectTransform tagModel)
        {
            List<Button> buttons = new List<Button>();

            NewsController controller = root.GetComponentInChildren<NewsController>(true);

            if (controller != null)
            {
                SerializedObject serialized = new SerializedObject(controller);
                CollectButton(serialized, "mSubmitButton", buttons);
                CollectButton(serialized, "mResetButton", buttons);
            }

            if (buttons.Count == 0)
            {
                Debug.LogWarning("[WritePanelTool] NewsController 上没有接 mSubmitButton / mResetButton，" +
                                 "按钮保持原位（不会误搬其它 Button）");
                return;
            }

            const float buttonWidth = 180f;
            const float buttonHeight = 38f;
            const float spacing = 16f;

            // 整组竖向高度，用它把组中心对准锚点
            float groupHeight = buttons.Count * buttonHeight + (buttons.Count - 1) * spacing;
            float topOffset = groupHeight * 0.5f - buttonHeight * 0.5f;

            for (int i = 0; i < buttons.Count; i++)
            {
                RectTransform rect = (RectTransform)buttons[i].transform;

                rect.SetParent(tagModel, false);

                // 锚点钉在 TagModel 的 1/4 高处：下半部分的正中间
                rect.anchorMin = new Vector2(0.5f, 0.25f);
                rect.anchorMax = new Vector2(0.5f, 0.25f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(buttonWidth, buttonHeight);
                rect.anchoredPosition = new Vector2(0f, topOffset - i * (buttonHeight + spacing));

                // 画在 Scroll View 上面
                rect.SetAsLastSibling();
            }

            Debug.Log($"[WritePanelTool] 提交/清空已搬进 {tagModel.name} 下半部分：{buttons.Count} 个，竖向排列");
        }

        private static void CollectButton(SerializedObject serialized, string propertyName, List<Button> output)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);

            if (property == null)
            {
                return;
            }

            Button button = property.objectReferenceValue as Button;

            if (button != null && !output.Contains(button))
            {
                output.Add(button);
            }
        }

        /// <summary>清掉被搬空的中间壳节点（比如嵌套的 word），只处理"没子物体、没按钮、也不是三栏"的直接子节点。</summary>
        private static void RemoveEmptyShells(RectTransform context, RectTransform[] keep)
        {
            for (int i = context.childCount - 1; i >= 0; i--)
            {
                RectTransform child = context.GetChild(i) as RectTransform;

                if (child == null || System.Array.IndexOf(keep, child) >= 0)
                {
                    continue;
                }

                if (child.childCount > 0 || child.GetComponent<Button>() != null)
                {
                    continue;
                }

                Debug.Log($"[WritePanelTool] 清理空壳节点：{child.name}");
                Object.DestroyImmediate(child.gameObject);
            }
        }

        // ---- 小工具 ----

        private static bool IsBackgroundName(string name)
        {
            return name == "BackGround" || name == "BckGround" || name == "Background";
        }

        private static bool IsPanelDecoration(string name)
        {
            return IsBackgroundName(name) || name == "Title";
        }

        private static void StretchToParent(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static Image EnsureImage(RectTransform rect, float fallbackAlpha = 0.004f)
        {
            Image image = rect.GetComponent<Image>();

            if (image == null)
            {
                image = rect.gameObject.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, fallbackAlpha);
            }
            else if (image.color.a <= 0.001f)
            {
                image.color = new Color(1f, 1f, 1f, fallbackAlpha);
            }

            image.raycastTarget = true;
            return image;
        }

        private static void RemoveLayoutComponents(RectTransform rect)
        {
            LayoutGroup group = rect.GetComponent<LayoutGroup>();

            if (group != null)
            {
                Object.DestroyImmediate(group);
            }

            ContentSizeFitter fitter = rect.GetComponent<ContentSizeFitter>();

            if (fitter != null)
            {
                Object.DestroyImmediate(fitter);
            }
        }

        private static RectTransform FindRect(Transform root, string name)
        {
            Transform hit = FindByName(root, name);
            return hit as RectTransform;
        }

        // ============================================================
        //  工具
        // ============================================================

        private static RectTransform EnsureChild(RectTransform parent, string name)
        {
            Transform existing = parent.Find(name);

            if (existing != null)
            {
                return existing as RectTransform;
            }

            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static Transform FindByName(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform hit = FindByName(root.GetChild(i), name);

                if (hit != null)
                {
                    return hit;
                }
            }

            return null;
        }
    }
}
