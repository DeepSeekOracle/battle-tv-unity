# battle-tv-unity — QA Matrix

**Date:** 2026-10-22
**Build:** full WebGL profile, 255,992,683 bytes
**Viewport:** 1280x720

| Mode | Entry | Live evidence | Result evidence | Console errors | Status |
|---|---|---|---|---|---|
| 1v1 / Duel | team 1/1, FIGHT | `webgl-mode-1v1-live-1280x720.png` | `webgl-mode-1v1-result-1280x720.png` at 2:24: "Vale takes the lane." | 0 | **pass** |
| 2v2 / Arena | team 2/2, FIGHT | captured | `webgl-mode-2v2-result-1280x720.png` at 0:35: "The blind side takes the sand." | 0 | **pass** |
| 3v3 / Arena | team 3/3, FIGHT | `webgl-arena-live-1280x720.png` | `webgl-arena-result-1280x720.png` at 0:28: "Your side takes the sand." | 0 | **pass** |
| 5v5 / Arena | team 5/5, FIGHT | captured | `webgl-mode-5v5-result-1280x720.png` at 0:35: "Your side takes the sand." | 0 | **pass** |

## Notes

- 1v1 is a separate `DuelView` path with `SideDuel.HardStop = 600`; a result within 70s is not expected.
- 2v2 and 5v5 use the Arena path and reached the result board.
- No browser console errors were observed in any mode.
- Input is automatic in Arena/Duel; menu/team navigation is pointer-driven.
- Not tested: performance/FPS, audio mix, mobile layout, keyboard-only navigation.
- `SLICE-02` is done; production/QA/release tasks remain pending.


## WebGL mode result (final)

- 1v1/Duel: **pass**, "Vale takes the lane." at 2:24, REPLAY/NEW TOONS.
- 2v2/Arena: **pass**, "The blind side takes the sand." at 0:35.
- 3v3/Arena: **pass**, "Your side takes the sand." at 0:28.
- 5v5/Arena: **pass**, "Your side takes the sand." at 0:35.
- Console errors: 0 in all four modes.
- Evidence: `Logs/webgl-mode-*` and prior `webgl-arena-*`.

## Performance probe (2026-10-22)

- Arena 3v3, headless Chrome/SwiftShader, 1280x720.
- 301 frames / 5,009.3 ms = **60.09 FPS**.
- JS heap 57.2 MB; console errors 0.
- Native GPU/thermal/mobile performance remains unmeasured.
- Evidence: `Logs/webgl-performance-1280x720.png`.
