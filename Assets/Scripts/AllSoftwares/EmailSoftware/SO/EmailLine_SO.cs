using UnityEngine;
namespace TapTapFirst
{
    [CreateAssetMenu(fileName = "EmailLine_SO", menuName = "ScriptableObjects/EmailLine_SO", order = 1)]
    public class EmailLine_SO : ScriptableObject
    {
        [Header("发送人")]
        public string WhoSend;
        [Header("发送日期")]
        public string WhenSend;
        [Header("信件内容")]
        [TextArea(2,3)]
        public string SendContent;
        [Header("邮件ID")]
        public int EmailID;
        [Header("地点")]
        public string[] Place;

    }
}
