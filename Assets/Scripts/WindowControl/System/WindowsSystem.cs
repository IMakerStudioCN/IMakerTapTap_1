using QFramework;
using System.Collections.Generic;

namespace TapTapFirst
{
    // 已生成模块接口 IWindowsSystem，请在 Architecture.Init() 中按接口类型注册：
    //     this.RegisterSystem<IWindowsSystem>(new WindowsSystem());
    public interface IWindowsSystem : ISystem
    {
        // TODO: 在这里声明模块对外 API（属性/方法）
        public void OpenWindows(string windowsName);
        public void RegisterWindows(string windowsName, WindowsSO windowsSO);
        public WindowsSO GetInfo(string windowsName);

    }
    public class WindowsSystem : AbstractSystem, IWindowsSystem
    {
        Dictionary<string,WindowsSO> windowsRegister = new Dictionary<string, WindowsSO>();
        protected override void OnInit()
        {
        }
        public void OpenWindows(string windowsName)
        {
            if (windowsRegister.ContainsKey(windowsName))
            {
                WindowsSO windowsSO = windowsRegister[windowsName];
                //打开窗口的逻辑
                UIKit.OpenPanel(windowsSO.windowName);
            }
        }
        public void RegisterWindows(string windowsName, WindowsSO windowsSO)
        {
            if (!windowsRegister.ContainsKey(windowsName))
            {
                windowsRegister.Add(windowsName, windowsSO);
            }
        }
        public void UnRegisterWindows(string windowsName)
        {
            if (windowsRegister.ContainsKey(windowsName))
            {
                windowsRegister.Remove(windowsName);
            }
        }
        public WindowsSO GetInfo(string windowsName)
        {
            if (windowsRegister.ContainsKey(windowsName))
            {
                return windowsRegister[windowsName];
            }
            return null;
        }

    }
}
