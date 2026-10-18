using System.Reflection;
using QFramework;
using System.Collections.Generic;
using TapTapFirst.GameOverShow;
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

        // ============================================================
        //  结局测试（右上角，只用于测试，不影响游戏逻辑）
        // ============================================================

        [Header("结局测试 UI（右上角，动态生成）")]
        [Tooltip("显示结局判定 / 结局表现的测试按钮")]
        [SerializeField] private bool mShowGameOverTestUi = true;

        [Tooltip("右上角结局测试面板宽度")]
        [SerializeField] private float mGameOverTestPanelWidth = 380f;

        private Vector2 mGameOverTestScroll;
        private string mGameOverTestMessage = "";

        private readonly ResLoader mDebugResLoader = ResLoader.Allocate();

        private INewsSystem NewsSystem => this.GetSystem<INewsSystem>();
        private ITagSystem TagSystem => this.GetSystem<ITagSystem>();

        private IPlayerModel PlayerModelRef => this.GetModel<IPlayerModel>();
        private ITimeModel TimeModelRef => this.GetModel<ITimeModel>();
        private IGlobalManagerSystem GlobalSystemRef => this.GetSystem<IGlobalManagerSystem>();
        private GlobalManagerSystem GlobalSystemConcrete =>
            TapTap.Interface.GetSystem<IGlobalManagerSystem>() as GlobalManagerSystem;

        private Vector2 mNewsScroll;
        private Vector2 mTagScroll;
        private string mDebugMessage = "";

        void Start()
        {
            // 兜底：UIKitSceneCleanup 会在场景加载/卸载时自动清理，这里再手动跑一次确保表里没有死条目。
            UIKitSceneCleanup.Cleanup();

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
            if (mShowDebugUi)
            {
                DrawDataTestPanel();
            }

            if (mShowGameOverTestUi)
            {
                DrawGameOverTestPanel();
            }
        }

        private void DrawDataTestPanel()
        {
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
        //  结局测试面板（右上角）
        // ============================================================

        private void DrawGameOverTestPanel()
        {
            const float margin = 20f;

            float width = Mathf.Max(300f, mGameOverTestPanelWidth);
            float height = Mathf.Min(620f, Screen.height - margin * 2f);
            var rect = new Rect(Screen.width - width - margin, margin, width, height);
            var labelStyle = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };

            GUILayout.BeginArea(rect, GUI.skin.box);
            mGameOverTestScroll = GUILayout.BeginScrollView(mGameOverTestScroll);

            GUILayout.Label("<b>结局测试（GlobalManager 判定）</b>", labelStyle);

            var player = PlayerModelRef;
            var time = TimeModelRef;
            var global = GlobalSystemRef;
            var concrete = GlobalSystemConcrete;

            int which = global != null ? global.WhichEnd(player.TargetFundsValue) : -99;

            GUILayout.Label($"信任 {player.CredibilityValue}    道德(Evil) {player.EvilValue}", labelStyle);
            GUILayout.Label($"资金 {player.FundsValue}/{player.TargetFundsValue}    天数 {time.days}/{time.endDays}", labelStyle);
            GUILayout.Label($"WhichEnd = {which}（{EndingName(which)}）", labelStyle);
            GUILayout.Label(
                $"IsEnded = {(concrete != null && concrete.IsEnded)}    EndingId = {(concrete != null ? concrete.EndingId : -99)}",
                labelStyle);

            GUILayout.Space(4f);
            GUILayout.Label("<b>① 只测表现</b>：直接发 GameOverEvent", labelStyle);
            GUILayout.BeginHorizontal();
            for (int id = 0; id <= 4; id++)
            {
                if (GUILayout.Button(id.ToString(), GUILayout.Width(52f)))
                {
                    PreviewEnding(id);
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(4f);
            GUILayout.Label("<b>② 测真实判定</b>：改内存数值 + 发真实事件", labelStyle);

            if (GUILayout.Button("信任归零 → 期望 0")) SimulateTrustZero();
            if (GUILayout.Button("道德归零 → 期望 1")) SimulateEvilZero();
            if (GUILayout.Button("最后一天 + 资金不足 → 期望 2")) SimulateFundsLow();
            if (GUILayout.Button("最后一天 + 资金够 → 期望 3")) SimulateNormal();
            if (GUILayout.Button("最后一天 + NB数值 → 4（需有过期主线任务，否则 3）")) SimulateNb();
            if (GUILayout.Button("按当前数值直接结算 EndTheGame()")) TriggerEndTheGame();

            GUILayout.Space(4f);
            GUILayout.Label("<b>③ 工具</b>", labelStyle);

            if (GUILayout.Button("跳过当前演出（两段式，等同按键）")) SkipCurrentEnding();
            if (GUILayout.Button("重置结算标记 + 重建表现 Director")) ResetEndingTestState();
            if (GUILayout.Button("恢复默认数值（只改内存）")) ResetPlayerValues();

            if (!string.IsNullOrEmpty(mGameOverTestMessage))
            {
                GUILayout.Space(4f);
                GUILayout.Label(mGameOverTestMessage, labelStyle);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private static string EndingName(int endingId)
        {
            switch (endingId)
            {
                case 0: return "信任归零";
                case 1: return "道德归零";
                case 2: return "资金不足";
                case 3: return "普通结局";
                case 4: return "NB结局";
                default: return "未触发";
            }
        }

        private void PreviewEnding(int endingId)
        {
            TapTap.Interface.SendEvent(new GameOverEvent { EndingId = endingId });
            mGameOverTestMessage = $"已发送结局表现事件：EndingId = {endingId}（{EndingName(endingId)}）\n" +
                                   "若没有反应，说明上一次表现还没结束，先点③里的重置。";
        }

        private void SimulateTrustZero()
        {
            SetPlayerValues(credibility: 0);
            this.SendCommand(new CredibilityValueCommand(0));
            ReportSimulation("信任归零");
        }

        private void SimulateEvilZero()
        {
            SetPlayerValues(evil: 0);
            this.SendCommand(new EvilValueCommand(0));
            ReportSimulation("道德归零");
        }

        private void SimulateFundsLow()
        {
            SetPlayerValues(credibility: 50, evil: 70, funds: Mathf.Max(0, PlayerModelRef.TargetFundsValue - 1));
            EnterLastDay();
            ReportSimulation("最后一天资金不足");
        }

        private void SimulateNormal()
        {
            SetPlayerValues(credibility: 50, evil: 70, funds: PlayerModelRef.TargetFundsValue);
            EnterLastDay();
            ReportSimulation("最后一天资金够");
        }

        private void SimulateNb()
        {
            int target = PlayerModelRef.TargetFundsValue;
            SetPlayerValues(credibility: 90, evil: 90, funds: Mathf.CeilToInt(target * 1.8f) + 1);
            EnterLastDay();
            ReportSimulation("最后一天NB数值");
        }

        private void TriggerEndTheGame()
        {
            GlobalSystemRef.EndTheGame();
            ReportSimulation("EndTheGame()");
        }

        private void SetPlayerValues(int credibility = -1, int evil = -1, int funds = -1)
        {
            var player = PlayerModelRef;

            if (credibility >= 0) player.CredibilityValue = credibility;
            if (evil >= 0) player.EvilValue = evil;
            if (funds >= 0) player.FundsValue = funds;
        }

        private void EnterLastDay()
        {
            var time = TimeModelRef;
            time.days = Mathf.Max(time.days, time.endDays);
            TapTap.Interface.SendEvent(new OnDaysChangeEvent());
        }

        private void ReportSimulation(string title)
        {
            var concrete = GlobalSystemConcrete;
            int which = GlobalSystemRef != null ? GlobalSystemRef.WhichEnd(PlayerModelRef.TargetFundsValue) : -99;

            mGameOverTestMessage =
                $"[{title}] WhichEnd = {which}（{EndingName(which)}），" +
                $"IsEnded = {(concrete != null && concrete.IsEnded)}，" +
                $"EndingId = {(concrete != null ? concrete.EndingId : -99)}\n" +
                "提示：一次 Play 只有第一次能真正结算，重复测试先点③里的重置。";
        }

        private void SkipCurrentEnding()
        {
            var director = GameOverDirector.Instance;

            if (director == null)
            {
                mGameOverTestMessage = "当前没有结局在播放（GameOverDirector 不存在）。";
                return;
            }

            director.RequestSkip();
            mGameOverTestMessage = "已请求跳过：第一段跳到名单，名单里再跳一次出结束按钮。";
        }

        private void ResetEndingTestState()
        {
            var concrete = GlobalSystemConcrete;

            if (concrete != null)
            {
                var endedField = typeof(GlobalManagerSystem).GetField("mGameEnded",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (endedField != null) endedField.SetValue(concrete, false);

                var endingProperty = typeof(GlobalManagerSystem).GetProperty("EndingId",
                    BindingFlags.Instance | BindingFlags.Public);
                if (endingProperty != null) endingProperty.SetValue(concrete, -1);
            }

            var directors = FindObjectsOfType<GameOverDirector>();
            for (int i = 0; i < directors.Length; i++)
            {
                if (directors[i] != null) Destroy(directors[i].gameObject);
            }

            var instanceProperty = typeof(GameOverDirector).GetProperty("Instance",
                BindingFlags.Static | BindingFlags.Public);
            if (instanceProperty != null) instanceProperty.SetValue(null, null);

            var oldCanvas = GameObject.Find("[GameOverCanvas]");
            if (oldCanvas != null) Destroy(oldCanvas);

            var go = new GameObject("[GameOverDirector]");
            DontDestroyOnLoad(go);
            go.AddComponent<GameOverDirector>();

            mGameOverTestMessage = "已重置 mGameEnded / EndingId，并重建 GameOverDirector，可以重复测试。";
        }

        private void ResetPlayerValues()
        {
            var defaults = new PlayerModel();
            var player = PlayerModelRef;

            player.FundsValue = defaults.FundsValue;
            player.EvilValue = defaults.EvilValue;
            player.CredibilityValue = defaults.CredibilityValue;
            player.TargetFundsValue = defaults.TargetFundsValue;

            TimeModelRef.days = 1;
            mGameOverTestMessage = "已恢复默认数值（只改内存，不写存档）。";
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
