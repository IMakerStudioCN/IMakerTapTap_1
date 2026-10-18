using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TapTapFirst.GameOverShow
{
    internal static class GameOverUi
    {
        public static bool SkipPressed
        {
            get
            {
                if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) return true;
                if (Input.anyKeyDown) return true;
                return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
            }
        }

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

        public static TextMeshProUGUI CreateText(string name, Transform parent, TMP_FontAsset font, float fontSize,
            Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        public static IEnumerator Fade(CanvasGroup group, float from, float to, float seconds,
            GameOverSkipState skip = null)
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
                if (skip != null && skip.ConsumeIfPressed())
                {
                    group.alpha = to;
                    yield break;
                }

                time += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(time / seconds));
                yield return null;
            }

            group.alpha = to;
        }

        public static IEnumerator FadeGraphic(Graphic graphic, float from, float to, float seconds)
        {
            if (graphic == null) yield break;

            var color = graphic.color;
            seconds = Mathf.Max(0f, seconds);
            if (seconds <= 0f)
            {
                color.a = to;
                graphic.color = color;
                yield break;
            }

            var time = 0f;
            while (time < seconds)
            {
                time += Time.unscaledDeltaTime;
                color.a = Mathf.Lerp(from, to, Mathf.Clamp01(time / seconds));
                graphic.color = color;
                yield return null;
            }

            color.a = to;
            graphic.color = color;
        }

        public static IEnumerator WaitSeconds(float seconds, GameOverSkipState skip = null)
        {
            if (seconds <= 0f) yield break;

            var time = 0f;
            while (time < seconds)
            {
                if (skip != null && skip.ConsumeIfPressed()) yield break;

                time += Time.unscaledDeltaTime;
                yield return null;
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
    }
}
