using QFramework;
using System.Collections.Generic;

namespace TapTapFirst
{
    // 已生成模块接口 ISoftwareSystem，请在 Architecture.Init() 中按接口类型注册：
    //     this.RegisterSystem<ISoftwareSystem>(new SoftwareSystem());
    public interface ISoftwareSystem : ISystem
    {
        // TODO: 在这里声明模块对外 API（属性/方法）
        void clickSoftware(int windowID);//点击网页
        bool updateSO(int windowID);//更新SO
        void checkTheDic(int windowID);//检测网页dic,没有就add并默认设置为ture
        void setDicisTrue(int windowID);//将对应值设置为真
        void setDicisFalse(int windowID);//设置为假
        Dictionary<int, bool> getDic();//获取字典

    }

    public class SoftwareSystem : AbstractSystem, ISoftwareSystem
    {
        public Dictionary<int, bool> haveNewMessage = new Dictionary<int, bool>();

        public void checkTheDic(int windowID)
        {
            if (!haveNewMessage.ContainsKey(windowID))
            {
                haveNewMessage.Add(windowID, true);
            }
        }

        public void clickSoftware(int windowID)
        {
            checkTheDic(windowID);
            haveNewMessage[windowID] = false;
        }

        public Dictionary<int, bool> getDic()
        {
            return haveNewMessage;
        }

        public bool updateSO(int windowID)
        {
            checkTheDic(windowID);
            return haveNewMessage[windowID];
        }
        /// <summary>
        /// 将对应ID的dic设置为真
        /// </summary>
        /// <param name="windowID"></param>
        public void setDicisTrue(int windowID)
        {
            checkTheDic(windowID);
            haveNewMessage[windowID] = true;
        }
        public void setDicisFalse(int windowID)
        {
            checkTheDic(windowID);
            haveNewMessage[windowID] = false;
        }
        protected override void OnInit()
        {
            
        }
    }
}
