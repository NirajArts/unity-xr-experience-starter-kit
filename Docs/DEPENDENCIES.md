# Dependency inventory

`UnityXRTemplate/Packages/manifest.json` declares the baseline; `packages-lock.json` records resolved transitive versions. UPM resolves them from its supported registry. There are no `file:` dependencies or paths to the reference workspace.

| Package | Version | Purpose |
|---|---|---|
| com.meta.xr.sdk.core | 77.0.0 | OVR tracking, camera rig and platform integration |
| com.meta.xr.sdk.interaction | 77.0.0 | Grab/ray/poke, synthetic hands, pose definitions |
| com.meta.xr.sdk.interaction.ovr | 77.0.0 | OVR input wiring and interaction rig prefabs |
| com.unity.xr.openxr | 1.14.3 | XR runtime loader and feature configuration |
| com.unity.xr.management | 4.5.1 | Per-platform XR lifecycle |
| com.unity.xr.core-utils | 2.5.2 | XR utilities |
| com.unity.inputsystem | 1.14.0 | Desktop UI actions and package dependency |
| com.unity.ugui | 1.0.0 | World-space UI |
| com.unity.textmeshpro | 3.0.7 | Compatible reusable tooling/package baseline; demo uses built-in font |
| com.unity.test-framework | 1.1.33 | Edit/Play tests |
| com.unity.ext.nunit | 1.0.6, transitive | NUnit assertions |
| com.unity.xr.legacyinputhelpers | 2.1.12, transitive | XR package dependency |

Built-in modules use Unity's 1.0.0 module identifiers. In addition to demo physics/audio/UI/animation/XR modules, Meta's package scripts require AI, AssetBundle, ParticleSystem, image conversion and Physics2D modules even though the demo itself does not use every subsystem. They are retained to compile the supported package unmodified.

XRI, URP, social/platform features, production video/audio, client assets and unrelated source packages are not part of this baseline. Meta's packages contain their own samples; those remain in the package cache and are not copied into the repository. First import can be substantial because UPM imports package content.

Review `THIRD_PARTY_NOTICES.md` and the package licenses before extracting third-party files. Do not edit Library/PackageCache to fix an SDK issue; record it and use supported package configuration or a project-owned adapter.

The SDK regenerates its local `OculusRuntimeSettings` (including a per-checkout telemetry ID) and platform-upload tool settings. Those generated Resources assets and metas are ignored, so this repository does not distribute one workstation's SDK telemetry/upload identity. The actual rig, hand mode, OpenXR and build settings are committed separately.
