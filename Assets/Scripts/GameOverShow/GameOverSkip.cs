using UnityEngine;

namespace TapTapFirst.GameOverShow
{
    /// <summary>
    /// 结局演出的跳过状态。两段式：
    /// 第一段（碎屏/图片/文字）跳过后直接进名单；第二段（名单）再跳一次就出结束按钮。
    /// </summary>
    public class GameOverSkipState
    {
        public bool Allow { get; set; }
        public float Cooldown { get; set; }
        public float ReadyAt { get; private set; }
        public bool Requested { get; private set; }

        /// <summary>进入下一段：清掉跳过标记，并在 delaySeconds 秒内忽略输入（防同一按键连跳）。</summary>
        public void BeginStage(float delaySeconds)
        {
            Requested = false;
            ReadyAt = Time.unscaledTime + Mathf.Max(0f, delaySeconds);
        }

        /// <summary>每帧调用；返回 true 表示本段要跳过（一次按下只消费一次）。</summary>
        public bool ConsumeIfPressed()
        {
            if (Requested) return true;
            if (!Allow) return false;

            var now = Time.unscaledTime;
            if (now < ReadyAt) return false;
            if (!GameOverUi.SkipPressed) return false;

            Request();
            return true;
        }

        /// <summary>外部（测试按钮等）直接请求跳过。</summary>
        public void Request()
        {
            Requested = true;
            ReadyAt = Time.unscaledTime + Mathf.Max(0f, Cooldown);
        }
    }
}
