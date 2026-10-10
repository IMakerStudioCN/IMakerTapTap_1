using QFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

namespace TapTapFirst
{
    /// <summary>模版里的一个顺序片段：要么是一段固定文本，要么是一个空缺。</summary>
    public class TagTextSegment
    {
        /// <summary>固定文本节点（空缺时为 null）。</summary>
        public GameObject TextObject;

        /// <summary>空缺（固定文本时为 null）。</summary>
        public TagKindSlot Slot;

        /// <summary>固定文本的内容。</summary>
        public string Text;

        public bool IsSlot => Slot != null;
    }

    /// <summary>一个空缺的配置（生成器用）。</summary>
    public struct SlotSetup
    {
        public string SlotId;
        public TagOfWords WordType;

        public SlotSetup(string slotId, TagOfWords wordType)
        {
            SlotId = slotId;
            WordType = wordType;
        }
    }
    /// <summary>进度。</summary>
    [Serializable]
    public class TagFillProgress
    {
        public int filled;
        public int total;

        public TagFillProgress(int filled, int total)
        {
            this.filled = filled;
            this.total = total;
        }
    }

    /// <summary>
    /// 【带空缺的文本模版】。
    ///
    /// 把它挂在模版根节点上，子节点按视觉顺序排列：
    ///
    ///   Template (本组件)
    ///   ├─ "我真的很"        <- 固定文本节点（TMP / Text）
    ///   ├─ Slot_A            <- TagKindSlot（空缺）
    ///   ├─ "，因为"
    ///   ├─ Slot_B            <- TagKindSlot（空缺）
    ///   └─ "。"
    ///
    /// 按【层级顺序（深度优先）】自动收集成线性片段序列，就是文本顺序。
    /// 因此可以通过调整节点顺序来调整阅读顺序，不需要额外配置。
    /// </summary>
    [DisallowMultipleComponent]
    public class TagTextTemplate : MonoBehaviour
    {
        [Header("文档标识（存档用）")]
        [SerializeField] private string mDocumentId = "text_fill";

        [Tooltip("玩家没有填的空缺在纯文本里显示成什么")]
        [SerializeField] private string mEmptyPlaceholder = "____";

        [Header("日志")]
        [SerializeField] private bool mLog;

        private readonly List<TagTextSegment> mSegments = new List<TagTextSegment>();

        /// <summary>片段内容发生变化时触发（空缺填入/清空，或重新收集）。自适应排版靠它重排。</summary>
        public event Action SegmentsChanged;
        /// <summary>文档 Id。</summary>
        public string DocumentId => mDocumentId;

        /// <summary>按文本顺序排好的片段（只读）。</summary>
        public IReadOnlyList<TagTextSegment> Segments => mSegments;

        /// <summary>是否已经收集过片段。</summary>
        public bool IsCollected => mSegments.Count > 0;

        /// <summary>所有空缺是否都已填满。</summary>
        public bool IsAllSlotsFilled
        {
            get
            {
                EnsureCollected();

                int total = 0;

                foreach (TagTextSegment segment in mSegments)
                {
                    if (!segment.IsSlot)
                    {
                        continue;
                    }

                    total++;

                    if (segment.Slot.IsEmpty)
                    {
                        return false;
                    }
                }

                return total > 0;
            }
        }

        // ---------------- 生命周期 ----------------

        private void Awake()
        {
            Collect();
        }

        private void OnEnable()
        {
            Collect();

            foreach (TagTextSegment segment in mSegments)
            {
                if (segment.IsSlot)
                {
                    segment.Slot.Changed += OnSlotChanged;
                }
            }
        }

        private void OnDisable()
        {
            foreach (TagTextSegment segment in mSegments)
            {
                if (segment.IsSlot)
                {
                    segment.Slot.Changed -= OnSlotChanged;
                }
            }
        }

        /// <summary>开始收集片段（会等一帧，保证 LayoutGroup 已经算完）。</summary>
        public void Collect()
        {
            mSegments.Clear();
            CollectInto(transform, mSegments);
            Log($"收集到 {mSegments.Count} 个片段，其中空缺 {CountSlots()} 个");
            SegmentsChanged?.Invoke();
        }

        /// <summary>确保已经收集过。</summary>
        public void EnsureCollected()
        {
            if (mSegments.Count == 0)
            {
                Collect();
            }
        }

        private int CountSlots()
        {
            int count = 0;

            foreach (TagTextSegment segment in mSegments)
            {
                if (segment.IsSlot)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>深度优先遍历，把固定文本与空缺按顺序收集起来。</summary>
        private static void CollectInto(Transform root, List<TagTextSegment> output)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);

                if (!child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                TagKindSlot slot = child.GetComponent<TagKindSlot>();

                if (slot != null)
                {
                    output.Add(new TagTextSegment { Slot = slot });
                    continue;   // 空缺内部的东西不再拆
                }

                string text = ExtractText(child.gameObject);

                if (text != null)
                {
                    output.Add(new TagTextSegment { TextObject = child.gameObject, Text = text });
                }

                // 继续往下找（比如中间还有一层布局节点）
                CollectInto(child, output);
            }
        }

