using UnityEngine;
using QFramework;
using QFramework.Example;

namespace TapTapFirst
{
	public partial class Test : ViewController
	{
		void Start()
		{
			UIKit.OpenPanel<ComputerDisplay>();
        }
	}
}
