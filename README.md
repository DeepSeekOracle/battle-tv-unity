# Battle TV Unity

Standalone menu for the Battle TV port. The live browser game at https://chatagent.ca/games/lattice-arena/ is not in this repo.

The design paper is `C:\Users\justi\Desktop\LYGO Arena look\Battle-TV-Unity-whitepaper.md`.

This slice is the intro and the game menu. The still is `Assets/Art/Menu/Vale-vs-Brute.jpg`. `Vale-vs-Brute-rest.mp4` loops in place over that still. The start clip plays once from `Vale-vs-Brute-push.mp4`, then fades to the loop. The intro carries the studio line. The intro button opens the game menu. That page links back to the live arena, PayPal, Patreon, and the Excavationpro radio, with a small Options button. The menu's `BATTLE TV` button calls `NoShow.RequestShow()` and does not load a fight.

Editor `6000.5.9f1`. Render pipeline is Built-in.

The test page is https://deepseekoracle.github.io/battle-tv-unity/. It is this menu only. The live arena is not in this repo and has no link here yet.
