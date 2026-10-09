using System;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace TapTapFirst
{
    /// <summary>
    /// 全局退出确认入口。任意界面都可以调用 GlobalExitConfirmUI.Open() 弹出退出确认面板。
    /// </summary>
    public static class GlobalExitConfirmUI
    {
        // ResKit 按 AB 表里的资源名查（= prefab 文件名，小写比较），只能填资源名，不能带路径
        private const string PrefabName = "ExitConfirmPanel";

        public static bool Open(Action onClosed = null)
        {
            ResKit.Init();
            if (!(UIKit.Config.PanelLoaderPool is ResKitPanelLoaderPool))
            {
                UIKit.Config.PanelLoaderPool = new ResKitPanelLoaderPool();
            }

            ExitConfirmPanelView panel = UIKit.OpenPanel<ExitConfirmPanelView>(
                UILevel.PopUI,
                prefabName: PrefabName);

            if (panel == null)
            {
                Debug.LogError("[GlobalExitConfirmUI] 退出确认面板加载失败");
                return false;
            }

            if (onClosed != null) panel.OnClosed(onClosed);
            return true;
        }
    }

    public sealed class ExitConfirmPanelView : UIPanel
    {
        // 主菜单场景名，需与 Build Settings 中的场景名保持一致
        private const string MainMenuSceneName = "Start";

        public Button QuitGameButton;   // 退出游戏
        public Button BackToMenuButton; // 退出游戏到主菜单
        public Button CancelButton;     // 取消

        private ResLoader mResLoader;

        protected override void OnInit(IUIData uiData = null)
        {
            QuitGameButton.onClick.AddListener(QuitGame);
            BackToMenuButton.onClick.AddListener(BackToMainMenu);
            CancelButton.onClick.AddListener(CloseSelf);
        }

        private void QuitGame()
        {
            GameApplication.Quit();
        }

        private void BackToMainMenu()
        {

            if (!Application.CanStreamedLevelBeLoaded(MainMenuSceneName))
            {
                Debug.LogError($"[ExitConfirmPanelView] 场景 {MainMenuSceneName} 尚未加入 Build Settings");
                return;
            }

            if (mResLoader == null)
            {
                mResLoader = ResLoader.Allocate();
            }

            mResLoader.LoadSceneAsync(MainMenuSceneName, onStartLoading: operation =>
            {
                Debug.Log($"[ExitConfirmPanelView] ResKit 开始异步加载主菜单场景：{MainMenuSceneName}");
            });
        }

        protected override void OnClose()
        {
        }

        protected override void OnBeforeDestroy()
        {
            QuitGameButton?.onClick.RemoveListener(QuitGame);
            BackToMenuButton?.onClick.RemoveListener(BackToMainMenu);
            CancelButton?.onClick.RemoveListener(CloseSelf);
            mResLoader?.Recycle2Cache();
            mResLoader = null;
            base.OnBeforeDestroy();
        }
    }
}