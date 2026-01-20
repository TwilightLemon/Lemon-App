sampler2D input : register(s0);

float HighlightPos : register(c0);
float HighlightWidth : register(c1);
float4 HighlightColor : register(c2);
float UseAdditive : register(c3); // 0 = lerp, 1 = additive
float HighlightIntensity : register(c4);

float4 main(float2 uv : TEXCOORD) : COLOR
{
    float4 color = tex2D(input, uv);
    float d = max(0, uv.x - HighlightPos);
    float glow = saturate(1 - d / HighlightWidth);
    glow = glow * glow;
    float intensity = glow * HighlightColor.a * HighlightIntensity;

    float3 lerpResult = lerp(color.rgb, HighlightColor.rgb, intensity);
    float lerpAlpha = lerp(color.a, 1.0, intensity);
    float3 additiveResult = color.rgb + HighlightColor.rgb * intensity;

    color.rgb = lerp(lerpResult, additiveResult, UseAdditive);
    color.a = lerp(lerpAlpha, color.a, UseAdditive);

    return color;
}