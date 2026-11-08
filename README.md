# Seraphim

A Windows-first, 3D overhead action-adventure about a six-winged angel. The game uses Unity URP and has no blood or gore.

This repository is the Unity project root. On this computer it lives at `F:\Temp\Apps\seraphim`.

[Sanctuary and HUD](Docs/images/sanctuary.png) · [Radiance](Docs/images/radiance.png) · [Word of God](Docs/images/word-of-god.png) · [Recorded validation results](Docs/validation.json)

The previews are camera renders from the exported Windows player, including its native angelic HUD.

## Play the prototype

Double-click **Play Seraphim.cmd** after a successful build. The executable is `Builds/Windows/Seraphim.exe`; distribute its entire folder, including `Seraphim_Data`, not just the executable.

- Use WASD for camera-relative movement, or click/hold the left mouse button to navigate. WASD cancels a click path; releasing the keys stops the character. Diagonal movement keeps the same speed and respects obstacles.
- Scroll to zoom the overhead camera.
- Press **1**, Space, or the right mouse button for **Radiance** (15 Spirit). Spirit regenerates automatically. Stand near a waystone to awaken it.
- Press **2** for **Word of God**: a one-second charge, a pillar of light, expanding seals, a particle burst, a brief glowing ascended form with larger wings and levitation, then a transition back to normal. It restores 25 Life and 35 Spirit and awakens nearby waystones in an 18-meter radius. It recharges 30 seconds after casting.
- The bottom HUD places Life on the left and Spirit on the right. Its six numbered slots share keyboard and clickable actions. Slots 3–6 are currently unassigned.
- Awaken all three waystones to complete this small exploration objective.
- Press Escape to exit.

This build focuses on navigation, camera, animation, abilities, resources, and presentation. Life is ready for future combat; the sanctuary currently has no enemies that deal damage. Enemy combat, classes, equipment, pets, saving, campaign generation, and multiplayer are future milestones. The character uses an existing animated model with custom procedural wings and remains prototype art.

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

Build already includes validation; a normal change only needs Build followed by Smoke-Test. The player test exercises click and WASD movement, control handoffs, diagonal speed, obstacle boundaries, HUD click blocking, Spirit spending/regeneration, character animation, and waystone activation. It also exercises the ultimate's charge, transformation, restoration, full 30-second recharge, and reuse. Allow roughly 45–60 seconds for the player checks. Screenshots include the HUD and ultimate. Results go to `Builds/Windows/SmokeTests/`, with the runtime log at `Logs/player-smoke.log`.

## Make changes

- `Assets/Seraphim/Generated/SanctuarySettings.asset`: movement speed, acceleration, camera, seed, and Radiance settings. In the editor, select it in the Project window and edit the Inspector.
- `Assets/Seraphim/Runtime/`: gameplay and world assembly.
- `Assets/Seraphim/Shaders/`: celestial effect shader.
- `Assets/Seraphim/Editor/`: asset import rules and the repeatable project/build setup.
- `Assets/Seraphim/Scenes/Sanctuary.unity`: bootstrap scene and its content references.

The layout is currently authored, with seeded decorative variation. This is not yet a procedural campaign generator. Keep major paths and quest anchors deliberate when expanding it into modular regions.

Build preparation preserves existing settings, materials, and the bootstrap scene. Do not delete their `.meta` files: Unity uses those identifiers to connect assets.

## Adding Asset Store packs later

Import packages into `Assets/ThirdParty/AssetStore/` where practical, and review their included license and dependencies. That directory and `.unitypackage` archives are ignored because the repository is public and raw Asset Store content generally cannot be redistributed. Record any external requirements before making one essential to a build. Do not commit accounts, activation files, or access tokens.

Viking Village, Hovl's Magic Effects FREE, and Mixamo are optional later additions, not dependencies of this build. The initial Quaternius download was unavailable due to Google Drive quota; Kenney's original CC0 downloads were used instead.
