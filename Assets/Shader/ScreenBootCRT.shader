Shader "TapTap/ScreenBootCRT"
{
    Properties
    {
        [Header(Base)]
        _Intensity ("总强度 (0 = 完全关闭)", Range(0, 1)) = 0

        [Header(Boot State)]
        _Blackout ("黑屏度", Range(0, 1)) = 1
        _StarRadius ("星半径", Range(0, 2)) = 0
        _StarGain ("星亮度", Range(0, 8)) = 0
        _StarSharp ("星芒尖锐 (越大越细)", Range(0, 1)) = 0.65
        _Flare ("十字星芒长度", Range(0, 1)) = 0.3
        _BandHalf ("亮线半高 (1 = 铺满)", Range(0, 1.5)) = 0
        _BandGain ("亮线亮度", Range(0, 4)) = 0
        _Flash ("白闪", Range(0, 1)) = 0
        _Noise ("底噪", Range(0, 1)) = 0

        [Header(Raster)]
        _ScanReveal ("扫描线展开 (0-1)", Range(0, 1)) = 0
        _ScanPeriod ("扫描线周期 (屏幕像素)", Range(2, 8)) = 3
        _ScanStrength ("扫描线强度", Range(0, 1)) = 0.65
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ScreenBootCRT"
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Intensity;
            float _Blackout;
            float _StarRadius;
            float _StarGain;
            float _StarSharp;
            float _Flare;
            float _BandHalf;
            float _BandGain;
            float _Flash;
            float _Noise;
            float _ScanReveal;
            float _ScanPeriod;
            float _ScanStrength;

            float3 SampleScreen(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // 四角星：星形线（astroid）度量，n 越小星芒越尖
            float StarMetric(float2 p, float n, float radius)
            {
                const float kInvSqrt2 = 0.70710678;
                float2 q = float2((p.x + p.y) * kInvSqrt2, (p.y - p.x) * kInvSqrt2);
                float2 a = abs(q);

                float m = pow(a.x + 1e-5, n) + pow(a.y + 1e-5, n);
                return m - pow(max(radius, 0.0) + 1e-5, n);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float gate = saturate(_Intensity);
                float2 uv = input.texcoord;
                float2 res = max(_ScreenParams.xy, float2(1.0, 1.0));
                float aspect = res.x / res.y;

                float3 screen = SampleScreen(uv);

                // 居中 + 宽高比校正后的坐标
                float2 p = (uv - 0.5) * float2(aspect, 1.0);

                float3 col = lerp(screen, float3(0.0, 0.0, 0.0), saturate(_Blackout));

                // ---- 四角星 ----
                float n = lerp(1.0, 0.55, saturate(_StarSharp));
                float d = StarMetric(p, n, _StarRadius);

                float aa = max(fwidth(d), 1e-5);
                float body = saturate(-d / aa);
                float glow = exp(-max(d, 0.0) * 14.0);

                float streaks = (exp(-abs(p.y) * 110.0) + exp(-abs(p.x) * 110.0)) * saturate(_Flare);

                float3 starTint = float3(1.0, 0.97, 0.92);
                col += (body + glow * 0.45 + streaks * 0.6) * _StarGain * starTint;

                // ---- CRT 亮线 + 扫描线展开 ----
                float halfH = max(_BandHalf, 0.0);
                float bandMask = 1.0 - smoothstep(halfH * 0.85, halfH * 1.0 + 1e-4, abs(p.y));

                float period = max(2.0, floor(_ScanPeriod + 0.5));
                float tri = abs(frac(input.positionCS.y / period) * 2.0 - 1.0);
                float scan = lerp(1.0, tri, saturate(_ScanStrength));

                float reveal = saturate(_ScanReveal * 1.15 - (1.0 - uv.y) * 0.15);
                float revealMask = saturate(reveal * 20.0);

                col += bandMask * _BandGain * scan * revealMask * float3(0.93, 0.97, 1.0);

                // ---- 白闪 / 底噪 ----
                col += saturate(_Flash) * float3(1.0, 1.0, 1.0);

                float noise = Hash21(input.positionCS.xy + float2(_Time.y * 37.0, _Time.y * 11.0));
                col += (noise - 0.5) * saturate(_Noise);

                col = lerp(screen, col, gate);

                return half4(max(col, 0.0), 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
