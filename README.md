<div align="center">

# Third Commercial

**Momentum-driven rogue-lite FPS featuring dual-wielding and card-driven upgrades in a dithered, cel-shaded style. Unity URP 6**

[![Unity](https://img.shields.io/badge/Unity-6000.0-black?logo=unity)](https://unity.com/)
[![URP](https://img.shields.io/badge/Render%20Pipeline-URP-blue)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest)
[![C#](https://img.shields.io/badge/Language-C%23-239120?logo=csharp)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![Blender](https://img.shields.io/badge/Blender-F5792A?logo=blender&logoColor=white)](https://www.blender.org/)
[![Photoshop](https://img.shields.io/badge/Photoshop-31A8FF?logo=adobephotoshop&logoColor=white)](https://www.adobe.com/products/photoshop.html)
[![Audacity](https://img.shields.io/badge/Audacity-0000CC?logo=audacity&logoColor=white)](https://www.audacityteam.org/)
[![License](https://img.shields.io/badge/License-Source--Available-red)](#license)
[![Status](https://img.shields.io/badge/Status-In%20Development-yellow)](#status)

<br/>

<!-- Add gameplay GIF here once trailer is recorded -->
![Gameplay](ThirdCommercialFootage.gif)

**[▶ Watch Trailer](#)** · **[🎮 Try Demo on itch.io](#)** · **[⭐ Wishlist on Steam](#)**

*(Links active from February 2027 — Steam Next Fest)*

</div>

---

## Overview

**Third Commercial** is a fast-paced, first-person roguelite shooter where each run challenges the player through an escalating cycle of combat rooms, enemy encounters, and permanent perk decisions. No two runs are the same — difficulty scales continuously and every perk choice compounds into a distinct playstyle.

Developed solo by [Seymur Shiriyev](https://github.com/Codymur) — Steamworks partner and AI Systems student at VILNIUS TECH.

---

## Status & Roadmap

| | Current | Target (Launch) |
|---|---|---|
| **Perks** | 6 | 20 |
| **Rooms** | 7 | 25 |
| **Enemy Types** | 2 | 4 |
| **Demo** | ✅ Playable prototype | ✅ Polished demo |
| **Steam Page** | 🔄 In preparation | ✅ Live |
| **Next Fest** | — | February 2027 |

---

## Core Gameplay Pillars

### 1. Kinetic First-Person Combat
Every weapon interaction is physically grounded. Guns produce procedural recoil with stacked displacement on rapid fire, muzzle flash, shell ejection, and pooled bullet-hole decals. The player's kick (melee) is animated via DOTween and synchronized with a raycast hit window for frame-perfect feel. Dual-wielding is supported, with each hand independently bound to a mouse button and its own recoil profile.

Hit detection distinguishes between body and head colliders — body hits route through the standard damage pipeline in `Target.cs`, while headshots bypass the damage calculation entirely and set health directly to zero for an instant kill. This adds a meaningful skill ceiling without complicating the core combat loop.

### 2. Fluid Movement
The movement system combines standard FPS locomotion with a dedicated dive mechanic. Diving applies a physics impulse, temporarily reshapes the collider, modifies air control, and triggers a landing slide — all coordinated through a coroutine pipeline. Every action feeds procedural camera effects (FOV kick, pitch offset, lateral tilt) to maximize physical presence.

### 3. Escalating Roguelite Loop
Runs are structured as infinite batches of **6 rooms**: 4 Normal → 1 Mini-Boss → 1 Perk. Difficulty is tied to the absolute room index across the entire run, so combat pressure never resets. After each Mini-Boss room, the player selects a perk that persists for the rest of the run, making every decision consequential.

### 4. Reactive Enemy Archetypes
All enemies share a common state machine (Idle → Alert → Attack) via `EnemyBase`. Two distinct archetypes create combat variety:
- **Rusher** — High-HP melee enemy that charges after a telegraphed windup
- **Shooter** — Ranged enemy that strafes laterally, fires bursts, and backs away when the player closes distance

---

## Technical Stack

| Layer | Technology |
|---|---|
| Engine | Unity 6000.0 |
| Render Pipeline | Universal Render Pipeline (URP) |
| Scripting | C# / .NET Standard 2.1 |
| Physics | Unity PhysX — raycasts, sphere casts, ragdoll rigidbodies |
| 3D Modeling | Blender |
| 2D Art & Textures | Adobe Photoshop |
| Animation | Mecanim + DOTween procedural animation |
| Tweening | DOTween (Demigiant) |
| VFX | Cartoon FX Remaster (JMO Assets) |
| Audio | Custom audio manager + Weapons of Choice FREE + Audacity |
| Outlining | QuickOutline |
| Post-Processing | URP Volume stack + custom dithering shaders |

---

## Architecture

```
Assets/Scripts/
├── Guns/                          # Weapon systems
│   ├── GunController.cs           # Raycast shooting, targeted hitbox detection (head/body), pooled decals, muzzle flash
│   ├── WeaponRecoil.cs            # Procedural positional & rotational recoil
│   ├── DualPistolManager.cs       # Dual-wield pistol coordination
│   ├── GunWallAvoidance.cs        # Prevents clipping into geometry
│   └── Sway.cs                    # Weapon sway on look input
│
├── Player/                        # Player systems
│   ├── PlayerMovementTutorial.cs  # Core locomotion, footsteps, ground detection
│   ├── PlayerDive.cs              # Dive impulse, collider reshape, landing slide
│   ├── KickSystem.cs              # Melee kick with DOTween animation sync
│   ├── ItemPickupManager.cs       # Dual hand-slot pickup & drop system
│   ├── PlayerHealth.cs            # HP, i-frames, damage flash, death reload
│   └── PlayerCam.cs               # Procedural FOV, tilt, and pitch effects
│
├── LevelManagement/               # Run & room systems
│   ├── RunManager.cs              # Singleton — run state, room index, difficulty
│   ├── RoomManager.cs             # Room batch instantiation & deferred destruction
│   ├── Room.cs                    # Enemy spawning, door lock/unlock, perk triggers
│   ├── PassageTrigger.cs          # Corridor trigger for room transitions
│   └── Door.cs                    # Door open/close state
│
├── PerkSystem/                    # Roguelite progression
│   ├── PerkSO.cs                  # ScriptableObject — stat modifier data
│   ├── PerkManager.cs             # Singleton — stacks perks, recalculates stats
│   ├── PerkSelectionUI.cs         # In-run perk selection screen
│   └── PerkCardUI.cs              # Individual perk card rendering
│
└── Target/                        # Health & enemy logic
    ├── Target.cs                  # Base damageable class
    ├── EnemyBase.cs               # Abstract enemy — state machine, detection, ragdoll
    ├── RusherEnemy.cs             # Melee charger archetype
    └── ShooterEnemy.cs            # Ranged strafe-and-fire archetype
```

---

## Room Batching & Difficulty

Rooms are loaded and destroyed in batches to maintain seamless world continuity. Destruction is **deferred** — a room is only unloaded after the player has physically entered the next one, preventing visible gaps or falling through the world.

Difficulty is a direct function of the **global room index** and never resets between batches:

```
Difficulty = CurrentRoomIndex + 1
```

Enemy counts and spawn configurations grow continuously across the entire run, not just within a single batch.

---

## Perk System

Perks are defined as `ScriptableObject` assets (`PerkSO`) and carry the following modifier categories:

| Modifier | Stack Method |
|---|---|
| Movement Speed | Additive |
| Jump Force | Additive |
| Damage | Multiplicative |
| Dive Cooldown | Multiplicative |
| Health Regen on Kill | Additive |

`PerkManager` recalculates all derived stats and pushes them to the relevant components whenever a new perk is added.

---

## Prerequisites

- **Unity 6000.0** or later
- **Universal Render Pipeline** package
- **DOTween** (included under `Assets/Plugins/Demigiant`)

> **Note:** Game assets (models, textures, audio) are excluded from this repository. The codebase is shared for portfolio and architectural reference purposes only.

---

## Getting Started

1. Clone the repository
2. Open the project in **Unity 6000.0+**
3. Open `Assets/Scenes/LevelTest.unity`
4. Press **Play**

---

## About the Developer

**Seymur Shiriyev** — Steamworks partner and solo indie developer based in Baku, Azerbaijan. This is the third commercial project following two published titles on Steam under the developer name **Sensible Brain** (757,000+ impressions combined). Currently pursuing an AI Systems BSc at Vilnius Gediminas Technical University starting September 2026.

[![GitHub](https://img.shields.io/badge/GitHub-Codymur-black?logo=github)](https://github.com/Codymur)
[![Steam](https://img.shields.io/badge/Steam-Sensible%20Brain-1b2838?logo=steam)](https://store.steampowered.com/search/?publisher=Sensible%20Brain)

---

## License

Copyright (c) 2026 Seymur Shiriyev. All Rights Reserved.

This source code is made publicly available for portfolio and educational viewing purposes only.

You may **NOT**:
- Copy, reproduce, or redistribute any part of this codebase
- Use this code, assets in any project, commercial or non-commercial
- Modify and redistribute this work in any form

For licensing inquiries: seymur.t.shiriyev@gmail.com
