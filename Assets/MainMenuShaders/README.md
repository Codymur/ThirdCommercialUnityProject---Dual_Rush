# MainMenu Interactive Shader Wallpapers

Seven animated, mouse-reactive background shaders for the main menu, written
for **URP**. They bake their own ordered dithering so they match the in-game
retro look on their own.

| # | Shader (`MainMenu/…`) | Mood | Interaction |
|---|------------------------|------|-------------|
| 1 | **Hyperspace** | cold cyan wormhole | move to steer · click to warp |
| 2 | **Molten** | tactical grid, rounds land on their own | flat dithered grid + self-spawning bullet impacts (genre piece) |
| 3 | **Grid** | toxic-green tactical grid | mouse warps the field · click pulses |
| 4 | **Shards** | synthwave Voronoi crystals | mouse lights cells · click shatters |
| 5 | **Flux** | electric blue energy field | mouse bends the charge · click discharges |
| 6 | **Steel** | minimalist black / silver gunmetal | move the light · click pulses · **self-dithering** |
| 7 | **Ballistics** | dark tactical wall, gunmetal + hot sparks | flashlight tracks the cursor · **left-click punches bullet holes** |

> **Ballistics** is the genre piece for a shooter menu: a gritty concrete/steel
> wall lit by a flashlight that follows the cursor. Every **left-click** drives a
> round into the wall — a dark crater with a hot jagged rim, radial cracks, an
> expanding dust ring and a brief muzzle flash. The last **16** impacts stay on
> screen. The brightest values warm to ember/spark colours, so the holes glow.

## Files
- `Hyperspace/Molten/Grid/Shards/Flux/Steel/Ballistics.shader` — the seven
  wallpapers (fully self-contained — no external include to resolve).
- `Editor/MenuWallpaperShaderGUI.cs` — grouped material inspector (Editor-only).
- `MenuWallpaperController.cs` — feeds mouse + click (and the bullet-hole
  buffer) into the active material.

## Install
1. Copy this whole `MainMenuShaders` folder into your project's `Assets/`.
   Let Unity import (it will generate `.meta` files).
2. For each shader, create a Material: right-click the `.shader` →
   **Create > Material**, or make a Material and set its shader to
   `MainMenu/Hyperspace`, etc. You'll have 7 materials. (Use **Ballistics**
   for the shooter look in the screenshot.)

## Scene setup (recommended: camera-facing Quad)
This path keeps the wallpaper *inside* the camera's color buffer, so your
`FullscreenDither` renderer feature dithers it just like the rest of the game.

1. **GameObject > 3D Object > Quad**, parent it under the menu **Camera**.
2. Assign one wallpaper material to the Quad's **MeshRenderer**.
3. Add **MenuWallpaperController** to the Camera and wire up:
   - `Cam` → menu camera
   - `Quad` → the quad's Transform
   - `Wallpaper Renderer` → the quad's MeshRenderer
   - `Wallpapers` → drag in the 6 materials (order = switch order)
4. Make sure menu buttons live on a Canvas that renders **on top** of the quad
   (Screen Space – Overlay does this automatically).
5. Press Play. The quad auto-fits the camera every frame, so its size/placement
   don't matter. Number keys **1–5** and **←/→** switch wallpapers (toggle off
   with `Keyboard Switching`, or call `SetWallpaper(int)` from your menu code).

## Inspector settings (per material)
**All six shaders now bake their own dithering** (8×8 Bayer + palette snap), so
they look correct on their own — you no longer depend on the `FullscreenDither`
post-pass for the menu. Select any `MainMenu_*` material and tweak it live in the
Inspector. A custom inspector (`Editor/MenuWallpaperShaderGUI.cs`) groups the
controls into sections and shows a swatch strip of the chosen palette:

