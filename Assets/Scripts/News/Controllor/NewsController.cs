using QFramework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TapTapFirst
{
    /// <summary>
    /// 写作界面控制器。
    ///
    /// 职责：
    ///   1. 点选模板：把内容预制体（ResKit 资源名）实例化到写作区，并按 NewsTemplate_SO 的配置生成文本与空缺
    ///   2. 给模版里的所有空缺铺上可拖拽的 Tag（从 Tag_List_SO 取全部可用 Tag）
    ///   3. 玩家把 Tag 填进空缺后，把"补全的完整文本"按文本顺序存档
    ///
    /// 存档 key 用 newsId，所以每条新闻各存一份，互不干扰。
    /// 依赖：TagTextTemplate / TagKindSlot / TagOption（Assets/Scripts/Tag）
    /// 存档：一律发命令给 INewsSystem（SaveWrittenNewsCommand / SaveNewsDraftCommand / ClearNewsDraftCommand），本类不碰 IJsonSaveUtility
    /// </summary>
    public class NewsController : MonoBehaviour, IController
    {
        // ============================================================
        //  模版与列表
        // ============================================================

        [Header("通用配置")]
        [Tooltip("点选模板时是否清空之前的填写内容")]
        [SerializeField] private bool mClearOnOpen = true;

        [Tooltip("没填的空缺在预览/导出文本里显示成什么")]
        [SerializeField] private string mEmptyPlaceholder = "____";

        // ============================================================
        //  界面显示
        // ============================================================

        [Header("界面显示")]
        [SerializeField] private TMP_Text mTitleText;
        [SerializeField] private TMP_Text mProgressText;

        [Tooltip("预览：由玩家填写内容拼出的完整文本")]
        [SerializeField] private TMP_Text mPreviewText;

        // ============================================================
        //  资源名（全部走 ResKit，需给对应预制体打 AssetBundle 标签）
        // ============================================================

        [Header("资源名（ResKit）")]
        [Tooltip("写作内容预制体资源名（根节点带 TagTextTemplate）")]
        [SerializeField] private string mContentResName = "NewsContent";

        [Tooltip("模板卡片预制体资源名（根节点带 TemplateCardItem）")]
        [SerializeField] private string mTemplateCardResName = "TemplateCard";

        [Tooltip("Tag 卡片预制体资源名（根节点带 TagOption）；也用作空缺里的显示件")]
        [SerializeField] private string mTagOptionResName = "TagOption";

        [Tooltip("新闻总表资源名，对应 Assets/Config/News/NewsTemplateList.asset")]
        [SerializeField] private string mNewsListResName = "NewsTemplateList";

        [Tooltip("Tag 总表资源名，对应 Assets/Config/Tag/Tag_List_SO")]
        [SerializeField] private string mTagListResName = "Tag_List_SO";

        // ============================================================
        //  容器
        // ============================================================

        [Header("容器（留空自动按名字找）")]
        [Tooltip("模板卡片容器：ModelList/Viewport/Content")]
        [SerializeField] private RectTransform mTemplateListRoot;

        [Tooltip("Tag 选项容器：TagLists/TagList/Options/Viewport/Content")]
        [SerializeField] private RectTransform mTagListRoot;

        [Tooltip("内容预制体的父节点：TagModel/Scroll View/Viewport/Content")]
        [SerializeField] private RectTransform mContentParent;

        // ============================================================
        //  行为开关
        // ============================================================

        [Header("行为")]
        [Tooltip("OnEnable 时自动铺模板列表")]
        [SerializeField] private bool mRebuildTemplateListOnEnable = true;

        [Tooltip("打开界面时自动选中第一条模板")]
        [SerializeField] private bool mSelectFirstTemplateOnOpen = true;

        [Tooltip("打开界面时自动铺满 Tag 列表")]
        [SerializeField] private bool mAutoSpawnTagOptions = true;

        [Tooltip("切换模板时销毁上一次实例化的内容")]
        [SerializeField] private bool mDestroyOldContentOnSwitch = true;

        [Header("日志")]
        [SerializeField] private bool mLog;

        private TagTextTemplate mContentInstance;

        private readonly List<TemplateCardItem> mCards = new List<TemplateCardItem>();

        // ============================================================
        //  按钮
        // ============================================================

        [Header("按钮（可选，脚本里也能直接调对应方法）")]
        [SerializeField] private Button mSubmitButton;
        [SerializeField] private Button mResetButton;
        [SerializeField] private Button mCloseButton;

        // ============================================================
        //  运行时
        // ============================================================

        private readonly ResLoader mResLoader = ResLoader.Allocate();

        /// <summary>当前打开的新闻配置。</summary>
        public NewsTemplate_SO CurrentNews { get; private set; }

        /// <summary>当前实例化出来的模版（写作区）。</summary>
        public TagTextTemplate CurrentTemplate { get; private set; }

        /// <summary>当前是否已经打开了一条新闻。</summary>
        public bool IsOpen => CurrentNews != null && CurrentTemplate != null;

        /// <summary>存档 key：每条新闻一份。</summary>
        public string SaveKey => CurrentNews != null ? CurrentNews.newsId.ToString() : null;

        private INewsSystem NewsSystem => this.GetSystem<INewsSystem>();

        private ITagSystem TagSystem => this.GetSystem<ITagSystem>();

        





        // ============================================================
        //  生命周期
        // ============================================================

        private void OnInit()
        {
            UIKit.OpenPanel("NewsPanel", UILevel.Common);
        }

        

        private void Awake()
        {
            if (mSubmitButton != null)
            {
                mSubmitButton.onClick.AddListener(OnSubmitClicked);
            }

            if (mResetButton != null)
            {
                mResetButton.onClick.AddListener(ResetContent);
            }

            if (mCloseButton != null)
            {
                mCloseButton.onClick.AddListener(Close);
            }
        }

        private void OnDestroy()
        {
            if (mSubmitButton != null)
            {
                mSubmitButton.onClick.RemoveListener(OnSubmitClicked);
            }

            if (mResetButton != null)
            {
                mResetButton.onClick.RemoveListener(ResetContent);
            }

            if (mCloseButton != null)
            {
                mCloseButton.onClick.RemoveListener(Close);
            }

            if (CurrentTemplate != null)
            {
                CurrentTemplate.SegmentsChanged -= RefreshUI;
            }

            mResLoader.Dispose();
        }

        private void OnEnable()
        {
            // Start 只跑一次；Mini 隐藏后再打开会走这里（手册 §2.3 模板 B）
            ResolveTemplateListRoot();
            ResolveTagListRoot();

            if (mAutoSpawnTagOptions)
            {
                SpawnTagOptions();
            }

            if (mRebuildTemplateListOnEnable)
            {
                RebuildTemplateList();
            }
            else
            {
                RefreshTemplateCards();
            }

            if (mSelectFirstTemplateOnOpen && CurrentNews == null && mCards.Count > 0)
            {
                SelectTemplate(mCards[0].NewsId);
            }

            // 没走到 SelectTemplate 时按钮状态也要算一次
            RefreshUI();
        }

        private void OnDisable()
        {
            // 面板被隐藏 / 关掉时也留一份草稿（不只 Close() 那条路）
            if (!Application.isPlaying)
            {
                return;
            }

            try
            {
                SaveDraft();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[NewsController] 关闭时存草稿失败：{e.Message}");
            }
        }
        // ============================================================
        //  模板列表
        // ============================================================

        /// <summary>按总表铺左侧模板列表（没有预制体时复用生成器已摆好的卡片）。</summary>
        public void RebuildTemplateList()
        {
            mCards.Clear();

            if (mTemplateListRoot == null)
            {
                ResolveTemplateListRoot();
            }

            if (mTemplateListRoot == null)
            {
                Debug.LogWarning("[NewsController] 找不到模板列表容器（ModelList/Viewport/Content）");
                return;
            }

            List<NewsTemplate_SO> templates = LoadTemplates();

            if (templates == null || templates.Count == 0)
            {
                Debug.LogWarning($"[NewsController] 取不到模板总表 {mNewsListResName}，模板列表为空");
                return;
            }

            // 只清理"卡片"，保留生成器摆好的标题 / 背景等装饰
            for (int i = mTemplateListRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = mTemplateListRoot.GetChild(i);

                if (child.GetComponent<TemplateCardItem>() != null)
                {
                    Destroy(child.gameObject);
                }
            }

            foreach (NewsTemplate_SO news in templates)
            {
                if (news == null)
                {
                    continue;
                }

                TemplateCardItem card = null;

                GameObject cardPrefab = LoadPrefab(mTemplateCardResName);

                if (cardPrefab != null)
                {
                    GameObject go = Instantiate(cardPrefab, mTemplateListRoot);
                    go.name = $"Template_{news.newsId}_{news.newsTitle}";
                    go.SetActive(true);
                    card = go.GetComponent<TemplateCardItem>();

                    if (card == null)
                    {
                        card = go.GetComponentInChildren<TemplateCardItem>(true);
                    }

                    if (card == null)
                    {
                        card = go.AddComponent<TemplateCardItem>();
                    }
                }

                if (card == null)
                {
                    continue;
                }

                card.Setup(news);
                card.OnSelected -= OnTemplateCardSelected;
                card.OnSelected += OnTemplateCardSelected;
                card.SetSelected(CurrentNews != null && CurrentNews.newsId == news.newsId);
                mCards.Add(card);
            }

            Debug.Log($"[NewsController] 模板列表已铺：{mCards.Count} 张卡片");
        }

        private void OnTemplateCardSelected(int newsId)
        {
            SelectTemplate(newsId);
        }

        /// <summary>切换当前写作的模板（刷标题、卡片选中态，并读回该模板的存档）。</summary>
        public void SelectTemplate(int newsId)
        {
            NewsTemplate_SO news = NewsSystem != null ? NewsSystem.GetConfig(newsId) : null;

            if (news == null)
            {
                Debug.LogWarning($"[NewsController] 找不到 newsId={newsId} 的新闻配置（检查 Assets/Config/News）");
                return;
            }

            // 切走之前把当前这条的草稿存了（没提交过的内容也能接着写）
            if (CurrentNews != null && CurrentTemplate != null && CurrentNews.newsId != newsId)
            {
                SaveDraft();
            }

            CurrentNews = news;

            if (mTitleText != null)
            {
                // 卡片显示文字优先（cardTitleOverride），没有就用 newsTitle
                mTitleText.text = news.CardTitle;
            }

            RefreshTemplateCards();

            // 按 SO 配置生成写作区内容
            ApplyContentFromConfig(news);

            // 切模板：清空写作区，再读回该模板自己的存档
            if (CurrentTemplate != null && mClearOnOpen)
            {
                CurrentTemplate.ClearAll();
            }

            LoadDocument();
            RefreshUI();
        }

        // ============================================================
        //  资源加载（统一 ResKit）
        // ============================================================

        /// <summary>
        /// 按资源名加载预制体（ResKit）。
        /// 资源名 = 预制体文件名，且该预制体必须打过 AssetBundle 标签。
        /// </summary>
        public GameObject LoadPrefab(string resName)
        {
            if (string.IsNullOrEmpty(resName))
            {
                Debug.LogError("[NewsController] 资源名为空，无法加载预制体");
                return null;
            }

            try
            {
                GameObject prefab = mResLoader.LoadSync<GameObject>(resName);

                if (prefab == null)
                {
                    Debug.LogError($"[NewsController] 加载不到预制体 [{resName}]。" +
                                   $"请检查：1) 资源名是否等于预制体文件名；2) 该预制体是否打过 AssetBundle 标签；" +
                                   $"3) 编辑器里若是 AB 模式需重新打一次包");
                }

                return prefab;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[NewsController] 加载预制体 [{resName}] 抛异常：{e.Message}");
                return null;
            }
        }

        /// <summary>取这条新闻该用哪个内容预制体资源名：自己的优先，否则用默认的。</summary>
        public string ResolveContentResName(NewsTemplate_SO news)
        {
            if (news != null && !string.IsNullOrEmpty(news.contentResName))
            {
                return news.contentResName;
            }

            return mContentResName;
        }

        /// <summary>
        /// 按 NewsTemplate_SO 的配置生成写作区：
        ///   1. 按资源名（ResKit）实例化内容预制体到 mContentParent
        ///   2. 按 contentSegments 生成「固定文本 + 空缺」
        /// 没有配置片段时，只切换模版、不改内容。
        /// </summary>
        public void ApplyContentFromConfig(NewsTemplate_SO news)
        {
            if (news == null)
            {
                return;
            }

            // 1) 换内容实例（强制用预制体）：优先用这条新闻自己的资源名，其次用 mContentResName
            if (mContentInstance == null || mDestroyOldContentOnSwitch)
            {
                InstantiateContentInstance(ResolveContentResName(news));
            }

            TagTextTemplate target = mContentInstance;

            if (target == null)
            {
                Debug.LogError($"[NewsController] 内容预制体没加载成功，无法生成写作区。请检查资源名 " +
                               $"[{ResolveContentResName(news)}] 的预制体是否存在、是否打过 AssetBundle 标签");
                return;
            }

            // 让 Controller 真正握住当前模版 —— 提交 / 清空 / 读档全靠它
            SetCurrentTemplate(target);

            // 2) 按配置生成片段
            if (!news.HasContent)
            {
                return;
            }

            // 空缺里显示 Tag 直接复用列表卡片预制体（TagOptionCard）
            target.Configure(news.BuildSequence(), LoadPrefab(mTagOptionResName));

            Debug.Log($"[NewsController] 已按配置生成写作区：{news.newsTitle}（{news.contentSegments.Count} 段 / {news.SlotCount} 个空缺）");
        }

        /// <summary>
        /// 切换 Controller 手里的当前模版，并同步 SegmentsChanged 的订阅。
        /// 之前这里一直是 null，导致 IsOpen 永远 false —— 提交 / 清空 / 读档全部失效。
        /// </summary>
        private void SetCurrentTemplate(TagTextTemplate template)
        {
            if (CurrentTemplate == template)
            {
                return;
            }

            if (CurrentTemplate != null)
            {
                CurrentTemplate.SegmentsChanged -= RefreshUI;
            }

            CurrentTemplate = template;

            if (CurrentTemplate != null)
            {
                CurrentTemplate.SegmentsChanged += RefreshUI;
            }
        }

        /// <summary>实例化内容预制体（换预制体时先销毁旧的）。</summary>
        /// <summary>实例化内容预制体（走 ResKit；换预制体时先销毁旧的）。</summary>
        private void InstantiateContentInstance(string resName)
        {
            GameObject source = LoadPrefab(resName);

            if (source == null)
            {
                return;
            }

            // 换预制体时先销毁旧的（先把 Controller 手里的引用摘掉，免得留着已销毁对象）
            if (mContentInstance != null)
            {
                SetCurrentTemplate(null);
                Destroy(mContentInstance.gameObject);
                mContentInstance = null;
            }

            Transform parent = ResolveContentParent();

            if (parent == null)
            {
                Debug.LogError("[NewsController] 找不到内容父节点（TagModel/Scroll View/Viewport/Content）");
                return;
            }

            GameObject go = Instantiate(source, parent);
            go.name = source.name;
            go.SetActive(true);

            RectTransform rect = go.transform as RectTransform;

            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
            }

            mContentInstance = go.GetComponent<TagTextTemplate>();

            if (mContentInstance == null)
            {
                mContentInstance = go.GetComponentInChildren<TagTextTemplate>(true);
            }
        }

        /// <summary>销毁上一次实例化的内容。</summary>
        public void DestroyContentInstance()
        {
            SetCurrentTemplate(null);

            if (mContentInstance == null)
            {
                return;
            }

            Destroy(mContentInstance.gameObject);
            mContentInstance = null;
        }

        /// <summary>找内容父节点：TagModel/Scroll View/Viewport/Content。</summary>
        private Transform ResolveContentParent()
        {
            if (mContentParent != null)
            {
                return mContentParent;
            }

            RectTransform self = transform as RectTransform;

            if (self == null)
            {
                return null;
            }

            RectTransform tagModel = WindowsUtility.FindChildByName(self, "TagModel");

            if (tagModel == null)
            {
                return null;
            }

            RectTransform scrollView = WindowsUtility.FindChildByName(tagModel, "Scroll View");

            if (scrollView == null)
            {
                return tagModel;
            }

            RectTransform viewport = WindowsUtility.FindChildByName(scrollView, "Viewport");
            RectTransform content = viewport != null ? WindowsUtility.FindChildByName(viewport, "Content") : null;

            if (content != null)
            {
                mContentParent = content;
            }
            else
            {
                mContentParent = scrollView;
            }

            return mContentParent;
        }

        /// <summary>刷新卡片选中态。</summary>
        public void RefreshTemplateCards()
        {
            foreach (TemplateCardItem card in mCards)
            {
                if (card != null)
                {
                    card.SetSelected(CurrentNews != null && card.NewsId == CurrentNews.newsId);
                }
            }
        }

        private void ResolveTemplateListRoot()
        {
            if (mTemplateListRoot != null)
            {
                return;
            }

            RectTransform self = transform as RectTransform;

            if (self == null)
            {
                return;
            }

            RectTransform modelList = WindowsUtility.FindChildByName(self, "ModelList");

            if (modelList == null)
            {
                return;
            }

            // ModelList/Viewport/Content 才是容器；没铺过列表就退回 ModelList 本身
            RectTransform viewport = WindowsUtility.FindChildByName(modelList, "Viewport");
            RectTransform content = viewport != null
                ? WindowsUtility.FindChildByName(viewport, "Content")
                : null;

            mTemplateListRoot = content != null ? content : modelList;
        }

        /// <summary>
        /// 找 Tag 选项容器：TagLists/TagList/Options/Viewport/Content。
        /// 注意：没有它 SpawnTagOptions 会直接 return，表现为"Tag 列表永远是空的"。
        /// </summary>
        private void ResolveTagListRoot()
        {
            if (mTagListRoot != null)
            {
                return;
            }

            RectTransform self = transform as RectTransform;

            if (self == null)
            {
                return;
            }

            RectTransform options = WindowsUtility.FindChildByName(self, "Options");

            if (options == null)
            {
                Debug.LogWarning("[NewsController] 找不到 Tag 列表容器（TagLists/TagList/Options），Tag 列表不会显示");
                return;
            }

            // Options/Viewport/Content 才是真正装选项的容器
            RectTransform viewport = WindowsUtility.FindChildByName(options, "Viewport");
            RectTransform content = viewport != null
                ? WindowsUtility.FindChildByName(viewport, "Content")
                : null;

            mTagListRoot = content != null ? content : options;
        }

        /// <summary>
        /// 模板列表的唯一数据源：NewsSystem 的"已获得新闻模板列表"。
        /// 不再回退整张总表 —— 没获得的就不该出现在列表里。
        /// </summary>
        private List<NewsTemplate_SO> LoadTemplates()
        {
            INewsSystem newsSystem = NewsSystem;

            if (newsSystem == null)
            {
                Debug.LogError("[NewsController] 拿不到 INewsSystem，请检查 TapTap.Init() 是否注册了 NewsSystem");
                return null;
            }

            return newsSystem.GetAcquiredNewsList();
        }

        // ============================================================
        //  打开 / 关闭
        // ============================================================


        /// <summary>关闭写作界面（先把当前内容存成草稿，再清掉实例化出来的模版）。</summary>
        public void Close()
        {
            // 关之前留一份草稿，下次打开能接着写
            SaveDraft();

            ClearContentInstance();

            CurrentNews = null;

            RefreshUI();
        }

        private void ClearContentInstance()
        {
            TagTextTemplate template = CurrentTemplate != null ? CurrentTemplate : mContentInstance;

            SetCurrentTemplate(null);
            mContentInstance = null;

            if (template != null)
            {
                Destroy(template.gameObject);
            }
        }

        // ============================================================
        //  Tag 列表
        // ============================================================

        /// <summary>清空并按【TagSystem 已获得的 Tag】铺满选项。</summary>
        public void SpawnTagOptions()
        {
            GameObject optionPrefab = LoadPrefab(mTagOptionResName);

            if (mTagListRoot == null || optionPrefab == null)
            {
                Debug.LogError($"[NewsController] Tag 列表没铺：容器={(mTagListRoot != null ? mTagListRoot.name : "null")}，预制体={(optionPrefab != null ? optionPrefab.name : "null")}");
                return;
            }

            for (int i = mTagListRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(mTagListRoot.GetChild(i).gameObject);
            }

            List<Tag_SO> acquiredTags = LoadTagOptions();

            if (acquiredTags == null || acquiredTags.Count == 0)
            {
                Debug.LogWarning("[NewsController] TagSystem 里还没有已获得的 Tag，Tag 列表为空（用 AddTagCommand 获取）");
                return;
            }

            foreach (Tag_SO tag in acquiredTags)
            {
                if (tag == null)
                {
                    continue;
                }

                GameObject go = Instantiate(optionPrefab, mTagListRoot);
                go.name = $"Option_{tag.tagName}";
                go.SetActive(true);

                TagOption option = go.GetComponent<TagOption>();

                if (option == null)
                {
                    option = go.GetComponentInChildren<TagOption>(true);
                }

                if (option != null)
                {
                    option.Setup(tag);
                }
            }
        }

        /// <summary>
        /// Tag 列表的唯一数据源：TagSystem 的"已获得 Tag 列表"。
        /// 不再回退整张 Tag_List_SO —— 没获得的就不该出现在列表里。
        /// </summary>
        private List<Tag_SO> LoadTagOptions()
        {
            ITagSystem tagSystem = TagSystem;

            if (tagSystem == null)
            {
                Debug.LogError("[NewsController] 拿不到 ITagSystem，请检查 TapTap.Init() 是否注册了 TagSystem");
                return null;
            }

            return tagSystem.GetAcquiredTagList();
        }

        // ============================================================
        //  存档
        // ============================================================

        /// <summary>按钮回调（Button.onClick 需要 void）。</summary>
        public void OnSubmitClicked()
        {
            if (Submit())
            {
                Debug.Log($"[NewsController] 提交成功：\n{GetResultContent(CurrentNews.newsId)}");
            }

            RefreshUI();
        }



        /// <summary>提交：存档 + 结算（等价 Submit(out _)）。</summary>
        public bool Submit()
        {
            return Submit(out _);
        }

        /// <summary>
        /// 提交：把当前排版序列化成 Json，发命令给 NewsSystem 保存。
        /// 结算（正文 + 所有 Tag 数值合计 + 模版倾向及其数值）由 NewsSystem 做，
        /// 存完顺带清掉草稿，之后用 GetResult(newsId) 取。
        /// </summary>
        public bool Submit(out NewsResultData result)
        {
            result = null;

            if (!IsOpen)
            {
                Debug.LogWarning("[NewsController] 还没有打开任何新闻，无法提交");
                return false;
            }

            // 没填满不给提交，避免把半成品当成"写好的新闻"存进 NewsSystem
            if (!CurrentTemplate.IsAllSlotsFilled)
            {
                Debug.LogWarning($"[NewsController] 新闻 {CurrentNews.newsId}({CurrentNews.newsTitle}) 还有空缺没填满，不能提交");
                RefreshUI();
                return false;
            }

            // 只负责把当前排版交出去，存哪儿、怎么结算都是 NewsSystem 的事
            string json = CurrentTemplate.SaveToJson();
            result = this.SendCommand(new SaveWrittenNewsCommand(CurrentNews.newsId, json));

            if (result == null)
            {
                Debug.LogError($"[NewsController] 新闻 {CurrentNews.newsId} 提交失败");
                return false;
            }
            Debug.Log($"[NewsController] 新闻 {CurrentNews.newsId}({CurrentNews.newsTitle}) 已提交\n{CurrentTemplate.GetPlainText()}");
            return true;
        }

        /// <summary>
        /// 还原这条新闻的写作区：
        ///   1) 优先用草稿（玩家正在写的，或清空后留下的"全空"文档）
        ///   2) 没有草稿但已经提交过，就用结算结果里的片段还原
        /// </summary>
        public bool LoadDocument()
        {
            if (!IsOpen)
            {
                return false;
            }

            TagFillDocument document = NewsSystem.GetDraftDocument(CurrentNews.newsId);

            if (document == null)
            {
                document = NewsSystem.GetWrittenDocument(CurrentNews.newsId);
            }

            if (document == null)
            {
                return false;
            }

            return CurrentTemplate.ApplyDocument(document);
        }

        /// <summary>
        /// 清空所有空缺。
        /// 顺手把"全空"存成草稿：否则切走再切回来，LoadDocument 会回退到已提交的内容，Tag 又活了。
        /// </summary>
        public void ResetContent()
        {
            if (CurrentTemplate == null)
            {
                return;
            }

            CurrentTemplate.ClearAll();

            if (CurrentNews != null)
            {
                SaveDraft();
            }

            RefreshUI();
        }

        /// <summary>发命令删掉当前这条新闻的草稿。</summary>
        public bool ClearDraft()
        {
            if (CurrentNews == null)
            {
                return false;
            }

            return this.SendCommand(new ClearNewsDraftCommand(CurrentNews.newsId));
        }

        /// <summary>发命令把当前排版存成草稿。</summary>
        public bool SaveDraft()
        {
            if (!IsOpen)
            {
                return false;
            }

            string json = CurrentTemplate.SaveToJson();
            return this.SendCommand(new SaveNewsDraftCommand(CurrentNews.newsId, json));
        }

        /// <summary>当前补全的完整文本（空位用模版里的占位符）。</summary>
        public string GetComposedText()
        {
            return CurrentTemplate != null ? CurrentTemplate.GetPlainText() : string.Empty;
        }

        /// <summary>草稿里的完整文本（不依赖当前是否打开）。</summary>
        public string GetSavedText(int newsId)
        {
            TagFillDocument document = NewsSystem.GetDraftDocument(newsId);
            return document != null ? document.ToPlainText(mEmptyPlaceholder) : null;
        }

        // ============================================================
        //  获取：写好的新闻
        //    （拼好的正文 + 所有 Tag 数值合计 + 模版类型及其数值）
        //    数据都在 NewsSystem 里，这里只是转手，不碰存档
        // ============================================================

        /// <summary>结算当前编辑中的这条新闻（只算不存）。</summary>
        public NewsResultData BuildResult()
        {
            if (!IsOpen)
            {
                return null;
            }

            return NewsResultBuilder.Build(CurrentNews, CurrentTemplate, TagSystem);
        }

        /// <summary>取某篇写好的新闻；没写过返回 null。</summary>
        public NewsResultData GetResult(int newsId) => NewsSystem.GetWrittenNews(newsId);

        /// <summary>取某篇写好的新闻。</summary>
        public bool TryGetResult(int newsId, out NewsResultData result) =>
            NewsSystem.TryGetWrittenNews(newsId, out result);

        /// <summary>当前打开这条新闻的结果（等价 GetResult(CurrentNews.newsId)）。</summary>
        public NewsResultData GetCurrentResult() =>
            CurrentNews != null ? GetResult(CurrentNews.newsId) : null;

        /// <summary>这篇新闻写好了没有。</summary>
        public bool HasResult(int newsId) => NewsSystem.HasWrittenNews(newsId);

        /// <summary>所有写好的新闻的 Id。</summary>
        public List<int> GetAllResultNewsIds() => NewsSystem.GetWrittenNewsIds();

        /// <summary>所有写好的新闻。</summary>
        public List<NewsResultData> GetAllResults() => NewsSystem.GetWrittenNewsList();

        /// <summary>写好的新闻数量。</summary>
        public int ResultCount => NewsSystem.WrittenNewsCount;

        /// <summary>某篇新闻里【所有 Tag 的该类型数值加起来】的总值。</summary>
        public float GetResultTagValueTotal(int newsId, TagValueType type) =>
            NewsSystem.GetWrittenTagValueTotal(newsId, type);

        /// <summary>某篇新闻里【所有 Tag 的所有数值加起来】的总值。</summary>
        public float GetResultTagValueTotal(int newsId) => NewsSystem.GetWrittenTagValueTotal(newsId);

        /// <summary>某篇新闻用的模版类型（客观 / 正面 / 负面）。</summary>
        public NewsEffectType GetResultEffectType(int newsId) => NewsSystem.GetWrittenEffectType(newsId);

        /// <summary>某篇新闻用的模版类型对应的数值。</summary>
        public float GetResultEffectValue(int newsId) => NewsSystem.GetWrittenEffectValue(newsId);

        /// <summary>某篇新闻拼好的正文。</summary>
        public string GetResultContent(int newsId) => NewsSystem.GetWrittenContent(newsId);

        // ---------------- 全部写好的新闻的合计（NewsSystem 已经算好） ----------------

        /// <summary>所有写好的新闻里，Tag 数值按类型加起来的总值。</summary>
        public Dictionary<TagValueType, float> GetTotalTagValues() => NewsSystem.GetTotalTagValues();

        /// <summary>所有写好的新闻里，Tag 的所有类型数值再加一起。</summary>
        public float GetTotalTagValue() => NewsSystem.GetTotalTagValue();

        /// <summary>所有写好的新闻里，按模版倾向分类的数值合计。</summary>
        public Dictionary<NewsEffectType, float> GetTotalEffectValues() => NewsSystem.GetTotalEffectValues();

        /// <summary>所有写好的新闻里，按模版倾向分类的篇数。</summary>
        public Dictionary<NewsEffectType, int> GetEffectNewsCounts() => NewsSystem.GetEffectNewsCounts();


        // ============================================================
        //  界面刷新
        // ============================================================

        /// <summary>按当前状态刷新标题 / 进度 / 预览 / 按钮可用性。</summary>
        public void RefreshUI()
        {
            if (mTitleText != null)
            {
                mTitleText.text = CurrentNews != null ? CurrentNews.newsTitle : string.Empty;
            }

            bool open = IsOpen;
            TagFillProgress progress = open ? CurrentTemplate.GetProgress() : null;

            if (mProgressText != null)
            {
                mProgressText.text = open ? $"{progress.filled}/{progress.total}" : string.Empty;
            }

            if (mPreviewText != null)
            {
                mPreviewText.text = open ? CurrentTemplate.GetPlainText() : string.Empty;
            }

            // 提交要求所有空缺都填满（没有空缺的模版不给提交）
            bool canSubmit = open && progress != null && progress.total > 0 && progress.filled >= progress.total;

            if (mSubmitButton != null)
            {
                mSubmitButton.interactable = canSubmit;
            }

            if (mResetButton != null)
            {
                mResetButton.interactable = open && progress != null && progress.filled > 0;
            }
        }

        

        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }
    }
}