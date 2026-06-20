Shader "MainMenu/Molten"
{
    Properties
    {
        [HideInInspector] _Mouse     ("Mouse",      Vector) = (0.5, 0.5, 0, 0)
        [HideInInspector] _ClickPos  ("Click Pos",  Vector) = (0.5, 0.5, 0, 0)
        [HideInInspector] _ClickTime ("Click Time", Float)  = 1000
        [HideInInspector] _MouseDown ("Mouse Down", Float)  = 0
        _Aspect         ("Aspect (w/h)",   Float)      = 1.7777

        [Enum(Native,0,Ice,1,Ember,2,Toxic,3,Synth,4,Steel,5,Mono,6,Gold,7,Vapor,8,Blood,9,Desert,10,NightVision,11,Cobalt,12,Thermal,13,Hazard,14,Military,15,Gunmetal,16,Rust,17,Sodium,18)]
        _Palette        ("Palette",            Float)      = 0
        _DitherStrength ("Dither Strength",    Range(0,1)) = 0.16
        _Pixelation     ("Pixelation",         Range(0,1)) = 0
        _Speed          ("Motion Speed",       Float)      = 1.0
        _Scale          ("Background Scale",   Range(0.25,4)) = 1.0
        _ClickScale     ("Click Impact Scale", Range(0.25,4)) = 1.0
        _BulletInterval ("Bullet Interval (s)", Range(0.1,5)) = 3
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

            #define NUM_BULLETS 6
            #define BULLET_LIFE 2.6
            #define NUM_CARDS   5

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
                float  _BulletInterval;
            CBUFFER_END

            #define iTime (_Time.y * _Speed)

            float  hash21(float2 p){ p=frac(p*float2(123.34,345.45)); p+=dot(p,p+34.345); return frac(p.x*p.y); }
            float  vnoise(float2 p){ float2 i=floor(p),f=frac(p); f=f*f*(3.0-2.0*f);
                float a=hash21(i),b=hash21(i+float2(1,0)),c=hash21(i+float2(0,1)),d=hash21(i+float2(1,1));
                return lerp(lerp(a,b,f.x),lerp(c,d,f.x),f.y); }
            float  fbm(float2 p){ float s=0.0,a=0.5; for(int i=0;i<5;i++){ s+=a*vnoise(p); p*=2.02; a*=0.5; } return s; }
            float3 ramp5(float t,float3 a,float3 b,float3 c,float3 d,float3 e){ t=saturate(t);
                float3 col=lerp(a,b,smoothstep(0.0,0.25,t)); col=lerp(col,c,smoothstep(0.25,0.5,t));
                col=lerp(col,d,smoothstep(0.5,0.75,t)); col=lerp(col,e,smoothstep(0.75,1.0,t)); return col; }

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
                else { // 0 = Native (gunmetal wall + hot sparks)
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
            // Dark crater + hot rim + radial cracks + expanding dust ring + muzzle flash.
            float bulletHole(float2 uv, float2 hp, float age, float impactScale)
            {
                float seed = hash21(floor(hp * 91.7) + 3.1);
                float r0   = 0.05 * impactScale;
                float2 rel = uv - hp;
                float  d   = length(rel);
                float  nd  = d / r0;
                float  ang = atan2(rel.y, rel.x);

                float fresh = exp(-age * 7.0);   // muzzle flash window
                float warm  = exp(-age * 1.7);   // rim glow cooldown

                float wob  = 0.13 * sin(ang * 7.0 + seed * 30.0) + 0.06 * sin(ang * 13.0 - seed * 18.0);
                float rimR = 1.0 + wob;

                float crater = -0.85 * smoothstep(1.05, 0.15, nd);
                float rim    = exp(-pow((nd - rimR) / 0.18, 2.0)) * (0.35 + 1.1 * warm);
                float spokes = pow(max(0.0, sin(ang * 5.0 + seed * 20.0)), 40.0)
                             + pow(max(0.0, sin(ang * 5.0 + 1.7 + seed * 9.0)), 40.0);
                float cracks = spokes * smoothstep(2.8, 0.8, nd) * smoothstep(0.7, 1.1, nd) * (0.45 + 0.8 * warm);
                float dr     = nd - (1.2 + age * 4.0);
                float dust   = exp(-dr * dr * 3.0) * exp(-age * 2.4) * 0.45;
                float flash  = fresh * exp(-nd * nd * 0.5) * 1.7;

                return crater + rim + cracks + dust + flash;
            }

            // Rounded-rectangle signed distance (perk card silhouette).
            float sdRoundBox(float2 p, float2 b, float r)
            {
                float2 q = abs(p) - b + r;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r;
            }

            // A single drifting perk card: filled body + bright outline + an
            // inner divider line that suggests the card's art box. Returns a
            // brightness contribution to the scene field.
            float perkCard(float2 uv, float2 c, float2 hs, float rot, float corner)
            {
                float2 p = uv - c;
                float s = sin(rot), co = cos(rot);
                p = float2(co * p.x - s * p.y, s * p.x + co * p.y);   // un-rotate

                float d = sdRoundBox(p, hs, corner);
                float inside = smoothstep(0.004, -0.004, d);

                float body    = inside * 0.22;                                  // dim fill
                float outline = exp(-pow(d / 0.009, 2.0)) * 0.85;               // bright edge
                float divider = exp(-pow((p.y - hs.y * 0.30) / 0.006, 2.0))      // art-box line
                              * step(abs(p.x), hs.x - corner) * inside * 0.45;
                float emblem  = exp(-dot(p - float2(0.0, hs.y * 0.05),          // center pip
                                         p - float2(0.0, hs.y * 0.05)) / 0.0016)
                              * inside * 0.4;
                return body + outline + divider + emblem;
            }

            float3 scene(float2 uv, float2 m, float2 clk, float clkT, float down)
            {
                // ── flat 2-D grid background ──────────────────────────────
                float2 gfp   = uv * 3.5;
                float2 gf    = frac(gfp);
                float2 gfw   = max(fwidth(gfp), 1e-5);
                float2 lineD = min(gf, 1.0 - gf);
                float  onLine = max(
                    1.0 - smoothstep(gfw.x * 0.5, gfw.x * 2.0, lineD.x),
                    1.0 - smoothstep(gfw.y * 0.5, gfw.y * 2.0, lineD.y));
                // ─────────────────────────────────────────────────────────

                float f = 0.0;

                // Grid cells get a base brightness boost so they separate
                // from grid lines in the palette quantiser.
                f += (1.0 - onLine) * 0.35;

                // ── drifting perk cards (nods to the perk system) ──────────
                // Each card scrolls slowly upward, wrapping around, swaying and
                // fading in/out at the top & bottom edges.
                for (int i = 0; i < NUM_CARDS; i++)
                {
                    float fi   = float(i);
                    float seed = hash21(float2(fi, 1.7));
                    float spd  = 0.045 + 0.05 * seed;

                    float prog = frac(iTime * spd + seed);                 // 0..1 bottom→top
                    float yPos = lerp(1.15, -0.72, prog);                  // travel
                    float lane = (fi + 0.5) / float(NUM_CARDS);             // even 0..1 across width
                    lane += (hash21(float2(fi, 3.1)) - 0.5) * 0.25;          // small jitter
                    float xPos = (lane - 0.5) * 1.9
                            + 0.05 * sin(iTime * 0.3 + fi * 2.0);         // sway
                    float rot  = (seed - 0.5) * 0.7 + 0.12 * sin(iTime * 0.25 + fi);
                    float2 hs  = float2(0.075, 0.105) * (0.75 + 0.7 * seed);
                    float fade = sin(prog * 3.14159);                      // soft edge in/out

                    f += perkCard(uv, float2(xPos, yPos), hs, rot, 0.022) * fade;
                }

                float t = saturate(f * 0.5);
                t += 0.07 * fbm(uv * 3.0 + iTime * 0.3) - 0.035;

                // ── cursor light effect (from Shards) ──────────
                // float dm   = length(uv - m);
                // float prox = exp(-dm * dm * 60.0);
    
                // float t = saturate((f * 0.5) + (prox * 0.5)); // Added prox to brightness
                // t += 0.07 * fbm(uv * 3.0 + iTime * 0.3) - 0.035;

                // ── self-spawning bullet impacts (time-driven, no clicks) ──
                // One bullet fires every _BulletInterval seconds; each lives
                // BULLET_LIFE seconds, so up to NUM_BULLETS overlap on screen.
                float interval = max(_BulletInterval, 0.05);
                float bullet = 0.0;
                for (int b = 0; b < NUM_BULLETS; b++)
                {
                    float fb = float(b);
                    // Global firing slot index for this lane, staggered per lane.
                    float slot = floor((iTime - fb * interval) / (NUM_BULLETS * interval));
                    float fireTime = (slot * NUM_BULLETS + fb) * interval;
                    float age = iTime - fireTime;                 // seconds since fired
                    if (age < 0.0 || age > BULLET_LIFE) continue; // not currently alive

                    float2 sd = float2(fb * 1.37, slot) + 0.123;
                    float  hx = hash21(sd);
                    float  hy = hash21(sd.yx + 23.1);
                    float2 hp = ((float2(hx, hy) - 0.5) * float2(_Aspect, 1.0)) / _Scale;

                    float bScale = _ClickScale * (0.7 + 0.6 * hash21(sd + 5.0));
                    float fade   = 1.0 - smoothstep(BULLET_LIFE * 0.45, BULLET_LIFE, age);

                    bullet += bulletHole(uv, hp, age, bScale) * fade;
                }
                t += bullet * 0.55;
                // ───────────────────────────────────────────────────────────

                t = saturate(t);

                return ramp5(t,
                    float3(0.02, 0.01, 0.02), float3(0.35, 0.06, 0.02), float3(0.82, 0.24, 0.02),
                    float3(1.0,  0.55, 0.10), float3(1.0,  0.92, 0.62));
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

                float2 uv  = baseUV / _Scale;
                float2 m   = ((_Mouse.xy    - 0.5) * float2(_Aspect, 1.0)) / _Scale;
                float2 clk = ((_ClickPos.xy - 0.5) * float2(_Aspect, 1.0)) / _Scale;

                float3 col = scene(uv, m, clk, _ClickTime, _MouseDown);
                col = quantize(col, ditherCoord, _DitherStrength, pal, pcount);
                return half4(ToDisplayLinear(col), 1.0);
            }
            ENDHLSL
        }
    }
    CustomEditor "MenuWallpaperShaderGUI"
    Fallback Off
}