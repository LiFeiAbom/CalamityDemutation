// 燃烧IV（HellfireIV）· 精灵溶解着色器
// 移植自灾厄现代版的 Effects/MiracleBlight.fx（那条链路自身注释说明：基于神吞死亡动画，源自 ExampleMod）。
// 本工程改动（用户 2026-10-17 指定）：
//   ① 文件名与技法名改成本工程的 HellfireIV；
//   ② 发光替换色不再硬编码荧光绿 (0.4902, 1, 0)，改为读取 uColor —— 调用方传"燃烧橙红"，
//      所以同一支着色器换个 UseColor() 就能换风格，不用重编译。
// 逻辑与原版一致：噪声贴图随时间漂移采样，噪点超过阈值就整块抹掉（alpha = 0），
// 略低一档的像素替换成发光色；阈值 = uOpacity + 三个 cos/sin 高频抖动项（原版注释称仿 Weierstrass 函数）。

sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float3 uColor;
float3 uSecondaryColor;
float uOpacity : register(C0);
float uSaturation;
float uRotation;
float uTime;
float4 uSourceRect;
float2 uWorldPosition;
float uDirection;
float3 uLightSource;
float2 uImageSize0;
float2 uImageSize1;
float4 uShaderSpecificData;

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    if (!any(color))
        return color;

    // 时间漂移：让精灵"滑过"噪声贴图
    float2 drift = float2(5 * sin(0.04165 * uTime), 1.1 * uTime);
    float2 noiseMapTexCoords = coords + drift;
    float4 noiseColor = tex2D(uImage1, noiseMapTexCoords);

    // 高频抖动出来的阈值：擦除线在贴着身体爬
    float flickerOne = 0.05 * cos(7 * uTime);
    float flickerTwo = 0.06 * cos(31 * uTime);
    float flickerThree = 0.04 * sin(167 * uTime);
    float fullErasureThreshold = uOpacity + flickerOne + flickerTwo + flickerThree;
    float glowThreshold = fullErasureThreshold - 0.1;

    // 噪点越过阈值：这个像素被彻底啃掉
    if (noiseColor.r > fullErasureThreshold)
    {
        color.rgba = 0;
    }
    // 否则若越过了略低一档的阈值：替换成发光色（燃烧橙红由 uColor 传入）
    else if (noiseColor.r > glowThreshold)
    {
        color = float4(uColor, 1) * color.a;
    }

    return color;
}

technique HellfireIV
{
    pass HellfireIVPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
