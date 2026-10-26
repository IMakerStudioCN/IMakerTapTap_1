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
        private const string SaveSlotsAssetBundleName = "saveslotpanel_prefab";
        private const string SaveSlotsPrefabName = "SaveSlotPanel";

        private ResLoader mResLoader;
        private bool mInitialized;

        // 加载场景用的 ResLoader 必须活到进程结束。
        // 一旦回收，ResKit 会对场景的依赖包执行 AssetBundle.Unload(true)，
        // 场景里直接引用到的跨包资产（TaskSingle / DialogSO / 材质…）会被销毁成 Missing，
        // 于是 TaskControl.config、DialogConfig.DialogConfigs 全变空 —— 任务、对话在真机包里全失效。
        // （模拟模式下资源来自 AssetDatabase，不会复现这个问题，所以容易被漏掉。）
        private static ResLoader sSceneResLoader;

        private static ResLoader SceneResLoader
        {
            get
            {
                if (sSceneResLoader == null)
                {
                    sSceneResLoader = ResLoader.Allocate();
                }

                return sSceneResLoader;
            }
        }

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
            mResLoader = SceneResLoader;
            Btn_Start.onClick.AddListener(StartGame);
            Btn_Archive.onClick.AddListener(OpenSaveSlots);
            Btn_Setting.onClick.AddListener(OpenSettings);
            Btn_Exit.onClick.AddListener(GameApplication.Quit);
        }

        private void StartGame()
        {
            IJsonSaveUtility saveUtility = TapTap.Interface.GetUtility<IJsonSaveUtility>();
            if (!saveUtility.TrySelectLatestPlayedSlot())
            {
                Debug.Log("[StartPanel] 当前没有可继续的存档，打开存档选择界面");
                OpenSaveSlots();
                return;
            }

            LoadGameScene();
        }

        private void OpenSettings()
        {
            SetMainButtonsInteractable(false);
            if (!GlobalSettingsUI.Open(() => SetMainButtonsInteractable(true)))
            {
                SetMainButtonsInteractable(true);
            }
        }

        private void OpenSaveSlots()
        {
            SetMainButtonsInteractable(false);
            SaveSlotPanelView panel = UIKit.OpenPanel<SaveSlotPanelView>(
                UILevel.PopUI,
                new SaveSlotPanelData(LoadGameScene),
                assetBundleName: SaveSlotsAssetBundleName,
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
            IGlobalManagerSystem globalManagerSystem = TapTap.Interface.GetSystem<IGlobalManagerSystem>();

            if (globalManagerSystem != null)
            {
                globalManagerSystem.ResetEndState();
            }

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
            Btn_Exit.interactable = interactable;
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
                Btn_Exit?.onClick.RemoveListener(GameApplication.Quit);
                // 这里不能 Recycle2Cache()：回收会连带 AssetBundle.Unload(true) 掉场景的依赖包，
                // 场景里的跨包引用会立刻变成 Missing。loader 由 sSceneResLoader 持有，随进程存活。
                mResLoader = null;
            }

            base.OnDestroy();
        }
    }
}
