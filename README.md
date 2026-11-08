# Seraphim

A Windows-first, 3D overhead action-adventure about a six-winged angel. The game uses Unity URP and has no blood or gore.

This repository is the Unity project root. On this computer it lives at `F:\Temp\Apps\seraphim`.

[Wave-ten pressure](Docs/images/wave-ten-pressure.png) · [Ember fields](Docs/images/ember-field.png) · [Guard charges](Docs/images/guard-charge.png) · [Controller HUD](Docs/images/controller-hud.png) · [Cathedral overview](Docs/images/cathedral-overview.png) · [Recorded validation results](Docs/validation.json)

[Main menu](Docs/images/main-menu.png) · [Audio settings](Docs/images/audio-settings.png)

[Vorath, Herald of Ash](Docs/images/vorath.png) · [Meteor rain](Docs/images/meteor-rain.png) · [Hellfire fan](Docs/images/hellfire-fan.png) · [Reaping scythe](Docs/images/reaping-scythe.png)

The previews are camera renders from the exported Windows player, including its native angelic HUD.

## Play the prototype

Double-click **Play Seraphim.cmd** after a successful build. The executable is `Builds/Windows/Seraphim.exe`; distribute its entire folder, including `Seraphim_Data`, not just the executable.

Version **0.8.0** opens at a shaded main menu with **Start** in the upper left. Its background is a live, autonomous battle: a moving seraphim uses all five powers against up to **96 demons**, with eight simultaneous special attacks. Start clears the demonstration and begins a fresh ten-wave campaign with the normal 24-enemy cap, followed by a final boss. **Boss Trial** skips directly to that fight for practice. The demonstration grants no campaign score, drops, or cooldown penalties.

Open **Settings** from the menu or the in-game button to adjust **Music** and **Sound Effects** separately. Zero mutes a channel, and volumes persist between runs. Escape or controller Options/Start opens in-game settings and pauses combat; music continues. Circle/B or Escape returns. Main-menu buttons and sliders support mouse, keyboard arrows/Enter, and controller navigation. Quit is available on the main menu.

- Use WASD, the controller's **left stick**, or click/hold the left mouse button to navigate. Keyboard and stick steering are camera-relative and take over a click path. Centering the stick or releasing the keys stops steering. Partial stick deflection gives slower walking; a radial deadzone rejects drift. WASD takes priority if keys and stick steer simultaneously, without adding their speeds.
- Scroll to zoom the overhead camera.
- Use **1–5** or controller buttons for the powers below. **Word of God is now slot 5.** Aim keyboard/mouse casts with the mouse; controller casts use the right stick with light aim assistance, or target a nearby demon when the aim stick is centered. Space and the right mouse button also cast Radiance.
- Keyboard, mouse, and gamepads are available together, with shared resources and cooldowns. No mode switch is required. Both Xbox/XInput and DualSense layouts use Unity's Input System; hot-plugging a controller is supported. HUD button hints follow the active controller family.
- Spirit regenerates at 12 per second. The bottom HUD places Life on the left and Spirit on the right, with clickable numbered skills and cooldowns in the center. Slot 6 remains available for a future skill. Clicking the HUD cannot issue a movement command; aimed skills cast from their HUD buttons select a nearby demon.
- Defend the cathedral through **ten waves**, with 10 demons in the first wave and 64 in the last: **370 total**. The first wave allows four living enemies at once; the limit rises to **24**. Spawn intervals fall from 1.25 to 0.32 seconds, with pairs from wave 6 and three-enemy bursts from wave 9. Six-second rests between waves restore some Life and Spirit.
- From wave 6, enemies also arrive in timed reinforcement groups, 12 seconds apart. Wave 10 unlocks groups of **22, 21, and 21** at 0, 12, and 24 seconds. This spreads pressure beyond the starting ultimate's active window. Actual spawns still obey the living cap and burst interval; the HUD counts down reinforcements when the floor is cleared early.
- Demons start 20% larger and grow further through the scenario. By wave 10 they are about 47% larger than the original models, move roughly 59% faster than their base speed, and have 2.62 times base damage and health. Late guards gain additional health, reaching about 195 HP. Knockback becomes less effective, while lightning's stun remains a reliable interrupt.
- **Keep moving:** melee strikes warn at a fixed location before landing. From wave 3, Ash Stalkers hold their distance and mark a **2.5-meter ember field** at your position, with a 1.05-second warning before repeated damage. From wave 5, guards show a **locked charge lane** for 0.9 seconds before rushing along it. Sidestep these warnings; solid architecture blocks charges. At most **four special attacks** can be active at once.
- Lightning interrupts pending fields and charging guards; killing a caster clears its field. Spears reach enemies beyond Radiance range, while Inferno damages groups and burns survivors. Combine these tools with movement and the ultimate when pressure builds. Word of God retains its 15-second moving aura and 30-second cooldown; it does not make the player invulnerable.
- Cinder Imps, winged Ash Stalkers, and armored Obsidian Guards have different health and movement speeds. Their overhead bars show remaining health, recent damage, and floating damage numbers. Each defeat awards **100/125/150 points**, plus 10 per wave after the first. Demons disperse into light without blood or gore.
- Every demon drops a **Life orb (+12)** and a **Spirit orb (+18)**. Walk near a drop to attract and collect it. Orbs stay available while the matching resource is full, expire after 45 seconds, and use a pool capped at 48. Nearby walls and pews block attraction.
- The **top-right minimap** shows the cathedral floor plan, obstacles, a gold player arrow, cyan waystones, and **red enemy dots**. Its north-up orientation is fixed. Clicking it does not move the character.
- If Life reaches zero, you return to the starting point with full resources and brief protection. The current wave restarts after a rest, retaining earned points. During the final boss, defeat retries that fight with full boss health after a seven-second rest. After banishing Vorath, press **R**, controller **Options**, or **Start** to replay with a fresh score and full resources. Score is per session and is not saved yet.
- Awaken the three waystones as an optional exploration objective while defending the cathedral.
- Press Escape or controller Options/Start for settings during play. Return to the main menu to quit.

