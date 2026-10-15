# Battle TV Unity

Standalone menu for the Battle TV port. The live browser game at https://chatagent.ca/games/lattice-arena/ is not in this repo.

The design paper is `C:\Users\justi\Desktop\LYGO Arena look\Battle-TV-Unity-whitepaper.md`.

This slice is the menu only. The still is `Assets/Art/Menu/Vale-vs-Brute.jpg`. The start clip plays once from `Assets/StreamingAssets/menu/Vale-vs-Brute-push.mp4`, then fades. One button, labelled `BATTLE TV`, calls `NoShow.RequestShow()`. That call does not load a fight.

Editor `6000.5.9f1`. Render pipeline is Built-in.

The test page is https://deepseekoracle.github.io/battle-tv-unity/. It is this menu only. The live arena is not in this repo and has no link here yet.
