# Interaction contracts

These rules carry forward production QA improvements without copying its procedure or engineering values.

1. **One transform owner.** SDK free grabbing, constrained mechanics and authored animation must not fight. Cancel selection before scripted movement and reset. Restore a usable interaction state after completion or interruption.
2. **Raw input and visual hands stay separate.** Read tracked wrist data for mechanics. Never derive mechanical motion from a SyntheticHand already fitted to the object. Preserve finger data and authored axis offsets. Fit the object to the hand on pickup; do not hide a poor fit by displacing the rendered wrist.
3. **Rigid two-hand objects stay rigid.** Do not stretch objects/hands when hand spacing changes. Define handle-only acquisition colliders when needed, stable pair ordering, handover behavior and tracking-loss policy.
4. **Placement is compatible and occupied.** Use explicit slot IDs, tolerant proximity/alignment and clear destination cues. Duplicate parts may share compatible slots. Reject occupied/incompatible destinations. Test approach from both sides when supported.
5. **Coordinate frames are explicit.** Store durable final mating poses separately from table and intermediate poses. Tool seating follows the fastener's current transform. Moving/rotating the workspace must move every mechanical reference consistently.
6. **Fastening direction is task-specific.** Define the working-side axis and sign. A ratcheting wrench allows a reset stroke without lost progress. A rigid spanner must detach/reseat to reset. Do not reuse one transformer indiscriminately. These specialized tools are patterns, not implemented demo features.
7. **Completion follows the action.** Observe real placement, coverage, travel or stroke criteria; not elapsed time, media completion or putting a tool away unless the procedure requires that return. Keep manual progression distinct from completion.
8. **Reset is an interruption path.** Cancel animation/media, selection and velocity, clear occupancy and return to current table homes. Recover floor-resting parts as well as falling ones. Do not recover a valid held or seated part.
9. **Feedback is independent.** Teaching media can be disabled while mechanical cues, completion checks and sounds remain. Cancel stale audio/subtitles on reset/disable/pause. Avoid double-triggered sounds on synthetic/replayed events.
10. **Spatial UI is authored deliberately.** Use reachable hit targets and readable modest type, labels in the indicator plane, visible approach cues and consistent fill direction. Billboard only what should face the user. Validate from both eyes on device.

The demo implements the general assembly/grab/reset contracts. Threading, torque strokes, painting, hoists, anchors and procedural narration need explicit task-specific extensions and tests; they are not silently claimed as delivered mechanics.
