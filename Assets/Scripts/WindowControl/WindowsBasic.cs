using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using QFramework;

namespace TapTapFirst
{
    // 所有窗口类面板的基类：自动全屏适配 / 标题栏拖拽 / 边缘缩放 / 边界夹取 / 置顶 / 最小化 / 关闭 / 位置记忆
    [RequireComponent(typeof(RectTransform))]
    public class WindowsBasic : ViewController, IController,
        IPointerDownHandler, IDragHandler, IPointerUpHandler, IPointerClickHandler
    {
        private enum PointerMode
        {
            None,
            Move,
            Resize
        }

        private struct SavedState
        {
            public bool Valid;
            public Rect WindowedRect;
            public bool IsFullScreen;
        }

        [Header("窗口标识（留空 = 自动用面板名）")]
        [SerializeField] private string panelName;

        [Header("窗口节点（留空 = 自动找同名子物体 Bar / ContentBox / Exit / Mini / Full）")]
        [SerializeField] private RectTransform frame;
        [SerializeField] private RectTransform dragHandle;
        [SerializeField] private RectTransform contentBox;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button miniButton;
        [SerializeField] private Toggle fullScreenToggle;

        [Header("初始窗口（尺寸填 0 = 用 prefab 里摆的尺寸，做套件用）")]
        [SerializeField] private Vector2 initWindowSize = Vector2.zero;
        [SerializeField] private bool initCenterOnDesktop = true;
        [SerializeField] private Vector2 initWindowOffset = Vector2.zero;
        [SerializeField] private bool startFullScreen;                 // 打开就全屏（不依赖 WindowsSO）

        [Header("几何 / 全屏")]
        [SerializeField] private float taskbarHeight = WindowsUtility.DefaultTaskbarHeight;
        [SerializeField] private Vector2 minSize = WindowsUtility.DefaultMinSize;
        [SerializeField] private float duration = WindowsUtility.DefaultDuration;
        [SerializeField] private bool autoAdaptLayout = true;
        [SerializeField] private bool rememberAcrossClose = true;
        [SerializeField] private bool autoCreateRaycastTarget = true;
        [SerializeField] private bool autoClipContent = true;              // 内容区自动加 RectMask2D：窗口缩小时内容不溢出到窗口外

        [Header("交互")]
        [SerializeField] private bool enableDrag = true;
        [SerializeField] private bool enableResize = true;
        [SerializeField] private float resizeBorder = 6f;
        [SerializeField] private bool bringToFrontOnPointerDown = true;
        [SerializeField] private bool doubleClickTitleBarTogglesFullScreen = true;
        [SerializeField] private bool dragFromFullScreenRestores = true;

        private static readonly Dictionary<string, SavedState> SavedStates = new Dictionary<string, SavedState>();

        internal static void ResetStatics()
        {
            SavedStates.Clear();
        }

        private IWindowsUtility mUtility;
        private PointerMode mPointerMode;
        private WindowEdge mResizeEdges;
        private Vector2 mPointerStart;
        private Rect mRectStart;
        private bool mInitialized;
        private bool mMinimized;
        private UIPanel mPanel;

        public WindowFrameState State { get; private set; }

        public string WindowName { get; private set; }

        // 窗口是否已初始化完成（WindowName / State 可用）。WindowKit 只对就绪的窗口广播事件。
        public bool IsReady => mInitialized;

        // WindowKit 内部用：这个窗口是否已经对外广播过"打开"
        internal bool OpenedNotified { get; set; }

        public bool IsFullScreen => State != null && State.IsFullScreen;

        public RectTransform Frame => frame != null ? frame : (RectTransform)transform;

        // 只有通过 UIKit 打开、并且还在 UIKit 面板表里的，才算"套件管理的窗口"（场景里手摆的副本不算）
        internal bool IsPanelManagedByUIKit
        {
            get { return mPanel != null && UIKit.GetPanel(mPanel.name) == mPanel; }
        }

        // 内容容器：套件里开发者只需要往这里面塞东西
        public RectTransform Content
        {
            get
            {
                if (contentBox == null) contentBox = WindowsUtility.FindChildByName(Frame, "ContentBox");
                return contentBox;
            }
        }

        // 初始窗口尺寸（0 = 用 prefab 里摆的尺寸）；派生类可在 OnWindowConfigure 里赋值
        public Vector2 InitWindowSize
        {
            get { return initWindowSize; }
            set { initWindowSize = value; }
        }

        public Vector2 InitWindowOffset
        {
            get { return initWindowOffset; }
            set { initWindowOffset = value; }
        }

        public bool InitCenterOnDesktop
        {
            get { return initCenterOnDesktop; }
            set { initCenterOnDesktop = value; }
        }

        protected IWindowsSystem WinSystem { get; private set; }

        protected IWindowsUtility WinUtility
        {
            get { return mUtility != null ? mUtility : (mUtility = this.GetUtility<IWindowsUtility>()); }
        }

        public virtual IArchitecture GetArchitecture()
        {
            return TapTap.Interface;
        }

        #region 生命周期

        protected virtual void Awake()
        {
            if (frame == null) frame = (RectTransform)transform;

            WinSystem = this.GetSystem<IWindowsSystem>();

            ResolveChrome();
        }

        protected virtual void Start()
        {
            InitWindow();
        }

        protected virtual void OnEnable()
        {
            if (!mInitialized) return;

            mMinimized = false;

            RestoreWindow();
            WindowKit.Focus(this);
            NotifyStateChanged();
        }

        protected virtual void OnDisable()
        {
            mPointerMode = PointerMode.None;
            NotifyStateChanged();
        }

        protected virtual void OnDestroy()
        {
            WindowKit.Unregister(this);
        }

        // 派生类如果没有调用 base.Start()，第一次交互时也会自动补上初始化
        public void InitWindow()
        {
            if (mInitialized) return;

            mInitialized = true;

            if (frame == null) frame = (RectTransform)transform;

            // 放在这里而不是 Awake：面板是 Instantiate 出来的，Awake 时名字还是 "Xxx(Clone)"，
            // UIKit 在 CreateUI 里才把根物体改回面板名（关窗/最小化按名字查找依赖它）
            mPanel = GetComponentInParent<UIPanel>();
            WindowName = ResolveWindowName();

            if (string.IsNullOrEmpty(WindowName))
            {
                Debug.LogWarning("[WindowsBasic] 没拿到窗口名：请给 panelName 赋值，或把 prefab / 根物体改成面板名。", this);
            }

            State = WindowsUtility.NewState(frame);

            OnWindowConfigure();

            if (!TryApplySavedState())
            {
                ApplyInitRect();
                ApplyCascade();
            }

            if (autoAdaptLayout)
            {
                AdaptChrome();
                EnsureMinSizeFitsChrome();      // 最小尺寸不能小于标题栏按钮需要的尺寸
            }

            if (autoCreateRaycastTarget) EnsureRaycastTarget();
            if (autoClipContent) EnsureContentClip();

            WindowsUtility.RestoreImmediate(frame, State, taskbarHeight, minSize);

            BindChrome();
            SyncToggle();

            if (IsPanelManagedByUIKit)
            {
                WindowKit.Register(this);
            }
            else
            {
                Debug.LogWarning("[WindowsBasic] \"" + WindowName +
                                 "\" 不是通过 UIKit 打开的面板（多半是场景里手摆的一份副本）：它不会参与套件的堆叠和按名字打开。" +
                                 "正式用请把场景里这份删掉，由图标 UIKit.OpenPanel 打开。", this);
            }

            OnWindowReady();
        }

        private void EnsureInit()
        {
            if (!mInitialized) InitWindow();
        }

        #endregion

        #region 全屏 / 关闭 / 最小化

        public virtual void SetFullScreen(bool full, bool animate = true)
        {
            EnsureInit();

            WindowsUtility.SetFullScreen(this, Frame, State, full, animate, animate ? duration : 0f, taskbarHeight, minSize);
            SyncToggle();
            SaveState();
            WindowKit.Focus(this);
            OnFullScreenChanged(IsFullScreen);
            NotifyStateChanged();
        }

        public void ToggleFullScreen()
        {
            SetFullScreen(!IsFullScreen);
        }

        public virtual void CloseWindow()
        {
            SaveState();
            OnWindowClose();

            if (mPanel != null) UIKit.ClosePanel(mPanel);                      // 用实例关，最稳（不依赖 UIKit 的名字索引）
            else if (!string.IsNullOrEmpty(WindowName)) UIKit.ClosePanel(WindowName);
            else WindowKit.Unregister(this);
        }

        public virtual void MinimizeWindow()
        {
            SaveState();
            OnWindowMinimize();

            mMinimized = true;

            if (mPanel != null) mPanel.Hide();                                  // 直接隐藏面板实例
            else if (!string.IsNullOrEmpty(WindowName)) UIKit.HidePanel(WindowName);
            else NotifyStateChanged();

        }

        // 从最小化恢复（已经显示时就是置顶）
        public virtual void ShowWindow()
        {
            EnsureInit();

            mMinimized = false;

            // 注意：隐藏是 UIKit 把"面板根"SetActive(false)，窗口自己的 activeSelf 仍是 true，
            // 所以这里必须看 activeInHierarchy（整条父级链）
            if (!gameObject.activeInHierarchy)
            {
                if (mPanel != null)
                {
                    mPanel.Show();                                  // 直接显示面板实例
                    if (gameObject.activeInHierarchy) return;
                }

                if (!string.IsNullOrEmpty(WindowName))
                {
                    UIKit.ShowPanel(WindowName);
                    if (gameObject.activeInHierarchy) return;

                    UIKit.OpenPanel(WindowName);                    // 兜底：UIKit 那边没这扇窗了，重新打开
                    return;
                }

                Debug.LogError("[WindowKit] 窗口 " + WindowName + " 恢复显示失败：面板实例=" + (mPanel != null) +
                               "，请检查面板根是不是被别的逻辑关掉了。", this);
                return;
            }

            WindowKit.Focus(this);
        }

        // 窗口当前状态：普通 / 最小化 / 全屏
        public WindowKitState KitState
        {
            get
            {
                if (IsFullScreen) return WindowKitState.FullScreen;
                return (mMinimized || !gameObject.activeInHierarchy) ? WindowKitState.Minimized : WindowKitState.Normal;
            }
        }

        public void BringToFront()
        {
            WindowKit.Focus(this);
        }

        protected void SyncToggle()
        {
            if (fullScreenToggle != null) fullScreenToggle.SetIsOnWithoutNotify(IsFullScreen);
        }

        #endregion

        #region 运行时窗口操作（自定义初始化 / 套件用）

        // 改当前窗口大小（窗口态；全屏时只记进窗口态，还原后生效）
        public void SetWindowSize(Vector2 size, bool keepCenter = true)
        {
            EnsureInit();

            var rect = WindowsUtility.ResizeToSize(WindowsUtility.GetRectInParent(Frame), size, keepCenter);
            WindowsUtility.SetWindowedRect(Frame, State, rect, true);
        }

        public void SetWindowRect(Rect rect)
        {
            EnsureInit();
            WindowsUtility.SetWindowedRect(Frame, State, rect, true);
        }

        public void CenterWindow()
        {
            EnsureInit();

            var desktop = WindowsUtility.GetDesktopRect(Frame, taskbarHeight);
            WindowsUtility.SetWindowedRect(Frame, State,
                WindowsUtility.CenterRect(desktop, WindowsUtility.GetRectInParent(Frame).size), true);
        }

        public void SetMinSize(Vector2 size)
        {
            minSize = size;
        }

        public void SetTaskbarHeight(float height)
        {
            taskbarHeight = height;
        }

        // 改下一次初始化的尺寸（在 OnWindowConfigure 里调最直观）
        public void SetInitWindowSize(Vector2 size, bool centerOnDesktop = true, Vector2 offset = default)
        {
            initWindowSize = size;
            initCenterOnDesktop = centerOnDesktop;
            initWindowOffset = offset;
        }

        #endregion

        #region 指针交互：拖拽 / 缩放 / 双击标题栏

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            EnsureInit();

            mPointerMode = PointerMode.None;

            if (bringToFrontOnPointerDown) WindowKit.Focus(this);
            if (eventData == null || !(Frame.parent is RectTransform space)) return;

            var onHandle = IsOnDragHandle(eventData.pointerCurrentRaycast.gameObject);

            if (onHandle && enableDrag)
            {
                if (IsFullScreen)
                {
                    if (!dragFromFullScreenRestores) return;

                    RestoreFromFullScreenByDrag(eventData, space);
                }

                mPointerMode = PointerMode.Move;
            }
            else if (enableResize && !IsFullScreen &&
                     RectTransformUtility.ScreenPointToLocalPointInRectangle(Frame, eventData.position,
                         eventData.pressEventCamera, out var localInFrame))
            {
                var edges = WindowsUtility.GetEdgeFromPoint(Frame, localInFrame, resizeBorder);
                if (edges == WindowEdge.None) return;

                mPointerMode = PointerMode.Resize;
                mResizeEdges = edges;
            }
            else
            {
                return;
            }

            CancelAnimation();

            mRectStart = WindowsUtility.GetRectInParent(Frame);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(space, eventData.position,
                    eventData.pressEventCamera, out var pointerInSpace))
            {
                mPointerMode = PointerMode.None;
                return;
            }

