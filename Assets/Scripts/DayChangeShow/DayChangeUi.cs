using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TapTapFirst.DayChangeShow
{
    internal static class DayChangeUi
    {
        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            return rect;
        }

        public static Image CreateImage(string name, Transform parent, Color color, bool raycastTarget = false)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        public static void SetAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null) return;
            var color = graphic.color;
            color.a = Mathf.Clamp01(alpha);
            graphic.color = color;
        }

        public static IEnumerator Fade(CanvasGroup group, float from, float to, float seconds)
        {
            if (group == null) yield break;

            seconds = Mathf.Max(0f, seconds);
            if (seconds <= 0f)
            {
                group.alpha = to;
                yield break;
            }

            var time = 0f;
            while (time < seconds)
            {
                time += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(time / seconds));
                yield return null;
            }

            group.alpha = to;
        }

        public static IEnumerator WaitSeconds(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            if (seconds <= 0f) yield break;

            var time = 0f;
            while (time < seconds)
            {
                time += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        /// <summary>两段演出并行推进。传入的枚举器只能 yield return null。</summary>
        public static IEnumerator RunTogether(IEnumerator first, IEnumerator second)
        {
            var firstRunning = first != null;
            var secondRunning = second != null;

            while (firstRunning || secondRunning)
            {
                if (firstRunning) firstRunning = first.MoveNext();
                if (secondRunning) secondRunning = second.MoveNext();
                if (firstRunning || secondRunning) yield return null;
            }
        }

        public static float EaseOutCubic(float value)
        {
            value = Mathf.Clamp01(value);
            return 1f - Mathf.Pow(1f - value, 3f);
        }

        public static float EaseInCubic(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * value;
        }

        public static float EaseOutQuart(float value)
        {
            value = Mathf.Clamp01(value);
            var inverse = 1f - value;
            return 1f - inverse * inverse * inverse * inverse;
        }

        public static float Range(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
