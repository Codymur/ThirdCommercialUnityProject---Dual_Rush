Shader "MainMenu/Ballistics"
{
    Properties
    {
        [HideInInspector] _Mouse     ("Mouse",      Vector) = (0.5, 0.5, 0, 0)
        [HideInInspector] _ClickPos  ("Click Pos",  Vector) = (0.5, 0.5, 0, 0)
        [HideInInspector] _ClickTime ("Click Time", Float)  = 1000
        [HideInInspector] _MouseDown ("Mouse Down", Float)  = 0
        [HideInInspector] _HoleCount ("Hole Count", Float)  = 0
        _Aspect         ("Aspect (w/h)",   Float)      = 1.7777

        [Enum(Native,0,Ice,1,Ember,2,Toxic,3,Synth,4,Steel,5,Mono,6,Gold,7,Vapor,8,Blood,9,Desert,10,NightVision,11,Cobalt,12,Thermal,13,Hazard,14,Military,15,Gunmetal,16,Rust,17,Sodium,18)]
        _Palette        ("Palette",            Float)      = 0
        _DitherStrength ("Dither Strength",    Range(0,1)) = 0.14
        _Pixelation     ("Pixelation",         Range(0,1)) = 0
        _Speed          ("Motion Speed",       Float)      = 1.0
        _Scale          ("Background Scale",   Range(0.25,4)) = 1.0
        _ClickScale     ("Click Impact Scale", Range(0.25,4)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "Wallpaper"
            Tags { "LightMode"="UniversalForward" }
            Cull Off  ZWrite Off  ZTest LEqual

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define MAX_HOLES 16

            CBUFFER_START(UnityPerMaterial)
                float4 _Mouse;
                float4 _ClickPos;
                float  _ClickTime;
                float  _MouseDown;
                float  _Aspect;
                float  _Palette;
                float  _DitherStrength;
                float  _Pixelation;
                float  _Speed;
                float  _Scale;
                float  _ClickScale;
            CBUFFER_END

            // Impact ring buffer, driven by MenuWallpaperController.
            // xy = screen pos (0..1), z = age in seconds, w = unused.
            float4 _Holes[MAX_HOLES];
            float  _HoleCount;

            #define iTime (_Time.y * _Speed)

            float  hash21(float2 p){ p=frac(p*float2(123.34,345.45)); p+=dot(p,p+34.345); return frac(p.x*p.y); }
            float  vnoise(float2 p){ float2 i=floor(p),f=frac(p); f=f*f*(3.0-2.0*f);
                float a=hash21(i),b=hash21(i+float2(1,0)),c=hash21(i+float2(0,1)),d=hash21(i+float2(1,1));
                return lerp(lerp(a,b,f.x),lerp(c,d,f.x),f.y); }
            float  fbm(float2 p){ float s=0.0,a=0.5; for(int i=0;i<5;i++){ s+=a*vnoise(p); p*=2.02; a*=0.5; } return s; }

            float bayer2(float2 a){ a=floor(a); return frac(a.x*0.5 + a.y*a.y*0.75); }
            float bayer4(float2 a){ return bayer2(0.5*a)*0.25 + bayer2(a); }
            float bayer8(float2 a){ return bayer4(0.5*a)*0.25 + bayer2(a); }

            // Convert authored (sRGB-ish) palette colors to LINEAR so they DISPLAY
            // exactly like the WebGL preview when the Unity project is in Linear
            // color space (the default). Without this, Unity's linear->sRGB output
            // conversion washes the palette out (dark charcoal -> grey, vivid
            // orange -> pale cream).
            //   * If your project Color Space is GAMMA, replace the body with
            //     'return c;' (no conversion needed there).
            float3 ToDisplayLinear(float3 c)
            {
                float3 lo = c / 12.92;
                float3 hi = pow(max((c + 0.055) / 1.055, 0.0), 2.4);
                return lerp(hi, lo, step(c, 0.04045));
            }

            // --- selectable palette bank. Index 0 = this shader's native look. ---
            void loadPalette(int sel, out float3 pal[8], out int n)
            {
                [unroll] for(int k=0;k<8;k++) pal[k]=float3(0,0,0);
                if(sel==1){ // Ice
                    pal[0]=float3(0.02,0.03,0.08); pal[1]=float3(0.02,0.10,0.22); pal[2]=float3(0.03,0.20,0.40);
                    pal[3]=float3(0.0,0.42,0.62);  pal[4]=float3(0.05,0.62,0.82); pal[5]=float3(0.30,0.85,1.0);
                    pal[6]=float3(0.65,0.95,1.0);  pal[7]=float3(0.92,0.99,1.0);  n=8; }
                else if(sel==2){ // Ember
                    pal[0]=float3(0.02,0.01,0.02); pal[1]=float3(0.16,0.03,0.02); pal[2]=float3(0.40,0.07,0.02);
                    pal[3]=float3(0.70,0.18,0.02); pal[4]=float3(0.92,0.38,0.04); pal[5]=float3(1.0,0.58,0.12);
                    pal[6]=float3(1.0,0.80,0.35);  pal[7]=float3(1.0,0.95,0.70);  n=8; }
                else if(sel==3){ // Toxic
                    pal[0]=float3(0.01,0.03,0.02); pal[1]=float3(0.02,0.10,0.05); pal[2]=float3(0.03,0.22,0.09);
                    pal[3]=float3(0.08,0.40,0.15); pal[4]=float3(0.25,0.68,0.22); pal[5]=float3(0.45,0.92,0.32);
                    pal[6]=float3(0.70,1.0,0.55);  pal[7]=float3(0.90,1.0,0.82);  n=8; }
                else if(sel==4){ // Synth
                    pal[0]=float3(0.02,0.01,0.05); pal[1]=float3(0.10,0.02,0.18); pal[2]=float3(0.28,0.04,0.36);
                    pal[3]=float3(0.55,0.08,0.52); pal[4]=float3(0.85,0.16,0.62); pal[5]=float3(1.0,0.30,0.72);
                    pal[6]=float3(0.45,0.78,1.0);  pal[7]=float3(0.92,0.95,1.0);  n=8; }
                else if(sel==5){ // Steel
                    pal[0]=float3(0.018,0.022,0.028); pal[1]=float3(0.07,0.085,0.10); pal[2]=float3(0.17,0.19,0.22);
                    pal[3]=float3(0.34,0.37,0.41);    pal[4]=float3(0.55,0.58,0.63);  pal[5]=float3(0.78,0.81,0.86);
                    pal[6]=float3(0.92,0.94,0.97);    n=7; }
                else if(sel==6){ // Mono
                    pal[0]=float3(0.02,0.02,0.02); pal[1]=float3(0.18,0.18,0.18); pal[2]=float3(0.36,0.36,0.36);
                    pal[3]=float3(0.55,0.55,0.55); pal[4]=float3(0.74,0.74,0.74); pal[5]=float3(1.0,1.0,1.0); n=6; }
                else if(sel==7){ // Gold
                    pal[0]=float3(0.03,0.02,0.0);  pal[1]=float3(0.16,0.10,0.02); pal[2]=float3(0.36,0.22,0.05);
                    pal[3]=float3(0.62,0.42,0.12); pal[4]=float3(0.85,0.66,0.28); pal[5]=float3(0.98,0.85,0.52);
                    pal[6]=float3(1.0,0.96,0.80);  n=7; }
                else if(sel==8){ // Vapor
                    pal[0]=float3(0.05,0.02,0.08); pal[1]=float3(0.18,0.07,0.24); pal[2]=float3(0.45,0.16,0.45);
                    pal[3]=float3(0.92,0.35,0.62); pal[4]=float3(1.0,0.62,0.66);  pal[5]=float3(0.55,0.92,0.92);
                    pal[6]=float3(0.80,1.0,0.95);  n=7; }
                else if(sel==9){ // Blood
                    pal[0]=float3(0.03,0.0,0.0);   pal[1]=float3(0.12,0.01,0.01); pal[2]=float3(0.28,0.02,0.03);
                    pal[3]=float3(0.50,0.04,0.05); pal[4]=float3(0.72,0.10,0.08); pal[5]=float3(0.90,0.22,0.14);
                    pal[6]=float3(1.0,0.45,0.30);  pal[7]=float3(1.0,0.80,0.70);  n=8; }
                else if(sel==10){ // Desert
                    pal[0]=float3(0.04,0.03,0.02); pal[1]=float3(0.14,0.10,0.06); pal[2]=float3(0.30,0.22,0.12);
                    pal[3]=float3(0.50,0.38,0.20); pal[4]=float3(0.70,0.56,0.32); pal[5]=float3(0.86,0.74,0.48);
                    pal[6]=float3(0.96,0.88,0.66); pal[7]=float3(1.0,0.97,0.85);  n=8; }
                else if(sel==11){ // NightVision
                    pal[0]=float3(0.0,0.02,0.0);   pal[1]=float3(0.0,0.08,0.02);  pal[2]=float3(0.0,0.18,0.05);
                    pal[3]=float3(0.02,0.34,0.10); pal[4]=float3(0.10,0.55,0.18); pal[5]=float3(0.25,0.78,0.30);
                    pal[6]=float3(0.50,0.95,0.45); pal[7]=float3(0.85,1.0,0.80);  n=8; }
                else if(sel==12){ // Cobalt
                    pal[0]=float3(0.01,0.02,0.04); pal[1]=float3(0.02,0.06,0.12); pal[2]=float3(0.04,0.12,0.24);
                    pal[3]=float3(0.06,0.22,0.40); pal[4]=float3(0.10,0.36,0.60); pal[5]=float3(0.20,0.52,0.80);
                    pal[6]=float3(0.45,0.72,0.95); pal[7]=float3(0.80,0.92,1.0);  n=8; }
                else if(sel==13){ // Thermal — weapon-optic thermal scope
                    pal[0]=float3(0.02,0.0,0.05);  pal[1]=float3(0.10,0.0,0.22);  pal[2]=float3(0.30,0.0,0.40);
                    pal[3]=float3(0.60,0.04,0.30); pal[4]=float3(0.85,0.18,0.10); pal[5]=float3(0.98,0.50,0.05);
                    pal[6]=float3(1.0,0.82,0.20);  pal[7]=float3(1.0,0.98,0.85);  n=8; }
                else if(sel==14){ // Hazard — industrial caution black/amber/yellow
                    pal[0]=float3(0.02,0.02,0.0);  pal[1]=float3(0.08,0.07,0.02); pal[2]=float3(0.16,0.13,0.03);
                    pal[3]=float3(0.34,0.26,0.04); pal[4]=float3(0.60,0.45,0.05); pal[5]=float3(0.85,0.66,0.08);
                    pal[6]=float3(1.0,0.85,0.15);  pal[7]=float3(1.0,0.96,0.70);  n=8; }
                else if(sel==15){ // Military — olive drab field green
                    pal[0]=float3(0.03,0.03,0.02); pal[1]=float3(0.08,0.09,0.05); pal[2]=float3(0.14,0.16,0.08);
                    pal[3]=float3(0.22,0.26,0.12); pal[4]=float3(0.34,0.38,0.18); pal[5]=float3(0.48,0.52,0.28);
                    pal[6]=float3(0.66,0.68,0.42); pal[7]=float3(0.86,0.86,0.66); n=8; }
                else if(sel==16){ // Gunmetal — cold blue tactical steel
                    pal[0]=float3(0.015,0.02,0.03); pal[1]=float3(0.05,0.07,0.10); pal[2]=float3(0.10,0.14,0.19);
                    pal[3]=float3(0.18,0.24,0.31);  pal[4]=float3(0.30,0.37,0.45); pal[5]=float3(0.45,0.53,0.62);
                    pal[6]=float3(0.66,0.73,0.82);  pal[7]=float3(0.90,0.94,1.0);  n=8; }
                else if(sel==17){ // Rust — corroded iron
                    pal[0]=float3(0.03,0.02,0.01); pal[1]=float3(0.10,0.05,0.03); pal[2]=float3(0.22,0.10,0.05);
                    pal[3]=float3(0.40,0.18,0.08); pal[4]=float3(0.60,0.30,0.12); pal[5]=float3(0.78,0.44,0.20);
                    pal[6]=float3(0.90,0.62,0.38); pal[7]=float3(0.98,0.84,0.66); n=8; }
                else if(sel==18){ // Sodium — urban-night sodium street-lamp
                    pal[0]=float3(0.01,0.02,0.05); pal[1]=float3(0.04,0.05,0.12); pal[2]=float3(0.10,0.09,0.14);
                    pal[3]=float3(0.24,0.16,0.10); pal[4]=float3(0.46,0.30,0.10); pal[5]=float3(0.72,0.48,0.12);
                    pal[6]=float3(0.94,0.70,0.22); pal[7]=float3(1.0,0.90,0.55);  n=8; }
                else { // 0 = Native (gunmetal wall with hot sparks)
                    pal[0]=float3(0.015,0.018,0.022); pal[1]=float3(0.05,0.06,0.07);  pal[2]=float3(0.11,0.12,0.14);
                    pal[3]=float3(0.20,0.22,0.25);    pal[4]=float3(0.34,0.36,0.40);  pal[5]=float3(0.58,0.52,0.42);
                    pal[6]=float3(0.88,0.55,0.22);    pal[7]=float3(1.0,0.86,0.55);   n=8; }
            }
            float3 quantize(float3 col, float2 frag, float strength, float3 pal[8], int n){
                float th = bayer8(frag) - 0.5;
                col = saturate(col + th * strength);
                float best=1e9; float3 bc=pal[0];
                for(int i=0;i<8;i++){ if(i<n){ float3 d=col-pal[i]; float dd=dot(d,d); if(dd<best){best=dd;bc=pal[i];} } }
                return bc;
            }

            // A single bullet impact at hp (aspect space). age = seconds since fired.
            // Returns a signed brightness delta: a dark crater plus a hot rim,
            // radial cracks, an expanding dust ring and a brief muzzle flash.
            float bulletHole(float2 uv, float2 hp, float age, float clickScale)
            {
                if (age < 0.0 || age > 30.0) return 0.0;   // unfired / expired slot
                float seed = hash21(floor(hp * 91.7) + 3.1);
                float r0   = 0.05 * clickScale;
                float2 rel = uv - hp;
                float  d   = length(rel);
                float  nd  = d / r0;
                float  ang = atan2(rel.y, rel.x);

                float fresh = exp(-age * 7.0);   // muzzle flash window
                float warm  = exp(-age * 1.7);   // rim glow cooldown

                // irregular jagged rim radius
                float wob  = 0.13 * sin(ang * 7.0 + seed * 30.0) + 0.06 * sin(ang * 13.0 - seed * 18.0);
                float rimR = 1.0 + wob;

                float crater = -0.85 * smoothstep(1.05, 0.15, nd);                         // dark punch (persistent)
                float rim    = exp(-pow((nd - rimR) / 0.18, 2.0)) * (0.35 + 1.1 * warm);   // bright lip
                float spokes = pow(max(0.0, sin(ang * 5.0 + seed * 20.0)), 40.0)
                             + pow(max(0.0, sin(ang * 5.0 + 1.7 + seed * 9.0)), 40.0);
                float cracks = spokes * smoothstep(2.8, 0.8, nd) * smoothstep(0.7, 1.1, nd) * (0.45 + 0.8 * warm);
                float dr     = nd - (1.2 + age * 4.0);
                float dust   = exp(-dr * dr * 3.0) * exp(-age * 2.4) * 0.45;               // expanding ring
                float flash  = fresh * exp(-nd * nd * 0.5) * 1.7;                          // muzzle bloom

                return crater + rim + cracks + dust + flash;
            }

            // Dark tactical wall. Flashlight tracks the cursor; bullet holes punch in.
            float scene(float2 uv, float2 m)
            {
                float n = fbm(uv * 2.3 + float2(0.0, iTime * 0.02));
                float grain = (hash21(floor(uv * 520.0)) - 0.5) * 0.06;
                float base = 0.11 + 0.11 * n + grain;
                base += 0.025 * sin(uv.x * 46.0 + n * 4.0);          // faint brushed striations

                float dM = length(uv - m);
                base += 0.30 * exp(-dM * 1.7);                       // flashlight pool
                base += 0.06 * exp(-pow((dM - 0.05) / 0.012, 2.0));  // thin aim reticle ring
                base *= 1.0 - 0.5 * dot(uv, uv);                     // vignette

                for (int i = 0; i < MAX_HOLES; i++)
                {
                    float4 H = _Holes[i];
                    if (H.w < 0.5) continue;        // inactive / unfired slot
                    float2 hp = ((H.xy - 0.5) * float2(_Aspect, 1.0)) / _Scale;
                    base += bulletHole(uv, hp, H.z, _ClickScale);
                }
                return saturate(base);
            }

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 pal[8]; int pcount; loadPalette((int)(_Palette + 0.5), pal, pcount);

                float2 baseUV = (IN.uv - 0.5) * float2(_Aspect, 1.0);
                float2 ditherCoord = IN.positionHCS.xy;
                if (_Pixelation > 0.0001)
                {
                    float cells = floor(lerp(420.0, 18.0, saturate(_Pixelation)));
                    float2 cell = floor(baseUV * cells);
                    baseUV = (cell + 0.5) / cells;
                    ditherCoord = cell;
                }

                float2 uv = baseUV / _Scale;
                float2 m  = ((_Mouse.xy - 0.5) * float2(_Aspect, 1.0)) / _Scale;

                float v = scene(uv, m);
                float3 col = float3(v, v, v);
                col = quantize(col, ditherCoord, _DitherStrength, pal, pcount);
                return half4(ToDisplayLinear(col), 1.0);
            }
            ENDHLSL
        }
    }
    CustomEditor "MenuWallpaperShaderGUI"
    Fallback Off
}
