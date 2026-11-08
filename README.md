# Seraphim

A Windows-first, 3D overhead action-adventure about a six-winged angel. The game uses Unity URP and has no blood or gore.

This repository is the Unity project root. On this computer it lives at `F:\Temp\Apps\seraphim`.

[Sanctuary preview](Docs/images/sanctuary.png) · [Radiance preview](Docs/images/radiance.png) · [Recorded validation results](Docs/validation.json)

The previews are camera renders from the exported Windows player; its interface is not included in the automated camera captures.

## Play the prototype

Double-click **Play Seraphim.cmd** after a successful build. The executable is `Builds/Windows/Seraphim.exe`; distribute its entire folder, including `Seraphim_Data`, not just the executable.

- Click or hold the left mouse button to move. The character navigates around the altar and columns.
- Scroll to zoom the overhead camera.
- Press Space or the right mouse button to release Radiance. Stand near a waystone to awaken it.
- Awaken all three waystones to complete this small exploration objective.
- Press Escape to exit.

This first build focuses on navigation, camera, animation, effects, and the development workflow. Enemy combat, classes, equipment, pets, saving, campaign generation, and multiplayer are future milestones. The character is a prototype using an existing animated model with custom procedural wings, not finished character art.

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
- URP **17.3.0**, pinned in `Packages/manifest.json`; resolved dependencies are in `Packages/packages-lock.json`.
- Windows x64 and Direct3D 11 for this prototype. The local build uses Unity's bundled Mono toolchain; Visual Studio and mobile SDKs are not required.
- Git. Binary installers, Unity caches, credentials, and generated builds are excluded from this public repository.

The build script checks the usual Unity Hub install path. For a different installation, set `SERAPHIM_UNITY_EDITOR` to the full path to `Unity.exe`, or create ignored `Tools/local.json` with a `unityEditor` property. Machine-specific paths are not baked into the game.

Free CC0 assets used by the prototype are included in the repository. To reacquire them:

```powershell
.\Tools\Download-Assets.ps1
```

The downloader uses the original publishers, caches archives locally, and verifies the pinned SHA-256 values in `Tools/asset-lock.json`. A changed upstream archive intentionally fails verification until its update is reviewed. Sources and licenses are recorded in `Assets/ThirdParty/CC0/asset-sources.json` and `THIRD_PARTY_NOTICES.txt`.

The Windows MSIX distribution of Unity Hub can place activation in its isolated per-user cache. The build helper makes that same user's existing signed license files available to the standalone Editor without modifying their contents. Licenses stay outside this repository.

## Check a change

```powershell
.\Tools\Validate.ps1
.\Tools\Build.ps1
.\Tools\Smoke-Test.ps1
```

Build already includes validation; a normal change only needs Build followed by Smoke-Test. The player test checks real movement, arrival, routing around the altar, six wings, camera initialization, waystone activation, and cooldown behavior. It also captures gameplay screenshots and watches for runtime errors. Results and screenshots go to `Builds/Windows/SmokeTests/`, with the runtime log at `Logs/player-smoke.log`.

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
