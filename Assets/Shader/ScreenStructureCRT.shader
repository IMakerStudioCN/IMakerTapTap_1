Shader "TapTap/ScreenStructureCRT"
{
    Properties
    {
        [Header(Base)]
        _Intensity ("全局强度 (0 = 完全关闭)", Range(0, 1)) = 1

        [Header(Curvature)]
        _Curvature ("曲率 (外凸)", Range(0, 0.15)) = 0.03
        _ScreenScale ("画面缩放 (越小四周黑边越宽)", Range(0.5, 1.05)) = 0.97
        _CornerRadius ("屏幕圆角 (像素)", Range(0, 300)) = 12
        _BorderColor ("边框颜色", Color) = (0.02, 0.02, 0.025, 1)
        _BorderSoftness ("边框柔化 (像素)", Range(0.2, 12)) = 1.5

        [Header(Pixel Structure)]
        [IntRange] _StructureMode ("结构模式 (0关 1方格 2RGB条纹 3三角点阵)", Range(0, 3)) = 1
        _StructurePeriod ("结构周期 (屏幕像素)", Range(1, 8)) = 3
        _StructureStrength ("结构强度", Range(0, 1)) = 0.3
        _StructureGain ("能量补偿 (提亮)", Range(0.5, 2)) = 1.15
        _LuminanceModulation ("亮度调制 (亮处更明显)", Range(0, 1)) = 1
        _DarkAreaStructure ("暗部结构可见度 (加性)", Range(0, 0.2)) = 0.01
        _SubpixelBlur ("子像素模糊 (水平)", Range(0, 1)) = 0

        [Header(Optional)]
        _ScanlineStrength ("扫描线强度", Range(0, 1)) = 0
        _ScanlinePeriod ("扫描线周期 (屏幕像素)", Range(2, 8)) = 3
        _Vignette ("暗角强度", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ScreenStructureCRT"
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float  _Intensity;
            float  _Curvature;
            float  _ScreenScale;
            float  _CornerRadius;
            float4 _BorderColor;
            float  _BorderSoftness;
            float  _StructureMode;
            float  _StructurePeriod;
            float  _StructureStrength;
            float  _StructureGain;
            float  _LuminanceModulation;
            float  _DarkAreaStructure;
            float  _SubpixelBlur;
            float  _ScanlineStrength;
            float  _ScanlinePeriod;
            float  _Vignette;

            static const float3 kLuminance = float3(0.2126, 0.7152, 0.0722);

            float3 SampleScreen(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
            }

            float3 StructureMask(float2 pixelPos)
            {
                float mode = floor(_StructureMode + 0.5);
                if (mode < 0.5)
                    return float3(1.0, 1.0, 1.0);

                float period = max(1.0, floor(_StructurePeriod + 0.5));
                float2 cell = pixelPos / period;
                float2 aa = max(fwidth(cell), float2(0.0001, 0.0001));

                if (mode < 1.5)
                {
                    float2 f = frac(cell);
                    float2 d = min(f, 1.0 - f);
                    float2 dark = saturate(1.0 - d / aa);
                    float bright = 1.0 - (dark.x + dark.y - dark.x * dark.y);
                    return float3(bright, bright, bright);
                }

                float sa = max(aa.x, 0.0001) * 3.0;
                float rowIndex = floor(cell.y);
                float phase = (mode < 2.5) ? 0.0 : rowIndex / 3.0;
                float sub = frac(cell.x + phase) * 3.0;
                float3 d3 = abs(sub - float3(0.5, 1.5, 2.5));
                float3 slot = saturate(((1.0 + sa) * 0.5 - d3) / sa);

                if (mode < 2.5)
                    return slot;

                float fy = frac(cell.y);
                float ay = max(aa.y, 0.0001);
                float band = saturate(1.0 - abs(fy - 0.5) / ay);
                return slot * band;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float t = saturate(_Intensity);
                float2 res = max(_ScreenParams.xy, float2(1.0, 1.0));
                float2 uv0 = input.texcoord;
                float2 pixelPos = input.positionCS.xy;
                float aspect = res.x / res.y;

                float2 c = (uv0 - 0.5) * lerp(1.0, _ScreenScale, t);
                float2 ca = float2(c.x * aspect, c.y);
                ca *= 1.0 + _Curvature * t * dot(ca, ca);
                float2 uv = float2(ca.x / aspect, ca.y) + 0.5;

                float2 texel = 1.0 / res;
                float3 center = SampleScreen(uv);
                float3 blur = (SampleScreen(uv - float2(texel.x, 0.0)) + 2.0 * center
                             + SampleScreen(uv + float2(texel.x, 0.0))) * 0.25;
                float3 col = lerp(center, blur, saturate(_SubpixelBlur) * t);

                float3 raw = StructureMask(pixelPos);
                float lum = saturate(dot(col, kLuminance));
                float strength = saturate(_StructureStrength * t * lerp(1.0, saturate(lum * 2.0), saturate(_LuminanceModulation)));
                col = col * (lerp(1.0, raw, strength) * lerp(1.0, _StructureGain, t))
                    + raw * (_DarkAreaStructure * t * (1.0 - lum));

                float2 borderDist = min(uv, 1.0 - uv) * res;
                float softEdge = max(_BorderSoftness, 0.05);
                float borderMask = saturate(min(borderDist.x, borderDist.y) / softEdge + 0.5);

                float cornerRadius = _CornerRadius * t;
                float2 q = abs(pixelPos - res * 0.5) - max(res * 0.5 - cornerRadius, float2(0.0, 0.0));
                float sd = length(max(q, float2(0.0, 0.0))) + min(max(q.x, q.y), 0.0) - cornerRadius;
                float screenMask = saturate(0.5 - sd);

                col = lerp(_BorderColor.rgb, col, min(borderMask, screenMask));

                float scanline = saturate(_ScanlineStrength) * t;
                if (scanline > 0.0)
                {
                    float linePeriod = max(2.0, floor(_ScanlinePeriod + 0.5));
                    float tri = abs(frac(pixelPos.y / linePeriod) * 2.0 - 1.0);
                    col *= lerp(1.0, tri, scanline);
                }

                float vignette = saturate(_Vignette) * t;
                if (vignette > 0.0)
                {
                    float2 v = (pixelPos / res - 0.5) * float2(aspect, 1.0);
                    float r = saturate(length(v) / length(float2(aspect * 0.5, 0.5)));
                    col *= 1.0 - vignette * r * r;
                }

                return half4(max(col, 0.0), 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
