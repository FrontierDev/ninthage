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

float ArcaneSlateSdAsymmetricChamferRect(
    float2 p,
    float2 halfExtents,
    float leftChamferPx,
    float rightChamferPx)
{
    float2 b = max(halfExtents, float2(0.5, 0.5));
    float leftChamfer = clamp(leftChamferPx, 0.0, b.y);
    float rightChamfer = clamp(rightChamferPx, 0.0, b.y);
    const float diagonalScale = 0.70710678118;

    float top = p.y - b.y;
    float bottom = -p.y - b.y;
    float left = -p.x - b.x;
    float right = p.x - b.x;
    float leftTop = (-b.x - b.y + leftChamfer - p.x + p.y) * diagonalScale;
    float leftBottom = (-b.x - b.y + leftChamfer - p.x - p.y) * diagonalScale;
    float rightTop = (p.x + p.y - b.x - b.y + rightChamfer) * diagonalScale;
    float rightBottom = (p.x - p.y - b.x - b.y + rightChamfer) * diagonalScale;

    return max(max(max(top, bottom), max(left, right)),
               max(max(leftTop, leftBottom), max(rightTop, rightBottom)));
}

float ArcaneSlateSdPointedBar(
    float2 p,
    float2 halfExtents,
    float endPointDepthPx,
    float endShoulderInsetPx)
{
    float2 b = max(halfExtents, float2(0.5, 0.5));
    float pointDepth = clamp(endPointDepthPx, 0.001, max(b.x - 0.5, 0.001));
    float shoulderInset = clamp(endShoulderInsetPx, 0.0, max(b.y - 0.5, 0.0));
    float bodyHalfWidth = max(0.5, b.x - pointDepth);
    float triangleHalfHeight = max(0.5, b.y - shoulderInset);
    float diagonalLength = max(length(float2(pointDepth, triangleHalfHeight)), 0.001);

    float body = max(abs(p.x) - bodyHalfWidth, abs(p.y) - b.y);

    float leftUpper = (pointDepth * p.y - triangleHalfHeight * (p.x + b.x)) / diagonalLength;
    float leftLower = (-pointDepth * p.y - triangleHalfHeight * (p.x + b.x)) / diagonalLength;
    float leftPoint = max(max(-p.x - b.x, p.x + b.x - pointDepth),
                          max(leftUpper, leftLower));

    float rightUpper = (pointDepth * p.y + triangleHalfHeight * (p.x - b.x)) / diagonalLength;
    float rightLower = (-pointDepth * p.y + triangleHalfHeight * (p.x - b.x)) / diagonalLength;
    float rightPoint = max(max(p.x - b.x, -p.x + b.x - pointDepth),
                           max(rightUpper, rightLower));

    return min(body, min(leftPoint, rightPoint));
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
