using UnityEngine;
using QFramework;
using System.Collections.Generic;

namespace TapTapFirst
{
	public partial class MapController : ViewController,IController
	{
		private IMapSystem mapSystem;
        public IJsonSaveUtility JsonSaveUtility;
        MapData data => JsonSaveUtility.Get<MapData>("MapData");
        void Start()
		{
			mapSystem = this.GetSystem<IMapSystem>();
            JsonSaveUtility = this.GetUtility<IJsonSaveUtility>();
            Init();

            // 订阅事件,事件传入的参数是一个字符串数组，表示需要显示的地点名称
            //监听事件变化,当有新的地点需要显示时,调用ShowPlace或者HidePlace方法
        }
        private void Init()
		{
			MapMarker[] makers =  this.GetComponentsInChildren<MapMarker>(true);
            foreach (var maker in makers)
            {
                mapSystem.RegisterPlace(maker.placeName, maker);
                if (data.visiblePlace.Contains(maker.placeName))
                {
                    maker.ShowHaveCase();
                }
                else
                {
                    maker.HideHaveCase();
                }

            }
        }
        //展示并存入可视化，再一天过完之后存入存档
        public void ShowPlace(params string[] placeNames)
        {
            foreach (var placeName in placeNames)
            {
                MapMarker marker = mapSystem.GetPlace(placeName);
                if(marker != null)
                {
                    marker.ShowHaveCase();
                    if (!data.visiblePlace.Contains(marker.placeName))
                    {
                        data.visiblePlace.Add(marker.placeName);
                    }
                }
            }
        }
        //移除可视化，一天过完后存入存档
        public void HidePlace(params string[] placeNames)
        {
            foreach (var placeName in placeNames)
            {
                MapMarker marker = mapSystem.GetPlace(placeName);
                if(marker != null)
                {
                    marker.HideHaveCase();
                    data.visiblePlace.Remove(marker.placeName);
                }
            }
        }

    }
}
