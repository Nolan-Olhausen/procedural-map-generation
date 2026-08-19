# Ocean & Sailing Plan

Status: **Architecture decided**; ship speed, gap sizing, and sea content are
open knobs. Reference feel: Pirates of the Caribbean Online (now The Legend
of Pirates Online) — walkable ship decks, zoomed-out sailing, broadsides,
anchoring at islands.

## Core Architecture: One Continuous World Space

The archipelago is **one world-scale tile grid**. Each island's baked region
files sit at fixed world offsets; everything between is ocean. Requirements
this satisfies:

- Islands are visible from the sea (shoreline + start of inland) and are
  approached, not loaded — no scene breaks at sea.
- The world map is honest: islands have real coordinates, and "check the
  map, sail that heading" genuinely finds them.

**Ocean is purely procedural — zero storage.** Ocean chunks are generated on
demand (water tiles + region-hashed content), so a mostly-ocean
60k×60k-tile world costs exactly what its islands cost on disk. Sailing uses
the same chunk-streaming system as walking with a larger load radius.

- **Finite, not infinite.** The world grid is bounded. The edge is an
  escalating deterrent, not an invisible wall: worsening storm, a warning,
  then the ship is turned back — a bounded grid that reads as a boundless
  sea.
- **World layout is an authored control map** one level above the island
  maps: a world-scale image placing island anchors/bounds, sea lanes, and
  event zones.
- **World map UI for free:** downsampled per-island biome grids composited
  at world positions.

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

## Anchoring

- Anchor anywhere in the **shallow-water band** ringing each island (the
  coastline pass already produces beaches/shallows). The player steps off
  onto the beach; the ship persists at its anchored world position in the
  save.
- Major port cities additionally include proper docks in their prefabs —
  free-explorer anchoring and civilized harbor arrivals both work.

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
- Anchoring polish: rowboat vs. direct beach step-off; re-boarding.

## Interiors (noted here as the other deferred thread)

- **Major POI interiors** (city buildings, story dungeons): hand-built maps,
  attached to entrances in the prefabs.
- **Minor POI interiors** (caves, crypts): a generation method is plausible
  but **deliberately deferred** — open-world generation is the current
  focus. Not blocking: minor POI *placement* is already specced and
  interiors attach behind entrance transitions later.
