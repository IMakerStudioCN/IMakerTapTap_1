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

        private void OnGUI()
        {
            if (!mShowDebugUi)
            {
                return;
            }

            const float panelWidth = 400f;
            const float panelHeight = 360f;

            GUILayout.BeginArea(new Rect(mDebugUiOrigin.x, mDebugUiOrigin.y, panelWidth, panelHeight), GUI.skin.box);

            GUILayout.Label("<b>临时测试：获得数据</b>", new GUIStyle(GUI.skin.label) { richText = true });

            if (!string.IsNullOrEmpty(mDebugMessage))
            {
                GUILayout.Label(mDebugMessage);
            }

            GUILayout.BeginHorizontal();

            DrawNewsBlock(panelWidth * 0.5f - 12f);
            DrawTagBlock(panelWidth * 0.5f - 12f);

            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawNewsBlock(float width)
        {
            GUILayout.BeginVertical(GUILayout.Width(width));
            GUILayout.Label($"新闻模板（{mNewsListResName}）");

            if (GUILayout.Button("全部获得"))
            {
                AcquireAllNews();
            }

            List<NewsTemplate_SO> all = LoadAllNews();

            mNewsScroll = GUILayout.BeginScrollView(mNewsScroll, GUI.skin.box,
                GUILayout.Height(240f), GUILayout.Width(width));

            if (all == null || all.Count == 0)
            {
                GUILayout.Label("取不到总表");
            }
            else
            {
                foreach (NewsTemplate_SO news in all)
                {
                    if (news == null)
                    {
                        continue;
                    }

                    GUILayout.BeginHorizontal();

                    bool owned = NewsSystem != null && NewsSystem.IsAcquired(news.newsId);

                    if (GUILayout.Button(owned ? $"✔ {news.newsTitle} ({news.newsId})" : $"{news.newsTitle} ({news.newsId})"))
                    {
                        this.SendCommand(new AddNewsCommand(news.newsId));
                        mDebugMessage = $"已获得新闻 {news.newsTitle}";
                    }

                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawTagBlock(float width)
        {
            GUILayout.BeginVertical(GUILayout.Width(width));
            GUILayout.Label($"Tag（{mTagListResName}）");

            if (GUILayout.Button("全部获得"))
            {
                AcquireAllTags();
            }

            List<Tag_SO> all = LoadAllTags();

            mTagScroll = GUILayout.BeginScrollView(mTagScroll, GUI.skin.box,
                GUILayout.Height(240f), GUILayout.Width(width));

            if (all == null || all.Count == 0)
            {
                GUILayout.Label("取不到总表");
            }
            else
            {
                foreach (Tag_SO tag in all)
                {
                    if (tag == null)
                    {
                        continue;
                    }

                    bool owned = TagSystem != null && TagSystem.IsAcquired(tag.tagId);

                    if (GUILayout.Button(owned ? $"✔ {tag.tagName} ({tag.tagId})" : $"{tag.tagName} ({tag.tagId})"))
                    {
                        this.SendCommand(new AddTagCommand(tag.tagId));
                        mDebugMessage = $"已获得 Tag {tag.tagName}";
                    }
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        // ============================================================
        //  数据来源（测试用：直接读总表，方便把没获得的也列出来）
        // ============================================================

        private List<NewsTemplate_SO> LoadAllNews()
        {
            try
            {
                NewsTemplateList_SO list = mDebugResLoader.LoadSync<NewsTemplateList_SO>(mNewsListResName);
                return list != null ? list.newsTemplateList : null;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Test] 加载新闻总表 {mNewsListResName} 失败：{e.Message}");
                return null;
            }
        }

        private List<Tag_SO> LoadAllTags()
        {
            try
            {
                TagList_SO list = mDebugResLoader.LoadSync<TagList_SO>(mTagListResName);
                return list != null ? list.allTagList : null;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Test] 加载 Tag 总表 {mTagListResName} 失败：{e.Message}");
                return null;
            }
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