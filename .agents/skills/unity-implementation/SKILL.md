---
name: unity-implementation
description: Implement or refactor Unity C# runtime components, Editor tools, serialized data and component wiring while preserving authored scene state.
---

Read AGENTS.md and Docs/INTERACTION_CONTRACTS.md. Follow Docs/CONNECTIONS.md for one supported connection route.

1. Inspect the affected component, actual consumers and serialized configuration; do not infer behavior from its name.
2. Verify live project/scene/GUID, dirty state and Play ownership before mutation. Preserve user authoring and Undo.
3. Use focused Editor APIs, SerializedObject, PrefabUtility and EditorSceneManager. Do not hand-rewrite complex YAML or add arbitrary execution endpoints.
4. Implement the smallest complete behavior. Make cancellation, disable/pause and invalid input explicit.
5. Compile, repair references, run meaningful regressions and inspect the relevant scene states.
6. Report behavior changes, test evidence and limits. Keep this starter journal empty; use Reports.