| Key | Power | Cooldown | Spirit | Effect |
| --- | --- | --- | --- | --- |
| 1 | Radiance | 0.6 s | 6 | Expanding light pulse; 24 damage per enemy, with a small knockback. Also awakens nearby waystones. |
| 2 | Lance of Dawn | 1.8 s | 12 | Three piercing golden spears with laser trails; 55 damage per spear hit. Solid architecture blocks the spears. |
| 3 | Chain of Heaven | 3 s | 18 | Cyan lightning jumps through up to four demons and stuns them briefly. First hit deals 36 damage, with 10% less per jump. Aim at the first demon. |
| 4 | Seraphic Inferno | 4.5 s | 22 | Expanding white-gold flame corona; 35 damage, knockback, and three additional burn ticks of 8 damage. |
| 5 | Word of God | 30 s | Free | One-second charge, followed by **15 seconds of pulses that travel with you**: 16 damage each second within 18 meters. Glowing ascended form, enlarged wings, levitation, a pillar of light, seals, and particles. Restores 25 Life and 35 Spirit once at revelation, then fades back to normal. Cooldown starts immediately on casting. |

| DualSense | Xbox 360 / Xbox | Key | Power |
| --- | --- | --- | --- |
| Cross (bottom face button) | A | 1 | Radiance; hold to repeat |
| Square (left face button) | X | 2 | Lance of Dawn |
| Circle (right face button) | B | 3 | Chain of Heaven |
| Triangle (top face button) | Y | 4 | Seraphic Inferno |
| D-pad Up | D-pad Up | 5 | Word of God |

The Xbox mapping follows the physical positions of the requested PlayStation buttons: PlayStation Cross corresponds to Xbox **A**, while Xbox **X** fires spears. Secondary abilities and the ultimate require a new button press. Unplugging a pad clears its held movement and attacks; keyboard and mouse remain available. Controllers must be powered on and exposed to Windows as active input devices. A Bluetooth pairing entry alone does not establish a live gamepad connection.

The sanctuary is now an **84 × 112 meter cathedral**, nearly six times the original floor area. Its cutaway architecture includes a nave, side aisles, Gothic ribs, clustered columns, wooden pews, candelabra, lancet windows, a rose window, memorials, and a raised high altar. Three CC0 2K texture sets provide marble, masonry, and oak, with world-scaled UVs and normal maps.

