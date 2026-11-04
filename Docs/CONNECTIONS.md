# Unity CLI and local bridge

Use either route. A closed project can be compiled, tested, captured and built through the native Unity CLI. A live Editor can be inspected and edited through this project's file bridge. Do not launch a second Editor against the same project.

## Discovery and identity

Read `ProjectVersion.txt`, discover the matching installed executable through Hub or `UNITY_EDITOR_PATH`, and resolve the actual project directory. Check Git state first. For a live Editor, request bridge `status` and verify returned project root, Unity version, active scene path/GUID, dirty state, prefab stage and Play state before any mutation. Never infer identity from a window title.

## Native CLI

```powershell
$env:UNITY_EDITOR_PATH = 'C:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe'
.\Tools\Invoke-Unity.ps1 -Action Validate
.\Tools\Invoke-Unity.ps1 -Action EditTests
.\Tools\Invoke-Unity.ps1 -Action PlayTests
.\Tools\Invoke-Unity.ps1 -Action Capture
.\Tools\Invoke-Unity.ps1 -Action BuildWindows
.\Tools\Invoke-Unity.ps1 -Action BuildAndroid
```

Paths are examples; the wrapper discovers its project relative to itself. Logs go to ignored `Reports/Raw`; result XML/reports go to `Reports`. The wrapper refuses a project with a live Editor instance. Do not remove its lock file or kill another user's Editor to bypass that check.

The separately installed official Unity CLI can also manage Editors and invoke runs/tests/builds. Discover its installed version and command help before using it. Its **live Unity Pipeline connection requires Unity 6+**, so it is not a requirement of this 2022.3 baseline. Do not silently upgrade the project to obtain that connection. See [Unity Pipeline prerequisites](https://docs.unity.com/en-us/unity-cli/unity-pipeline/unity-pipeline-package).

## Project-local bridge

The Editor bridge loads with the project after compilation. It polls `UnityXRTemplate/UserSettings/CodexSceneBridge`, which is ignored by Git. It opens no network port and has no arbitrary-code command.

```powershell
.\Tools\Invoke-UnityBridge.ps1 -Action status
.\Tools\Invoke-UnityBridge.ps1 -Action scene_summary
.\Tools\Invoke-UnityBridge.ps1 -Action find_objects -NameContains 'Rotor'
.\Tools\Invoke-UnityBridge.ps1 -Action inspect_object -GlobalObjectId '<returned ID>'
.\Tools\Invoke-UnityBridge.ps1 -Action starter_validate
.\Tools\Invoke-UnityBridge.ps1 -Action starter_capture
```

Supported authoring actions include create/open/save scene, create object, add component, reparent and `set_property`. Mutations use exact scene paths and fresh object IDs; property changes require the expected current value. Use `-DryRun` first for uncertain edits. No scene is automatically overwritten to recover a lost reference. Save only within the user's authorized scope; dirty scenes are not silently discarded.

Requests include the expected project root and an expiration deadline. A timed-out mutation may have executed: inspect before retrying. Never queue stale mutations for a project that is closed. Exchanges are local IPC, not a security boundary against another process running under your account.

Bounded Play actions: `starter_enter_play` refuses pre-existing Play and dirty scenes, returning a `SessionToken`. Poll `status` until entered. Pass that exact token to `starter_reset_demo` or `starter_exit_play`. These commands refuse user-owned Play sessions. There is no force-takeover command. Validation/capture can inspect a running demo without claiming ownership.

Screenshots are saved under `Reports/Screenshots`. **Open and inspect each relevant PNG.** Report what it shows and what it cannot establish. If live desktop capture fails, use these camera captures; do not keep retrying a broken capture session.
