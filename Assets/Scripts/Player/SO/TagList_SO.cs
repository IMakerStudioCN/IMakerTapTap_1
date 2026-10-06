using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace TapTapFirst
{
    [CreateAssetMenu(fileName = "TagList_SO", menuName = "ScriptableObjects/TagList_SO", order = 1)]
    public class TagList_SO : ScriptableObject
    {
        /// <summary>
        /// 全部的 Tag_SO 列表
        /// </summary>
        [SerializeField]
        public List<Tag_SO> allTagList = new List<Tag_SO>();
        public Tag_SO GetTagById(int id)=> allTagList.Find(tag => tag.tagId == id);
    }
}