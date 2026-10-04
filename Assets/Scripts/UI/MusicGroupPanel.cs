using System;
using QFramework; // 保持原有的引用
using UnityEngine;
using UnityEngine.UI;

namespace TapTapFirst
{
    public class MusicGroupPanel : MonoBehaviour
    {
        // 原代码中的变量定义
        public Slider SoundSlider;
        public Slider MusicSlider;
        public Slider VoiceSlider;

        void Start()
        {
            // 复制原 OnInit 和 OnOpen 中的逻辑
            // 1. 初始化：读取当前的音量值，显示在滑块上
            if (SoundSlider != null)
            {
                SoundSlider.SetValueWithoutNotify(AudioKit.Settings.SoundVolume.Value);
            }
            if (MusicSlider != null)
            {
                MusicSlider.SetValueWithoutNotify(AudioKit.Settings.MusicVolume.Value);
            }
            if(VoiceSlider != null)
            {
                VoiceSlider.SetValueWithoutNotify(AudioKit.Settings.VoiceVolume.Value);
            }

            // 2. 绑定事件：拖动滑块时修改音量
            if (SoundSlider != null)
            {
                SoundSlider.onValueChanged.AddListener(OnSoundVolumeChanged);
            }
            if (MusicSlider != null)
            {
                MusicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }
            if (VoiceSlider != null)
            {
                VoiceSlider.onValueChanged.AddListener(OnVoiceVolumeChanged);
            }
        }

        //原代码中的核心方法
        private void OnSoundVolumeChanged(float value) => AudioKit.Settings.SoundVolume.Value = value;
        private void OnMusicVolumeChanged(float value) => AudioKit.Settings.MusicVolume.Value = value;
        private void OnVoiceVolumeChanged(float value) => AudioKit.Settings.VoiceVolume.Value = value;

        // 复制原 OnBeforeDestroy 中的清理逻辑
        void OnDestroy()
        {
            if (SoundSlider != null) SoundSlider.onValueChanged.RemoveListener(OnSoundVolumeChanged);
            if (MusicSlider != null) MusicSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            if (VoiceSlider != null) VoiceSlider.onValueChanged.RemoveListener(OnVoiceVolumeChanged);
        }
    }
}