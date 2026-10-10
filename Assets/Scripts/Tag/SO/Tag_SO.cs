using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace TapTapFirst
{
    /// <summary>
    ///  Struct 数值:（未定）
    /// </summary>
    public struct TagValue
    {
        int v1;
        int v2;
        int v3;
        int v4;
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
        /// tag 的数值
        /// </summary>
        [SerializeField]
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
    }
}