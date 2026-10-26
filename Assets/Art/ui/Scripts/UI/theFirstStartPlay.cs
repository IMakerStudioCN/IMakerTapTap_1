using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace TapTapFirst
{
	public class theFirstStartPlayData : UIPanelData
	{
	}
	public partial class theFirstStartPlay : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as theFirstStartPlayData ?? new theFirstStartPlayData();
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
