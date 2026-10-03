using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace TapTapFirst
{
    public sealed class SettingsPanelView : UIPanel
    {
        public Slider SoundSlider;
        public Slider MusicSlider;
        public Slider VoiceSlider;
        public Button CloseButton;
        public Button ExitButton;

        protected override void OnInit(IUIData uiData = null)
        {
            SoundSlider.onValueChanged.AddListener(OnSoundVolumeChanged);
            MusicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            VoiceSlider.onValueChanged.AddListener(OnVoiceVolumeChanged);
            CloseButton.onClick.AddListener(CloseSelf);
            ExitButton.onClick.AddListener(ExitGame);
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            SoundSlider.SetValueWithoutNotify(AudioKit.Settings.SoundVolume.Value);
            MusicSlider.SetValueWithoutNotify(AudioKit.Settings.MusicVolume.Value);
            VoiceSlider.SetValueWithoutNotify(AudioKit.Settings.VoiceVolume.Value);
        }

        private static void OnSoundVolumeChanged(float value)
        {
            AudioKit.Settings.SoundVolume.Value = value;
        }

        private static void OnMusicVolumeChanged(float value)
        {
            AudioKit.Settings.MusicVolume.Value = value;
        }

        private static void OnVoiceVolumeChanged(float value)
        {
            AudioKit.Settings.VoiceVolume.Value = value;
        }

        private void ExitGame()
        {
            PlayerPrefs.Save();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        protected override void OnClose()
        {
            PlayerPrefs.Save();
        }

        protected override void OnBeforeDestroy()
        {
            SoundSlider?.onValueChanged.RemoveListener(OnSoundVolumeChanged);
            MusicSlider?.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            VoiceSlider?.onValueChanged.RemoveListener(OnVoiceVolumeChanged);
            CloseButton?.onClick.RemoveListener(CloseSelf);
            ExitButton?.onClick.RemoveListener(ExitGame);
            base.OnBeforeDestroy();
        }
    }
}
