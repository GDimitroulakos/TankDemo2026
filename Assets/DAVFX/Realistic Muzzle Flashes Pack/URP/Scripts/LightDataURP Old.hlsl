#ifndef ADDITIONAL_LIGHT_INCLUDED
#define ADDITIONAL_LIGHT_INCLUDED

void GetMainDirectionalLight_float(out float3 Color)
{
#ifdef SHADERGRAPH_PREVIEW
    Color = 1.0f;
#else
    Light mainLight = GetMainLight();
    Color = mainLight.color;
#endif
}

#endif

void AllAdditionalLights_float(float3 WorldPosition, float3 WorldNormal, float2 CutoffThresholds, out float3 LightColor)
{
    LightColor = 0.0f;

#ifndef SHADERGRAPH_PREVIEW
    int lightCount = GetAdditionalLightsCount();

    for (int i = 0; i < lightCount; ++i)
    {
        Light light = GetAdditionalLight(i, WorldPosition);

        float3 color = dot(light.direction, WorldNormal);
        color = smoothstep(CutoffThresholds.x, CutoffThresholds.y, color);
        color *= light.color;
        color *= light.distanceAttenuation;

        LightColor += color;
    }
#endif
}