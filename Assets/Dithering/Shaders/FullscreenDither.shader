Shader "Custom/FullscreenDither"
{
    Properties
    {
        _MainTex       ("Screen Texture",   2D)     = "white" {}
        _BayerTex      ("Bayer Matrix",     2D)     = "white" {}
        _PaletteTex    ("Palette",          2D)     = "white" {}
        _DitherStrength ("Dither Strength", Range(0, 1)) = 0.15
        _PaletteSize   ("Palette Size",     Float)  = 32
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "FullscreenDither"

            HLSLPROGRAM
            #pragma vertex   FullscreenVert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);    SAMPLER(sampler_MainTex);
            TEXTURE2D(_BayerTex);   SAMPLER(sampler_BayerTex);
            TEXTURE2D(_PaletteTex); SAMPLER(sampler_PaletteTex);

            float _DitherStrength;
            float _PaletteSize;

            float4 _MainTex_TexelSize;

            struct Attributes { uint vertexID : SV_VertexID; };
            struct Varyings   { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings FullscreenVert(Attributes IN)
            {
                Varyings OUT;
                // Full-screen triangle
                OUT.uv  = float2((IN.vertexID << 1) & 2, IN.vertexID & 2);
                OUT.pos = float4(OUT.uv * 2.0 - 1.0, 0.0, 1.0);
                #if UNITY_UV_STARTS_AT_TOP
                OUT.uv.y = 1.0 - OUT.uv.y;
                #endif
                return OUT;
            }

            // Find closest color in the palette texture (1D strip, sampled at y=0.5)
            float3 NearestPaletteColor(float3 color)
            {
                float3 best     = float3(0, 0, 0);
                float  bestDist = 1e9;

                for (int i = 0; i < (int)_PaletteSize; i++)
                {
                    float  u          = (i + 0.5) / _PaletteSize;
                    float3 candidate  = SAMPLE_TEXTURE2D(_PaletteTex, sampler_PaletteTex, float2(u, 0.5)).rgb;
                    float3 diff       = color - candidate;
                    float  dist       = dot(diff, diff);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best     = candidate;
                    }
                }
                return best;
            }

            float4 Frag(Varyings IN) : SV_Target
            {
                float3 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).rgb;

                // Pixel coordinate for Bayer lookup (Bayer texture is 8x8 tiled)
                float2 pixelCoord = IN.uv * _ScreenParams.xy;
                float2 bayerUV    = fmod(pixelCoord, 8.0) / 8.0;
                float  threshold  = SAMPLE_TEXTURE2D(_BayerTex, sampler_BayerTex, bayerUV).r - 0.5;

                // Apply dither offset before palette snapping
                float3 dithered = saturate(color + threshold * _DitherStrength);

                // Snap to nearest palette color
                float3 result = NearestPaletteColor(dithered);

                return float4(result, 1.0);
            }
            ENDHLSL
        }
    }
}
