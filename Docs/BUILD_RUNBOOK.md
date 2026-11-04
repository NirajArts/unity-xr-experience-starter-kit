# Build runbook

1. Verify project version, branch, dirty/Play state and scene GUID. Keep another Editor's project untouched. Install the required platform module through Hub.
2. Let UPM resolve the committed lock; run Edit and Play tests. Review `Reports/` for current results, not an old workstation's acceptance.
3. Review OpenXR project validation and target identity. Android baseline is ARM64/IL2CPP/Vulkan. Choose a target API appropriate to your actual distribution requirements; `Automatic` uses the installed SDK and is not a store-compliance guarantee.
4. Run `Tools/Invoke-Unity.ps1 -Action BuildWindows` or `BuildAndroid`, passing `-UnityPath` if discovery does not find the exact Editor. Outputs go to ignored `Builds/`; reports go to `Reports/`.
5. For a Quest, install via your normal authorized developer workflow. Test hands/controllers, both eyes, UI, sounds, sleep/resume and reset. Profile CPU/GPU/frame time and thermal behavior on the intended headset.
6. For a release, replace neutral identity/art, review permissions/licenses, choose signing credentials outside the repository and repeat release-build acceptance. Never commit a keystore or private signing data.

Native example: `Unity -batchmode -buildTarget Android -projectPath <absolute-project> -executeMethod XRStarter.Editor.StarterValidation.BuildAndroid -quit -logFile <log>`.

Select the platform at Editor startup (`-buildTarget Android` or `-buildTarget Win64`). Switching only inside `BuildPipeline.BuildPlayer` can miss SDK Editor processors compiled conditionally for that platform. Inspect the resulting Android manifest as well as the build result: this demo needs controller/hand support, not eye/face tracking permissions.

Do not use `-nographics` for screenshot validation. Avoid simultaneous batch and interactive Editors for the same project. If Unity cannot reach its license service in a sandbox, retry a specifically authorized isolated launch with normal workstation access; do not bypass licensing or stop unrelated sessions.
