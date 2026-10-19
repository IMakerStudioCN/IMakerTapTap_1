using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TapTapFirst.DayChangeShow
{
    /// <summary>
    /// 纯代码构建的全屏过场层：黑底 + 碎屑层 + 数字层。
    /// </summary>
    public class DayChangeView
    {
        public CanvasGroup Root { get; private set; }
        public RectTransform DustRoot { get; private set; }
        public RectTransform NumberRoot { get; private set; }
        public Image Backdrop { get; private set; }
        public TMP_FontAsset Font { get; private set; }

        GameObject mRoot;

        public static DayChangeView Create(DayChangeConfig config)
        {
            if (config == null) config = DayChangeConfig.Load();

            var view = new DayChangeView { Font = ResolveFont(config) };
            view.Build(config);
            return view;
        }

        void Build(DayChangeConfig config)
        {
            mRoot = new GameObject("[DayChangeCanvas]", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            UnityEngine.Object.DontDestroyOnLoad(mRoot);

            var canvas = mRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = config.canvasSortingOrder;

            var scaler = mRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Root = mRoot.AddComponent<CanvasGroup>();
            Root.alpha = 0f;
            Root.interactable = false;
            Root.blocksRaycasts = true;

            var rootRect = (RectTransform)mRoot.transform;

            Backdrop = DayChangeUi.CreateImage("Backdrop", rootRect, config.backdropColor, true);
            DustRoot = DayChangeUi.CreateRect("Dust", rootRect);
            NumberRoot = DayChangeUi.CreateRect("Number", rootRect);
        }

        public void Destroy()
        {
            if (mRoot != null) UnityEngine.Object.Destroy(mRoot);
            mRoot = null;
        }

        static TMP_FontAsset ResolveFont(DayChangeConfig config)
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
