using UnityEngine;
using UnityEngine.EventSystems;

namespace TapTapFirst.ScreenFx
{
    [DisallowMultipleComponent]
    public class ScreenCurvaturePointer : MonoBehaviour
    {
        public float curvature = 0.03f;
        public float screenScale = 0.97f;
        [Range(0f, 1f)] public float intensity = 1f;
        public float cornerRadius = 12f;
        public bool blockBezelClicks = true;
        public Material sourceMaterial;

        static ScreenCurvaturePointer s_Instance;
        WarpedPointerInput m_Input;
        bool m_MaterialSearched;

        public static ScreenCurvaturePointer Instance { get { return s_Instance; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (s_Instance != null) return;
            var go = new GameObject("[ScreenCurvaturePointer]");
            go.hideFlags = HideFlags.DontSave;
            DontDestroyOnLoad(go);
            s_Instance = go.AddComponent<ScreenCurvaturePointer>();
        }

        void Awake()
        {
            if (s_Instance == null) s_Instance = this;
            m_Input = GetComponent<WarpedPointerInput>();
            if (m_Input == null) m_Input = gameObject.AddComponent<WarpedPointerInput>();
        }

        void Update()
        {
            SyncFromMaterial();

            var system = EventSystem.current;
            if (system == null) return;
            var module = system.currentInputModule;
            if (module == null) return;
            if (module.inputOverride != m_Input) module.inputOverride = m_Input;
        }

        void SyncFromMaterial()
        {
            if (sourceMaterial == null)
            {
                if (m_MaterialSearched) return;
                m_MaterialSearched = true;
                sourceMaterial = FindEffectMaterial();
                if (sourceMaterial == null) return;
            }

            if (!sourceMaterial.HasProperty("_Curvature"))
            {
                sourceMaterial = null;
                return;
            }

            curvature = sourceMaterial.GetFloat("_Curvature");
            screenScale = sourceMaterial.GetFloat("_ScreenScale");
            intensity = sourceMaterial.GetFloat("_Intensity");
            cornerRadius = sourceMaterial.GetFloat("_CornerRadius");
        }

        static Material FindEffectMaterial()
        {
            var materials = Resources.FindObjectsOfTypeAll<Material>();
            for (int i = 0; i < materials.Length; i++)
            {
                var candidate = materials[i];
                if (candidate != null && candidate.shader != null && candidate.shader.name == "TapTap/ScreenStructureCRT")
                    return candidate;
            }
            return null;
        }

        public Vector2 Warp(Vector2 screenPosition)
        {
            float width = Screen.width;
            float height = Screen.height;
            if (width <= 1f || height <= 1f) return screenPosition;

            float amount = Mathf.Clamp01(intensity);
            if (amount <= 0f) return screenPosition;

            float k = curvature * amount;
            float scale = Mathf.Lerp(1f, screenScale, amount);
            float aspect = width / height;

            Vector2 uv = new Vector2(screenPosition.x / width, screenPosition.y / height);
            Vector2 c = (uv - new Vector2(0.5f, 0.5f)) * scale;
            Vector2 ca = new Vector2(c.x * aspect, c.y);
            ca *= 1f + k * ca.sqrMagnitude;
            c = new Vector2(ca.x / aspect, ca.y);
            Vector2 mapped = c + new Vector2(0.5f, 0.5f);

            if (blockBezelClicks && !IsPointerHeld())
            {
                if (mapped.x < 0f || mapped.x > 1f || mapped.y < 0f || mapped.y > 1f)
                    return OffScreen;
                if (!InsideRoundedScreen(screenPosition, width, height))
                    return OffScreen;
            }

            return new Vector2(Mathf.Clamp(mapped.x * width, 0f, width - 1f),
                               Mathf.Clamp(mapped.y * height, 0f, height - 1f));
        }

        static Vector2 OffScreen
        {
            get { return new Vector2(-10000f, -10000f); }
        }

        static bool IsPointerHeld()
        {
            return Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2) || Input.touchCount > 0;
        }

        bool InsideRoundedScreen(Vector2 position, float width, float height)
        {
            float radius = Mathf.Min(cornerRadius, Mathf.Min(width, height) * 0.5f);
            if (radius <= 0f) return true;

            Vector2 q = new Vector2(
                Mathf.Abs(position.x - width * 0.5f) - Mathf.Max(width * 0.5f - radius, 0f),
                Mathf.Abs(position.y - height * 0.5f) - Mathf.Max(height * 0.5f - radius, 0f));

            float outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude;
            float distance = outside + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
            return distance <= 0f;
        }
    }

    public class WarpedPointerInput : BaseInput
    {
        public override Vector2 mousePosition
        {
            get
            {
                Vector2 raw = base.mousePosition;
                var pointer = ScreenCurvaturePointer.Instance;
                if (pointer == null || !pointer.isActiveAndEnabled) return raw;
                return pointer.Warp(raw);
            }
        }
    }
}
