# Test matrix

| Layer | Required checks | Evidence |
|---|---|---|
| Editor | Compilation; no missing scripts/broken references; input actions; XR loader/features; build scene; bilateral grip definitions | Edit tests + `EDITOR_VALIDATION.md` |
| Simulated Play | SDK select/move/release; handover; no scale drift; compatible/vacant slots; reset while held/moving; dropped-part recovery; rotated workspace; UI callbacks; independent feedback | Play test XML |
| Visual desktop | Staged, assembled, exploded, inspection; readable UI; no intersections/unsupported materials | Opened/inspected PNGs + observation report |
| Builds | Windows player; Android IL2CPP ARM64/Vulkan; dependency/permission review | Build reports and logs |
| Portability | Fresh directory without Library; UPM resolves; compile/validate; no external paths or missing metas | Clone/import audit |
| Physical Quest | Left/right hands and controllers, ray/poke, near grab/release, tracking loss/reacquire, physical grip fit, two-eye rendering, audio/spatialization, pause/resume, performance | Human/device acceptance record required |

Scripted pointer events do not prove optical hand tracking, controller haptics, hand mesh fitting or reach comfort. Camera PNGs do not prove single-pass stereo on a headset. A successful APK does not prove install/launch/device behavior.

Use meaningful regression tests for changed mechanics. Do not duplicate implementation line-by-line in tests. Keep deterministic test inputs and explicit assertions about user-visible outcomes. Record initial failures and fixes, not only the final passing total.
