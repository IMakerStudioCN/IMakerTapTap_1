using QFramework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TapTapFirst
{
    public partial class Test : ViewController, IController
    {
        // ============================================================
        //  临时测试开关（测完把 mShowDebugUi 关掉或删掉本区块）
        // ============================================================

        [Header("临时测试 UI（动态生成）")]
        [Tooltip("显示获得 Tag / 新闻模板的测试按钮")]
        [SerializeField] private bool mShowDebugUi = true;

        [Tooltip("测试面板左上角位置")]
        [SerializeField] private Vector2 mDebugUiOrigin = new Vector2(20f, 20f);

        [Header("资源名（与 NewsController 保持一致）")]
        [SerializeField] private string mNewsListResName = "NewsTemplateList";
        [SerializeField] private string mTagListResName = "Tag_List_SO";

        private readonly ResLoader mDebugResLoader = ResLoader.Allocate();

        private INewsSystem NewsSystem => this.GetSystem<INewsSystem>();
        private ITagSystem TagSystem => this.GetSystem<ITagSystem>();

        private Vector2 mNewsScroll;
        private Vector2 mTagScroll;
        private string mDebugMessage = "";

        void Start()
        {
            UIKit.OpenPanel<ComputerDisplay>();
            //UIKit.OpenPanel<WindowsSample>();
        }

        private void OnDestroy()
        {
            mDebugResLoader.Dispose();
        }

        // ============================================================
        //  临时测试 UI
        // ============================================================
        //  临时测试 UI
        //
        //  注意两件事（都是曾经踩过的坑）：
        //   1. 绝对不能在 OnGUI 里每帧 LoadSync 资源 —— 会不停申请 ResKit 句柄，
        //      而且 GUI 绘制过程中触发资源加载/导入，编辑器会报
        //      "TLS Allocator ALLOC_TEMP_TLS ... has unfreed allocations"
        //   2. 每一组 Begin*/End* 都要用 try/finally 包住。绘制中途抛异常时
        //      Unity 不会自动帮你补 End，GUI 组栈和临时分配就泄漏了，同样报上面那条。
        // ============================================================

        private GUIStyle mRichLabelStyle;

        private List<NewsTemplate_SO> mAllNewsCache;
        private List<Tag_SO> mAllTagsCache;

        private INewsSystem mCachedNewsSystem;
        private ITagSystem mCachedTagSystem;

        private string mLastGuiError;

        private void OnGUI()
        {
            if (!mShowDebugUi)
            {
                return;
            }

            const float panelWidth = 400f;
            const float panelHeight = 360f;

            EnsureDebugReferences();

            GUILayout.BeginArea(new Rect(mDebugUiOrigin.x, mDebugUiOrigin.y, panelWidth, panelHeight), GUI.skin.box);

            try
            {
                if (mRichLabelStyle == null)
                {
                    mRichLabelStyle = new GUIStyle(GUI.skin.label) { richText = true };
                }

                GUILayout.Label("<b>临时测试：获得数据</b>", mRichLabelStyle);

                if (!string.IsNullOrEmpty(mDebugMessage))
                {
                    GUILayout.Label(mDebugMessage);
                }

                GUILayout.BeginHorizontal();

                try
                {
                    DrawNewsBlock(panelWidth * 0.5f - 12f);
                    DrawTagBlock(panelWidth * 0.5f - 12f);
                }
                finally
                {
                    GUILayout.EndHorizontal();
                }
            }
            catch (System.Exception e)
            {
                ReportGuiError(e);
            }
            finally
            {
                GUILayout.EndArea();
            }
        }

        private void EnsureDebugReferences()
        {
            if (mCachedNewsSystem == null)
            {
                mCachedNewsSystem = this.GetSystem<INewsSystem>();
            }

            if (mCachedTagSystem == null)
            {
                mCachedTagSystem = this.GetSystem<ITagSystem>();
            }
        }

        /// <summary>同一条错误只往 Console 报一次，避免 OnGUI 每帧刷屏。</summary>
        private void ReportGuiError(System.Exception e)
        {
            if (mLastGuiError == e.Message)
            {
                return;
            }

            mLastGuiError = e.Message;
            Debug.LogError($"[Test] 测试面板绘制出错（同一条只报一次）：{e.Message}\n{e.StackTrace}");
        }

        private void DrawNewsBlock(float width)
        {
            GUILayout.BeginVertical(GUILayout.Width(width));

            try
            {
                GUILayout.Label($"新闻模板（{mNewsListResName}）");

                GUILayout.BeginHorizontal();

                try
                {
                    if (GUILayout.Button("全部获得"))
                    {
                        AcquireAllNews();
                    }

                    if (GUILayout.Button("重读总表"))
                    {
                        mAllNewsCache = null;
                    }
                }
                finally
                {
                    GUILayout.EndHorizontal();
                }

                List<NewsTemplate_SO> all = LoadAllNews();

                mNewsScroll = GUILayout.BeginScrollView(mNewsScroll, GUI.skin.box,
                    GUILayout.Height(240f), GUILayout.Width(width));

                try
                {
                    DrawNewsList(all);
                }
                finally
                {
                    GUILayout.EndScrollView();
                }
            }
            finally
            {
                GUILayout.EndVertical();
            }
        }

        private void DrawNewsList(List<NewsTemplate_SO> all)
        {
            if (all == null || all.Count == 0)
            {
                GUILayout.Label("取不到总表");
                return;
            }

            for (int i = 0; i < all.Count; i++)
            {
                NewsTemplate_SO news = all[i];

                if (news == null)
                {
                    continue;
                }

                GUILayout.BeginHorizontal();

                try
                {
                    bool owned = mCachedNewsSystem != null && mCachedNewsSystem.IsAcquired(news.newsId);
                    string label = owned ? $"✔ {news.newsTitle} ({news.newsId})" : $"{news.newsTitle} ({news.newsId})";

                    if (GUILayout.Button(label))
                    {
                        this.SendCommand(new AddNewsCommand(news.newsId));
                        mDebugMessage = $"已获得新闻 {news.newsTitle}";
                    }
                }
                finally
                {
                    GUILayout.EndHorizontal();
                }
            }
        }

        private void DrawTagBlock(float width)
        {
            GUILayout.BeginVertical(GUILayout.Width(width));

            try
            {
                GUILayout.Label($"Tag（{mTagListResName}）");

                GUILayout.BeginHorizontal();

                try
                {
                    if (GUILayout.Button("全部获得"))
                    {
                        AcquireAllTags();
                    }

                    if (GUILayout.Button("重读总表"))
                    {
                        mAllTagsCache = null;
                    }
                }
                finally
                {
                    GUILayout.EndHorizontal();
                }

                List<Tag_SO> all = LoadAllTags();

                mTagScroll = GUILayout.BeginScrollView(mTagScroll, GUI.skin.box,
                    GUILayout.Height(240f), GUILayout.Width(width));

                try
                {
                    DrawTagList(all);
                }
                finally
                {
                    GUILayout.EndScrollView();
                }
            }
            finally
            {
                GUILayout.EndVertical();
            }
        }

        private void DrawTagList(List<Tag_SO> all)
        {
            if (all == null || all.Count == 0)
            {
                GUILayout.Label("取不到总表");
                return;
            }

            for (int i = 0; i < all.Count; i++)
            {
                Tag_SO tag = all[i];

                if (tag == null)
                {
                    continue;
                }

                bool owned = mCachedTagSystem != null && mCachedTagSystem.IsAcquired(tag.tagId);
                string label = owned ? $"✔ {tag.tagName} ({tag.tagId})" : $"{tag.tagName} ({tag.tagId})";

                if (GUILayout.Button(label))
                {
                    this.SendCommand(new AddTagCommand(tag.tagId));
                    mDebugMessage = $"已获得 Tag {tag.tagName}";
                }
            }
        }

        // ============================================================
        //  数据来源（测试用：直接读总表，方便把没获得的也列出来）
        // ============================================================

        /// <summary>总表只读一次，缓存起来。OnGUI 里绝对不能每帧 LoadSync。</summary>
        private List<NewsTemplate_SO> LoadAllNews()
        {
            if (mAllNewsCache != null)
            {
                return mAllNewsCache;
            }

            try
            {
                NewsTemplateList_SO list = mDebugResLoader.LoadSync<NewsTemplateList_SO>(mNewsListResName);
                mAllNewsCache = list != null ? list.newsTemplateList : new List<NewsTemplate_SO>();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Test] 加载新闻总表 {mNewsListResName} 失败：{e.Message}");
                mAllNewsCache = new List<NewsTemplate_SO>();
            }

            return mAllNewsCache;
        }

        /// <summary>同上：缓存，避免每帧申请 ResKit 句柄。</summary>
        private List<Tag_SO> LoadAllTags()
        {
            if (mAllTagsCache != null)
            {
                return mAllTagsCache;
            }

            try
            {
                TagList_SO list = mDebugResLoader.LoadSync<TagList_SO>(mTagListResName);
                mAllTagsCache = list != null ? list.allTagList : new List<Tag_SO>();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Test] 加载 Tag 总表 {mTagListResName} 失败：{e.Message}");
                mAllTagsCache = new List<Tag_SO>();
            }

            return mAllTagsCache;
        }

        private void AcquireAllNews()
        {
            List<NewsTemplate_SO> all = LoadAllNews();

            if (all == null)
            {
                return;
            }

            int count = 0;

            foreach (NewsTemplate_SO news in all)
            {
                if (news == null || (NewsSystem != null && NewsSystem.IsAcquired(news.newsId)))
                {
                    continue;
                }

                this.SendCommand(new AddNewsCommand(news.newsId));
                count++;
            }

            mDebugMessage = $"已获得 {count} 条新闻";
        }

        private void AcquireAllTags()
        {
            List<Tag_SO> all = LoadAllTags();

            if (all == null)
            {
                return;
            }

            int count = 0;

            foreach (Tag_SO tag in all)
            {
                if (tag == null || (TagSystem != null && TagSystem.IsAcquired(tag.tagId)))
                {
                    continue;
                }

                this.SendCommand(new AddTagCommand(tag.tagId));
                count++;
            }

            mDebugMessage = $"已获得 {count} 个 Tag";
        }

        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }
    }
}