using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace TapTapFirst
{
	public class DialogData : UIPanelData
	{
	}
	public partial class Dialog : UIPanel
	{
		private CanvasGroup mGroup;
        protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as DialogData ?? new DialogData();
			// please add init code here
			mGroup = gameObject.GetOrAddComponent<CanvasGroup>();
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
