using QFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TapTapFirst
{
    /// <summary>
    /// Tag 的目标槽位（一个槽位同一时刻只装一个 Tag）。
    ///
    /// 职责：
    ///   1. 接收拖进来的 Tag（OnDrop）
    ///   2. 接住"想从槽位里把 Tag 摘下来"的那次按下，并转发给 TagSingle
    ///      —— 否则点到 Tag 的空白背景时射线打不到 Tag，就会变成"概率摘不下来"
    ///   3. 悬停高亮
    ///
    /// 摘下之后由 TagSingle 负责自动飞回初始列表的初始位置，本组件不管归属关系。
    /// </summary>
    public class DropSlot : MonoBehaviour,
        IDropHandler, IPointerDownHandler, IBeginDragHandler, IInitializePotentialDragHandler
    {
        /// <summary>场景中所有激活的槽位。</summary>
        public static readonly List<DropSlot> AllSlots = new List<DropSlot>();

        [SerializeField] private string mAcceptTag; // 可接受的物品标签（留空 = 不限）
        [SerializeField] private Image mHighlight;  // 高亮边框（可选）

        /// <summary>当前占用的 Tag（空闲为 null）。</summary>
        public TagSingle CurrentItem { get; private set; }

        /// <summary>是否空闲。</summary>
        public bool IsEmpty => CurrentItem == null;

        private void OnEnable()
        {
            AllSlots.Remove(this);
            AllSlots.Add(this);
            ApplyHighlight(false);
        }

        private void OnDisable()
        {
            AllSlots.Remove(this);
        }

        // ============================================================
        //  归属
        // ============================================================

        /// <summary>是否可接受该 Tag。</summary>
        public bool CanAccept(TagSingle item)
        {
            if (item == null)
            {
                return false;
            }

            // 自己已经装了这个 Tag -> 视作可以（幂等）
            if (CurrentItem == item)
            {
                return true;
            }

            // 一个槽位只能装一个 Tag
            if (CurrentItem != null)
            {
                return false;
            }

            // 需要按标签过滤时打开下面这行
            // if (!string.IsNullOrEmpty(mAcceptTag) && mAcceptTag != item.Tag) return false;

            return true;
        }

        /// <summary>登记占用。</summary>
        public void Place(TagSingle item)
        {
            CurrentItem = item;
            ApplyHighlight(false);

            item?.LogDiag($"DropSlot[{name}].Place 已占用 -> {item.name}");
        }

        /// <summary>解除占用。</summary>
        public void Clear()
        {
            CurrentItem = null;
            ApplyHighlight(false);
        }

        /// <summary>悬停高亮开关（拖拽过程中由 TagSingle 调用）。</summary>
        public void SetHighlight(bool on)
        {
            ApplyHighlight(on && CurrentItem == null);
        }

        private void ApplyHighlight(bool on)
        {
            if (mHighlight != null)
            {
                mHighlight.enabled = on;
            }
        }

        // ============================================================
        //  接收拖入
        // ============================================================

        public void OnDrop(PointerEventData eventData)
        {
            TagSingle item = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<TagSingle>()
                : null;

            item?.LogDiag($"DropSlot[{name}].OnDrop 收到=" +
                          $"{(eventData.pointerDrag != null ? eventData.pointerDrag.name : "null")}，" +
                          $"可接受={(item != null && CanAccept(item))}，" +
                          $"当前占用={(CurrentItem != null ? CurrentItem.name : "空")}");

            if (item == null || !CanAccept(item))
            {
                return;
            }

            item.PlaceIntoSlot(this);
        }

        // ============================================================
        //  转发"摘下"
        // ============================================================

        // 点到槽位（含槽位里 Tag 的空白区域）时，把按下事件导向已放置的 Tag
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (CurrentItem == null)
            {
                return;
            }

            CurrentItem.LogDiag($"DropSlot[{name}].OnPointerDown 转发给 {CurrentItem.name}");

            eventData.pointerDrag = CurrentItem.gameObject;
            eventData.pointerPress = CurrentItem.gameObject;

            ExecuteEvents.Execute(CurrentItem.gameObject, eventData, ExecuteEvents.initializePotentialDrag);
        }

        // 拖拽真正开始时再转发一次：把 Tag 从槽位里摘出来
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (CurrentItem == null)
            {
                return;
            }

            eventData.pointerDrag = CurrentItem.gameObject;
            CurrentItem.BeginDragFrom(eventData);
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            // 接口占位：让 ExecuteEvents 能把 initializePotentialDrag 派发到本节点
        }
    }
}
