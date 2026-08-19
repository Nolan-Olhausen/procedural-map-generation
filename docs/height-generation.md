# Height Generation Plan

Status: **Decided** (level-scale size, per-biome parameters, and ramp/stair
art constraints to be finalized against the art pack).

Runs after coastline refinement and **before final biome resolution** (so
altitude can bias biome dithering — snowcaps), and before rivers/roads (which
need elevation to flow/pathfind).

## Model: Continuous Field → Discrete Terraces

16px top-down art has no smooth slopes: height is expressed as flat
**terraces** separated by cliff faces drawn with the art pack's terrain wall
tiles, connected by ramps/stairs (Pokémon-style). The pipeline builds a
continuous height field, then quantizes it.

1. **Authored elevation layer** — coarse grayscale companion to the biome
   control map. Macro intent only: where the highlands are, which way the
   land slopes. Must fade to 0 at the painted shoreline (validated;
   coastline is level 0 by definition).
2. **Per-biome noise detail** — layered noise added to the authored base,
   with **amplitude and frequency taken from each biome's manifest entry**:
   - Mountains: high amplitude + frequency → many closely packed terraces.
   - Plains: low amplitude → terrain crosses only 1–2 quantization
     thresholds; occasional gentle ledges.
   - In biome transition bands, the already-computed blend weights
     interpolate these generation parameters, so terrain character shifts
     across the band with no special casing.
3. **Quantize** to a global integer level scale (working assumption 0–7;
   final count set by the art pack). Cliff lines fall on level boundaries.
4. **Morphological cleanup** — raw quantized noise produces shapes wall
   tiles cannot draw. Enforce until every configuration is representable:
   - Minimum terrace width (~3 tiles); no single-tile ledges or holes.
   - No diagonal stair-stepping the autotiler can't express.
   - Room for the wall art's vertical run (top edge + face + base rows).

   The old prototype stalled on rock-wall generation precisely because this
   cleanup wasn't a distinct stage; it is one now.

Deterministic as always: same inputs + seed → same terrain.

## Height ↔ Biome Interaction (Snow at Altitude)

The painted biome map stays regionally authoritative. The manifest adds
per-island **altitude rules** — e.g. `level ≥ 5 → snow`, `level ≥ 4 →
rocky` — which inject extra biome pressure into the same clumped-dithering
resolution used for biome blending. Results:

- Snowlines are ragged and natural (snow fingers down gullies, bare patches
  on ridges), never a horizontal stripe.
- Snowcapped peaks appear on any island tall enough, unpainted.
- Sea-level snow (arctic island) still works — painted pressure and altitude
  pressure sum.

## Accessibility Guarantee

**Hard rule: every walkable region is reachable unless explicitly flagged
off-limits.** Enforced by the pipeline on every bake, not by authoring care.

1. **Plateau graph** — flood-fill contiguous walkable areas per level
   (nodes); ramps/stairs are edges.
2. **Ramp placement pass** — every plateau gets ≥1 connection toward its
   lower neighbor, sited on straight cliff runs long enough for the
   ramp/stair art, avoiding POI footprints.
3. **Connectivity validation** — flood-fill from the coast/spawn. Any
   unreached walkable region → **auto-carve a ramp** at the shortest cliff
   segment separating it from reachable land; hard-fail with coordinates if
   impossible. The preview tool reports this check alongside the span and
   friction checks.
4. **Off-limits is opt-in** — a control-map marker or POI flag exempts a
   region (scenic peak, story-gated area). Isolation is always an authored
   decision, never an accident.

### Ramp Density Is the Friction Knob

The island sizing rule requires a 1.75×–2.0× detour factor. Cliff walls with
sparse, well-placed ramps are the primary source of that friction: a plateau
whose only ramp faces away from the traveler forces exactly the go-around the
rule wants. If the friction check reads low, reduce/relocate ramps; if high,
add them. Sizing rule and height generation tune each other.

## Baked Data & Runtime

- Per-tile **elevation level** ships as one more byte layer in the chunk
  format (vast same-level runs → compresses to nearly nothing).
- Runtime uses it for: movement blocking at cliff edges, wall rendering and
  draw-order/overlap, and later systems if wanted (one-way ledge hops,
  height advantage, fall rules). The data is present either way.

## Art Pack Dependencies (to verify)

- Wall tile set: which cliff configurations exist (outer/inner corners,
  face heights), per-biome wall variants.
- Ramp/stair tiles: widths, which biomes have which style (stairs in rock,
  slope in grass).
- Whether walls have per-biome variants matching the terrain variant arrays.

These determine the final level count, minimum terrace width, and ramp siting
constraints in the cleanup pass.

## Tuning Knobs (config, not repaints)

| Knob                                | Effect                                  |
| ----------------------------------- | --------------------------------------- |
| Global level count                  | Vertical granularity of the world       |
| Per-biome noise amplitude/frequency | Terrace density per biome               |
| Minimum terrace width               | Chunkiness of cliffs; autotile safety   |
| Altitude rules (level → biome)      | Snowline/rockline heights per island    |
| Ramps per plateau + siting bias     | Traversal friction (detour factor)      |
| Off-limits markers                  | Authored unreachable areas              |
