using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TapTapFirst
{
    /// <summary>
    /// 开始界面控制器。弹窗均从 Resources/UI 下的预制体加载，运行时不创建 UI 控件。
    /// </summary>
    public partial class StartPanel : ViewController
    {
        private const string GameSceneName = "GamePlay";
        private const string SettingsPrefabPath = "UI/SettingsPanel";
        private const string SaveSlotsPrefabPath = "UI/SaveSlotPanel";

        private SettingsPanelView mSettingsView;
        private SaveSlotPanelView mSaveSlotView;

        private void Start()
        {
            Btn_Start.onClick.AddListener(StartGame);
            Btn_Archive.onClick.AddListener(OpenSaveSlots);
            Btn_Setting.onClick.AddListener(OpenSettings);

            mSettingsView = LoadPanel<SettingsPanelView>(SettingsPrefabPath);
            mSaveSlotView = LoadPanel<SaveSlotPanelView>(SaveSlotsPrefabPath);
            BindSettingsPanel();
            BindSaveSlotPanel();
        }

        private void OnDestroy()
        {
            Btn_Start?.onClick.RemoveListener(StartGame);
            Btn_Archive?.onClick.RemoveListener(OpenSaveSlots);
            Btn_Setting?.onClick.RemoveListener(OpenSettings);
        }

        private T LoadPanel<T>(string resourcesPath) where T : Component
        {
            GameObject prefab = Resources.Load<GameObject>(resourcesPath);
            if (prefab == null)
            {
                Debug.LogError($"[StartPanel] 找不到 UI 预制体：Resources/{resourcesPath}");
                return null;
            }

            GameObject instance = Instantiate(prefab, transform, false);
            instance.SetActive(false);
            return instance.GetComponent<T>();
        }

        private void BindSettingsPanel()
        {
            if (mSettingsView == null) return;

            mSettingsView.SoundSlider.SetValueWithoutNotify(AudioKit.Settings.SoundVolume.Value);
            mSettingsView.MusicSlider.SetValueWithoutNotify(AudioKit.Settings.MusicVolume.Value);
            mSettingsView.VoiceSlider.SetValueWithoutNotify(AudioKit.Settings.VoiceVolume.Value);

            mSettingsView.SoundSlider.onValueChanged.AddListener(
                value => AudioKit.Settings.SoundVolume.Value = value);
            mSettingsView.MusicSlider.onValueChanged.AddListener(
                value => AudioKit.Settings.MusicVolume.Value = value);
            mSettingsView.VoiceSlider.onValueChanged.AddListener(
                value => AudioKit.Settings.VoiceVolume.Value = value);
            mSettingsView.CloseButton.onClick.AddListener(CloseSettings);
        }

        private void BindSaveSlotPanel()
        {
            if (mSaveSlotView == null) return;

            for (int i = 0; i < SaveSlotPanelView.SlotCount; i++)
            {
                int slotIndex = i + 1;
                mSaveSlotView.NameInputs[i].onEndEdit.AddListener(value =>
                {
                    TapTap.Interface.GetUtility<IJsonSaveUtility>().SetSlotName(slotIndex, value);
                    RefreshSaveSlots();
                });
                mSaveSlotView.PlayButtons[i].onClick.AddListener(() => SelectSaveSlot(slotIndex));
            }

            mSaveSlotView.CloseButton.onClick.AddListener(CloseSaveSlots);
        }

        private void StartGame()
        {
            if (!Application.CanStreamedLevelBeLoaded(GameSceneName))
            {
                Debug.LogError($"[StartPanel] 场景 {GameSceneName} 尚未加入 Build Settings");
                return;
            }

            SceneManager.LoadScene(GameSceneName);
        }

        private void OpenSettings()
        {
            if (mSettingsView == null) return;

            mSettingsView.SoundSlider.SetValueWithoutNotify(AudioKit.Settings.SoundVolume.Value);
            mSettingsView.MusicSlider.SetValueWithoutNotify(AudioKit.Settings.MusicVolume.Value);
            mSettingsView.VoiceSlider.SetValueWithoutNotify(AudioKit.Settings.VoiceVolume.Value);
            mSettingsView.gameObject.SetActive(true);
            SetMainButtonsInteractable(false);
        }

        private void CloseSettings()
        {
            mSettingsView.gameObject.SetActive(false);
            SetMainButtonsInteractable(true);
            PlayerPrefs.Save();
        }

        private void OpenSaveSlots()
        {
            if (mSaveSlotView == null) return;

            RefreshSaveSlots();
            mSaveSlotView.gameObject.SetActive(true);
            SetMainButtonsInteractable(false);
        }

        private void CloseSaveSlots()
        {
            mSaveSlotView.gameObject.SetActive(false);
            SetMainButtonsInteractable(true);
        }

        private void SelectSaveSlot(int slotIndex)
        {
            IJsonSaveUtility saveUtility = TapTap.Interface.GetUtility<IJsonSaveUtility>();
            int arrayIndex = slotIndex - 1;
            saveUtility.SetSlotName(slotIndex, mSaveSlotView.NameInputs[arrayIndex].text);

            bool isNewSlot = !saveUtility.GetSlotInfo(slotIndex).HasSave;
            saveUtility.SelectSlot(slotIndex);
            if (isNewSlot) saveUtility.Save();

            StartGame();
        }

        private void RefreshSaveSlots()
        {
            IJsonSaveUtility saveUtility = TapTap.Interface.GetUtility<IJsonSaveUtility>();
            for (int i = 0; i < SaveSlotPanelView.SlotCount; i++)
            {
                SaveSlotInfo info = saveUtility.GetSlotInfo(i + 1);
                mSaveSlotView.NameInputs[i].SetTextWithoutNotify(info.DisplayName);
                mSaveSlotView.StatusTexts[i].text = info.HasSave
                    ? $"槽位 {i + 1}\n{info.LastSaveTime:MM-dd HH:mm}"
                    : $"槽位 {i + 1}\n空存档";
            }
        }

        private void SetMainButtonsInteractable(bool interactable)
        {
            Btn_Start.interactable = interactable;
            Btn_Archive.interactable = interactable;
            Btn_Setting.interactable = interactable;
        }
    }
}
