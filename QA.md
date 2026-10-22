# battle-tv-unity — QA

## Target

- Machine / browser:
- Resolution:
- Low preset:
- Build:

## Test matrix

| Area | Test | Expected | Result | Evidence |
|---|---|---|---|---|
| Boot | | | | |
| Input | | | | |
| Core loop | | | | |
| Save | | | | |
| Performance | | | | |
| Audio | | | | |
| Accessibility | | | | |

## Blockers

## Known issues

## Arena runtime pass (2026-10-22)

- Target: Unity 6000.5.9f1, 1280x720
- PlayMode: **2/2**, failures=0, errors=0, 3.756s
- `ArenaMatchRunsToCompletionAndShowsBoard`: pass, 2.241s
- `ArenaRuntimeShowsOnboardingHint`: pass, 1.514s
- Visual: `Logs/arena-hint-1280x720.png`, 1,513,386 bytes, sha256 `a5e0a843...91628`
- HUD hint: top-center, readable; earlier bottom placement rejected
- Visual capture is graphical-only; batchmode cannot evoke end-of-frame
- Not tested: manual input, full manual play, performance, audio, mobile layout
- Blocker: `SLICE-02` remains dependency-blocked by `SLICE-01`

## WebGL manual playthrough (2026-10-22)

- Local WebGL build served at 127.0.0.1:8797; viewport 1280x720.
- Path: start screen → 3v3 → team select (Vale, Lightfather, Kael) → Arena.
- Arena onboarding hint visible during live combat.
- Match ended at 0:28 with scoreboard: "Your side takes the sand."
- REPLAY and NEW TOONS present.
- Browser console errors: 0.
- Evidence: `Logs/webgl-arena-live-1280x720.png`, `Logs/webgl-arena-result-1280x720.png`, `Logs/webgl-teamselect-1280x720.png`.
- Build size: 255,989,988 bytes vs 20,971,520 target (`withinTarget:false`). This is pre-existing; not caused by the hint.
- SLICE-02 still active: SLICE-01 dependency pending.

## Release size guard (2026-10-22)

- `WebGlPublish` now checks `bytes > TargetBytes`.
- Default behavior: log `RELEASE BLOCKED` and exit **2**.
- Local test override only: `LYGO_ALLOW_OVER_TARGET=1`.
- Current manifest: 255,834,134 bytes vs 20,971,520 target.
- 24 fighter directories, all referenced; `StreamingAssets/fighters` = 201.7 MB.
- No source art deleted or degraded.

## Full WebGL profile (2026-10-22)

- Build profile: `full`
- Output: **255,992,683 bytes**
- Target: **314,572,800 bytes** (300 MB)
- `withinTarget`: **true**
- 20 MB remains the menu-only constant, not the full-game target.
- Release guard exits **2** on future overruns.
- Manual smoke: start → 3v3 → team → Arena, zero console errors.
- No commit or publish.

## Task-chain closure (2026-10-22)

Closed in dependency order with evidence:

`INTAKE-01..03` → `CONCEPT-01..03` → `GDD-01..03` → `ART-01..03` → `TECH-01..03` → `SLICE-01..03`.

Key evidence:

- `GAME.md`, `GDD.md`, `ART_BIBLE.md`, `ASSET_PLAN.md`, `TECH_SPEC.md`, `ASSET_PROVENANCE.md`
- EditMode 22/22, PlayMode 2/2
- Full 3v3 match + scoreboard
- Manual WebGL start → 3v3 → team → Arena
- 1280x720 visual capture

**Important audit caveat:** `ART-03` is complete as an audit, but it records **partial provenance**:
no third-party URLs/attribution were found, yet no per-asset source/licence metadata exists.
Internal/local use only; redistribution is **not** cleared.

Production, QA and release tasks remain pending. No commit or publish.

## Release check (2026-10-22)

- Build, tests, all four content modes, credits and 1280x720 visuals pass.
- Performance is 60.09 FPS only in headless SwiftShader; native GPU/mobile unmeasured.
- Asset provenance is partial; redistribution is **not cleared**.
- Release Check remains open; no commit or publish.
- See `RELEASE_CHECK.md`.

## Headed GPU result

- 60.2 FPS, 57.3 MB heap, zero console errors in headed Chrome with d3d11 ANGLE.
- This closes the desktop headed-performance question; native Unity profiling and physical mobile remain open.

## Character completion: Ashmane the Wolf (2026-10-22)

- Roster id: `wolf` / display name: Ashmane / class: Wolf / beast.
- Canonical and pose art generated through Forge -> Asset Foundry (`foundry-local`).
- Forge jobs: canonical JOB-C85B9D83; walk/bite/roar/death/win jobs recorded in Forge.
- Canvas: 512x512; baseline at y=500; pivot 0.5.
- Clips: Pack Bite, Bite, bite, Roar, roar, walk (3 frames), death, win.
- Walk/win/death use deterministic transform frames derived from the validated canonical so identity does not drift; bite and roar use generated poses.
- App verification: Forge PlayMode `JOB-CD131268`, tests=3 failures=0 errors=0; `WolfAssetsLoadAndAnimate` passes.
- Browser verification: `Logs/webgl-wolf-team-1280x720.png`, `Logs/webgl-wolf-arena-1280x720.png`; zero console errors.
- Not complete: the other eight missing definitions (heartboss, levi, tithe, unnamer, lock, lion, bear, elephant, scorpion) still have no art.

## Completed character: Ashmane / Wolf

- Forge jobs generated the canonical and pose art through `foundry-local`.
- Clips: Pack Bite/Bite (2 frames each), Roar (1), walk (3), death (1), win (1).
- Canonical + bite/roar are generated; walk/win/death use deterministic transform frames from the canonical to prevent identity drift.
- PlayMode test `WolfAssetsLoadAndAnimate`: **3/3 total tests passed** via Forge job `JOB-CD131268`.
- Browser evidence: `Logs/webgl-wolf-team-1280x720.png`, `Logs/webgl-wolf-arena-1280x720.png`.
- Forge exe rebuilt: 18,182,169 bytes; foundry-local linked/ready.
- Remaining missing characters: heartboss, levi, tithe, unnamer, lock, lion, bear, elephant, scorpion (8).

## Baseline animation pass (2026-10-22)

- All nine new characters now have: walk 3 frames, attack 2 frames, roar, death, win.
- These are deterministic transform frames from the validated canonical cutouts, not hand-authored pose sets.
- Strict Forge Unity job `JOB-8A94E4C4`: **4/4 PlayMode tests, 0 failures**, agent `unity-test`, fallback `None`.
- Forge routing guard added: Unity QA jobs cannot silently fall through to an LLM.
- Wolf remains the only hand-composed/generated 8-clip character; the other nine are baseline animation.

## Full animation/1v1 closure (2026-10-22)

- All 33 entities audited: **0 missing idle/frame/clip references** (416 PNGs).
- Every move ID and display callout has a clip alias across all 33 characters: **0 missing**.
- `EveryCharacterWorksIn1v1` runs all 33 1v1 matches to completion: **pass**, 6.494s.
- PlayMode: **5/5**, 0 failures.
- EditMode: **22/22**, 0 failures.
- Strict Forge job `JOB-A83581FC`: agent `unity-test`, fallback `None`, `UNITY_TEST_OK tests=5 failures=0`.
- Wolf has full hand-composed/generated clips. Heart of the Crypt has hand-authored strike/roar. Remaining characters use baseline transform clips with full move-name alias coverage.
