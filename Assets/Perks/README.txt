PERK ICONS  —  20 pixel-art glyphs
==================================

White fill + dark pixel outline on transparent background, so you can
TINT them to any color in Unity (Image.color) — e.g. per category.
Each ships at 80x80 (name.png) and 160x160 (name-2x.png).

MOVEMENT
  01_SpeedyGonzales   triple speed chevrons   (+25% move speed)
  02_DiveDecrease     down arrow + minus      (-10% dive cooldown)
  03_HairSpring       coil + up arrow         (higher jump)
  04_QuickDive        double down chevron     (-20% dive cooldown)
  05_Parkour          curved leap arrow       (air control)
  06_Adrenaline       ECG heartbeat           (speed after hit)
COMBAT
  07_Killer           skull                   (+25% damage)
  08_TriggerFinger    bullet                  (+10% fire rate)
  09_HairTrigger      lightning bolt          (+25% fire rate)
  10_Warhead          rocket                  (+15% damage)
  11_GlassCannon      cracked gem             (+40% dmg / +20% taken)
  12_Executioner      axe / maul              (finisher damage)
  13_Bloodrush        blood drop + streaks    (kill fire-rate buff)
  14_LastStand        flag                    (low-HP damage)
SURVIVAL
  15_IronSkin         shield                  (-10% damage taken)
  16_Regeneration     medkit cross            (HP on kill)
  17_Resilience       reinforced shield       (-20% damage taken)
  18_Lifesteal        heart + drop            (more HP on kill)
  19_Toughness        heart + plus            (+25 max HP)
  20_IronWill         shield + star           (longer i-frames)

Made for you — use freely, commercial or not, no credit needed.

UNITY
-----
1. Drop the PNGs in Assets/ (e.g. Assets/UI/PerkIcons/).
2. Select all → Texture Type = Sprite (2D and UI),
   Filter Mode = Point, Compression = None → Apply.
3. Assign to an Image/SpriteRenderer. To color by category, set the
   Image "Color" (white icons tint cleanly):
     Movement  #5BD0E0   Combat  #EF6B3A   Survival  #F2C14E
   (Leave white for a neutral look.)
