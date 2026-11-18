using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TapTapFirst
{
    /// <summary>某一个 Tag 数值类型的合计（类型 -> 加起来的和）。</summary>
    [Serializable]
    public class NewsTagValueTotal
    {
        public TagValueType type;
        public float total;

        public NewsTagValueTotal() { }

        public NewsTagValueTotal(TagValueType type, float total)
        {
            this.type = type;
            this.total = total;
        }
    }

    /// <summary>
    /// 明细：这个空缺填的是哪个 Tag、这个 Tag 各项数值分别多少。
    /// 主要给"算账 / 回溯 / 显示每一处加了多少分"用，不需要可以忽略。
    /// </summary>
    [Serializable]
    public class NewsTagValueEntry
    {
        public string slotId;
        public int tagId;
        public TagOfWords wordType;
        public string content;
        public List<NewsTagValueTotal> values = new List<NewsTagValueTotal>();

        public float GetValue(TagValueType type)
        {
            if (values != null)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    if (values[i] != null && values[i].type == type)
                    {
                        return values[i].total;
                    }
                }
            }

            return 0f;
        }
    }

    /// <summary>
    /// 一篇【写完的新闻】的结果，四样东西存在一起：
    ///   1. content          拼好的新闻正文（配 items 保留按文本顺序的片段）
    ///   2. tagValueTotals   这篇里用到的所有 Tag 的相应数值，按类型加起来的总值
    ///   3. effectType / effectValue   当前用的模版的类型（客观/正面/负面）及该类型对应的数值
    ///   4. fundsChange / evilChange / credibilityChange   这篇新闻对主角数值（资金 / 邪恶 / 可信度）的改变
    ///
    /// 整体是纯 C# 类，JsonUtility 可直接序列化，丢进 NewsResultSaveData 或别的存档结构都行。
    /// </summary>
    [Serializable]
    public class NewsResultData
    {
        public int version = 1;

        // ---------------- 标识 ----------------

        /// <summary>新闻 Id（对应 NewsTemplate_SO.newsId）。</summary>
        public int newsId;

        /// <summary>新闻标题。</summary>
        public string newsTitle;

        /// <summary>内容文档 Id（TagTextTemplate.DocumentId）。</summary>
        public string documentId;

        // ---------------- 1. 拼好的新闻内容 ----------------

        /// <summary>拼好的完整新闻正文。</summary>
        public string content;

        /// <summary>按文本顺序的片段（含空位；空位的 content 为空串）。</summary>
        public List<TagFillItem> items = new List<TagFillItem>();

        /// <summary>生成这份结果时是否所有空缺都已填满。</summary>
        public bool isComplete;

        // ---------------- 2. 用到的所有 Tag 的数值合计 ----------------

        /// <summary>Tag 数值的分类合计（每个 TagValueType 一条）。</summary>
        public List<NewsTagValueTotal> tagValueTotals = new List<NewsTagValueTotal>();

        /// <summary>计入合计的空缺数（同一个 Tag 用了两次就算两次）。</summary>
        public int tagCount;

        /// <summary>用到的 Tag Id（已去重）。</summary>
        public List<int> usedTagIds = new List<int>();

        /// <summary>每个空缺的明细。</summary>
        public List<NewsTagValueEntry> tagDetails = new List<NewsTagValueEntry>();

        // ---------------- 3. 模版倾向及其数值 ----------------

        /// <summary>当前用的模版的类型：客观 / 正面 / 负面。</summary>
        public NewsEffectType effectType = NewsEffectType.Objective;

        /// <summary>该类型对应的数值（NewsTemplate_SO.GetEffectValue()）。</summary>
        public float effectValue;

        public int pageViews;

        // ---------------- 4. 对主角数值（资金 / 邪恶 / 可信度）的改变 ----------------

        /// <summary>该新闻对主角【资金】的改变（= 每篇固定 +5）。</summary>
        public int fundsChange;

        /// <summary>
        /// 该新闻对主角【邪恶】的改变（带符号，直接喂给 EvilValueCommand）：
        /// 消极（负面）模版为 +，正面为 −，客观模版为 0；幅度 3~8，浏览量越大越接近上限。
        /// </summary>
        public int evilChange;

        /// <summary>
        /// 该新闻对主角【可信度】的改变（带符号，直接喂给 CredibilityValueCommand）：
        /// 客观模版为 +，其余（正面 / 负面）为 −；幅度 3~8，胡扯 Tag 占比越高越接近上限。
        /// </summary>
        public int credibilityChange;

        // ============================================================
        //  获取：新闻内容
        // ============================================================

        /// <summary>拼好的新闻正文。</summary>
        public string GetContent()
        {
            if (!string.IsNullOrEmpty(content))
            {
                return content;
            }

            return GetPlainText();
        }

        /// <summary>按 items 现场拼接（空位用 emptyPlaceholder）；content 有值就直接用它。</summary>
        public string GetPlainText(string emptyPlaceholder = "")
        {
            if (items == null || items.Count == 0)
            {
                return content ?? string.Empty;
            }

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

        /// <summary>有没有正文。</summary>
        public bool HasContent => !string.IsNullOrEmpty(GetContent());

        /// <summary>空位数。</summary>
        public int SlotCount
        {
            get
            {
                int count = 0;

                if (items != null)
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (items[i] != null && items[i].kind == TagFillItem.KindSlot)
                        {
                            count++;
                        }
                    }
                }

                return count;
            }
        }

        /// <summary>已填空位数。</summary>
        public int FilledCount
        {
            get
            {
                int count = 0;

                if (items != null)
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (items[i] != null && items[i].kind == TagFillItem.KindSlot && items[i].filled)
                        {
                            count++;
                        }
                    }
                }

                return count;
            }
        }

        // ============================================================
        //  获取：所有 Tag 数值加起来的总值
        // ============================================================

        /// <summary>
        /// 取指定类型的总值 —— 这篇新闻里所有已填 Tag 的该类型数值之和。
        /// 例：GetTagValueTotal(TagValueType.PageViews) = 所有 Tag 的浏览量加起来。
        /// </summary>
        public float GetTagValueTotal(TagValueType type)
        {
            if (tagValueTotals != null)
            {
                for (int i = 0; i < tagValueTotals.Count; i++)
                {
                    NewsTagValueTotal total = tagValueTotals[i];

                    if (total != null && total.type == type)
                    {
                        return total.total;
                    }
                }
            }

            return 0f;
        }

        /// <summary>取指定类型的总值；没这条返回 false（out 为 0）。</summary>
        public bool TryGetTagValueTotal(TagValueType type, out float total)
        {
            total = 0f;

            if (tagValueTotals == null)
            {
                return false;
            }

            for (int i = 0; i < tagValueTotals.Count; i++)
            {
                NewsTagValueTotal item = tagValueTotals[i];

                if (item != null && item.type == type)
                {
                    total = item.total;
                    return true;
                }
            }

            return false;
        }

        /// <summary>所有类型再全部加一起的总值（不需要分类型时用它）。</summary>
        public float GetTagValueTotal()
        {
            float sum = 0f;

            if (tagValueTotals != null)
            {
                for (int i = 0; i < tagValueTotals.Count; i++)
                {
                    if (tagValueTotals[i] != null)
                    {
                        sum += tagValueTotals[i].total;
                    }
                }
            }

            return sum;
        }

        public float GetPageViews()
        {
            return new Calculate().CalculatePageViews(PageViewsTotal,GetEffectValue());
        }

        /// <summary>浏览量总值。</summary>
        public float PageViewsTotal => GetTagValueTotal(TagValueType.PageViews);

        /// <summary>压力值总值。</summary>
        public float PressureValueTotal => GetTagValueTotal(TagValueType.PressureValue);

        /// <summary>计入合计的 Tag 个数（按空缺算）。</summary>
        public int TagCount => tagCount;

        /// <summary>用到的 Tag Id（去重）的副本。</summary>
        public List<int> GetUsedTagIds()
        {
            return usedTagIds != null ? new List<int>(usedTagIds) : new List<int>();
        }

        /// <summary>取某个空缺的明细；找不到返回 null。</summary>
        public NewsTagValueEntry GetTagDetail(string slotId)
        {
            if (tagDetails != null)
            {
                for (int i = 0; i < tagDetails.Count; i++)
                {
                    if (tagDetails[i] != null && tagDetails[i].slotId == slotId)
                    {
                        return tagDetails[i];
                    }
                }
            }

            return null;
        }

        // ============================================================
        //  获取：模版类型及其数值
        // ============================================================

        /// <summary>当前模版的类型：客观 / 正面 / 负面。</summary>
        public NewsEffectType GetEffectType()
        {
            return effectType;
        }

        /// <summary>当前模版类型对应的数值。</summary>
        public float GetEffectValue()
        {
            return effectValue;
        }

        /// <summary>当前模版类型对应的数值（按类型重取，防止存档里类型被改过）。</summary>
        public float GetEffectValue(NewsTemplate_SO template)
        {
            return template != null ? template.GetEffectValue(effectType) : effectValue;
        }

        /// <summary>当前模版类型的中文名：客观 / 正面 / 负面。</summary>
        public string GetEffectTypeName()
        {
            return NewsEffectTypeNames.Get(effectType);
        }

        // ============================================================
        //  获取：对主角数值（资金 / 邪恶 / 可信度）的改变
        //  （数值怎么算在 Calculate 里，这里只是记录 + 读取）
        // ============================================================

        /// <summary>资金变化（浏览量 / 1000）。</summary>
        public int GetFundsChange()
        {
            return fundsChange;
        }

        /// <summary>邪恶变化（带符号：负面模版为正）。</summary>
        public int GetEvilChange()
        {
            return evilChange;
        }

        /// <summary>可信度变化（带符号：客观模版为正）。</summary>
        public int GetCredibilityChange()
        {
            return credibilityChange;
        }

        /// <summary>
        /// 新闻用到的 Tag 里【胡扯（blbl）】的占比：0~1。
        /// 按已填的空缺算（同一个 Tag 用了两次就算两次）——空缺的词性是「胡扯」，
        /// 那么这一段读起来就是胡扯，可信度那一路的幅度就是按它算的。
        /// </summary>
        public float NonsenseRatio
        {
            get
            {
                if (tagDetails == null || tagDetails.Count == 0)
                {
                    return 0f;
                }

                int nonsenseCount = 0;

                for (int i = 0; i < tagDetails.Count; i++)
                {
                    NewsTagValueEntry entry = tagDetails[i];

                    if (entry != null && entry.wordType == TagOfWords.blbl)
                    {
                        nonsenseCount++;
                    }
                }

                return Mathf.Clamp01((float)nonsenseCount / tagDetails.Count);
            }
        }

        // ============================================================
        //  还原
        // ============================================================

        /// <summary>
        /// 把这份结果还原成填空文档（读档回填用）。
        /// 没有草稿但已经提交过的新闻，靠它把写作区恢复原样。
        /// </summary>
        public TagFillDocument ToDocument()
        {
            TagFillDocument document = new TagFillDocument
            {
                documentId = documentId,
                version = version,
                items = new List<TagFillItem>()
            };

            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    TagFillItem item = items[i];

                    if (item == null)
                    {
                        continue;
                    }

                    document.items.Add(item);

                    if (item.kind == TagFillItem.KindSlot)
                    {
                        document.slots.Add(new TagFillSlotEntry(item.slotId, item.tagId, item.filled));
                    }
                }
            }

            return document;
        }

        // ============================================================
        //  序列化
        // ============================================================

        public string ToJson(bool pretty = true)
        {
            return JsonUtility.ToJson(this, pretty);
        }

        public static NewsResultData FromJson(string json)
        {
            return TryParse(json, out NewsResultData result) ? result : null;
        }

        public static bool TryParse(string json, out NewsResultData result)
        {
            result = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                result = JsonUtility.FromJson<NewsResultData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NewsResultData] 解析失败：{e.Message}");
                return false;
            }

            if (result == null)
            {
                return false;
            }

            result.items ??= new List<TagFillItem>();
            result.tagValueTotals ??= new List<NewsTagValueTotal>();
            result.usedTagIds ??= new List<int>();
            result.tagDetails ??= new List<NewsTagValueEntry>();
            return true;
        }

        public override string ToString()
        {
            return $"[NewsResult] newsId={newsId} 倾向={GetEffectTypeName()}({effectValue}) " +
                   $"Tag数={tagCount} 浏览量合计={PageViewsTotal} 压力合计={PressureValueTotal} " +
                   $"资金={fundsChange} 邪恶={evilChange} 可信度={credibilityChange}\n{GetContent()}";
        }
    }

    /// <summary>
    /// 写完的新闻结果的存档载体：newsId -> Json 字符串。
    /// 在 TapTap.Init() 里 save.Add&lt;NewsResultSaveData&gt;() 注册后即可用。
    /// </summary>
    [Serializable]
    public class NewsResultSaveData
    {
        public List<NewsResultEntryData> results = new List<NewsResultEntryData>();

        public void Set(int newsId, string json)
        {
            NewsResultEntryData entry = Find(newsId);

            if (entry == null)
            {
                results.Add(new NewsResultEntryData(newsId, json));
                return;
            }

            entry.json = json;
        }

        /// <summary>存一份结果（顺带存它的 Json）。</summary>
        public void SetResult(NewsResultData result)
        {
            if (result == null)
            {
                return;
            }

            Set(result.newsId, result.ToJson(false));
        }

        public string Get(int newsId)
        {
            NewsResultEntryData entry = Find(newsId);
            return entry != null ? entry.json : null;
        }

        public bool TryGet(int newsId, out string json)
        {
            json = Get(newsId);
            return !string.IsNullOrEmpty(json);
        }

        /// <summary>直接取解析好的结果；没有就返回 null。</summary>
        public NewsResultData GetResult(int newsId)
        {
            return TryGet(newsId, out string json) ? NewsResultData.FromJson(json) : null;
        }

        public bool TryGetResult(int newsId, out NewsResultData result)
        {
            result = GetResult(newsId);
            return result != null;
        }

        /// <summary>有没有这篇新闻的结果。</summary>
        public bool Has(int newsId)
        {
            return Find(newsId) != null;
        }

        public bool Remove(int newsId)
        {
            NewsResultEntryData entry = Find(newsId);
            return entry != null && results.Remove(entry);
        }

        /// <summary>所有已结算新闻的 Id。</summary>
        public List<int> GetAllNewsIds()
        {
            List<int> ids = new List<int>();

            for (int i = 0; i < results.Count; i++)
            {
                if (results[i] != null)
                {
                    ids.Add(results[i].newsId);
                }
            }

            return ids;
        }

        private NewsResultEntryData Find(int newsId)
        {
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i] != null && results[i].newsId == newsId)
                {
                    return results[i];
                }
            }

            return null;
        }
    }

    [Serializable]
    public class NewsResultEntryData
    {
        public int newsId;
        public string json;

        public NewsResultEntryData() { }

        public NewsResultEntryData(int newsId, string json)
        {
            this.newsId = newsId;
            this.json = json;
        }
    }
}