---
name: grabbing-and-grips
description: Implement or debug one/two-hand grabbing, authored finger and wrist poses, handover, scaling, release and dropped-part recovery using the included Grip Editor.
---

Read Docs/GRIP_EDITOR.md and Docs/INTERACTION_CONTRACTS.md. Inspect the actual saved GripTarget policy, axes, contact, scale and profile destination before changing anything.

1. Confirm interaction ownership, root scale, colliders, Rigidbody and SDK selecting IDs. Keep visual mesh scale beneath an editable wrapper.
2. Read raw tracked wrists for mechanics. Never feed the synthetic posed hand into object motion.
3. Preserve Fingers Only versus Fixed Visual Wrist intent. Fit the object to the authored grip smoothly; do not solve offsets by displacing the visual hand blindly.
4. For two hands, keep fixed object size, stable side ordering and safe handover. Restrict acquisition to handles when the design requires it.
5. Release/cancel SDK selection before animation/reset. Restore interaction and physical state, clear occupancy and recover floor-resting drops.
6. Test both sides, individual finger edits, mirrored poses, uniform resizing, reset while held and tracking loss. Device fit remains a separate acceptance check.
