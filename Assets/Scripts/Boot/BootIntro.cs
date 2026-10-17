using System.Collections;
using QFramework;
using TapTapFirst.Audio;
using UnityEngine;

namespace TapTapFirst.Boot
{
    /// <summary>
    /// 老式 CRT 开机动画：黑屏 → 中心 4 角星爆开 → 铺满 → 压成亮线 → 扫描线展开 → 露出界面。
    /// 视觉走新增的 ScreenBootCRT 全屏 pass（Renderer Feature 需要手动加一次），
    /// 星星会被现有的 ScreenStructureCRT 后处理二次弯折/扫描线切割。
    ///
    /// 任意键 / 鼠标点击 可跳过。编辑器里想整段关掉：PlayerPrefs 设 BootIntroForceOff = 1
    /// （Unity 菜单 Console 里执行 PlayerPrefs.SetInt("BootIntroForceOff", 1); 即可）。
    /// </summary>
    public class BootIntro : MonoBehaviour
    {
        private const string EffectShaderName = "TapTap/ScreenBootCRT";
        private const string ForceOffKey = "BootIntroForceOff";

        /// <summary>动画总时长，同时就是开机音的截断时长。</summary>
        public const float Duration = 4.0f;

        private const float UiFadeInStart = 2.95f;
        private const float UiFadeInSeconds = 0.35f;
        private const float TailFadeStart = 3.60f;
        private const float TailFadeSeconds = 0.25f;
        private const float SkipFadeSeconds = 0.12f;

        private static BootIntro sInstance;
        private static bool sPlayed;

        private Material mMaterial;
        private CanvasGroup mUiGroup;

        private int mIdIntensity, mIdBlackout, mIdStarRadius, mIdStarGain, mIdStarSharp, mIdFlare;
        private int mIdBandHalf, mIdBandGain, mIdFlash, mIdNoise, mIdScanReveal, mIdScanPeriod, mIdScanStrength;

        private bool mFinished;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            if (sInstance != null || sPlayed) return;

            var go = new GameObject("[BootIntro]");
            go.AddComponent<BootIntro>();
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
            if (sPlayed)
            {
                gameObject.SetActive(false);
                return;
            }

            sPlayed = true;

            CacheIds();

            mMaterial = FindEffectMaterial();

            var visualsOn = mMaterial != null && PlayerPrefs.GetInt(ForceOffKey, 0) == 0;

            if (mMaterial == null)
            {
                Debug.LogWarning("[BootIntro] 没找到 shader 为 " + EffectShaderName + " 的材质。" +
                                 "请新建材质并在 URP-HighFidelity-Renderer 上加一个 Full Screen Pass Renderer Feature（注入点 Before Rendering Post Processing）。" +
                                 "本次只播开机音，不播画面。");
            }

            // 开机音只在这里播一次，动画和声音同一个时间轴起点
            GameAudio.PlaySfxFor(GameAudioId.SfxBoot, Duration);

            if (!visualsOn)
            {
                gameObject.SetActive(false);
                return;
            }

            HideGameUi();

            mMaterial.SetFloat(mIdIntensity, 1f);

            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            var t = 0f;

            while (t < Duration)
            {
                if (SkipPressed())
                {
                    yield return SkipToEnd();
                    yield break;
                }

                t += Time.unscaledDeltaTime;

                ApplyState(t);
                ApplyUiAlpha(t);

                yield return null;
            }

            ApplyState(Duration);

            yield return FadeTail(SkipFadeSeconds + TailFadeSeconds);

            Finish();
        }

        private IEnumerator SkipToEnd()
        {
            ApplyState(TailFadeStart);
            yield return FadeTail(SkipFadeSeconds);
            Finish();
        }

