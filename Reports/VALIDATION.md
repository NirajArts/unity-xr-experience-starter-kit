# Validation results

Completed 2026-11-04 with Unity **2022.3.62f2**. This is a working starter baseline; physical Quest acceptance remains outstanding.

| Validation layer | Result |
|---|---|
| Compilation | Editor, Windows and Android assemblies compiled |
| Editor tests | **34 passed, 0 failed**, including the reused Grip Editor suite |
| Scene/XR structure | **36 checks passed**: references/scripts, bilateral grips, colliders, rig sources, raw/synthetic visual ownership, ray/poke surfaces, UI input actions/callbacks, OpenXR loaders/features and relative dependencies |
| Simulated Play tests | **11 passed, 0 failed** in an independently imported copy |
| Windows x64 build | Succeeded, **0 build errors** |
| Android IL2CPP/ARM64/Vulkan build | Succeeded, **0 build errors**; APK **69,934,558 bytes** |
| Actual APK manifest | Neutral application ID; API 32 minimum / installed target API 36; optional hand tracking and its permission; Quest 2/Pro/3/3S; no eye-tracking requirement |
| Local bridge | Identity, structural validation, serialized self-test, owned Play/reset/capture/exit and wrong-token refusal verified |
| Portability | Separate directory imported without a copied Library/cache; UPM resolved and tests passed |
| Skills | All **14** SKILL.md files passed the structural skill validator |
| Content audit | No forbidden source-workspace paths, missing asset metas, local-package paths, included caches/credentials or clipping WAV peaks detected |
| Starter journal | **0 bytes** |

The Play suite exercises real SDK pointer selection/movement/release, regrab occupancy, two-pointer handover/no scaling, compatible/occupied slots, recovery while resting on the floor, reset during selection/animation, rotated workspace, assembly/explosion/inspection/reset controls, one active EventSystem, actual world UI graphic raycasting/callback and mechanical audio independent of guidance. These are simulated inputs, not tracked-hand tests.

## Images actually opened and inspected

- [Staged](Screenshots/01-staged.png): three separate parts, central fixture, supported teal/metal/amber materials, empty progress and unobscured instructions/controls.
- [Assembled](Screenshots/02-assembled.png): all parts seated on the central pedestal, completion status and full progress line.
- [Exploded](Screenshots/03-exploded.png): visible gaps between core, collar and cap; the assembly stays below the panel and does not cover controls or instructions.
- [Inspection](Screenshots/04-inspection.png): selected core moved out to the left inspection location; other exploded parts remain separated; controls stay accessible.
- A live bridge runtime capture was also opened and inspected in the staged state.

These camera captures establish desktop material/layout/state rendering. They do not prove headset stereo, physical reach or fitted-hand comfort.

## Built-player and hardware limits

The final Windows player was launched for a startup smoke check. It initialized input/physics with no duplicate EventSystem warning or managed exception in the inspected log. With no working headset runtime, OpenXR reported runtime-unavailable messages and the desktop fallback was used. Hidden-window PrintWindow capture returned black; that PNG was opened, rejected as visual evidence and retained only privately. Built-player mouse interaction and a visible player rendering review were not separately performed; their equivalent demo interactions were tested in Editor Play Mode.

No Android/Quest device was connected. APK install/launch, optical hands, controller tracking, both-eye stereo, physical authored grip fit, haptics, audio perception/spatialization, pause/resume on hardware, comfort and device performance are **unperformed**. The six original WAVs passed format/peak checks and playback-trigger tests, not a human listening approval. No narration recording is shipped; the optional narration/caption component is an extension point.

The official Unity CLI live Pipeline route was not tested; native batch CLI and the project-local bridge were tested independently. Fresh import was on this workstation, not a second PC. Internet, licensed Editor and installed platform modules are first-import/build prerequisites.

## Preservation and evidence

The reference repository remains on its original `dev` HEAD with its pre-existing modified journal and two untracked files. Six critical source content hashes match. One source OpenXR settings timestamp changed while its Editor remained open; its Git content hash still matches HEAD. No agent write/bridge/save/stop command targeted the reference project. No original valve project files were edited.

See [extraction corrections](EXTRACTION.md), [source preservation](SOURCE_PRESERVATION.json), [independent import](PORTABILITY.md), [bridge evidence](CONNECTION_VALIDATION.md), [APK audit](ANDROID_PACKAGE_AUDIT.json), [content audit](CONTENT_AUDIT.json), [Editor checks](EDITOR_VALIDATION.md), [Edit XML](EditMode-results.xml) and [Play XML](PlayMode-results.xml). Build summary `bytes` can include intermediate data; use the APK audit for the actual APK size/hash.

Builds, raw logs, connection exchanges and private validation caches are ignored. The portable repository includes the reports and inspected demo images. No GitHub remote or publication was configured for this new kit.
