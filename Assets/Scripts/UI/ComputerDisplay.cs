using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace QFramework.Example
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
			Menu.onClick.AddListener(() =>
			{
				UIKit.OpenPanel<ComputerMenuPanel>(UILevel.PopUI);
            });
			PlayStatus.onClick.AddListener(() =>
			{
				
			});
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
