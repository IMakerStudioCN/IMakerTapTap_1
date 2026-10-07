using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace TapTapFirst
{
    [CreateAssetMenu(fileName = "baLineList_SO", menuName = "ScriptableObjects/BaLineList_SO", order = 1)]
    public class baLineList_SO : ScriptableObject
    {
        [Header("ListID")]
        public int listID;
        [Header("存储ba用的List")]
        public List<baLineSingleSO> baLineList;
        [Header("帖子种类")]
        public int baWebType;
        [Header("可以获取的tag种类")]
        public int tagID;
        [Header("可以获得的NewsTemplate种类")]
        public int NewsTemplateID;
        [Header("获取了tag")]
        public bool isGetTag = false;
        [Header("获取了NewsTemp")]
        public bool isGetNewsTemp = false;
    }
}
