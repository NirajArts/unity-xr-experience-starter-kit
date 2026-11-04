---
name: rig-input-setup
description: Configure or diagnose the Meta OpenXR rig, tracked controllers/hands, desktop fallback, input actions and XR UI routing in this starter.
---

Read Docs/PROJECT_CONFIGURATION.md and Docs/CONNECTIONS.md.

1. Verify the saved OpenXR loader, feature set, package versions, target platform and active runtime. This baseline is Meta Interaction SDK, not XRI.
2. Inspect OVRCameraRig, tracking anchors, OVRCameraRigRef, raw hand sources, controller sources and SyntheticHand visuals.
3. Maintain exactly one active camera/listener/input route for the selected mode. World UI needs PointableCanvas and the matching module; desktop uses its own action asset.
4. Keep tracking origin and workspace placement independent. Do not add an unrequested second rig or locomotion system.
5. Check raw input before visual output. Test both hands/controllers, ray/poke, disconnect/reacquire and pause/resume.
6. Report simulated checks separately from physical tracking and stereo acceptance.
