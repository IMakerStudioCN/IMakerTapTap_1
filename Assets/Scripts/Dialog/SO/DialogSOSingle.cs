using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TapTapFirst
{
    [System.Serializable]
    public class DialogSOSingle
    {
        [Header("立绘")]
        public Sprite Standing;
        [Header("对应的对话UI")]
        public GameObject prefab;
        [Header("单句")]
        public string Sentence;
    }
}
