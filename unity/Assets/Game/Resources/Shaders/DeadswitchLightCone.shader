// Soft additive light cone under work lamps ("volumetric" practical light, doc 11). Vertex color R fades from
// the lamp to the ground; the rim term keeps silhouettes soft. Mirrors tools/basepreview's cone material.
Shader "Deadswitch/LightCone"
{
    Properties
    {
        _Color("Color", Color) = (1, 0.72, 0.42, 1)
        _Intensity("Intensity", Float) = 0.12
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Cone"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
            CBUFFER_END

            // time-of-day scale (BaseView): cones only show at dusk and night
            half _DsConeScale;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewWS : TEXCOORD1;
                half a : TEXCOORD2;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.viewWS = GetWorldSpaceNormalizeViewDir(p.positionWS);
                o.a = v.color.r;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float rim = pow(saturate(abs(dot(normalize(i.normalWS), normalize(i.viewWS)))), 1.5);
                half a = i.a * i.a * i.a;
                return half4(_Color.rgb * _Intensity * _DsConeScale * a * rim * rim, 1);
            }
            ENDHLSL
        }
    }
}
