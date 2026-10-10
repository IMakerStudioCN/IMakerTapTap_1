using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using TMPro;

namespace TapTapFirst
{
    /// <summary>
    /// 空缺里显示的那份 Tag（由 TagKindSlot 在运行时创建）。
    ///
    /// 它既是"显示"，也是"改主意"的入口：
    ///   - 点一下       -> 清空该空缺（Tag 仍留在列表里）
    ///   - 拖出去       -> 从该空缺移走；拖到别的空缺上则移动过去
    ///   - 可选清空按钮 -> 同上（想做得显式就挂一个 Button 并填 mClearButton）
    /// </summary>
    [DisallowMultipleComponent]
    public class TagDisplay : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("清空按钮（可选）。点它等于把该空缺清空")]
        [SerializeField] private Button mClearButton;

        [Header("拖动")]
        [SerializeField] private Vector2 mDragOffset = Vector2.zero;
        [Range(0.2f, 1f)]
        [SerializeField] private float mDragAlpha = 0.9f;

        private RectTransform mRect;
        private RectTransform mDragLayer;
        private CanvasGroup mDragLayerGroup;
        private Canvas mCanvas;
        private Camera mDragCamera;
        private Vector2 mLocalAtPress;
        private int mDragStartedFrame = -1;

        /// <summary>所属空缺。</summary>
        public TagKindSlot Owner { get; set; }

        /// <summary>当前显示的 Tag 配置。</summary>
        public Tag_SO TagData { get; private set; }

        /// <summary>本体（非副本）的 RectTransform。</summary>
        public RectTransform RectTransform => mRect != null ? mRect : (mRect = (RectTransform)transform);

        /// <summary>拖动时用的临时父节点（拖动期间挂到这里）。</summary>
        private Transform mOriginalParent;

        private void Awake()
        {
            mRect = (RectTransform)transform;
            mCanvas = GetComponentInParent<Canvas>();

            EnsureClickable();

            if (mClearButton != null)
            {
                mClearButton.onClick.AddListener(OnClearClicked);
            }
        }

        private void OnDestroy()
        {
            if (mClearButton != null)
            {
                mClearButton.onClick.RemoveListener(OnClearClicked);
            }
        }

        /// <summary>运行时 / 编辑器里配置（生成器用）。</summary>
        public void ApplySetup(Button clearButton)
        {
            if (mClearButton != null)
            {
                mClearButton.onClick.RemoveListener(OnClearClicked);
            }

            mClearButton = clearButton;

            if (mClearButton != null)
            {
                mClearButton.onClick.RemoveListener(OnClearClicked);
                mClearButton.onClick.AddListener(OnClearClicked);
            }
        }
        /// <summary>设置要显示的 Tag。文本内容由所属空缺的种类决定。</summary>
        public void SetTag(Tag_SO tag)
        {
            TagData = tag;
            ApplyText();
        }

        /// <summary>拖动副本用：直接指定显示文本。</summary>
        public void SetPreviewText(string text)
        {
            ApplyText(text);
        }

        /// <summary>按所属空缺的种类刷新显示文本。</summary>
        public void ApplyText(string overrideText = null)
        {
            string text = overrideText;

            if (text == null)
            {
                if (TagData == null)
                {
                    text = string.Empty;
                }
                else
                {
                    TagOfWords wordType = Owner != null ? Owner.WordType : TagData.tagOfWords;
                    text = Tag_SO.GetDisplayContent(TagData, wordType);
                }
            }

            TMP_Text tmp = GetComponentInChildren<TMP_Text>(true);

            if (tmp != null)
            {
                tmp.text = text;
                return;
            }

            Text uiText = GetComponentInChildren<Text>(true);

            if (uiText != null)
            {
                uiText.text = text;
            }
        }

        private void OnClearClicked()
        {
            // 记一帧，避免这次点击继续冒泡到空缺又被当成"点空缺清空"（等于清两次）
            mClearClickedFrame = Time.frameCount;
            Owner?.Clear();
        }

        private int mClearClickedFrame = -1;

        /// <summary>公开版：供 TagKindSlot 判断这次按下是否落在清空按钮上。</summary>
        public bool IsPointerOnClearButtonPublic(PointerEventData eventData)
        {
            return IsPointerOnClearButton(eventData);
        }
        /// <summary>这次按下是不是落在清空按钮上。</summary>
        private bool IsPointerOnClearButton(PointerEventData eventData)
        {
            if (mClearButton == null)
            {
                return false;
            }

            if (mClearClickedFrame == Time.frameCount)
            {
                return true;
            }

            return eventData.pointerEnter != null &&
                   eventData.pointerEnter.transform.IsChildOf(mClearButton.transform);
        }

        /// <summary>
        /// 这一次按下是否已经被当作"拖走"处理。
        /// TagKindSlot.OnPointerDown 用它区分"点击清空"和"拖出去"。
        /// </summary>
        public bool ConsumeDragStarted()
        {
            if (mDragStartedFrame == Time.frameCount)
            {
                mDragStartedFrame = -1;
                return true;
            }

            return false;
        }

        // ============================================================
        //  拖出去（把 Tag 从这个空缺移走）
        // ============================================================

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (Owner == null || TagData == null)
            {
                return;   // 这个是拖拽副本本身，不参与
            }

            mDragStartedFrame = Time.frameCount;

            EnsureDragLayer();

            if (mDragLayer == null)
            {
                return;
            }

            Tag_SO tag = TagData;
            TagOption source = Owner.SourceOption;
            TagOfWords wordTypeAtPickup = Owner.WordType;   // 必须在 Clear() 之前取好，Clear 之后 Owner 就没了

            // 先把空缺清掉（副本会被销毁），再在拖拽层生成一份新的副本跟着鼠标
            Owner.Clear();

            GameObject go = new GameObject("TagGhost", typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(mDragLayer, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = mRect.rect.size;
            rect.SetAsLastSibling();

            CanvasGroup group = go.AddComponent<CanvasGroup>();
            group.alpha = mDragAlpha;
            group.blocksRaycasts = false;
            group.interactable = false;

            TagDisplay ghost = go.AddComponent<TagDisplay>();
            ghost.Owner = null;
            ghost.SetTag(tag);
            ghost.SetPreviewText(Tag_SO.GetDisplayContent(tag, wordTypeAtPickup));

            mGhost = ghost;
            mGhostSource = source;
            mGhostTag = tag;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mDragLayer, eventData.position, eventData.pressEventCamera, out mLocalAtPress);

            ghost.RectTransform.anchoredPosition = mLocalAtPress + mDragOffset;
        }

        private TagDisplay mGhost;
        private TagOption mGhostSource;
        private Tag_SO mGhostTag;

        public void OnDrag(PointerEventData eventData)
        {
            if (mGhost == null)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mDragLayer, eventData.position, mDragCamera, out Vector2 pointerLocal);

            mGhost.RectTransform.anchoredPosition = pointerLocal + mDragOffset;

            SetHover(FindSlotUnder(eventData));
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (mGhost == null)
            {
                return;
            }

            TagKindSlot target = FindSlotUnder(eventData);
            SetHover(null);

            if (target != null)
            {
                target.Place(mGhostTag, mGhostSource);
            }
            // 没命中任何空缺 -> 保持"已清空"，Tag 仍在列表里可再次使用

            Destroy(mGhost.gameObject);
            mGhost = null;
            mGhostTag = null;
            mGhostSource = null;
        }

        private TagKindSlot mHoverSlot;

        private void SetHover(TagKindSlot slot)
        {
            if (mHoverSlot == slot)
            {
                return;
            }

            mHoverSlot?.SetHighlight(false);
            mHoverSlot = slot;
            mHoverSlot?.SetHighlight(true);
        }

        private TagKindSlot FindSlotUnder(PointerEventData eventData)
        {
            const float snapDistance = 140f;

            TagKindSlot inside = null;
            TagKindSlot nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (TagKindSlot slot in TagKindSlot.AllSlots)
            {
                if (slot == null || !slot.isActiveAndEnabled) continue;
                if (slot.IsFilled) continue;

                RectTransform rect = slot.transform as RectTransform;

                if (rect == null) continue;

                if (RectTransformUtility.RectangleContainsScreenPoint(rect, eventData.position, mDragCamera))
                {
                    if (inside == null)
                    {
                        inside = slot;
                    }

                    continue;
                }

                float distance = DistanceToRect(rect, eventData.position, mDragCamera);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = slot;
                }
            }

            if (inside != null)
            {
                return inside;
            }

            return nearest != null && nearestDistance <= snapDistance ? nearest : null;
        }

        private static float DistanceToRect(RectTransform rect, Vector2 pointer, Camera camera)
        {
            Vector2 center = RectTransformUtility.WorldToScreenPoint(camera, rect.position);

            float halfW = rect.rect.width * 0.5f * Mathf.Abs(rect.lossyScale.x);
            float halfH = rect.rect.height * 0.5f * Mathf.Abs(rect.lossyScale.y);

            float dx = Mathf.Max(0f, Mathf.Abs(pointer.x - center.x) - halfW);
            float dy = Mathf.Max(0f, Mathf.Abs(pointer.y - center.y) - halfH);

            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private void EnsureDragLayer()
        {
            if (mDragLayer != null)
            {
                return;
            }

            if (mCanvas == null)
            {
                mCanvas = GetComponentInParent<Canvas>();
            }

            if (mCanvas == null)
            {
                return;
            }

            mDragCamera = mCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mCanvas.worldCamera;

            GameObject go = new GameObject("TagDragLayer", typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(mCanvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsLastSibling();

            mDragLayerGroup = go.AddComponent<CanvasGroup>();
            mDragLayerGroup.blocksRaycasts = false;
            mDragLayerGroup.interactable = false;

            mDragLayer = rect;
        }

        private void EnsureClickable()
        {
            if (GetComponent<Graphic>() != null)
            {
                return;
            }

            Image image = gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;
        }
    }
}