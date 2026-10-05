using UnityEngine;
using QFramework;
using System.Collections.Generic;

namespace TapTapFirst
{
	public partial class MapController : ViewController,IController
	{
		private IMapSystem mapSystem;
        void Start()
		{
			mapSystem = this.GetSystem<IMapSystem>();
			Init();

            // 订阅事件,事件传入的参数是一个字符串数组，表示需要显示的地点名称
            //监听事件变化,当有新的地点需要显示时,调用ShowPlace或者HidePlace方法
        }
        private void Init()
		{
			MapMarker[] makers =  this.GetComponentsInChildren<MapMarker>();
            foreach (var maker in makers)
            {
				maker.HideHaveCase();
                mapSystem.RegisterPlace(maker.placeName, maker);
            }
        }
        public void ShowPlace(params string[] placeNames)
        {
            foreach (var placeName in placeNames)
            {
                MapMarker marker = mapSystem.GetPlace(placeName);
                if(marker != null)
                {
                    marker.ShowHaveCase();
                }
            }
        }
        public void HidePlace(params string[] placeNames)
        {
            foreach (var placeName in placeNames)
            {
                MapMarker marker = mapSystem.GetPlace(placeName);
                if(marker != null)
                {
                    marker.HideHaveCase();
                }
            }
        }

    }
}
