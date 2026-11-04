# Connection validation

2026-11-04, Unity 2022.3.62f2. All live commands targeted the new starter project; none targeted the reference Editor.

| Check | Observed result |
|---|---|
| Native CLI | Compilation, validation, Edit/Play tests, captures and platform builds executed through the documented executable/wrapper |
| Live bridge identity | Returned exact starter project, version, active demo path and GUID `671dc2035044148478a1899953424a1c`; scene clean, Edit Mode |
| Bridge structure validation | 36 checks passed |
| Bridge self-test | Float property, Vector3 property and serialized inspection passed in a temporary additive scene; original scene restored |
| Owned Play session | Enter succeeded with returned token; status confirmed Play |
| Invalid exit token | Rejected; Play remained active |
| Owned reset/capture/exit | Correct token reset the demo, saved a runtime PNG and exited Play |
| Final state | Clean Edit Mode before the agent-owned batch Editor was closed for builds |

Requests carry exact project identity and deadlines. Code rejects dirty scenes/pre-existing Play for automated entry and rejects self-test during Play. Those additional refusal branches were inspected, not all independently exercised with dirty user scenes. No test intentionally changed a user's unsaved scene. The bridge provides bounded actions, not arbitrary C# evaluation or a network server.

The official Unity CLI's live Pipeline route was not installed or tested; it requires a newer Unity baseline. The tested native Unity command line and local bridge each work independently on this baseline.
