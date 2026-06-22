// DUAL RUSH — Perk Card Hover FX  ·  2/5  SCAN HOLOGRAM
// CRT scanlines with a bright sweep band that runs up the card on hover, plus a
// hologram flicker tint toward the accent color. UI/Canvas compatible.
// Drive _Hover / _Mouse / _FxTime / _Aspect with PerkCardFX.cs.
Shader "DualRush/PerkCard/ScanHologram"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BaseColor ("Base (idle)", Color) = (0.105,0.121,0.149,1)
        _AccentColor ("Accent", Color) = (1,0.416,0.078,1)
        _Hover ("Hover", Range(0,1)) = 0
        _Mouse ("Mouse UV", Vector) = (0.5,0.5,0,0)
        _Aspect ("Aspect", Float) = 0.7
        _FxTime ("Fx Time", Float) = 0
        _Glow ("Glow Strength", Range(0,3)) = 1.4
        _ScanDensity ("Scanline Density", Float) = 220
        _DitherScale ("Dither Scale", Float) = 0.6

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off  Lighting Off  ZWrite Off  ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t { float4 vertex:POSITION; float4 color:COLOR; float2 texcoord:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 texcoord:TEXCOORD0; float4 worldPosition:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };

            sampler2D _MainTex; float4 _MainTex_ST;
            fixed4 _Color, _BaseColor, _AccentColor, _TextureSampleAdd;
            float _Hover, _Aspect, _FxTime, _Glow, _ScanDensity, _DitherScale;
            float4 _Mouse, _ClipRect;

            static const float4x4 BAYER = float4x4(0,8,2,10, 12,4,14,6, 3,11,1,9, 15,7,13,5) / 16.0;
            float bayer4(float2 p){ int2 i = (int2)fmod(abs(floor(p)), 4.0); return BAYER[i.y][i.x]; }

            v2f vert(appdata_t v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i):SV_Target
            {
                float2 uv = i.texcoord;
                fixed4 tex = tex2D(_MainTex, uv) + _TextureSampleAdd;
                float bd = bayer4(i.worldPosition.xy * _DitherScale);

                // scanlines — subtle when idle, stronger on hover
                float scan = 1.0 - (0.06 + 0.12 * _Hover) * (0.5 + 0.5 * sin(uv.y * _ScanDensity - _FxTime * 4.0));

                // sweep band rising up the card
                float sweepPos = frac(_FxTime * 0.35);
                float sweep = smoothstep(0.11, 0.0, abs(uv.y - sweepPos)) * _Hover;

                // hologram flicker tint
                float holo = 0.5 + 0.5 * sin(_FxTime * 8.0 + uv.y * 26.0);

                float3 tint = lerp(_BaseColor.rgb, _BaseColor.rgb * 1.08 + _AccentColor.rgb * 0.34, _Hover);
                float3 col = tint * scan;
                col += _AccentColor.rgb * sweep * _Glow;
                col += _AccentColor.rgb * 0.10 * _Hover * holo;
                col += (bd - 0.5) * 0.02;

                fixed4 outc;
                outc.rgb = saturate(col) * i.color.rgb;
                outc.a = i.color.a * tex.a;

                #ifdef UNITY_UI_CLIP_RECT
                outc.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(outc.a - 0.001);
                #endif
                return outc;
            }
            ENDCG
        }
    }
}
