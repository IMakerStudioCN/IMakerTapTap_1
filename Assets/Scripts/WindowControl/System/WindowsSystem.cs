using QFramework;
using System.Collections.Generic;

namespace TapTapFirst
{
    // 已生成模块接口 IWindowsSystem，请在 Architecture.Init() 中按接口类型注册：
    //     this.RegisterSystem<IWindowsSystem>(new WindowsSystem());
    public interface IWindowsSystem : ISystem
    {
        // TODO: 在这里声明模块对外 API（属性/方法）
        
    }
    public class WindowsSystem : AbstractSystem, IWindowsSystem
    {
        Dictionary<string,WindowsInfo> windowsRegister = new Dictionary<string, WindowsInfo>();
        protected override void OnInit()
        {
        }

        public void RegisterWindows(string windowsName, WindowsInfo windowsInfo)
        {
            if (!windowsRegister.ContainsKey(windowsName))
            {
                windowsRegister.Add(windowsName, windowsInfo);
            }
        }
        public void UnRegisterWindows(string windowsName)
        {
            if (windowsRegister.ContainsKey(windowsName))
            {
                windowsRegister.Remove(windowsName);
            }
        }
        public WindowsInfo GetInfo(string windowsName)
        {
            if (windowsRegister.ContainsKey(windowsName))
            {
                return windowsRegister[windowsName];
            }
            return null;
        }
    }
}
