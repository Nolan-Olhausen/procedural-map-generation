# World & Map Specification

Status: **Draft** — captures design decisions to date; numbers marked
*(provisional)* still need validation via the map preview tool and playtesting.

## Vision

A top-down 2D pixel RPG: Pokémon Gen-4-style presentation (16×16 tiles,
grid-ish movement) with Skyrim-style structure (open world, classes, skills,
quests, NPCs, story by our DM collaborator). The world is an **archipelago**:
several large islands connected by open-ocean sailing (ship combat, sailing
events, boarding, anchoring at islands).

## Core Decisions

### 1. One fixed, authored world — baked offline, streamed at runtime

Unlike Minecraft (different world per player), every player gets the same
story world. Therefore:

- The procedural generation pipeline runs **offline as a tool**, not in the
  shipped game. It can be slow, run expensive global passes (rivers, roads,
  quest-connectivity guarantees), and its output can be inspected and
  hand-edited before players ever see it.
- The game ships the **baked result** as chunked binary map files. Runtime is
  purely chunk streaming around the player — no generation logic in-game.
- Persistent world changes (looted chests, cleared camps) live in a small
  save-file **delta layer** on top of the static map.
- Storage is a non-issue: tile maps compress extremely well (RLE/zstd), and
  ocean chunks are generated from a trivial function rather than stored.
  Expect tens-to-hundreds of MB total, not GB.

### 2. Scale — The Island Sizing Rule

**Tile size:** 16×16 px.

There is exactly **one sizing rule**, applied to every major island:

> The island's two farthest straight points — measured along a straight
> vertical or straight horizontal line (axis-aligned, no diagonals) — must
> take **no less than 45 minutes and no more than 60 minutes** to walk with a
> clear, uninterrupted path (no terrain or obstacles factored in), at player
> walk speed (3.5 tiles/sec).

In tile terms, the island's longest axis-aligned span must be:

| Bound | Time (walk) | Tiles  |
| ----- | ----------- | ------ |
| Min   | 45 min      | 9,450  |
| Max   | 60 min      | 12,600 |

**Friction target (applies regardless of where the span lands in that
range):** with terrain and obstacles in place — mountains to navigate through
or around, lakes to skirt, rivers with sparse crossings — the *actual*
traversal between those two points should take **75–100% longer** than the
unobstructed time (~79–120 minutes experienced).

Both halves of the rule are automated pipeline checks, reported by the map
preview tool on every regeneration:

- **Span check:** longest axis-aligned land span within bounds (9,450–12,600).
- **Friction check:** A* walk-speed path between the two extreme points vs.
  the straight-line distance; the detour factor must land in **1.75×–2.0×**.
  Below 1.75 → generator/control map needs more obstruction between the
  extremes; above 2.0 → traversal is too punishing.

Notes:

- The 45/60-minute bounds are tunable; the 75–100% friction target holds no
  matter what the bounds are set to.
- **Minor islands** (islets, lighthouses, smuggler's coves, other sailing
  filler) are exempt from the minimum span — the rule governs major islands.
  *(Assumption to confirm.)*
- See `movement-speeds.md` for crossing times at other gaits.
- The **ocean** provides the "vast world" feeling nearly for free — procedural
  water + encounters, no authored content per tile. Fewer, denser islands beat
  one giant continent: every tile not generated is a tile that doesn't need to
  be made interesting.
- Each island is its own map file/space; the ocean is its own layer connecting
  them.

### 3. Authoring model: exact-color control map, generated blending

Full design: **`biome-generation.md`**. In brief:

- Each island is authored as an **indexed-color control map** painted with a
  pixel (aliased) brush — every pixel an exact palette color, hard edges,
  default scale **1 px = 4 tiles** (a max island ≈ 3,150 px canvas; scale is
  a per-island knob).
- Pixel color = biome region; a reserved color = water. An island manifest
  (JSON) defines the palette, per-pair blend band widths, and forbidden
  adjacencies. Unknown colors hard-fail validation.
- **All blending is generated, none is painted:** a distance transform on the
  drawn boundaries produces blend weights within configurable per-pair band
  widths, then clumped low-frequency noise dithering interleaves the two
  biomes' pure tiles as coherent patches whose ratio shifts across the band.
  The art pack has no transition tiles and doesn't need them — the gradient
  is statistical, reinforced by decoration-density crossfades.
- Per-tile overrides remain available where exact control is needed (that's
  what POI stamps are).

### 4. Generation pipeline (offline)

Runs in this order:

1. **Coastline refinement** — upscale the painted island silhouette, then
   perturb the edge with domain-warped fBm noise on a signed distance field:
   the authored shape with natural coves and beaches.
2. **Height generation** (see `height-generation.md`) — authored elevation
   layer + per-biome noise, quantized to terraces with cliff walls and
   steepness-scaled ramp placement.
3. **Rivers & lakes** — **authored, like biomes**: river centerlines and
   lake polygons painted in the control map. The pipeline makes them
   plausible: carve a shallow valley along each river course, flatten lake
   basins to one level, and resolve cliff crossings (waterfall tiles if the
   art pack has them — inventory item — else reroute the carved cliff).
