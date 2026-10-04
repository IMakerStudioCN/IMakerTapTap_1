using System;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace TapTapFirst
{
    /// <summary>
    /// 全局设置入口。任意界面都可以调用 GlobalSettingsUI.Open() 打开同一个设置面板。
    /// </summary>
    public static class GlobalSettingsUI
    {
        // ResKit 按 AB 表里的资源名查（= prefab 文件名，小写比较），只能填资源名，不能带路径
        private const string PrefabName = "SettingsPanel";

        public static bool Open(Action onClosed = null)
        {
            ResKit.Init();
            if (!(UIKit.Config.PanelLoaderPool is ResKitPanelLoaderPool))
            {
                UIKit.Config.PanelLoaderPool = new ResKitPanelLoaderPool();
            }

            SettingsPanelView panel = UIKit.OpenPanel<SettingsPanelView>(
                UILevel.PopUI,
                prefabName: PrefabName);

            if (panel == null)
            {
                Debug.LogError("[GlobalSettingsUI] 设置面板加载失败");
                return false;
            }

            if (onClosed != null) panel.OnClosed(onClosed);
            return true;
        }
    }

    public static class GameApplication
    {
        public static void Quit()
        {
            PlayerPrefs.Save();

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }

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
            GlobalExitConfirmUI.Open();
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
