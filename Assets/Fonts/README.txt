OVERRUN TYPE  —  detailed pixel display fonts
=============================================

Four original TrueType fonts made for this project. The three pixel
fonts are rasterized at a ~13px cap height, so they keep crisp pixel
edges but carry real detail — curves, varied stroke weight, serifs —
and include full lowercase.

  OverrunSans.ttf    detailed pixel sans (regular)
  OverrunBlack.ttf   heavy pixel sans (titles)
  OverrunSerif.ttf   pixel serif (most detail / retro-RPG feel)
  OverrunRound.ttf   smooth rounded (NON-pixel) — uppercase only

Character set (Sans/Black/Serif): A-Z, a-z, 0-9 and
. , ! ? ' " - : ; / ( ) + % & * # @ = _ < > [ ]
Round is an uppercase display font (lowercase renders as caps).


LICENSE  —  do whatever you want
--------------------------------
These files were made for you. You own them. Commercial or personal
use, ship them in your game, modify them — NO credit required, no
restrictions.


USE IN UNITY (TextMeshPro)
--------------------------
1. Copy the .ttf files into  Assets/Fonts/
2. Window > TextMeshPro > Font Asset Creator
3. Source Font File = OverrunBlack (or whichever)
   - Atlas Resolution: 512 x 512 (or 1024 if you add many glyphs)
   - Render Mode: RASTER  for the crispest pixels
     (use SDF only if you want to scale/soften — pixels will blur)
4. Generate Font Atlas > Save the .asset
5. Assign it to any TMP text's Font Asset field.

For pixel-perfect crispness keep text at integer scales and the
camera/canvas at a fixed reference resolution.


USE ON THE WEB
--------------
@font-face{ font-family:'Overrun Black';
            src:url('OverrunBlack.ttf') format('truetype'); }
body{ -webkit-font-smoothing:none; }
