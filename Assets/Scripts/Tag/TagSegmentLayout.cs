using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

namespace TapTapFirst
{
    /// <summary>排版参数。</summary>
    public class TagLayoutSettings
    {
        public Vector4 Padding = new Vector4(24f, 16f, 24f, 16f);
        public float SegmentSpacing = 8f;
        public float LineSpacing = 12f;
        public float MinFontSize = 16f;
        public float MaxFontSize = 48f;
        public float SlotScale = 1f;
        public int MaxSegmentsPerLine = 0;

        /// <summary>从左到右的行序（true = 正常阅读顺序）</summary>
        public bool LeftToRight = true;
    }

    /// <summary>
    /// 文本模版的排版引擎。
    /// 生成器（编辑器）和 TagTextAdaptiveLayout（运行时）共用这一份实现，
    /// 这样编辑器里看到的位置和运行时完全一致。
    /// </summary>
    public static class TagSegmentLayout
    {
        private struct RowItem
        {
            public RectTransform Rect;
            public string Text;
            public bool IsSlot;
            public float BaseFontSize;
            public float SlotBoxWidth;
            public float SlotBoxHeight;
        }

        /// <summary>
        /// 按 root 的矩形把 segments 排好。
        /// 返回 false 表示尺寸还没就绪（调用方可以下一帧重试）。
        /// </summary>
        public static bool Layout(RectTransform root, IReadOnlyList<TagTextSegment> segments,
            TagLayoutSettings settings, out string report)
        {
            report = string.Empty;

            if (root == null || segments == null || segments.Count == 0)
            {
                report = "没有可排版的片段";
                return false;
            }

            if (settings == null)
            {
                settings = new TagLayoutSettings();
            }

            Vector2 selfSize = root.rect.size;
            Vector2 parentSize = root.parent is RectTransform parentRect ? parentRect.rect.size : Vector2.zero;

            // 可用区域优先用父节点尺寸；父节点不可用时用自身尺寸
            float boxWidth = parentSize.x > 1f ? parentSize.x : selfSize.x;
            float boxHeight = parentSize.y > 1f ? parentSize.y : selfSize.y;

            if (boxWidth <= 1f)
            {
                report = "可用宽度还是 0（等布局算完）";
                return false;
            }

            float availableWidth = Mathf.Max(1f, boxWidth - settings.Padding.x - settings.Padding.z);

            // ---------- 收集 ----------
            List<RowItem> items = new List<RowItem>(segments.Count);
            float baseFontSize = Mathf.Clamp(settings.MaxFontSize, settings.MinFontSize, settings.MaxFontSize);

            foreach (TagTextSegment segment in segments)
            {
                RectTransform rect;
                string text;

                if (segment.IsSlot)
                {
                    rect = segment.Slot != null ? segment.Slot.transform as RectTransform : null;
                    text = segment.Slot != null && segment.Slot.IsFilled
                        ? segment.Slot.Content
                        : GetPlaceholderText(segment.Slot);
                }
                else
                {
                    rect = segment.TextObject != null ? segment.TextObject.transform as RectTransform : null;
                    text = segment.Text;
                }

                if (rect == null)
                {
                    continue;
                }

                items.Add(new RowItem
                {
                    Rect = rect,
                    Text = text ?? string.Empty,
                    IsSlot = segment.IsSlot,
                    BaseFontSize = baseFontSize,
                    SlotBoxWidth = Mathf.Max(rect.sizeDelta.x, 40f),
                    SlotBoxHeight = Mathf.Max(rect.sizeDelta.y, 30f),
                });
            }

            if (items.Count == 0)
            {
                report = "没有可排版的元素";
                return false;
            }

            // ---------- 贪心折行 ----------
            List<List<int>> lines = new List<List<int>>();
            List<int> current = new List<int>();
            float currentWidth = 0f;

            for (int i = 0; i < items.Count; i++)
            {
                float width = ItemBoxWidth(items[i], items[i].BaseFontSize);

                bool overflow = current.Count > 0 &&
                                currentWidth + settings.SegmentSpacing + width > availableWidth;
                bool tooMany = settings.MaxSegmentsPerLine > 0 &&
                               current.Count >= settings.MaxSegmentsPerLine;

                if (overflow || (tooMany && current.Count > 0))
                {
                    lines.Add(current);
                    current = new List<int>();
                    currentWidth = 0f;
                }

                if (current.Count > 0)
                {
                    currentWidth += settings.SegmentSpacing;
                }

                current.Add(i);
                currentWidth += width;
            }

            if (current.Count > 0)
            {
                lines.Add(current);
            }

            // ---------- 超宽行缩字号 ----------
            float[] fontSizeOfItem = new float[items.Count];

            for (int i = 0; i < items.Count; i++)
            {
                fontSizeOfItem[i] = items[i].BaseFontSize;
            }

            foreach (List<int> line in lines)
            {
                float totalWidth = MeasureLineWidth(items, line, fontSizeOfItem, settings);

                if (totalWidth <= availableWidth)
                {
                    continue;
                }

                float scale = availableWidth / totalWidth;

                foreach (int index in line)
                {
                    fontSizeOfItem[index] = Mathf.Max(settings.MinFontSize, fontSizeOfItem[index] * scale);
                }
            }

            // ---------- 行高 ----------
            List<float> lineHeights = new List<float>(lines.Count);
            float totalHeight = 0f;

            foreach (List<int> line in lines)
            {
                float height = 0f;

                foreach (int index in line)
                {
                    height = Mathf.Max(height, MeasureHeight(items[index], fontSizeOfItem[index]));
                }

                lineHeights.Add(height);
                totalHeight += height;
            }

            totalHeight += settings.LineSpacing * Mathf.Max(0, lines.Count - 1);

            // ---------- 落位 ----------
            float topY = totalHeight * 0.5f;
            float minX = float.MaxValue;
            float maxX = float.MinValue;

            for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                List<int> line = lines[lineIndex];
                float lineHeight = lineHeights[lineIndex];
                float lineWidth = MeasureLineWidth(items, line, fontSizeOfItem, settings);

                float x = -lineWidth * 0.5f;
                float rowCenterY = topY - lineHeight * 0.5f;

                foreach (int index in line)
                {
                    RowItem item = items[index];
                    float fontSize = fontSizeOfItem[index];
                    float scale = item.BaseFontSize > 0.001f ? fontSize / item.BaseFontSize : 1f;

                    float boxW = ItemBoxWidth(item, fontSize);
                    float boxH = Mathf.Max(MeasureHeight(item, fontSize),
                        item.SlotBoxHeight * scale * settings.SlotScale);

                    item.Rect.anchorMin = new Vector2(0.5f, 0.5f);
                    item.Rect.anchorMax = new Vector2(0.5f, 0.5f);
                    item.Rect.pivot = new Vector2(0.5f, 0.5f);
                    item.Rect.sizeDelta = new Vector2(boxW, boxH);
                    item.Rect.anchoredPosition = new Vector2(x + boxW * 0.5f, rowCenterY);

                    ApplyText(item.Rect, item.Text, fontSize);

                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x + boxW);

                    x += boxW + settings.SegmentSpacing;
                }

