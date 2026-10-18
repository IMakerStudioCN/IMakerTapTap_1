using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TapTapFirst.GameOverShow
{
    public class CreditsRoll
    {
        readonly RectTransform mRoot;
        readonly RectTransform mViewport;
        readonly RectTransform mContent;
        readonly CanvasGroup mGroup;
        readonly TextMeshProUGUI mTitle;
        readonly TextMeshProUGUI mSkipHint;
        readonly TMP_FontAsset mFont;
        readonly List<GameObject> mRows = new List<GameObject>();

        public CreditsRoll(RectTransform parent, TMP_FontAsset font)
        {
            mFont = font;

            mRoot = GameOverUi.CreateRect("Credits", parent);
            mGroup = mRoot.gameObject.AddComponent<CanvasGroup>();
            mGroup.alpha = 0f;
            mGroup.interactable = false;
            mGroup.blocksRaycasts = false;
            mRoot.gameObject.SetActive(false);

            mTitle = GameOverUi.CreateText("Title", mRoot, font, 56f, Color.white);
            var titleRect = (RectTransform)mTitle.transform;
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(1200f, 120f);
            titleRect.anchoredPosition = new Vector2(0f, -80f);

            mViewport = GameOverUi.CreateRect("Viewport", mRoot);
            mViewport.anchorMin = new Vector2(0.1f, 0.08f);
            mViewport.anchorMax = new Vector2(0.9f, 0.84f);
            mViewport.offsetMin = Vector2.zero;
            mViewport.offsetMax = Vector2.zero;
            mViewport.gameObject.AddComponent<RectMask2D>();

            mContent = GameOverUi.CreateRect("Content", mViewport);
            mContent.anchorMin = new Vector2(0f, 0f);
            mContent.anchorMax = new Vector2(1f, 0f);
            mContent.pivot = new Vector2(0.5f, 0f);
            mContent.sizeDelta = new Vector2(0f, 0f);
            mContent.anchoredPosition = Vector2.zero;

            mSkipHint = GameOverUi.CreateText("SkipHint", mRoot, font, 26f, new Color(1f, 1f, 1f, 0.55f));
            var hintRect = (RectTransform)mSkipHint.transform;
            hintRect.anchorMin = new Vector2(0.5f, 0f);
            hintRect.anchorMax = new Vector2(0.5f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.sizeDelta = new Vector2(900f, 60f);
            hintRect.anchoredPosition = new Vector2(0f, 40f);
        }

        public IEnumerator Play(GameOverCreditsSettings settings, GameOverSkipState skip = null)
        {
            if (settings == null) yield break;

            ClearRows();

            mTitle.text = settings.title ?? "";
            mTitle.fontSize = settings.titleFontSize;
            mTitle.color = settings.titleColor;

            var entries = settings.entries ?? new GameOverCreditsEntry[0];
            var lineHeight = Mathf.Max(1f, settings.lineHeight);
            var contentHeight = Mathf.Max(lineHeight, entries.Length * lineHeight) + 80f;

            for (var i = 0; i < entries.Length; i++)
            {
                CreateRow(entries[i], i * lineHeight, lineHeight, settings);
            }

            mContent.sizeDelta = new Vector2(0f, contentHeight);
            mContent.anchoredPosition = new Vector2(0f, -contentHeight);

            mRoot.gameObject.SetActive(true);
            mSkipHint.gameObject.SetActive(settings.allowSkip);
            mSkipHint.text = settings.skipHint ?? "";

            var fadeSeconds = Mathf.Max(0.01f, settings.fadeSeconds);
            yield return GameOverUi.Fade(mGroup, 0f, 1f, fadeSeconds, skip);

            Canvas.ForceUpdateCanvases();
            var viewportHeight = mViewport.rect.height;
            if (viewportHeight <= 1f) viewportHeight = 720f;

            var duration = Mathf.Max(0.5f, settings.scrollSeconds);
            var time = 0f;
            while (time < duration)
            {
                if (settings.allowSkip && (skip != null ? skip.ConsumeIfPressed() : GameOverUi.SkipPressed)) break;

                time += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(time / duration);
                mContent.anchoredPosition = new Vector2(0f, Mathf.Lerp(-contentHeight, viewportHeight, k));
                yield return null;
            }

            yield return GameOverUi.Fade(mGroup, mGroup.alpha, 0f, fadeSeconds, skip);

            mRoot.gameObject.SetActive(false);
            ClearRows();
        }

        void CreateRow(GameOverCreditsEntry entry, float offset, float lineHeight, GameOverCreditsSettings settings)
        {
            var row = GameOverUi.CreateRect("Row", mContent);
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.sizeDelta = new Vector2(0f, lineHeight);
            row.anchoredPosition = new Vector2(0f, -offset);
            mRows.Add(row.gameObject);

            var role = GameOverUi.CreateText("Role", row, mFont, settings.fontSize, settings.color,
                TextAlignmentOptions.Right);
            role.text = entry != null ? entry.role : "";
            role.enableWordWrapping = false;
            var roleRect = (RectTransform)role.transform;
            roleRect.anchorMin = new Vector2(0.5f, 0.5f);
            roleRect.anchorMax = new Vector2(0.5f, 0.5f);
            roleRect.pivot = new Vector2(1f, 0.5f);
            roleRect.sizeDelta = new Vector2(settings.leftColumnWidth, lineHeight);
            roleRect.anchoredPosition = new Vector2(-settings.columnGap * 0.5f, 0f);

            var name = GameOverUi.CreateText("Name", row, mFont, settings.fontSize, settings.color,
                TextAlignmentOptions.Left);
            name.text = entry != null ? entry.name : "";
            name.enableWordWrapping = false;
            var nameRect = (RectTransform)name.transform;
            nameRect.anchorMin = new Vector2(0.5f, 0.5f);
            nameRect.anchorMax = new Vector2(0.5f, 0.5f);
            nameRect.pivot = new Vector2(0f, 0.5f);
            nameRect.sizeDelta = new Vector2(settings.leftColumnWidth, lineHeight);
            nameRect.anchoredPosition = new Vector2(settings.columnGap * 0.5f, 0f);
        }

        void ClearRows()
        {
            for (var i = 0; i < mRows.Count; i++)
            {
                if (mRows[i] != null) Object.Destroy(mRows[i]);
            }

            mRows.Clear();
        }
    }
}
