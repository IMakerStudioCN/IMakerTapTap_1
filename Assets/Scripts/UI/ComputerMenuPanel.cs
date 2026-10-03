using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace TapTapFirst
{
	public class ComputerMenuPanelData : UIPanelData
	{
	}
	public partial class ComputerMenuPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as ComputerMenuPanelData ?? new ComputerMenuPanelData();
			Setting.onClick.AddListener(OpenGlobalSettings);
		}

		private void OpenGlobalSettings()
		{
			GlobalSettingsUI.Open();
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

		protected override void OnBeforeDestroy()
		{
			Setting?.onClick.RemoveListener(OpenGlobalSettings);
			base.OnBeforeDestroy();
		}
	}
}