                topY -= lineHeight + settings.LineSpacing;
            }

            report = $"{items.Count} 段 / {lines.Count} 行 / 可用宽 {availableWidth:0.##} / " +
                     $"内容 {Mathf.Max(0f, maxX - minX):0.##}x{totalHeight:0.##} / 基准字号 {baseFontSize:0.##}";

            return true;
        }

        // ---------------- 内部 ----------------

        private static float ItemBoxWidth(RowItem item, float fontSize)
        {
            float textWidth = MeasureWidth(item.Text, fontSize);
            float scale = item.BaseFontSize > 0.001f ? fontSize / item.BaseFontSize : 1f;
            float minWidth = item.SlotBoxWidth * scale;

            return item.IsSlot ? Mathf.Max(textWidth + 16f, minWidth) : textWidth;
        }

        private static float MeasureLineWidth(List<RowItem> items, List<int> line,
            float[] fontSizeOfItem, TagLayoutSettings settings)
        {
            float width = 0f;

            for (int i = 0; i < line.Count; i++)
            {
                width += ItemBoxWidth(items[line[i]], fontSizeOfItem[line[i]]);

                if (i < line.Count - 1)
                {
                    width += settings.SegmentSpacing;
                }
            }

            return width;
        }

        private static float MeasureHeight(RowItem item, float fontSize)
        {
            return fontSize * 1.3f;
        }

        /// <summary>估算文本宽度：中文按 1.0 个字宽、ASCII 按 0.55 估算。</summary>
        public static float MeasureWidth(string text, float fontSize)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0f;
            }

            float units = 0f;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c == '\n' || c == '\r')
                {
                    continue;
                }

                units += c < 0x2E80 ? 0.55f : 1.0f;
            }

            return units * fontSize;
        }

        /// <summary>尺寸是否已经有效可用。</summary>
        public static bool HasUsableSize(RectTransform root)
        {
            if (root == null)
            {
                return false;
            }

            float w = root.rect.width;

            if (w <= 1f && root.parent is RectTransform parentRect)
            {
                w = parentRect.rect.width;
            }

            return w > 1f;
        }

        private static string GetPlaceholderText(TagKindSlot slot)
        {
            if (slot == null)
            {
                return string.Empty;
            }

            Transform placeholder = slot.transform.Find("Placeholder");

            if (placeholder == null)
            {
                return "____";
            }

            TMP_Text tmp = placeholder.GetComponent<TMP_Text>();

            if (tmp != null)
            {
                return tmp.text;
            }

            Text uiText = placeholder.GetComponent<Text>();
            return uiText != null ? uiText.text : "____";
        }

        private static void ApplyText(RectTransform rect, string text, float fontSize)
        {
            TMP_Text tmp = rect.GetComponent<TMP_Text>();

            if (tmp != null)
            {
                tmp.text = text;
                tmp.fontSize = fontSize;
                return;
            }

            Text uiText = rect.GetComponent<Text>();

            if (uiText != null)
            {
                uiText.text = text;
                uiText.fontSize = Mathf.RoundToInt(fontSize);
            }
        }
    }
}