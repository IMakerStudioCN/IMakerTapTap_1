using UnityEngine;
using QFramework;

namespace TapTapFirst
{
	public partial class WindowsController : ViewController,IController
	{
		private WindowsSystem mWindowsSystem;
		private RectTransform mRectTransform;

        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }

        void Start()
		{
			mWindowsSystem = this.GetSystem<WindowsSystem>();
            Exit.onClick.AddListener(() =>
			{
				UIKit.ClosePanel(windowsSO.windowName);
                //刷新任务栏,关闭任务栏图标
            });
			Mini.onClick.AddListener(() =>
			{ 
				UIKit.HidePanel(windowsSO.windowName);
			});
			Full.onClick.AddListener(() =>
			{
                //Windows系统做全屏操作
				mRectTransform = this.GetComponent<RectTransform>();
				mRectTransform.anchorMin = new Vector2(0, 0);
				mRectTransform.anchorMax = new Vector2(1, 1);
				mRectTransform.offsetMin = new Vector2(0, 0);
				mRectTransform.offsetMax = new Vector2(0, 0);
            });

        }
	}
}
