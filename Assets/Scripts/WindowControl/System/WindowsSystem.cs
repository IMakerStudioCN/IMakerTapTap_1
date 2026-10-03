using QFramework;

namespace TapTapFirst
{
    // 已生成模块接口 IWindowsSystem，请在 Architecture.Init() 中按接口类型注册：
    //     this.RegisterSystem<IWindowsSystem>(new WindowsSystem());
    //
    // 这层只是 WindowKit 的"架构门面"：想在 Command / System / Model 里写
    //     this.GetSystem<IWindowsSystem>().OpenWindows("面板名")
    // 就用它；普通 MonoBehaviour 直接调 WindowKit 就行。
    public interface IWindowsSystem : ISystem
    {
        void OpenWindows(string windowsName);
        void CloseWindows(string windowsName);
        void MinimizeWindows(string windowsName);
        void RestoreWindows(string windowsName);
        void FocusWindows(string windowsName);
        void SetFullScreen(string windowsName, bool full);
        void ToggleFullScreenWindows(string windowsName);
        void CloseAllWindows();
        bool IsOpened(string windowsName);
    }

    public class WindowsSystem : AbstractSystem, IWindowsSystem
    {
        protected override void OnInit()
        {
        }

        public void OpenWindows(string windowsName)
        {
            WindowKit.Open(windowsName);
        }

        public void CloseWindows(string windowsName)
        {
            WindowKit.Close(windowsName);
        }

        public void MinimizeWindows(string windowsName)
        {
            WindowKit.Minimize(windowsName);
        }

        public void RestoreWindows(string windowsName)
        {
            WindowKit.Restore(windowsName);
        }

        public void FocusWindows(string windowsName)
        {
            WindowKit.Focus(windowsName);
        }

        public void SetFullScreen(string windowsName, bool full)
        {
            WindowKit.SetFullScreen(windowsName, full);
        }

        public void ToggleFullScreenWindows(string windowsName)
        {
            WindowKit.ToggleFullScreen(windowsName);
        }

        public void CloseAllWindows()
        {
            WindowKit.CloseAll();
        }

        public bool IsOpened(string windowsName)
        {
            return WindowKit.IsOpened(windowsName);
        }
    }
}
