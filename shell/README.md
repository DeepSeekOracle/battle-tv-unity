# Battle TV Unity — page shell

`index.html` here is the source of truth for `docs/index.html`, which is what
GitHub Pages serves. A Unity WebGL build regenerates `docs/index.html` from the
engine template, so after every rebuild:

    cp shell/index.html docs/index.html

and re-apply the `buildStamp` line if the build hash changed (the sync script
updates it, or edit the four `?v=` values by hand).

## What the shell adds on top of Unity's template

- One failure surface: `btBanner()` renders into `#unity-warning` with a title, a
  plain sentence, a Reload button and a link to the HTML5 version. Unity's own
  error type is routed through it, and `alert()` is gone — some in-app browsers
  block it and it leaves a dead-looking page behind the dialog.
- A WebGL probe before the loader is requested. No WebGL means an honest message
  instead of a long download that ends in a black canvas.
- `script.onerror`, so a missing engine file says so instead of hanging on the
  loading bar forever.
- A loading line under the bar: `LOADING THE BROADCAST · 47%`. The build is 29 MB;
  a percentage makes the wait legible.
- `role="application"` and an `aria-label` on the canvas.
- A `<noscript>` panel with a link to the HTML5 arena.
- `#unity-warning` is raised above the site chrome (z-index 60) and restyled in
  the site's amber-on-black, because the template's white box sat under the
  top bar.

## Geometry and chrome (2026-10-24 sweep)

Two defects found by loading the deployed build in a real browser:

1. **The canvas was offset and clipped.** Unity's template centres a 960×600
   container with `transform: translate(-50%,-50%)` while the loader sizes the
   canvas in `vw/vh`. Measured at 1264×569 the canvas sat at **(-7.5, -9.5)** and
   both scrollbars appeared (`scrollHeight 564` vs `clientHeight 554`). The shell
   now pins the container to `inset: 0` with `transform: none` and sets
   `html, body { overflow: hidden }`; measured after the fix: canvas `1264×569 @
   0,0`, scroll == client == viewport.

2. **The chrome waited for a callback the build never makes.** Instrumenting
   `window.battleTvSetScreen` for 60 s recorded **zero calls**, so `data-screen`
   stayed `intro` and everything gated on `menu` — radio, fullscreen, options,
   the links — was permanently hidden and unreachable. The controls are now
   available on every screen, and the intro credit moved to the top edge so it no
   longer sits on the game's own "BATTLE TV" title plate.

Still open on the game side: the intro scene does not advance on click, key or
after the menu videos finish, and it never calls `battleTvSetScreen("menu")`.
That is a scene/C# issue, not a shell one.