        private IEnumerator FadeTail(float seconds)
        {
            var t = 0f;

            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;

                var k = Mathf.Clamp01(t / seconds);

                mMaterial.SetFloat(mIdIntensity, 1f - k);
                mMaterial.SetFloat(mIdBlackout, 0f);
                mMaterial.SetFloat(mIdBandGain, 0f);
                mMaterial.SetFloat(mIdStarGain, 0f);
                mMaterial.SetFloat(mIdFlash, 0f);
                mMaterial.SetFloat(mIdNoise, 0f);

                if (mUiGroup != null) mUiGroup.alpha = k;

                yield return null;
            }
        }

        private void Finish()
        {
            if (mFinished) return;
            mFinished = true;

            if (mMaterial != null)
            {
                mMaterial.SetFloat(mIdIntensity, 0f);
                mMaterial.SetFloat(mIdBlackout, 0f);
                mMaterial.SetFloat(mIdStarGain, 0f);
                mMaterial.SetFloat(mIdBandGain, 0f);
                mMaterial.SetFloat(mIdFlash, 0f);
                mMaterial.SetFloat(mIdNoise, 0f);
                mMaterial.SetFloat(mIdScanReveal, 1f);
            }

            if (mUiGroup != null) mUiGroup.alpha = 1f;

            sInstance = null;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (mUiGroup != null) mUiGroup.alpha = 1f;

            if (mMaterial != null) mMaterial.SetFloat(mIdIntensity, 0f);

            if (sInstance == this) sInstance = null;
        }

        #region 时间轴

        private void ApplyState(float t)
        {
            float blackout = 1f, radius = 0f, gain = 0f, bandHalf = 0f, bandGain = 0f;
            float flash = 0f, noise = 0.05f, reveal = 0f;

            if (t < 0.20f)
            {
                // 全黑 + 底噪
            }
            else if (t < 0.30f)
            {
                // 中心白点
                var k = Seg(t, 0.20f, 0.30f);
                radius = Mathf.Lerp(0.006f, 0.02f, k);
                gain = Mathf.Lerp(0f, 2.5f, k);
                noise = Mathf.Lerp(0.05f, 0.10f, k);
            }
            else if (t < 0.50f)
            {
                // 点爆成 4 角星
                var k = Seg(t, 0.10f, 0.50f);
                radius = Mathf.Lerp(0.02f, 0.20f, EaseOutCubic(k));
                gain = Mathf.Lerp(2.0f, 4.5f, k);
                noise = Mathf.Lerp(0.10f, 0.04f, k);
                flash = Mathf.Clamp01(1f - Mathf.Abs(t - 0.36f) / 0.06f) * 0.8f;
            }
            else if (t < 1.25f)
            {
                // 星芒迅速扩大
                var k = Seg(t, 0.50f, 1.25f);
                radius = Mathf.Lerp(0.20f, 1.50f, EaseInCubic(k));
                gain = Mathf.Lerp(4.5f, 2.0f, k);
                noise = 0.02f;
            }
            else if (t < 1.55f)
            {
                // 铺满 → 白屏
                var k = Seg(t, 1.25f, 1.55f);
                radius = Mathf.Lerp(1.50f, 1.90f, k);
                gain = Mathf.Lerp(2.0f, 1.0f, k);
                flash = k;
                noise = 0f;
            }
            else if (t < 1.75f)
            {
                // 白屏压成一条水平亮线
                var k = Seg(t, 1.55f, 1.75f);
                flash = Mathf.Lerp(1f, 0f, Seg(t, 1.55f, 1.68f));
                bandHalf = Mathf.Lerp(1.20f, 0.015f, EaseInOutCubic(k));
                bandGain = Mathf.Lerp(0f, 2.2f, k);
                noise = 0f;
            }
            else if (t < 2.30f)
            {
                // 亮线纵向展开 + 扫描线铺满
                var k = Seg(t, 1.75f, 2.30f);
                bandHalf = Mathf.Lerp(0.015f, 1.0f, EaseOutCubic(k));
                bandGain = Mathf.Lerp(2.2f, 1.0f, k);
                reveal = k;
                noise = 0f;
            }
            else if (t < TailFadeStart)
            {
                // 黑屏淡出，露出画面；亮线收掉
                var k = Seg(t, 2.30f, TailFadeStart);
                blackout = Mathf.Lerp(1f, 0f, k);
                bandHalf = 1f;
                bandGain = Mathf.Lerp(1f, 0f, Seg(t, 2.30f, 2.90f));
                reveal = 1f;
                noise = 0f;
            }
            else
            {
                blackout = 0f;
                bandHalf = 1f;
                reveal = 1f;
                noise = 0f;
            }

            var intensity = 1f - Seg(t, TailFadeStart, TailFadeStart + TailFadeSeconds);

            mMaterial.SetFloat(mIdIntensity, Mathf.Clamp01(intensity));
            mMaterial.SetFloat(mIdBlackout, blackout);
            mMaterial.SetFloat(mIdStarRadius, radius);
            mMaterial.SetFloat(mIdStarGain, gain);
            mMaterial.SetFloat(mIdBandHalf, bandHalf);
            mMaterial.SetFloat(mIdBandGain, bandGain);
            mMaterial.SetFloat(mIdFlash, flash);
            mMaterial.SetFloat(mIdNoise, noise);
            mMaterial.SetFloat(mIdScanReveal, reveal);

            mMaterial.SetFloat(mIdStarSharp, 0.65f);
            mMaterial.SetFloat(mIdFlare, 0.30f);
            mMaterial.SetFloat(mIdScanPeriod, 3f);
            mMaterial.SetFloat(mIdScanStrength, 0.65f);
        }

        private void ApplyUiAlpha(float t)
        {
            // UIRoot / 面板可能比本组件晚创建，还没抓到就继续找
            if (mUiGroup == null && t < UiFadeInStart) HideGameUi();

            if (mUiGroup == null) return;

            mUiGroup.alpha = t < UiFadeInStart
                ? 0f
                : Mathf.Clamp01((t - UiFadeInStart) / UiFadeInSeconds);
        }

        private static float Seg(float t, float from, float to) =>
            to <= from ? 1f : Mathf.Clamp01((t - from) / (to - from));

        private static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);

        private static float EaseInCubic(float x) => x * x * x;

        private static float EaseInOutCubic(float x) =>
            x < 0.5f ? 4f * x * x * x : 1f - Mathf.Pow(-2f * x + 2f, 3f) * 0.5f;

        #endregion

        #region 基础设施

        private void CacheIds()
        {
            mIdIntensity = Shader.PropertyToID("_Intensity");
            mIdBlackout = Shader.PropertyToID("_Blackout");
            mIdStarRadius = Shader.PropertyToID("_StarRadius");
            mIdStarGain = Shader.PropertyToID("_StarGain");
            mIdStarSharp = Shader.PropertyToID("_StarSharp");
            mIdFlare = Shader.PropertyToID("_Flare");
            mIdBandHalf = Shader.PropertyToID("_BandHalf");
            mIdBandGain = Shader.PropertyToID("_BandGain");
            mIdFlash = Shader.PropertyToID("_Flash");
            mIdNoise = Shader.PropertyToID("_Noise");
            mIdScanReveal = Shader.PropertyToID("_ScanReveal");
            mIdScanPeriod = Shader.PropertyToID("_ScanPeriod");
            mIdScanStrength = Shader.PropertyToID("_ScanStrength");
        }

        private static Material FindEffectMaterial()
        {
            var materials = Resources.FindObjectsOfTypeAll<Material>();

            for (var i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                if (material == null || material.shader == null) continue;
                if (material.shader.name == EffectShaderName) return material;
            }

            return null;
        }

        private void HideGameUi()
        {
            var uiRoot = FindObjectOfType<UIRoot>();
            if (uiRoot != null)
            {
                mUiGroup = EnsureGroup(uiRoot.gameObject);
            }
            else
            {
                var canvases = FindObjectsOfType<Canvas>();
                for (var i = 0; i < canvases.Length; i++)
                {
                    var canvas = canvases[i];
                    if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                    if (canvas.gameObject == gameObject) continue;

                    mUiGroup = EnsureGroup(canvas.gameObject);
                    break;
                }
            }

            if (mUiGroup != null) mUiGroup.alpha = 0f;
        }

        private static CanvasGroup EnsureGroup(GameObject target)
        {
            var group = target.GetComponent<CanvasGroup>();
            if (group == null) group = target.AddComponent<CanvasGroup>();
            return group;
        }

        private static bool SkipPressed() =>
            Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);

        #endregion
    }
}
