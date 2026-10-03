using System;
using QFramework;
using UnityEngine.UI;

namespace TapTapFirst
{
    public sealed class SaveSlotPanelData : UIPanelData
    {
        public Action OnSlotSelected { get; }

        public SaveSlotPanelData(Action onSlotSelected)
        {
            OnSlotSelected = onSlotSelected;
        }
    }

    public sealed class SaveSlotPanelView : UIPanel
    {
        public const int SlotCount = 3;

        public Text[] StatusTexts = new Text[SlotCount];
        public InputField[] NameInputs = new InputField[SlotCount];
        public Button[] PlayButtons = new Button[SlotCount];
        public Button CloseButton;

        private SaveSlotPanelData mData;

        protected override void OnInit(IUIData uiData = null)
        {
            CloseButton.onClick.AddListener(CloseSelf);

            for (int i = 0; i < SlotCount; i++)
            {
                int slotIndex = i + 1;
                NameInputs[i].onEndEdit.AddListener(value => RenameSlot(slotIndex, value));
                PlayButtons[i].onClick.AddListener(() => SelectSaveSlot(slotIndex));
            }
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            mData = uiData as SaveSlotPanelData;
            RefreshSaveSlots();
        }

        private static void RenameSlot(int slotIndex, string value)
        {
            TapTap.Interface.GetUtility<IJsonSaveUtility>().SetSlotName(slotIndex, value);
        }

        private void RefreshSaveSlots()
        {
            IJsonSaveUtility saveUtility = TapTap.Interface.GetUtility<IJsonSaveUtility>();
            for (int i = 0; i < SlotCount; i++)
            {
                SaveSlotInfo info = saveUtility.GetSlotInfo(i + 1);
                NameInputs[i].SetTextWithoutNotify(info.DisplayName);
                StatusTexts[i].text = info.HasSave
                    ? $"槽位 {i + 1}\n{info.LastSaveTime:MM-dd HH:mm}"
                    : $"槽位 {i + 1}\n空存档";
            }
        }

        private void SelectSaveSlot(int slotIndex)
        {
            IJsonSaveUtility saveUtility = TapTap.Interface.GetUtility<IJsonSaveUtility>();
            int arrayIndex = slotIndex - 1;
            saveUtility.SetSlotName(slotIndex, NameInputs[arrayIndex].text);

            bool isNewSlot = !saveUtility.GetSlotInfo(slotIndex).HasSave;
            saveUtility.SelectSlot(slotIndex);
            if (isNewSlot) saveUtility.Save();

            Action onSlotSelected = mData?.OnSlotSelected;
            CloseSelf();
            onSlotSelected?.Invoke();
        }

        protected override void OnClose()
        {
            mData = null;
        }
    }
}
