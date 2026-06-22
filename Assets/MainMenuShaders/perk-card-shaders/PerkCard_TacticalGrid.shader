// DUAL RUSH — Perk Card Hover FX  ·  3/5  TACTICAL GRID
// A dim tactical grid that ignites toward the accent color on hover, with cells
// near the cursor flaring up and a slow pulse. UI/Canvas compatible.
// Drive _Hover / _Mouse / _FxTime / _Aspect with PerkCardFX.cs.
Shader "DualRush/PerkCard/TacticalGrid"
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
        _Glow ("Glow Strength", Range(0,3)) = 1.3
        _GridCells ("Grid Cells", Float) = 9
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
            float _Hover, _Aspect, _FxTime, _Glow, _GridCells, _DitherScale;
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

                // grid (aspect-corrected square cells)
                float2 gp = frac(uv * float2(_GridCells * _Aspect, _GridCells));
                float2 gd = abs(gp - 0.5);
                float line = smoothstep(0.40, 0.5, max(gd.x, gd.y));

                // cursor flare
                float2 d = float2((uv.x - _Mouse.x) * _Aspect, uv.y - _Mouse.y);
                float dist = length(d);
                float hi = smoothstep(0.55, 0.0, dist) * _Hover;
                float pulse = 0.6 + 0.4 * sin(_FxTime * 3.0);

                float3 gridCol = lerp(_AccentColor.rgb * 0.22, _AccentColor.rgb, _Hover);
                float3 col = _BaseColor.rgb;
                col += gridCol * line * (0.22 + hi * 1.3 * pulse) * _Glow;
                col += _AccentColor.rgb * hi * 0.45;
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