The playable angel uses KayKit's rigged Knight as an armored base, an ivory palette shader, bone-mounted celestial vestments, a crown, and six animated wings with sculpted feathers. The body and demons remain stylized prototype art. This is a playable wave scenario; classes, equipment, pets, saving, campaign generation, and multiplayer are future milestones.

## Final boss: Vorath, Herald of Ash

After all **370 wave demons**, a seven-second preparation rest restores 35 Life and full Spirit. Vorath then descends nearby on reachable ground. His original procedural model stands over eight meters tall, with a skeletal rib cage, horned skull, torn mantle, spectral bone train, turning sigil crown, and hooked scythe. A large top-center health bar shows his health, phase, and attack warning; the minimap marks him with a larger magenta dot. The camera widens and includes him while retaining player movement and aiming controls.

Vorath has **4,800 HP** and three phases: phase two below 65% health and phase three below 30%. He moves toward distant players, alternates ranged patterns, and uses a separate close-range scythe attack. Later phases shorten ranged intervals from 4.3 to 3.7 to 3.1 seconds. All five player powers damage him, including Inferno's burn; boss poise prevents permanent stun or knockback locking. Each phase transition releases two nearby pairs of restoration orbs, which require moving to collect.

| Attack | Warning and damage | How to evade |
| --- | --- | --- |
| Meteor rain | Three / four / five fixed circles by phase; 1.5-second warning plus staggered impacts. Each meteor deals 32 damage and leaves a 4.2-second crater that burns for 11 damage every 0.65 seconds. | Leave the marked circles and avoid the burning ground. Targets stop tracking once marked. |
| Hellfire fan | Five / seven traveling firebolts, with 1.15 seconds of marked paths before launch. Each bolt deals 27.5 damage once, travels at 11 m/s, and stops at solid architecture. | Move between the paths, behind the boss, or behind solid architecture. |
| Hollow nova | A purple annular warning for 1.45 seconds, then 34 damage between roughly 5 and 12 meters. Phase three repeats the nova after 1.9 seconds. | Enter the safe inner circle or escape beyond the outer circle; watch the repeated cast. |
| Reaping scythe | A locked 130-degree sector reaching 5.8 meters, with a 1.05-second windup, followed by an animated weapon sweep dealing 38 damage once. | Get behind him or retreat beyond the sector before the blade lands. |

Victory requires defeating Vorath and awards **5,000 bonus points** once. His death clears pending impacts, fire, and slashes. Boss effects use at most **18 reusable hazard objects**; the dedicated boss instance cannot be reused as an ordinary guard. Leaving, retrying, replaying, and starting a new campaign clear the fight and restore normal camera settings. The model and attacks contain no blood or gore. Difficulty is a first tuning pass and still needs human playtesting.

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
- URP **17.3.0**, Unity UI **2.0.0**, and Input System **1.20.1**, pinned in `Packages/manifest.json`; resolved dependencies are in `Packages/packages-lock.json`. Both input back ends are enabled to preserve existing keyboard/mouse and native HUD behavior.
- Windows x64 and Direct3D 11 for this prototype. The local build uses Unity's bundled Mono toolchain; Visual Studio and mobile SDKs are not required.
- Git. Binary installers, Unity caches, credentials, and generated builds are excluded from this public repository.

The build script checks the usual Unity Hub install path. For a different installation, set `SERAPHIM_UNITY_EDITOR` to the full path to `Unity.exe`, or create ignored `Tools/local.json` with a `unityEditor` property. Machine-specific paths are not baked into the game.

The prototype's CC0 assets and OFL-licensed display font are included in the repository. To reacquire them:

```powershell
.\Tools\Download-Assets.ps1
```

The downloader uses the original publishers, caches archives locally, and verifies pinned SHA-256 values in `Tools/asset-lock.json`. Cathedral assets additionally use the pinned publisher revision and file hashes in `Assets/ThirdParty/CC0/KayKitAngel/source.json` and `CathedralTextures/source.json`; run `Tools/Download-CathedralAssets.ps1` to verify or reacquire just those dependencies. A changed upstream file intentionally fails verification until its update is reviewed. Sources and licenses are recorded alongside the assets and in `THIRD_PARTY_NOTICES.txt`. The build includes the font's full OFL license in the Windows distribution.

The Windows MSIX distribution of Unity Hub can place activation in its isolated per-user cache. The build helper makes that same user's existing signed license files available to the standalone Editor without modifying their contents. Licenses stay outside this repository.

