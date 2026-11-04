---
name: unity-debugging
description: Diagnose Unity-specific compilation, serialization, transform, physics, interaction, UI or rendering failures with evidence and focused regression checks.
---

Read Docs/TROUBLESHOOTING.md. Verify exact project/scene and user-owned state before interacting.

1. Reproduce the symptom with the smallest deterministic input. Capture before/after poses, component state and relevant logs.
2. Separate observed facts from hypotheses. Trace input through state ownership to transform/visual output.
3. Inspect serialized data and runtime state together; a correct Inspector value does not prove the visual or mechanical consumer is using it.
4. Fix the cause at the proper boundary. Avoid global offsets, speculative package changes or disabling symptoms.
5. Add a focused regression for the failure, run adjacent interaction/reset checks and inspect images where appropriate.
6. Report the cause, behavior change, evidence and device limitations. Keep the starter journal empty.
