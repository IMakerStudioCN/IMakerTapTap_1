using TMPro;
using UnityEngine;

namespace TapTapFirst.DayChangeShow
{
    [CreateAssetMenu(menuName = "TapTap/Day Change Config", fileName = "DayChangeConfig")]
    public class DayChangeConfig : ScriptableObject
    {
        public const string ResourcePath = "DayChangeShow/DayChangeConfig";

        static DayChangeConfig s_RuntimeConfig;

        [Header("字体（留空则自动找 TerrarumSansBitmap）")]
        public TMP_FontAsset font;

        [Header("层级（低于 GameOverShow 的 5000）")]
        public int canvasSortingOrder = 4500;

        [Header("颜色")]
        public Color backdropColor = Color.black;
        public Color digitColor = Color.white;

        [Header("数字")]
        public float fontSize = 220f;
        public float digitAdvance = 0.62f;
        public float numberStartScale = 0.72f;

        [Header("节奏")]
        public float fadeInSeconds = 0.45f;
        public float oldNumberInSeconds = 0.35f;
        public float oldNumberHoldSeconds = 0.5f;
        public float glitchSeconds = 0.16f;
        public float burstSeconds = 0.34f;
        public float reformSeconds = 0.72f;
        public float reformStaggerSeconds = 0.08f;
        public float newNumberHoldSeconds = 0.7f;
        public float fadeOutSeconds = 0.45f;

        [Header("崩解")]
        public float burstRadius = 340f;
        public float burstRotation = 34f;
        public float burstScale = 0.45f;

        [Header("重组")]
        public float reformOvershoot = 0.06f;

        [Header("碎屑")]
        public int dustPerDigit = 26;
        public float dustSizeMin = 6f;
        public float dustSizeMax = 15f;
        public float dustScatterRadius = 430f;
        public float dustAlpha = 0.7f;
        public float dustSpin = 260f;

        [Header("规则")]
        public bool skipOnFinalDay = true;

        public static DayChangeConfig Load()
        {
            var config = Resources.Load<DayChangeConfig>(ResourcePath);

            if (config == null)
            {
                var all = Resources.LoadAll<DayChangeConfig>(string.Empty);
                for (var i = 0; i < all.Length; i++)
                {
                    if (all[i] == null) continue;
                    config = all[i];
                    break;
                }
            }

            if (config != null) return config;

            if (s_RuntimeConfig == null)
            {
                s_RuntimeConfig = CreateInstance<DayChangeConfig>();
                s_RuntimeConfig.name = "DayChangeConfig(Runtime)";
            }

            return s_RuntimeConfig;
        }
    }
}