Unity Package Manager installs the pinned Input System package automatically. Its official registry source, license, and verified archive checksums are recorded in `Tools/package-sources.json`; the package cache is excluded from Git.

The local Windows build includes the owner-authorized **Track Realm** recording, starting at **1:03:51** and looping through the end of Repeat Cycle 3 at **1:35:40** (31 minutes 49 seconds). The game reads a local streaming OGG file; it does not contact YouTube during play. Recording provenance and conversion-tool checksums are in `Tools/music-source.json`. The recording and its SHA-256 receipt stay in ignored `LocalAudio/`; build preparation copies them into `Builds/Windows/Music/`. Permission to use the recording was confirmed by the project owner; no raw recording is published in this repository.

A clean checkout uses an original 32-second choir/organ-style loop. All combat, pickup, movement, wave, and UI effects are generated from original synthesis code, with a bounded 20-voice pool and hit-rate limits. To prepare the authorized recording again, supply its local file and an FFmpeg installation:

```powershell
.\Tools\Import-LicensedMusic.ps1 -InputFile 'C:\Music\authorized-recording.webm' -FfmpegPath 'C:\Tools\ffmpeg.exe'
.\Tools\Build.ps1
```

Use `-AlreadyTrimmed` only if the input already begins at 3831 seconds and ends at 5740 seconds in the source recording. Conversion applies half-second boundary fades; the whole selected segment repeats. The long recording is streamed from disk rather than decoded entirely into memory.

## Check a change

```powershell
.\Tools\Validate.ps1
.\Tools\Build.ps1
.\Tools\Smoke-Test.ps1
```

Build already includes validation; a normal change only needs Build followed by Smoke-Test. The player test exercises click and WASD movement, control handoffs, diagonal speed, obstacle boundaries, HUD click blocking, Spirit spending/regeneration, character animation, and waystone activation. It checks the ultimate's traveling aura, fifteen pulses, transformation, restoration, full 30-second recharge, and reuse. Combat checks cover spawning, population caps, health bars, damage, demon reuse, spears, lightning, burn ticks, melee attacks, and recovery after defeat. Cathedral checks cover the model and textures, navigation through the expanded world, minimap behavior, drop collection, scoring, all ten waves, the final boss, victory, replay, and recovery quotas. Allow roughly 130–180 seconds for the full player checks. Screenshots include the HUD, each power, cathedral previews, and boss attacks. Results go to `Builds/Windows/SmokeTests/`, with the runtime log at `Logs/player-smoke.log`.

For a focused combat iteration, use `.\Tools\Smoke-Test.ps1 -CombatOnly`. This skips baseline movement and the long ultimate recharge sequence; run the full test before releasing gameplay changes. The smoke-test launch uses isolated encounters instead of the normal automatic encounter so damage and cooldown checks are repeatable.

Use `.\Tools\Smoke-Test.ps1 -ScenarioOnly` for focused cathedral, drop, minimap, and wave checks. The ten-wave scheduler is exercised with an advancing test clock and lethal test hits; the remaining combat checks use normal ability damage. Automated screenshots are actual GPU renders from the exported player, including two preview camera angles. These checks do not replace a human balance playtest or a frame-rate benchmark.

Use `.\Tools\Smoke-Test.ps1 -ControllerOnly` for focused controller checks. The full run includes these checks too: actual Input System state events drive a temporary gamepad through analog movement, control handoffs, every face-button binding, D-pad ultimate, controller aiming, shared cooldowns, held-button behavior, and reconnection. Native DualSense-format reports additionally validate axis normalization, all five attack buttons, Options, and PlayStation prompts. Temporary devices are removed afterward. The report records physical gamepads detected by the player; automated injected events do not constitute a physical button-by-button playtest.

Use `.\Tools\Smoke-Test.ps1 -DifficultyOnly` for pressure checks. These exercise late-wave melee damage, marked-field damage and escape, lightning interrupts, special-attack limits, charge dodges and wall collisions, pooled difficulty reset, a three-power kill, and a live 24-enemy wave-ten scene. Wave progression in the scheduler checks uses an accelerated clock; dodges and damage checks run in real time. The wave-ten preview uses a wider camera view. Human playtesting is still needed to assess the challenge and frame rate.

