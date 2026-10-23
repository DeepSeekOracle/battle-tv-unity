# battle-tv-unity — Performance Probe

**Date:** 2026-10-22
**Build:** full WebGL profile, 255,992,683 bytes
**Viewport:** 1280x720
**Browser:** headless Chrome + SwiftShader

## Result

| Metric | Value |
|---|---:|
| Sampling window | 5,009.3 ms |
| Frames | 301 |
| Frame cadence | **60.09 FPS** |
| JS heap at end | **57.2 MB** |
| JS heap at start | 57.7 MB |
| Browser console errors | 0 |

## Limit

This is a **headless SwiftShader** probe of browser requestAnimationFrame cadence, not a native-GPU
Unity profiler measurement. It proves the page can sustain ~60 Hz in this environment; it does not
prove GPU headroom, thermal behavior, or memory on a real device.

## Evidence

- `Logs/webgl-performance-1280x720.png`
- Run log from the Playwright probe (301 frames / 5,009.3 ms).

## Headed GPU comparison (2026-10-22)

- Headed Chrome, `--use-angle=d3d11`, 1280x720.
- 301 frames / 5,000.4 ms = **60.2 FPS**.
- JS heap **57.3 MB**; console errors **0**.
- SwiftShader probe: 60.09 FPS / 57.2 MB.
- Evidence: `Logs/webgl-performance-gpu-1280x720.png`.
- Still not a native Unity profiler or physical mobile-device test.