            mPointerStart = pointerInSpace;
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (mPointerMode == PointerMode.None || eventData == null) return;
            if (!(Frame.parent is RectTransform space)) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(space, eventData.position,
                    eventData.pressEventCamera, out var pointerInSpace)) return;

            var delta = pointerInSpace - mPointerStart;
            var rect = mPointerMode == PointerMode.Move
                ? WindowsUtility.MoveRect(Frame, mRectStart, delta, taskbarHeight, minSize)
                : WindowsUtility.ResizeRect(Frame, mRectStart, delta, mResizeEdges, taskbarHeight, minSize);

            WindowsUtility.ApplyRect(Frame, rect);
            WindowsUtility.SaveWindowedRect(Frame, State);
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            if (mPointerMode == PointerMode.None) return;

            mPointerMode = PointerMode.None;
            SaveState();
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (!doubleClickTitleBarTogglesFullScreen || eventData == null || eventData.clickCount != 2) return;
            if (!IsOnDragHandle(eventData.pointerCurrentRaycast.gameObject)) return;

            SetFullScreen(!IsFullScreen);
        }

        #endregion

        #region 内部

        private bool IsOnDragHandle(GameObject hit)
        {
            if (hit == null || dragHandle == null) return false;

            var t = hit.transform;
            return t == dragHandle || t.IsChildOf(dragHandle);
        }

        private void CancelAnimation()
        {
            if (State == null || State.Animation == null) return;

            StopCoroutine(State.Animation);
            State.Animation = null;
        }

        // 全屏状态下按住标题栏拖动：先还原成窗口态，再让窗口跟着指针走
        private void RestoreFromFullScreenByDrag(PointerEventData eventData, RectTransform space)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(space, eventData.position,
                    eventData.pressEventCamera, out var pointer)) return;

            var fullRect = WindowsUtility.GetRectInParent(Frame);
            WindowsUtility.SetFullScreenImmediate(Frame, State, false, taskbarHeight, minSize);
            SyncToggle();

            var windowRect = WindowsUtility.GetRectInParent(Frame);
            var ratio = fullRect.width > 0f ? Mathf.InverseLerp(fullRect.xMin, fullRect.xMax, pointer.x) : 0.5f;
            windowRect.x = pointer.x - windowRect.width * ratio;
            windowRect.y = pointer.y - (fullRect.yMax - pointer.y);

            WindowsUtility.ApplyRect(Frame, WindowsUtility.ClampToDesktop(Frame, windowRect, taskbarHeight, minSize));
            WindowsUtility.SaveWindowedRect(Frame, State);
        }

        protected virtual string ResolveWindowName()
        {
            if (!string.IsNullOrEmpty(panelName)) return panelName;

            var root = WindowsUtility.FindPanelRoot(Frame);
            if (root == null) return null;

            // 面板实例在 Awake 阶段还叫 "Xxx(Clone)"，UIKit 之后才改回 "Xxx"，这里兜底去掉后缀
            return root.name.Replace("(Clone)", string.Empty).Trim();
        }

        private void ResolveChrome()
        {
            var root = Frame;

            if (dragHandle == null) dragHandle = WindowsUtility.FindChildByName(root, "Bar");
            if (contentBox == null) contentBox = WindowsUtility.FindChildByName(root, "ContentBox");

            if (closeButton == null)
            {
                var exit = WindowsUtility.FindChildByName(root, "Exit");
                if (exit != null) closeButton = exit.GetComponent<Button>();
            }

            if (miniButton == null)
            {
                var mini = WindowsUtility.FindChildByName(root, "Mini");
                if (mini != null) miniButton = mini.GetComponent<Button>();
            }

            if (fullScreenToggle == null)
            {
                var full = WindowsUtility.FindChildByName(root, "Full");
                if (full != null) fullScreenToggle = full.GetComponent<Toggle>();
            }
        }

        // 自动适配：标题栏横向拉伸、内容区四边拉伸、三个按钮钉在最近的角（窗口态像素完全不变）
        private void AdaptChrome()
        {
            if (dragHandle != null) WindowsUtility.AutoFit(dragHandle, true, false);
            if (contentBox != null) WindowsUtility.AutoFit(contentBox, true, true);

            if (closeButton != null) WindowsUtility.AutoPinCorner((RectTransform)closeButton.transform);
            if (miniButton != null) WindowsUtility.AutoPinCorner((RectTransform)miniButton.transform);
            if (fullScreenToggle != null) WindowsUtility.AutoPinCorner((RectTransform)fullScreenToggle.transform);
        }

        private void BindChrome()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(CloseWindow);
                closeButton.onClick.AddListener(CloseWindow);
            }

            if (miniButton != null)
            {
                miniButton.onClick.RemoveListener(MinimizeWindow);
                miniButton.onClick.AddListener(MinimizeWindow);
            }

            if (fullScreenToggle != null)
            {
                fullScreenToggle.onValueChanged.RemoveListener(OnToggleFullScreenChanged);
                fullScreenToggle.onValueChanged.AddListener(OnToggleFullScreenChanged);
            }
        }

        private void OnToggleFullScreenChanged(bool isFull)
        {
            if (isFull == IsFullScreen) return;

            SetFullScreen(isFull);
        }

        // 内容区自动加 RectMask2D：窗口缩小时内容被裁在内容区内，不会溢出到窗口外面
        private void EnsureContentClip()
        {
            var content = Content;
            if (content == null || content.GetComponent<RectMask2D>() != null) return;

            content.gameObject.AddComponent<RectMask2D>();
        }

        // 标题栏按钮是按"离右上角的距离"钉住的，窗口太窄它们会挤出窗口 —— 把需要的尺寸并进 minSize
        private void EnsureMinSizeFitsChrome()
        {
            var need = Vector2.zero;

            need = Vector2.Max(need, ChromeNeed(closeButton != null ? closeButton.transform as RectTransform : null));
            need = Vector2.Max(need, ChromeNeed(miniButton != null ? miniButton.transform as RectTransform : null));
            need = Vector2.Max(need, ChromeNeed(fullScreenToggle != null ? fullScreenToggle.transform as RectTransform : null));

            // 标题栏只贡献高度（宽度本来就是拉伸的）
            if (dragHandle != null && dragHandle.parent is RectTransform barParent)
            {
                var bar = WindowsUtility.GetRectInParent(dragHandle);
                need.y = Mathf.Max(need.y, barParent.rect.yMax - bar.yMin);
            }

            if (need.x <= 0f && need.y <= 0f) return;

            minSize = Vector2.Max(minSize, need + new Vector2(8f, 8f));
        }

        // 这个物体从父级右上角算起，需要多少宽/高才装得下
        private static Vector2 ChromeNeed(RectTransform chrome)
        {
            if (chrome == null || !(chrome.parent is RectTransform parent)) return Vector2.zero;

            var rect = WindowsUtility.GetRectInParent(chrome);
            var parentRect = parent.rect;
            return new Vector2(parentRect.xMax - rect.xMin, parentRect.yMax - rect.yMin);
        }

        // 窗口根没有可点击的 Graphic 时，指针事件到不了这里，补一个全透明 Image
        private void EnsureRaycastTarget()
        {
            if (Frame.GetComponent<Graphic>() != null) return;

            var image = Frame.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = true;
        }

        // 用 InitWindowSize / InitCenterOnDesktop / InitWindowOffset 覆盖"设计态尺寸"
        private void ApplyInitRect()
        {
            if (initWindowSize.x <= 0f || initWindowSize.y <= 0f) return;

            var size = new Vector2(Mathf.Max(initWindowSize.x, minSize.x), Mathf.Max(initWindowSize.y, minSize.y));
            var desktop = WindowsUtility.GetDesktopRect(Frame, taskbarHeight);
            var rect = initCenterOnDesktop
                ? WindowsUtility.CenterRect(desktop, size, initWindowOffset)
                : new Rect(WindowsUtility.GetRectInParent(Frame).position + initWindowOffset, size);

            WindowsUtility.SetWindowedRect(Frame, State,
                WindowsUtility.ClampToDesktop(Frame, rect, taskbarHeight, minSize), false);
        }

        private void RestoreWindow()
        {
            if (State == null) return;

            WindowsUtility.RestoreImmediate(Frame, State, taskbarHeight, minSize);
            SyncToggle();
        }

        // 记住窗口位置/全屏状态：按窗口名缓存，关窗再开还能回到原位（同一次运行内有效）
        private void SaveState()
        {
            if (!rememberAcrossClose || State == null || string.IsNullOrEmpty(WindowName)) return;

            SavedStates[WindowName] = new SavedState
            {
                Valid = State.Initialized,
                WindowedRect = State.WindowedRect,
                IsFullScreen = State.IsFullScreen
            };
        }

        // 位置记忆优先：套用上次的位置/全屏（返回 true 表示不用再算初始尺寸和层叠）
        private bool TryApplySavedState()
        {
            if (State == null) return false;

            if (rememberAcrossClose && !string.IsNullOrEmpty(WindowName) &&
                SavedStates.TryGetValue(WindowName, out var saved) && saved.Valid)
            {
                WindowsUtility.SetWindowedRect(Frame, State, saved.WindowedRect, false);
                State.IsFullScreen = saved.IsFullScreen;
                return true;
            }

            if (startFullScreen) State.IsFullScreen = true;
            return false;
        }

        // 打开窗口时自动居中 + 层叠错位（WindowKit.CascadeOnOpen 控制）
        private void ApplyCascade()
        {
            if (!WindowKit.CascadeOnOpen) return;

            var size = WindowsUtility.GetRectInParent(Frame).size;
            var desktop = WindowsUtility.GetDesktopRect(Frame, taskbarHeight);
            var rect = WindowsUtility.CenterRect(desktop, size, WindowKit.NextCascadeOffset());

            WindowsUtility.SetWindowedRect(Frame, State,
                WindowsUtility.ClampToDesktop(Frame, rect, taskbarHeight, minSize), false);
        }

        // 摆到层叠顺序的第 index 个位置（WindowKit.CascadeAll 用）
        public void CascadeTo(int index)
        {
            EnsureInit();

            var size = WindowsUtility.GetRectInParent(Frame).size;
            var desktop = WindowsUtility.GetDesktopRect(Frame, taskbarHeight);
            var rect = WindowsUtility.CenterRect(desktop, size, WindowKit.CascadeOffsetAt(index));

            WindowsUtility.SetWindowedRect(Frame, State,
                WindowsUtility.ClampToDesktop(Frame, rect, taskbarHeight, minSize), true);
        }

        #region 给 WindowKit 的内部回调

        // 只改层级，不参与堆叠记录（避免和 WindowKit.Focus 递归）
        internal void RaisePanel()
        {
            WindowsUtility.BringToFront(Frame);
        }

        internal void NotifyFocused()
        {
            NotifyFocusChanged(true);
        }

        // focused = true 拿到焦点，false 失去焦点（被别的窗口顶下去、被最小化/关闭）
        internal void NotifyFocusChanged(bool focused)
        {
            if (focused) OnWindowFocused();

            OnWindowFocusChanged(focused);
        }

        internal void NotifyStateChanged()
        {
            OnWindowStateChanged(KitState);
            WindowKit.NotifyStateChanged(this);
        }

        #endregion

        #endregion

        #region 派生类可重写

        // 自定义初始化：派生类在这里调整初始尺寸/最小尺寸/任务栏高度等
        protected virtual void OnWindowConfigure() { }

        protected virtual void OnWindowReady() { }

        // 被置顶（点窗口、进全屏、打开时都会触发）
        protected virtual void OnWindowFocused() { }

        // 焦点变化：true 拿到焦点，false 失去焦点
        protected virtual void OnWindowFocusChanged(bool focused) { }

        // 状态变化：普通 / 最小化 / 全屏
        protected virtual void OnWindowStateChanged(WindowKitState state) { }

        protected virtual void OnWindowClose() { }

        protected virtual void OnWindowMinimize() { }

        protected virtual void OnFullScreenChanged(bool isFullScreen) { }

        #endregion
    }
}
