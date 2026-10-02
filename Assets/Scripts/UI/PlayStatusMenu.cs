using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace QFramework.Example
{
	public class PlayStatusMenuData : UIPanelData
	{
	}
	public partial class PlayStatusMenu : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as PlayStatusMenuData ?? new PlayStatusMenuData();
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
