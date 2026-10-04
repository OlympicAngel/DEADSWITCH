// Procedural fire and smoke particles for battle scars (SPEC-018, doc 11): no textures. A soft, noisy round
// puff from the particle UVs times the particle color. Smoke blends (lit by _DsSmokeLight, set by BaseView from
// the time of day); fire and embers add. _SrcBlend/_DstBlend pick the mode per material.
Shader "Deadswitch/Particle"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        _Softness("Edge softness", Float) = 1.6
        _Noise("Noise", Float) = 0.35
        _Lit("Lit by time of day (smoke)", Float) = 1
        [HideInInspector] _SrcBlend("Src", Float) = 5
        [HideInInspector] _DstBlend("Dst", Float) = 10
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Particle"
            Tags { "LightMode" = "UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Softness;
                half _Noise;
                half _Lit;
                half _SrcBlend;
                half _DstBlend;
            CBUFFER_END

            half4 _DsSmokeLight;

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }

            Varyings Vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.color = v.color * _Color;
                o.uv = v.uv;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 d = i.uv * 2.0 - 1.0;
                float r = length(d);
                // billowing edge: noise from world position so neighbouring puffs never repeat
                float n = ValueNoise(d * 2.3 + i.positionWS.xy * 0.7 + i.positionWS.z * 0.37);
                float shape = saturate(1.0 - pow(saturate(r + (n - 0.5) * _Noise), _Softness));
                half3 rgb = i.color.rgb * lerp(1.0, _DsSmokeLight.rgb, _Lit);
                half a = shape * i.color.a;
                // additive materials (fire) premultiply; blended ones (smoke) use alpha
                return half4(rgb * lerp(1.0, a, step(_DstBlend, 1.5)), a);
            }
            ENDHLSL
        }
    }
}