        /// <summary>取出节点上的文本；没有文本组件则返回 null。</summary>
        private static string ExtractText(GameObject go)
        {
            TMP_Text tmp = go.GetComponent<TMP_Text>();

            if (tmp != null)
            {
                return tmp.text;
            }

            Text uiText = go.GetComponent<Text>();

            if (uiText != null)
            {
                return uiText.text;
            }

            return null;
        }

        // ---------------- 拼接 / 进度 ----------------

        /// <summary>把当前画面上的内容按顺序拼成完整文本。</summary>
        public string GetPlainText()
        {
            EnsureCollected();

            StringBuilder builder = new StringBuilder();

            foreach (TagTextSegment segment in mSegments)
            {
                if (segment.IsSlot)
                {
                    builder.Append(segment.Slot.IsFilled ? segment.Slot.Content : mEmptyPlaceholder);
                }
                else
                {
                    builder.Append(segment.Text);
                }
            }

            return builder.ToString();
        }

        /// <summary>已填 / 总数。</summary>
        public TagFillProgress GetProgress()
        {
            EnsureCollected();

            int filled = 0;
            int total = 0;

            foreach (TagTextSegment segment in mSegments)
            {
                if (!segment.IsSlot)
                {
                    continue;
                }

                total++;

                if (segment.Slot.IsFilled)
                {
                    filled++;
                }
            }

            return new TagFillProgress(filled, total);
        }

        /// <summary>清空所有空缺。</summary>
        public void ClearAll(bool destroyDisplay = true)
        {
            EnsureCollected();

            foreach (TagTextSegment segment in mSegments)
            {
                segment.Slot?.Clear(destroyDisplay);
            }
        }

        // ---------------- 配置（生成器 / 运行时搭建用） ----------------

        /// <summary>
        /// 直接指定片段顺序（生成器在创建节点后调用，比自动遍历更可靠）。
        /// </summary>
        public void SetSegments(IEnumerable<TagTextSegment> segments)
        {
            mSegments.Clear();

            if (segments != null)
            {
                foreach (TagTextSegment segment in segments)
                {
                    if (segment != null && (segment.IsSlot || segment.TextObject != null))
                    {
                        mSegments.Add(segment);
                    }
                }
            }

            Log($"SetSegments -> {mSegments.Count} 个片段，其中空缺 {CountSlots()} 个");
            SegmentsChanged?.Invoke();
        }

        /// <summary>
        /// 在模版下创建一个空缺节点（生成器 / 运行时搭建用）。
        /// 会自动补一个可命中的透明 Image。
        /// </summary>
        public TagKindSlot CreateSlot(string slotId, TagOfWords wordType,
            GameObject tagDisplayPrefab = null, string placeholderText = "____")
        {
            GameObject go = new GameObject(slotId, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(transform, false);
            rect.sizeDelta = new Vector2(120f, 60f);

            // 可命中的热区
            UnityEngine.UI.Image hit = go.AddComponent<UnityEngine.UI.Image>();
            hit.color = new Color(1f, 1f, 1f, 0.12f);
            hit.raycastTarget = true;

            // 空缺提示
            GameObject placeholder = new GameObject("Placeholder", typeof(RectTransform));
            RectTransform phRect = (RectTransform)placeholder.transform;
            phRect.SetParent(rect, false);
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = Vector2.zero;
            phRect.offsetMax = Vector2.zero;

            TMPro.TextMeshProUGUI phText = placeholder.AddComponent<TMPro.TextMeshProUGUI>();
            phText.text = placeholderText;
            phText.alignment = TMPro.TextAlignmentOptions.Center;
            phText.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            phText.raycastTarget = false;

            TagKindSlot slot = go.AddComponent<TagKindSlot>();
            slot.ApplySetup(slotId, wordType, tagDisplayPrefab);
            slot.SetPlaceholder(placeholder);

            return slot;
        }

        /// <summary>
        /// 按 (文本, 空缺) 交替的配置一次性搭好整个模版。
        /// 生成器用：会先清空所有子节点再重建。
        /// </summary>
        public void Configure(IList<object> sequence, GameObject tagDisplayPrefab = null)
        {
            // 清空现有子节点
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediateSafe(transform.GetChild(i).gameObject);
            }

            mSegments.Clear();

            int textIndex = 0;
            int slotIndex = 0;

            if (sequence != null)
            {
                foreach (object item in sequence)
                {
                    if (item is string text)
                    {
                        GameObject go = new GameObject($"Text_{textIndex++}", typeof(RectTransform));
                        RectTransform rect = (RectTransform)go.transform;
                        rect.SetParent(transform, false);
                        rect.sizeDelta = new Vector2(160f, 60f);

                        TMPro.TextMeshProUGUI tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
                        tmp.text = text;
                        tmp.alignment = TMPro.TextAlignmentOptions.Center;
                        tmp.raycastTarget = false;

                        mSegments.Add(new TagTextSegment { TextObject = go, Text = text });
                    }
                    else if (item is SlotSetup setup)
                    {
                        TagKindSlot slot = CreateSlot(
                            string.IsNullOrEmpty(setup.SlotId) ? $"Slot_{slotIndex}" : setup.SlotId,
                            setup.WordType,
                            tagDisplayPrefab);

                        slotIndex++;

                        mSegments.Add(new TagTextSegment { Slot = slot });
                    }
                }
            }

            Log($"Configure -> {mSegments.Count} 个片段，其中空缺 {CountSlots()} 个");
        }

