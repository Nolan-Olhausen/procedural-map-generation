# Biome Generation Plan

Status: **Decided** — implementation is preview-tool milestone #1.
Height/elevation generation is designed separately (next up) and consumes this
pass's output.

## Summary

Biomes are **authored** per island as an exact-color control map (pixel brush,
no anti-aliasing) and **blended** by the pipeline. There is no Minecraft-style
climate simulation: the control map replaces the climate model, giving direct
authorial control over where biomes go, while distance-transform blend bands +
clumped noise dithering manufacture all the natural-looking softness.

The art pack has no biome-to-biome transition tiles — every tile is a pure
biome tile. Blending is therefore **statistical, not per-tile**: in a
transition band the two biomes interleave in coherent patches whose ratio
shifts across the band (halftone/dither principle). At gameplay zoom
(~30 tiles visible across), a band of shifting ratios reads as a gradual
transition; there is no line to stand on. The decoration/scatter layer
reinforces this with continuous density crossfades (trees thin out, shrubs
thicken), which carries much of the perceived blend.

## Inputs (per island)

1. **Biome control map** — indexed-color PNG.
   - Painted with a pixel (aliased) brush only; every pixel is an exact
     palette color. No anti-aliasing, no soft brushes.
   - Default scale **1 px = 4 tiles** (a max-size 12,600-tile island is a
     ~3,150 px canvas). Scale is a per-island knob; finer is allowed if an
     island needs tile-level boundary control.
   - One reserved color = water/not-land; all other colors = biomes.
2. **Island manifest** (JSON) —
   - Palette: exact hex color → biome ID.
   - Biome list for this island (each island has its own palette subset, e.g.
     jungle+plains island vs. plains+snow island).
   - Blend rules per biome pair: transition band width in tiles, fade vs.
     hard edge.
   - Forbidden adjacencies (e.g. jungle must never directly touch snow; an
     intermediate biome band is required between them — as in real
     geography).
3. **World seed** — fixed and shipped; the world is identical for every
   player.

## Pipeline Stages

1. **Validate.**
   - Every pixel must exactly match the palette. Unknown color → hard fail
     with coordinates (no tolerance matching — with a pixel brush, an
     off-palette pixel is always an authoring bug).
   - Check forbidden adjacencies in the drawn regions; report violations for
     the author to fix. (Possible later upgrade: auto-insert intermediate
     bands.)
2. **Upscale to tile resolution** — nearest-neighbor (regions are exact;
   softness is generated in later stages, never by image interpolation).
3. **Coastline refinement** — signed distance field on the land/sea mask,
   edge perturbed with domain-warped fBm noise: the authored silhouette gains
   natural coves and beaches. The island sizing rule's span check runs on the
   refined mask.
4. **Distance transform on biome boundaries** — for each tile near a border,
   compute distance to the boundary and derive blend weights within that
   pair's configured band width. This recreates what a soft gradient brush
   would have painted, but with pipeline-controlled, per-pair, globally
   retunable transition widths; the drawn line remains the authoritative
   centerline of every transition.
5. **Clumped dithering** — resolve each tile's final biome by thresholding
   its blend weight against **low-frequency** noise, so the minority biome
   appears as coherent patches and fingers (~3–10 tiles), never single-tile
   speckle. Band edges become organic interlocking frontiers.
6. **Output: per-tile biome grid** — dense array, one biome ID per tile.
7. **Debug render** — 1 px per tile PNG of the biome grid for the preview
   tool (regenerate-and-look iteration loop).

Deterministic throughout: same control map + manifest + seed → identical
output.

## The Biome Grid Is the Foundation

Every downstream pass consumes the per-tile biome grid:

- **Height/cliff pass** (next to be designed) — biome can influence
  elevation treatment.
- **Autotiling** — biome ID indexes the art pack's parallel tile-variant
  arrays (one autotiling ruleset, per-biome tiles).
- **Decoration/scatter pass** — density crossfades across transition bands.
- **POI placement rules** — e.g. "crypts spawn in snow," "camps avoid
  jungle."
- **Runtime systems** — encounter tables, ambient audio, weather by biome.
- **World map/minimap UI** — the biome grid downsampled and recolored is the
  in-game map for free.

## Sharp Edges Stay Sharp

Blending applies only to biome pairs marked as fades. Land/water, cliff
bases, and beach edges want crisp boundaries and get them (handled by
within-biome edge autotiles, which is a separate concern from biome-to-biome
blending). Snow transitions can be configured tighter than e.g.
plains↔jungle.

If true transition tiles are ever added to the art pack for a jarring pair,
they slot directly into the already-computed transition band — no pipeline
redesign.

## Tuning Knobs (config, not repaints)

| Knob                          | Effect                                    |
| ----------------------------- | ----------------------------------------- |
| Map scale (px : tiles)        | Authoring precision vs. canvas size       |
| Band width (per biome pair)   | How gradual each transition is            |
| Dither noise frequency        | Patch/clump size in transitions           |
| Coastline perturbation        | How wiggly/natural the refined coast is   |
| Adjacency table               | Which pairs fade / hard-edge / are banned |

## Milestone

This entire pass is engine-agnostic image-and-array work: **implement it as
the standalone map preview tool first** (stages 1–7, PNG in → PNG out, seconds
per iteration), before any in-engine integration.
