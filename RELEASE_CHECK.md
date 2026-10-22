# Battle TV — Release Check

**Date:** 2026-10-22
**Build:** full WebGL profile

## Gates

| Gate | Result | Evidence |
|---|---|---|
| Build profile | PASS | `docs/build-manifest.json`: 255,992,683 / 314,572,800, withinTarget true |
| Unity EditMode | PASS | 22/22 |
| Unity PlayMode | PASS | 2/2 |
| Content modes | PASS | 1v1 2:24, 2v2 0:35, 3v3 0:28, 5v5 0:35; 0 console errors |
| Onboarding visual | PASS | 1280x720 hint capture |
| Performance | PARTIAL | 60.09 FPS SwiftShader + 60.2 FPS headed d3d11; physical mobile/native Unity still unmeasured |
| Credits | PASS | `CREDITS.md`; footer matches |
| Asset provenance | **FAIL / BLOCKED** | audit complete, but no per-asset source/licence metadata; redistribution not cleared |\n| Character completeness | **PARTIAL** | all 33 definitions have art; wolf has 8 clips; all nine others have baseline transform clips; hand-authored animation and tuning remain |
| Native/mobile/audio | NOT VERIFIED | no device or audio-mix pass |
| Commit/publish | NOT PERFORMED | no release action |

## Decision

**Do not release.** The build is locally playable and content-complete, but provenance is partial and
native/mobile performance is unmeasured. `RELEASE-03` stays open until those are resolved or explicitly
accepted by the owner.






