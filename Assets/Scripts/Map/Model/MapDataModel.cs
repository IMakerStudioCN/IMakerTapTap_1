using QFramework;
using System;
using System.Collections.Generic;

namespace TapTapFirst
{
    // 已生成模块接口 IMapDataModel，请在 Architecture.Init() 中按接口类型注册：
    //     this.RegisterModel<IMapDataModel>(new MapDataModel());
    public interface IMapDataModel : IModel
    {
        // TODO: 在这里声明模块对外 API（属性/方法）
    }

    public class MapDataModel : AbstractModel, IMapDataModel
    {
        
        protected override void OnInit()
        {
        }
    }
    [Serializable]
    public class MapData
    {
        //可视化地点列表
        public List<string> visiblePlace = new List<string>();
    }
}
