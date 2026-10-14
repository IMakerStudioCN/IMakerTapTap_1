using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using TMPro;

namespace TapTapFirst
{
    /// <summary>
    /// Tag 列表里的一个【可选 Tag】。
    ///
    /// 关键点：它不会被"消耗"。同一个 Tag 可以被拖到多个空缺里，
    /// 因为拖动时是在拖拽层里生成一份【显示副本】，原项始终留在列表里。
    ///
    /// 拖动流程：
    ///   按下 -> 生成副本跟着鼠标 -> 松手在某个空缺上则填入该空缺，否则销毁副本
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class TagOption : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("这是个什么 Tag")]
        [SerializeField] private Tag_SO mTagData;

        [Tooltip("列表里显示的文本；留空则用 Tag 的名字")]
        [SerializeField] private string mDisplayOverride;

        [Header("拖动")]
        [Tooltip("拖动副本相对鼠标的偏移")]
        [SerializeField] private Vector2 mDragOffset = Vector2.zero;

        [Tooltip("拖动时显示的副本预制体；留空则自动生成一个带文字的半透明幽灵")]
        [SerializeField] private GameObject mDragGhostPrefab;

        [Tooltip("拖动时副本的透明度")]
        [Range(0.2f, 1f)]
        [SerializeField] private float mDragAlpha = 0.9f;

        [Header("日志")]
        [SerializeField] private bool mLog;

        private RectTransform mRect;
        private RectTransform mDragLayer;
        private Canvas mCanvas;

        private TagDisplay mGhost;          // 拖动中的副本
        private Camera mDragCamera;
        private Vector2 mGhostLocalAtPress; // 按下时鼠标在拖拽层局部空间的位置

        /// <summary>这个选项对应的 Tag 配置。</summary>
        public Tag_SO TagData => mTagData;

        /// <summary>列表里显示的文本。</summary>
        public string DisplayText => !string.IsNullOrEmpty(mDisplayOverride)
            ? mDisplayOverride
            : (mTagData != null ? mTagData.tagName : name);

        public bool IsDragging => mGhost != null;

        private void Awake()
        {
            mRect = (RectTransform)transform;
            mCanvas = GetComponentInParent<Canvas>();

            RefreshLabel();
        }

        /// <summary>运行时设置数据。</summary>
        public void Setup(Tag_SO data, string displayOverride = null)
        {
            mTagData = data;

            if (displayOverride != null)
            {
                mDisplayOverride = displayOverride;
            }

            RefreshLabel();
        }

        /// <summary>把列表里显示的文本刷成 Tag 的名字。</summary>
        public void RefreshLabel()
        {
            string text = DisplayText;

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

        // ============================================================
        //  拖拽
        // ============================================================

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (mTagData == null)
            {
                Debug.LogWarning($"[TagOption/{name}] 没有配置 Tag_SO，无法拖动");
                return;
            }

            EnsureDragLayer();

            if (mDragLayer == null)
            {
                return;
            }

            mDragCamera = mCanvas != null && mCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? mCanvas.worldCamera
                : null;

            mGhost = CreateGhost();

            if (mGhost == null)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mDragLayer, eventData.position, eventData.pressEventCamera, out mGhostLocalAtPress);

            mGhost.RectTransform.anchoredPosition = mGhostLocalAtPress + mDragOffset;

            LogDiag($"开始拖动 {DisplayText}");
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (mGhost == null)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mDragLayer, eventData.position, mDragCamera, out Vector2 pointerLocal);

            mGhost.RectTransform.anchoredPosition = pointerLocal + mDragOffset;

            // 悬停高亮
            TagKindSlot hover = FindSlotUnder(eventData);
            SetHover(hover);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (mGhost == null)
            {
                return;
            }

            TagKindSlot target = FindDropTarget(eventData);   // 落点判定更严格
            SetHover(null);

            if (target != null)
            {
                // 已填的空缺：先清掉旧的，再放新的（= 替换）
                target.Place(mTagData, this);
                LogDiag($"填入空缺 {target.SlotId}");
            }
            else
            {
                LogDiag("没命中空缺，副本已丢弃（Tag 仍在列表里）");
            }

            Destroy(mGhost.gameObject);
            mGhost = null;
        }

        // ============================================================
        //  内部
        // ============================================================

                private TagDisplay CreateGhost()
        {
            // 幽灵必须"看得见"：之前只建了空 GameObject + TagDisplay 组件，
            // 既没有 Image 也没有文字，等于一个隐身物体跟着鼠标，玩家什么都看不到。
            TMP_FontAsset font = null;
            TMP_Text ownLabel = GetComponentInChildren<TMP_Text>(true);

            if (ownLabel != null)
            {
                font = ownLabel.font;
            }

            return TagDragUtility.CreateGhost(
                mDragLayer,
                mDragGhostPrefab,
                DisplayText,
                mRect.rect.size,
                mDragAlpha,
                font);
        }

        /// <summary>鼠标下方的空缺（矩形内优先，否则取最近且在吸附半径内的）。</summary>
        /// <summary>鼠标下的空缺（高亮用：允许一点余量，方便看出"会吸到哪"）。</summary>
        private TagKindSlot FindSlotUnder(PointerEventData eventData)
        {
            return FindSlotUnder(eventData, SnapRadiusForHover);
        }

        /// <summary>松手落点：必须鼠标真的在空缺框里（或极小的容差内），避免"路过就被挂上"。</summary>
        private TagKindSlot FindDropTarget(PointerEventData eventData)
        {
            return FindSlotUnder(eventData, DropTolerance);
        }

        private const float SnapRadiusForHover = 140f;   // 高亮提示用
        private const float DropTolerance = 12f;         // 真正落点的容差

        private TagKindSlot FindSlotUnder(PointerEventData eventData, float tolerance)
        {
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

            return nearest != null && nearestDistance <= tolerance ? nearest : null;
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

                private void EnsureDragLayer()
        {
            if (mCanvas == null)
            {
                mCanvas = GetComponentInParent<Canvas>();
            }

            if (mCanvas == null)
            {
                Debug.LogWarning($"[TagOption/{name}] 找不到 Canvas");
                return;
            }

            // 整个 Canvas 共用一份拖拽层，重复创建会导致多层叠加、互相干扰
            mDragLayer = TagDragUtility.GetLayer(mCanvas);
        }

        /// <summary>就地打印一条诊断日志。</summary>
        public void LogDiag(string message)
        {
            if (mLog)
            {
                Debug.Log($"[TagOption/{name}] {message}");
            }
        }
    }
}
