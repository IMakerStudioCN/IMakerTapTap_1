using QFramework;
using UnityEngine;

namespace TapTapFirst.Audio
{
    /// <summary>
    /// 纯新增的音频挂点：只听工程里已有的事件，不改任何既有代码。
    ///   WindowKit.OnOpened        → 窗口打开音（Open / OpenWindows / OpenPanel 首次打开都走这里）
    ///   SendEmailEvent（带 Task）→ 来信音
    ///   OnTaskEnd 且未过期        → 任务完成音
    /// </summary>
    public class GameAudioAutoHook : MonoBehaviour, IController
    {
        private static GameAudioAutoHook sInstance;

        public IArchitecture GetArchitecture() => TapTap.Interface;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            if (sInstance != null) return;

            var go = new GameObject("[GameAudioAutoHook]");
            go.AddComponent<GameAudioAutoHook>();
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
        }

        private void Start()
        {
            // 首次打开窗口不会经过 WindowsBasic.ShowWindow，所以这里听 WindowKit 的 OnOpened（每个窗口每次打开只触发一次）
            WindowKit.OnOpened.Register(window => GameAudio.PlaySfx(GameAudioId.SfxWindowOpen))
                .UnRegisterWhenGameObjectDestroyed(gameObject);

            this.RegisterEvent<SendEmailEvent>(e =>
            {
                if (e.Task == null) return;
                GameAudio.PlaySfx(GameAudioId.SfxMail);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.RegisterEvent<OnTaskEnd>(e =>
            {
                if (e.isExpired) return;
                GameAudio.PlaySfx(GameAudioId.SfxAchievement);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void OnDestroy()
        {
            if (sInstance == this) sInstance = null;
        }
    }
}
