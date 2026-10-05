// The opening's planet (SPEC-044): no textures from disk. _Map is generated at runtime (OpeningGlobe):
// R land, G city lights, B region order for the blackout, A burn and terrain noise. Night side carries the city
// lights; a thin sunlit crescent, ocean glint and an atmosphere rim give it scale. _Burn lights fires across the
// land, _Dark puts the cities out region by region, and up to 16 blooms (direction in object space, age in
// seconds) flash, ring and glow where the warheads land. Unlit and fog-free: it is drawn in space.
Shader "Deadswitch/Globe"
{
    Properties
    {
        _Map("Map (generated)", 2D) = "black" {}
        _SunDir("Sun direction (world)", Vector) = (0.6, 0.25, 0.75, 0)
        _Lights("City lights", Float) = 1
        _Dark("Blackout progress", Float) = 0
        _Burn("Burn", Float) = 0
        _Embers("Fires sunk to embers", Float) = 0
        _Atmos("Atmosphere", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Globe"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_Map);
            SAMPLER(sampler_Map);

            CBUFFER_START(UnityPerMaterial)
                float4 _Map_ST;
                float4 _SunDir;
                float _Lights;
                float _Dark;
                float _Burn;
                float _Embers;
                float _Atmos;
            CBUFFER_END

            // blooms: xyz direction (object space), w age in seconds (negative = unused)
            float4 _GlobeBlooms[16];

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalOS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalOS = v.normalOS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalOS);
                float2 uv = float2(atan2(n.z, n.x) / (2.0 * PI) + 0.5, asin(clamp(n.y, -1.0, 1.0)) / PI + 0.5);
                float4 m = SAMPLE_TEXTURE2D_LOD(_Map, sampler_Map, uv, 0);
                float land = m.r;

                float3 nW = normalize(i.normalWS);
                float3 sunDir = normalize(_SunDir.xyz);
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.positionWS);
                float sun = dot(nW, sunDir);
                float day = smoothstep(-0.06, 0.3, sun);
                float night = 1.0 - smoothstep(-0.2, 0.08, sun);

                // surface: deep ocean, dry olive and ochre land, a little ice at the poles
                float3 ocean = float3(0.006, 0.02, 0.045);
                float3 ground = lerp(float3(0.06, 0.075, 0.045), float3(0.2, 0.17, 0.11), m.a);
                float ice = smoothstep(0.82, 0.92, abs(n.y));
                float3 albedo = lerp(ocean, ground, land);
                albedo = lerp(albedo, float3(0.55, 0.6, 0.65), ice * 0.8);
                float3 col = albedo * (0.015 + (day * 1.15));

                // the sun's glint on the water
                float3 h = normalize(viewDir + sunDir);
                col += (1.0 - land) * pow(saturate(dot(nW, h)), 80.0) * day * float3(1.0, 0.86, 0.62) * 0.9;

                // cities: warm points on the night side; the blackout takes them region by region
                float alive = saturate((m.b - _Dark) * 10.0);
                col += m.g * _Lights * alive * night * float3(1.0, 0.72, 0.38) * 2.6;

                // the land burns in patches (ash between them), then sinks to embers
                float burn = saturate(((m.a - (1.0 - (0.5 * _Burn))) * 6.0)) * land * step(0.001, _Burn);
                float ash = saturate(_Burn * 1.5) * land;
                col *= 1.0 - (0.6 * ash);
                float flick = 0.7 + (0.3 * sin((_Time.y * 6.0) + (m.b * 60.0)));
                col += burn * flick * lerp(float3(1.0, 0.36, 0.08) * 1.6, float3(0.45, 0.07, 0.02) * 0.5, _Embers);

                // warheads: a white flash, a ring running out across the ground, a lasting glow
                [unroll]
                for (int k = 0; k < 16; k++)
                {
                    float4 b = _GlobeBlooms[k];
                    if (b.w < 0.0)
                    {
                        continue;
                    }

                    float d = acos(clamp(dot(n, normalize(b.xyz)), -1.0, 1.0));
                    float age = b.w;
                    float flash = exp(-age * 3.0) * exp(-d * d * 500.0) * 7.0;
                    float ring = exp(-pow((d - (age * 0.035)) * 90.0, 2.0)) * exp(-age * 1.6) * 0.6;
                    float glow = exp(-d * d * 420.0) * exp(-age * 0.3) * (1.0 - _Embers * 0.7);
                    col += (flash * float3(1.0, 0.95, 0.88)) + (ring * float3(1.0, 0.75, 0.5)) + (glow * float3(1.0, 0.32, 0.06) * 1.2);
                }

                // the thin blue air at the limb, brighter on the day side
                float rim = pow(1.0 - saturate(dot(nW, viewDir)), 3.5);
                col += rim * float3(0.22, 0.48, 1.0) * (0.12 + (day * 0.9)) * _Atmos;
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
