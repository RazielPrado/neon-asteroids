Shader "LoneRetr0/CRTFullscreen"
{
    Properties
    {
        _Curvature ("Curvature", Range(0, 0.4)) = 0.08
        _ScanlineCount ("Scanline Count", Float) = 360
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.3
        _ScanlineSpeed ("Scanline Speed", Float) = 2
        _MaskIntensity ("Phosphor Mask", Range(0, 1)) = 0.12
        _RGBSplit ("RGB Split (px)", Range(0, 4)) = 0.8
        _VignetteIntensity ("Vignette", Range(0, 2)) = 0.7
        _NoiseIntensity ("Noise", Range(0, 0.3)) = 0.04
        _Flicker ("Flicker", Range(0, 0.2)) = 0.025
        _RollBandIntensity ("Rolling Band", Range(0, 0.5)) = 0.06
        _Brightness ("Brightness", Range(0.5, 2)) = 1.15
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "CRT"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Curvature;
            float _ScanlineCount;
            float _ScanlineIntensity;
            float _ScanlineSpeed;
            float _MaskIntensity;
            float _RGBSplit;
            float _VignetteIntensity;
            float _NoiseIntensity;
            float _Flicker;
            float _RollBandIntensity;
            float _Brightness;

            float _CRTGlitch;

            float CRTHash(float2 p)
            {
                p = frac(p * float2(443.897, 441.423));
                p += dot(p, p.yx + 19.19);
                return frac((p.x + p.y) * p.x);
            }

            float2 CurveUV(float2 uv)
            {
                float2 c = uv * 2.0 - 1.0;
                c *= 1.0 + _Curvature * dot(c, c);
                return c * 0.5 + 0.5;
            }

            float4 Frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float t = _Time.y;
                float2 uv = CurveUV(input.texcoord);

                float2 in2 = smoothstep(0.0, 0.004, uv) * (1.0 - smoothstep(0.996, 1.0, uv));
                float inside = in2.x * in2.y;

                float glitch = _CRTGlitch;
                float row = floor(uv.y * 48.0);
                float n1 = CRTHash(float2(row, floor(t * 24.0)));
                uv.x += (n1 - 0.5) * 0.12 * glitch * step(0.55, n1);
                uv.y += sin(t * 43.0) * 0.006 * glitch;

                float px = 1.0 / _ScreenParams.x;
                float split = (_RGBSplit + glitch * 10.0) * px;
                float3 col;
                col.r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(split, 0)).r;
                col.g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).g;
                col.b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(split, 0)).b;

                float scan = sin((uv.y * _ScanlineCount + t * _ScanlineSpeed) * PI * 2.0) * 0.5 + 0.5;
                col *= 1.0 - _ScanlineIntensity * (1.0 - scan);

                float colIndex = fmod(floor(input.texcoord.x * _ScreenParams.x), 3.0);
                float3 mask = float3(colIndex < 0.5, colIndex >= 0.5 && colIndex < 1.5, colIndex >= 1.5);
                col *= lerp(1.0.xxx, mask * 1.5 + 0.25, _MaskIntensity);

                float roll = frac(uv.y + t * 0.15);
                col *= 1.0 + _RollBandIntensity * smoothstep(0.92, 1.0, roll);

                float n = CRTHash(input.texcoord * _ScreenParams.xy + frac(t) * float2(91.7, 13.3)) - 0.5;
                col += n * (_NoiseIntensity + glitch * 0.25);
                col *= 1.0 - _Flicker * (0.5 + 0.5 * sin(t * 120.0));

                float2 v = uv * (1.0 - uv);
                float vig = saturate(pow(max(v.x * v.y * 16.0, 0.0), _VignetteIntensity * 0.35));
                col *= vig;

                col *= _Brightness * inside;
                return float4(max(col, 0.0), 1.0);
            }
            ENDHLSL
        }
    }
}
