using System.Collections;
using System.Collections.Generic;
using TapTapFirst.ScreenFx;
using UnityEngine;
using UnityEngine.UI;

namespace TapTapFirst.GameOverShow
{
    public static class GameOverScreenFx
    {
        struct ShardPiece
        {
            public RectTransform rect;
            public RawImage graphic;
            public Vector2 velocity;
            public float angularVelocity;
            public float rotation;
        }

        public static IEnumerator Shatter(GameOverView view, Texture2D screenshot, float duration,
            GameOverSkipState skip = null)
        {
            if (view == null) yield break;

            duration = Mathf.Max(0.35f, duration);
            var layer = view.ShardLayer;
            var flash = GameOverUi.CreateImage("Flash", layer, new Color(1f, 1f, 1f, 0.9f));
            flash.rectTransform.SetAsLastSibling();
            var pieces = new List<ShardPiece>();

            if (screenshot != null)
            {
                const int columns = 7;
                const int rows = 5;

                for (var y = 0; y < rows; y++)
                {
                    for (var x = 0; x < columns; x++)
                    {
                        var u0 = (float)x / columns;
                        var v0 = (float)y / rows;
                        var w = 1f / columns;
                        var h = 1f / rows;

                        var rect = GameOverUi.CreateRect("Shard", layer);
                        rect.anchorMin = new Vector2(u0, v0);
                        rect.anchorMax = new Vector2(u0 + w, v0 + h);
                        rect.offsetMin = Vector2.zero;
                        rect.offsetMax = Vector2.zero;
                        rect.pivot = new Vector2(0.5f, 0.5f);

                        var raw = rect.gameObject.AddComponent<RawImage>();
                        raw.texture = screenshot;
                        raw.uvRect = new Rect(u0, v0, w, h);
                        raw.raycastTarget = false;

                        var center = new Vector2(u0 + w * 0.5f, v0 + h * 0.5f);
                        var direction = center - new Vector2(0.5f, 0.5f);
                        if (direction.sqrMagnitude < 0.0001f)
                            direction = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
                        direction.Normalize();

                        var speed = Random.Range(260f, 820f);
                        var velocity = direction * speed +
                                       new Vector2(Random.Range(-120f, 120f), Random.Range(80f, 380f));

                        pieces.Add(new ShardPiece
                        {
                            rect = rect,
                            graphic = raw,
                            velocity = velocity,
                            angularVelocity = Random.Range(-220f, 220f),
                            rotation = 0f
                        });
                    }
                }

                view.SetScreenshotVisible(false);
            }

            var time = 0f;
            const float gravity = 2200f;

            while (time < duration)
            {
                if (skip != null && skip.ConsumeIfPressed()) break;

                var delta = Time.unscaledDeltaTime;
                time += delta;
                var k = Mathf.Clamp01(time / duration);

                for (var i = 0; i < pieces.Count; i++)
                {
                    var piece = pieces[i];
                    piece.velocity.y -= gravity * delta;
                    piece.rect.anchoredPosition += piece.velocity * delta;
                    piece.rotation += piece.angularVelocity * delta;
                    piece.rect.localRotation = Quaternion.Euler(0f, 0f, piece.rotation);

                    var alpha = k < 0.55f ? 1f : Mathf.Clamp01(1f - (k - 0.55f) / 0.45f);
                    piece.graphic.color = new Color(1f, 1f, 1f, alpha);
                    pieces[i] = piece;
                }

                if (flash != null)
                    flash.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - time / (duration * 0.3f)) * 0.9f);

                yield return null;
            }

            for (var i = 0; i < pieces.Count; i++)
            {
                if (pieces[i].rect != null) Object.Destroy(pieces[i].rect.gameObject);
            }

