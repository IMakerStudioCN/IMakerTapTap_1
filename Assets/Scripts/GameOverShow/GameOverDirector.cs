using System.Collections;
using QFramework;
using TapTapFirst.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TapTapFirst.GameOverShow
{
    public class GameOverDirector : MonoBehaviour
    {
        public static GameOverDirector Instance { get; private set; }

        GameOverConfig mConfig;
        GameOverView mView;
        GameOverSkipState mSkip;
        bool mPlaying;
        bool mSubscribed;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            TapTap.Interface.RegisterEvent<GameOverEvent>(OnGameOver);
            mSubscribed = true;
        }

        void OnDestroy()
        {
            if (mSubscribed)
            {
                TapTap.Interface.UnRegisterEvent<GameOverEvent>(OnGameOver);
                mSubscribed = false;
            }

            DestroyView();
            if (Instance == this) Instance = null;
        }

        void OnGameOver(GameOverEvent gameOverEvent)
        {
            Play(gameOverEvent.EndingId);
        }

        /// <summary>直接播放某个结局（调试用，0~4）</summary>
        public void Play(int endingId)
        {
            if (mPlaying) return;

            mPlaying = true;
            StartCoroutine(PlayEnding(endingId));
        }

        /// <summary>请求跳过当前段（测试按钮/外部按钮用，绕过输入保护）</summary>
        public void RequestSkip()
        {
            if (mSkip != null) mSkip.Request();
        }

        IEnumerator PlayEnding(int endingId)
        {
            mConfig = GameOverConfig.Load();
            var preset = mConfig.GetEnding(endingId);
            var style = preset.screenStyle;

            GameAudio.StopAmbience();

            mSkip = new GameOverSkipState
            {
                Allow = mConfig.allowSkipEnding,
                Cooldown = mConfig.skipCooldown
            };
            mSkip.BeginStage(mConfig.skipStartDelay);

            var usesScreenshot = style == EndingScreenStyle.Shatter || style == EndingScreenStyle.CrtShutdown;

            yield return new WaitForEndOfFrame();
            var screenshot = usesScreenshot ? CaptureScreenshot() : null;

            mView = GameOverView.Create(mConfig);
            mView.SetBackdrop(preset.backgroundColor, preset.showBackground ? 1f : 0f);
            if (screenshot != null) mView.SetScreenshot(screenshot);
            mView.ShowSkipHint(mConfig.allowSkipEnding, mConfig.endingSkipHint);
            yield return null;

            switch (style)
            {
                case EndingScreenStyle.Shatter:
                    yield return GameOverScreenFx.Shatter(mView, screenshot, preset.screenEffectSeconds, mSkip);
                    break;
                case EndingScreenStyle.CrtShutdown:
                    yield return GameOverScreenFx.CrtShutdown(mView, screenshot, preset.screenEffectSeconds, mSkip);
                    break;
                case EndingScreenStyle.RemoveScreenEffect:
                    yield return GameOverScreenFx.RemoveScreenEffect(preset.screenEffectSeconds, mSkip);
                    break;
            }

            if (screenshot != null)
            {
                mView.SetScreenshot(null);
                Destroy(screenshot);
                screenshot = null;
            }

            if (style != EndingScreenStyle.RemoveScreenEffect)
                mView.SetBackdrop(preset.backgroundColor, 1f);

            if (!mSkip.Requested)
                yield return GameOverUi.WaitSeconds(preset.delayBeforeContent, mSkip);

            if (!mSkip.Requested)
            {
                var beforeText = endingId <= 3 && preset.cg != null ? new[] { preset.cg } : preset.images;
                yield return GameOverImageSequence.Play(mView, beforeText, preset.imageSeconds,
                    preset.imageFadeSeconds, mSkip);
            }

            if (!mSkip.Requested)
                yield return mView.PlayText(preset, mSkip);

            if (!mSkip.Requested && endingId == 4 && preset.cg != null)
            {
                yield return GameOverImageSequence.Play(mView, new[] { preset.cg }, preset.imageSeconds,
                    preset.imageFadeSeconds, mSkip);
            }

            mView.ShowSkipHint(false, "");

            if (mConfig.credits != null && mConfig.credits.enabled)
            {
                if (!mSkip.Requested)
                    yield return GameOverUi.WaitSeconds(preset.delayBeforeCredits, mSkip);

                // 第二段：名单本身再跳一次就出结束按钮（受 credits.allowSkip 控制）
                mSkip.Allow = mConfig.credits.allowSkip;
                mSkip.BeginStage(mConfig.skipCooldown);
                yield return mView.Credits.Play(mConfig.credits, mSkip);
            }

            mView.ShowSkipHint(false, "");

            switch (mConfig.endAction)
            {
                case GameOverEndAction.ShowButtons:
                    yield return mView.ShowEndButtons(mConfig, Restart, Quit);
                    break;
                case GameOverEndAction.LoadScene:
                    Restart();
                    break;
            }

            mSkip = null;
        }

        void Restart()
        {
            DestroyView();
            mPlaying = false;

            if (mConfig != null && !string.IsNullOrEmpty(mConfig.restartSceneName))
                SceneManager.LoadScene(mConfig.restartSceneName);
        }

        void Quit()
        {
            GameApplication.Quit();
        }

        void DestroyView()
        {
            if (mView == null) return;

            mView.Destroy();
            mView = null;
        }

        static Texture2D CaptureScreenshot()
        {
            try
            {
                return ScreenCapture.CaptureScreenshotAsTexture();
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("[GameOverShow] 抓屏失败：" + exception.Message);
                return null;
            }
        }
    }
}
