---
name: authored-animation
description: Integrate captured poses, model animation clips, constrained motion or manual animation controls without competing with XR grabbing or moving unrelated parents.
---

Read Docs/INTERACTION_CONTRACTS.md.

1. Inspect actual clip bindings, frame rate, duration, loop endpoints, local axes and hierarchy. Do not assume frame zero is an approved engineering target.
2. Capture approved start/end/intermediate references before runtime staging. Keep original assembled references immutable.
3. Choose the narrowest motion root; confirm unrelated geometry and references do not inherit an unintended rotation.
4. Transfer transform ownership explicitly from grab to animation. Drive user-controlled animation from signed raw input with bounded, stable response.
5. Keep visible motion, progress, target tolerance and final confirmation snap consistent. Preserve continuity at loop boundaries.
6. Test reverse motion, overshoot, stop, interruption/reset and relocated workspaces. Inspect images through the full movement, not just endpoints.
