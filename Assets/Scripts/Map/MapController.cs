using QFramework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

namespace TapTapFirst
{
	public partial class MapController : ViewController,IController
	{
		private IMapSystem mapSystem;
        public IJsonSaveUtility JsonSaveUtility;
        MapData data => JsonSaveUtility.Get<MapData>("MapData");
        private IUnRegister mMapVisibilityChangedUnRegister;
        void Awake()
        {
            mapSystem = this.GetSystem<IMapSystem>();
            JsonSaveUtility = this.GetUtility<IJsonSaveUtility>();
        }
        void OnEnable()
        {
            // 监听地图可见性变化，窗口开着时任何地方改了数据都能立刻刷新
            mMapVisibilityChangedUnRegister = this.RegisterEvent<OnMapVisibilityChangedEvent>(OnMapVisibilityChanged);

            // 每次窗口显示都按数据全量同步一次，窗口关着期间的变化也能补上
            Init();
        }
        void OnDisable()
        {
            if (mMapVisibilityChangedUnRegister != null)
            {
                mMapVisibilityChangedUnRegister.UnRegister();
                mMapVisibilityChangedUnRegister = null;
            }

            // 窗口隐藏/销毁时把标记从 MapSystem 摘掉，避免留下已销毁的引用
            if (mapSystem != null)
            {
                MapMarker[] makers = this.GetComponentsInChildren<MapMarker>(true);
                foreach (var maker in makers)
                {
                    mapSystem.UnregisterPlace(maker.placeName);
                }
            }
        }
        private void Init()
		{
			MapData mapData = data;

			if (mapData == null)
			{
				Debug.LogWarning("[MapController] 找不到 MapData，地图标记按未解锁处理");
			}

			MapMarker[] makers =  this.GetComponentsInChildren<MapMarker>(true);
            foreach (var maker in makers)
            {
                mapSystem.RegisterPlace(maker.placeName, maker);
                if (mapData != null && mapData.visiblePlace.Contains(maker.placeName))
                {
                    maker.ShowHaveCase();
                }
                else
                {
                    maker.HideHaveCase();
                }

            }
		}
        private void OnMapVisibilityChanged(OnMapVisibilityChangedEvent e)
        {
            MapMarker marker = mapSystem.GetPlace(e.PlaceName);

            if (marker == null)
            {
                return;
            }

            if (e.Visible)
            {
                marker.ShowHaveCase();
            }
            else
            {
                marker.HideHaveCase();
            }
        }
        //展示并存入可视化，再一天过完之后存入存档
        public void ShowPlace(params string[] placeNames)
        {
            // 数据写入、通知、存盘统一由 MapSystem 负责，这里只转发
            mapSystem.ShowPlace(placeNames);
        }


    }
}
