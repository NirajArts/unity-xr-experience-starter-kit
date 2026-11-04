# Independent import evidence

2026-11-04. Created a separate private test directory from the repository's authored/configuration files using its Git ignore rules. No `Library`, package cache, `UserSettings`, build output, source Git history or reference-project files were copied.

The matching Unity Editor independently resolved the pinned UPM manifest/lock, imported assets, compiled and passed structural validation. The copy was then synchronized with final runtime/scene changes and ran **34 passing Edit Mode tests and 11 passing simulated Play Mode tests**. The checked-in XML results and four numbered runtime images come from that independent copy.

The final Android-only manifest callback ordering was checked in the actual Android build, rather than the Standalone Edit/Play tests. No runtime or scene changes followed those tests.

This is a fresh-project-directory test on the same workstation, using a licensed Editor, installed platform modules, network and the workstation's shared UPM download cache where available. It is not evidence of a second PC, offline first import or a fresh operating-system account. The project does not depend on the reference workspace or its Library cache.

The repository includes complete Assets/metas, Packages/lock and ProjectSettings. Generated SDK telemetry/upload identity, credentials, machine settings and validation caches are ignored. Dependency/license prerequisites are documented in `Docs/DEPENDENCIES.md` and `THIRD_PARTY_NOTICES.md`.
