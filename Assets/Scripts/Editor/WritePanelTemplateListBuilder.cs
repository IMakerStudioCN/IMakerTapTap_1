using System.Collections.Generic;
using TapTapFirst;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TapTapFirst.EditorTools
{
    /// <summary>
    /// 【临时工具】给 WritePanel.prefab 的 ModelList 节点搭一个竖版模板列表（带垂直滚动条）。
    /// 参考 TagModel 写作区的 ScrollRect 结构。
    ///
    /// 菜单：Tools/WritePanel/生成 ModelList 模板列表
    /// 跑完、确认 prefab 效果后，这个脚本可以直接删掉。
    /// </summary>
    public static class WritePanelTemplateListBuilder
    {
        private const string PrefabPath = "Assets/Art/ui/UIPrefab/WritePanel.prefab";

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

            TextMeshProUGUI label = CreateTextNode(rect, "Title", "模板标题", 28f, Color.white);
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
                // newsContent 留空：写作区就是 prefab 里那个 TagTextTemplate，
                // 当前 NewsController 不实例化它。真要用请指到专门的模版 prefab，
                // 千万别指向 WritePanel 自己（会循环引用）。

                EditorUtility.SetDirty(asset);
                created.Add(asset);
            }

            return created;
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
            titleText.color = Color.white;
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
            text.color = Color.white;
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
