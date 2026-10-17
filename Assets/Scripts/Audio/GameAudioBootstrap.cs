using System;
using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TapTapFirst.Audio
{
    /// <summary>
    /// 音频接线：绑定 Sound pack 的 clip，并按场景切换音频。
    ///   启动游戏        → 开机.wav 只放前 4 秒（整个游戏一次）
    ///   Start 场景      → 开局循环.wav（循环）
    ///   GamePlay 场景   → 背景.wav（主 BGM）
    /// 场景里不放也会自动创建。
    /// </summary>
    [DisallowMultipleComponent]
    public class GameAudioBootstrap : MonoBehaviour
    {
        private const string StartSceneName = "Start";
        private const string GameSceneName = "GamePlay";

        [Serializable]
        public class ClipEntry
        {
            public GameAudioId Id;
            public AudioClip Clip;
        }

        [SerializeField] private List<ClipEntry> mClips = new List<ClipEntry>();

        [SerializeField] private bool mAutoPlayMainBgm = true;

        [SerializeField] private bool mDontDestroyOnLoad = true;

        private static GameAudioBootstrap sInstance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            if (sInstance != null) return;

            var go = new GameObject("[GameAudioBootstrap]");
            go.AddComponent<GameAudioBootstrap>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (sInstance != null && sInstance != this)
            {
                Destroy(this);
                return;
            }

            sInstance = this;

            BindClips();

            if (mDontDestroyOnLoad && transform.parent == null) DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplySceneAudio(SceneManager.GetActiveScene().name);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (sInstance == this)
            {
                sInstance = null;
                GameAudio.ClearClips();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplySceneAudio(scene.name);

        private void ApplySceneAudio(string sceneName)
        {
            // 开机音由 BootIntro 统一播（动画和声音共用一个时间轴起点）
            if (sceneName == StartSceneName)
            {
                GameAudio.StopBgm();
                GameAudio.PlayAmbience(GameAudioId.AmbienceStartup);
                return;
            }

            GameAudio.StopAmbience();

            if (sceneName == GameSceneName && mAutoPlayMainBgm) GameAudio.PlayMainBgm();
        }

        /// <summary>播放后 seconds 秒自动停（只放前一段）。</summary>
        internal static void PlayAndStopAfter(AudioPlayer player, float seconds, string audioName)
        {
            if (sInstance == null) return;

            sInstance.StartCoroutine(StopAfter(player, seconds, audioName));
        }

        private static IEnumerator StopAfter(AudioPlayer player, float seconds, string audioName)
        {
            yield return new WaitForSeconds(seconds);

            if (player != null && !player.IsRecycled && player.AudioName == audioName) player.Stop();
        }

        private void BindClips()
        {
            var total = Enum.GetValues(typeof(GameAudioId)).Length;

            for (var i = 0; i < total; i++)
            {
                var id = (GameAudioId)i;
                var clip = FindClip(id);
                if (clip == null) clip = LoadClipFromAssetPath(id);
                if (clip != null) GameAudio.SetClip(id, clip);
            }
        }

        private AudioClip FindClip(GameAudioId id)
        {
            if (mClips == null) return null;

            for (var i = 0; i < mClips.Count; i++)
            {
                var entry = mClips[i];
                if (entry != null && entry.Id == id && entry.Clip != null) return entry.Clip;
            }

            return null;
        }

        private static AudioClip LoadClipFromAssetPath(GameAudioId id)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(GameAudio.GetAssetPath(id));
#else
            return null;
#endif
        }

#if UNITY_EDITOR
        private void Reset()
        {
            FillClips();
        }

        private void OnValidate()
        {
            if (mClips == null) mClips = new List<ClipEntry>();
            if (mClips.Count == 0) FillClips();
        }

        private void FillClips()
        {
            mClips = new List<ClipEntry>();

            var total = Enum.GetValues(typeof(GameAudioId)).Length;
            for (var i = 0; i < total; i++)
            {
                var id = (GameAudioId)i;
                mClips.Add(new ClipEntry
                {
                    Id = id,
                    Clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(GameAudio.GetAssetPath(id))
                });
            }
        }
#endif
    }
}
