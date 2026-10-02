using System.Collections;
using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TapTapFirst
{
    /// <summary>
    /// 开始界面。按钮引用由 QFramework 的 Designer 文件生成；
    /// 设置弹窗暂时由代码创建，后续可直接替换为正式 UI 预制体。
    /// </summary>
    public partial class StartPanel : ViewController
    {
        private const string GameSceneName = "GamePlay";

        private GameObject mSettingPanel;
        private GameObject mSaveSlotPanel;
        private Text mMessageText;
        private Coroutine mMessageCoroutine;
        private readonly InputField[] mSlotNameInputs = new InputField[3];
        private readonly Text[] mSlotStatusTexts = new Text[3];

        private void Start()
        {
            Btn_Start.onClick.AddListener(StartGame);
            Btn_Archive.onClick.AddListener(OpenSaveSlots);
            Btn_Setting.onClick.AddListener(OpenSettings);
            CreateMessageText();
            CreateSettingPanel();
            CreateSaveSlotPanel();
        }

        private void OnDestroy()
        {
            Btn_Start?.onClick.RemoveListener(StartGame);
            Btn_Archive?.onClick.RemoveListener(OpenSaveSlots);
            Btn_Setting?.onClick.RemoveListener(OpenSettings);
        }

        private void StartGame()
        {
            if (!Application.CanStreamedLevelBeLoaded(GameSceneName))
            {
                ShowMessage($"场景 {GameSceneName} 尚未加入 Build Settings");
                Debug.LogError($"[StartPanel] 无法加载场景：{GameSceneName}");
                return;
            }

            SceneManager.LoadScene(GameSceneName);
        }

        private void OpenSaveSlots()
        {
            RefreshSaveSlots();
            mSaveSlotPanel.SetActive(true);
            SetMainButtonsInteractable(false);
        }

        private void OpenSettings()
        {
            mSettingPanel.SetActive(true);
            SetMainButtonsInteractable(false);
        }

        private void CloseSettings()
        {
            mSettingPanel.SetActive(false);
            SetMainButtonsInteractable(true);
            PlayerPrefs.Save();
        }

        private void CloseSaveSlots()
        {
            mSaveSlotPanel.SetActive(false);
            SetMainButtonsInteractable(true);
        }

        private void SelectSaveSlot(int slotIndex)
        {
            IJsonSaveUtility saveUtility = TapTap.Interface.GetUtility<IJsonSaveUtility>();
            int arrayIndex = slotIndex - 1;
            saveUtility.SetSlotName(slotIndex, mSlotNameInputs[arrayIndex].text);

            bool isNewSlot = !saveUtility.GetSlotInfo(slotIndex).HasSave;
            saveUtility.SelectSlot(slotIndex);

            // 新槽位先写入一份初始数据，使其立刻成为可继续的存档。
            if (isNewSlot)
            {
                saveUtility.Save();
            }

            StartGame();
        }

        private void SetMainButtonsInteractable(bool interactable)
        {
            Btn_Start.interactable = interactable;
            Btn_Archive.interactable = interactable;
            Btn_Setting.interactable = interactable;
        }

        private void CreateMessageText()
        {
            mMessageText = CreateText("Message", transform, "", 20, TextAnchor.MiddleCenter);
            SetAnchors(mMessageText.rectTransform, new Vector2(0.25f, 0.05f), new Vector2(0.75f, 0.13f));
            mMessageText.gameObject.SetActive(false);
        }

        private void ShowMessage(string message)
        {
            if (mMessageCoroutine != null) StopCoroutine(mMessageCoroutine);
            mMessageCoroutine = StartCoroutine(ShowMessageRoutine(message));
        }

        private IEnumerator ShowMessageRoutine(string message)
        {
            mMessageText.text = message;
            mMessageText.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(2f);
            mMessageText.gameObject.SetActive(false);
            mMessageCoroutine = null;
        }

        private void CreateSettingPanel()
        {
            mSettingPanel = CreateUiObject("SettingPanel", transform);
            Image background = mSettingPanel.AddComponent<Image>();
            background.color = new Color(0.08f, 0.09f, 0.12f, 0.96f);
            RectTransform panelRect = mSettingPanel.GetComponent<RectTransform>();
            SetAnchors(panelRect, new Vector2(0.25f, 0.18f), new Vector2(0.75f, 0.82f));

            CreateAnchoredText("Title", panelRect, "设置", 32, new Vector2(0.15f, 0.84f), new Vector2(0.85f, 0.95f));
            CreateVolumeControl(panelRect, "Sound", "音效音量", 0.68f,
                AudioKit.Settings.SoundVolume.Value,
                value => AudioKit.Settings.SoundVolume.Value = value);
            CreateVolumeControl(panelRect, "Music", "音乐音量", 0.50f,
                AudioKit.Settings.MusicVolume.Value,
                value => AudioKit.Settings.MusicVolume.Value = value);
            CreateVolumeControl(panelRect, "Voice", "对话音量", 0.32f,
                AudioKit.Settings.VoiceVolume.Value,
                value => AudioKit.Settings.VoiceVolume.Value = value);

            Button closeButton = CreateButton(panelRect, "返回", new Vector2(0.3f, 0.06f), new Vector2(0.7f, 0.19f));
            closeButton.onClick.AddListener(CloseSettings);
            mSettingPanel.SetActive(false);
        }

        private void CreateSaveSlotPanel()
        {
            mSaveSlotPanel = CreateUiObject("SaveSlotPanel", transform);
            Image background = mSaveSlotPanel.AddComponent<Image>();
            background.color = new Color(0.08f, 0.09f, 0.12f, 0.97f);
            RectTransform panelRect = mSaveSlotPanel.GetComponent<RectTransform>();
            SetAnchors(panelRect, new Vector2(0.18f, 0.12f), new Vector2(0.82f, 0.88f));

            CreateAnchoredText("Title", panelRect, "选择存档", 32,
                new Vector2(0.15f, 0.86f), new Vector2(0.85f, 0.96f));

            for (int i = 0; i < 3; i++)
            {
                int slotIndex = i + 1;
                float centerY = 0.69f - i * 0.2f;

                mSlotStatusTexts[i] = CreateAnchoredText(
                    $"Slot{slotIndex}Status",
                    panelRect,
                    string.Empty,
                    18,
                    new Vector2(0.04f, centerY - 0.065f),
                    new Vector2(0.25f, centerY + 0.065f));

                InputField nameInput = CreateInputField(
                    $"Slot{slotIndex}Name",
                    panelRect,
                    new Vector2(0.27f, centerY - 0.06f),
                    new Vector2(0.69f, centerY + 0.06f));
                mSlotNameInputs[i] = nameInput;
                nameInput.onEndEdit.AddListener(value =>
                {
                    TapTap.Interface.GetUtility<IJsonSaveUtility>().SetSlotName(slotIndex, value);
                    RefreshSaveSlots();
                });

                Button playButton = CreateButton(
                    panelRect,
                    "开始",
                    new Vector2(0.72f, centerY - 0.06f),
                    new Vector2(0.95f, centerY + 0.06f));
                playButton.gameObject.name = $"Slot{slotIndex}PlayButton";
                playButton.onClick.AddListener(() => SelectSaveSlot(slotIndex));
            }

            Button closeButton = CreateButton(panelRect, "返回",
                new Vector2(0.34f, 0.06f), new Vector2(0.66f, 0.17f));
            closeButton.onClick.AddListener(CloseSaveSlots);
            mSaveSlotPanel.SetActive(false);
        }

        private void RefreshSaveSlots()
        {
            IJsonSaveUtility saveUtility = TapTap.Interface.GetUtility<IJsonSaveUtility>();
            for (int i = 0; i < 3; i++)
            {
                SaveSlotInfo info = saveUtility.GetSlotInfo(i + 1);
                mSlotNameInputs[i].SetTextWithoutNotify(info.DisplayName);
                mSlotStatusTexts[i].text = info.HasSave
                    ? $"槽位 {i + 1}\n{info.LastSaveTime:MM-dd HH:mm}"
                    : $"槽位 {i + 1}\n空存档";
            }
        }

        private static void CreateVolumeControl(
            RectTransform parent,
            string controlName,
            string labelText,
            float centerY,
            float initialValue,
            UnityEngine.Events.UnityAction<float> onValueChanged)
        {
            CreateAnchoredText(
                controlName + "Label",
                parent,
                labelText,
                22,
                new Vector2(0.08f, centerY - 0.055f),
                new Vector2(0.38f, centerY + 0.055f));

            Slider slider = CreateSlider(
                controlName + "Slider",
                parent,
                new Vector2(0.42f, centerY - 0.04f),
                new Vector2(0.88f, centerY + 0.04f));
            slider.SetValueWithoutNotify(initialValue);
            slider.onValueChanged.AddListener(onValueChanged);
        }

        private static Slider CreateSlider(string name, RectTransform parent, Vector2 min, Vector2 max)
        {
            GameObject root = CreateUiObject(name, parent);
            SetAnchors(root.GetComponent<RectTransform>(), min, max);
            Image background = root.AddComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.22f);

            GameObject fill = CreateUiObject("Fill", root.transform);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.25f, 0.7f, 1f, 1f);
            SetAnchors(fill.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

            GameObject handle = CreateUiObject("Handle", root.transform);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(22f, 0f);

            Slider slider = root.AddComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            return slider;
        }

        private static Button CreateButton(RectTransform parent, string title, Vector2 min, Vector2 max)
        {
            GameObject root = CreateUiObject(title + "Button", parent);
            SetAnchors(root.GetComponent<RectTransform>(), min, max);
            Image image = root.AddComponent<Image>();
            image.color = new Color(0.18f, 0.45f, 0.68f, 1f);
            Button button = root.AddComponent<Button>();
            button.targetGraphic = image;
            CreateText("Text", root.transform, title, 22, TextAnchor.MiddleCenter);
            return button;
        }

        private static InputField CreateInputField(string name, RectTransform parent, Vector2 min, Vector2 max)
        {
            GameObject root = CreateUiObject(name, parent);
            SetAnchors(root.GetComponent<RectTransform>(), min, max);
            Image background = root.AddComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.16f);

            Text valueText = CreateText("Text", root.transform, string.Empty, 20, TextAnchor.MiddleLeft);
            valueText.rectTransform.offsetMin = new Vector2(12f, 3f);
            valueText.rectTransform.offsetMax = new Vector2(-12f, -3f);

            Text placeholder = CreateText("Placeholder", root.transform, "输入存档名称", 18, TextAnchor.MiddleLeft);
            placeholder.color = new Color(1f, 1f, 1f, 0.4f);
            placeholder.rectTransform.offsetMin = new Vector2(12f, 3f);
            placeholder.rectTransform.offsetMax = new Vector2(-12f, -3f);

            InputField input = root.AddComponent<InputField>();
            input.targetGraphic = background;
            input.textComponent = valueText;
            input.placeholder = placeholder;
            input.characterLimit = 16;
            return input;
        }

        private static Text CreateAnchoredText(string name, RectTransform parent, string content, int size, Vector2 min, Vector2 max)
        {
            Text text = CreateText(name, parent, content, size, TextAnchor.MiddleCenter);
            SetAnchors(text.rectTransform, min, max);
            return text;
        }

        private static Text CreateText(string name, Transform parent, string content, int size, TextAnchor alignment)
        {
            GameObject root = CreateUiObject(name, parent);
            Text text = root.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            SetAnchors(text.rectTransform, Vector2.zero, Vector2.one);
            return text;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            return root;
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
