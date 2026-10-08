using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TapTapFirst
{
    [CreateAssetMenu(fileName = "DialogSO", menuName = "ScriptableObjects/DialogSO", order = 1)]
    public class DialogSO : ScriptableObject
    {
        [Header("对话ID")]
        public string title;
        [Header("配置")]
        public List<DialogSOSingle> dialog;
    }
}
