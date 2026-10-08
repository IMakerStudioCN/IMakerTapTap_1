using QFramework;
using System.Collections.Generic;

namespace TapTapFirst
{
    // 已生成模块接口 IMybaSystem，请在 Architecture.Init() 中按接口类型注册：
    //     this.RegisterSystem<IMybaSystem>(new MybaSystem());
    public interface IMybaSystem : ISystem
    {
        // TODO: 在这里声明模块对外 API（属性/方法）
        public List<string> GetMyBaLineContentList();
        public List<int> Getdata();
        public int GetViewTimes();
        //增删查改
        public void AddMyBaLineContent(string content);
        public void RemoveMyBaLineContent(string content);
        public string GetMyBaLineContent(string content);
    }

    public class MybaSystem : AbstractSystem, IMybaSystem
    {
        public void AddMyBaLineContent(string content)
        {
            throw new System.NotImplementedException();
        }

        public List<int> Getdata()
        {
            throw new System.NotImplementedException();
        }

        public string GetMyBaLineContent(string content)
        {
            throw new System.NotImplementedException();
        }

        public List<string> GetMyBaLineContentList()
        {
            throw new System.NotImplementedException();
        }

        public int GetViewTimes()
        {
            throw new System.NotImplementedException();
        }

        public void RemoveMyBaLineContent(string content)
        {
            throw new System.NotImplementedException();
        }
        #region
        #endregion

        protected override void OnInit()
        {
        }
    }
}
