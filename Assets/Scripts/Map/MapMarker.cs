using UnityEngine;
using QFramework;
using UnityEngine.UI;

namespace TapTapFirst
{
	public partial class MapMarker : ViewController
	{
		public string placeName;
		Button haveCase;
		
		void Awake()
		{
            haveCase = GetComponent<Button>();
            haveCase.onClick.AddListener(OnClick);
        }
		public void ShowHaveCase()
		{
			if (haveCase == null) return;
			if (!haveCase.isActiveAndEnabled)
			{
				haveCase.gameObject.SetActive(true);
			}
		}
		public void HideHaveCase()
		{
			if (haveCase == null) return;
			if (haveCase.isActiveAndEnabled)
			{
                haveCase.gameObject.SetActive(false);
			}
        }
        public void OnClick()
		{
			// 点击地图标记时的逻辑,跳转到对应的
			UIKit.OpenPanel<Dialog>(UILevel.PopUI);
        }
	}
}
