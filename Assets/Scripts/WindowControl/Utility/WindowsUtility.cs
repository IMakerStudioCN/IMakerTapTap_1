using System;
using System.Collections;
using UnityEngine;
using QFramework;

namespace TapTapFirst
{
    // 已生成模块接口 IWindowsUtility，请在 Architecture.Init() 中按接口类型注册：
    //     this.RegisterUtility<IWindowsUtility>(new WindowsUtility());
    public interface IWindowsUtility : IUtility
    {
        // ---- 状态 ----
        WindowFrameState NewState(RectTransform frame);
        void ResetState(RectTransform frame, WindowFrameState state);
        void SetWindowedRect(RectTransform frame, WindowFrameState state, Rect windowedRect, bool applyNow = true);
        void SaveWindowedRect(RectTransform frame, WindowFrameState state);

        // ---- 几何 / 查询 ----
        Rect GetRect(RectTransform rt, RectTransform space);
        Rect GetRectInParent(RectTransform rt);
        Rect RectLerp(Rect a, Rect b, float t);
        RectTransform FindPanelRoot(RectTransform frame);
        RectTransform FindChildByName(RectTransform root, string childName);
        WindowEdge GetEdgeFromPoint(RectTransform frame, Vector2 localPoint, float border);
        Rect MoveRect(RectTransform frame, Rect startRect, Vector2 delta,
            float taskbarHeight = WindowsUtility.DefaultTaskbarHeight, Vector2 minSize = default);
        Rect ResizeRect(RectTransform frame, Rect startRect, Vector2 delta, WindowEdge edges,
            float taskbarHeight = WindowsUtility.DefaultTaskbarHeight, Vector2 minSize = default);
        Rect GetDesktopRect(RectTransform frame, float taskbarHeight = WindowsUtility.DefaultTaskbarHeight);
        Rect GetFullScreenRect(RectTransform frame, float taskbarHeight = WindowsUtility.DefaultTaskbarHeight);
        Rect GetTargetRect(RectTransform frame, WindowFrameState state, bool full,
            float taskbarHeight = WindowsUtility.DefaultTaskbarHeight, Vector2 minSize = default);
        Rect ClampToDesktop(RectTransform frame, Rect rect,
            float taskbarHeight = WindowsUtility.DefaultTaskbarHeight, Vector2 minSize = default);
        Rect CenterRect(Rect desktop, Vector2 size, Vector2 offset = default);
        Rect ResizeToSize(Rect rect, Vector2 size, bool keepCenter = true);

        // ---- 应用 ----
        void ApplyRect(RectTransform rt, Rect rect);
        void BringToFront(RectTransform frame);

        // ---- 全屏切换 ----
        void SetFullScreen(MonoBehaviour host, RectTransform frame, WindowFrameState state, bool full,
            bool animate = true, float duration = WindowsUtility.DefaultDuration,
            float taskbarHeight = WindowsUtility.DefaultTaskbarHeight, Vector2 minSize = default);
        void ToggleFullScreen(MonoBehaviour host, RectTransform frame, WindowFrameState state,
            bool animate = true, float duration = WindowsUtility.DefaultDuration,
            float taskbarHeight = WindowsUtility.DefaultTaskbarHeight, Vector2 minSize = default);
        void SetFullScreenImmediate(RectTransform frame, WindowFrameState state, bool full,
            float taskbarHeight = WindowsUtility.DefaultTaskbarHeight, Vector2 minSize = default);
        void RestoreImmediate(RectTransform frame, WindowFrameState state,
            float taskbarHeight = WindowsUtility.DefaultTaskbarHeight, Vector2 minSize = default);
        IEnumerator CoSetFullScreen(RectTransform frame, WindowFrameState state, bool full,
            bool animate = true, float duration = WindowsUtility.DefaultDuration,
            float taskbarHeight = WindowsUtility.DefaultTaskbarHeight, Vector2 minSize = default);

