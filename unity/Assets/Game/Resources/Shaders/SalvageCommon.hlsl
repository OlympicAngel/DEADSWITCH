#ifndef DS_SALVAGE_COMMON
#define DS_SALVAGE_COMMON
// Shared salvage-material math (doc 11 Master art direction). This file is the single source: the Unity
// shader (DeadswitchLit.shader, HLSL) and tools/basepreview (GLSL) both include it; it uses only syntax that
// is valid in both after the preview's #define shim (float2/3/4 -> vec2/3/4, lerp -> mix, saturate -> clamp).
float ds_hash(float3 p)
{
    p = frac(p * 0.3183099 + float3(0.11, 0.17, 0.13));
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

float ds_noise(float3 x)
{
    float3 i = floor(x);
    float3 f = frac(x);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(lerp(ds_hash(i + float3(0, 0, 0)), ds_hash(i + float3(1, 0, 0)), f.x),
                     lerp(ds_hash(i + float3(0, 1, 0)), ds_hash(i + float3(1, 1, 0)), f.x), f.y),
                lerp(lerp(ds_hash(i + float3(0, 0, 1)), ds_hash(i + float3(1, 0, 1)), f.x),
                     lerp(ds_hash(i + float3(0, 1, 1)), ds_hash(i + float3(1, 1, 1)), f.x), f.y), f.z);
}

float ds_fbm(float3 p)
{
    float v = 0.0;
    float a = 0.5;
    for (int i = 0; i < 4; i++)
    {
        v += a * ds_noise(p);
        p = p * 2.03 + float3(1.7, 9.2, 5.1);
        a *= 0.5;
    }
    return v;
}

// Inputs: world position, world normal, vertex masks (R = AO, G = worn edge, B = variation), material params.
// Outputs albedo (linear), roughness, metallic and a bump height (scaled by bump).
void ds_salvage(float3 wp, float3 n, float3 masks, float3 baseCol, float3 bareCol, float metallicIn, float roughIn,
                float chip, float rust, float dirt, float streak, float bump, float scale, float ground,
                out float3 albedo, out float rough, out float metal, out float height)
{
    float3 p = wp * scale;
    float n1 = ds_fbm(p * 0.35);
    float n2 = ds_fbm(p * 1.7 + float3(3.1, 7.7, 1.3));
    float n3 = ds_noise(p * 4.0);
    float ao = masks.x;
    float up = saturate(n.y);
    float vert = 1.0 - abs(n.y);

    // macro variation (meters-wide): breaks up uniform panels so large forms read as weathered at gameplay zoom
    float macro = ds_fbm(wp * 0.11 + float3(11.0, 3.0, 7.0));
    albedo = baseCol * (0.86 + 0.28 * masks.z) * (0.82 + 0.36 * n2) * (0.72 + 0.56 * macro);
    rough = roughIn;
    metal = metallicIn;

    if (ground > 0.5)
    {
        // three grounds: wet dark mud (smooth, reflective), damp soil (base), dry gravel (light, speckled)
        float3 soil = baseCol * (0.72 + 0.5 * n1) * (0.92 + 0.16 * n2);
        float wet = smoothstep(0.5, 0.6, ds_fbm(float3(wp.x, 0.0, wp.z) * 0.12 + float3(4.0, 0.0, 2.0)));
        float dry = smoothstep(0.48, 0.68, ds_fbm(float3(wp.x, 0.0, wp.z) * 0.07 + float3(9.0, 0.0, 5.0)) + n2 * 0.15) * (1.0 - wet);
        float speck = smoothstep(0.55, 0.75, ds_noise(wp * 9.0));
        float3 gravel = float3(0.36, 0.35, 0.32) * (0.8 + 0.45 * speck);
        albedo = lerp(soil, gravel, dry * 0.85) * ao;
        albedo *= lerp(1.0, 0.42, wet);
        rough = lerp(lerp(0.88, 0.97, dry), 0.12, wet);
        metal = 0.0;
        height = (n2 * 0.6 + n3 * 0.2 + speck * dry * 0.5) * bump * (1.0 - wet);
        return;
    }

    // chipped paint revealing the bare substrate (edges first)
    float chipMask = smoothstep(0.62, 0.68, n3 * 0.55 + n2 * 0.45 + masks.y * 0.32) * chip;
    albedo = lerp(albedo, bareCol * (0.8 + 0.4 * n3), chipMask);
    metal = lerp(metal, 0.55, chipMask * step(0.2, metallicIn + 0.01));
    rough = lerp(rough, 0.55, chipMask);

    // rust blooms: low on the object, along edges, in noise patches
    float rustMask = smoothstep(0.55, 0.78, n1 + (1.0 - ao) * 0.45 + masks.y * 0.12 + n3 * 0.15) * rust;
    float3 rustCol = lerp(float3(0.20, 0.10, 0.05), float3(0.36, 0.17, 0.08), n3);
    albedo = lerp(albedo, rustCol, rustMask);
    rough = lerp(rough, 0.9, rustMask);
    metal *= 1.0 - rustMask;

    // rain streaks and soot on vertical faces; rusty run-off below metal
    float streaks = smoothstep(0.4, 0.8, ds_noise(float3(wp.x * 7.0, wp.y * 0.45, wp.z * 7.0))) * vert * streak;
    float3 runoff = lerp(float3(0.12, 0.11, 0.10), float3(0.25, 0.13, 0.07), saturate(rust * 1.5));
    albedo = lerp(albedo, albedo * runoff * 3.0, 0.5 * streaks);

    // dirt and mud splash from the ground up (world height), dust settling on top faces
    float low = 1.0 - smoothstep(0.0, 1.3, wp.y + (n2 - 0.5) * 0.6);
    float dirtMask = saturate((1.0 - ao) * 1.6 + (n1 - 0.55) * 0.8 + low * vert * 0.9) * dirt;
    albedo = lerp(albedo, float3(0.16, 0.13, 0.10), dirtMask * 0.8);
    float dust = smoothstep(0.55, 0.95, up) * dirt * (0.25 + 0.5 * n2);
    albedo = lerp(albedo, float3(0.30, 0.28, 0.24), dust);
    rough = lerp(rough, 0.95, saturate(dirtMask + dust));

    albedo *= lerp(1.0, ao, 0.75);
    height = (n2 * 0.6 + n3 * 0.4 + chipMask * 0.3) * bump;
}
#endif