        private static void DestroyImmediateSafe(GameObject go)
        {
            if (go == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(go);
                return;
            }
#endif
            UnityEngine.Object.Destroy(go);
        }

        // ---------------- 存档 ----------------

        /// <summary>
        /// 生成按文本顺序保存的文档（每个固定文本段一条、每个空缺一条，空位也保留）。
        /// </summary>
        public TagFillDocument BuildDocument()
        {
            EnsureCollected();

            TagFillDocument document = new TagFillDocument
            {
                documentId = mDocumentId,
                version = 1
            };

            foreach (TagTextSegment segment in mSegments)
            {
                if (segment.IsSlot)
                {
                    TagKindSlot slot = segment.Slot;

                    document.items.Add(new TagFillItem
                    {
                        kind = TagFillItem.KindSlot,
                        slotId = slot.SlotId,
                        wordType = slot.WordType,
                        content = slot.Content,
                        tagId = slot.TagId,
                        filled = slot.IsFilled
                    });

                    document.slots.Add(new TagFillSlotEntry(slot.SlotId, slot.TagId, slot.IsFilled));
                }
                else
                {
                    document.items.Add(new TagFillItem
                    {
                        kind = TagFillItem.KindText,
                        content = segment.Text
                    });
                }
            }

            Log($"已生成文档，{document.SlotCount} 个空缺 / 已填 {document.FilledCount}");
            return document;
        }

        /// <summary>直接拿到 Json 字符串（可以丢进任何存档结构）。</summary>
        public string SaveToJson(bool pretty = true)
        {
            return BuildDocument().ToJson(pretty);
        }

        /// <summary>按 Json 字符串回填空缺。</summary>
        public bool LoadFromJson(string json)
        {
            if (!TagFillDocument.TryParse(json, out TagFillDocument document))
            {
                return false;
            }

            return ApplyDocument(document);
        }

        /// <summary>按文档回填空缺（先清空所有空缺，再按 tagId 重新填）。</summary>
        public bool ApplyDocument(TagFillDocument document)
        {
            if (document == null)
            {
                return false;
            }

            EnsureCollected();
            ClearAll();

            ITagSystem tagSystem = TapTap.Interface.GetSystem<ITagSystem>();

            int applied = 0;

            foreach (TagFillSlotEntry entry in document.slots)
            {
                if (entry == null || !entry.filled || entry.tagId == 0)
                {
                    continue;
                }

                TagKindSlot slot = FindSlot(entry.slotId);

                if (slot == null)
                {
                    Debug.LogWarning($"[TagTextTemplate] 存档里的空缺 {entry.slotId} 在当前模版中不存在，跳过");
                    continue;
                }

                Tag_SO tag = tagSystem != null ? tagSystem.GetTagById(entry.tagId) : null;

                if (tag == null)
                {
                    Debug.LogWarning($"[TagTextTemplate] 找不到 tagId={entry.tagId} 的配置，跳过");
                    continue;
                }

                slot.Place(tag);
                applied++;
            }

            Log($"回填完成：{applied}/{document.SlotCount}");
            return true;
        }

        /// <summary>按 docId 找空缺。</summary>
        public TagKindSlot FindSlot(string slotId)
        {
            EnsureCollected();

            foreach (TagTextSegment segment in mSegments)
            {
                if (segment.IsSlot && segment.Slot.SlotId == slotId)
                {
                    return segment.Slot;
                }
            }

            return null;
        }

        // ---------------- 事件 ----------------

        private void OnSlotChanged(TagKindSlot slot)
        {
            TypeEventSystem.Global.Send(new TagSlotFilledEvent
            {
                Template = this,
                Slot = slot
            });

            SegmentsChanged?.Invoke();
        }

        private void Log(string message)
        {
            if (mLog)
            {
                Debug.Log($"[TagTextTemplate/{name}] {message}");
            }
        }
    }
}