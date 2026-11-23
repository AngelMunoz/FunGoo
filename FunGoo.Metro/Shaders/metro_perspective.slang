#include "goo_effect.slang"

float4 gooEffect(float2 uv, float4 source, float4 backdrop)
{
    float4 p0 = gooParameter(0);
    float4 p1 = gooParameter(1);

    float angleX = p0.x;
    float angleY = p0.y;
    float perspectiveDepth = max(p0.z, 0.25);
    float2 pivot = float2(p0.w, p1.x);
    float opacity = clamp(p1.y, 0.0, 1.0);
    float backfaceDim = clamp(p1.z, 0.0, 1.0);
    float edgeFeather = max(p1.w, 0.0);

    float cx = cos(angleX);
    float sx = sin(angleX);
    float cy = cos(angleY);
    float sy = sin(angleY);

    // Orthonormal basis of R = Rx(angleX) * Ry(angleY) around pivot at z = 0.
    float3 ex = float3(cy, sx * sy, -cx * sy);
    float3 ey = float3(0.0, cx, sx);
    float3 normal = float3(sy, -sx * cy, cx * cy);

    float3 rayOrigin = float3(0.0, 0.0, perspectiveDepth);
    float3 rayDir = float3(uv - pivot, -perspectiveDepth);
    float denom = dot(rayDir, normal);
    float safeDenom = abs(denom) > 1e-5 ? denom : -1.0;
    float t = -(perspectiveDepth * normal.z) / safeDenom;

    float3 hit = rayOrigin + t * rayDir;
    float2 sampleUv = float2(dot(hit, ex), dot(hit, ey)) + pivot;

    float validRay = (abs(denom) > 1e-5 && t > 0.0) ? 1.0 : 0.0;
    float inBounds = step(0.0, sampleUv.x) * step(sampleUv.x, 1.0)
        * step(0.0, sampleUv.y) * step(sampleUv.y, 1.0);

    float2 clampedUv = clamp(sampleUv, float2(0.0, 0.0), float2(1.0, 1.0));
    float4 mapping = gooPrimitive().params;
    float4 sampled = gooSourceTexture.Sample(mapping.xy + clampedUv * mapping.zw);

    float featherWidth = max(edgeFeather, 1e-6);
    float2 edgeDist = min(clampedUv, float2(1.0, 1.0) - clampedUv);
    float edgeMask = edgeFeather > 0.0
        ? smoothstep(0.0, featherWidth, min(edgeDist.x, edgeDist.y))
        : 1.0;

    float facing = clamp(abs(normal.z), 0.0, 1.0);
    float shade = lerp(1.0 - backfaceDim, 1.0, facing);
    float alphaScale = opacity * validRay * inBounds * edgeMask;

    return float4(sampled.rgb * shade * alphaScale, sampled.a * alphaScale);
}