Use `.\Tools\Smoke-Test.ps1 -FrontEndOnly` for menu/showcase, start/reset, pause/resume, independent volumes, preference persistence, and music-loading checks. Add `-FallbackMusic` to verify the original soundtrack without removing the authorized local recording. These checks restore the user's saved volumes afterward. Audio signal measurements verify playback, not perceived musical quality or speaker routing.

Use `.\Tools\Smoke-Test.ps1 -BossOnly` for the final encounter. Checks cover all five powers, boss poise, health and phase transitions, meteors and persistent fire, warning escape routes, traveling fire and wall blocking, nova safe zones, scythe hits and dodges, automatic attack chaining, hazard bounds, retry cleanup, final scoring, and normal guard reuse. Full scenario checks additionally verify that wave ten leads to the boss and that victory requires all 371 enemies. Boss Trial uses the same encounter as the normal campaign.

## Make changes

- `Assets/Seraphim/Generated/SanctuarySettings.asset`: movement speed, acceleration, camera, seed, resource regeneration, ability tuning, and enemy population. In the editor, select it in the Project window and edit the Inspector.
- `Assets/Seraphim/Runtime/`: gameplay and world assembly.
- `SeraphimGamepadInput.cs`: native controller discovery, semantic button bindings, stick filtering, controller aiming, and prompts. `SeraphimSkillBar.cs` defines the shared numbered skill order; `SeraphimMotor.cs` handles movement arbitration with keyboard and mouse.
- `EnemyDirector.cs`, `DemonEnemy.cs`, and `DemonHealthBar.cs`: spawning, damage/status effects, demon behavior, and overhead readouts. Demons and spell effects are reused through bounded pools.
- `WaveDirector.cs`: ten-wave quotas, spawn pacing, concurrency limits, intermissions, victory, and retries. `CelestialDrops.cs` owns pooled restoration drops; `CathedralMinimap.cs` draws the same obstacle definitions used to build navigation.
- `DemonDifficulty.cs`: settings-driven health, damage, speed, visual scale, and resistance curves. `DemonOffense.cs` owns pooled attack warnings, ember fields, interruptible charges, and melee attacks. Wave-pressure tuning lives in the existing `SanctuarySettings.asset`.
- `SeraphimFrontEnd.cs` owns menu/settings UI and input handoffs; `CathedralShowcase.cs` runs the automatic menu battle. `SanctuaryAudio.cs` separates persistent channel volumes, streamed music, and bounded effects voices; `SanctuarySound.cs` creates original clips.
- `CathedralBoss.cs` schedules the final encounter and phase changes; `BossHazardField.cs` owns bounded warning geometry and damage. `CathedralBossArt.cs` creates and animates the original model. `BossHealthHud.cs` presents the shared enemy health pool. Boss tuning lives in the existing `SanctuarySettings.asset`.
- `CathedralBuilder.cs`: cathedral geometry, world-scaled surfaces, lighting props, collision, and minimap placement definitions. `AngelicVestments.cs` and `SeraphimWings.cs` own the angel additions.
- `DivinePowers.cs` and `DivineProjectile.cs`: the three new powers, cursor aiming, and swept projectile hits. `CelestialAbility.cs` and `WordOfGodAbility.cs` own the two pulse powers.
- `Assets/Seraphim/Shaders/`: celestial effect shader.
- `Assets/Seraphim/Editor/`: asset import rules and the repeatable project/build setup.
- `Assets/Seraphim/Scenes/Sanctuary.unity`: bootstrap scene and its content references.

The cathedral layout is currently authored; demon positions vary between runs. This is not yet a procedural campaign generator. Keep major paths and quest anchors deliberate when expanding it into modular regions.

Build preparation preserves existing settings, materials, and the bootstrap scene. Do not delete their `.meta` files: Unity uses those identifiers to connect assets.

## Adding Asset Store packs later

Import packages into `Assets/ThirdParty/AssetStore/` where practical, and review their included license and dependencies. That directory and `.unitypackage` archives are ignored because the repository is public and raw Asset Store content generally cannot be redistributed. Record any external requirements before making one essential to a build. Do not commit accounts, activation files, or access tokens.

Viking Village, Hovl's Magic Effects FREE, and Mixamo are optional later additions, not dependencies of this build. The initial Quaternius download was unavailable due to Google Drive quota; Kenney's original CC0 downloads were used instead.
