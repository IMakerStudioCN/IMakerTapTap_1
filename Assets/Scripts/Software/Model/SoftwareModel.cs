using QFramework;
using System.Collections.Generic;
using UnityEngine;
namespace TapTapFirst
{
    // 已生成模块接口 ISoftwareModel，请在 Architecture.Init() 中按接口类型注册：
    //     this.RegisterModel<ISoftwareModel>(new SoftwareModel());
    public interface ISoftwareModel : IModel
    {
        // TODO: 在这里声明模块对外 API（属性/方法）
        Dictionary<int, bool> haveNewMessage { get; set; }
    }

    public class SoftwareModel : AbstractModel, ISoftwareModel
    {
        public Dictionary<int,bool> haveNewMessage { get; set; } = new Dictionary<int, bool>();
        protected override void OnInit()
        {
            Debug.Log("[SoftwareModel] OnInit 被执行了 ");
        }
    }
}
