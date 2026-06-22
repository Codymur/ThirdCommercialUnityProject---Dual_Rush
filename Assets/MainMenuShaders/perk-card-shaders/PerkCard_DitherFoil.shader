// DUAL RUSH — Perk Card Hover FX  ·  DITHERED GLOW POOL (minimalist)
// Solid dark panel for clean text. An ordered-dither amber glow pools under
// the cursor (echoing the gravity-well background), a dithered edge frame
// breathes on hover, tilt nudges the pool, flick/click burst the bloom.
// Drive _Hover/_Mouse/_FxTime/_Aspect/_Tilt/_Flare/_Click/_Dissolve with the scripts.
//
// CATEGORY GLOW: the glow color is chosen from three category slots by _Category.
// Index convention is LOCKED to PerkSO.PerkType: 0 = Movement, 1 = Combat, 2 = Survival.
// PerkCardFX.SetCategory() maps the enum to these indices by name. Set
// _UseCategory = 0 to instead drive the glow from _AccentColor (SetAccent).
Shader "DualRush/PerkCard/DitherGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BaseColor ("Base (idle panel)", Color) = (0.055,0.05,0.04,1)
        _AccentColor ("Accent (glow)", Color) = (1,0.55,0.12,1)

        // ── Category glow selection ────────────────────────────────────────
        // 0 = Movement, 1 = Combat, 2 = Survival  (matches PerkSO.PerkType order).
        [Header(Category Glow)]
        [Enum(Movement,0,Combat,1,Survival,2)] _Category ("Perk Category", Float) = 0
        [Toggle] _UseCategory ("Use Category Color (else _AccentColor)", Float) = 1
        _MovementColor ("Movement Glow  (#5BD0E0)", Color) = (0.357,0.816,0.878,1)
        _CombatColor   ("Combat Glow    (#EF6B3A)", Color) = (0.937,0.420,0.227,1)
        _SurvivalColor ("Survival Glow  (#F2C14E)", Color) = (0.949,0.757,0.306,1)

        _Hover ("Hover", Range(0,1)) = 0
        _Mouse ("Mouse UV", Vector) = (0.5,0.5,0,0)
        _Aspect ("Aspect", Float) = 0.7
        _FxTime ("Fx Time", Float) = 0

        _Tilt  ("Tilt (xy)", Vector) = (0,0,0,0)
        _Flare ("Flare (flick)", Range(0,1)) = 0
        _Click ("Click Burst", Range(0,1)) = 0
        _Dissolve ("Dissolve", Range(0,1)) = 0

        _GlowRadius  ("Glow Radius", Range(0.1,1.5)) = 0.6
        _GlowStrength("Glow Strength", Range(0,2)) = 1.0
        _IdleGlow    ("Idle Glow", Range(0,1)) = 0.18
        _DitherScale ("Dither Scale", Float) = 1.0
        _EdgeWidth   ("Edge Frame Width", Range(0,0.2)) = 0.04
        _Grain       ("Static Grain", Range(0,0.1)) = 0.025

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
            fixed4 _MovementColor, _CombatColor, _SurvivalColor;
            float _Category, _UseCategory;
            float _Hover, _Aspect, _FxTime, _Flare, _Click, _Dissolve;
            float _GlowRadius, _GlowStrength, _IdleGlow, _DitherScale, _EdgeWidth, _Grain;
            float4 _Mouse, _Tilt, _ClipRect;

            // 4x4 Bayer ordered-dither matrix.
            static const float4x4 BAYER = float4x4(0,8,2,10, 12,4,14,6, 3,11,1,9, 15,7,13,5) / 16.0;
            float bayer4(float2 p){ int2 i = (int2)fmod(abs(floor(p)), 4.0); return BAYER[i.y][i.x]; }
            float hash21(float2 p){ p=frac(p*float2(123.34,345.45)); p+=dot(p,p+34.345); return frac(p.x*p.y); }

            // Pick the glow color from the chosen category.
            // Index convention LOCKED to PerkSO.PerkType: 0 Movement, 1 Combat, 2 Survival.
            float3 categoryGlow()
            {
                float c = floor(_Category + 0.5);
                float wMov = step(c, 0.5);                      // c == 0  Movement
                float wCom = step(0.5, c) * step(c, 1.5);       // c == 1  Combat
                float wSur = step(1.5, c);                      // c == 2  Survival
                return _MovementColor.rgb * wMov
                     + _CombatColor.rgb   * wCom
                     + _SurvivalColor.rgb * wSur;
            }

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
                float2 uv  = i.texcoord;
                fixed4 tex = tex2D(_MainTex, uv) + _TextureSampleAdd;

                // Resolve the accent: category dropdown, or _AccentColor when _UseCategory is off.
                float3 accent = lerp(_AccentColor.rgb, categoryGlow(), step(0.5, _UseCategory));

                float2 c  = (uv - 0.5) * float2(_Aspect, 1.0);
                float  bd = bayer4(i.worldPosition.xy * _DitherScale);

                // ── glow pool under the cursor (tilt nudges its center) ────
                float2 mp = (_Mouse.xy - 0.5) * float2(_Aspect, 1.0) + _Tilt.xy * 0.5;
                float  dist = length(c - mp);
                float  radius = _GlowRadius * (0.7 + 0.5 * _Hover) + _Click * 0.4;
                float  pool = saturate(1.0 - dist / radius);
                pool = pool * pool;                                   // soft falloff

                // intensity: idle floor → hover, plus flick/click bursts
                float amt = lerp(_IdleGlow, 1.0, _Hover) * _GlowStrength;
                amt += _Flare * 0.4 + _Click * 0.7;
                float glow = pool * amt;

                // ordered-dither the glow so it reads as retro stipple, not a smooth blob
                float glowDith = saturate(glow * 1.3 - 0.15);
                float on = step(bd, glowDith);

                // ── dithered edge frame (breathes on hover) ────────────────
                float2 ec = abs(c) / float2(_Aspect * 0.5, 0.5);      // 0 center → 1 edge
                float  edge = max(ec.x, ec.y);
                float  frame = smoothstep(1.0 - _EdgeWidth, 1.0, edge);
                float  framePulse = frame * (0.4 + 0.6 * _Hover)
                                  * (0.7 + 0.3 * sin(_FxTime * 2.0));
                float  frameOn = step(bd, saturate(framePulse));

                // ── compose ────────────────────────────────────────────────
                float3 col = _BaseColor.rgb;
                col = lerp(col, accent, on * 0.9);                    // glow stipple
                col += accent * glow * 0.35;                          // soft additive bloom
                col = lerp(col, accent, frameOn * 0.85);              // edge frame
                col += (bd - 0.5) * _Grain;                           // constant static grain
                col += (hash21(i.worldPosition.xy + _FxTime) - 0.5) * _Grain * 0.5; // live grain

                // ── ordered-dither dissolve (driven by C# on close) ────────
                float aboveCut = step(_Dissolve, bd);                 // 1 if pixel survives
                float burnEdge = aboveCut
                               * (1.0 - smoothstep(0.0, 0.08, bd - _Dissolve))
                               * step(0.001, _Dissolve);              // only during dissolve
                col = lerp(col, accent * 1.3, burnEdge * 0.9);

                fixed4 outc;
                outc.rgb = saturate(col) * i.color.rgb;
                outc.a   = i.color.a * tex.a * aboveCut;              // kill dissolved pixels

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