# Vegetation & Scatter Plan

Status: **Decided.** Covers natural props: grass, flowers, trees, bushes,
rocks, boulders, and similar "extras." Runs after height generation and the
reserved-path pass; before/alongside POI decoration. The simplest pass in the
pipeline — but it participates in the accessibility guarantee.

## Placement Model

Per-biome prop tables in the island manifest: each biome lists its props with
target densities (jungle: dense trees + bushes; plains: sparse trees, grass
tufts, occasional boulder; snow: dead trees, rocks). Placement is
deterministic from the world seed.

- **Clumping** — density is modulated by low-frequency noise so trees form
  groves with clearings, rocks cluster, instead of uniform speckle.
- **Spacing** — Poisson-disk sampling so props never stack or crowd
  awkwardly; blocking props additionally respect canopy-width spacing (see
  Representation below).
- **Crossfades** — in biome transition bands, the existing blend weights
  scale the density tables (jungle trees thin out as plains shrubs thicken).
  This carries much of the perceived biome blending.
- **Terrain-aware rules** (cheap config, big naturalness win): no trees
  within N tiles of cliff edges; rocks *more* likely near cliff bases;
  nothing on ramps; reeds/lilies near water; density cap so forests stay
  wanderable.

## Accessibility: Reserved Paths First, Scatter Second

Connectivity holds **by construction**, not by repair:

1. After the height pass, compute a **reserved path network**: A* routes
   linking every plateau's designated ramp, every POI entrance, and the
   coast/spawn into one connected web. Reserve those tiles as a no-spawn
   corridor 2–3 tiles wide.
2. The scatter pass never places blocking props on reserved tiles.
3. Every plateau and POI therefore keeps **at least one clear path**.
   Non-reserved ramps MAY end up blocked by a boulder or tree — acceptable
   and desirable: the protected route reads as "the obvious pass," blocked
   alternates as overgrown back routes. If cuttable/smashable props are
   added later, blocked alternates retroactively become discoverable
   shortcuts with no generator changes.
4. **Safety-net check:** a post-scatter connectivity flood-fill (blocking
   props treated as obstacles) still runs in the preview tool's validation
   suite — not as the mechanism, but to catch reservation-logic bugs as
   bake-time errors with coordinates instead of stuck-player bugs.

Explicitly closed-off areas remain exempt, as in `height-generation.md`.

## Representation: Tile Prefabs, Not Entities

All props are **tile data**, baked into the normal chunk tile layers — the
Gen-4 Pokémon approach. No object entities, no y-sorting.

- **Trees** are multi-tile prefab stamps: trunk tiles carry collision on the
  prop layer; canopy tiles go on an **overhead layer that always renders
  above the player**. Walking north behind a tree gives correct canopy
  overlap; trunk collision prevents any incorrect overlap from the south.
- **Fixed layer scheme:** ground → decor → props/collision → overhead.
- **Small props** are the degenerate case: grass tufts/flowers = single
  decor tiles (walkable); boulders = single collision tiles.
- **Collision** is a per-tile-ID property in the tile database, not a
  separate baked layer.
- **Grid snap & overlap:** prefabs snap to the tile grid. v1 rule: canopies
  must not overlap (Poisson radius ≥ canopy width). If the art pack has
  interlocking dense-forest patterns, "dense grove" becomes its own
  multi-tree prefab later.

Accepted trade-off: pure tiles means props have no individual object
identity. Future harvestable/cuttable props are handled as tile edits in the
save-file delta layer ("tile at x,y → stump"), not entity state.

## Tuning Knobs (config, not repaints)

| Knob                              | Effect                                 |
| --------------------------------- | -------------------------------------- |
| Per-biome prop tables + densities | What grows where, how thick            |
| Clump noise frequency             | Grove/clearing size                    |
| Blocking-density cap              | Forest wanderability                   |
| Reserved corridor width           | How generous the guaranteed paths are  |
| Terrain-aware rules               | Props reacting to cliffs/water/ramps   |
