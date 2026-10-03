// Graphic-novel grade for battle report stills (SPEC-006 rule 6): exposure + ACES, posterized light, Sobel ink
// lines, 45-degree halftone in the shadows, lamps kept bright, warm paper tint. Used with Graphics.Blit on the
// report camera's HDR target. Mirrors the "novel" pass in tools/basepreview/page.html.
Shader "Deadswitch/Novel"
{
    Properties
    {
        _MainTex("Source", 2D) = "black" {}
        _Exposure("Exposure", Float) = 2.2
        _Ink("Ink", Float) = 0.85
        _Levels("Levels", Float) = 4
        _DotSize("Dot size (px)", Float) = 7
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Exposure;
            float _Ink;
            float _Levels;
            float _DotSize;

            struct V2F
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            V2F Vert(appdata_img v)
            {
                V2F o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            float3 Aces(float3 x)
            {
                return saturate((x * (2.51 * x + 0.03)) / (x * (2.43 * x + 0.59) + 0.14));
            }

            float3 Graded(float2 uv)
            {
                return Aces(tex2D(_MainTex, uv).rgb * _Exposure);
            }

            float Luma(float3 c)
            {
                return dot(c, float3(0.299, 0.587, 0.114));
            }

            float L(float2 uv, float2 o)
            {
                return Luma(Graded(uv + o * _MainTex_TexelSize.xy));
            }

            float4 Frag(V2F i) : SV_Target
            {
                float3 c = Graded(i.uv);
                float l = Luma(c);
                float gx = -L(i.uv, float2(-1, 1)) - 2 * L(i.uv, float2(-1, 0)) - L(i.uv, float2(-1, -1)) + L(i.uv, float2(1, 1)) + 2 * L(i.uv, float2(1, 0)) + L(i.uv, float2(1, -1));
                float gy = -L(i.uv, float2(-1, -1)) - 2 * L(i.uv, float2(0, -1)) - L(i.uv, float2(1, -1)) + L(i.uv, float2(-1, 1)) + 2 * L(i.uv, float2(0, 1)) + L(i.uv, float2(1, 1));
                float edge = smoothstep(0.10, 0.32, length(float2(gx, gy)));

                float q = floor(pow(l, 0.8) * _Levels + 0.5) / _Levels;
                float3 col = lerp(q.xxx, c * (q / max(l, 0.002)), 0.42);

                float2 px = i.uv * _MainTex_TexelSize.zw;
                float2 hp = float2(0.7071 * px.x - 0.7071 * px.y, 0.7071 * px.x + 0.7071 * px.y) / _DotSize;
                float d = length(frac(hp) - 0.5);
                float shade = saturate(1.0 - l * 2.6);
                col *= 1.0 - 0.42 * step(d, shade * 0.6);

                float glow = smoothstep(0.75, 1.0, max(c.r, max(c.g, c.b)));
                col = lerp(col, c * 1.15, glow * 0.7);
                col *= 1.0 - edge * _Ink;
                col = lerp(col, col * float3(1.04, 1.0, 0.9), 0.5);
                return float4(col, 1);
            }
            ENDHLSL
        }
    }
}
