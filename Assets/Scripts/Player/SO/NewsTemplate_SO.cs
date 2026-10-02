using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TapTapFirst
{
    [CreateAssetMenu(fileName = "NewsTemplate", menuName = "ScriptableObjects/NewsTemplate_SO", order = 1)]
    public class NewsTemplate_SO : ScriptableObject
    {
        [SerializeField]
        public int newsId;
        [SerializeField]
        public string newsTitle;
        [SerializeField]
        public string newsContent;
        [SerializeField]
        public List<string> blank;
    }
}