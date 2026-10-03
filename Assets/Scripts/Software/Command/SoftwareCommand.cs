using QFramework;
using UnityEngine;
using static UnityEditor.PlayerSettings;

namespace TapTapFirst
{
    public class SoftwareClickedCommand : AbstractCommand
    {
        int webID;
        public SoftwareClickedCommand(int webID)
        {
            this.webID = webID;
        }

        protected override void OnExecute()
        {
            this.SendEvent(new SoftwareClickedEvent { webID = webID });
            Debug.Log("打开的网页ID为：" + webID);
        }
    }
    /// <summary>
    /// 传入一个软件对象，设置其haveNewMessage属性为true或false
    /// </summary>
    public class HaveNewMessageCommand : AbstractCommand
    {
        int webID;
        bool haveNewMessage;
        public HaveNewMessageCommand(int webID,bool haveNewMessage)
        {
            this.webID = webID;
            this.haveNewMessage = haveNewMessage;
        }
        protected override void OnExecute()
        {
            this.SendEvent(new HaveNewMessageEvent { webID = this.webID });
            Debug.Log("网站ID为：" + this.webID + "的软件的红点状态为：" + this.GetModel<SoftwareModel>().haveNewMessage[this.webID]);
        }
    }

}
