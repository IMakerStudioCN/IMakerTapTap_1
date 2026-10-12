using QFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using TMPro;

namespace TapTapFirst
{
    /// <summary>
    /// 文本模版里的一个【空缺】。
    ///
    /// 结构约定：
    ///   SlotRoot                  <- 本组件（必须有可命中的 Graphic，否则收不到拖放）
    ///     Placeholder             <- 空缺提示（如"____"），填上后自动隐藏
    ///     (运行时生成的 TagDisplay 会挂在这里)
    ///
    /// 玩家填进来的 Tag 会按本空缺的【词性】(mWordType) 去
    /// Tag_SO.theTypeToContent 的对应下标取显示文本：
    ///   0 形容词 / 1 名词 / 2 量词 / 3 胡扯
    /// </summary>
    [DisallowMultipleComponent]
    public class TagKindSlot : MonoBehaviour, IDropHandler, IPointerDownHandler
    {
        /// <summary>场景中所有激活的空缺。</summary>
        public static readonly List<TagKindSlot> AllSlots = new List<TagKindSlot>();

        [Header("空缺标识（存档用，必须唯一且稳定）")]
        [SerializeField] private string mSlotId = "slot_1";

        [Header("这个空缺的词性（决定取 theTypeToContent 的哪一项）")]
        [SerializeField] private TagOfWords mWordType = TagOfWords.adjective;

        [Header("显示用的 Tag 预制体（可挂 TagDisplay）")]
        [SerializeField] private GameObject mTagDisplayPrefab;

        [Header("空缺提示（填上后自动隐藏）")]
        [SerializeField] private GameObject mPlaceholder;

        [Header("悬停高亮（可选）")]
        [SerializeField] private Image mHighlight;
        [SerializeField] private Color mHighlightColor = new Color(0.35f, 0.75f, 1f, 0.45f);

        [Header("日志")]
        [SerializeField] private bool mLog;

        private Color mOriginColor = Color.white;
        private bool mOriginColorCached;

        // ---------- 运行时状态 ----------
        private TagDisplay mDisplay;
        private Tag_SO mTag;
        private TagOption mSource;

        /// <summary>空缺唯一 Id，存档按它定位。</summary>
        public string SlotId => mSlotId;

        /// <summary>这个空缺的词性。</summary>
        public TagOfWords WordType => mWordType;

        /// <summary>是否已填。</summary>
        public bool IsFilled => mTag != null;

        /// <summary>是否为空缺。</summary>
        public bool IsEmpty => mTag == null;

        /// <summary>里面 Tag 的配置（空则为 null）。</summary>
        public Tag_SO Tag => mTag;

        /// <summary>里面 Tag 的 id（空则为 0）。</summary>
        public int TagId => mTag != null ? mTag.tagId : 0;

        /// <summary>这份内容是哪个列表项拖进来的（运行时生成的副本可能为 null）。</summary>
        public TagOption SourceOption => mSource;

        /// <summary>当前应显示的文本（空则为空串）。</summary>
        public string Content => mTag != null ? Tag_SO.GetDisplayContent(mTag, mWordType) : string.Empty;

        /// <summary>填入 / 清空时触发。</summary>
        public event Action<TagKindSlot> Changed;

        // ---------------- 生命周期 ----------------

        private void OnEnable()
        {
            AllSlots.Remove(this);
            AllSlots.Add(this);
            Refresh();
        }

        private void OnDisable()
        {
            AllSlots.Remove(this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(mSlotId))
            {
                mSlotId = name;
            }
        }
#endif

        // ---------------- 放置 / 清空 ----------------

        /// <summary>这个空缺能不能接收该 Tag（已填满则必须先清空）。</summary>
        public bool CanAccept(Tag_SO tag)
        {
            return tag != null && mTag == null;
        }

        /// <summary>把 Tag 填进这个空缺。</summary>
        public void Place(Tag_SO tag, TagOption source = null)
        {
            if (tag == null)
            {
                return;
            }

            if (mTag != null)
            {
                Clear();
            }

            mTag = tag;
            mSource = source;

            EnsureDisplay();
            mDisplay?.SetTag(tag);
            Refresh();
            Changed?.Invoke(this);

            Log($"填入 tagId={tag.tagId}，词性={mWordType}，显示内容=\"{Content}\"");
        }