            if (flash != null) Object.Destroy(flash.gameObject);
        }

        public static IEnumerator CrtShutdown(GameOverView view, Texture2D screenshot, float duration,
            GameOverSkipState skip = null)
        {
            if (view == null) yield break;

            duration = Mathf.Max(0.45f, duration);
            var layer = view.ShardLayer;

            var screenRect = GameOverUi.CreateRect("CrtScreen", layer);
            Graphic screenGraphic;

            if (screenshot != null)
            {
                var raw = screenRect.gameObject.AddComponent<RawImage>();
                raw.texture = screenshot;
                raw.raycastTarget = false;
                screenGraphic = raw;
                view.SetScreenshotVisible(false);
            }
            else
            {
                var image = screenRect.gameObject.AddComponent<Image>();
                image.color = new Color(0.72f, 0.75f, 0.78f, 1f);
                image.raycastTarget = false;
                screenGraphic = image;
            }

            var lineRect = GameOverUi.CreateRect("CrtLine", layer);
            lineRect.anchorMin = new Vector2(0f, 0.5f);
            lineRect.anchorMax = new Vector2(1f, 0.5f);
            lineRect.offsetMin = new Vector2(0f, -2f);
            lineRect.offsetMax = new Vector2(0f, 2f);

            var line = lineRect.gameObject.AddComponent<Image>();
            line.color = new Color(1f, 1f, 1f, 0f);
            line.raycastTarget = false;

            var collapseSeconds = duration * 0.4f;
            var pinchSeconds = duration * 0.25f;
            var fadeSeconds = Mathf.Max(0.01f, duration - collapseSeconds - pinchSeconds);

            var time = 0f;
            while (time < collapseSeconds)
            {
                if (skip != null && skip.ConsumeIfPressed()) break;

                time += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(time / collapseSeconds);
                screenRect.localScale = new Vector3(1f, Mathf.Lerp(1f, 0.006f, GameOverUi.EaseInCubic(k)), 1f);
                line.color = new Color(1f, 1f, 1f, k);
                yield return null;
            }

            screenRect.localScale = new Vector3(1f, 0.006f, 1f);
            line.color = Color.white;

            if (skip == null || !skip.Requested)
            {
                time = 0f;
                while (time < pinchSeconds)
                {
                    if (skip != null && skip.ConsumeIfPressed()) break;

                    time += Time.unscaledDeltaTime;
                    var k = GameOverUi.EaseInCubic(Mathf.Clamp01(time / pinchSeconds));
                    screenRect.localScale = new Vector3(Mathf.Lerp(1f, 0.004f, k), 0.006f, 1f);
                    lineRect.localScale = new Vector3(Mathf.Lerp(1f, 0.004f, k), 1f, 1f);
                    yield return null;
                }
            }

            if (skip == null || !skip.Requested)
            {
                time = 0f;
                while (time < fadeSeconds)
                {
                    if (skip != null && skip.ConsumeIfPressed()) break;

                    time += Time.unscaledDeltaTime;
                    var alpha = 1f - Mathf.Clamp01(time / fadeSeconds);
                    line.color = new Color(1f, 1f, 1f, alpha);
                    screenGraphic.color = new Color(1f, 1f, 1f, alpha);
                    yield return null;
                }
            }

            line.color = new Color(1f, 1f, 1f, 0f);
            screenGraphic.color = new Color(1f, 1f, 1f, 0f);

            Object.Destroy(screenRect.gameObject);
            Object.Destroy(lineRect.gameObject);
        }

        public static IEnumerator RemoveScreenEffect(float duration, GameOverSkipState skip = null)
        {
            var startIntensity = ScreenEffect.Intensity;
            var startCurvature = ScreenEffect.Curvature;
            var startScale = ScreenEffect.ScreenScale;
            var startRadius = ScreenEffect.CornerRadius;
            var startStructure = ScreenEffect.StructureStrength;
            var startBlur = ScreenEffect.SubpixelBlur;
            var startScanline = ScreenEffect.ScanlineStrength;
            var startVignette = ScreenEffect.Vignette;

            var seconds = Mathf.Max(0.01f, duration);
            var time = 0f;

            while (time < seconds)
            {
                if (skip != null && skip.ConsumeIfPressed()) break;

                time += Time.unscaledDeltaTime;
                var k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / seconds));

                ScreenEffect.Intensity = Mathf.Lerp(startIntensity, 0f, k);
                ScreenEffect.Curvature = Mathf.Lerp(startCurvature, 0f, k);
                ScreenEffect.ScreenScale = Mathf.Lerp(startScale, 1f, k);
                ScreenEffect.CornerRadius = Mathf.Lerp(startRadius, 0f, k);
                ScreenEffect.StructureStrength = Mathf.Lerp(startStructure, 0f, k);
                ScreenEffect.SubpixelBlur = Mathf.Lerp(startBlur, 0f, k);
                ScreenEffect.ScanlineStrength = Mathf.Lerp(startScanline, 0f, k);
                ScreenEffect.Vignette = Mathf.Lerp(startVignette, 0f, k);

                yield return null;
            }

            ScreenEffect.Intensity = 0f;
            ScreenEffect.Curvature = 0f;
            ScreenEffect.ScreenScale = 1f;
            ScreenEffect.CornerRadius = 0f;
            ScreenEffect.StructureStrength = 0f;
            ScreenEffect.SubpixelBlur = 0f;
            ScreenEffect.ScanlineStrength = 0f;
            ScreenEffect.Vignette = 0f;
        }
    }
}
