# battle-tv-unity — Game Design Document

> Evidence-backed against the shipped Unity project; not a pitch template.

## 1. Player fantasy

Pick a side from the LYGO roster, send it into a painted colosseum, and watch a fast automatic
line fight resolve into a result board. The player is a fight promoter/commander, not a direct
puppeteer: the drama is in the matchup, team composition and the automatic clash.

## 2. Controls and camera

- Menu, mode select and team select are pointer-driven UGUI screens.
- Arena is an automatic simulation; there is no direct movement or attack input.
- The camera is a fixed arena view; HUD and onboarding are screen-space overlays.
- The onboarding line is `CLOSE TO DASH · SLIP TO EVADE` at the top center.

## 3. Core loop

- **30 seconds:** choose 1v1, 2v2, 3v3 or 5v5; pick a team; press FIGHT.
- **5 minutes:** watch the auto-battle, read the HUD and the callout.
- **Session:** return to REPLAY or NEW TOONS for another matchup.

## 4. Verbs and systems

- Choose a mode.
- Draft and remove roster members.
- Start a fight.
- Read HP/MP/ST/cards, callouts and the result board.
- Replay or return to team select.

## 5. Combat / interaction

`ArenaMatch` runs a deterministic, time-stepped simulation. Bodies have team, slot, health, mana,
stamina, attack, range, speed, armor and status effects. The match closes on side elimination or the
hard stop. `ArenaView` renders the bodies, bolts, HUD, hint and result board.

## 6. Progression and economy

No account, currency, store or save economy is implemented in this slice. Roster choice is the
only pre-fight progression expression. `NEW TOONS` returns to selection.

## 7. Content and levels

- **33 roster definitions** in `LaneRoster.All`.\n- **33 actual fighter asset directories** as of 2026-10-22.\n- **wolf** is a complete 8-clip character completed through Forge.\n- The other nine newly made characters have canonical art and static generic clips; full animation is still pending.
- Five modes: 1v1, 2v2, 3v3, 5v5 and the Duel path.
- Scenes: `Menu`, `GameMenu`, `TeamSelect`, `Duel`, `Arena`.

## 8. UI / HUD / onboarding

- Onboarding: one persistent line below the arena callout, with a dark outline for contrast.
- HUD: both teams' names, classes, HP/MP/ST, card slots.
- Result: title, detail, REPLAY and NEW TOONS.
- Menus: mode buttons, team list, fighter card, FIGHT.

## 9. Audio and feedback

Menu music and pre-rendered menu/duel clips exist in `StreamingAssets`. Full audio mixing,
ducking and accessibility have not been profiled. The arena itself is visually driven; no sound
claim is made beyond shipped assets.

## 10. Accessibility

- 1280×720 verified.
- Hint has a dark outline for contrast over the crowd.
- Text is not scalable yet; color-blind and screen-reader passes are not done.
- No keyboard-only menu path is verified.

## 11. Balance numbers

The implementation is the source of truth: `ArenaMatch` / `LaneRoster` values. Example observed
roster values are HP 805–1685 and mana 54–160. A full balance pass is not claimed.

## 12. Test cases

- EditMode: 22/22.
- PlayMode: 2/2, including full match and scoreboard activation.
- Manual WebGL: start → 3v3 → team select → Arena → 0:28 result.
- Visual: 1280×720 hint capture inspected.




