CRUEL-STYLE HAND CURSOR
=======================

A pixel-art pointing-hand cursor (skin-tone, dark outline, index finger up)
matching the menu cursor from the reference game.

  cursor_hand.png      44x60   default        — HOTSPOT = 14, 3  (fingertip)
  cursor_hand-2x.png   66x90   hi-dpi          — HOTSPOT = 21, 5
  cursor_hand-3x.png   88x120  4K / large UI   — HOTSPOT = 28, 6

Made for you — use freely, commercial or not, no credit needed.


USE IN UNITY
------------
1. Drop the PNG in  Assets/  (e.g. Assets/UI/Cursors/)
2. Select it. In the Inspector set:
     Texture Type   = Cursor
     Filter Mode    = Point (no filter)     ← keeps the pixels crisp
     Compression    = None
   Apply.

3. Set it from a script (hotspot = fingertip):
     using UnityEngine;
     public class HandCursor : MonoBehaviour {
       public Texture2D hand;                       // assign in Inspector
       void Start(){
         Cursor.SetCursor(hand, new Vector2(14,3), CursorMode.Auto);
       }
     }

   Or: Project Settings > Player > Default Cursor (set Cursor Hotspot 14,3).

Note: pick the file whose on-screen size you want (44 / 66 / 88 px tall)
and use the matching hotspot above. Hardware cursors are capped at 32x32
on some platforms — if the bigger sizes don't show, use CursorMode.ForceSoftware.
