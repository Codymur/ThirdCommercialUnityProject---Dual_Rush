OVERRUN CURSORS  —  themed pixel cursors
========================================

Native Molten palette: gunmetal + hot-orange spark, dark pixel outline.

  cursor_reticle.png      64x64   gameplay aim cursor — HOTSPOT = 32,32 (center)
  cursor_reticle-2x.png   128x128 hi-dpi version       — HOTSPOT = 64,64
  cursor_pointer.png      42x54   menu arrow cursor     — HOTSPOT = 0,0 (top-left tip)
  cursor_pointer-2x.png   84x108  hi-dpi version        — HOTSPOT = 0,0

These are made for you — use freely, commercial or not, no credit.


USE IN UNITY
------------
1. Drop the PNGs in  Assets/  (e.g. Assets/UI/Cursors/)
2. Select each one. In the Inspector set:
     Texture Type      = Cursor
     Filter Mode       = Point (no filter)   ← keeps pixels crisp
     Compression       = None
   Click Apply.

3a. Set it globally from a script:
      using UnityEngine;
      public class CursorSetup : MonoBehaviour {
        public Texture2D reticle;             // assign in Inspector
        public Vector2  hotspot = new Vector2(32,32);
        void Start(){
          Cursor.SetCursor(reticle, hotspot, CursorMode.Auto);
        }
      }

3b. Or set it without code:
      Edit > Project Settings > Player > Default Cursor
      (hotspot field is right below it — use 32,32 for the reticle).

Tip: use cursor_pointer (hotspot 0,0) on menus, and swap to
cursor_reticle (hotspot 32,32) when gameplay starts.
For 4K/high-DPI, assign the  -2x  textures and double the hotspot.
