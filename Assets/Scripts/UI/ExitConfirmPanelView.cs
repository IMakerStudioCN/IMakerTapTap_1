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
        private const string AssetBundleName = "exitconfirmpanel_prefab";
        private const string PrefabName = "ExitConfirmPanel";

        public static bool Open(Action onClosed = null)
        {
            ExitConfirmPanelView panel = UIKit.OpenPanel<ExitConfirmPanelView>(
                UILevel.PopUI,
                assetBundleName: AssetBundleName,
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

            // 先关闭所有面板（包括自己），清空 UIKit 缓存，避免场景卸载后残留“已销毁面板”引用
            UIKit.CloseAllPanel();

            // 用独立加载器加载主菜单场景：面板自身已随 CloseAllPanel 被关闭，不能再依赖成员加载器
            ResLoader loader = ResLoader.Allocate();
            loader.LoadSceneAsync(MainMenuSceneName, onStartLoading: operation =>
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
            base.OnBeforeDestroy();
        }
    }
}
