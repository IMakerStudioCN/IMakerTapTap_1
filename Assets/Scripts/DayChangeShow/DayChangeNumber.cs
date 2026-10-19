using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TapTapFirst.DayChangeShow
{
    /// <summary>
    /// 天数数字：浮现 → 抖动 → 炸开 → 从碎屑里重新聚成新数字。
    /// </summary>
    public class DayChangeNumber
    {
        class Digit
        {
            public TextMeshProUGUI text;
            public RectTransform rect;
            public Vector2 home;
            public Vector2 burstTo;
            public float burstAngle;
            public float burstDelay;
        }

        const float kPopScale = 1.14f;
        const float kPopPortion = 0.2f;
        const float kDissolvePortion = 0.62f;

        readonly RectTransform mRoot;
        readonly DayChangeConfig mConfig;
        readonly TMP_FontAsset mFont;
        readonly List<Digit> mDigits = new List<Digit>();
        readonly System.Random mRandom = new System.Random();

        int mReformTarget;

        public DayChangeNumber(RectTransform root, DayChangeConfig config, TMP_FontAsset font)
        {
            mRoot = root;
            mConfig = config;
            mFont = font;
        }

        public List<Vector2> GetInkCenters()
        {
            var centers = new List<Vector2>(mDigits.Count);
            for (var i = 0; i < mDigits.Count; i++) centers.Add(mDigits[i].home);
            return centers;
        }

        public void SetNumber(int value)
        {
            Clear();

            var chars = Mathf.Max(0, value).ToString();
            for (var i = 0; i < chars.Length; i++) mDigits.Add(CreateDigit());

            Layout(chars.Length);

            for (var i = 0; i < mDigits.Count; i++)
            {
                var digit = mDigits[i];
                digit.text.text = chars[i].ToString();
                digit.burstTo = digit.home;
                Apply(digit, digit.home, 0f, Mathf.Clamp(mConfig.numberStartScale, 0.01f, 1f), 0f);
            }

            mReformTarget = mDigits.Count;
        }

        public IEnumerator PlayIn(float seconds)
        {
            seconds = Mathf.Max(0.01f, seconds);

            var startScale = Mathf.Clamp(mConfig.numberStartScale, 0.01f, 1f);
            var stagger = seconds * 0.12f;
            var total = seconds + stagger * Mathf.Max(0, mDigits.Count - 1);

            var time = 0f;
            while (time < total)
            {
                time += Time.unscaledDeltaTime;
                for (var i = 0; i < mDigits.Count; i++)
                {
                    var k = DayChangeUi.EaseOutCubic(Mathf.Clamp01((time - i * stagger) / seconds));
                    var digit = mDigits[i];
                    Apply(digit, digit.home, 0f, Mathf.Lerp(startScale, 1f, k), k);
                }

                yield return null;
            }

            for (var i = 0; i < mDigits.Count; i++)
                Apply(mDigits[i], mDigits[i].home, 0f, 1f, 1f);
        }

        /// <summary>炸开前的抖动：越抖越厉害，把数字撕到崩解瞬间。</summary>
        public IEnumerator PlayGlitch(float seconds)
        {
            seconds = Mathf.Max(0.01f, seconds);

            var amplitude = mConfig.fontSize * 0.07f;
            var offsets = new Vector2[mDigits.Count];

            var time = 0f;
            var refreshAt = 0f;
            while (time < seconds)
            {
                time += Time.unscaledDeltaTime;

                if (time >= refreshAt)
                {
                    refreshAt = time + 0.035f;
                    for (var i = 0; i < offsets.Length; i++)
                    {
                        offsets[i] = new Vector2(DayChangeUi.Range(mRandom, -1f, 1f),
                            DayChangeUi.Range(mRandom, -0.4f, 0.4f)) * amplitude;
                    }
                }

                var strength = Mathf.Clamp01(time / seconds);
                for (var i = 0; i < mDigits.Count; i++)
                    Apply(mDigits[i], mDigits[i].home + offsets[i] * strength, 0f, 1f, 1f);

                yield return null;
            }

            for (var i = 0; i < mDigits.Count; i++)
                Apply(mDigits[i], mDigits[i].home, 0f, 1f, 1f);
        }

        /// <summary>崩解：每位数字先鼓一下，再朝外加速飞出、旋转、缩小、淡出。</summary>
        public IEnumerator PlayBurst()
        {
            var seconds = Mathf.Max(0.01f, mConfig.burstSeconds);
            var count = mDigits.Count;

            for (var i = 0; i < count; i++)
            {
                var digit = mDigits[i];
                var direction = DirectionOf(digit.home);
                direction = Rotate(direction, DayChangeUi.Range(mRandom, -0.6f, 0.6f));

                digit.burstTo = digit.home + direction * mConfig.burstRadius * DayChangeUi.Range(mRandom, 0.75f, 1.15f);
                digit.burstAngle = DayChangeUi.Range(mRandom, -1f, 1f) * mConfig.burstRotation;
                digit.burstDelay = i * 0.03f;
            }

            var total = seconds + 0.03f * Mathf.Max(0, count - 1);

            var time = 0f;
            while (time < total)
            {
                time += Time.unscaledDeltaTime;
                StepBurst(seconds, time, false);
                yield return null;
            }

            StepBurst(seconds, time, true);
        }

        void StepBurst(float seconds, float time, bool finish)
        {
            for (var i = 0; i < mDigits.Count; i++)
            {
                var digit = mDigits[i];
                var local = finish ? 1f : Mathf.Clamp01((time - digit.burstDelay) / seconds);

                var scale = Mathf.LerpUnclamped(1f, kPopScale, DayChangeUi.EaseOutCubic(Mathf.Clamp01(local / kPopPortion)));
                if (local > kPopPortion)
                {
                    var shrink = DayChangeUi.EaseInCubic((local - kPopPortion) / (1f - kPopPortion));
                    scale = Mathf.LerpUnclamped(kPopScale, mConfig.burstScale, shrink);
                }

                var alpha = 1f - DayChangeUi.EaseOutCubic(Mathf.Clamp01(local / kDissolvePortion));
                var position = Vector2.LerpUnclamped(digit.home, digit.burstTo, DayChangeUi.EaseOutQuart(local));

                Apply(digit, position, digit.burstAngle * DayChangeUi.EaseOutCubic(local), scale, alpha);
            }
        }

        /// <summary>把新天数的排版定好并换好字形（此时透明度为 0，看不见切换）。</summary>
        public List<Vector2> PrepareReform(int value)
        {
            var chars = Mathf.Max(0, value).ToString();
            var target = Mathf.Max(1, chars.Length);
            mReformTarget = target;

            while (mDigits.Count < target) mDigits.Add(CreateIncomingDigit());

            Layout(target);

            for (var i = 0; i < target; i++) mDigits[i].text.text = chars[i].ToString();

            var centers = new List<Vector2>(target);
            for (var i = 0; i < target; i++) centers.Add(mDigits[i].home);
            return centers;
        }

        /// <summary>重组：逐位从散点聚回排版位置，带一点过冲再收住。</summary>
        public IEnumerator PlayReform()
        {
            var seconds = Mathf.Max(0.01f, mConfig.reformSeconds);
            var stagger = Mathf.Max(0f, mConfig.reformStaggerSeconds);
            var total = seconds + stagger * Mathf.Max(0, mReformTarget - 1);

            var time = 0f;
            while (time < total)
            {
                time += Time.unscaledDeltaTime;
                StepReform(seconds, stagger, time, false);
                yield return null;
            }

            StepReform(seconds, stagger, time, true);
        }

        void StepReform(float seconds, float stagger, float time, bool finish)
        {
            var overshoot = 1f + Mathf.Max(0f, mConfig.reformOvershoot);

            for (var i = 0; i < mDigits.Count; i++)
            {
                var digit = mDigits[i];

                if (i >= mReformTarget)
                {
                    var extra = finish ? 1f : Mathf.Clamp01(time / seconds);
                    Apply(digit, Vector2.LerpUnclamped(digit.burstTo, digit.burstTo * 1.5f, extra),
                        digit.burstAngle * (1f + extra), mConfig.burstScale * (1f - extra), 0f);
                    continue;
                }

                var local = finish ? 1f : Mathf.Clamp01((time - i * stagger) / seconds);
                var approach = DayChangeUi.EaseOutQuart(local);

                var position = Vector2.LerpUnclamped(digit.burstTo, digit.home, approach);
                var angle = Mathf.LerpUnclamped(digit.burstAngle, 0f, DayChangeUi.EaseOutCubic(local));

                var scale = Mathf.LerpUnclamped(mConfig.burstScale, overshoot, approach);
                var settle = DayChangeUi.EaseOutCubic(Mathf.Clamp01((local - 0.55f) / 0.45f));
                scale = Mathf.LerpUnclamped(scale, 1f, settle);

                var alpha = DayChangeUi.EaseOutCubic(Mathf.Clamp01(local / 0.45f));

                Apply(digit, position, angle, scale, alpha);
            }
        }

        public void Clear()
        {
            for (var i = 0; i < mDigits.Count; i++)
            {
                if (mDigits[i].rect != null) UnityEngine.Object.Destroy(mDigits[i].rect.gameObject);
            }

            mDigits.Clear();
        }

        void Layout(int count)
        {
            var advance = mConfig.fontSize * Mathf.Max(0.1f, mConfig.digitAdvance);

            for (var i = 0; i < mDigits.Count; i++)
                mDigits[i].home = new Vector2((i - (count - 1) * 0.5f) * advance, 0f);
        }

        Digit CreateDigit()
        {
            var rect = DayChangeUi.CreateRect("Digit", mRoot);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(mConfig.fontSize * 1.2f, mConfig.fontSize * 1.4f);
            rect.anchoredPosition = Vector2.zero;

            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (mFont != null) text.font = mFont;
            text.text = "";
            text.fontSize = mConfig.fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.color = mConfig.digitColor;

            return new Digit { text = text, rect = rect };
        }

        Digit CreateIncomingDigit()
        {
            var digit = CreateDigit();
            var direction = DirectionOf(Vector2.zero);

            digit.burstTo = direction * mConfig.burstRadius * DayChangeUi.Range(mRandom, 0.6f, 1f);
            digit.burstAngle = DayChangeUi.Range(mRandom, -1f, 1f) * mConfig.burstRotation;
            digit.home = digit.burstTo;

            Apply(digit, digit.burstTo, digit.burstAngle, mConfig.burstScale, 0f);
            return digit;
        }

        void Apply(Digit digit, Vector2 position, float angle, float scale, float alpha)
        {
            digit.rect.anchoredPosition = position;
            digit.rect.localEulerAngles = new Vector3(0f, 0f, angle);
            digit.rect.localScale = new Vector3(scale, scale, 1f);

            var color = mConfig.digitColor;
            color.a = Mathf.Clamp01(alpha);
            digit.text.color = color;
        }

        Vector2 DirectionOf(Vector2 home)
        {
            if (home.sqrMagnitude > 1f) return home.normalized;

            var angle = DayChangeUi.Range(mRandom, 0f, Mathf.PI * 2f);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        static Vector2 Rotate(Vector2 value, float radians)
        {
            var cos = Mathf.Cos(radians);
            var sin = Mathf.Sin(radians);
            return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
        }
    }
}
