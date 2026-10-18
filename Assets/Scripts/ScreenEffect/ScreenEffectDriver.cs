using UnityEngine;

namespace TapTapFirst.ScreenFx
{
    public static class ScreenEffect
    {
        public const int ParamCount = 9;

        public static readonly string[] ParamNames =
        {
            "_Intensity",
            "_Curvature",
            "_ScreenScale",
            "_CornerRadius",
            "_StructureStrength",
            "_StructureGain",
            "_SubpixelBlur",
            "_ScanlineStrength",
            "_Vignette"
        };

        public static readonly float[] InitialDefaults = { 1f, 0.03f, 0.97f, 12f, 0.3f, 1.15f, 0f, 0f, 0.1f };
        static readonly float[] s_Values = (float[])InitialDefaults.Clone();
        static readonly bool[] s_Touched = new bool[ParamCount];
        static ScreenEffectDriver s_Driver;

        public static float Intensity { get { return s_Values[0]; } set { Set(0, value); } }
        public static float GlobalIntensity { get { return s_Values[0]; } set { Set(0, value); } }
        public static float Curvature { get { return s_Values[1]; } set { Set(1, value); } }
        public static float ScreenScale { get { return s_Values[2]; } set { Set(2, value); } }
        public static float CornerRadius { get { return s_Values[3]; } set { Set(3, value); } }
        public static float StructureStrength { get { return s_Values[4]; } set { Set(4, value); } }
        public static float StructureGain { get { return s_Values[5]; } set { Set(5, value); } }
        public static float SubpixelBlur { get { return s_Values[6]; } set { Set(6, value); } }
        public static float ScanlineStrength { get { return s_Values[7]; } set { Set(7, value); } }
        public static float Vignette { get { return s_Values[8]; } set { Set(8, value); } }

        public static Material Material { get { return Driver.TargetMaterial; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            var driver = Driver;
            for (int i = 0; i < ParamCount; i++)
                if (!s_Touched[i]) s_Values[i] = driver.Default(i);
        }

        static ScreenEffectDriver Driver
        {
            get
            {
                if (s_Driver == null)
                {
                    s_Driver = ScreenEffectDriver.Create();
                    s_Driver.Initialize();
                }
                return s_Driver;
            }
        }

        static void Set(int index, float value)
        {
            s_Values[index] = value;
            s_Touched[index] = true;
            Driver.Apply(index, value);
        }

        public static void ResetToDefaults()
        {
            var driver = Driver;
            for (int i = 0; i < ParamCount; i++)
            {
                s_Values[i] = driver.Default(i);
                s_Touched[i] = false;
                driver.Apply(i, s_Values[i]);
            }
        }

        public static void SetMaterial(Material material)
        {
            Driver.UseMaterial(material);
            for (int i = 0; i < ParamCount; i++)
            {
                if (!s_Touched[i]) s_Values[i] = Driver.Default(i);
                else Driver.Apply(i, s_Values[i]);
            }
        }
    }

    public class ScreenEffectDriver : MonoBehaviour
    {
        const string EffectShaderName = "TapTap/ScreenStructureCRT";

        Material m_Material;
        int[] m_Ids;
        float[] m_Defaults;
        float[] m_Applied;
        bool m_Cached;
        float m_NextSearchTime;
        bool m_Warned;

        public Material TargetMaterial { get { return m_Material; } }

        public static ScreenEffectDriver Create()
        {
            var existing = FindObjectOfType<ScreenEffectDriver>();
            if (existing != null) return existing;

            var go = new GameObject("[ScreenEffectDriver]");
            go.hideFlags = HideFlags.DontSave;
            DontDestroyOnLoad(go);
            return go.AddComponent<ScreenEffectDriver>();
        }

        void Awake()
        {
            int count = ScreenEffect.ParamCount;
            m_Ids = new int[count];
            for (int i = 0; i < count; i++) m_Ids[i] = Shader.PropertyToID(ScreenEffect.ParamNames[i]);
            m_Defaults = new float[count];
            m_Applied = new float[count];
            for (int i = 0; i < count; i++)
            {
                m_Defaults[i] = 0f;
                m_Applied[i] = float.NaN;
            }
        }

        public void Initialize()
        {
            EnsureMaterial();
        }

        public void UseMaterial(Material material)
        {
            if (material == null) return;
            m_Material = material;
            m_Cached = false;
            CacheDefaults();
        }

        public float Default(int index)
        {
            if (!m_Cached) EnsureMaterial();
            return m_Cached ? m_Defaults[index] : ScreenEffect.InitialDefaults[index];
        }

        public void Apply(int index, float value)
        {
            if (!EnsureMaterial()) return;
            if (Mathf.Approximately(m_Applied[index], value)) return;
            m_Applied[index] = value;
            m_Material.SetFloat(m_Ids[index], value);
        }

        bool EnsureMaterial()
        {
            if (m_Material != null && m_Cached) return true;
            if (m_Material == null)
            {
                if (Time.unscaledTime < m_NextSearchTime) return false;
                m_NextSearchTime = Time.unscaledTime + 0.5f;
                m_Material = FindEffectMaterial();
            }

            if (m_Material == null)
            {
                if (!m_Warned)
                {
                    m_Warned = true;
                    Debug.LogWarning("[ScreenEffect] 没找到 shader 为 " + EffectShaderName + " 的材质：参数已被记录，但暂时不会作用到画面上。");
                }
                return false;
            }

            CacheDefaults();
            return true;
        }

        void CacheDefaults()
        {
            if (m_Material == null) return;
            for (int i = 0; i < m_Ids.Length; i++)
            {
                m_Defaults[i] = m_Material.GetFloat(m_Ids[i]);
                m_Applied[i] = m_Defaults[i];
            }
            m_Cached = true;
        }

        static Material FindEffectMaterial()
        {
            var materials = Resources.FindObjectsOfTypeAll<Material>();
            for (int i = 0; i < materials.Length; i++)
            {
                var candidate = materials[i];
                if (candidate != null && candidate.shader != null && candidate.shader.name == EffectShaderName)
                    return candidate;
            }
            return null;
        }

        void OnApplicationQuit()
        {
            RestoreDefaults();
        }

        void OnDestroy()
        {
            RestoreDefaults();
        }

        void RestoreDefaults()
        {
            if (m_Material == null || !m_Cached) return;
            for (int i = 0; i < m_Ids.Length; i++) m_Material.SetFloat(m_Ids[i], m_Defaults[i]);
        }
    }
}
