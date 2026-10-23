# battle-tv-unity — Technical Spec

**Profile:** Unity HD-2D
**Engine:** Unity 6 URP, `6000.5.9f1`
**Status:** playable local WebGL slice; release-size profile `full`

## Architecture

- Unity scene flow: `Menu` → `GameMenu` → `TeamSelect` → `Arena` (or `Duel`).
- `ArenaView` builds the runtime HUD/chrome, renders bodies and bolts, and shows the result board.
- `ArenaMatch` is a static, time-stepped simulation with no direct player movement.
- `LaneRoster` is the roster source of truth (33 IDs).
- `FighterBodies` loads fighter art from `StreamingAssets/fighters`.
- `WebGlPublish` builds scenes and writes the static player to `docs/`.

## Scene / module structure

| Scene/module | Purpose |
|---|---|
| `Assets/Menu/Menu.unity` | Intro/start screen |
| `Assets/Menu/GameMenu.unity` | Mode select |
| `Assets/Menu/TeamSelect.unity` | Team draft |
| `Assets/Fight/Duel.unity` | 1v1 path |
| `Assets/Fight/Arena.unity` | Full arena |
| `Assets/Fight/ArenaView.cs` | Runtime presentation |
| `Assets/Fight/ArenaMatch.cs` | Simulation |
| `Assets/Menu/LaneRoster.cs` | Roster, stats, moves |
| `Assets/Menu/Editor/WebGlPublish.cs` | Publishing and release guard |

## Data and save format

- No save/account/economy data.
- Roster definitions are code plus `StreamingAssets/fighters/<id>/entity.json`.
- Art frames are PNG files in the same directory.
- No database, telemetry, credits or store dependency.

## Input

- Menu/team/fight screens use UGUI pointer events.
- Arena is automatic; no movement/attack input exists.
- Intro music listens for `Input.anyKeyDown` / mouse down.
- Keyboard-only menu navigation is **not verified**.

## Audio

- `StreamingAssets/menu` and `duel` carry shipped MP3/MP4 assets.
- Full mix, ducking, loudness and mobile audio behavior are **not profiled**.

## Rendering and performance budget

- Target FPS: 60
- Verified resolution: 1280×720
- Full WebGL build: **270,543,922 bytes**
- Full profile cap: **314,572,800 bytes** (`withinTarget:true`)
- Menu-only constant: **20 MB** (not the full-game cap)
- `StreamingAssets/fighters`: **215.6 MB** across 33 fighters
- `Build/`: **31.9 MB**
- Low preset: baked lighting, one shadow-casting directional light, no realtime GI.
- FPS measured at **60.09 headless / 60.20 headed**; native/mobile profiling still pending.

## Test hooks

- EditMode suite: 22 tests.
- PlayMode suite: 2 tests (`ArenaRuntimeShowsOnboardingHint`,
  `ArenaMatchRunsToCompletionAndShowsBoard`).
- Graphical capture: `ArenaVisualCapture` (session-gated; batchmode cannot capture frames).
- Reports: Unity JUnit output and `QA.md`.

## Build / packaging

- `WebGlPublish.Publish()` builds the five scenes into `docs/`.
- Full profile target: 300 MB.
- Over-target builds exit **2**; local override is `LYGO_ALLOW_OVER_TARGET=1`.
- No commit or external publish is performed by the build.

## Risks

1. **Payload size:** 256 MB is heavy for web; content packs or build-only compression are not done.
2. **Provenance:** no third-party URLs/attribution found, but per-asset licence/source metadata is
   absent; redistribution clearance is not established.
3. **Performance/audio:** no FPS, memory or mix profile yet.
4. **Input/accessibility:** Arena is automatic; keyboard-only and assistive paths are unverified.
5. **Worker availability:** OpenCode workers are quota-blocked; Forge remains partially offline.

