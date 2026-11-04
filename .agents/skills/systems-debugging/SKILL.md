---
name: systems-debugging
description: Investigate failures crossing Unity, SDK/runtime, bridge/CLI, licensing, OS, graphics, audio, device or network boundaries without disrupting unrelated work.
---

Read Docs/CONNECTIONS.md and Docs/TROUBLESHOOTING.md.

1. Map the failing path and identify exact project, process, version, device, transport and timestamp. Preserve unrelated applications and sessions.
2. Check each boundary independently: filesystem, compile, Editor lifecycle, bridge request/response, runtime input/render/audio and hardware.
3. Collect minimal logs without exposing credentials or private content. Record timeouts as uncertain outcomes, not automatic proof of failure.
4. Change one cause at a time. Do not kill all Unity processes, delete caches blindly, bypass licensing or expose an arbitrary-code service.
5. Use programmatic PNG capture if live computer capture fails, and open the PNG. Do not repeatedly retry a broken capture channel.
6. Verify the repaired boundary and the original user workflow. Distinguish a workaround from a root-cause fix and list remaining external blockers precisely.