        /// <summary>清空这个空缺（Tag 本身留在列表里，可再次使用）。</summary>
        public void Clear(bool destroyDisplay = true)
        {
            if (mTag == null && mDisplay == null)
            {
                return;
            }

            mTag = null;
            mSource = null;

            if (destroyDisplay && mDisplay != null)
            {
                Destroy(mDisplay.gameObject);
            }

            mDisplay = null;
            Refresh();
            Changed?.Invoke(this);

            Log("已清空");
        }

        /// <summary>刷新显示。</summary>
        public void Refresh()
        {
            bool filled = mTag != null;

            if (mPlaceholder != null)
            {
                mPlaceholder.SetActive(!filled);
            }

            if (filled && mDisplay != null)
            {
                mDisplay.SetTag(mTag);
            }

            ApplyHighlight(false);
        }

        private void EnsureDisplay()
        {
            if (mDisplay != null)
            {
                return;
            }

            if (mTagDisplayPrefab != null)
            {
                GameObject go = Instantiate(mTagDisplayPrefab, transform);
                go.name = "TagDisplay";
                go.SetActive(true);
                mDisplay = go.GetComponent<TagDisplay>();

                if (mDisplay == null)
                {
                    mDisplay = go.AddComponent<TagDisplay>();
                }
            }
            else
            {
                GameObject go = new GameObject("TagDisplay", typeof(RectTransform));
                RectTransform rect = (RectTransform)go.transform;
                rect.SetParent(transform, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                mDisplay = go.AddComponent<TagDisplay>();
            }

            mDisplay.Owner = this;
            mDisplay.transform.SetAsLastSibling();
        }

        /// <summary>运行时 / 编辑器里配置这个空缺（生成器用）。</summary>
        public void ApplySetup(string slotId, TagOfWords wordType, GameObject tagDisplayPrefab = null)
        {
            if (!string.IsNullOrEmpty(slotId))
            {
                mSlotId = slotId;
            }

            mWordType = wordType;

            if (tagDisplayPrefab != null)
            {
                mTagDisplayPrefab = tagDisplayPrefab;
            }

            Refresh();
        }

        /// <summary>设置空缺提示节点。</summary>
        public void SetPlaceholder(GameObject placeholder)
        {
            mPlaceholder = placeholder;
            Refresh();
        }

        /// <summary>设置高亮边框。</summary>
        public void SetHighlightImage(Image highlight)
        {
            mHighlight = highlight;
            mOriginColorCached = false;
        }
        // ---------------- 悬停高亮 ----------------

        public void SetHighlight(bool on)
        {
            ApplyHighlight(on && mTag == null);
        }

        private void ApplyHighlight(bool on)
        {
            if (mHighlight == null)
            {
                return;
            }

            if (!mOriginColorCached)
            {
                mOriginColor = mHighlight.color;
                mOriginColorCached = true;
            }

            mHighlight.color = on ? mHighlightColor : mOriginColor;
            mHighlight.enabled = on || mOriginColor.a > 0.001f;
        }

        // ============================================================
        //  接收拖入 / 点击清空
        // ============================================================

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null)
            {
                return;
            }

            // 从 Tag 列表拖来的
            TagOption option = eventData.pointerDrag.GetComponent<TagOption>();

            if (option != null)
            {
                if (mTag != null)
                {
                    Clear();
                }

                Place(option.TagData, option);
                option.LogDiag($"放到空缺 {mSlotId}（词性 {mWordType}）");
                return;
            }

            // 从别的空缺拖过来的
            TagDisplay display = eventData.pointerDrag.GetComponent<TagDisplay>();

            if (display != null && display.Owner != null && display.Owner != this)
            {
                Tag_SO tag = display.TagData;
                TagOption source = display.Owner.SourceOption;
                display.Owner.Clear();
                Place(tag, source);
            }
        }

        // 点到已填的空缺 -> 清空（"改主意"）
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (mTag == null)
            {
                return;
            }

            if (mDisplay != null && mDisplay.ConsumeDragStarted())
            {
                // 这次按下已经被显示副本当作"拖走"处理了，不在这里清空
                return;
            }

            if (mDisplay != null && mDisplay.IsPointerOnClearButtonPublic(eventData))
            {
                // 点在"×"按钮上，交给按钮自己处理
                return;
            }

            Clear();
        }

        // ---------------- 工具 ----------------

        private void Log(string message)
        {
            if (mLog)
            {
                Debug.Log($"[TagKindSlot/{name}] {message}");
            }
        }

        /// <summary>给空缺补一个可命中的射线目标。</summary>
        [ContextMenu("补齐 Raycast 目标")]
        private void EnsureRaycastTarget()
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