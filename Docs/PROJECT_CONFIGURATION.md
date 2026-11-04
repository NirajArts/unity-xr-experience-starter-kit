# Project configuration

Open `UnityXRTemplate/` with **2022.3.62f2**. The revision is recorded in `ProjectVersion.txt`. Keep the committed package lock when cloning. Do not open this baseline in Unity 6 without a separately tested migration.

| Setting | Baseline |
|---|---|
| Renderer | Built-in, Linear colour; Standard materials; no URP asset |
| XR provider | OpenXR, Android and Standalone; single-pass instanced stereo |
| Interaction | Meta Interaction SDK / OVR data sources; not XRI |
| Tracking origin | Floor level; rig root at workspace entry |
| Input | Meta controller/hand sources for XR; Input System actions for desktop UI |
| Android | IL2CPP, ARM64, Vulkan, minimum API 32, installed automatic target SDK |
| Desktop | Windows x64, Direct3D11; non-XR preview if no device |
| Identity | Neutral company/product/bundle identifiers; replace before shipping |
| Spatial services | Passthrough, scene understanding and anchors are not required by the demo |
| Scene | `Assets/StarterKit/Scenes/ExperienceDemo.unity`, only enabled build scene |
| Physics | Metres, positive scale wrappers; layer 8 `Grabbable`, default collision matrix |

The layer name is an authoring aid; Meta proximity/selection is determined by its colliders and interaction components, not XRI layer masks. Do not add a second competing XR rig or input stack.

Meta hand tracking and OpenXR features are saved in project assets/settings. Confirm them through `XR Starter > Validate Loaded Demo`, then check Project Settings > XR Plug-in Management > OpenXR validation before a device build. The source's exact versions were the starting point; unrelated packages and production IDs were not copied.

The automatic desktop fallback is a convenience, not an XR emulator. Headset behavior still requires a suitable active OpenXR runtime, permissions and a real device. Use `DesktopPreview.forceDesktop` explicitly for controlled desktop-only previews.

This demo does not use eye tracking. OpenXR 1.14.3 otherwise adds a mandatory eye-tracking manifest requirement for Quest Pro, so `StarterAndroidManifest` removes that unrequested requirement after Meta's build processor. Enabling an OVR eye-tracking option or an OpenXR eye/gaze feature bypasses that safeguard. Review the final APK permissions whenever adding features or upgrading packages.
