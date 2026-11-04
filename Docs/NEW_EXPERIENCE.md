# Create a new experience

1. Establish devices, interaction modality, room scale, guidance modes and observable completion criteria. Record unresolved engineering decisions rather than inventing values.
2. Duplicate `ExperienceDemo` using Unity so it gets a new GUID. Preserve the rig and input setup initially. Remove the generic objects through Editor APIs/normal authoring, not a search-and-delete by name.
3. Import only owned or licensed assets. Keep mesh visuals beneath neutral interaction wrappers. Use metres, positive scale, stable axes, simple colliders and separate attachment markers. Preserve a final assembled pose before staging.
4. Build a small working slice: pick up, place, inspect, reset. Test it in a rotated workspace and interrupt it while held. Expand the sequence only after ownership and frame behavior are correct.
5. Use Grip Editor on the `HandGrabInteractable` root. Validate both hands, individual joints, mirroring and scale behavior. Generic demo grips are Fingers Only; authored wrist contacts must be set for the new object, not copied from an unrelated shape.
6. Add domain controllers and explicit action state. Keep media, visual indicators, audio and progression separate. Prefer data assets and narrow adapters over one giant scene-specific behavior.
7. Add original/licensed sounds, narration and subtitles. Use audio time for captions. Test guidance-off behavior and cancellation during reset/Next. No cloud voice service is required.
8. Test with the provided matrix and add regressions for real failures. Capture and inspect screenshots, build the target, then perform physical headset acceptance.
9. Replace neutral bundle identity, icons and content; review licenses and permissions. Create your own journal only when starting the downstream project's continuity process.

For exploded/assembly views, retain independent immutable mating references and a reversible intermediate pose set. For authored animation, use task-specific pivots and local frames; never animate a shared parent that drags unrelated mechanisms with it.
