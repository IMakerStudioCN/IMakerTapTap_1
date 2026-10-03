using UnityEngine;
using QFramework;

namespace TapTapFirst
{
	public partial class WindowsController : ViewController,IController
	{
		public string windowName;

        private IWindowsSystem mWindowsSystem;
		private IWindowsUtility mWindowsUtillity;
		private WindowsSO windowsSO;
		private WindowFrameState mState;

        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }


        void Start()
		{
            mWindowsSystem = this.GetSystem<IWindowsSystem>();
			mWindowsUtillity = this.GetUtility<IWindowsUtility>();
            windowsSO = mWindowsSystem.GetInfo(windowName);

            if (windowName == null || windowName == "")
			{
				Debug.LogError("窗口名称不能为空,请检查预制体");
				return;
            }
            var frame = (RectTransform)transform;
            var bar = (RectTransform)transform.Find("Bar");
            var content = (RectTransform)transform.Find("ContentBox");
            var exit = (RectTransform)transform.Find("Exit");

            WindowsUtility.AutoFit(bar, true, false);      // 宽度跟窗口，高度和贴顶距离不变（标题栏/工具栏）
            WindowsUtility.AutoFit(content, true, true);   // 四边都跟窗口（内容区铺满）
            WindowsUtility.PinCorner(exit, WindowCorner.TopRight, 3f, 3f);   // 固定尺寸钉右上角（按钮
            mState = mWindowsUtillity.NewState(frame);
			if (windowsSO.isFullScreen) mState.IsFullScreen = true;
			mWindowsUtillity.RestoreImmediate(frame,mState);
			Full.SetIsOnWithoutNotify(mState.IsFullScreen);

            
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
                mWindowsUtillity.SetFullScreen(this,frame, mState, isFull);
            });
        }
	}
}
