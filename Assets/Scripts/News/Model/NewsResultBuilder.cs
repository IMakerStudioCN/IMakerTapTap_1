using System;
using System.Collections.Generic;
using UnityEngine;

namespace TapTapFirst
{
    /// <summary>
    /// 把"一篇写完的新闻"结算成 NewsResultData：
    ///   拼好的正文 + 所有用到的 Tag 的相应数值合计 + 模版类型及其数值。
    ///
    /// 用法（编辑中直接结算）：
    ///   NewsResultData result = NewsResultBuilder.Build(currentNews, currentTemplate);
    ///
    /// 用法（从存档的 Json 结算）：
    ///   NewsResultData result = NewsResultBuilder.BuildFromJson(currentNews, savedJson);
    /// </summary>
    public static class NewsResultBuilder
    {
        /// <summary>从 TagTextTemplate 现场结算（会先 Collect 一次，拿到当前填空状态）。</summary>
        public static NewsResultData Build(NewsTemplate_SO template, TagTextTemplate textTemplate, ITagSystem tagSystem = null)
        {
            if (textTemplate == null)
            {
                Debug.LogWarning("[NewsResultBuilder] textTemplate 为空，无法结算");
                return null;
            }

            return Build(template, textTemplate.BuildDocument(), tagSystem);
        }

        /// <summary>从存档 Json 结算（读档后也能算出同样的结果）。</summary>
        public static NewsResultData BuildFromJson(NewsTemplate_SO template, string json, ITagSystem tagSystem = null)
        {
            if (!TagFillDocument.TryParse(json, out TagFillDocument document))
            {
                return null;
            }

            return Build(template, document, tagSystem);
        }

        /// <summary>核心：按文档结算。</summary>
        public static NewsResultData Build(NewsTemplate_SO template, TagFillDocument document, ITagSystem tagSystem = null)
        {
            if (document == null)
            {
                Debug.LogWarning("[NewsResultBuilder] document 为空，无法结算");
                return null;
            }

            if (tagSystem == null)
            {
                tagSystem = TapTap.Interface.GetSystem<ITagSystem>();
            }

            NewsResultData result = new NewsResultData
            {
                // 2 = 增加了对主角数值（资金 / 邪恶 / 可信度）的影响；读档时靠它判断要不要补算
                version = 2,
                newsId = template != null ? template.newsId : 0,
                newsTitle = template != null ? template.newsTitle : string.Empty,
                documentId = document.documentId,

                // 1) 拼好的新闻内容
                content = document.ToPlainText(),
                items = document.items,
                isComplete = document.IsComplete,

                // 3) 当前用的模版的类型 + 该类型对应的数值
                effectType = template != null ? template.newsEffectType : NewsEffectType.Objective,
                effectValue = template != null ? template.GetEffectValue() : 0f,

                
            };

            // 2) 用到的所有 Tag 的相应数值加起来
            AccumulateTagValues(result, document, tagSystem);

            result.pageViews = new Calculate().CalculatePageViews((int)result.GetTagValueTotal(TagValueType.PageViews), result.effectValue);

            // 4) 这篇新闻对主角数值（资金 / 邪恶 / 可信度）的改变
            ApplyPlayerStatChanges(result);

            return result;
        }

        /// <summary>
        /// 结算这篇新闻对主角数值的影响（只记录，不直接改主角，UI/系统自己决定发不发 Command）：
        ///   资金   = 每篇固定 +5                              （恒为正，与浏览量无关）
        ///   邪恶   = 负面模版 +，正面 −，客观 0                （幅度 3~8，随浏览量）
        ///   可信度 = 客观模版 +，正面 / 负面 −                 （幅度 3~8，随胡扯 Tag 占比）
        /// 具体公式都在 Calculate 里，改规则只改那里。
        ///
        /// 想把它应用到主角身上：
        ///   this.SendCommand(new FundsValueCommand(result.fundsChange));
        ///   this.SendCommand(new EvilValueCommand(result.evilChange));
        ///   this.SendCommand(new CredibilityValueCommand(result.credibilityChange));
        /// </summary>
        public static void ApplyPlayerStatChanges(NewsResultData result)
        {
            if (result == null)
            {
                return;
            }

            Calculate calculate = new Calculate();

            result.fundsChange = calculate.CalculateFundsChange(result.pageViews);
            result.evilChange = calculate.CalculateEvilChange(result.pageViews, result.effectType);
            result.credibilityChange = calculate.CalculateCredibilityChange(result.NonsenseRatio, result.effectType);
        }

