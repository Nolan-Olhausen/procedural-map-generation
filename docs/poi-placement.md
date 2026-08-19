# POI Placement Plan

Status: **Minor POI density rule decided**; major POI process settled at the
map-specs level; stamina-dependent travel formula pending stamina system
design.

POIs come in two tiers (see `map-specs.md`):

- **Major POIs** — cities, story locations, key lakes: hand-built tile
  prefabs placed at authored anchor points in the control map. The pipeline
  flattens/clears the footprint and connects roads. Not governed by the
  density rule below (their placement is story-driven).
- **Minor POIs** — free-roam content: camps, crypts, ruins, ponds, shrines,
  ransacked villages, caves, etc. Procedurally placed, governed by the
  density rule.

## The Minor POI Density Rule

> From any walkable point on a major island, the nearest minor POI must be
> reachable within **5–6 minutes of travel time**, measured at the player's
> effective sustained speed (sprint/walk mix under stamina).

Rationale: standard open-world pacing guidance — always something to
interact with, explore, fight, or loot within a few minutes, so free-roam
never goes dead.

### Effective sustained speed (v_eff)

Sprint is stamina-gated, so the measurement speed is a sustained mix of
sprint (5.5 tiles/sec) and walk (3.5 tiles/sec). v_eff is defined at
**median stamina** — a mid-progression character, not starting or max
stamina. The exact formula depends on the stamina system (not yet designed).

- **Placeholder until then: v_eff ≈ 4.5 tiles/sec** (walk/sprint midpoint).
- Rule in tile terms at the placeholder: nearest minor POI within
  **~1,350–1,620 tiles of travel (path) distance** from any walkable tile.
- Travel distance is path distance on the finished map (around lakes, up
  ramps), not straight-line. At the terrain's target 1.75–2× detour factor
  this corresponds to a straight-line coverage radius of roughly 700–850
  tiles.
- When the stamina system lands, v_eff is recomputed and the tile thresholds
  update automatically — the rule is stored in minutes, not tiles.

### Content budget implication

Coverage-packing a major island (9,450–12,600 span) at that radius requires
ballpark **40–100 minor POIs per island** — Skyrim-like density. With ~8–10
minor POI types × 5–10 handmade variants each, the generator covers this
without obvious repetition.

## Placement Pipeline

1. **Statistical placement** — deterministic region-hash scatter (Minecraft
   structure-style): hash(seed, region coords) decides spawn/type/variant/
   rotation per region, subject to:
   - Terrain/biome validity per POI type (crypts favor snow/rock, ponds
     need flat lowland, camps avoid dense forest cores, etc.).
   - Minimum spacing between minor POIs.
   - Category mixing — the nearest few POIs to any point should not all be
     the same type.
2. **Coverage repair** — multi-source Dijkstra flood from all placed POIs
   over the walkability graph. Tiles whose travel time to the nearest POI
   exceeds the cap mark coverage gaps; insert an additional POI at each gap
   center (respecting validity + spacing) and re-flood until no gaps
   remain. The density rule holds **by construction**, per-bake.
3. **Path integration** — the reserved path network (see
   `vegetation-scatter.md`) links every POI entrance into the guaranteed
   walkable web, so no POI is ever sealed off by scatter.

## Preview Tool Additions

- **Coverage heatmap PNG** — per-tile travel time to nearest minor POI
  (cool near POIs, hot in dead zones).
- **Metric line** — "worst-case travel time to nearest minor POI: X min",
  reported alongside the span, friction, connectivity, and scatter checks.

## Synergies

- Minor POIs are the distraction-bait that stretches real traversal beyond
  straight-line time — this rule and the sizing rule's friction target
  reinforce each other.
- POI footprints are no-spawn zones for scatter and ramp placement (already
  specced in those passes).

## Open Items

- Stamina system → real v_eff formula (replaces the 4.5 placeholder).
- Minor POI type list and per-type terrain validity rules (with the DM /
  story collaborator).
- Whether some minor POI types are biome-exclusive vs. reskinned per biome
  (art pack variant question, same pattern as tiles).
