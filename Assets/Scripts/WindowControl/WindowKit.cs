using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace TapTapFirst
{
    // 窗口的三种状态（任务栏、系统层可以用它判断）
    public enum WindowKitState
    {
        Normal,
        Minimized,
        FullScreen
    }

    // 窗口套件：统一管理所有窗口的 打开/关闭/最小化/全屏 + 堆叠（z 序）
    // 用法：WindowKit.Open("WindowsSample");  WindowKit.Close("WindowsSample");  WindowKit.ToggleFullScreen("WindowsSample");
    public static class WindowKit
    {
        // ---------------- 可调参数 ----------------

        // 打开新窗口时的层叠错位（每个窗口相对上一个的偏移）
        public static Vector2 CascadeOffset = new Vector2(32f, -32f);

        // 错位到第几个后回到起点（0/1 表示不错位）
        public static int CascadeSteps = 8;

        // 打开窗口时是否自动居中 + 层叠错位（false = 用 prefab 里摆的位置）
        public static bool CascadeOnOpen = true;


        // ---------------- 事件（任务栏 / 存档 / 统计都可以订阅） ----------------

        // 契约：四个事件只在"窗口已打开且初始化完成"（WindowsBasic.IsReady == true）之后广播，
        // 回调里可以放心读 WindowName / State；没就绪就销毁的窗口不会广播 OnOpened，也不会广播 OnClosed（成对缺席）。
        // 单个订阅者抛异常不影响其它订阅者（见 SafeTrigger）。
        public static readonly EasyEvent<WindowsBasic> OnOpened = new EasyEvent<WindowsBasic>();
        public static readonly EasyEvent<WindowsBasic> OnClosed = new EasyEvent<WindowsBasic>();
        public static readonly EasyEvent<WindowsBasic> OnFocused = new EasyEvent<WindowsBasic>();
        public static readonly EasyEvent<WindowsBasic> OnStateChanged = new EasyEvent<WindowsBasic>();

        private static readonly List<WindowsBasic> sStack = new List<WindowsBasic>();                          // 底 -> 顶
        private static readonly Dictionary<string, WindowsBasic> sOpened = new Dictionary<string, WindowsBasic>();

        // 关掉 Domain Reload 时静态状态会跨会话残留（上一局的已销毁窗口还留在栈里），每次启动子系统时清一遍
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            sStack.Clear();
            sOpened.Clear();
            WindowsBasic.ResetStatics();
        }

        // EasyEvent 是多播委托：一个订阅者抛异常会中断整条订阅链，后面的系统会静默收不到事件。
        // 这里把广播包起来，异常照旧打日志，但不再影响别人。
        private static void SafeTrigger(EasyEvent<WindowsBasic> evt, WindowsBasic window)
        {
            try
            {
                evt.Trigger(window);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }

        // ---------------- 查询 ----------------

        // 当前打开的窗口，按堆叠顺序（底 -> 顶）
        public static IReadOnlyList<WindowsBasic> Windows { get { return sStack; } }

        public static int OpenedCount { get { return sStack.Count; } }

        // 当前最上面的窗口
        public static WindowsBasic Active
        {
            get { return sStack.Count > 0 ? sStack[sStack.Count - 1] : null; }
        }

        public static WindowsBasic Get(string windowName)
        {
            if (string.IsNullOrEmpty(windowName)) return null;

            WindowsBasic window;
            return sOpened.TryGetValue(windowName, out window) ? window : null;
        }

        public static bool IsOpened(string windowName)
        {
            return Get(windowName) != null;
        }

        // ---------------- 打开 / 关闭 ----------------

        // 打开窗口：已打开则恢复并置顶；没打开则按名字（= 面板名 = prefab 名）打开
        public static WindowsBasic Open(string windowName, UILevel level = UILevel.Common)
        {
            if (string.IsNullOrEmpty(windowName)) return null;

            var opened = Get(windowName);

            if (opened != null && opened.IsPanelManagedByUIKit)
            {
                opened.ShowWindow();
                return opened;
            }

            return OpenPanel(windowName, level);
        }

        // 打开并指定初始尺寸（居中）
        public static WindowsBasic Open(string windowName, Vector2 size, bool center = true, UILevel level = UILevel.Common)
        {
            var window = Open(windowName, level);
            if (window == null) return null;

            if (center) window.CenterWindow();
            window.SetWindowSize(size);
            return window;
        }

        // 用类型打开（UIKit 约定：类型名 = 面板名）
        public static WindowsBasic Open<T>(UILevel level = UILevel.Common) where T : UIPanel
        {
            var panel = UIKit.GetPanel<T>();
            if (panel != null)
            {
                var window = panel.GetComponentInChildren<WindowsBasic>(true);
                if (window != null) return Register(window);
            }

            return OpenPanel<T>(level);
        }

        public static WindowsBasic Open<T>(Vector2 size, bool center = true, UILevel level = UILevel.Common) where T : UIPanel
        {
            var window = Open<T>(level);
            if (window == null) return null;

            if (center) window.CenterWindow();
            window.SetWindowSize(size);
            return window;
        }

        // 直接按面板名打开（不需要注册表）：panelName = prefab 名 = 根物体名
        public static WindowsBasic OpenPanel(string panelName, UILevel level = UILevel.Common)
        {
            if (string.IsNullOrEmpty(panelName)) return null;

            return OpenPanelSafe(() => UIKit.OpenPanel(panelName, level), panelName);
        }

        // 用类型打开（UIKit 约定：类型名 = 面板名），编译期就能查出名字写错
        public static WindowsBasic OpenPanel<T>(UILevel level = UILevel.Common) where T : UIPanel
        {
            return OpenPanelSafe(() => UIKit.OpenPanel<T>(level), typeof(T).Name);
        }

        // UIKit / ResKit 打开失败时给一条能照着修的错误（否则只会看到一堆 Failed to Create Res + NRE）
        private static WindowsBasic OpenPanelSafe(System.Func<UIPanel> open, string panelName)
        {
            try
            {
                var panel = open();
                if (panel == null) return null;

                var window = panel.GetComponentInChildren<WindowsBasic>(true);
                if (window == null)
                {
                    Debug.LogWarning("[WindowKit] 面板 " + panelName + " 打开了，但窗口根上没挂 WindowsBasic。", panel);
                }

                return Register(window);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[WindowKit] 打开面板失败：" + panelName + "\n" +
                               "  1) 先确认 prefab 文件名 = 打开用的名字（大小写不敏感）：Art/UIPrefab/" + panelName + ".prefab\n" +
                               "  2) 该 prefab 必须标了 AssetBundle —— ResKit 是按资源名从 AB 表里找的，没标 AB 就找不到它\n" + e);
                return null;
            }
        }


        public static void Close(string windowName)
        {
            var window = Get(windowName);
            if (window != null)
            {
                window.CloseWindow();
                return;
            }

            if (!string.IsNullOrEmpty(windowName) && UIKit.GetPanel(windowName) != null) UIKit.ClosePanel(windowName);
        }

        public static void CloseAll()
        {
            var all = sStack.ToArray();
            for (var i = all.Length - 1; i >= 0; i--)
            {
                if (all[i] != null) all[i].CloseWindow();
            }
        }

        public static void Minimize(string windowName)
        {
            var window = Get(windowName);
            if (window != null) window.MinimizeWindow();
        }

        // 从最小化恢复（或已打开时置顶）
        public static void Restore(string windowName)
        {
            var window = Get(windowName);
            if (window != null) window.ShowWindow();
            else Open(windowName);
        }

        // ---------------- 全屏 ----------------

        public static void SetFullScreen(string windowName, bool full)
        {
            var window = Get(windowName);
            if (window != null) window.SetFullScreen(full);
        }

        public static void ToggleFullScreen(string windowName)
        {
            var window = Get(windowName);
            if (window != null) window.ToggleFullScreen();
        }

        // ---------------- 堆叠（z 序） ----------------

        // 把窗口提到最上层（点任意位置、进全屏、打开时都会走这里）
        public static void Focus(WindowsBasic window)
        {
            if (window == null) return;

            if (sStack.Contains(window))
            {
                if (sStack[sStack.Count - 1] == window)
                {
                    window.RaisePanel();
                    return;
                }

                sStack.Remove(window);
            }

            sStack.Add(window);
            window.RaisePanel();

            if (!window.OpenedNotified) return;      // 还没广播过打开（窗口尚未初始化完成）就不发聚焦

            window.NotifyFocused();
            SafeTrigger(OnFocused, window);
        }

        public static void Focus(string windowName)
        {
            Focus(Get(windowName));
        }

        // 按名字改尺寸 / 居中（不想先 Get 一遍时用）
        public static void SetWindowSize(string windowName, Vector2 size, bool keepCenter = true)
        {
            var window = Get(windowName);
            if (window != null) window.SetWindowSize(size, keepCenter);
        }

        public static void CenterWindow(string windowName)
        {
            var window = Get(windowName);
            if (window != null) window.CenterWindow();
        }

        // 层叠重排所有可见窗口
        public static void CascadeAll()
        {
            var index = 0;
            for (var i = 0; i < sStack.Count; i++)
            {
                var window = sStack[i];
                if (window == null || !window.gameObject.activeInHierarchy) continue;

                window.CascadeTo(index++);
            }
        }

        // 第 index 个窗口的层叠偏移
        public static Vector2 CascadeOffsetAt(int index)
        {
            if (CascadeSteps <= 1) return Vector2.zero;

            var step = index % CascadeSteps;
            return new Vector2(CascadeOffset.x * step, CascadeOffset.y * step);
        }

        // ---------------- 内部：给 WindowsBasic 调用 ----------------

        internal static WindowsBasic Register(WindowsBasic window)
        {
            if (window == null) return null;

            if (!string.IsNullOrEmpty(window.WindowName)) sOpened[window.WindowName] = window;

            sStack.Remove(window);
            sStack.Add(window);
            window.RaisePanel();

            // 面板会被注册两次：UIKit.OpenPanel 当帧一次（此时还没 Start，WindowName / State 都没就绪），
            // 窗口 InitWindow 末尾再一次。只有就绪后的那一次才对外广播，订阅者拿到的窗口一定可用。
            if (!window.IsReady) return window;

            if (!window.OpenedNotified)
            {
                window.OpenedNotified = true;
                SafeTrigger(OnOpened, window);
            }

            window.NotifyFocused();
            SafeTrigger(OnFocused, window);

            return window;
        }

        internal static void Unregister(WindowsBasic window)
        {
            if (window == null) return;

            sStack.Remove(window);

            if (!string.IsNullOrEmpty(window.WindowName))
            {
                WindowsBasic current;
                if (sOpened.TryGetValue(window.WindowName, out current) && current == window) sOpened.Remove(window.WindowName);
            }

            // 没广播过打开的窗口也不广播关闭，保证 OnOpened / OnClosed 成对
            if (window.OpenedNotified)
            {
                window.OpenedNotified = false;
                SafeTrigger(OnClosed, window);
            }

            FocusTopVisible();
        }

        internal static void NotifyStateChanged(WindowsBasic window)
        {
            if (window == null) return;

            if (window.OpenedNotified) SafeTrigger(OnStateChanged, window);

            if (window.KitState == WindowKitState.Minimized) FocusTopVisible();
        }

        internal static Vector2 NextCascadeOffset()
        {
            return CascadeOnOpen ? CascadeOffsetAt(sStack.Count) : Vector2.zero;
        }

        internal static void FocusTopVisible()
        {
            for (var i = sStack.Count - 1; i >= 0; i--)
            {
                var window = sStack[i];
                if (window == null || !window.gameObject.activeInHierarchy) continue;

                Focus(window);
                return;
            }
        }

        // ---------------- 私有 ----------------

        // （这里以前有个 GetWindowsSystem 辅助，SO/配置表移除后已经不需要了）
    }
}
