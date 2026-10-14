using UnityEngine;
using UnityEngine.UI;

using TMPro;

namespace TapTapFirst
{
    /// <summary>
    /// 拖拽公共设施：拖拽层 + 拖动幽灵。
    ///
    /// 拖拽层（TagDragLayer）是 Canvas 下一个铺满的空 RectTransform，
    /// 所有拖动中的副本都挂到它上面，保证：
    ///   - 画在所有 UI 之上
    ///   - 本身不吃射线（blocksRaycasts = false），不会挡住底下的空缺
    /// 整个 Canvas 只创建一份，重复调用会复用。
    /// </summary>
    public static class TagDragUtility
    {
        private const string LayerName = "TagDragLayer";

        /// <summary>取（或创建）这个 Canvas 下的拖拽层。</summary>
        public static RectTransform GetLayer(Canvas canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            // 先找已有的（不同组件重复创建会导致多层叠加）
            Transform[] children = canvas.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in children)
            {
                if (child != null && child.name == LayerName && child.parent == canvas.transform)
                {
                    return child as RectTransform;
                }
            }

            GameObject go = new GameObject(LayerName, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.SetAsLastSibling();

            CanvasGroup group = go.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 1f;

            return rect;
        }

        /// <summary>
        /// 创建一个可见的拖动幽灵：半透明底 + 文字。
        /// 挂了 mDragGhostPrefab 时会用预制体，否则现建一个能看得见的。
        /// </summary>
        public static TagDisplay CreateGhost(RectTransform layer, GameObject ghostPrefab,
            string text, Vector2 size, float alpha, TMP_FontAsset font)
        {
            if (layer == null)
            {
                return null;
            }

            TagDisplay ghost;

            if (ghostPrefab != null)
            {
                GameObject instance = Object.Instantiate(ghostPrefab, layer);
                instance.name = "TagGhost";
                ghost = instance.GetComponent<TagDisplay>();

                if (ghost == null)
                {
                    ghost = instance.AddComponent<TagDisplay>();
                }

                RectTransform prefabRect = instance.transform as RectTransform;

                if (prefabRect != null)
                {
                    prefabRect.anchorMin = new Vector2(0.5f, 0.5f);
                    prefabRect.anchorMax = new Vector2(0.5f, 0.5f);
                    prefabRect.pivot = new Vector2(0.5f, 0.5f);
                }
            }
            else
            {
                ghost = BuildRuntimeGhost(layer, text, size, font);
            }

            if (ghost == null)
            {
                return null;
            }

            ghost.Owner = null;
            ghost.SetPreviewText(text);

            CanvasGroup group = ghost.GetComponent<CanvasGroup>();

            if (group == null)
            {
                group = ghost.gameObject.AddComponent<CanvasGroup>();
            }

            group.alpha = alpha;
            group.blocksRaycasts = false;
            group.interactable = false;

            RectTransform rect = ghost.transform as RectTransform;

            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.SetAsLastSibling();
            }

            return ghost;
        }

        /// <summary>没有预制体时，现建一个看得见的幽灵：半透明底 + 居中文字。</summary>
        private static TagDisplay BuildRuntimeGhost(RectTransform layer, string text, Vector2 size,
            TMP_FontAsset font)
        {
            GameObject go = new GameObject("TagGhost", typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(layer, false);
            rect.sizeDelta = size.x > 1f ? size : new Vector2(200f, 60f);

            Image bg = go.AddComponent<Image>();
            bg.color = new Color(0.24f, 0.45f, 0.85f, 0.85f);
            bg.raycastTarget = false;

            GameObject label = new GameObject("Label", typeof(RectTransform));
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 4f);
            labelRect.offsetMax = new Vector2(-8f, -4f);

            TextMeshProUGUI tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = Mathf.Min(size.y * 0.5f, 32f);
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            if (font != null)
            {
                tmp.font = font;
            }

            TagDisplay display = go.AddComponent<TagDisplay>();
            display.SetPreviewText(text);

            return display;
        }

        /// <summary>取 Canvas 的渲染相机（Overlay 模式返回 null）。</summary>
        public static Camera GetCamera(Canvas canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }
    }
}