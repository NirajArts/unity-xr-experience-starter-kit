# Extraction record

Date: 2026-11-04. The reference was inspected read-only. This repository was created separately; no production scene was duplicated or cleaned to create it.

## Inspected baseline

Unity 2022.3.62f2, Built-in rendering with Linear colour, Meta XR SDK 77.0.0, OpenXR 1.14.3, XR Management 4.5.1, Input System 1.14.0, TMP 3.0.7 and UGUI 1.0.0. Actual production scene components use Meta OVR raw data sources, Grabbable/HandGrabInteractable, synthetic hand visuals and custom transformers. XRI was installed in the reference but was not its active production interaction architecture, so it was not added here.

## Reused with owner authorization

- Generic Grip Editor runtime, authoring window, preview/parity tools and isolated Edit Mode tests, including `.meta` files. Scene-specific diagnostics and production Play tests were excluded.
- Generic bounded file bridge and PowerShell client. The starter adds exact project identity/deadline checks and token-owned bounded Play/capture/validation commands; fixes relative-root discovery and timeout cleanup.
- Raw-wrist rigid-fit mathematics, generalized into `AuthoredGripTransformer` with no production part/procedure type dependency.

`REUSED_FILES.json` preserves the initial copied-file hashes. Those are provenance hashes, not a claim that the adapted bridge files remained byte-identical. The user explicitly confirmed ownership and permission to reuse custom scripts, Grip Editor and skills on this date.

## Recreated

Fresh Unity project settings, generic primitive/parametric assembly, materials, layout, world UI, runtime flow/placement/recovery, six synthesized feedback WAVs, demo pose references, tests, CLI workflow, documentation and fourteen focused agent skills. The XR rig is constructed from official package prefab references with explicit scene wiring. Packages were downloaded by UPM; no source `Library` or package cache was copied.

No production geometry, texture, recording, video, client graphic, brand, engineering document, Blender source, proprietary training sequence, credentials or source Git history was included. No global package removal, source branch change or source Editor operation was performed.

## Corrections discovered during validation

1. The initial minimal manifest missed built-in AI/AssetBundle/ParticleSystem/image-conversion/Physics2D modules required by Meta package scripts. Added the required built-in modules without editing SDK code.
2. Desktop fallback originally disabled the XR EventSystem while leaving PointableCanvas surfaces active. Disabled complete XR surface objects before their Start and set the correct event camera for each mode.
3. Interpolated Rigidbody state could restore stale transforms after synthetic grabs, inspection or reset. Explicitly transfer interpolation/pose ownership and synchronize physics poses for mechanical movement.
4. UI progress initially lacked a suitable fill sprite. Added a small original solid sprite and inspected its empty/full states.
5. Exploded parts initially covered controls. Reduced spacing and reviewed the capture states; final observations are in VALIDATION.md.
6. Explicitly refresh/configure OpenXR features after the Meta feature-set initializer, and assert the saved Touch/Meta runtime/Android support configuration rather than only the loader.
7. Disable the raw OVR hand mesh renderers so the Interaction SDK synthetic hand remains the sole visual owner when tracking becomes active.
8. Start platform builds with the appropriate `-buildTarget`, ensuring Meta's conditionally compiled Android build processor participates. Inspecting the actual APK revealed the missing hand-tracking manifest before this correction.
9. OpenXR 1.14.3 adds required eye tracking whenever Quest Pro is targeted, independently of enabled eye features. A project-owned Android manifest merger safeguard removes that requirement only while OVR and OpenXR eye/gaze options remain disabled. SDK files are unchanged; explicit eye-tracking opt-in retains its permission policy.
10. The built desktop player exposed a transient duplicate EventSystem warning during startup. Save both input systems inactive and enable only the selected mode in the early bootstrap; the Play test also asserts exactly one active EventSystem before raycasting the visible control.
11. Repeated Windows builds exposed Meta 77's non-overwriting copy of `RuntimeActionBindings.json`. The bounded build command removes only that generated sidecar inside its ignored Builds directory before rebuilding. It also rejects a nominally successful build if Unity reports any build errors.

## Preservation

A private baseline recorded 3,478 source files under Assets, Packages, ProjectSettings, Docs and Tools. Final checks found one timestamp-only difference in the reference OpenXR settings; its Git content hash still matches the unchanged HEAD. All six critical content hashes match, and the source branch and pre-existing Git changes are unchanged. No agent mutation targeted the reference project; its existing Editor was left running. Baseline files and raw logs remain outside the distributed content audit.

The starter journal remains zero bytes. This report, test results and validation reports hold the extraction evidence.
