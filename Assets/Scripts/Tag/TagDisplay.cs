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
        IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
    {
        [Header("清空按钮（可选）。点它等于把该空缺清空")]
        [SerializeField] private Button mClearButton;

        [Header("拖动")]
        [SerializeField] private Vector2 mDragOffset = Vector2.zero;

        [Tooltip("拖出去时显示的副本预制体；留空则自动生成半透明幽灵")]
        [SerializeField] private GameObject mDragGhostPrefab;
        [Range(0.2f, 1f)]
        [SerializeField] private float mDragAlpha = 0.9f;

        [Header("诊断")]
        [Tooltip("打开后打印每次指针事件，用于排查点不动/拖不动的问题")]
        [SerializeField] private bool mLogEvents;

        private RectTransform mRect;
        private RectTransform mDragLayer;
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
            DestroyGhost();
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

        /// <summary>只为诊断：确认射线有没有打到这个显示件。</summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            LogEvent($"OnPointerDown 命中自己（Owner={(Owner != null ? Owner.name : "null")} tagId={(TagData != null ? TagData.tagId : -1)}）");
        }

        /// <summary>全局诊断开关：打开后所有 TagDisplay 都会打印指针事件（不用改预制体）。</summary>
        public static bool GlobalLogEvents;

        /// <summary>是否开启本实例的诊断日志。</summary>
        public bool LogEventsEnabled
        {
            get => mLogEvents || GlobalLogEvents;
            set => mLogEvents = value;
        }

        private void LogEvent(string message)
        {
            if (mLogEvents || GlobalLogEvents)
            {
                Debug.Log($"[TagDisplay/{name}] {message}");
            }
        }

        /// <summary>打印这个显示件的完整状态（右键菜单 / 诊断用）。</summary>
        [ContextMenu("打印显示件诊断")]
        public void DumpState()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"===== TagDisplay 诊断 {name} =====");
            sb.AppendLine($"Owner={(Owner != null ? Owner.name : "null")}  tagId={(TagData != null ? TagData.tagId : -1)}  activeInHierarchy={gameObject.activeInHierarchy}");
            sb.AppendLine($"interactable/raycast: 自身 Graphic={(GetComponent<Graphic>() != null)} 子物体数={transform.childCount}");

            CanvasGroup group = GetComponent<CanvasGroup>();
            sb.AppendLine($"CanvasGroup={(group != null ? "有, blocksRaycasts=" + group.blocksRaycasts : "无")}");

            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            foreach (Graphic g in graphics)
            {
                sb.AppendLine($"    Graphic {g.GetType().Name} on {g.name} raycastTarget={g.raycastTarget} enabled={g.enabled}");
            }

            sb.AppendLine($"实现接口: IBeginDrag={this is IBeginDragHandler} IDrag={this is IDragHandler} IEndDrag={this is IEndDragHandler} IPointerDown={this is IPointerDownHandler}");
            sb.AppendLine($"父级链: {BuildPath(transform)}");
            sb.AppendLine("=========================");
            Debug.Log(sb.ToString());
        }

        private static string BuildPath(Transform t)
        {
            string path = t.name;
            Transform current = t.parent;

            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (Owner == null || TagData == null)
            {
                LogEvent($"OnBeginDrag 被忽略（Owner={(Owner != null ? Owner.name : "null")} TagData={(TagData != null ? "有" : "null")}）——这是拖拽副本或没归属");
                return;   // 这个是拖拽副本本身，不参与
            }

            LogEvent("OnBeginDrag 开始");

            mDragStartedFrame = Time.frameCount;

            EnsureDragLayer();

            if (mDragLayer == null)
            {
                return;
            }

            Tag_SO tag = TagData;
            TagOption source = Owner.SourceOption;
            TagOfWords wordTypeAtPickup = Owner.WordType;
            TagKindSlot slot = Owner;
            Vector2 ghostSize = mRect.rect.size;

            // 只把空缺标记为空，绝不能调用 Owner.Clear()：
            // Clear() 会 Destroy 掉"正在执行 OnBeginDrag 的自己"，
            // EventSystem 随即找不到 pointerDrag，OnEndDrag 永不触发，拖动幽灵会永久留在屏幕上。
            slot.MarkEmptied();

            TMP_FontAsset font = GetComponentInChildren<TMP_Text>(true)?.font;
            string previewText = Tag_SO.GetDisplayContent(tag, wordTypeAtPickup);

            TagDisplay ghost = TagDragUtility.CreateGhost(
                mDragLayer, mDragGhostPrefab, previewText, ghostSize, mDragAlpha, font);

            if (ghost == null)
            {
                return;
            }

            ghost.Owner = null;
            ghost.SetTag(tag);
            ghost.SetPreviewText(previewText);

            mGhost = ghost;
            mGhostSource = source;
            mGhostTag = tag;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mDragLayer, eventData.position, eventData.pressEventCamera, out mLocalAtPress);

            ghost.RectTransform.anchoredPosition = mLocalAtPress + mDragOffset;

            // 幽灵已经在跟着鼠标了，把空缺里原来那份显示副本收掉（延迟销毁，下一帧才真正移除）
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>销毁当前拖动幽灵（重复调用安全）。</summary>
        private void DestroyGhost()
        {
            if (mGhost == null)
            {
                return;
            }

            if (mGhost.gameObject != null)
            {
                Destroy(mGhost.gameObject);
            }

            mGhost = null;
            mGhostTag = null;
            mGhostSource = null;
        }

        private void OnDisable()
        {
            // 拖拽中途被禁用时，别把幽灵落在场上
            DestroyGhost();
        }

        private TagDisplay mGhost;
        private TagOption mGhostSource;
        private Tag_SO mGhostTag;

        public void OnDrag(PointerEventData eventData)
        {
            if (mGhost == null)
            {
                LogEvent("OnDrag 但 mGhost 为空（说明 OnBeginDrag 没成功建幽灵）");
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
                LogEvent("OnEndDrag 但 mGhost 为空");
                return;
            }

            LogEvent("OnEndDrag 结束拖拽");

            TagKindSlot target = FindDropTarget(eventData);   // 落点判定更严格
            SetHover(null);

            if (target != null)
            {
                target.Place(mGhostTag, mGhostSource);
            }
            // 没命中任何空缺 -> 保持"已清空"，Tag 仍在列表里可再次使用

            DestroyGhost();
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

        /// <summary>鼠标下的空缺（高亮用，允许余量）。</summary>
        private TagKindSlot FindSlotUnder(PointerEventData eventData)
        {
            return FindSlotUnder(eventData, SnapRadiusForHover);
        }

        /// <summary>松手落点：必须鼠标真在空缺框里，避免"路过就被挂上"。</summary>
        private TagKindSlot FindDropTarget(PointerEventData eventData)
        {
            return FindSlotUnder(eventData, DropTolerance);
        }

        private const float SnapRadiusForHover = 140f;
        private const float DropTolerance = 12f;

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

        private void EnsureDragLayer()
        {
            if (mCanvas == null)
            {
                mCanvas = GetComponentInParent<Canvas>();
            }

            if (mCanvas == null)
            {
                return;
            }

            mDragCamera = TagDragUtility.GetCamera(mCanvas);

            // 整个 Canvas 共用一份拖拽层，重复创建会导致多层叠加、互相干扰
            mDragLayer = TagDragUtility.GetLayer(mCanvas);
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
