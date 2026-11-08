# Seraphim

A Windows-first, 3D overhead action-adventure about a six-winged angel. The game uses Unity URP and has no blood or gore.

This repository is the Unity project root. On this computer it lives at `F:\Temp\Apps\seraphim`.

[Sanctuary and HUD](Docs/images/sanctuary.png) · [Demon encounter](Docs/images/demons.png) · [Word of God](Docs/images/word-of-god.png) · [Lance of Dawn](Docs/images/lance-of-dawn.png) · [Chain of Heaven](Docs/images/chain-of-heaven.png) · [Seraphic Inferno](Docs/images/seraphic-inferno.png) · [Recorded validation results](Docs/validation.json)

The previews are camera renders from the exported Windows player, including its native angelic HUD.

## Play the prototype

Double-click **Play Seraphim.cmd** after a successful build. The executable is `Builds/Windows/Seraphim.exe`; distribute its entire folder, including `Seraphim_Data`, not just the executable.

- Use WASD for camera-relative movement, or click/hold the left mouse button to navigate. WASD cancels a click path; releasing the keys stops the character. Diagonal movement keeps the same speed and respects obstacles.
- Scroll to zoom the overhead camera.
- Use **1–5** for the powers below. Aim spears and lightning with the mouse. Space and the right mouse button also cast Radiance.
- Spirit regenerates at 12 per second. The bottom HUD places Life on the left and Spirit on the right, with clickable numbered skills and cooldowns in the center. Slot 6 remains available for a future skill. Clicking the HUD cannot issue a movement command; aimed skills cast from their HUD buttons select a nearby demon.
- Five demons appear at the start. Further demons spawn at random navigable positions around you roughly every 3.5 seconds, up to **eight living enemies**. Cinder Imps, winged Ash Stalkers, and armored Obsidian Guards have different health and movement speeds. Their overhead bars show remaining health, recent damage, and floating damage numbers.
- Demons chase and attack gently. They disperse into light when defeated. If Life reaches zero, you return to the starting point with full resources, brief protection, and the nearby demons cleared.
- Awaken all three waystones to complete this small exploration objective.
- Press Escape to exit.

| Key | Power | Cooldown | Spirit | Effect |
| --- | --- | --- | --- | --- |
| 1 | Radiance | 0.6 s | 6 | Expanding light pulse; 24 damage per enemy, with a small knockback. Also awakens nearby waystones. |
| 2 | Word of God | 30 s | Free | One-second charge, followed by **15 seconds of pulses that travel with you**: 16 damage each second within 18 meters. Glowing ascended form, enlarged wings, levitation, a pillar of light, seals, and particles. Restores 25 Life and 35 Spirit once at revelation, then fades back to normal. Cooldown starts immediately on casting. |
| 3 | Lance of Dawn | 1.8 s | 12 | Three piercing golden spears with laser trails; 55 damage per spear hit. Aim with the mouse. Solid architecture blocks the spears. |
| 4 | Chain of Heaven | 3 s | 18 | Cyan lightning jumps through up to four demons and stuns them briefly. First hit deals 36 damage, with 10% less per jump. Aim at the first demon. |
| 5 | Seraphic Inferno | 4.5 s | 22 | Expanding white-gold flame corona; 35 damage, knockback, and three additional burn ticks of 8 damage. |

This build focuses on navigation, camera, animation, five playable powers, resources, and an introductory combat encounter. Classes, equipment, pets, saving, campaign generation, and multiplayer are future milestones. The character uses an existing animated model with custom procedural wings; the demons use original animated geometry. Both remain prototype art. There is no blood or gore.

## Build again

Use **Build Seraphim.cmd**, or run from PowerShell:

```powershell
.\Tools\Build.ps1
```

The build prepares missing content, validates the scene dependencies and shaders, then exports the Windows player. Close an Editor that has this project open before running a separate batch build. Logs are in `Logs/build-windows.log`.

Open **Open Unity Project.cmd**, then open `Assets/Seraphim/Scenes/Sanctuary.unity` and press Play to work in the editor. The **Seraphim** menu also exposes preparation, validation, and build commands. An empty bootstrap scene is expected before entering Play mode: the environment is assembled at runtime.

## Requirements and reproducibility

