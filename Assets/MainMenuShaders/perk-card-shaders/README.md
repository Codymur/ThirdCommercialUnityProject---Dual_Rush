# DUAL RUSH — Perk Card Hover Shaders

Five cursor-reactive **UI card shaders** for the perk-selection screen, plus one
small feeder script. They change **color / glow / pattern on hover** and leave
**scale, position and rotation untouched** — those stay fully under your
`PerkHoverEffect` script.

> Works with URP **and** Built-in (these are standard Canvas/UI shaders).
> Tested logic via an offline preview; compile in Unity to fine-tune.

## Files
| File | Effect on hover |
|------|-----------------|
| `PerkCard_EmberSpotlight.shader` | A hot ember glow tracks the cursor; the card edges ignite with a rim glow. |
| `PerkCard_ScanHologram.shader`   | CRT scanlines intensify and a bright sweep band runs up the card; hologram tint. |
| `PerkCard_TacticalGrid.shader`   | A dim tactical grid lights up toward the accent color; cells flare near the cursor. |
| `PerkCard_FresnelCharge.shader`  | Accent fill "charges up" from the bottom with a dithered surface line; edges rim-glow. |
| `PerkCard_DitherDissolve.shader` | An ordered-dither (Bayer) reveal of the accent expands out from the cursor. |
| `PerkCardFX.cs`                  | Feeds `_Hover`, `_Mouse`, `_FxTime`, `_Aspect`, `_AccentColor`, `_BaseColor` to the material. |

## Setup (per card)
1. Drop all files into your project (e.g. `Assets/Shaders/PerkCards/`). Let Unity import.
2. For each shader, create a **Material**: right-click the `.shader` → **Create → Material**.
   You'll have up to 5 materials; pick one to try.
3. Select the card's **background Image** (the dark panel). Set its **Material** to
   your chosen material.
4. Add the **`PerkCardFX`** component to that **same** GameObject.
5. Press Play and hover. Swap the material to try another look; tune in the inspector.

> The card's **icon, category text, and description** are child UI objects — they
> render on top of the shader, so they stay crisp and readable.

## Why a feeder script?
A Canvas UI shader can't read the cursor on its own. `PerkCardFX`:
- maps the mouse to the card's local **UV** (`_Mouse`) and eases a **`_Hover`** value;
- clones the material per card so each one animates **independently**;
- drives animation from **`_FxTime` = `Time.unscaledTime`**, because your
  `PerkSelectionUI` sets `Time.timeScale = 0` while choosing — which would otherwise
  freeze the built-in `_Time` and stop the effect. Unscaled time keeps it alive.

It never moves, scales, or rotates anything.

## Colors (set on `PerkCardFX`)
- **Accent** — the hover color. Match the perk category:
  - Movement `#5BD0E0` · Combat `#EF6B3A` · Survival `#F2C14E` · Ember (neutral) `#FF6A14`
- **Base** — idle card color, default gunmetal `#1B1F26`.

Set the category color at runtime from your perk data:
```csharp
GetComponent<PerkCardFX>().SetAccent(categoryColor);
```

## Tuning (material inspector)
- `_Glow` — effect intensity.
- `_DitherScale` — grain size of the retro dither.
- `_ScanDensity` (Scan Hologram) — scanline count.
- `_GridCells` (Tactical Grid) — grid resolution.
- `_Hover` / `_Mouse` / `_FxTime` / `_Aspect` are **driven by the script** — leave them.

## Notes
- These render in the **Transparent** UI queue with stencil + clip-rect support, so they
  behave inside Masks and nested canvases like a normal `UI/Default` Image.
- If a card uses an Image **with a sprite**, the effect is multiplied by the sprite's
  alpha (so non-rectangular cards keep their shape). A plain solid Image fills the rect.
