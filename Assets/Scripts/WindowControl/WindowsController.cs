using UnityEngine;
using QFramework;

namespace TapTapFirst
{
	public partial class WindowsController : ViewController,IController
	{
		public string windowName;

        private IWindowsSystem mWindowsSystem;
		private RectTransform mRectTransform;
		private WindowsSO windowsSO;

        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }

        void Start()
		{
			if(windowName == null || windowName == "")
			{
				Debug.LogError("窗口名称不能为空,请检查预制体");
				return;
            }
            mWindowsSystem = this.GetSystem<IWindowsSystem>();
			mRectTransform = this.GetComponent<RectTransform>();
			windowsSO = mWindowsSystem.GetInfo(windowName);
            Exit.onClick.AddListener(() =>
			{
				UIKit.ClosePanel(windowsSO.windowName);
                //刷新任务栏,关闭任务栏图标
            });
			Mini.onClick.AddListener(() =>
			{ 
				UIKit.HidePanel(windowsSO.windowName);
			});
			Full.onValueChanged.AddListener((isFull) =>
			{
                
            });
        }
	}
}