- Unity Hub with an active Unity Personal or other valid license.
- **Unity 6000.3.28f1**, revision `8f082d2fe476`.
- URP **17.3.0** and Unity UI **2.0.0**, pinned in `Packages/manifest.json`; resolved dependencies are in `Packages/packages-lock.json`.
- Windows x64 and Direct3D 11 for this prototype. The local build uses Unity's bundled Mono toolchain; Visual Studio and mobile SDKs are not required.
- Git. Binary installers, Unity caches, credentials, and generated builds are excluded from this public repository.

The build script checks the usual Unity Hub install path. For a different installation, set `SERAPHIM_UNITY_EDITOR` to the full path to `Unity.exe`, or create ignored `Tools/local.json` with a `unityEditor` property. Machine-specific paths are not baked into the game.

The prototype's CC0 assets and OFL-licensed display font are included in the repository. To reacquire them:

```powershell
.\Tools\Download-Assets.ps1
```

The downloader uses the original publishers, caches archives locally, and verifies pinned SHA-256 values in `Tools/asset-lock.json`. A changed upstream archive intentionally fails verification until its update is reviewed. Sources and licenses are recorded alongside the assets and in `THIRD_PARTY_NOTICES.txt`. The build includes the font's full OFL license in the Windows distribution.

The Windows MSIX distribution of Unity Hub can place activation in its isolated per-user cache. The build helper makes that same user's existing signed license files available to the standalone Editor without modifying their contents. Licenses stay outside this repository.

## Check a change

```powershell
.\Tools\Validate.ps1
.\Tools\Build.ps1
.\Tools\Smoke-Test.ps1
```

Build already includes validation; a normal change only needs Build followed by Smoke-Test. The player test exercises click and WASD movement, control handoffs, diagonal speed, obstacle boundaries, HUD click blocking, Spirit spending/regeneration, character animation, and waystone activation. It checks the ultimate's traveling aura, fifteen pulses, transformation, restoration, full 30-second recharge, and reuse. Combat checks cover random spawning, the population cap, health bars, damage, demon reuse, piercing spears, chained lightning, burn ticks, melee attacks, and recovery after defeat. Allow roughly 75–100 seconds for the full player checks. Screenshots include the HUD and each power. Results go to `Builds/Windows/SmokeTests/`, with the runtime log at `Logs/player-smoke.log`.

For a focused combat iteration, use `.\Tools\Smoke-Test.ps1 -CombatOnly`. This skips baseline movement and the long ultimate recharge sequence; run the full test before releasing gameplay changes. The smoke-test launch uses isolated encounters instead of the normal automatic encounter so damage and cooldown checks are repeatable.

## Make changes

- `Assets/Seraphim/Generated/SanctuarySettings.asset`: movement speed, acceleration, camera, seed, resource regeneration, ability tuning, and enemy population. In the editor, select it in the Project window and edit the Inspector.
- `Assets/Seraphim/Runtime/`: gameplay and world assembly.
- `EnemyDirector.cs`, `DemonEnemy.cs`, and `DemonHealthBar.cs`: spawning, damage/status effects, demon behavior, and overhead readouts. Demons and spell effects are reused through bounded pools.
- `DivinePowers.cs` and `DivineProjectile.cs`: the three new powers, cursor aiming, and swept projectile hits. `CelestialAbility.cs` and `WordOfGodAbility.cs` own the two pulse powers.
- `Assets/Seraphim/Shaders/`: celestial effect shader.
- `Assets/Seraphim/Editor/`: asset import rules and the repeatable project/build setup.
- `Assets/Seraphim/Scenes/Sanctuary.unity`: bootstrap scene and its content references.

The layout is currently authored, with seeded decorative variation. This is not yet a procedural campaign generator. Keep major paths and quest anchors deliberate when expanding it into modular regions.

Build preparation preserves existing settings, materials, and the bootstrap scene. Do not delete their `.meta` files: Unity uses those identifiers to connect assets.

## Adding Asset Store packs later

Import packages into `Assets/ThirdParty/AssetStore/` where practical, and review their included license and dependencies. That directory and `.unitypackage` archives are ignored because the repository is public and raw Asset Store content generally cannot be redistributed. Record any external requirements before making one essential to a build. Do not commit accounts, activation files, or access tokens.

Viking Village, Hovl's Magic Effects FREE, and Mixamo are optional later additions, not dependencies of this build. The initial Quaternius download was unavailable due to Google Drive quota; Kenney's original CC0 downloads were used instead.