4. **Biome resolution** (see `biome-generation.md`) — final per-tile biome
   grid via distance-transform blend bands + clumped dithering, with
   altitude rules biasing the result (snowcaps).
5. **POI stamping** (see `poi-placement.md`) — major POIs stamped at
   authored anchors (footprint leveled); minor POIs region-hash placed
   under the density rule (nearest minor POI within 5–6 min travel from
   anywhere, coverage-repaired per bake).
6. **Reserved path network** (see `vegetation-scatter.md`) — A* web linking
   plateau ramps, POI entrances, and the coast; reserved tiles stay
   prop-free. Rivers cut walkability, so this pass also places
   **bridges/fords** exactly as ramps are placed: guaranteed where the
   network needs them, sparse elsewhere (river crossings are prime
   detour-friction per the sizing rule). Accessibility holds by
   construction; a final flood-fill verifies.
7. **Roads** — visible dirt/cobble paths pathfound (A*, slope- and
   crossing-averse costs) between major POI entrances, drawn with road
   autotiles, preferring wide gentle ramps and placed bridges.
8. **Scatter pass** (see `vegetation-scatter.md`) — props via clumped noise
   + Poisson-disk spacing, biome-band density crossfades, never on reserved
   tiles. Props are tile prefabs (tree canopies on an overhead layer), not
   entities.
9. **Enemy spawn zones** — baked as *data, not entities*: zones/points with
   enemy table, density, level range, and respawn rules (biome-driven,
   sparser than vegetation, denser near hostile minor POIs, suppressed near
   roads/towns). The runtime spawner manages live enemies; cleared-area
   state lives in the save delta layer.
10. **Autotiling** — pipeline works in *logical* terrain ("grass", "water",
    "cliff-north-edge"); this pass maps logic → art tiles using standard
    blob/Wang autotiling, with the art pack's per-biome variant arrays
    indexed by biome.
11. **Patch layer** — authored per-island override files applied last, every
    bake (manual fixes never live in baked output; see `poi-placement.md`).

**Hand-authored, validated content (not generated):** NPCs are hand-placed
with authored movement routes (they carry dialogue/interaction), living in
POI prefabs or the patch layer; the bake validates every NPC route against
the walkability grid so a terrain change that breaks a route is a bake error,
not an in-game NPC walking into a wall.

**Explicitly not map generation:** weather is a runtime system reading the
baked biome grid (snow falls in snow biomes); designed with gameplay systems
later.

### 5. Runtime

- Stream pre-baked chunks from disk around the player (and around the ship at
  sea). Chunk size TBD (16×16 or 32×32 tiles typical).
- Multiple tile layers (ground, water, decoration/overlay) per chunk.
- Lessons from the old `ChunkHandler.cs` prototype: no `GameObject.Find` for
  chunk lookups, no per-tile `GetPixel`, no full Grid object per chunk —
  runtime is "read binary, blit tiles."

## First Milestone: Map Preview Tool

Before any in-engine work: a tool that runs pipeline steps 1–4 and renders a
whole-island PNG (1 px per tile) in seconds. World generation quality is a
function of iteration speed — tweak parameters/control map, regenerate, look.
Then add later pipeline passes, then the chunk-file exporter, and only then
the in-engine streaming renderer.

Also early (parallel, ~2 hours): the **flat-map speed prototype** to lock the
movement values in `movement-speeds.md`.

## Open Questions

- Engine confirmation (Unity assumed; the offline pipeline/tooling can be
  engine-agnostic regardless).
- On-disk map format — proposed, to confirm: Minecraft-style region files
  (32×32-chunk regions, 32×32-tile chunks, layers as flat u16 tile-ID arrays,
  per-chunk zstd/deflate compression, position implicit from index). ~6 KB
  raw per 3-layer chunk; an 85M-tile island lands ~30–100 MB compressed.
  Not JSON for tile data (10+ GB as text) — JSON is for manifests, palettes,
  and POI tables only. Region granularity also makes re-bakes incremental.
- Art-pack verification for height: available wall/ramp tile configurations,
  which set the final level count and cleanup constraints (see
  `height-generation.md`).
- Ocean/sailing: architecture settled in `ocean-sailing.md` (separate
  sailing world with auto-derived island shells, procedural ocean,
  vehicle-mode ship, dock transitions); open knobs are ship speed, island
  gap sizing, and sea content.
- POI interiors: majors hand-built; minor-interior generation deliberately
  deferred (see `ocean-sailing.md` note) — placement/entrances already
  specced, interiors attach later.
- Exact biome list (art pack supports parallel variants of the same tiles
  across biomes, e.g. grass ↔ snow).
- Art-pack check for water features: waterfall tiles (river cliff
  crossings), bridge/ford tiles, road autotiles.
- Enemy spawn zone details (enemy tables, density/level rules, respawn
  timers) — designed with the combat/enemy systems.