| Section | Property | What it does |
|---------|----------|--------------|
| **Coloring** | **Palette** | Dropdown: `Native` (the shader's signature look) or one of **18** shared palettes — `Ice, Ember, Toxic, Synth, Steel, Mono, Gold, Vapor, Blood, Desert, NightVision, Cobalt, Thermal, Hazard, Military, Gunmetal, Rust, Sodium`. The last six are shooter-genre flavored (thermal optic, industrial hazard, olive drab, gunmetal, corroded rust, sodium street-lamp). The image is snapped to the chosen palette by luminance/hue.`. The image is snapped to the chosen palette by luminance/hue. |
| | **Dither Strength** | amount of ordered-dither grain (0 = clean bands, ~0.16 default) |
| **Resolution** | **Pixelation** | 0 = crisp full-res; higher = chunkier retro pixels (the dither aligns to each block). |
| **Motion** | **Motion Speed** | animation rate / effect duration (1 = normal, lower = slower/calmer) |
| **Interaction & Layout** | **Background Scale** | zooms the pattern (0.25 = far out, 4 = way in). The mouse highlight stays under the cursor. |
| | **Click Impact Scale** | size/reach of the left-click effect (ripple, warp, discharge…). |

> `Aspect (w/h)`, plus the hidden `_Mouse / _ClickPos / _ClickTime / _MouseDown`
> properties, are driven automatically by `MenuWallpaperController` every frame —
> leave them alone.

### Custom inspector setup
Keep `Editor/MenuWallpaperShaderGUI.cs` inside a folder named **Editor** (it ships
in `MainMenuShaders/Editor/`). It's referenced by each shader via
`CustomEditor "MenuWallpaperShaderGUI"`. If that script is ever absent, Unity just
falls back to the default material inspector — the shaders still work.

**Editing the palettes themselves:** the 18 shared palettes live in the
`loadPalette()` function near the top of every `.shader` (identical in all seven),
and the swatch previews mirror them in `MenuWallpaperShaderGUI.cs`. To add or
recolor a palette, edit both. The browser preview (`Shader Wallpapers.html`) is a
handy place to find a look first.

### Bullet holes (Ballistics)
`MenuWallpaperController` keeps a 16-slot ring buffer of recent left-clicks and
feeds it to any material that declares `_Holes` (only **Ballistics** does). Each
entry is `float4(x, y, ageSeconds, 0)` in 0..1 screen space. The newest 16 shots
show; older ones are recycled. No setup needed — it's automatic once the
controller is on the camera. **Click Impact Scale** controls hole size; the
flashlight follows the mouse via the usual `_Mouse` feed.

### Self-spawning bullet impacts (Molten)
**Molten** is the genre piece: a flat dithered **tactical grid** where rounds land
on their own — no clicks or controller wiring needed. It runs the same
crater/rim/cracks/dust/muzzle-flash impact as Ballistics, but fires them on a
timer so the menu feels like a live firefight in the background.
- **Bullet Interval (s)** — time between shots (0.1–5; default 3). Up to 6
  impacts overlap and fade out on their own.
- **Click Impact Scale** — base hole size (each shot adds a little random size
  variation on top).
- **Palette** — `Native` is gunmetal-with-hot-sparks; try **Thermal**, **Hazard**,
  **Rust** or **Gunmetal** for different genre moods.

**About your existing post-pass:** since the wallpaper self-dithers now, you can
**disable the `FullscreenDither` feature for the menu** to avoid double-dithering
(or leave it — the look stacks acceptably). Because they no longer need the
post-pass, these also work fine on an Overlay **RawImage** if you prefer that to
a camera Quad.

## Color space (IMPORTANT — matching the WebGL preview)
These shaders now output their palette colors through a `ToDisplayLinear()`
sRGB→linear conversion so they look **exactly like the browser preview** when your
project is in **Linear** color space (Unity's default). Without it, Unity's
linear→sRGB output pass washes the palette out — dark charcoal becomes mid-grey
and the vivid orange becomes pale cream.

- **Linear color space project (default):** leave as-is — it matches the preview.
- **Gamma color space project:** no conversion is needed; open each `.shader` and
  change the body of `ToDisplayLinear` to `return c;` (or the colors will look too
  dark/saturated). It's the function right after `bayer8`, identical in all seven.

## If the quad is invisible — checklist
**Most common cause (now fixed by default):** if your menu camera clears to a
**Skybox**, an `Opaque`/Geometry-queue quad with `ZWrite Off` gets painted over by
the skybox (the skybox renders after opaque geometry and the quad never wrote
depth). These shaders now ship in the **Transparent** queue so they render *after*
the skybox and stay visible. If you ever need to change it, the **Render Queue**
field is at the bottom of the material Inspector under **Advanced**.

The shaders are standard URP unlit. If a quad with the material still shows
nothing, it is almost always the quad's transform or the camera, not the shader:

1. **Console** — any shader error? A *compile* error shows the quad magenta, not
   invisible; no error means the shader compiled fine.
2. **Is the quad in front of the camera and facing it?** A quad parented to the
   camera sits at the camera's exact position (clipped by the near plane) until
   the controller fits it. Either add `MenuWallpaperController` (it fits the quad
   every frame, in edit mode too) or move the quad ~10 units in front.
3. **Scale** isn't zero, and the quad's **layer** is in the camera's Culling Mask.
4. **Sanity test:** select the material; the Inspector preview sphere should show
   moving color. If the preview animates but the quad doesn't, it's the
   quad/camera (step 2), not the shader.

Still stuck? Tell me what the Console says and whether the material preview
(step 4) animates, and I'll pinpoint it.

## Notes
- Uses the legacy **Input Manager** (`Input.mousePosition`). For the new Input
  System, swap the input reads in `MenuWallpaperController.Update`.
- The `_Aspect` property is set automatically each frame; the `_Mouse`,
  `_ClickPos`, `_ClickTime`, `_MouseDown` properties (and `_Holes` on Ballistics)
  are hidden / driven by the controller.
- All seven are single-pass fragment shaders with small loops — cheap enough for
  a menu background at full screen resolution. (Ballistics declares a small
  uniform array for impacts, so it isn't SRP-batched — negligible for one quad.)
