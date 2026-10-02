using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TapTapFirst
{
    [CreateAssetMenu(fileName = "NewsTemplateList", menuName = "ScriptableObjects/NewsTemplateList_SO", order = 1)]
    public class NewsTemplateList_SO : ScriptableObject
    {
        [SerializeField]
        public List<NewsTemplate_SO> newsTemplateList;

        public NewsTemplate_SO GetNewsTemplateById(int id) => newsTemplateList.Find(news => news.newsId == id);
    }
}