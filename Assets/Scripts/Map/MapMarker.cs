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
        public IJsonSaveUtility JsonSaveUtility;
        MapData data => JsonSaveUtility.Get<MapData>("MapData");

        void Awake()
		{
            haveCase = GetComponent<Button>();
            haveCase.onClick.AddListener(OnClick);
        }
		public void ShowHaveCase()
		{
			if (haveCase == null) return;;
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
			//并且隐藏自己
			HideHaveCase();
			data.visiblePlace.Remove(placeName);
        }

        public IArchitecture GetArchitecture()
        {
			return TapTap.Interface;
        }
    }
}