        // ---- 内容适配 ----
        void StretchBox(RectTransform child, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f);
        void AutoFit(RectTransform child, bool horizontal, bool vertical);
        void PinCorner(RectTransform child, WindowCorner corner, float dx = 0f, float dy = 0f);
        void AutoPinCorner(RectTransform child);
        void AutoPinCorner(RectTransform child, WindowCorner corner);
    }

    public enum WindowCorner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    [Flags]
    public enum WindowEdge
    {
        None = 0,
        Left = 1,
        Right = 2,
        Bottom = 4,
        Top = 8
    }

    // 一个窗口实例自己的几何状态：由外部持有（每个面板一个），工具类本身不存状态
    public class WindowFrameState
    {
        public bool Initialized;
        public bool IsFullScreen;
        public Rect WindowedRect;      // 窗口态矩形，坐标系 = 窗口根父物体的局部坐标
        internal Coroutine Animation;  // 动画互斥用
    }

    // 窗口几何算法：静态调用即可；也可以 RegisterUtility 之后用 this.GetUtility<IWindowsUtility>() 调
    public class WindowsUtility : IWindowsUtility
    {
        public const float DefaultTaskbarHeight = 50f;   // ComputerDisplay/Taskbar 的高度
        public const float DefaultDuration = 0.18f;
        public static readonly Vector2 DefaultMinSize = new Vector2(240f, 160f);

        #region 基础几何

        // 取 rt 在 space 局部坐标下的矩形（和锚点/轴心无关，怎么摆都正确）
        public static Rect GetRect(RectTransform rt, RectTransform space)
        {
            if (rt == null || space == null) return default;

            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);                 // 0=左下 1=左上 2=右上 3=右下
            var min = space.InverseTransformPoint(corners[0]);
            var max = space.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        public static Rect GetRectInParent(RectTransform rt)
        {
            return rt != null && rt.parent is RectTransform parent ? GetRect(rt, parent) : default;
        }

        // 把矩形写进 RectTransform：统一改成 stretch 锚点(0,0)-(1,1) + offset
        public static void ApplyRect(RectTransform rt, Rect rect)
        {
            if (rt == null || !(rt.parent is RectTransform space)) return;

            var parentRect = space.rect;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(rect.xMin - parentRect.xMin, rect.yMin - parentRect.yMin);
            rt.offsetMax = new Vector2(rect.xMax - parentRect.xMax, rect.yMax - parentRect.yMax);
        }

        public static Rect RectLerp(Rect a, Rect b, float t)
        {
            return new Rect(
                Mathf.Lerp(a.xMin, b.xMin, t),
                Mathf.Lerp(a.yMin, b.yMin, t),
                Mathf.Lerp(a.width, b.width, t),
                Mathf.Lerp(a.height, b.height, t));
        }

        // 面板根：UIKit 每次 OpenPanel 都会把它拉满画布，所以它就是"桌面区"
        public static RectTransform FindPanelRoot(RectTransform frame)
        {
            if (frame == null) return null;

            var panel = frame.GetComponentInParent<UIPanel>();
            if (panel != null) return (RectTransform)panel.transform;

            var root = frame;
            while (root.parent is RectTransform parent && parent.GetComponent<Canvas>() == null) root = parent;
            return root;
        }

        // 递归找一个同名子物体（自动装配 Bar / ContentBox / Exit / Mini / Full 用）
        public static RectTransform FindChildByName(RectTransform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName)) return null;

            for (var i = 0; i < root.childCount; i++)
            {
                if (!(root.GetChild(i) is RectTransform child)) continue;
                if (child.name == childName) return child;

                var found = FindChildByName(child, childName);
                if (found != null) return found;
            }

