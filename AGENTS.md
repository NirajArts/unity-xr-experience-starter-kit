# Agent operating rules

The Unity project is `UnityXRTemplate/`. Discover the actual checkout path and read `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`, `packages-lock.json`, scene/build configuration and current Git status before editing. Do not assume a workstation path, a live Editor, XRI, URP or a headset.

## Preserve

- Preserve user-owned assets, scene GUIDs, uncommitted changes, current branch and unsaved scenes. Never replace an authored scene with the demo generator.
- This repository is independent of its reference project. Do not discover, edit, stage or publish files in a neighboring production project to solve a starter task.
- Do not reset Git, upgrade packages, change render pipeline, publish, or change remotes without task authorization.
- Keep `Docs/PROJECT_JOURNAL.md` **entirely empty** in the distributable starter. Record starter extraction/testing in `Reports/`. A downstream project can establish its own journal with explicit authorization.
- Preserve required SDK notices and package-managed assets. Do not copy caches, credentials, licensed store assets or client content into Assets.

## Connect and implement

Use either the project-local bridge or Unity CLI as described in `Docs/CONNECTIONS.md`. Verify returned project path, Unity version, active scene path/GUID, dirty state and Play state before mutation. A status request is not permission to save unsaved authoring or stop a user's Play session.

Use supported Editor APIs, Undo, SerializedObject, PrefabUtility and EditorSceneManager. Avoid handwritten scene/prefab YAML. Do not create arbitrary-code execution endpoints. The bridge supports bounded commands and token-owned Play sessions. Timeouts are uncertain outcomes: inspect fresh state before retrying.

Use the smallest matching skill under `.agents/skills/`. No delegation is required. Make behavior concrete, then validate it; do not stop at a deletion list, compile-only success or a proposed test plan.

## Interaction contracts

Read `Docs/INTERACTION_CONTRACTS.md`. Raw tracked input drives mechanics. Synthetic hand visuals never feed mechanical calculations. Exactly one system owns a transform at a time. Release SDK selection before scripted motion/reset. Preserve separate staging/mating/intermediate poses. Occupancy and compatibility are explicit. Media and feedback observe mechanical state without changing it.

## Validate and report

Run focused Edit/Play checks and appropriate builds. Test reset while held and during animation, wrong/occupied sockets, tracking loss, reversed workspace orientation, UI input, audio cancellation and recovery. Capture PNGs and actually inspect them; saving is not visual validation. Avoid repeatedly retrying a broken live capture session.

Distinguish structural Editor checks, simulated SDK events, desktop builds, Android builds and physical headset testing. Do not claim Quest acceptance, performance or physical hand fit without device evidence. Report failures and limitations, changed files, recovery boundaries and pending configuration choices. Keep evidence free of private paths or machine credentials.
