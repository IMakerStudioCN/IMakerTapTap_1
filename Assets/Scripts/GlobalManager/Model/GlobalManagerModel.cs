using QFramework;
using System.Collections.Generic;
using UnityEngine;

namespace TapTapFirst
{
    // 已生成模块接口 IGlobalManagerModel，请在 Architecture.Init() 中按接口类型注册：
    //     this.RegisterModel<IGlobalManagerModel>(new GlobalManagerModel());
    public interface IGlobalManagerModel : IModel
    {
        // TODO: 在这里声明模块对外 API（属性/方法）
        List<GameObject> emails { get; set; }
        Dictionary<int, bool> branchs { get; set; }
    }

    public class GlobalManagerModel : AbstractModel, IGlobalManagerModel
    {
        public List<GameObject> emails { get ; set ; } = new List<GameObject>();
        public Dictionary<int, bool> branchs { get ; set ; } = new Dictionary<int, bool>();

        protected override void OnInit()
        {
        }
    }
    public class GlobalManagerSaveData
    {
        public List<GameObject> emails = new List<GameObject>();
        public Dictionary<int,bool> branchs = new Dictionary<int,bool>();
    }
}