            return null;
        }

        // localPoint 用 RectTransformUtility.ScreenPointToLocalPointInRectangle(frame, ...) 取
        // 返回指针落在窗口的哪几条边上（border 内算命中），None 表示不在边缘
        public static WindowEdge GetEdgeFromPoint(RectTransform frame, Vector2 localPoint, float border)
        {
            if (frame == null || border <= 0f) return WindowEdge.None;

            var rect = frame.rect;
            var edge = WindowEdge.None;
            if (localPoint.x - rect.xMin <= border) edge |= WindowEdge.Left;
            if (rect.xMax - localPoint.x <= border) edge |= WindowEdge.Right;
            if (localPoint.y - rect.yMin <= border) edge |= WindowEdge.Bottom;
            if (rect.yMax - localPoint.y <= border) edge |= WindowEdge.Top;
            return edge;
        }

        // 拖动：整块平移，尺寸不变，自动夹回桌面区
        public static Rect MoveRect(RectTransform frame, Rect startRect, Vector2 delta,
            float taskbarHeight = DefaultTaskbarHeight, Vector2 minSize = default)
        {
            return ClampToDesktop(frame, new Rect(startRect.position + delta, startRect.size), taskbarHeight, minSize);
        }

        // 缩放：只动被拖的那几条边，保持不小于 minSize，并夹在桌面区内
        public static Rect ResizeRect(RectTransform frame, Rect startRect, Vector2 delta, WindowEdge edges,
            float taskbarHeight = DefaultTaskbarHeight, Vector2 minSize = default)
        {
            if (edges == WindowEdge.None) return startRect;
            if (minSize.x <= 0f || minSize.y <= 0f) minSize = DefaultMinSize;

            var desktop = GetDesktopRect(frame, taskbarHeight);
            var min = startRect.min;
            var max = startRect.max;

            if ((edges & WindowEdge.Left) != 0) min.x = Mathf.Min(min.x + delta.x, max.x - minSize.x);
            if ((edges & WindowEdge.Right) != 0) max.x = Mathf.Max(max.x + delta.x, min.x + minSize.x);
            if ((edges & WindowEdge.Bottom) != 0) min.y = Mathf.Min(min.y + delta.y, max.y - minSize.y);
            if ((edges & WindowEdge.Top) != 0) max.y = Mathf.Max(max.y + delta.y, min.y + minSize.y);

            if ((edges & WindowEdge.Left) != 0) min.x = Mathf.Max(min.x, desktop.xMin);
            if ((edges & WindowEdge.Right) != 0) max.x = Mathf.Min(max.x, desktop.xMax);
            if ((edges & WindowEdge.Bottom) != 0) min.y = Mathf.Max(min.y, desktop.yMin);
            if ((edges & WindowEdge.Top) != 0) max.y = Mathf.Min(max.y, desktop.yMax);

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        #endregion

        #region 桌面区 / 目标矩形

        // 桌面可用区（坐标系 = 窗口根的父物体），已扣掉底部任务栏
        public static Rect GetDesktopRect(RectTransform frame, float taskbarHeight = DefaultTaskbarHeight)
        {
            if (frame == null || !(frame.parent is RectTransform space)) return default;

            var root = FindPanelRoot(frame);
            var rect = root != null && root != space ? GetRect(root, space) : space.rect;
            rect.yMin += taskbarHeight;      // 想连任务栏一起盖住就传 0
            return rect;
        }

        public static Rect GetFullScreenRect(RectTransform frame, float taskbarHeight = DefaultTaskbarHeight)
        {
            return GetDesktopRect(frame, taskbarHeight);
        }

        // 在桌面区里居中放一个指定尺寸的矩形（offset 是相对中心的偏移）
        public static Rect CenterRect(Rect desktop, Vector2 size, Vector2 offset = default)
        {
            size = new Vector2(Mathf.Max(size.x, 1f), Mathf.Max(size.y, 1f));
            var pos = new Vector2(
                desktop.xMin + (desktop.width - size.x) * 0.5f + offset.x,
                desktop.yMin + (desktop.height - size.y) * 0.5f + offset.y);
            return new Rect(pos, size);
        }

        // 改矩形尺寸，keepCenter = 保持中心不动
        public static Rect ResizeToSize(Rect rect, Vector2 size, bool keepCenter = true)
        {
            size = new Vector2(Mathf.Max(size.x, 1f), Mathf.Max(size.y, 1f));
            if (!keepCenter) return new Rect(rect.position, size);

            var center = rect.center;
            return new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y);
        }

        public static Rect ClampToDesktop(RectTransform frame, Rect rect,
            float taskbarHeight = DefaultTaskbarHeight, Vector2 minSize = default)
        {
            if (minSize.x <= 0f || minSize.y <= 0f) minSize = DefaultMinSize;

            var desktop = GetDesktopRect(frame, taskbarHeight);
            var size = new Vector2(Mathf.Max(rect.width, minSize.x), Mathf.Max(rect.height, minSize.y));
            var pos = rect.min;
            pos.x = Mathf.Clamp(pos.x, desktop.xMin, Mathf.Max(desktop.xMin, desktop.xMax - size.x));
            pos.y = Mathf.Clamp(pos.y, desktop.yMin, Mathf.Max(desktop.yMin, desktop.yMax - size.y));
            return new Rect(pos, size);
        }

        public static Rect GetTargetRect(RectTransform frame, WindowFrameState state, bool full,
            float taskbarHeight = DefaultTaskbarHeight, Vector2 minSize = default)
        {
            if (full) return GetDesktopRect(frame, taskbarHeight);

            var rect = state != null && state.Initialized ? state.WindowedRect : GetRectInParent(frame);
            return ClampToDesktop(frame, rect, taskbarHeight, minSize);
        }

        #endregion

        #region 状态

        // 在 Start / OnOpen 里调用：把"你在 prefab 里摆的窗口位置"记成窗口态
        public static WindowFrameState NewState(RectTransform frame)
        {
            var state = new WindowFrameState();
            ResetState(frame, state);
            return state;
        }

        public static void ResetState(RectTransform frame, WindowFrameState state)
        {
            if (state == null) return;

            state.WindowedRect = GetRectInParent(frame);
            state.IsFullScreen = false;
            state.Initialized = true;
        }

        public static void SetWindowedRect(RectTransform frame, WindowFrameState state, Rect windowedRect,
            bool applyNow = true)
        {
            if (state == null) return;

            state.WindowedRect = windowedRect;
            state.Initialized = true;
            if (applyNow && !state.IsFullScreen) ApplyRect(frame, ClampToDesktop(frame, windowedRect));
        }

        // 关窗 / 最小化前调用，记住当前位置
        public static void SaveWindowedRect(RectTransform frame, WindowFrameState state)
        {
            if (state == null || state.IsFullScreen) return;

            state.WindowedRect = GetRectInParent(frame);
            state.Initialized = true;
        }

        #endregion

        #region 全屏切换

        public static void SetFullScreen(MonoBehaviour host, RectTransform frame, WindowFrameState state, bool full,
            bool animate = true, float duration = DefaultDuration,
            float taskbarHeight = DefaultTaskbarHeight, Vector2 minSize = default)
        {
            if (host == null || !host.isActiveAndEnabled)
            {
                SetFullScreenImmediate(frame, state, full, taskbarHeight, minSize);
                return;
            }

            if (state != null && state.Animation != null) host.StopCoroutine(state.Animation);
            if (state != null) state.Animation = null;

            var coroutine = host.StartCoroutine(
                CoSetFullScreen(frame, state, full, animate, duration, taskbarHeight, minSize));
            if (state != null) state.Animation = coroutine;
        }

        public static void ToggleFullScreen(MonoBehaviour host, RectTransform frame, WindowFrameState state,
            bool animate = true, float duration = DefaultDuration,
            float taskbarHeight = DefaultTaskbarHeight, Vector2 minSize = default)
        {
            if (state == null) state = NewState(frame);

            SetFullScreen(host, frame, state, !state.IsFullScreen, animate, duration, taskbarHeight, minSize);
        }

        public static void SetFullScreenImmediate(RectTransform frame, WindowFrameState state, bool full,
            float taskbarHeight = DefaultTaskbarHeight, Vector2 minSize = default)
        {
            if (state == null) state = NewState(frame);
            if (!state.Initialized) ResetState(frame, state);

            if (full && !state.IsFullScreen) state.WindowedRect = GetRectInParent(frame);
            state.IsFullScreen = full;

            ApplyRect(frame, GetTargetRect(frame, state, full, taskbarHeight, minSize));
            BringToFront(frame);
        }

        // 打开面板 / 重新显示时用：直接摆到状态对应的位置，不做动画（避免开窗闪一下）
        public static void RestoreImmediate(RectTransform frame, WindowFrameState state,
            float taskbarHeight = DefaultTaskbarHeight, Vector2 minSize = default)
        {
            if (state == null) return;
            if (!state.Initialized) ResetState(frame, state);

            ApplyRect(frame, GetTargetRect(frame, state, state.IsFullScreen, taskbarHeight, minSize));
        }

        public static IEnumerator CoSetFullScreen(RectTransform frame, WindowFrameState state, bool full,
            bool animate = true, float duration = DefaultDuration,
            float taskbarHeight = DefaultTaskbarHeight, Vector2 minSize = default)
        {
            if (state == null) state = NewState(frame);
            if (!state.Initialized) ResetState(frame, state);

            var from = GetRectInParent(frame);
            var to = GetTargetRect(frame, state, full, taskbarHeight, minSize);

            if (full) state.WindowedRect = from;
            state.IsFullScreen = full;
            BringToFront(frame);

            if (!animate || duration <= 0f)
            {
                ApplyRect(frame, to);
                yield break;
            }

            for (var time = 0f; time < duration; time += Time.unscaledDeltaTime)
            {
                if (frame == null) yield break;
                ApplyRect(frame, RectLerp(from, to, Mathf.SmoothStep(0f, 1f, time / duration)));
                yield return null;
            }

            if (frame != null) ApplyRect(frame, to);
        }

        // 把面板提到最前（UIKit 下所有面板都是 UIRoot 的兄弟节点）
        public static void BringToFront(RectTransform frame)
        {
            var panel = frame != null ? frame.GetComponentInParent<UIPanel>() : null;
            if (panel != null) panel.transform.SetAsLastSibling();
        }

        #endregion

        #region 内容适配（只在初始化时换算一次锚点，之后窗口变形自动跟随）

        // 四边跟窗口走：l/b/r/t = 距离父级四边的像素（内容区最常用）
        public static void StretchBox(RectTransform child,
            float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            if (child == null) return;

            child.anchorMin = Vector2.zero;
            child.anchorMax = Vector2.one;
            child.offsetMin = new Vector2(left, bottom);
            child.offsetMax = new Vector2(-right, -top);
        }

        // 用"你现在摆的样子"自动推边距：
        //   标题栏 AutoFit(bar, true, false) / 内容区 AutoFit(content, true, true) / 侧栏 AutoFit(side, false, true)
        public static void AutoFit(RectTransform child, bool horizontal, bool vertical)
        {
            if (child == null || !(child.parent is RectTransform parent)) return;

            var rect = GetRectInParent(child);
            var parentRect = parent.rect;
            var left = rect.xMin - parentRect.xMin;
            var bottom = rect.yMin - parentRect.yMin;
            var right = parentRect.xMax - rect.xMax;
            var top = parentRect.yMax - rect.yMax;

            if (horizontal && vertical)
            {
                StretchBox(child, left, bottom, right, top);
                return;
            }

            if (horizontal)
            {
                child.anchorMin = new Vector2(0f, 1f);
                child.anchorMax = new Vector2(1f, 1f);
                child.offsetMin = new Vector2(left, -(top + rect.height));
                child.offsetMax = new Vector2(-right, -top);
                return;
            }

            if (vertical)
            {
                child.anchorMin = new Vector2(0f, 0f);
                child.anchorMax = new Vector2(0f, 1f);
                child.offsetMin = new Vector2(left, bottom);
                child.offsetMax = new Vector2(left + rect.width, -top);
            }
        }

        // 固定尺寸钉在某个角（Exit/Mini/Full 这种按钮），dx/dy = 离父级该角的距离
        public static void PinCorner(RectTransform child, WindowCorner corner, float dx = 0f, float dy = 0f)
        {
            if (child == null || !(child.parent is RectTransform parent)) return;

            var rect = GetRectInParent(child);
            var parentRect = parent.rect;
            var anchor = corner == WindowCorner.TopLeft ? new Vector2(0f, 1f)
                : corner == WindowCorner.TopRight ? new Vector2(1f, 1f)
                : corner == WindowCorner.BottomLeft ? new Vector2(0f, 0f)
                : new Vector2(1f, 0f);

            var min = new Vector2(
                anchor.x > 0.5f ? parentRect.xMax - dx - rect.width : parentRect.xMin + dx,
                anchor.y > 0.5f ? parentRect.yMax - dy - rect.height : parentRect.yMin + dy);
            var anchorPoint = new Vector2(
                parentRect.xMin + anchor.x * parentRect.width,
                parentRect.yMin + anchor.y * parentRect.height);
            var pivotPoint = new Vector2(
                min.x + rect.width * child.pivot.x,
                min.y + rect.height * child.pivot.y);

            child.anchorMin = anchor;
            child.anchorMax = anchor;
            child.sizeDelta = rect.size;
            child.anchoredPosition = pivotPoint - anchorPoint;
        }

        // 不改数值，只把现在摆的位置翻译成 钉在最近的那个角
        public static void AutoPinCorner(RectTransform child)
        {
            if (child == null || !(child.parent is RectTransform parent)) return;

            var rect = GetRectInParent(child);
            var parentRect = parent.rect;
            var left = rect.xMin - parentRect.xMin;
            var right = parentRect.xMax - rect.xMax;
            var bottom = rect.yMin - parentRect.yMin;
            var top = parentRect.yMax - rect.yMax;

            var corner = bottom <= top
                ? (left <= right ? WindowCorner.BottomLeft : WindowCorner.BottomRight)
                : (left <= right ? WindowCorner.TopLeft : WindowCorner.TopRight);

            PinCorner(child, corner, left <= right ? left : right, bottom <= top ? bottom : top);
        }

        public static void AutoPinCorner(RectTransform child, WindowCorner corner)
        {
            if (child == null || !(child.parent is RectTransform parent)) return;

            var rect = GetRectInParent(child);
            var parentRect = parent.rect;
            var dx = corner == WindowCorner.TopLeft || corner == WindowCorner.BottomLeft
                ? rect.xMin - parentRect.xMin
                : parentRect.xMax - rect.xMax;
            var dy = corner == WindowCorner.BottomLeft || corner == WindowCorner.BottomRight
                ? rect.yMin - parentRect.yMin
                : parentRect.yMax - rect.yMax;

            PinCorner(child, corner, dx, dy);
        }

        #endregion

        #region IWindowsUtility 显式实现（转发到上面的静态方法，两种调用方式都能用）

        WindowFrameState IWindowsUtility.NewState(RectTransform frame) => NewState(frame);

        void IWindowsUtility.ResetState(RectTransform frame, WindowFrameState state) => ResetState(frame, state);

        void IWindowsUtility.SetWindowedRect(RectTransform frame, WindowFrameState state, Rect windowedRect,
            bool applyNow) => SetWindowedRect(frame, state, windowedRect, applyNow);

        void IWindowsUtility.SaveWindowedRect(RectTransform frame, WindowFrameState state)
            => SaveWindowedRect(frame, state);

        Rect IWindowsUtility.GetRect(RectTransform rt, RectTransform space) => GetRect(rt, space);

        Rect IWindowsUtility.GetRectInParent(RectTransform rt) => GetRectInParent(rt);

        Rect IWindowsUtility.RectLerp(Rect a, Rect b, float t) => RectLerp(a, b, t);

        RectTransform IWindowsUtility.FindPanelRoot(RectTransform frame) => FindPanelRoot(frame);

        Rect IWindowsUtility.GetDesktopRect(RectTransform frame, float taskbarHeight)
            => GetDesktopRect(frame, taskbarHeight);

        Rect IWindowsUtility.GetFullScreenRect(RectTransform frame, float taskbarHeight)
            => GetFullScreenRect(frame, taskbarHeight);

        Rect IWindowsUtility.GetTargetRect(RectTransform frame, WindowFrameState state, bool full,
            float taskbarHeight, Vector2 minSize) => GetTargetRect(frame, state, full, taskbarHeight, minSize);

        Rect IWindowsUtility.ClampToDesktop(RectTransform frame, Rect rect, float taskbarHeight, Vector2 minSize)
            => ClampToDesktop(frame, rect, taskbarHeight, minSize);

        Rect IWindowsUtility.CenterRect(Rect desktop, Vector2 size, Vector2 offset)
            => CenterRect(desktop, size, offset);

        Rect IWindowsUtility.ResizeToSize(Rect rect, Vector2 size, bool keepCenter)
            => ResizeToSize(rect, size, keepCenter);

        void IWindowsUtility.ApplyRect(RectTransform rt, Rect rect) => ApplyRect(rt, rect);

        void IWindowsUtility.BringToFront(RectTransform frame) => BringToFront(frame);

        void IWindowsUtility.SetFullScreen(MonoBehaviour host, RectTransform frame, WindowFrameState state,
            bool full, bool animate, float duration, float taskbarHeight, Vector2 minSize)
            => SetFullScreen(host, frame, state, full, animate, duration, taskbarHeight, minSize);

        void IWindowsUtility.ToggleFullScreen(MonoBehaviour host, RectTransform frame, WindowFrameState state,
            bool animate, float duration, float taskbarHeight, Vector2 minSize)
            => ToggleFullScreen(host, frame, state, animate, duration, taskbarHeight, minSize);

        void IWindowsUtility.SetFullScreenImmediate(RectTransform frame, WindowFrameState state, bool full,
            float taskbarHeight, Vector2 minSize)
            => SetFullScreenImmediate(frame, state, full, taskbarHeight, minSize);

        void IWindowsUtility.RestoreImmediate(RectTransform frame, WindowFrameState state,
            float taskbarHeight, Vector2 minSize) => RestoreImmediate(frame, state, taskbarHeight, minSize);

        IEnumerator IWindowsUtility.CoSetFullScreen(RectTransform frame, WindowFrameState state, bool full,
            bool animate, float duration, float taskbarHeight, Vector2 minSize)
            => CoSetFullScreen(frame, state, full, animate, duration, taskbarHeight, minSize);

        void IWindowsUtility.StretchBox(RectTransform child, float left, float bottom, float right, float top)
            => StretchBox(child, left, bottom, right, top);

        void IWindowsUtility.AutoFit(RectTransform child, bool horizontal, bool vertical)
            => AutoFit(child, horizontal, vertical);

        void IWindowsUtility.PinCorner(RectTransform child, WindowCorner corner, float dx, float dy)
            => PinCorner(child, corner, dx, dy);

        void IWindowsUtility.AutoPinCorner(RectTransform child) => AutoPinCorner(child);

        void IWindowsUtility.AutoPinCorner(RectTransform child, WindowCorner corner) => AutoPinCorner(child, corner);

        RectTransform IWindowsUtility.FindChildByName(RectTransform root, string childName)
            => FindChildByName(root, childName);

        WindowEdge IWindowsUtility.GetEdgeFromPoint(RectTransform frame, Vector2 localPoint, float border)
            => GetEdgeFromPoint(frame, localPoint, border);

        Rect IWindowsUtility.MoveRect(RectTransform frame, Rect startRect, Vector2 delta,
            float taskbarHeight, Vector2 minSize) => MoveRect(frame, startRect, delta, taskbarHeight, minSize);

        Rect IWindowsUtility.ResizeRect(RectTransform frame, Rect startRect, Vector2 delta, WindowEdge edges,
            float taskbarHeight, Vector2 minSize) => ResizeRect(frame, startRect, delta, edges, taskbarHeight, minSize);

        #endregion
    }
}
