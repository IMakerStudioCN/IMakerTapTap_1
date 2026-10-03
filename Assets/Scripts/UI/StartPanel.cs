using QFramework;
using UnityEngine;

namespace TapTapFirst
{
    public class StartPanelData : UIPanelData
    {
    }

    /// <summary>
    /// 开始界面只负责入口。设置和存档由 UIKit 作为独立面板管理，场景由 ResKit 加载。
    /// </summary>
    public partial class StartPanel : UIPanel
    {
        private const string GameSceneName = "GamePlay";
        private const string SettingsPrefabName = "Resources/UI/SettingsPanel";
        private const string SaveSlotsPrefabName = "Resources/UI/SaveSlotPanel";

        private ResLoader mResLoader;
        private bool mInitialized;

        private void Start()
        {
            // StartPanel 目前直接放在 Start 场景中，因此在 Unity Start 生命周期中初始化。
            InitializeStartPanel();
        }

        private void InitializeStartPanel()
        {
            if (mInitialized) return;
            mInitialized = true;

            ResKit.Init();
            UIKit.Config.PanelLoaderPool = new ResKitPanelLoaderPool();
            mResLoader = ResLoader.Allocate();
            Btn_Start.onClick.AddListener(StartGame);
            Btn_Archive.onClick.AddListener(OpenSaveSlots);
            Btn_Setting.onClick.AddListener(OpenSettings);
        }

        private void StartGame()
        {
            LoadGameScene();
        }

        private void OpenSettings()
        {
            SetMainButtonsInteractable(false);
            SettingsPanelView panel = UIKit.OpenPanel<SettingsPanelView>(
                UILevel.PopUI,
                prefabName: SettingsPrefabName);

            if (panel == null)
            {
                Debug.LogError("[StartPanel] 设置面板加载失败");
                SetMainButtonsInteractable(true);
                return;
            }

            panel.OnClosed(() => SetMainButtonsInteractable(true));
        }

        private void OpenSaveSlots()
        {
            SetMainButtonsInteractable(false);
            SaveSlotPanelView panel = UIKit.OpenPanel<SaveSlotPanelView>(
                UILevel.PopUI,
                new SaveSlotPanelData(LoadGameScene),
                prefabName: SaveSlotsPrefabName);

            if (panel == null)
            {
                Debug.LogError("[StartPanel] 存档面板加载失败");
                SetMainButtonsInteractable(true);
                return;
            }

            panel.OnClosed(() => SetMainButtonsInteractable(true));
        }

        private void LoadGameScene()
        {
            if (!Application.CanStreamedLevelBeLoaded(GameSceneName))
            {
                Debug.LogError($"[StartPanel] 场景 {GameSceneName} 尚未加入 Build Settings");
                return;
            }

            SetMainButtonsInteractable(false);
            mResLoader.LoadSceneAsync(GameSceneName, onStartLoading: operation =>
            {
                Debug.Log($"[StartPanel] ResKit 开始异步加载场景：{GameSceneName}");
            });
        }

        private void SetMainButtonsInteractable(bool interactable)
        {
            Btn_Start.interactable = interactable;
            Btn_Archive.interactable = interactable;
            Btn_Setting.interactable = interactable;
        }

        protected override void OnClose()
        {
        }

        protected override void OnDestroy()
        {
            if (mInitialized)
            {
                Btn_Start?.onClick.RemoveListener(StartGame);
                Btn_Archive?.onClick.RemoveListener(OpenSaveSlots);
                Btn_Setting?.onClick.RemoveListener(OpenSettings);
                mResLoader?.Recycle2Cache();
                mResLoader = null;
            }

            base.OnDestroy();
        }
    }
}
