#ifndef NINTHAGE_ARCANE_SLATE_COMMON_INCLUDED
#define NINTHAGE_ARCANE_SLATE_COMMON_INCLUDED

// ArcaneSlate shared geometry uses pixel-space dimensions. When a material has
// not supplied its RectTransform size, recover the dimensions from the UV
// derivatives so the same shader contract also works in previews and effects.
float2 ArcaneSlateResolveRectSizePx(float4 rectSize, float2 uv)
{
    if (rectSize.x > 1.0 && rectSize.y > 1.0)
        return rectSize.xy;

    float2 gdx = ddx(uv);
    float2 gdy = ddy(uv);
    float det = gdx.x * gdy.y - gdx.y * gdy.x;
    float invDet = 1.0 / (abs(det) + 1e-6);
    float widthPx = length(float2(gdy.y, -gdx.y)) * invDet;
    float heightPx = length(float2(-gdy.x, gdx.x)) * invDet;
    return float2(max(widthPx, 1.0), max(heightPx, 1.0));
}

float ArcaneSlateAntiAliasWidth(float sdf)
{
    return max(1.0, fwidth(sdf));
}

float ArcaneSlateClampCornerRadiusPx(float radius, float minDimension)
{
    return min(max(0.0, radius), max(0.0, minDimension * 0.5 - 1.0));
}

float ArcaneSlateSdRoundRect(float2 p, float2 halfExtents, float radius)
{
    float2 q = abs(p) - halfExtents;
    float outside = length(max(q, 0.0));
    float inside = min(max(q.x, q.y), 0.0);
    return outside + inside - radius;
}

float ArcaneSlateSdBeveledRect(float2 p, float2 halfExtents, float bevel)
{
    float2 q = abs(p) - halfExtents + bevel;
    return length(max(q, 0.0)) - bevel;
}

float ArcaneSlateHash(float2 p)
{
    return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
}

float ArcaneSlateValueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);

    float a = ArcaneSlateHash(i);
    float b = ArcaneSlateHash(i + float2(1.0, 0.0));
    float c = ArcaneSlateHash(i + float2(0.0, 1.0));
    float d = ArcaneSlateHash(i + float2(1.0, 1.0));
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

float ArcaneSlateCenterLineMask(float coordinate, float lineWidth)
{
    float distanceFromCenterPx = abs((coordinate - 0.5) * max(1.0, 1.0 / fwidth(coordinate)));
    return smoothstep(lineWidth + 1.0, lineWidth - 1.0, distanceFromCenterPx);
}

float ArcaneSlateEdgeFade(float coordinate, float fadeStart, float fadeEnd)
{
    return smoothstep(fadeEnd, fadeStart, coordinate)
         * smoothstep(1.0 - fadeEnd, 1.0 - fadeStart, coordinate);
}

#endif
