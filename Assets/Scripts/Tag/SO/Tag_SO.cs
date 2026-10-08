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
        int v1 ;
        int v2;
        int v3;
        int v4;
    }

    /// <summary>
    /// Enum Tag 词的种类:形容词、名词、量词、胡扯
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
        /// 写入 tag 的种类的中文内容
        /// </summary>
        [SerializeField]
        [Tooltip("Tag 的种类的中文内容:0是形容词,1是名词,2是量词,3是胡扯")]
        public List<string> theTypeToContent = new List<string>()
        {
            "形容词",
            "名词",
            "量词",
            "胡扯"
        };
        /// <summary>
        /// 获取 tag 的种类的中文内容
        /// </summary>
        public string getContent
        {
            get
            {
                switch(tagOfWords)
                {
                    case TagOfWords.adjective:
                        return theTypeToContent[0];
                    case TagOfWords.noun:
                        return theTypeToContent[1];
                    case TagOfWords.measure:
                        return theTypeToContent[2];
                    case TagOfWords.blbl:
                        return theTypeToContent[3];
                    default:
                        return "未知";
                }
            }
        }

    }
}