# battle-tv-unity — Art Bible

## Look in one sentence

A painted torchlit colosseum, warm sand against cool crowd shadow, with bold flat HUD bars and
readable oversized type.

## Silhouette and shape language

- Fighters read as compact, high-contrast silhouettes: winged/disc shields for champions, heavy
  shoulders and blunt weapons for brutes.
- Arena is circular: ring, fire bowls, crowd tiers, central fight floor.
- UI is rectangular and flat; no glassmorphism or tiny type.

## Palette

| Role | Hex | Use |
|---|---|---|
| Background | `#141B26` | Crowd/night occlusion |
| Midground | `#6B4A2F` | Arena stone and timber |
| Sand | `#C99A4A` | Fight floor |
| Focal | `#E7B84B` | Buttons, callouts, names |
| Paper | `#F4E7C3` | Body text |
| Warning | `#D9483B` | Damage/defeat accents |

These are visual targets sampled from the shipped UI, not a generated palette spec.

## Lighting and atmosphere

Warm torch pools against a dark crowd; the fight floor is the brightest large shape. No realtime
GI is required for the low preset; baked light and one shadow-casting directional light are enough.

## Camera and composition

Fixed side-on arena camera. The fight floor dominates; HUD occupies the upper corners; the
onboarding line sits top-center below the callout, away from the bright sand ring.

## Character / prop rules

- Fighter frames are full-body painted sprites with transparent backgrounds.
- Keep the same fighter's silhouette and palette across frames.
- No new fighter art was generated for this slice; existing roster art is reused.

## VFX rules

- Warm gold for buffs/impacts, cool cyan for shields/bolts, red for damage.
- Effects must not obscure the HUD or onboarding line.

## UI language

- Flat panels, strong gold edges, heavy readable labels.
- The onboarding hint uses paper text with a dark outline for contrast.
- Do not invent controls; only show inputs that exist.

## Low-end fallback

Baked lighting, one shadow-casting light, no realtime GI, 1280×720 verified. FPS/memory profiling
not yet measured.

## References and licence notes

No third-party source URLs or attribution files were found in the shipped asset tree. Per-asset
provenance is incomplete; see `ASSET_PROVENANCE.md`. Redistribution rights are not established by
this document.
