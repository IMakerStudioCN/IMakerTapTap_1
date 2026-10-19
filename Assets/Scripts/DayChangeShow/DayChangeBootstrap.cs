using UnityEngine;

namespace TapTapFirst.DayChangeShow
{
    /// <summary>
    /// 自动挂载 DayChangeDirector，不需要在任何场景 / 预制体上手动挂脚本。
    /// </summary>
    public static class DayChangeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (DayChangeDirector.Instance != null) return;
            if (Object.FindObjectOfType<DayChangeDirector>() != null) return;

            var go = new GameObject("[DayChangeDirector]");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<DayChangeDirector>();
        }
    }
}
