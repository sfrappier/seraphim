# Seraphim project instructions

- The repository root is the Unity project root. Keep Unity 6000.3.28f1 and URP 17.3.0 pinned until an upgrade is requested or needed for a documented fix.
- Windows x64 is the current release target. Maintain the overhead camera, WASD and clicking movement, six-winged angel identity, and no blood/gore direction.
- Use C# for gameplay and editor automation. Use the existing build tools instead of requiring manual scene wiring for routine changes.
- `Tools/Build.ps1` includes scene/shader/dependency validation. After gameplay changes, also run `Tools/Smoke-Test.ps1` against the exported Windows player and inspect relevant screenshots. Report actual results and limitations.
- Preserve asset `.meta` files and serialized settings. Existing settings assets override C# field initializer defaults; change the relevant asset when changing an existing project's tuning.
- Never commit Library, Temp, Logs, UserSettings, Builds, installer caches, tokens, license files, or machine-local tool configuration.
- This is a public repository. CC0 assets and source notices may be checked in. Keep restricted Asset Store content in the ignored `Assets/ThirdParty/AssetStore/` directory; inspect licenses before adding other third-party content.
- Record asset origin, license, and checksums when adding downloadable dependencies. Keep the project buildable from a clean checkout plus the pinned editor and an active local Unity license.
- Keep movement, abilities, presentation, and content definitions separate. Expand systems through small playable milestones rather than implementing every planned RPG feature at once.
- The HUD uses native Unity UI in camera space. Keep Life left, Spirit right, numbered skill slots in the center, and HUD clicks isolated from world movement. Preserve the 30-second Word of God cooldown and restore temporary character/camera effects after casting.
- Word of God follows the player and pulses for 15 active seconds after charging. Keep all five combat powers playable, enemy spawning bounded, and damage/status effects and overhead health bars consistent when pooled demons are reused.
- Keep the ten-wave cathedral scenario, score, bounded restoration drops, and north-up minimap coherent. Navigation, spawn bounds, and map landscape share cathedral dimensions and obstacle definitions. Defeat retries the current quota; victory stops spawning and permits replay.
