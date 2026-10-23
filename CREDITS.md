# Credits and Asset Licences

Every third-party asset in this project is released under **Creative Commons CC0 1.0
Universal** (public domain dedication). CC0 permits commercial use, modification and
redistribution, and does **not** require attribution. The credits below are given
voluntarily, and because keeping a written record of provenance is what lets you
answer a takedown or store review with evidence rather than memory.

## Art and sound effects

**Kenney — New Platformer Pack**

| | |
|---|---|
| Author | Kenney (Kenney Vleugels) — <https://kenney.nl> |
| Source | <https://opengameart.org/content/new-platformer-pack> |
| Licence | CC0 1.0 Universal — <https://creativecommons.org/publicdomain/zero/1.0/> |
| Downloaded | 1 September 2026 |
| File | `kenney_new-platformer-pack-1.0.zip` |

Used in this project:

- `Assets/Art/Characters` — previous Kenney runner frames (kept on disk, not used in gameplay)
- `Assets/Art/Tiles` — terrain, platforms, coins, gems, stars, hazards, HUD icons
- `Assets/Art/Backgrounds` — parallax backdrop layers
- `Assets/Art/Enemies` — not yet used, kept for future expansion
- `Assets/Audio/SFX` — 10 sound effects (jump, coin, gem, hurt, select, and others)

## Original character

**Rukajja**

| | |
|---|---|
| Author | Rukajja Games |
| Source | First-party character sheet from RukajjasWorld |
| Licence | Original work owned by Rukajja Games |
| Used as | Player visual in Endless Runner |

Used in this project:

- `Assets/Art/Rukajja/Frames/` — standalone idle, five run frames, jump, fall, hit
- `Assets/Art/Rukajja/Animation/` — `RukajjaCharacterAnimator` and motion clips

## Music

The game ships with **no background music** by deliberate design; only sound effects
are used. `AudioManager` still exposes music clip slots, so a track can be added later
without any code change.

## Engine and packages

Unity 6.3 LTS and its official packages (Universal Render Pipeline, Input System,
TextMeshPro) are covered by the Unity Terms of Service and the Unity Companion
Licence. They do not require attribution in-game.

## Before publishing

If you add any further assets, record them here with author, source URL, licence and
download date. Assets that are free for personal use are **not** necessarily free for
commercial use — check that the licence explicitly permits commercial distribution
before shipping anything to the Play Store.
