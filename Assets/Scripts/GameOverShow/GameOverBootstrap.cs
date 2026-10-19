using UnityEngine;

namespace TapTapFirst.GameOverShow
{
    public static class GameOverBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (GameOverDirector.Instance != null) return;
            if (Object.FindObjectOfType<GameOverDirector>() != null) return;

            var go = new GameObject("[GameOverDirector]");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<GameOverDirector>();
        }
    }
}
