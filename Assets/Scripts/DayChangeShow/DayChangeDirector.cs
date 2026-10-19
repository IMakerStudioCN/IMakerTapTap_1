using System.Collections;
using QFramework;
using UnityEngine;

namespace TapTapFirst.DayChangeShow
{
    /// <summary>
    /// 天数改变过场的总控。
    /// 标准触发点：订阅 OnDaysChangeEvent（由 DaysAddCommand 发出），收到后播放
    /// 「全屏变黑 → 当前天数浮现 → 抖动 → 崩解 → 重组为下一天 → 黑屏退场」。
    /// </summary>
    public class DayChangeDirector : MonoBehaviour
    {
        public static DayChangeDirector Instance { get; private set; }

        DayChangeConfig mConfig;
        DayChangeView mView;
        DayChangeNumber mNumber;
        DayChangeDust mDust;
        bool mPlaying;
        bool mSubscribed;

        public bool IsPlaying => mPlaying;

        DayChangeConfig Config
        {
            get
            {
                if (mConfig == null) mConfig = DayChangeConfig.Load();
                return mConfig;
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            TapTap.Interface.RegisterEvent<OnDaysChangeEvent>(OnDaysChanged);
            mSubscribed = true;
        }

        void OnDestroy()
        {
            if (mSubscribed)
            {
                TapTap.Interface.UnRegisterEvent<OnDaysChangeEvent>(OnDaysChanged);
                mSubscribed = false;
            }

            DestroyView();
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 事件在天数 +1 之后才发出，事件本身不带数据，
        /// 所以这里用「新天数 - 1」反推当前天数。
        /// </summary>
        void OnDaysChanged(OnDaysChangeEvent daysChangeEvent)
        {
            if (mPlaying) return;

            var timeModel = TapTap.Interface.GetModel<ITimeModel>();
            if (timeModel == null) return;

            var toDay = timeModel.days;
            if (toDay <= 0) return;

            // 最后一天由 GlobalManagerSystem 立刻判定结局并交给 GameOverShow，本过场不播
            if (Config.skipOnFinalDay && timeModel.endDays > 0 && toDay >= timeModel.endDays) return;

            Play(toDay - 1, toDay);
        }

        /// <summary>
        /// 手动播放（调试 / 编辑器菜单用）。演出期间重复调用会被忽略。
        /// </summary>
        public void Play(int fromDay, int toDay)
        {
            if (mPlaying) return;

            mPlaying = true;
            StartCoroutine(PlayRoutine(Mathf.Max(0, fromDay), Mathf.Max(0, toDay)));
        }

        IEnumerator PlayRoutine(int fromDay, int toDay)
        {
            try
            {
                var config = Config;

                mView = DayChangeView.Create(config);
                mNumber = new DayChangeNumber(mView.NumberRoot, config, mView.Font);
                mDust = new DayChangeDust(mView.DustRoot, config);

                mNumber.SetNumber(fromDay);

                yield return DayChangeUi.Fade(mView.Root, 0f, 1f, config.fadeInSeconds);

                yield return mNumber.PlayIn(config.oldNumberInSeconds);
                yield return DayChangeUi.WaitSeconds(config.oldNumberHoldSeconds);
                yield return mNumber.PlayGlitch(config.glitchSeconds);

                mDust.Build(mNumber.GetInkCenters());
                yield return DayChangeUi.RunTogether(mDust.PlayBurst(config.burstSeconds), mNumber.PlayBurst());

                var reformCenters = mNumber.PrepareReform(toDay);
                yield return DayChangeUi.RunTogether(mDust.PlayGather(reformCenters, config.reformSeconds),
                    mNumber.PlayReform());

                yield return DayChangeUi.WaitSeconds(config.newNumberHoldSeconds);
                yield return DayChangeUi.Fade(mView.Root, 1f, 0f, config.fadeOutSeconds);
            }
            finally
            {
                DestroyView();
                mPlaying = false;
            }
        }

        void DestroyView()
        {
            if (mDust != null)
            {
                mDust.Destroy();
                mDust = null;
            }

            if (mNumber != null)
            {
                mNumber.Clear();
                mNumber = null;
            }

            if (mView != null)
            {
                mView.Destroy();
                mView = null;
            }
        }
    }
}
