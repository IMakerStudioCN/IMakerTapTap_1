using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace TapTapFirst
{
	public class ComputerDisplayData : UIPanelData
	{
	}
	public partial class ComputerDisplay : UIPanel
	{ 
        protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as ComputerDisplayData ?? new ComputerDisplayData();
			// please add init code here
			Menu.onValueChanged.AddListener((value) =>
			{
				if (value)
				{
					UIKit.OpenPanel<ComputerMenuPanel>(UILevel.PopUI);
                }
				else
				{
					UIKit.ClosePanel<ComputerMenuPanel>();
                }
			});
			PlayStatuMenu.onValueChanged.AddListener((value) =>
			{
				if(value)
				{
					UIKit.OpenPanel<PlayStatusMenu>(UILevel.PopUI);
				}
				else
				{
					UIKit.ClosePanel<PlayStatusMenu>();
                }
            } );
			

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
