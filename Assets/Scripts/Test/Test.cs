using UnityEngine;
using QFramework;

namespace TapTapFirst
{
	public partial class Test : ViewController,IController
	{
		void Start()
		{
			UIKit.OpenPanel<ComputerDisplay>();
            //UIKit.OpenPanel<WindowsSample>();
        }
        private void OnGUI()
        {
            if (GUI.Button(new Rect(20, 40, 100, 60), "隐藏"))
            {
                UIKit.GetPanel<Dialog>().Hide();
            }
            if (GUI.Button(new Rect(20, 240, 100, 60), "展示"))
            {
                UIKit.GetPanel<Dialog>().Show();
            }
        }

        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }
    }
}
