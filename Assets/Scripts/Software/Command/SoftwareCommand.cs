using QFramework;
using UnityEngine;
using static UnityEditor.PlayerSettings;

namespace TapTapFirst
{
    public class SoftwareClickedCommand : AbstractCommand
    {
        WindowsSO windowsSO;
        private IWindowsSystem mWindowsSystem ;
        public SoftwareClickedCommand(WindowsSO windowsSO)
        {
            this.windowsSO = windowsSO;
        }

        protected override void OnExecute()
        {
            mWindowsSystem = this.GetSystem<IWindowsSystem>();
            //接入WindowsSystem，获取软件对应的窗口信息，打开窗口
            mWindowsSystem.RegisterWindows(windowsSO.windowName, windowsSO);
            mWindowsSystem.OpenWindows(windowsSO.windowName);
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
            Debug.Log("网站ID为：" + this.webID + "的软件的红点状态为：" + this.GetSystem<ISoftwareSystem>().getDic()[webID]);
        }
    }

}
                                                                             