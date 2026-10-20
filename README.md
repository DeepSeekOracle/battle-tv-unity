# Battle TV Unity

The Unity 6 port of the LYGO Arena broadcast-fight concept. The live browser game is
https://chatagent.ca/games/lattice-arena/ and is not in this repo.

**Play the Unity build:** https://deepseekoracle.github.io/battle-tv-unity/

## What is in this build

- Intro film and station ident.
- Game menu with mode selection: **1v1, 2v2, 3v3, 5v5**.
- **1v1** opens `Duel.unity`, the side-view lane. Vale versus Brute is the baked
  opening card; other painted fighters use their own StreamingAssets folder.
- **2v2, 3v3, and 5v5** open `Arena.unity`, the overhead sand arena.
- Team Select drafts every book kit. A painted kit loads its folder; an unpainted
  kit fields as a colored mark and does not request a folder that does not exist.
- The Arena uses the book's HP, damage, speed, armor, range, moves, and cards. It
  does not retune fight numbers to fix presentation.
- 1v1 and Arena share the painted roster and the corner Close/Slip rule.

## Scenes

`Menu`, `GameMenu`, `TeamSelect`, `Duel`, then `Arena`. They are all enabled in
Build Settings.

## Editor and pipeline

- Unity `6000.5.9f1`
- Built-in render pipeline
- WebGL build with decompression fallback
- Publish: `tools/publish-webgl.ps1` → `docs/`
- Build cache stamp includes the source SHA so a republish cannot reuse a stale
  CDN/IndexedDB query key.

## Tests

`Assets/Menu/Tests/MenuTests.cs` covers the menu, roster, both fight modes, the
book kits, painted lane entries, the corner leap, and the unpainted-kit loader
gate.

Run EditMode tests from the project:

```powershell
& "<Unity editor path>" `
  -batchmode -projectPath . -runTests -testPlatform EditMode `
  -testResults "$env:TEMP\battle-tv-editmode.xml" `
  -logFile "$env:TEMP\battle-tv-editmode.log"
```

Do not pass `-quit` with `-runTests`.

## Known incomplete art

Nine book kits do not have painted fighter folders yet: `bear`, `lion`,
`elephant`, `scorpion`, `heartboss`, `levi`, `lock`, `tithe`, and `unnamer`.
They are playable as marks and do not produce missing-file errors. Wolf is now
painted as Ashmane. Painting the rest is the next roster pass; do not shrink or retune the existing fight art to compensate.

## Licence and provenance

Copyright 2026 Justin Helmer / Excavationpro. The Arena web game and this Unity
port share the LYGO Arena design language; the web game is not bundled here.
