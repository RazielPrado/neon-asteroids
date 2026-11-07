Shader "LoneRetr0/SynthwaveBackground"
{
    Properties
    {
        _SkyTop ("Sky Top", Color) = (0.02, 0.0, 0.07, 1)
        _SkyHorizon ("Sky Horizon", Color) = (0.45, 0.03, 0.38, 1)
        _GroundColor ("Ground", Color) = (0.02, 0.0, 0.05, 1)
        [HDR] _GridColor ("Grid", Color) = (1.0, 0.12, 1.3, 1)
        [HDR] _SunTop ("Sun Top", Color) = (1.7, 1.05, 0.2, 1)
        [HDR] _SunBottom ("Sun Bottom", Color) = (1.6, 0.1, 0.65, 1)
        _HorizonY ("Horizon (UV)", Range(0, 1)) = 0.32
        _SunRadius ("Sun Radius", Range(0.05, 0.5)) = 0.2
        _GridSpeed ("Grid Speed", Float) = 0.6
        _GridDepth ("Grid Depth", Float) = 0.5
        _GridDensity ("Grid Density", Float) = 3
        _Dim ("Dim", Range(0, 1)) = 0.55
        _Aspect ("Aspect", Float) = 1.777
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Background" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Synthwave"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _SkyTop, _SkyHorizon, _GroundColor, _GridColor, _SunTop, _SunBottom;
                float _HorizonY, _SunRadius, _GridSpeed, _GridDepth, _GridDensity, _Dim, _Aspect;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert (Attributes i)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv;
                return o;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                float t = _Time.y;

                float skyT = saturate((uv.y - _HorizonY) / max(1.0 - _HorizonY, 1e-3));
                float3 sky = lerp(_SkyHorizon.rgb, _SkyTop.rgb, pow(skyT, 0.55));

                float2 sp = float2(uv.x * _Aspect, uv.y) * 70.0;
                float2 cell = floor(sp);
                float h = Hash21(cell);
                float2 jitter = (float2(Hash21(cell + 3.1), Hash21(cell + 7.7)) - 0.5) * 0.6;
                float2 f = frac(sp) - 0.5 - jitter;
                float star = step(0.975, h) * smoothstep(0.12, 0.0, length(f));
                star *= 0.55 + 0.45 * sin(t * (2.0 + h * 4.0) + h * 40.0);
                sky += star * skyT * 1.2;

                float2 sunC = float2(0.5, _HorizonY + _SunRadius * 0.6);
                float2 d = float2((uv.x - sunC.x) * _Aspect, uv.y - sunC.y);
                float r = length(d);
                float sy = saturate((uv.y - (sunC.y - _SunRadius)) / (2.0 * _SunRadius));
                float band = frac(sy * 11.0 - t * 0.12);
                float gap = saturate(0.6 - sy) * 0.9;
                float stripes = sy > 0.6 ? 1.0 : step(gap, band);
                float aa = fwidth(r) * 1.5 + 1e-4;
                float sunMask = 1.0 - smoothstep(_SunRadius - aa, _SunRadius, r);
                float3 sunCol = lerp(_SunBottom.rgb, _SunTop.rgb, sy);
                sky = lerp(sky, sunCol, sunMask * stripes);
                sky += _SunBottom.rgb * exp(-max(r - _SunRadius, 0.0) * 9.0) * 0.18 * (1.0 - sunMask);

                float gy = max(_HorizonY - uv.y, 1e-4);
                float z = _GridDepth / gy;
                float2 g = float2((uv.x - 0.5) * _Aspect * z * _GridDensity, z + t * _GridSpeed);
                float2 w = max(fwidth(g), 1e-4);
                float2 gl = abs(frac(g - 0.5) - 0.5) / w;
                float lineMask = 1.0 - saturate(min(gl.x, gl.y) - 0.5);
                float fade = saturate(gy * 14.0) * saturate(1.2 - z * 0.03);
                float3 ground = _GroundColor.rgb + _GridColor.rgb * lineMask * fade;

                float isSky = step(_HorizonY, uv.y);
                float3 col = lerp(ground, sky, isSky);
                col += _SkyHorizon.rgb * exp(-abs(uv.y - _HorizonY) * 70.0) * 1.2;

                return half4(col * _Dim, 1.0);
            }
            ENDHLSL
        }
    }
}
