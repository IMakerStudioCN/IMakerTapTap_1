using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace TapTapFirst
{
    public class ComputerMenuPanelData : UIPanelData
    {
    }

    public partial class ComputerMenuPanel : UIPanel
    {
        // 主菜单场景名，需与 Build Settings 中的场景名保持一致
        private const string MainMenuSceneName = "Start";

        // 这三个按钮由你在 Unity 的 Inspector 面板中手动拖拽绑定
        public Button BackToMainMenuButton; // 返回主菜单
        public Button QuitGameButton;       // 退出游戏
        public Button CloseButton;          // 关闭此面板

        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as ComputerMenuPanelData ?? new ComputerMenuPanelData();

            // 显式判空代替 ?.，绑定缺失时能在 Console 立刻看到明确报错
            if (BackToMainMenuButton != null)
                BackToMainMenuButton.onClick.AddListener(OnClickBackToMainMenu);
            else
                Debug.LogError("[ComputerMenuPanel] BackToMainMenuButton 未在 Inspector 中绑定，返回主菜单将无效");

            if (QuitGameButton != null)
                QuitGameButton.onClick.AddListener(OnClickQuitGame);
            else
                Debug.LogError("[ComputerMenuPanel] QuitGameButton 未在 Inspector 中绑定，退出游戏将无效");

            if (CloseButton != null)
                CloseButton.onClick.AddListener(OnClickCloseSelf);
            else
                Debug.LogError("[ComputerMenuPanel] CloseButton 未在 Inspector 中绑定，关闭面板将无效");
        }

        // 1. 返回主菜单（加载 Start 场景）
        private void OnClickBackToMainMenu()
        {
            if (!Application.CanStreamedLevelBeLoaded(MainMenuSceneName))
            {
                Debug.LogError($"[ComputerMenuPanel] 场景 {MainMenuSceneName} 尚未加入 Build Settings");
                return;
            }

            // 切换场景前先关闭所有面板（包括自己），清空 UIKit 缓存，防止残留引用崩溃
            UIKit.CloseAllPanel();

            // 用独立加载器加载主菜单场景（当前面板即将被销毁，不能再依赖成员加载器）
            ResLoader loader = ResLoader.Allocate();
            loader.LoadSceneAsync(MainMenuSceneName, onStartLoading: operation =>
            {
                Debug.Log($"[ComputerMenuPanel] ResKit 开始异步加载主菜单场景：{MainMenuSceneName}");
            });
        }

        // 2. 退出游戏
        private void OnClickQuitGame()
        {
            GameApplication.Quit();
        }

        // 3. 关闭当前面板
        private void OnClickCloseSelf()
        {
            //先调用CloseSelf触发正常的生命周期
            CloseSelf();

            // 同步桌面的 Menu 开关状态，避免下次要点两下
            ComputerDisplay display = UIKit.GetPanel<ComputerDisplay>();
            if (display != null && display.Menu != null)
            {
                display.Menu.SetIsOnWithoutNotify(false);
            }
        }

        protected override void OnOpen(IUIData uiData = null) { }
        protected override void OnShow() { }
        protected override void OnHide() { }
        protected override void OnClose() { }

        protected override void OnBeforeDestroy()
        {
            // 移除监听，防止内存泄漏
            if (BackToMainMenuButton != null) BackToMainMenuButton.onClick.RemoveListener(OnClickBackToMainMenu);
            if (QuitGameButton != null) QuitGameButton.onClick.RemoveListener(OnClickQuitGame);
            if (CloseButton != null) CloseButton.onClick.RemoveListener(OnClickCloseSelf);

            // 必须调用基类：会触发 Designer 里的 ClearUIComponents() 清空 Setting/Exit/mData
            base.OnBeforeDestroy();

            // 顺手把自己这三个引用也置空，避免残留
            BackToMainMenuButton = null;
            QuitGameButton = null;
            CloseButton = null;
        }
    }
}