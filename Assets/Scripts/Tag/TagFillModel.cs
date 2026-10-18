using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TapTapFirst
{
    /// <summary>某一个空缺被填了什么：slotId -> tagId。</summary>
    [Serializable]
    public class TagFillSlotEntry
    {
        public string slotId;
        public int tagId;
        public bool filled;

        public TagFillSlotEntry() { }

        public TagFillSlotEntry(string slotId, int tagId, bool filled)
        {
            this.slotId = slotId;
            this.tagId = tagId;
            this.filled = filled;
        }
    }

    /// <summary>
    /// 按【文本顺序】保存的一份完整文本。
    ///
    /// items 与画面上从左到右、从上到下一致：
    ///   固定文本 -> { kind:"text" }
    ///   空缺     -> { kind:"slot" }   （空位也存一条，content 为空串）
    ///
    /// 把 items 里每条的 content 依次拼接，就是补全后的整段文字。
    /// 额外提供 slots（按 slotId 索引）方便读档时直接回填。
    /// </summary>
    [Serializable]
    public class TagFillDocument
    {
        public string documentId = "text_fill";
        public int version = 1;

        /// <summary>按文本顺序排好的序列（含空位）。</summary>
        public List<TagFillItem> items = new List<TagFillItem>();

        /// <summary>空缺索引：slotId -> 内容（方便回填）。</summary>
        public List<TagFillSlotEntry> slots = new List<TagFillSlotEntry>();

        // ---------------- 查询 ----------------

        public TagFillItem GetItemBySlot(string slotId)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].slotId == slotId)
                {
                    return items[i];
                }
            }

            return null;
        }

        public string GetSlotContent(string slotId)
        {
            TagFillItem item = GetItemBySlot(slotId);
            return item != null ? item.content : null;
        }

        public int GetSlotTagId(string slotId)
        {
            TagFillItem item = GetItemBySlot(slotId);
            return item != null ? item.tagId : 0;
        }

        /// <summary>已填的空缺数。</summary>
        public int FilledCount
        {
            get
            {
                int count = 0;

                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] != null && items[i].kind == TagFillItem.KindSlot && items[i].filled)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>空缺总数。</summary>
        public int SlotCount
        {
            get
            {
                int count = 0;

                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] != null && items[i].kind == TagFillItem.KindSlot)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>是否所有空缺都已填满。</summary>
        public bool IsComplete => SlotCount > 0 && FilledCount >= SlotCount;

        /// <summary>把 items 依次拼接成完整文本（空位按 emptyPlaceholder 处理）。</summary>
        public string ToPlainText(string emptyPlaceholder = "")
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < items.Count; i++)
            {
                TagFillItem item = items[i];

                if (item == null)
                {
                    continue;
                }

                if (item.kind == TagFillItem.KindSlot && !item.filled)
                {
                    builder.Append(emptyPlaceholder);
                }
                else
                {
                    builder.Append(item.content);
                }
            }

            return builder.ToString();
        }

        // ---------------- 序列化 ----------------

        public string ToJson(bool pretty = true)
        {
            return JsonUtility.ToJson(this, pretty);
        }

        public static TagFillDocument FromJson(string json)
        {
            return TryParse(json, out TagFillDocument document) ? document : null;
        }

        public static bool TryParse(string json, out TagFillDocument document)
        {
            document = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                document = JsonUtility.FromJson<TagFillDocument>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TagFillDocument] 解析失败：{e.Message}");
                return false;
            }

            if (document == null)
            {
                return false;
            }

            document.items ??= new List<TagFillItem>();
            document.slots ??= new List<TagFillSlotEntry>();
            return true;
        }
    }

    /// <summary>文本序列里的一项：要么是固定文本，要么是一个空缺。</summary>
    [Serializable]
    public class TagFillItem
    {
        public const string KindText = "text";
        public const string KindSlot = "slot";

        /// <summary>"text" 或 "slot"。</summary>
        public string kind = KindText;

        /// <summary>固定文本的内容；空缺时是当时显示的文本。</summary>
        public string content;

        /// <summary>空缺的唯一 Id（kind=="text" 时为空）。</summary>
        public string slotId;

        /// <summary>空缺的词性（kind=="slot" 时有效）。枚举值序列化成 int。</summary>
        public TagOfWords wordType = TagOfWords.adjective;

        /// <summary>空缺里填的 Tag id（0 = 空）。</summary>
        public int tagId;

        /// <summary>空缺是否已填。</summary>
        public bool filled;

        public bool IsSlot => kind == KindSlot;
    }
}