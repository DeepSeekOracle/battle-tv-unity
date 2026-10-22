# battle-tv-unity — Asset Plan

Every asset must have a source, licence, format, budget and status. No unknown-source assets.

## Shipped fighter assets

| ID | Asset | Type | Use | Source / licence | Format | Budget | Status |
|---|---|---|---|---|---|---|---|
| A001 | fighters/arkos | Fighter frames + entity | LaneRoster roster | Project-local; no source/licence metadata found | PNG + JSON | 7.8 MB (26 files) | shipped; provenance partial |
| A002 | fighters/brute | Fighter frames + entity | LaneRoster roster | Project-local; no source/licence metadata found | PNG + JSON | 8.9 MB (58 files) | shipped; provenance partial |
| A003 | fighters/crown | Fighter frames + entity | LaneRoster roster | Project-local; no source/licence metadata found | PNG + JSON | 4.6 MB (20 files) | shipped; provenance partial |
| A004 | fighters/d9ra | Fighter frames + entity | LaneRoster roster | Project-local; no source/licence metadata found | PNG + JSON | 5.4 MB (20 files) | shipped; provenance partial |
| A005 | fighters/drain | Fighter frames + entity | LaneRoster roster | Project-local; no source/licence metadata found | PNG + JSON | 5.9 MB (20 files) | shipped; provenance partial |
| A006 | fighters/gate | Fighter frames + entity | LaneRoster roster | Project-local; no source/licence metadata found | PNG + JSON | 7.7 MB (20 files) | shipped; provenance partial |
| A007 | fighters/hurler | Fighter frames + entity | LaneRoster roster | Project-local; no source/licence metadata found | PNG + JSON | 6.7 MB (18 files) | shipped; provenance partial |
| A008 | fighters/imp | Fighter frames + entity | LaneRoster roster | Project-local; no source/licence metadata found | PNG + JSON | 6.9 MB (18 files) | shipped; provenance partial |
| A009 | fighters/justicae | Fighter frames + entity | LaneRoster roster | Project-local; no source/licence metadata found | PNG + JSON | 10.9 MB (22 files) | shipped; provenance partial |
| A010 | fighters/kael | Fighter frames + entity | LaneRoster roster | Project-local; no source/licence metadata found | PNG + JSON | 20.5 MB (54 files) | shipped; provenance partial |
| A011 | fighters/kairos | Fighter frames + entity | LaneRoster roster | Project-local; no source/licence metadata found | PNG + JSON | 6.5 MB (20 files) | shipped; provenance partial |
| A012 | fighters/lightfather | Fighter frames + entity | LaneRoster roster | Project-local; no source/licence metadata found | PNG + JSON | 20.3 MB (64 files) | shipped; provenance partial |
| A013 | fighters/lyra | Fighter frames + entity | LaneRoster roster | Project-local; no source/licence metadata found | PNG + JSON | 14 MB (34 files) | shipped; provenance partial |
| A014 | fighters/nia | Fighter frames + entity | LaneRoster roster | Project-local; no source/licence metadata found | PNG + JSON | 7.1 MB (34 files) | shipped; provenance partial |
| A015 | fighters/orin | Fighter frames + entity | LaneRoster roster | Project-local; no source/licence metadata found | PNG + JSON | 15.7 MB (46 files) | shipped; provenance partial |
| A016 | fighters/sancora | Fighter frames + entity | LaneRoster roster | Project-local; no source/licence metadata found | PNG + JSON | 8.2 MB (20 files) | shipped; provenance partial |
| A017 | fighters/seidon | Fighter frames + entity | LaneRoster roster | Project-local; no source/licence metadata found | PNG + JSON | 9.2 MB (20 files) | shipped; provenance partial |
| A018 | fighters/shade | Fighter frames + entity | LaneRoster roster | Project-local; no source/licence metadata found | PNG + JSON | 5.7 MB (18 files) | shipped; provenance partial |
| A019 | fighters/smith | Fighter frames + entity | LaneRoster roster | Project-local; no source/licence metadata found | PNG + JSON | 4.3 MB (20 files) | shipped; provenance partial |
| A020 | fighters/srath | Fighter frames + entity | LaneRoster roster | Project-local; no source/licence metadata found | PNG + JSON | 4.9 MB (20 files) | shipped; provenance partial |
| A021 | fighters/thief | Fighter frames + entity | LaneRoster roster | Project-local; no source/licence metadata found | PNG + JSON | 6.2 MB (18 files) | shipped; provenance partial |
| A022 | fighters/vale | Fighter frames + entity | LaneRoster roster | Project-local; no source/licence metadata found | PNG + JSON | 8 MB (90 files) | shipped; provenance partial |
| A023 | fighters/wolf | Fighter frames + entity | LaneRoster roster | Project-local; generated through Forge/Foundry; provenance partial | PNG + JSON | 1.6 MB (19 files) | complete via Forge; identity locked |
| A024 | fighters/wraith | Fighter frames + entity | LaneRoster roster | Project-local; no source/licence metadata found | PNG + JSON | 6.3 MB (18 files) | shipped; provenance partial |

## Menu / arena assets

| ID | Asset | Type | Use | Source / licence | Format | Budget | Status |
|---|---|---|---|---|---|---|---|
| A025 | menu/ | Intro music and clips | Start screen | Project-local; no source/licence metadata found | MP3/MP4/PNG | 9.3 MB | shipped; provenance partial |
| A026 | duel/ | Duel clip assets | Duel path | Project-local; no source/licence metadata found | MP4 | 1.1 MB | shipped; provenance partial |

## Sourcing rules

- CC0 / public domain / explicitly compatible only.
- Record source URL, author, licence and SHA-256.
- Prefer local reuse and generated assets before downloads.
- Never ship a ripped or unlicensed asset.

## Generated assets

| ID | Prompt / model | Seed | Output | Verified |
|---|---|---|---|---|
| — | Not recorded in the repository | — | Existing fighter/menu art | Visual + runtime use only; provenance not established |


## Missing roster assets (2026-10-22)

Roster definitions: **33**
Asset folders: **24**
Missing folders: **9**

- `heartboss`
- `levi`
- `tithe`
- `unnamer`
- `lock`
- `lion`
- `bear`
- `elephant`
- `scorpion`

`wolf` exists but has only `wolf.png`; every clip references that same frame, so it is not animated.

Therefore character production is **incomplete**: 24 folders exist, one is static, nine are absent.


## Nine matching roster assets added (2026-10-22)

| ID | Name | Source | Status |
|---|---|---|---|
| heartboss | Heart of the Crypt | Forge Foundry canonical | made; static clips |
| levi | Leviathan | Forge Foundry canonical + keyed cutout | made; static clips |
| tithe | Tithe-King | Forge Foundry canonical | made; static clips |
| unnamer | Name-Eater | Forge Foundry canonical | made; static clips |
| lock | The Lock | Forge Foundry canonical | made; static clips |
| lion | Solstride | Forge Foundry canonical + keyed cutout | made; static clips |
| bear | Ironhide | Forge Foundry canonical | made; static clips |
| elephant | Tuskward | Forge Foundry canonical | made; static clips |
| scorpion | Glassbarb | Forge Foundry canonical | made; static clips |

App load proof: strict Forge Unity `JOB-D4D54F6B`, 4/4 PlayMode tests, 0 failures.
