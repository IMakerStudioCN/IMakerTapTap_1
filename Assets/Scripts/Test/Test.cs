using UnityEngine;
using QFramework;

namespace TapTapFirst
{
	public partial class Test : ViewController,IController
	{
		void Start()
		{
			UIKit.OpenPanel<ComputerDisplay>();
        }
        private void OnGUI()
        {
            if (GUI.Button(new Rect(20, 40, 100, 60), "天数增加"))
            {
                this.SendCommand(new DaysAddCommand());    
            }
        }

        public IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }
    }
}
