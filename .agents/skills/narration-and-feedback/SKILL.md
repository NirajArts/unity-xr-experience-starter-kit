---
name: narration-and-feedback
description: Add owned narration, audio-timed subtitles, videos and interaction sounds while preserving mechanical behavior and guidance-mode policies.
---

Read Docs/INTERACTION_CONTRACTS.md and Docs/REDISTRIBUTION.md.

1. Inventory approved media, activity mapping, duration and transcript. Flag conflicts with mechanics; do not silently change the procedure to fit a recording.
2. Separate instructional media, affirmation speech, completion cues and mechanical audio. Specify guidance-off behavior per category.
3. Time subtitles against AudioSource time. Keep video/audio ownership and cancellation explicit on reset/Next/disable/pause.
4. Interaction sounds observe accepted events and endpoints; they must not increment progress or alter poses. Avoid duplicate playback from repeated event dispatch.
5. Use suitable spatial blend/range and an active listener. Check import settings, clipping, output route and voice overlap.
6. Test both guidance modes and mid-clip interruption. Inspect subtitle/video framing and listen when possible. Do not claim human audio review from a successful clip reference alone.

No online STT/TTS service is required. Use local tools only when needed and authorized.
