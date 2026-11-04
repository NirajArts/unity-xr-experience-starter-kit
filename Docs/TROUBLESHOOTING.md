# Troubleshooting

| Symptom | Check first | Focused action |
|---|---|---|
| No scene/compile failure | Opened folder, exact Editor, manifest/lock | Open `UnityXRTemplate`, read the first compiler error; restore package resolution before scene edits |
| License IPC timeout | Editor license service and execution sandbox | Use authorized normal-workstation launch; do not alter license files |
| Wrong Editor responds | Bridge project root, scene GUID, process/project mapping | Stop issuing mutations; reconnect to exact project |
| Hands visible but authored fingers unchanged | GripTarget owner, definition policy, adapter binding, draft vs saved profile | Inspect saved data and synthetic visual source; use Grip Editor preview/test |
| Object jumps or rolls | Competing transformers, wrong pivot/frame, synthetic feedback input | Identify the single mechanical owner and raw input; inspect reference poses |
| Tool looks offset from wrist | Grip policy/axis/contact/scale settings | Fix authored contact or object-to-wrist fit; do not globally move the hand model |
| Pink or flat stereo graphics | Shader pipeline/stereo compatibility, material refs | Use compatible shaders; verify both eyes on device |
| UI visible but unresponsive | Active EventSystem, PointableCanvas ray/poke surfaces, graphic raycasts | Verify one active input path and actual callback; inspect button hit plane |
| Part not seating | Compatible ID, occupied slot, pose reference, tolerance | Show guide and inspect distance/angle in the intended workspace frame |
| Part stuck after reset | Selection IDs, kinematic state, animation ownership | Cancel SDK selection before pose restoration; run reset-during-grab test |
| No sound | Muted flag, active listener, output route, clip assignment, focus | Test clip playback independently; do not change mechanics to trigger sound |
| Bridge timeout | Compilation/update state, expired request, project open | Inspect status before retrying; no arbitrary code backdoor |
| Capture unavailable | Graphics device and camera target | Render to PNG with graphics enabled, open it and record observations |

For cross-layer incidents, isolate project code, package/runtime, Editor/player, OS/device and transport. Reproduce the smallest failure, collect timestamped evidence, change one cause at a time, and distinguish observation from hypothesis.
# Build permission and startup checks

On this pinned OpenXR version, targeting Quest Pro causes the package to emit an eye-tracking requirement. The starter's Android postprocessor removes it only when no eye/gaze feature is requested, and must run after Meta's processor. Verify the built APK, not only project settings.

The demo saves its desktop and XR EventSystems inactive. `DesktopPreview` enables one during startup. If creating another scene, keep that ownership rule; two initially active systems can produce a transient startup warning even when one is disabled later.
