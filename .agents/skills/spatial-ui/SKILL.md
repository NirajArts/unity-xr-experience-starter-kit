---
name: spatial-ui
description: Author and validate XR world-space controls, progress indicators, labels, instruction panels and ray/poke input with consistent scale and orientation.
---

Read Docs/ARCHITECTURE.md and Docs/PROJECT_CONFIGURATION.md.

1. Inspect Canvas scale, plane, hit area, EventSystem/input module and actual callbacks. Use the project's existing uGUI/Meta surfaces.
2. Keep controls near their procedure when spatially meaningful and reachable. Keep labels parented to moving faces or indicators.
3. Use modest, legible type and consistent contrast. Put indicator labels in the intended plane; billboard only when requested.
4. Define clockwise/counterclockwise progress from the user's working side. Keep decorative graphics independent from mechanical state.
5. Test ray, poke, desktop fallback, disabled states, repeat clicks and reset. Prevent double activation from competing input paths.
6. Capture and inspect PNGs at realistic viewpoints, then validate stereo readability/reach on a headset when available.
