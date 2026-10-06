using QFramework;
using System.Collections.Generic;
using UnityEngine;

namespace TapTapFirst
{
    // 已生成模块接口 IMapSystem，请在 Architecture.Init() 中按接口类型注册：
    //     this.RegisterSystem<IMapSystem>(new MapSystem());
    public interface IMapSystem : ISystem
    {
        void RegisterPlace(string placeName, MapMarker marker);
        MapMarker GetPlace(string placeName);
        void UnregisterPlace(string placeName);

        // 地图可见性的唯一写入口：数据 + 通知 + 存档都从这里走
        void ShowPlace(params string[] placeNames);
        void HidePlace(string placeName);
        bool IsVisible(string placeName);
        IReadOnlyList<string> VisiblePlaces { get; }
    }

    public class MapSystem : AbstractSystem, IMapSystem
    {
        public Dictionary<string, MapMarker> placeDictionary = new Dictionary<string, MapMarker>();

        private IJsonSaveUtility JsonSaveUtility => this.GetUtility<IJsonSaveUtility>();
        private MapData data => JsonSaveUtility?.Get<MapData>("MapData");
        private bool mTakeTaskRegistered;

        protected override void OnInit()
        {
            RegisterTakeTaskEvent();
        }

        public void RegisterPlace(string placeName, MapMarker marker)
        {
            if (string.IsNullOrEmpty(placeName) || marker == null)
            {
                return;
            }
            placeDictionary[placeName] = marker;

            // 架构初始化被别的系统异常打断时 OnInit 可能没跑到，这里兜一次
            RegisterTakeTaskEvent();
        }
        public MapMarker GetPlace(string placeName)
        {
            if (placeDictionary.TryGetValue(placeName, out MapMarker marker))
            {
                return marker;
            }
            return null;
        }

        public void UnregisterPlace(string placeName)
        {
            if (string.IsNullOrEmpty(placeName))
            {
                return;
            }
            placeDictionary.Remove(placeName);
        }

        public void ShowPlace(params string[] placeNames)
        {
            if (placeNames == null)
            {
                return;
            }

            foreach (string placeName in placeNames)
            {
                ApplyVisibility(placeName, true);
            }
        }

        public void HidePlace(string placeName)
        {
            ApplyVisibility(placeName, false);
        }

        public bool IsVisible(string placeName)
        {
            MapData mapData = data;

            if (mapData == null || string.IsNullOrEmpty(placeName))
            {
                return false;
            }

            return mapData.visiblePlace.Contains(placeName);
        }

        public IReadOnlyList<string> VisiblePlaces
        {
            get
            {
                MapData mapData = data;

                if (mapData == null)
                {
                    return new List<string>();
                }

                return mapData.visiblePlace;
            }
        }

        private void ApplyVisibility(string placeName, bool visible)
        {
            if (string.IsNullOrEmpty(placeName))
            {
                return;
            }

            MapData mapData = data;

            if (mapData == null)
            {
                Debug.LogWarning($"[MapSystem] 找不到 MapData，{placeName} 的可见性修改已忽略");
                return;
            }

            if (visible)
            {
                if (!mapData.visiblePlace.Contains(placeName))
                {
                    mapData.visiblePlace.Add(placeName);
                }
            }

            // 地图窗口开着却找不到这个标记，多半是 prefab 里的 placeName 和传进来的对不上
            if (placeDictionary.Count > 0 && !placeDictionary.ContainsKey(placeName))
            {
                Debug.LogWarning($"[MapSystem] 地图上没有名为 {placeName} 的标记，只写了数据，没刷新 UI");
            }

            this.SendEvent(new OnMapVisibilityChangedEvent { PlaceName = placeName, Visible = visible });

        }

        private void RegisterTakeTaskEvent()
        {
            if (mTakeTaskRegistered)
            {
                return;
            }

            mTakeTaskRegistered = true;
            this.RegisterEvent<OnTakeTask>(e => ShowPlace(e.Place));
        }


    }
}
