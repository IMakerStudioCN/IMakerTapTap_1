using QFramework;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace TapTapFirst
{
	public partial class MapMarker : ViewController,IController
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
			if (haveCase == null) haveCase = GetComponent<Button>();
			if (haveCase == null) return;;
			if (!haveCase.isActiveAndEnabled)
			{
				haveCase.gameObject.SetActive(true);
			}
		}
		public void HideHaveCase()
		{
			if (haveCase == null) haveCase = GetComponent<Button>();
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
			//可见性统一交给 MapSystem：数据、通知、存档一起走
			this.GetSystem<IMapSystem>().HidePlace(placeName);
        }

        public IArchitecture GetArchitecture()
        {
			return TapTap.Interface;
        }
    }
}
