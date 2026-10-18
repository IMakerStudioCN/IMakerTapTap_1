using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TapTapFirst
{
    /// <summary>写作模版里的一段固定文本。</summary>
    [Serializable]
    public class NewsTextPart
    {
        [Tooltip("这一段固定文本的内容")]
        public string text = "";
    }

    /// <summary>写作模版里的一个空缺。</summary>
    [Serializable]
    public class NewsSlotPart
    {
        [Tooltip("空缺唯一 Id（存档按它定位，同一条新闻内不要重复）")]
        public string slotId = "slot_1";

        [Tooltip("词性：0 形容词 / 1 名词 / 2 量词 / 3 胡扯（对应 Tag_SO.theTypeToContent 的下标）")]
        public TagOfWords wordType = TagOfWords.adjective;
    }

    /// <summary>
    /// 内容片段：要么是一句固定文本，要么是一个空缺。
    /// isSlot=false 时用 text，isSlot=true 时用 slot。
    /// </summary>
    [Serializable]
    public class NewsContentSegment
    {
        [Tooltip("勾上 = 空缺，不勾 = 固定文本")]
        public bool isSlot;

        [Tooltip("固定文本内容（isSlot=false 时有效）")]
        public string text = "";

        [Tooltip("空缺配置（isSlot=true 时有效）")]
        public NewsSlotPart slot = new NewsSlotPart();

        public static NewsContentSegment Text(string value)
        {
            return new NewsContentSegment { isSlot = false, text = value };
        }

        public static NewsContentSegment Slot(string slotId, TagOfWords wordType)
        {
            return new NewsContentSegment
            {
                isSlot = true,
                slot = new NewsSlotPart { slotId = slotId, wordType = wordType }
            };
        }
    }

    /// <summary>
    /// 新闻的感情倾向类型：客观 / 正面 / 负面（三选一）。
    /// 下标即 effectValues 里的 key：0 客观 / 1 正面 / 2 负面。
    /// </summary>
    public enum NewsEffectType
    {
        Objective = 0,  // 客观
        Positive = 1,   // 正面
        Negative = 2,   // 负面
    }

    /// <summary>新闻倾向的中文名，编辑器/调试用（下标与 NewsEffectType 一致）。</summary>
    public static class NewsEffectTypeNames
    {
        public static readonly string[] All = { "客观", "正面", "负面" };

        public static string Get(NewsEffectType type)
        {
            int index = (int)type;
            return index >= 0 && index < All.Length ? All[index] : type.ToString();
        }
    }

    /// <summary>一对"倾向类型 -> 数值"。</summary>
    [Serializable]
    public class NewsEffectPair
    {
        public NewsEffectType type = NewsEffectType.Objective;
        public float value;

        public NewsEffectPair() { }

        public NewsEffectPair(NewsEffectType type, float value)
        {
            this.type = type;
            this.value = value;
        }
    }

    [CreateAssetMenu(fileName = "NewsTemplate", menuName = "ScriptableObjects/NewsTemplate_SO", order = 1)]
    public class NewsTemplate_SO : ScriptableObject
    {
        [Header("标识")]
        [SerializeField]
        public int newsId;

        [SerializeField]
        public string newsTitle;

        [Header("写作内容")]
        [Tooltip("这条新闻专属的内容预制体在 ResKit 里的资源名（根节点需带 TagTextTemplate）。" +
                 "留空 = 用 NewsController 上的默认内容预制体名。")]
        [SerializeField]
        public string contentResName;

        [Tooltip("内容片段：按顺序排「固定文本 / 空缺」。点选模板时按它生成写作区内容。")]
        [SerializeField]
        public List<NewsContentSegment> contentSegments = new List<NewsContentSegment>();

        [Header("卡片")]
        [Tooltip("模板列表卡片上显示的文字；留空则用 newsTitle")]
        [SerializeField]
        public string cardTitleOverride;

        [Header("倾向 / 数值")]
        [Tooltip("这条新闻的感情倾向：客观 / 正面 / 负面（三选一）。" +
                 "GetEffectValue() 会按它到 Effect Values 里取对应数值。")]
        [SerializeField]
        public NewsEffectType newsEffectType = NewsEffectType.Objective;

        [Tooltip("每种倾向各存一个 float，取法：GetEffectValue(NewsEffectType.Positive)。")]
        [SerializeField]
        public List<NewsEffectPair> effectValues = new List<NewsEffectPair>();

        [Header("限时")]
        [Tooltip("勾上 = 限时模版：获得后经过 Limit Days 个游戏日，就从可写列表里消失。" +
                 "已经写好并发送到贴吧的新闻不受影响。")]
        [SerializeField]
        public bool isTimeLimited;

        [Tooltip("限时天数：获得后经过这么多个游戏日消失。<= 0 视为不限时。")]
        [SerializeField]
        public int limitDays = 7;

        /// <summary>卡片显示文字。</summary>
        public string CardTitle => string.IsNullOrEmpty(cardTitleOverride) ? newsTitle : cardTitleOverride;

        /// <summary>这条配置里有没有可用的内容片段。</summary>
        public bool HasContent => contentSegments != null && contentSegments.Count > 0;

        /// <summary>是不是限时模版（勾了限时且天数 > 0 才算）。</summary>
        public bool IsTimeLimited => isTimeLimited && limitDays > 0;

        /// <summary>
        /// 限时模版的到期日 = 获得日 + 限时天数。
        /// 到期当天（CurrentDay &gt;= 到期日）就算过期，也就是能写 limitDays 天。
        /// 不限时返回 0。
        /// </summary>
        public int GetExpireDay(int acquireDay)
        {
            return IsTimeLimited ? acquireDay + limitDays : 0;
        }

        /// <summary>空缺数量。</summary>
        public int SlotCount
        {
            get
            {
                if (contentSegments == null)
                {
                    return 0;
                }

                int count = 0;

                foreach (NewsContentSegment segment in contentSegments)
                {
                    if (segment != null && segment.isSlot)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>
        /// 转成 TagTextTemplate.Configure 需要的序列（string = 固定文本，SlotSetup = 空缺）。
        /// </summary>
        public List<object> BuildSequence()
        {
            List<object> sequence = new List<object>();

            if (contentSegments == null)
            {
                return sequence;
            }

            int autoIndex = 1;

            foreach (NewsContentSegment segment in contentSegments)
            {
                if (segment == null)
                {
                    continue;
                }

                if (segment.isSlot)
                {
                    NewsSlotPart slot = segment.slot ?? new NewsSlotPart();
                    string slotId = string.IsNullOrEmpty(slot.slotId) ? $"slot_{autoIndex}" : slot.slotId;
                    autoIndex++;

                    sequence.Add(new SlotSetup(slotId, slot.wordType));
                }
                else
                {
                    sequence.Add(segment.text ?? "");
                }
            }

            return sequence;
        }

        // ============================================================
        //  倾向数值查询（effectValues）
        // ============================================================

        /// <summary>取的当前倾向（newsEffectType）对应的数值；没配过返回 0。</summary>
        public float GetEffectValue()
        {
            return GetEffectValue(newsEffectType);
        }

        /// <summary>取指定倾向的数值；没配过返回 0。</summary>
        public float GetEffectValue(NewsEffectType type)
        {
            TryGetEffectValue(type, out float value);
            return value;
        }

        /// <summary>取指定倾向的数值；没配过返回 false（out 为 0）。</summary>
        public bool TryGetEffectValue(NewsEffectType type, out float value)
        {
            value = 0f;

            if (effectValues == null)
            {
                return false;
            }

            for (int i = 0; i < effectValues.Count; i++)
            {
                NewsEffectPair pair = effectValues[i];

                if (pair != null && pair.type == type)
                {
                    value = pair.value;
                    return true;
                }
            }

            return false;
        }

        /// <summary>写指定倾向的数值；没有这条就新建。</summary>
        public void SetEffectValue(NewsEffectType type, float value)
        {
            if (effectValues == null)
            {
                effectValues = new List<NewsEffectPair>();
            }

            for (int i = 0; i < effectValues.Count; i++)
            {
                NewsEffectPair pair = effectValues[i];

                if (pair != null && pair.type == type)
                {
                    pair.value = value;
                    return;
                }
            }

            effectValues.Add(new NewsEffectPair(type, value));
        }

        /// <summary>当前倾向数值（等价 GetEffectValue()）。</summary>
        public float EffectValue => GetEffectValue();

        /// <summary>当前倾向的中文名：客观 / 正面 / 负面。</summary>
        public string EffectTypeName => NewsEffectTypeNames.Get(newsEffectType);

        /// <summary>把 effectValues 补齐到三种倾向各一条（生成器 / 编辑器用）。</summary>
        public void EnsureEffectValues()
        {
            foreach (NewsEffectType type in Enum.GetValues(typeof(NewsEffectType)))
            {
                if (!TryGetEffectValue(type, out _))
                {
                    SetEffectValue(type, 0f);
                }
            }
        }

        // ============================================================
        //  结算：拼好的内容 + Tag 数值合计 + 本模版类型对应数值
        // ============================================================

        /// <summary>从当前排版结算出一篇写完的新闻结果。</summary>
        public NewsResultData BuildResult(TagTextTemplate textTemplate, ITagSystem tagSystem = null)
        {
            return NewsResultBuilder.Build(this, textTemplate, tagSystem);
        }

        /// <summary>从存档 Json 结算出一篇写完的新闻结果。</summary>
        public NewsResultData BuildResult(TagFillDocument document, ITagSystem tagSystem = null)
        {
            return NewsResultBuilder.Build(this, document, tagSystem);
        }

        /// <summary>从存档 Json 结算出一篇写完的新闻结果。</summary>
        public NewsResultData BuildResultFromJson(string json, ITagSystem tagSystem = null)
        {
            return NewsResultBuilder.BuildFromJson(this, json, tagSystem);
        }
    }
}