        /// <summary>
        /// 把文档里所有【已填】空缺的 Tag 数值按 TagValueType 累加进 result。
        /// 同一个 Tag 填了两个空缺就累加两次；空位 / tagId 为 0 的不计。
        /// </summary>
        public static void AccumulateTagValues(NewsResultData result, TagFillDocument document, ITagSystem tagSystem = null)
        {
            if (result == null || document == null || document.items == null)
            {
                return;
            }

            result.tagValueTotals ??= new List<NewsTagValueTotal>();
            result.tagDetails ??= new List<NewsTagValueEntry>();
            result.usedTagIds ??= new List<int>();

            result.tagValueTotals.Clear();
            result.tagDetails.Clear();
            result.usedTagIds.Clear();
            result.tagCount = 0;

            TagValueType[] types = (TagValueType[])Enum.GetValues(typeof(TagValueType));

            for (int i = 0; i < document.items.Count; i++)
            {
                TagFillItem item = document.items[i];

                if (item == null ||
                    item.kind != TagFillItem.KindSlot ||
                    !item.filled ||
                    item.tagId == 0)
                {
                    continue;
                }

                Tag_SO tag = tagSystem != null ? tagSystem.GetTagById(item.tagId) : null;

                if (tag == null)
                {
                    Debug.LogWarning($"[NewsResultBuilder] 空缺 {item.slotId} 的 tagId={item.tagId} 在配置里找不到，跳过");
                    continue;
                }

                NewsTagValueEntry detail = new NewsTagValueEntry
                {
                    slotId = item.slotId,
                    tagId = item.tagId,
                    wordType = item.wordType,
                    content = item.content
                };

                // 这个 Tag 的每一项数值：进合计，同时留在明细里
                for (int t = 0; t < types.Length; t++)
                {
                    float value = tag.GetValue(types[t]);
                    detail.values.Add(new NewsTagValueTotal(types[t], value));
                    AddTotal(result.tagValueTotals, types[t], value);
                }

                result.tagDetails.Add(detail);
                result.tagCount++;

                if (!result.usedTagIds.Contains(item.tagId))
                {
                    result.usedTagIds.Add(item.tagId);
                }
            }

            
        }

        /// <summary>把某个类型的数值累加进合计表；没有这条就新建。</summary>
        private static void AddTotal(List<NewsTagValueTotal> totals, TagValueType type, float value)
        {
            for (int i = 0; i < totals.Count; i++)
            {
                NewsTagValueTotal total = totals[i];

                if (total != null && total.type == type)
                {
                    total.total += value;
                    return;
                }
            }

            totals.Add(new NewsTagValueTotal(type, value));
        }

        /// <summary>
        /// 只要"所有 Tag 数值加起来的总值"、不需要完整结果时用它。
        /// type 传 null = 所有类型加一起。
        /// </summary>
        public static float SumTagValues(TagFillDocument document, TagValueType? type = null, ITagSystem tagSystem = null)
        {
            NewsResultData result = Build(null, document, tagSystem);
            return result == null ? 0f : (type.HasValue ? result.GetTagValueTotal(type.Value) : result.GetTagValueTotal());
        }

        /// <summary>同上，但直接从画面上的模版结算。</summary>
        public static float SumTagValues(TagTextTemplate textTemplate, TagValueType? type = null, ITagSystem tagSystem = null)
        {
            return textTemplate == null ? 0f : SumTagValues(textTemplate.BuildDocument(), type, tagSystem);
        }
    }
}