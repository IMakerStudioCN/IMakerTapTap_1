using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace TapTapFirst
{
    [CreateAssetMenu(fileName = "Software_SO", menuName = "ScriptableObjects/Software_SO", order = 1)]
    public class Software_SO : ScriptableObject
    {
        [Header("软件名称")]
        public string softwareName;
        [Header("软件图标")]
        public Sprite softwareIcon;
        [Header("跳转网页的ID")]
        public int webID;
        [Header("是否有新消息")]
        public bool haveNewMessage;
    }
}