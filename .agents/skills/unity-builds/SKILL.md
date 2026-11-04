---
name: unity-builds
description: Produce and diagnose Windows or Quest Android builds using the pinned Unity baseline, safe CLI discovery, package resolution and explicit device acceptance limits.
---

Read Docs/BUILD_RUNBOOK.md, Docs/DEPENDENCIES.md and Docs/REDISTRIBUTION.md.

1. Verify exact Editor version, installed modules, project identity, enabled scene list and clean/dirty state. Do not launch a second Editor on an open project.
2. Resolve the committed package lock through UPM. Inspect OpenXR validation and target identity before building.
3. Run relevant tests and use Tools/Invoke-Unity.ps1 with the installed Editor. Keep logs/reports distinct from ignored binary outputs.
4. Diagnose the first build failure at its actual layer: compile, shader, SDK, native toolchain, signing or packaging. Do not alter source packages or unrelated projects.
5. Keep credentials outside Git. Review permissions and applicable target SDK/distribution rules for the actual release.
6. Record build result and artifact hash. Install/test only when authorized and hardware is available; report unperformed headset acceptance explicitly.
