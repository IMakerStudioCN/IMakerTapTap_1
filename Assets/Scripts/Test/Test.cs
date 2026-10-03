using UnityEngine;
using QFramework;

namespace TapTapFirst
{
	public partial class Test : ViewController
	{
		void Start()
		{
			UIKit.OpenPanel<ComputerDisplay>();
			//UIKit.OpenPanel<softwarePanel>();
        }
	}
}
