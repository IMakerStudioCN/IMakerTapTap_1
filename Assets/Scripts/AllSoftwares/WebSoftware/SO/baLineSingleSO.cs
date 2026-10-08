using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace TapTapFirst
{
    [CreateAssetMenu(fileName = "baLineSingle_SO", menuName = "ScriptableObjects/Ba/baSingleLine_SO", order = 1)]
    public class baLineSingleSO : ScriptableObject
    {

        [Header("baID")]
        public int baID;
        [Header("点击数")]
        public string clicks;
        [Header("回复数")]
        public string replies;
        [Header("标题")]
        public string title;
        [Header("日期")]
        public string data;
        [Header("内容")]
        [TextArea(2,3)]
        public string content;
        [Header("作者")]
        public string writter;





    }
}
