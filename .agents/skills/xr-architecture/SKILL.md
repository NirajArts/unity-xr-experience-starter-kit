---
name: xr-architecture
description: Design or review an XR experience architecture, device baseline, interaction ownership, or package boundary before a substantial feature or migration.
---

Read AGENTS.md, Docs/PROJECT_CONFIGURATION.md and Docs/ARCHITECTURE.md from the repository root.

1. Inspect actual Unity version, lock, renderer, XR loader/features, rig and scene; distinguish installed packages from active components.
2. Establish target device, raw input source, tracking origin, modality and measurable acceptance criteria. Record unresolved engineering choices.
3. Map one owner for each moving transform and explicit transitions between free grab, constrained motion and animation.
4. Keep mechanics independent from progression, guidance media, visual hands and feedback. Prefer small adapters to a second interaction framework.
5. Identify dependency/licensing impact and a recoverable migration boundary before changing shared infrastructure.
6. Verify a small working slice, then expand. Report Editor, simulated, build and headset evidence separately.

Deliver a concrete architecture decision with preserved contracts, changed dependencies and targeted validation. Do not add networking, voice services or analytics without a task need.
