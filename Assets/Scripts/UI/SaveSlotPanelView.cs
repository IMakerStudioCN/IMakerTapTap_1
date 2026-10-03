using UnityEngine;
using UnityEngine.UI;

namespace TapTapFirst
{
    public sealed class SaveSlotPanelView : MonoBehaviour
    {
        public const int SlotCount = 3;

        public Text[] StatusTexts = new Text[SlotCount];
        public InputField[] NameInputs = new InputField[SlotCount];
        public Button[] PlayButtons = new Button[SlotCount];
        public Button CloseButton;
    }
}
