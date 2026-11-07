Shader "LoneRetr0/NeonUnlit"
{
    Properties
    {
        _Intensity ("HDR Intensity", Range(0, 20)) = 4
        _CoreBoost ("Core Boost", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "NeonUnlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Intensity;
                float _CoreBoost;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color       : COLOR;
                float2 uv          : TEXCOORD0;
            };

            Varyings vert (Attributes i)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(i.positionOS.xyz);
                o.color = i.color;
                o.uv = i.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float edge = 1.0 - abs(i.uv.y * 2.0 - 1.0);
                float body = smoothstep(0.0, 0.65, edge);
                float core = smoothstep(0.7, 1.0, edge);

                float fade = i.color.a;
                float3 col = i.color.rgb * body * _Intensity * fade;
                col += core * _CoreBoost * _Intensity * fade;
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
