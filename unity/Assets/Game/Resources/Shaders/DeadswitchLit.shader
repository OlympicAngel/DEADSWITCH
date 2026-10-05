// URP lit shader for Deadswitch.Art meshes: the procedural salvage material (SalvageCommon.hlsl, shared with
// tools/basepreview) layers chipped paint, rust, dirt, dust, rain streaks, wetness and bump from world noise
// and vertex masks (R = AO, G = worn edge, B = variation). Forward / Forward+; shadow and depth passes reuse URP Lit.
Shader "Deadswitch/VertexColorLit"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _BaseMap("Base Map", 2D) = "white" {}
        _Metallic("Metallic", Range(0, 1)) = 0
        _Smoothness("Smoothness", Range(0, 1)) = 0.3
        [HDR] _EmissionColor("Emission", Color) = (0, 0, 0, 0)
        _BareColor("Bare Substrate", Color) = (0.25, 0.25, 0.25, 1)
        _Wear("Chip Rust Dirt Streak", Vector) = (0, 0, 0, 0)
        _WearB("Bump Scale Ground Procedural", Vector) = (0, 1, 0, 0)
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
        [HideInInspector] _DsDamage("Damage glow (rgb, flat share)", Vector) = (0, 0, 0, 0)
        [HideInInspector] _SrcBlend("Src", Float) = 1
        [HideInInspector] _DstBlend("Dst", Float) = 0
        [HideInInspector] _ZWrite("ZWrite", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            // opaque by default; water blends (base colour alpha) so the ground shows through a puddle
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "SalvageCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Metallic;
                half _Smoothness;
                half4 _EmissionColor;
                half _Cutoff;
                half4 _BareColor;
                float4 _Wear;
                float4 _WearB;
                float4 _DsDamage;
            CBUFFER_END

            // time-of-day emission scale (BaseView): lamps and windows glow more at night
            half _DsEmissionScale;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 color : COLOR;
                half fogFactor : TEXCOORD2;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(v.normalOS);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = n.normalWS;
                o.color = v.color;
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float3 n = NormalizeNormalPerPixel(i.normalWS);
                float3 albedo = _BaseColor.rgb * i.color.r;
                float rough = 1.0 - _Smoothness;
                float metal = _Metallic;
                if (_WearB.w > 0.5)
                {
                    float h;
                    ds_salvage(i.positionWS, n, i.color.rgb, _BaseColor.rgb, _BareColor.rgb, _Metallic, 1.0 - _Smoothness,
                               _Wear.x, _Wear.y, _Wear.z, _Wear.w, _WearB.x, _WearB.y, _WearB.z, albedo, rough, metal, h);

                    // bump from screen-space height derivatives (Mikkelsen), matching the preview
                    float3 dpdx = ddx(i.positionWS);
                    float3 dpdy = ddy(i.positionWS);
                    float dhx = ddx(h);
                    float dhy = ddy(h);
                    float3 r1 = cross(dpdy, n);
                    float3 r2 = cross(n, dpdx);
                    float det = dot(dpdx, r1);
                    float3 g = sign(det) * (dhx * r1 + dhy * r2) * 0.06;
                    float3 nn = abs(det) * n - g;
                    if (abs(det) > 1e-12 && dot(nn, nn) > 1e-20)
                    {
                        n = normalize(nn);
                    }
                }

                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.positionCS = i.positionCS;
                inputData.normalWS = n;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                inputData.fogCoord = i.fogFactor;
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData s = (SurfaceData)0;
                s.albedo = albedo;
                s.metallic = metal;
                s.smoothness = 1.0 - rough;
                s.occlusion = 1;
                s.alpha = 1;
                s.emission = _EmissionColor.rgb * _DsEmissionScale;
                // damaged facility (BaseView, per renderer): red rim that reads as an outline glow, plus a flat tint
                float rim = 1.0 - saturate(dot(n, inputData.viewDirectionWS));
                s.emission += _DsDamage.rgb * (_DsDamage.w + rim * rim);
                s.normalTS = half3(0, 0, 1);

                half4 c = UniversalFragmentPBR(inputData, s);
                c.rgb = MixFog(c.rgb, inputData.fogCoord);
                c.a = _BaseColor.a;
                return c;
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }

    FallBack "Universal Render Pipeline/Lit"
}
