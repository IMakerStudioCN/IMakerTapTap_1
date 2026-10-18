using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace TapTapFirst
{
    /// <summary>
    /// Tag 数值的类型（可扩展：往后加类型不用动结构体）。
    /// 下标即 valuePairs 里的 key：
    ///   0 = PageViews      浏览量
    ///   1 = PressureValue  压力值
    /// </summary>
    public enum TagValueType
    {
        PageViews = 0,
        PressureValue = 1,
    }

    /// <summary>一对"类型 -> 数值"。</summary>
    [Serializable]
    public class TagValuePair
    {
        public TagValueType type = TagValueType.PageViews;
        public float value;

        public TagValuePair() { }

        public TagValuePair(TagValueType type, float value)
        {
            this.type = type;
            this.value = value;
        }
    }

    /// <summary>
    /// 【旧结构，保留兼容】原先是两个 private 字段（外部读不到、也从没被使用过）。
    /// 新数据请用 Tag_SO.valuePairs + Tag_SO.GetValue(...)。
    /// </summary>
    [Serializable]
    public struct TagValue
    {
        public float PageViews;
        public float PressureValue;
    }

    /// <summary>
    /// Enum Tag 词的种类:形容词、名词、量词、胡扯
    /// 下标即 theTypeToContent 的下标：
    ///   0 = adjective 形容词
    ///   1 = noun      名词
    ///   2 = measure   量词
    ///   3 = blbl      胡扯
    /// </summary>
    public enum TagOfWords
    {
        adjective,
        noun,
        measure,
        blbl
    }

    [CreateAssetMenu(fileName = "Tag_SO", menuName = "ScriptableObjects/Tag_SO", order = 1)]
    public class Tag_SO : ScriptableObject
    {
        /// <summary>词性数量（theTypeToContent 应该有这么多个元素）</summary>
        public const int WordTypeCount = 4;

        /// <summary>词性的中文名，编辑器/调试用</summary>
        public static readonly string[] WordTypeNames = { "形容词", "名词", "量词", "胡扯" };

        /// <summary>
        /// Tag 的 ID
        /// </summary>
        [SerializeField]
        public int tagId;
        /// <summary>
        /// tag 的名字
        /// </summary>
        [SerializeField]
        public string tagName;
        /// <summary>
        /// 【推荐】tag 的数值：类型 -> 数值 的列表。
        /// 取法：GetValue(TagValueType.PageViews)
        /// </summary>
        [SerializeField]
        public List<TagValuePair> valuePairs = new List<TagValuePair>();

        /// <summary>
        /// 【旧字段，保留兼容】老的数值结构体；新数据请用 valuePairs。
        /// </summary>
        [SerializeField]
        [Tooltip("旧数值结构体；新数据请用上面的 Value Pairs")]
        public TagValue tagValues;
        /// <summary>
        /// tag 的种类:形容词、名词、量词、胡扯
        /// </summary>
        [SerializeField]
        public TagOfWords tagOfWords;

        /// <summary>
        /// 每种词性下这个 Tag 显示成什么内容。
        /// 下标与 TagOfWords 一一对应：0 形容词 / 1 名词 / 2 量词 / 3 胡扯。
        /// 例：tagName="开心" -> ["开心的", "开心", "一次开心", "开心个鬼"]
        /// </summary>
        [SerializeField]
        [Tooltip("每种词性下显示的内容：0是形容词,1是名词,2是量词,3是胡扯。空缺按它的词性到对应下标取文本。")]
        public List<string> theTypeToContent = new List<string>()
        {
            "形容词",
            "名词",
            "量词",
            "胡扯"
        };

        /// <summary>
        /// 获取 tag 的种类的中文内容（旧逻辑，保留）
        /// </summary>
        public string getContent
        {
            get { return GetContentByWordType(tagOfWords); }
        }

        /// <summary>
        /// 取这个 Tag 在指定词性下应该显示的文本。
        /// </summary>
        public string GetContentByWordType(TagOfWords wordType)
        {
            return GetDisplayContent(this, wordType);
        }

        /// <summary>
        /// 静态版：取这个 Tag 在指定词性下应该显示的文本。
        /// 词性下标直接对应 theTypeToContent 的下标（0~3）。
        /// 越界或为空时回退到 tagName。
        /// </summary>
        public static string GetDisplayContent(Tag_SO tag, TagOfWords wordType)
        {
            if (tag == null)
            {
                return string.Empty;
            }

            int index = (int)wordType;

            if (tag.theTypeToContent != null &&
                index >= 0 &&
                index < tag.theTypeToContent.Count &&
                !string.IsNullOrEmpty(tag.theTypeToContent[index]))
            {
                return tag.theTypeToContent[index];
            }

            return string.IsNullOrEmpty(tag.tagName) ? string.Empty : tag.tagName;
        }

        /// <summary>把 theTypeToContent 补齐到 4 项（生成器用）。</summary>
        public void EnsureWordTypeSlots()
        {
            if (theTypeToContent == null)
            {
                theTypeToContent = new List<string>();
            }

            while (theTypeToContent.Count < WordTypeCount)
            {
                theTypeToContent.Add(string.Empty);
            }
        }

        /// <summary>设置某个词性下显示的内容（生成器用）。</summary>
        public void SetContent(TagOfWords wordType, string content)
        {
            EnsureWordTypeSlots();
            theTypeToContent[(int)wordType] = content;
        }

        // ============================================================
        //  数值查询（valuePairs）
        // ============================================================

        /// <summary>取指定类型的数值；没配过返回 0。</summary>
        public float GetValue(TagValueType type)
        {
            if (valuePairs != null)
            {
                for (int i = 0; i < valuePairs.Count; i++)
                {
                    TagValuePair pair = valuePairs[i];

                    if (pair != null && pair.type == type)
                    {
                        return pair.value;
                    }
                }
            }

            // 兜底：旧的 tagValues 结构体（只有前两个类型）
            switch (type)
            {
                case TagValueType.PageViews:
                    return tagValues.PageViews;
                case TagValueType.PressureValue:
                    return tagValues.PressureValue;
                default:
                    return 0f;
            }
        }

        /// <summary>取指定类型的数值；没配过返回 false（out 为 0）。</summary>
        public bool TryGetValue(TagValueType type, out float value)
        {
            value = 0f;

            if (valuePairs == null)
            {
                return false;
            }

            for (int i = 0; i < valuePairs.Count; i++)
            {
                TagValuePair pair = valuePairs[i];

                if (pair != null && pair.type == type)
                {
                    value = pair.value;
                    return true;
                }
            }

            return false;
        }

        /// <summary>写指定类型的数值；没有这条就新建。</summary>
        public void SetValue(TagValueType type, float value)
        {
            if (valuePairs == null)
            {
                valuePairs = new List<TagValuePair>();
            }

            for (int i = 0; i < valuePairs.Count; i++)
            {
                TagValuePair pair = valuePairs[i];

                if (pair != null && pair.type == type)
                {
                    pair.value = value;
                    return;
                }
            }

            valuePairs.Add(new TagValuePair(type, value));
        }

        /// <summary>浏览量。</summary>
        public float PageViews => GetValue(TagValueType.PageViews);

        /// <summary>压力值。</summary>
        public float PressureValue => GetValue(TagValueType.PressureValue);

        /// <summary>把 valuePairs 补齐到所有类型各一条（生成器 / 编辑器用）。</summary>
        public void EnsureValuePairs()
        {
            foreach (TagValueType type in Enum.GetValues(typeof(TagValueType)))
            {
                if (!TryGetValue(type, out _))
                {
                    SetValue(type, 0f);
                }
            }
        }
    }
}
