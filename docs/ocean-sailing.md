# Ocean & Sailing Plan

Status: **Architecture decided**; ship speed, gap sizing, and sea content are
open knobs. Reference feel: Pirates of the Caribbean Online (now The Legend
of Pirates Online) — walkable ship decks, zoomed-out sailing, broadsides,
anchoring at islands.

## Core Architecture: Two Spaces — Sailing World + Island Maps

The **sailing world** and the **island maps** are separate spaces (the
Pirates Online model). Islands appear in the sailing world only as
simplified **shells**; anchoring/docking is a load transition into the real
island map, and boarding at a dock transitions back. The full islands are
never rendered or streamed at sea.

Both spaces share one coordinate system and tile scale: the world layout map
places each island at fixed world offsets, so the world map UI is honest and
"check the map, sail that heading" genuinely finds the island's shell.

### Island shells are derived, not authored

A post-bake step generates each shell automatically from the island's
finished terrain grids:

1. Extract a **coastal ring** — everything within ~100 tiles of the shore
   (the zoomed sailing camera sees ~60–80 tiles inland when hugging the
   coast; 100 gives margin).
2. Keep base terrain only: shallow water, beach, the first inland
   grass/snow/rock tiles, cliff walls where cliffs meet the sea. Strip all
   decorations, props, overhead layers, and POIs (dock locations excepted —
   they must be visible to sail to).
3. Void the interior entirely — never visible, zero tiles stored.
4. Write as a small region-file set for the sailing world (~1 MB per island
   compressed vs. tens of MB for the full island).

Derived means never stale: re-bake an island and its shell regenerates with
the correct beaches/cliffs automatically. "Which edges have beach vs. cliff
vs. grass" is answered by the real island's own data.

### Ocean

- **Purely procedural — zero storage.** Ocean chunks are generated on demand
  (water + region-hashed content). The sailing world costs only its shells
  on disk.
- **Finite, not infinite.** The grid is bounded; the edge is an escalating
  deterrent, not an invisible wall: worsening storm, a warning, then the
  ship is turned back.
- **World layout is an authored control map** one level above the island
  maps: island anchors/bounds, sea lanes, event zones.
- **World map UI for free:** downsampled per-island biome grids composited
  at world positions.
- Perf isolation: sailing never streams full island data; islands never
  stream ocean.

## Sailing Camera

- Normal gameplay viewport ≈ 30 tiles across; sailing zooms out ~3–4× →
  ~90–130 tiles visible across. Enough to read a long stretch of shoreline
  and the beginning of inland when approaching.
- Rendering cost is trivial for a tilemap (≤ ~10k visible tiles); no LOD
  system. If profiling ever demands it, skip decoration/overhead layers at
  max zoom.
- The zoomed viewport raises the ship-speed ceiling: the screen-crossing
  readability rule (~3+ seconds to cross the view) permits ~15–20 tiles/sec
  at sail, far above any on-foot gait, without control problems.

## The Ship

Ship tiles are character-scale (a ship is building-sized; the player walks
its deck). Two modes with the **wheel interaction as the seam**:

1. **Anchored/deck mode** — the ship is a static tile prefab at its world
   position: walkable deck, normal movement/collision rules, like a small
   building.
2. **Sailing mode** — at the wheel, the ship becomes a **vehicle sprite**
   piloted over the ocean: zoomed camera, broadsides, sailing events. The
   walkable deck is not simulated while under sail.

**Explicit non-goal (v1): a moving walkable tilemap.** Walking the deck of a
ship in motion is a notorious tile-engine tar pit and single-player loses
almost nothing by cutting it.

## Anchoring & Transitions

**Docks are hand-placed major POI pieces** on each island, present both in
the real island map (boarding point) and marked on the island's shell
(sail-to target). They are the seam between the two spaces.

- **v1 (recommended): docks-only.** Arrival and departure both happen at
  docks — sail into a dock zone on the shell → load the island map at the
  dock; interact with the ship at the dock → load the sailing world. The
  ship is always "at the dock you arrived at." Symmetric, no stranded-ship
  states, pure Pirates Online.
- **Later upgrade: free anchoring.** Anchor in any shallow band → spawn on
  the nearest beach; the island map spawns a re-boarding marker
  (rowboat/anchored-ship prop) at that spot, and the anchor position
  persists in the save. Adds wild-coast exploration freedom at the cost of
  extra state; fits on top of docks-only without redesign.

## Ocean Content (structure now, detail later)

Reuses the minor-POI machinery at sea: region-hashed encounters and sea
POIs — enemy ships, shipwrecks, sailing events (kraken etc.), minor islets
(exempt from the island sizing minimum). A sailing-speed analogue of the
minor-POI density rule (something to see/fight/loot within N minutes at
sail) can apply once ship speed is set.

## Open Knobs

- **Ship speed(s)** — and whether wind/upgrades modify it; added to
  `movement-speeds.md` when decided.
- **Gap sizing rule** — pick a crossing-time budget between neighboring
  islands (suggested 3–8 min at sail); gap distance = budget × ship speed.
  The world-layout map is sized from this, same philosophy as the island
  sizing rule.
- Sea encounter/event tables and densities; ship combat design.
- Whether/when to add free anchoring on top of docks-only (see Anchoring &
  Transitions).

## Interiors (noted here as the other deferred thread)

- **Major POI interiors** (city buildings, story dungeons): hand-built maps,
  attached to entrances in the prefabs.
- **Minor POI interiors** (caves, crypts): a generation method is plausible
  but **deliberately deferred** — open-world generation is the current
  focus. Not blocking: minor POI *placement* is already specced and
  interiors attach behind entrance transitions later.
