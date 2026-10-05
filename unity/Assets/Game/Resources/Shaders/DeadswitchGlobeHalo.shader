// The planet's atmosphere halo (SPEC-044): a slightly larger sphere drawn from the inside, additive, glowing at
// the limb and fading into space; brighter where the sun is. Fog-free.
Shader "Deadswitch/GlobeHalo"
{
    Properties
    {
        _Color("Color", Color) = (0.22, 0.48, 1, 1)
        _SunDir("Sun direction (world)", Vector) = (0.6, 0.25, 0.75, 0)
        _Strength("Strength", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Halo"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Front

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _SunDir;
                half _Strength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float3 nW = -normalize(i.normalWS);
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.positionWS);
                float facing = saturate(dot(nW, viewDir));
                // only the shell around the planet shows (the planet hides the rest): for a shell 6% larger
                // "facing" runs from 0 at its outer edge to about 0.33 at the planet's edge
                float limb = pow(saturate(facing / 0.33), 5.0);
                float sun = saturate(dot(-nW, normalize(_SunDir.xyz)) * 0.5 + 0.5);
                return half4(_Color.rgb * limb * (0.25 + sun * 1.4) * _Strength, 1.0);
            }
            ENDHLSL
        }
    }
}
