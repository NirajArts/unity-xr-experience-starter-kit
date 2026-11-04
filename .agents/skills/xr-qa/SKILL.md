---
name: xr-qa
description: Plan and execute XR regression and acceptance checks across Editor structure, simulated interaction, visual inspection, builds and physical headset behavior.
---

Read Docs/TEST_MATRIX.md and Reports/VALIDATION.md for prior evidence, then verify the current state.

1. Turn the requested behavior into observable acceptance criteria. Preserve authored poses, grips, scene GUIDs and existing successful flows.
2. Run relevant Edit and Play tests; test adverse paths such as held reset, interrupted animation, occupied slots, drops and rotated workspaces.
3. Verify actual UI callbacks, input routing, media cancellation and mechanical/completion audio independence.
4. Capture important states to PNG and actually inspect them. Record specific visual observations, not only filenames.
5. Build the intended target, then perform device checks if hardware is available: both eyes, left/right input, physical grip fit, tracking loss, pause/resume and performance.
6. Report each layer separately with pass/fail/not-run and evidence. Never promote simulated SDK input or compilation to headset acceptance.
