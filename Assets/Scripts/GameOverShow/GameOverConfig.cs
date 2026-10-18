using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TapTapFirst.GameOverShow
{
    public enum EndingScreenStyle
    {
        Shatter = 0,
        CrtShutdown = 1,
        RemoveScreenEffect = 2,
        None = 3
    }

    public enum GameOverEndAction
    {
        ShowButtons = 0,
        LoadScene = 1,
        Stay = 2
    }

    [Serializable]
    public class GameOverCreditsEntry
    {
        public string role = "";
        public string name = "";
    }

    [Serializable]
    public class GameOverCreditsSettings
    {
        public bool enabled = true;
        public string title = "制作人员";
        public GameOverCreditsEntry[] entries = new GameOverCreditsEntry[0];

        public float titleFontSize = 56f;
        public float fontSize = 32f;
        public float lineHeight = 64f;
        public float leftColumnWidth = 320f;
        public float columnGap = 60f;

        public Color titleColor = Color.white;
        public Color color = Color.white;

        public float scrollSeconds = 18f;
        public float fadeSeconds = 0.6f;

        public bool allowSkip = true;
        public string skipHint = "点击 / 任意键 跳过";
    }

    [Serializable]
    public class GameOverEndingPreset
    {
        public int id = 0;
        public string label = "";

        [Header("中央文字")]
        public string title = "";
        [TextArea] public string subtitle = "";
        public Color titleColor = Color.white;
        public Color subtitleColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        public float titleFontSize = 96f;
        public float subtitleFontSize = 36f;
        public float textStartScale = 0.15f;
        public float textGrowSeconds = 2.5f;
        public float textHoldSeconds = 2.5f;
        public float textFadeSeconds = 0.6f;

        [Header("屏幕效果")]
        public EndingScreenStyle screenStyle = EndingScreenStyle.Shatter;
        public float screenEffectSeconds = 1.1f;
        public bool showBackground = true;
        public Color backgroundColor = Color.black;

        [Header("图片 / CG")]
        public Sprite cg;
        public Sprite[] images = new Sprite[0];
        public float imageSeconds = 2.5f;
        public float imageFadeSeconds = 0.5f;

        [Header("节奏")]
        public float delayBeforeContent = 0.3f;
        public float delayBeforeCredits = 0.6f;
    }

    [CreateAssetMenu(menuName = "TapTap/Game Over Config", fileName = "GameOverConfig")]
    public class GameOverConfig : ScriptableObject
    {
        public const string ResourcePath = "GameOverShow/GameOverConfig";

        static GameOverConfig s_RuntimeConfig;

        [Header("字体（留空则自动找中文字体）")]
        public TMP_FontAsset font;

        [Header("层级")]
        public int canvasSortingOrder = 5000;

        [Header("结局内容（按 id 0~4 配置）")]
        public GameOverEndingPreset[] endings = new GameOverEndingPreset[0];

        [Header("制作人员名单")]
        public GameOverCreditsSettings credits = new GameOverCreditsSettings();

        [Header("名单之后")]
        public GameOverEndAction endAction = GameOverEndAction.ShowButtons;
        public string restartSceneName = "Start";
        public string restartButtonText = "重新开始";
        public string quitButtonText = "退出游戏";
        public float buttonFadeSeconds = 0.6f;
        public Color buttonColor = new Color(1f, 1f, 1f, 0.14f);
        public Color buttonTextColor = Color.white;
        public float buttonFontSize = 34f;

        [Header("跳过演出（两段式：第一次跳到名单，第二次跳过名单）")]
        public bool allowSkipEnding = true;
        public float skipStartDelay = 0.5f;
        public float skipCooldown = 0.4f;
        [TextArea] public string endingSkipHint = "点击 / 按任意键 跳过演出";

        void OnEnable()
        {
            EnsureDefaults();
        }

        public void EnsureDefaults()
        {
            if (endings == null) endings = new GameOverEndingPreset[0];

            var list = new List<GameOverEndingPreset>(endings.Length + 5);
            for (var i = 0; i < endings.Length; i++)
            {
                if (endings[i] != null) list.Add(endings[i]);
            }

            for (var id = 0; id <= 4; id++)
            {
                var found = false;
                for (var i = 0; i < list.Count; i++)
                {
                    if (list[i].id == id)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found) list.Add(CreatePreset(id));
            }

            endings = list.ToArray();

            if (credits == null) credits = new GameOverCreditsSettings();
            if (credits.entries == null || credits.entries.Length == 0) credits.entries = CreateDefaultCredits();
        }

        public GameOverEndingPreset GetEnding(int id)
        {
            if (endings != null)
            {
                for (var i = 0; i < endings.Length; i++)
                {
                    if (endings[i] != null && endings[i].id == id) return endings[i];
                }
            }

            return CreatePreset(id);
        }

        public static GameOverConfig Load()
        {
            var config = Resources.Load<GameOverConfig>(ResourcePath);

            if (config == null)
            {
                // 资源被改名/挪到别的 Resources 目录时也能找到
                var all = Resources.LoadAll<GameOverConfig>(string.Empty);
                for (var i = 0; i < all.Length; i++)
                {
                    if (all[i] != null)
                    {
                        config = all[i];
                        break;
                    }
                }
            }

            if (config != null)
            {
                config.EnsureDefaults();
                return config;
            }

            Debug.LogWarning("[GameOverShow] 没找到结局配置，使用内置默认配置。请把配置放到 Resources/GameOverShow/GameOverConfig.asset");

            if (s_RuntimeConfig == null)
            {
                s_RuntimeConfig = CreateInstance<GameOverConfig>();
                s_RuntimeConfig.name = "GameOverConfig(Runtime)";
            }

            s_RuntimeConfig.EnsureDefaults();
            return s_RuntimeConfig;
        }

        public static GameOverEndingPreset CreatePreset(int id)
        {
            var preset = new GameOverEndingPreset { id = id, label = "结局 " + id };

            switch (id)
            {
                case 0:
                    preset.label = "信任归零";
                    preset.screenStyle = EndingScreenStyle.Shatter;
                    preset.title = "信任崩塌";
                    preset.subtitle = "当信任归零，再没有人愿意听你说话。";
                    break;
                case 1:
                    preset.label = "道德归零";
                    preset.screenStyle = EndingScreenStyle.Shatter;
                    preset.title = "底线失守";
                    preset.subtitle = "你追上了流量，却弄丢了自己。";
                    break;
                case 2:
                    preset.label = "资金不足";
                    preset.screenStyle = EndingScreenStyle.Shatter;
                    preset.title = "资金断裂";
                    preset.subtitle = "你没能撑到最后一页。";
                    break;
                case 3:
                    preset.label = "普通结局";
                    preset.screenStyle = EndingScreenStyle.CrtShutdown;
                    preset.title = "尘埃落定";
                    preset.subtitle = "你活了下来，故事暂时落幕。";
                    break;
                case 4:
                    preset.label = "NB结局";
                    preset.screenStyle = EndingScreenStyle.RemoveScreenEffect;
                    preset.showBackground = false;
                    preset.screenEffectSeconds = 1.2f;
                    preset.title = "传奇";
                    preset.subtitle = "你做到了几乎不可能的事。";
                    break;
            }

            return preset;
        }

        public static GameOverCreditsEntry[] CreateDefaultCredits()
        {
            return new[]
            {
                new GameOverCreditsEntry { role = "策划", name = "待填写" },
                new GameOverCreditsEntry { role = "程序", name = "待填写" },
                new GameOverCreditsEntry { role = "美术", name = "待填写" },
                new GameOverCreditsEntry { role = "音乐", name = "待填写" },
                new GameOverCreditsEntry { role = "测试", name = "待填写" },
                new GameOverCreditsEntry { role = "特别鸣谢", name = "每一位玩家" }
            };
        }
    }
}
