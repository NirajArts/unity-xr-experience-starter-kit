---
name: assembly-inspection
description: Build generic assembly/disassembly, compatible placement slots, exploded views, inspection selection and reversible staging without corrupting final mating poses.
---

Read Docs/ARCHITECTURE.md and Docs/NEW_EXPERIENCE.md.

1. Capture final assembled poses before moving anything. Keep mating, staging, exploded and inspection transforms separate under the workspace.
2. Define stable part/slot IDs, compatibility and occupancy. Interchangeable parts may share a destination class, but cannot occupy one slot twice.
3. Use local frames for repositionable workspaces. Do not guess attachment axes from mesh pivots.
4. Cancel grabs before authored movement; restore the intended free or seated state after it. Handle interruption and repeated reset.
5. Show nearby placement feedback and selected-part information without changing progress merely because a panel opens.
6. Test manual placement, automated reassembly, explode/inspect/reset, occupied slots and a rotated workspace.
