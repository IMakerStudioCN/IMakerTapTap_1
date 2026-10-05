using QFramework;
using System.Collections.Generic;

namespace TapTapFirst
{
    // 已生成模块接口 IMapSystem，请在 Architecture.Init() 中按接口类型注册：
    //     this.RegisterSystem<IMapSystem>(new MapSystem());
    public interface IMapSystem : ISystem
    {
        void RegisterPlace(string placeName, MapMarker marker);
        public void ShowPlace(params string[] placeNames);
        public void HidePlace(params string[] placeNames);
    }

    public class MapSystem : AbstractSystem, IMapSystem
    {
        public Dictionary<string, MapMarker> placeDictionary = new Dictionary<string, MapMarker>();
        protected override void OnInit()
        {
        }

        public void RegisterPlace(string placeName, MapMarker marker)
        {
            if (string.IsNullOrEmpty(placeName) || marker == null)
            {
                return;
            }
            placeDictionary[placeName] = marker;
        }
        public void ShowPlace(params string[] placeNames)
        {
            foreach (var placeName in placeNames)
            {
                if (placeDictionary.TryGetValue(placeName, out MapMarker marker))
                {
                    marker.ShowHaveCase();
                }
            }
        }
        public void HidePlace(params string[] placeNames)
        {
            foreach (var placeName in placeNames)
            {
                if (placeDictionary.TryGetValue(placeName, out MapMarker marker))
                {
                    marker.HideHaveCase();
                }
            }
        }


    }
}
