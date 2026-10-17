using System;
using System.IO;
using QFramework;
using UnityEngine;

namespace TapTapFirst.Audio
{
    /// <summary>
    /// 音频 Id。顺序必须与 <see cref="GameAudio"/> 里的文件名表一一对应，不要插在中间。
    /// </summary>
    public enum GameAudioId
    {
        BgmMain = 0,
        BgmBadEnding = 1,
        AmbienceCafe = 2,
        AmbienceStartup = 3,
        SfxBoot = 4,
        SfxWindowOpen = 5,
        SfxWindowClose = 6,
        SfxClick = 7,
        SfxMail = 8,
        SfxAchievement = 9,
        SfxGameOver = 10,
        SfxEndingPerfect = 11,
        SfxTravel = 12
    }

    /// <summary>
    /// 音频门面：把 Art/Sound pack 里的音频通过 AudioKit 播出来。
    /// 播放音乐走 AudioKit.PlayMusic，环境音和音效走 AudioKit.PlaySound（会跟着 Sound 音量/开关走）。
    /// </summary>
    public static class GameAudio
    {
        public const string AssetFolder = "Assets/Art/Sound pack/";

        /// <summary>与 GameAudioId 顺序严格对应。</summary>
        private static readonly string[] kFileNames =
        {
            "背景.wav",              // BgmMain
            "背景（坏结局）.wav",    // BgmBadEnding
            "场景一咖啡厅(4).wav",   // AmbienceCafe
            "开局循环.wav",          // AmbienceStartup
            "开机.wav",              // SfxBoot
            "窗口打开.wav",          // SfxWindowOpen
            "关闭窗口（4）.wav",     // SfxWindowClose
            "鼠标点击（3）.wav",     // SfxClick
            "来信（1）.wav",         // SfxMail
            "成就（任务完成）.wav",  // SfxAchievement
            "游戏结束（1）.wav",     // SfxGameOver
            "结局（完美）.wav",      // SfxEndingPerfect
            "出行（1）.wav"          // SfxTravel
        };

        public static int ClipCount => kFileNames.Length;

        /// <summary>工程内的资源路径，给编辑器自动绑定和排错用。</summary>
        public static string GetAssetPath(GameAudioId id) => AssetFolder + kFileNames[(int)id];

        /// <summary>Unity 资源名（不带扩展名），ResKit 按名字加载时用这个。</summary>
        public static string GetAssetName(GameAudioId id) => Path.GetFileNameWithoutExtension(kFileNames[(int)id]);

        private static readonly AudioClip[] sClips = new AudioClip[kFileNames.Length];

        private static AudioPlayer sAmbiencePlayer;

        private static string sAmbienceName;

        public static bool HasClip(GameAudioId id) => sClips[(int)id] != null;

        public static AudioClip GetClip(GameAudioId id) => sClips[(int)id];

        public static void SetClip(GameAudioId id, AudioClip clip)
        {
            if (clip != null) sClips[(int)id] = clip;
        }

        public static void ClearClips()
        {
            StopAmbience();
            Array.Clear(sClips, 0, sClips.Length);
        }

        #region 背景音乐

        public static void PlayBgm(GameAudioId id, bool loop = true, float volume = 1f)
        {
            if (!AudioKit.Settings.IsMusicOn.Value) return;

            var clip = sClips[(int)id];
            if (clip != null) AudioKit.PlayMusic(clip, loop, null, null, volume);
            else AudioKit.PlayMusic(GetAssetName(id), loop, null, null, volume);
        }

        public static void PlayMainBgm(float volume = 1f) => PlayBgm(GameAudioId.BgmMain, true, volume);

        public static void PlayBadEndingBgm(float volume = 1f) => PlayBgm(GameAudioId.BgmBadEnding, true, volume);

        public static void StopBgm() => AudioKit.StopMusic();

        public static void PauseBgm() => AudioKit.PauseMusic();

        public static void ResumeBgm() => AudioKit.ResumeMusic();

        #endregion

        #region 环境音

        public static void PlayAmbience(GameAudioId id, float volume = 0.6f)
        {
            StopAmbience();

            var clip = sClips[(int)id];
            sAmbiencePlayer = clip != null
                ? AudioKit.PlaySound(clip, true, null, volume)
                : AudioKit.PlaySound(GetAssetName(id), true, null, volume);
            sAmbienceName = GetAssetName(id);
        }

        public static void PlayCafeAmbience(float volume = 0.6f) => PlayAmbience(GameAudioId.AmbienceCafe, volume);

        public static void StopAmbience()
        {
            // 播放器是对象池复用的：只有还挂在本环境音上时才停，避免误停别人复用到这个播放器的声音。
            if (sAmbiencePlayer != null && !sAmbiencePlayer.IsRecycled && sAmbiencePlayer.AudioName == sAmbienceName)
            {
                sAmbiencePlayer.Stop();
            }

            sAmbiencePlayer = null;
            sAmbienceName = null;
        }

        #endregion

        #region 音效

        public static AudioPlayer PlaySfx(GameAudioId id, float volume = 1f, float pitch = 1f)
        {
            var clip = sClips[(int)id];
            return clip != null
                ? AudioKit.PlaySound(clip, false, null, volume, pitch)
                : AudioKit.PlaySound(GetAssetName(id), false, null, volume, pitch);
        }

        /// <summary>播一次音效，seconds 秒后自动停掉（只放前一段）。</summary>
        public static AudioPlayer PlaySfxFor(GameAudioId id, float seconds, float volume = 1f)
        {
            var player = PlaySfx(id, volume);
            if (player != null && seconds > 0f) GameAudioBootstrap.PlayAndStopAfter(player, seconds, GetAssetName(id));
            return player;
        }

        public static void StopAllSfx() => AudioKit.StopAllSound();

        #endregion


    }
}
