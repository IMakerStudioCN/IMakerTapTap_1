// Generate Id:5770acfd-2e81-4c06-8e92-a7a709053e8e
using UnityEngine;

namespace TapTapFirst
{
	public partial class MapController : QFramework.IController
	{
		public UnityEngine.GameObject Button;
		
		QFramework.IArchitecture QFramework.IBelongToArchitecture.GetArchitecture()=>TapTapFirst.TapTap.Interface;
	}
}
