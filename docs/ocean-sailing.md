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

**Positional fidelity, not distance fidelity.** The sailing world runs at
its own compressed **sea scale** relative to land (shells are smaller than
their islands); the dock transition means the two scales are never visible
at once. What must hold — and does, by construction — is that island
*positions and bearings* match the viewable world map: the authored world
layout map is the single source for the sailing world, the shell placements,
and the map UI (rendered with a live ship marker). "Check the map, sail that
heading" always finds the island because chart and sea are the same data.

### Island shells are derived, not authored

A post-bake step generates each shell automatically from the island's
finished terrain grids:

1. Extract a **coastal ring** — the shoreline plus enough inland to fill the
   zoomed sailing camera's view when hugging the coast.
2. Keep base terrain only (logical grids): shallow water, beach, first
   inland grass/snow/rock, cliff walls where cliffs meet the sea. No
   decorations, props, overhead layers, or POIs (dock locations excepted —
   they must be visible to sail to).
3. **Downsample to sea scale** — majority-vote the logical terrain per
   block, then **re-run the autotiler at shell scale**. Downsampling logic
   and re-tiling (rather than scaling tiles) keeps coastlines clean; the
   shell still faithfully shows which stretches are beach vs. cliff vs.
   grass, just smaller. Docks get a guaranteed minimum shell footprint so
   they stay visible and enterable at any scale.
4. Void the interior entirely — never visible, zero tiles stored.
5. Write as a small region-file set for the sailing world (~a megabyte per
   island vs. tens for the full island).

Derived means never stale: re-bake an island and its shell regenerates with
the correct coastline automatically.

### Ocean Generation

Everything at sea is a deterministic function of (world seed, chunk coords),
generated in a ring around the ship and discarded behind it — **zero
storage**; the sailing world costs only its shells on disk.

- **Water** — noise-varied tile variants so open sea isn't visually flat.
- **Obstacles** — rocks, reefs, sandbars, wreckage via region-hash
  placement; density controlled by zones painted in the world layout map
  (calm near docks, treacherous where a strait should be feared).
- **Sea content** — shipwrecks, drifting cargo, minor islets (exempt from
  the island sizing minimum): region-hashed like land minor POIs. Once ship
  speed is set, the minor-POI density rule can apply at sail so open water
  never goes dead.
- **Enemies/events** — spawn *data*, not entities (same as land):
  distance-from-island danger bands (coastal water calm, deep water
  dangerous) plus painted event zones for kraken-class encounters and story
  moments; a runtime spawner manages live ships.
- **Sea lanes: the reserved-path guarantee at sea.** A* lanes between all
  dock pairs through the obstacle-zone map are reserved as obstacle-free
  corridors and connectivity-validated per bake. A reef can narrow a lane;
  it can never close one.

### Bounds & Navigation

- **Finite.** The layout map's bounds are the world: an **invisible barrier
  plus an "Uncharted Seas — too dangerous to sail further" warning** at the
  edge. (An escalating storm effect can layer on later as pure
  presentation.)
- **World layout is an authored control map** one level above the island
  maps: island anchors/bounds, obstacle/danger zones, event zones.
- **World map UI** renders the layout map with a live ship marker —
  chart and sea are the same data, so map navigation is honest by
  construction.
- **Ocean sizing rule** (companion to the island sizing rule): the sailing
  world's edge-to-edge crossing takes **30–45 minutes at sailing speed**.
  Exact tile dimensions are derived, not chosen, via the dependency chain:

  > sailing camera zoom (set by how the ship reads on screen) →
  > readable ship speed (screen-crossing rule: ~3+ seconds to cross the
  > viewport) → ocean dimensions (30–45 min × speed) → sea scale / shell
  > downsample ratio.

  All four resolve together from the sailing prototype (ship sprite size +
  zoom feel); none are whiteboard decisions. Island gaps then fall out of
  the layout map's positions within the sized ocean.
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

- **The zoom/speed/size/scale bundle** — resolved together by the sailing
  prototype per the dependency chain in Bounds & Navigation (zoom → ship
  speed → ocean dimensions → sea scale). Ship speed lands in
  `movement-speeds.md` once set; wind/upgrade modifiers decided with ship
  combat.
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
