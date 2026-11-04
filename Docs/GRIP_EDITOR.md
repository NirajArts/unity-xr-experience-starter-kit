# Grip Editor

Select the GameObject owning Meta `HandGrabInteractable`, then open **Tools > XR > Grip Editor**. Bind a target if necessary. The selected interaction root must have positive uniform inherited scale; keep nonuniform model scale on a visual child.

The reused tool supports left/right definitions, curls/spread/thumb adjustments, individual joint rotations, mirror, preview, axis-specific wrist orbits, finger freedom overrides, scene-owned data and shared profiles. It preserves data through Undo/Redo and uses installed Meta hand pose references rather than copied hand models.

**Policies:** Existing delegates to SDK behavior. Fingers Only applies authored fingers while leaving the wrist acquisition unchanged. Fixed Visual Wrist requests an authored wrist fit; the demo's `AuthoredGripTransformer` moves the object toward the raw tracked wrist and the visual adapter keeps mechanics independent from synthetic output. Fixed-size pair fitting cannot make physically incompatible hand spacing exact at both wrists.

The demo supplies valid bilateral Fingers Only definitions. They are a safe generic starting point, not a claim that each primitive has a headset-approved fixed contact. Author and inspect actual contact transforms before choosing Fixed Visual Wrist. Keep the source project's fitted grips out of generic assets.

Save through the displayed destination. Shared-profile edits affect every consumer; scene-instance edits do not automatically apply to base prefabs. Stop preview and check cleanup before saving/building. Use the supplied tests for finger composition, authoring persistence and pose math; verify tracking/physical fit on a device.

`scaleGripPositions` and captured `gripReferenceScale` are opt-in for roots intended to resize. They adjust wrist placement without scaling the rendered hand or rewriting finger data. Do not enable this globally for metre-frame offsets.
