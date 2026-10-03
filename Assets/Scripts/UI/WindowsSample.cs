using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace TapTapFirst
{
	public class WindowsSampleData : UIPanelData
	{
	}
	public partial class WindowsSample : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as WindowsSampleData ?? new WindowsSampleData();
			// please add init code here
		}
		
		protected override void OnOpen(IUIData uiData = null)
		{
		}
		
		protected override void OnShow()
		{
		}
		
		protected override void OnHide()
		{
		}
		
		protected override void OnClose()
		{
		}
	}
}
