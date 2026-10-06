using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace TapTapFirst
{
    [CreateAssetMenu(fileName = "EmailLineList_SO", menuName = "ScriptableObjects/EmailLineList_SO", order = 1)]
    public class EmailLineList_SO : ScriptableObject
    {
        [Header("拥有所有邮件列表")]
        public List<EmailLine_SO> EmailLines;
    }
}