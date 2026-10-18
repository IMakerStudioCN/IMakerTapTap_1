using QFramework;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TapTapFirst
{
    /// <summary>
    /// Tag 元件的三种状态：
    ///
    ///   InSourceList  在初始列表里（游戏开始时的状态）
    ///   Dragging      被鼠标拖着
    ///   InSlot        已经放进槽位
    ///
    /// 规则：
    ///   - 游戏开始时 Tag 固定在初始列表（TagSourceList）里的初始位置
    ///   - 拖动时只能被放到槽位（DropSlot）上；没放到槽位就自动飞回初始位置
    ///   - 从槽位里点住再拖出来，松手同样飞回初始位置
    ///
    /// 拖拽跟随在"拖拽层（Canvas 下的空 RectTransform）的局部空间"里做增量，
    /// 不用屏幕坐标当世界坐标 —— 嵌套 Canvas 下那样会产生固定偏移。
    /// </summary>
    public class TagSingle : MonoBehaviour,
        IBeginDragHandler, IInitializePotentialDragHandler, IDragHandler, IEndDragHandler
    {
        public enum Stage
        {
            InSourceList,
            Dragging,
            InSlot
        }

        private TextMeshPro m_TextMeshPro;

        private string m_Tag;

        [Header("引用")]
        [SerializeField] private Canvas mCanvas;

        [Header("吸附")]
        [Tooltip("吸附阈值（屏幕像素）。判定的是到槽位矩形的最近距离；2560 宽画面建议 100~200")]
        [SerializeField] private float mSnapDistance = 120f;

        [Tooltip("鼠标离开原槽位矩形这么多像素后，才允许再放回原槽位（防止一拿起又被吸回去）")]
        [SerializeField] private float mEscapePadding = 40f;

        [Tooltip("松手飞回初始位置的时长（秒），0 = 瞬移")]
        [SerializeField] private float mReturnDuration = 0.18f;

        [SerializeField] private bool mPlaySnapPop = true;

        [Header("拾取")]
        [Tooltip("保证自身始终有一个可命中的 Graphic，否则点到 Tag 空白背景时拖不起来")]
        [SerializeField] private bool mForceClickable = true;

        [Tooltip("打开后会在 Console 打印拾取/吸附诊断。排查完请关掉")]
        [SerializeField] private bool mLogPickup;

        // ---------- 运行时状态 ----------
        private RectTransform mRect;
        private CanvasGroup mCanvasGroup;

        /// <summary>当前状态。</summary>
        public Stage CurrentStage { get; private set; } = Stage.InSourceList;

        /// <summary>当前所在槽位（没在槽位里为 null）。</summary>
        public DropSlot CurrentSlot { get; private set; }

        /// <summary>是否正在被拖拽。</summary>
        public bool IsDragging => CurrentStage == Stage.Dragging;

        /// <summary>物品标签，供 DropSlot 的 mAcceptTag 过滤使用。</summary>
        public string Tag => m_Tag;

        /// <summary>是否开启诊断日志。</summary>
        public bool LogPickupEnabled => mLogPickup;

        // ---------- 初始位置（"老家"） ----------
        private bool mHomeRecorded;
        private Transform mHomeParent;
        private Vector2 mHomeAnchoredPos;
        private int mHomeSiblingIndex;
        private Vector3 mHomeLocalScale = Vector3.one;

        // ---------- 拖拽期间 ----------
        private RectTransform mDragLayer;          // 拖动时 Tag 临时挂到这里（Canvas 下最上层）
        private CanvasGroup mDragLayerGroup;
        private Camera mDragCamera;                // Overlay 为 null
        private Vector3 mOriginalWorldScale = Vector3.one;   // 按下时 Tag 的世界缩放
        private Vector2 mDragPointerAtPress;       // 按下时鼠标在拖拽层局部空间的位置
        private Vector2 mDragItemAtPress;          // 按下时 Tag 在拖拽层局部空间的 anchoredPosition
        private Vector2 mPressScreenPos;           // 按下时的屏幕坐标（用来判断"是否已拖离原槽位"）
        private DropSlot mOriginSlot;              // 从哪个槽位里拖出来的（没在槽位里则 null）
        private DropSlot mHoverSlot;               // 当前高亮的槽位
        private bool mPlacedByDrop;                // 槽位的 OnDrop 已经处理过放置
        private Coroutine mReturnRoutine;          // 正在播放的"飞回老家"动画

        private static DropSlot sCurrentHoverSlot;

        // ============================================================
        //  生命周期
        // ============================================================

        private void Awake()
        {
            mRect = GetComponent<RectTransform>();
            mCanvasGroup = GetComponent<CanvasGroup>();

            if (mCanvasGroup == null)
            {
                mCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (mCanvas == null)
            {
                mCanvas = GetComponentInParent<Canvas>();
            }

            ApplyClickable();
        }

        private void OnEnable()
        {
            // 场景加载 / 重新激活时，把初始位置重新记一遍（此时还没被挪动过）
            if (CurrentStage != Stage.InSlot)
            {
                RecordHome();
            }

            ResetDragState();
        }

        private void OnDisable()
        {
            StopReturn();
            ResetDragState();
        }

        /// <summary>把当前位置记录为"老家"（游戏开始时在初始列表里的位置）。</summary>
        public void RecordHome()
        {
            if (mRect == null)
            {
                mRect = GetComponent<RectTransform>();
            }

            mHomeParent = mRect.parent;
            mHomeAnchoredPos = mRect.anchoredPosition;
            mHomeSiblingIndex = mRect.GetSiblingIndex();
            mHomeLocalScale = mRect.localScale;
            mHomeRecorded = mHomeParent != null && !(mHomeParent.GetComponent<DropSlot>() != null);
        }

        /// <summary>是否已经记录过老家。</summary>
        public bool HasHome => mHomeRecorded;

        // ============================================================
        //  拖拽
        // ============================================================

        /// <summary>按下即清零标志。</summary>
        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            mPlacedByDrop = false;
            LogDiag("OnInitializePotentialDrag");
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            LogDiag("OnBeginDrag（点到了 Tag 自己）");
            BeginDragFrom(eventData);
        }

        /// <summary>
        /// 开始拖拽。既可由自身的 IBeginDragHandler 触发，
        /// 也可由 DropSlot 转发（点到槽位背景而不是 Tag 本身时兜底）。
        /// </summary>
        public void BeginDragFrom(PointerEventData eventData)
        {
            if (CurrentStage == Stage.Dragging || eventData == null)
            {
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (mCanvas == null)
            {
                mCanvas = GetComponentInParent<Canvas>();
            }

            if (mCanvas == null)
            {
                Debug.LogError($"[TagSingle] {name} 找不到 Canvas，无法拖拽");
                return;
            }

            StopReturn();
            mPlacedByDrop = false;

            // 从槽位里往外拖时，起点槽位可能没同步上，直接按父级推导兜底
            mOriginSlot = CurrentSlot != null ? CurrentSlot : GetComponentInParent<DropSlot>();
            mPressScreenPos = eventData.position;

            // 记住现在的世界缩放，拖拽期间保持不变
            mOriginalWorldScale = mRect.lossyScale;

            EnsureDragLayer();

            if (mDragLayer == null)
            {
                Debug.LogWarning($"[TagSingle] {name} 没有可用的拖拽层，已取消拖拽");
                return;
            }

            mDragCamera = mCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mCanvas.worldCamera;

            CurrentStage = Stage.Dragging;
            LogDiag($"BeginDrag 起点槽位={(mOriginSlot != null ? mOriginSlot.name : "无")} " +
                    $"老家={(mHomeParent != null ? mHomeParent.name : "无")} 老家位置={mHomeAnchoredPos}");

            // 挂到拖拽层，并保持屏幕位置不变
            mRect.SetParent(mDragLayer, true);
            mRect.SetAsLastSibling();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mDragLayer, eventData.position, eventData.pressEventCamera, out mDragPointerAtPress);

            mDragItemAtPress = new Vector2(mRect.anchoredPosition.x, mRect.anchoredPosition.y);
            mRect.localScale = WorldScaleToLocal(mOriginalWorldScale);

            mCanvasGroup.blocksRaycasts = false;
            mCanvasGroup.alpha = 0.85f;

            SetHover(null);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (CurrentStage != Stage.Dragging)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mDragLayer, eventData.position, mDragCamera, out Vector2 pointerLocal);

            Vector2 local = mDragItemAtPress + (pointerLocal - mDragPointerAtPress);

            // 磁吸：只在"允许放下"的槽位附近生效（原槽位在拖离前不参与）
            DropSlot hover = FindBestSlot(eventData, out _);
            SetHover(hover);

            if (hover != null)
            {
                Vector2 slotLocal = WorldToDragLayer(hover.transform.position);
                local = Vector2.Lerp(local, slotLocal, Time.unscaledDeltaTime * 12f);
            }

            mRect.anchoredPosition = local;

            if (mLogPickup)
            {
                Debug.Log($"[TagSingle/{name}] OnDrag 鼠标={eventData.position} 鼠标局部={pointerLocal} " +
                          $"Tag局部={local} 悬停={(hover != null ? hover.name : "无")} " +
                          $"起点槽位={(mOriginSlot != null ? mOriginSlot.name : "无")} 老家={mHomeAnchoredPos}");
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (CurrentStage != Stage.Dragging)
            {
                return;
            }

            mCanvasGroup.blocksRaycasts = true;
            mCanvasGroup.interactable = true;
            mCanvasGroup.alpha = 1f;
            mRect.localScale = mHomeLocalScale;

            // 槽位的 OnDrop 已经处理过放置，这里不重复处理
            if (mPlacedByDrop)
            {
                mPlacedByDrop = false;
                SetHover(null);
                return;
            }

            DropSlot target = FindBestSlot(eventData, out _);
            SetHover(null);

            if (mLogPickup)
            {
                Debug.Log($"[TagSingle/{name}] OnEndDrag 松手={eventData.position} " +
                          $"落点槽位={(target != null ? target.name : "无")} " +
                          $"起点槽位={(mOriginSlot != null ? mOriginSlot.name : "无")} " +
                          $"=> {(target != null ? "放入槽位" : "飞回初始位置 " + mHomeAnchoredPos)}");
            }

            if (target != null && target.CanAccept(this))
            {
                PlaceIntoSlot(target);
            }
            else
            {
                // 没放到槽位 -> 自动回到初始列表的初始位置
                ReturnHome(animated: true);

                TypeEventSystem.Global.Send(new TagSinglePlaceFailedEvent { Item = this });
            }

            mOriginSlot = null;
        }

        // ============================================================
        //  放入槽位 / 回初始位
        // ============================================================

        /// <summary>把 Tag 放进槽位（TagSingle.OnEndDrag 与 DropSlot.OnDrop 都走这里）。</summary>
        public void PlaceIntoSlot(DropSlot slot)
        {
            if (slot == null || !slot.CanAccept(this))
            {
                return;
            }

            StopReturn();
            mPlacedByDrop = true;

            DropSlot previous = CurrentSlot;

            // 同一个 Tag 同一时刻只能占一个槽位：
            // 如果它原本在别的槽位里，必须先把旧槽位腾空，否则旧槽位会一直"以为"自己还装着它
            // （表现为旧槽位永远 CanAccept=false、高亮失效、TagSystem 的"全部填满"判定永远不成立）
            if (previous != null && previous != slot)
            {
                previous.Clear();
            }

            RectTransform target = slot.transform as RectTransform;
            mRect.SetParent(target, false);
            mRect.anchoredPosition = Vector2.zero;
            mRect.localScale = mHomeLocalScale;
            mRect.SetAsLastSibling();

            if (mCanvasGroup != null)
            {
                mCanvasGroup.blocksRaycasts = true;
                mCanvasGroup.interactable = true;
                mCanvasGroup.alpha = 1f;
            }

            ApplyClickable();

            slot.Place(this);
            CurrentSlot = slot;
            CurrentStage = Stage.InSlot;
            SetHover(null);
            LogDiag($"PlaceIntoSlot -> {slot.name}（原槽位={(previous != null ? previous.name : "无")}）");

            if (mPlaySnapPop)
            {
                StartCoroutine(SnapPop());
            }

            TypeEventSystem.Global.Send(new TagSinglePlacedEvent { Item = this, Slot = slot });
        }

        /// <summary>
        /// 回到初始列表的初始位置。animated=true 时平滑飞回去。
        /// </summary>
        public void ReturnHome(bool animated = true)
        {
            if (!mHomeRecorded || mHomeParent == null)
            {
                // 没有老家（比如一开始就被摆在槽位里），就留在原地但脱离槽位状态
                CurrentSlot = null;
                CurrentStage = Stage.InSourceList;
                return;
            }

            DropSlot slot = CurrentSlot;
            CurrentSlot = null;
            slot?.Clear();
            SetHover(null);

            mRect.localScale = mHomeLocalScale;

            if (animated && mReturnDuration > 0.01f && CurrentStage != Stage.InSourceList)
            {
                LogDiag($"ReturnHome（动画）-> {mHomeParent.name} {mHomeAnchoredPos}");
                StopReturn();
                mReturnRoutine = StartCoroutine(ReturnHomeRoutine());
                return;
            }

            SnapToHome();
        }

        private void SnapToHome()
        {
            mRect.SetParent(mHomeParent, false);
            mRect.SetSiblingIndex(Mathf.Clamp(mHomeSiblingIndex, 0, mHomeParent.childCount - 1));
            mRect.anchoredPosition = mHomeAnchoredPos;
            mRect.localScale = mHomeLocalScale;

            CurrentSlot = null;
            CurrentStage = Stage.InSourceList;
            ResetDragState();
            ApplyClickable();
            LogDiag("ReturnHome（瞬移）完成");
        }

        private IEnumerator ReturnHomeRoutine()
        {
            // 飞行动画期间保持挂在一个共同父级下，避免中途换父级导致位置跳
            Transform flightParent = mHomeParent;
            mRect.SetParent(flightParent, false);
            mRect.SetSiblingIndex(Mathf.Clamp(mHomeSiblingIndex, 0, flightParent.childCount - 1));

            Vector2 from = mRect.anchoredPosition;
            Vector3 fromScale = mRect.localScale;
            float time = 0f;

            while (time < mReturnDuration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / mReturnDuration);
                t = t * t * (3f - 2f * t);   // smoothstep

                mRect.anchoredPosition = Vector2.Lerp(from, mHomeAnchoredPos, t);
                mRect.localScale = Vector3.Lerp(fromScale, mHomeLocalScale, t);
                yield return null;
            }

            mReturnRoutine = null;
            SnapToHome();
        }

        private void StopReturn()
        {
            if (mReturnRoutine != null)
            {
                StopCoroutine(mReturnRoutine);
                mReturnRoutine = null;
            }
        }

        // ============================================================
        //  槽位查找
        // ============================================================

        /// <summary>
        /// 找当前最合适的槽位。返回 null 表示"没有可放下的槽位"（松手就回初始位）。
        /// out 参数 freeSlot 是"忽略原槽位判定"的那个候选，用于调试输出。
        /// </summary>
        public DropSlot FindBestSlot(PointerEventData eventData, out DropSlot freeSlot)
        {
            freeSlot = null;

            if (eventData == null || DropSlot.AllSlots.Count == 0)
            {
                return null;
            }

            Camera camera = mDragCamera;
            Vector2 pointer = eventData.position;

            DropSlot inside = null;
            DropSlot nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (DropSlot slot in DropSlot.AllSlots)
            {
                if (slot == null || !slot.isActiveAndEnabled) continue;
                if (slot == CurrentSlot) continue;                 // 已经在里面了
                if (!slot.CanAccept(this)) continue;               // 被占用 / 类型不匹配

                RectTransform rect = slot.transform as RectTransform;

                if (rect == null) continue;

                // 刚从它里面拖出来、且还没拖离 -> 不允许再放回去
                if (slot == mOriginSlot && !HasEscapedOriginSlot(rect, pointer)) continue;

                freeSlot = slot;

                bool insideRect = RectTransformUtility.RectangleContainsScreenPoint(rect, pointer, camera);

                if (insideRect && inside == null)
                {
                    inside = slot;
                }

                float distance = DistanceToRect(rect, pointer, camera);

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

            if (nearest != null && nearestDistance <= mSnapDistance)
            {
                return nearest;
            }

            return null;
        }

        /// <summary>鼠标是否已经离开原槽位矩形足够远（超过 mEscapePadding）。</summary>
        private bool HasEscapedOriginSlot(RectTransform originRect, Vector2 pointer)
        {
            Camera camera = mDragCamera;
            Vector2 slotScreen = RectTransformUtility.WorldToScreenPoint(camera, originRect.position);

            float halfW = originRect.rect.width * 0.5f * Mathf.Abs(originRect.lossyScale.x);
            float halfH = originRect.rect.height * 0.5f * Mathf.Abs(originRect.lossyScale.y);

            float dx = Mathf.Max(0f, Mathf.Abs(pointer.x - slotScreen.x) - halfW);
            float dy = Mathf.Max(0f, Mathf.Abs(pointer.y - slotScreen.y) - halfH);

            return dx > mEscapePadding || dy > mEscapePadding;
        }

        /// <summary>鼠标到槽位矩形的最近距离（像素）。</summary>
        private static float DistanceToRect(RectTransform rect, Vector2 pointer, Camera camera)
        {
            Vector2 center = RectTransformUtility.WorldToScreenPoint(camera, rect.position);

            float halfW = rect.rect.width * 0.5f * Mathf.Abs(rect.lossyScale.x);
            float halfH = rect.rect.height * 0.5f * Mathf.Abs(rect.lossyScale.y);

            float dx = Mathf.Max(0f, Mathf.Abs(pointer.x - center.x) - halfW);
            float dy = Mathf.Max(0f, Mathf.Abs(pointer.y - center.y) - halfH);

            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>悬停高亮。</summary>
        private void SetHover(DropSlot slot)
        {
            if (mHoverSlot == slot)
            {
                return;
            }

            mHoverSlot?.SetHighlight(false);
            mHoverSlot = slot;
            mHoverSlot?.SetHighlight(true);

            sCurrentHoverSlot = mHoverSlot;
        }

        // ============================================================
        //  拖拽层 / 坐标换算
        // ============================================================

        private void EnsureDragLayer()
        {
            if (mDragLayer != null)
            {
                return;
            }

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

        private Vector2 WorldToDragLayer(Vector3 worldPos)
        {
            Vector3 local = mDragLayer.InverseTransformPoint(worldPos);
            return new Vector2(local.x, local.y);
        }

        private Vector3 WorldScaleToLocal(Vector3 worldScale)
        {
            Vector3 parentScale = mRect.parent != null ? mRect.parent.lossyScale : Vector3.one;

            return new Vector3(
                parentScale.x != 0f ? worldScale.x / parentScale.x : 1f,
                parentScale.y != 0f ? worldScale.y / parentScale.y : 1f,
                1f);
        }

        // ============================================================
        //  拾取保障 / 状态复位
        // ============================================================

        /// <summary>把拖拽相关的临时状态复位。</summary>
        public void ResetDragState()
        {
            mPlacedByDrop = false;
            mOriginSlot = null;
            mHoverSlot = null;
            sCurrentHoverSlot = null;

            if (mCanvasGroup != null)
            {
                mCanvasGroup.blocksRaycasts = true;
                mCanvasGroup.interactable = true;
                mCanvasGroup.alpha = 1f;
            }
        }

        /// <summary>
        /// 保证自身有可命中的 Graphic。
        /// 关掉 raycastTarget 的空白背景会让拖拽变成"概率性成功"：点到文字才行、点到背景就抓不住。
        /// </summary>
        private void ApplyClickable()
        {
            if (!mForceClickable)
            {
                return;
            }

            Graphic[] graphics = GetComponents<Graphic>();

            if (graphics.Length == 0)
            {
                Image fallback = gameObject.AddComponent<Image>();
                fallback.color = new Color(1f, 1f, 1f, 0f);
                fallback.raycastTarget = true;
                return;
            }

            if (graphics[0] != null && !graphics[0].raycastTarget)
            {
                graphics[0].raycastTarget = true;
            }
        }

        private IEnumerator SnapPop()
        {
            const float duration = 0.12f;
            float time = 0f;

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);
                mRect.localScale = mHomeLocalScale * (1f + 0.12f * Mathf.Sin(t * Mathf.PI));
                yield return null;
            }

            mRect.localScale = mHomeLocalScale;
        }

        /// <summary>设置物品标签（运行时生成 Tag 时调用）。</summary>
        public void SetTag(string tag)
        {
            m_Tag = tag;
        }

        // ============================================================
        //  诊断
        // ============================================================

        /// <summary>就地打印一条诊断日志（供槽位等外部调用）。</summary>
        public void LogDiag(string message)
        {
            if (mLogPickup)
            {
                Debug.Log($"[TagSingle/{name}] {message}");
            }
        }
    }
}
