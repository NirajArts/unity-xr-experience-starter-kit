---
name: unity-connections
description: Discover and use either native Unity CLI or the starter's bounded project-local bridge, protecting unsaved scenes and user-owned Play sessions.
---

Read Docs/CONNECTIONS.md in full before connecting.

1. Resolve the repository and Unity project paths, exact Editor version and current process ownership. Never assume a historical workstation path.
2. For a live Editor, obtain status and compare project root, Unity version, scene path/GUID, dirty state and Play state before mutations.
3. Use the local wrapper's bounded actions, fresh IDs/expected values, dry runs and request deadlines. No arbitrary eval command or network endpoint.
4. Automated Play can only exit/reset a session it started with its returned token. Preserve pre-existing Play and unsaved scenes.
5. For a closed project, use native batch CLI for tests/builds/captures. The Unity 6+ live Pipeline is optional and unavailable on this baseline; do not require both routes.
6. After a timeout, inspect state before retrying. Open captured PNGs and distinguish structural checks from visible/runtime/device validation.
