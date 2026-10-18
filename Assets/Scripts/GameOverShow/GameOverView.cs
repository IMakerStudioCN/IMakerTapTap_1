using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TapTapFirst.GameOverShow
{
    public class GameOverView
    {
        public GameOverConfig Config { get; private set; }
        public CreditsRoll Credits { get; private set; }
        public RectTransform ShardLayer { get; private set; }
        public RawImage Screenshot { get; private set; }
        public CanvasGroup PictureGroup { get; private set; }

        GameObject mRoot;
        Image mBackdrop;
        Image mPicture;
        RectTransform mTextRoot;
        CanvasGroup mTextGroup;
        TextMeshProUGUI mTitle;
        TextMeshProUGUI mSubtitle;
        TextMeshProUGUI mSkipHint;
        RectTransform mButtonRoot;
        TMP_FontAsset mFont;

        public static GameOverView Create(GameOverConfig config)
        {
            if (config == null) config = GameOverConfig.Load();
            config.EnsureDefaults();

            var view = new GameOverView { Config = config, mFont = ResolveFont(config) };
            view.Build();
            return view;
        }

        void Build()
        {
            EnsureEventSystem();

            mRoot = new GameObject("[GameOverCanvas]", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            UnityEngine.Object.DontDestroyOnLoad(mRoot);

            var canvas = mRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = Config.canvasSortingOrder;

            var scaler = mRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var rootGroup = mRoot.AddComponent<CanvasGroup>();
            rootGroup.alpha = 1f;
            rootGroup.interactable = true;
            rootGroup.blocksRaycasts = true;

            var rootRect = (RectTransform)mRoot.transform;

            mBackdrop = GameOverUi.CreateImage("Backdrop", rootRect, Color.black, true);

            var screenshotRect = GameOverUi.CreateRect("Screenshot", rootRect);
            Screenshot = screenshotRect.gameObject.AddComponent<RawImage>();
            Screenshot.raycastTarget = false;
            Screenshot.enabled = false;

            ShardLayer = GameOverUi.CreateRect("Shards", rootRect);

            mPicture = GameOverUi.CreateImage("Picture", rootRect, Color.white);
            mPicture.preserveAspect = true;
            mPicture.enabled = false;
            PictureGroup = mPicture.gameObject.AddComponent<CanvasGroup>();
            PictureGroup.alpha = 0f;
            PictureGroup.interactable = false;
            PictureGroup.blocksRaycasts = false;

            mTextRoot = GameOverUi.CreateRect("Text", rootRect);
            mTextGroup = mTextRoot.gameObject.AddComponent<CanvasGroup>();
            mTextGroup.alpha = 0f;
            mTextGroup.interactable = false;
            mTextGroup.blocksRaycasts = false;

            mTitle = GameOverUi.CreateText("Title", mTextRoot, mFont, 96f, Color.white);
            var titleRect = (RectTransform)mTitle.transform;
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.sizeDelta = new Vector2(1700f, 320f);
            titleRect.anchoredPosition = new Vector2(0f, 70f);

            mSubtitle = GameOverUi.CreateText("Subtitle", mTextRoot, mFont, 36f, new Color(0.85f, 0.85f, 0.85f, 1f));
            var subtitleRect = (RectTransform)mSubtitle.transform;
            subtitleRect.anchorMin = new Vector2(0.5f, 0.5f);
            subtitleRect.anchorMax = new Vector2(0.5f, 0.5f);
            subtitleRect.sizeDelta = new Vector2(1500f, 260f);
            subtitleRect.anchoredPosition = new Vector2(0f, -150f);

            Credits = new CreditsRoll(rootRect, mFont);

            mSkipHint = GameOverUi.CreateText("SkipHint", rootRect, mFont, 26f, new Color(1f, 1f, 1f, 0.55f));
            var skipHintRect = (RectTransform)mSkipHint.transform;
            skipHintRect.anchorMin = new Vector2(0.5f, 0f);
            skipHintRect.anchorMax = new Vector2(0.5f, 0f);
            skipHintRect.pivot = new Vector2(0.5f, 0f);
            skipHintRect.sizeDelta = new Vector2(1100f, 60f);
            skipHintRect.anchoredPosition = new Vector2(0f, 40f);
            mSkipHint.text = "";

            mButtonRoot = GameOverUi.CreateRect("Buttons", rootRect);
            mButtonRoot.gameObject.SetActive(false);
        }

        public void SetBackdrop(Color color, float alpha)
        {
            if (mBackdrop == null) return;
            color.a = Mathf.Clamp01(alpha);
            mBackdrop.color = color;
        }

        public void SetScreenshot(Texture2D texture)
        {
            if (Screenshot == null) return;
            Screenshot.texture = texture;
            Screenshot.color = Color.white;
            Screenshot.enabled = texture != null;
            ((RectTransform)Screenshot.transform).localScale = Vector3.one;
        }

        public void SetScreenshotVisible(bool visible)
        {
            if (Screenshot == null) return;
            Screenshot.enabled = visible && Screenshot.texture != null;
        }

        public void SetPicture(Sprite sprite)
        {
            if (mPicture == null) return;
            mPicture.sprite = sprite;
            mPicture.enabled = sprite != null;
            PictureGroup.alpha = 0f;
        }

        public void HidePicture()
        {
            if (mPicture == null) return;
            mPicture.enabled = false;
            mPicture.sprite = null;
        }

        public void ShowSkipHint(bool show, string text)
        {
            if (mSkipHint == null) return;
            mSkipHint.text = show ? (text ?? "") : "";
        }

        public IEnumerator PlayText(GameOverEndingPreset preset, GameOverSkipState skip = null)
        {
            if (preset == null) yield break;

            mTitle.text = preset.title ?? "";
            mTitle.color = preset.titleColor;
            mTitle.fontSize = preset.titleFontSize;

            mSubtitle.text = preset.subtitle ?? "";
            mSubtitle.color = preset.subtitleColor;
            mSubtitle.fontSize = preset.subtitleFontSize;

            var startScale = Mathf.Clamp(preset.textStartScale, 0.01f, 1f);
            var growSeconds = Mathf.Max(0.01f, preset.textGrowSeconds);

            mTextGroup.alpha = 0f;
            mTextRoot.localScale = Vector3.one * startScale;

            var time = 0f;
            while (time < growSeconds)
            {
                if (skip != null && skip.ConsumeIfPressed())
                {
                    mTextGroup.alpha = 0f;
                    yield break;
                }

                time += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(time / growSeconds);
                mTextRoot.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, GameOverUi.EaseOutCubic(k));
                mTextGroup.alpha = k;
                yield return null;
            }

            mTextRoot.localScale = Vector3.one;
            mTextGroup.alpha = 1f;

            yield return GameOverUi.WaitSeconds(preset.textHoldSeconds, skip);

            if (skip != null && skip.Requested)
            {
                mTextGroup.alpha = 0f;
                yield break;
            }

            var fadeSeconds = Mathf.Max(0f, preset.textFadeSeconds);
            if (fadeSeconds > 0f) yield return GameOverUi.Fade(mTextGroup, 1f, 0f, fadeSeconds, skip);
            else mTextGroup.alpha = 0f;
        }

        public IEnumerator ShowEndButtons(GameOverConfig config, Action onRestart, Action onQuit)
        {
            if (mButtonRoot == null) yield break;

            mButtonRoot.gameObject.SetActive(true);

            var group = mButtonRoot.GetComponent<CanvasGroup>();
            if (group == null) group = mButtonRoot.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            CreateButton("Restart", config.restartButtonText, new Vector2(-220f, -260f), onRestart);
            CreateButton("Quit", config.quitButtonText, new Vector2(220f, -260f), onQuit);

            yield return GameOverUi.Fade(group, 0f, 1f, Mathf.Max(0.01f, config.buttonFadeSeconds));

            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        void CreateButton(string name, string label, Vector2 position, Action onClick)
        {
            var rect = GameOverUi.CreateRect(name, mButtonRoot);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(360f, 96f);
            rect.anchoredPosition = position;

            var background = rect.gameObject.AddComponent<Image>();
            background.color = Config.buttonColor;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.75f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;

            if (onClick != null) button.onClick.AddListener(() => onClick());

            var text = GameOverUi.CreateText("Label", rect, mFont, Config.buttonFontSize, Config.buttonTextColor);
            text.text = label ?? "";
            text.enableWordWrapping = false;
        }

        public void Destroy()
        {
            if (mRoot != null) UnityEngine.Object.Destroy(mRoot);
            mRoot = null;
        }

        static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null) return;

            var go = new GameObject("[GameOverEventSystem]", typeof(EventSystem), typeof(StandaloneInputModule));
            UnityEngine.Object.DontDestroyOnLoad(go);
        }

        static TMP_FontAsset ResolveFont(GameOverConfig config)
        {
            if (config != null && config.font != null) return config.font;

            var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            TMP_FontAsset first = null;

            for (var i = 0; i < fonts.Length; i++)
            {
                var font = fonts[i];
                if (font == null) continue;
                if (font.name.IndexOf("Terrarum", StringComparison.OrdinalIgnoreCase) >= 0) return font;
                if (first == null) first = font;
            }

            if (TMP_Settings.defaultFontAsset != null) return TMP_Settings.defaultFontAsset;
            return first;
        }
    }
}
