# Movement Speed Specification

Status: **Decided** (pending final feel-check in the flat-map speed prototype)

## Decided Speeds

| Gait          | Tiles/sec | Pixels/sec (16px tiles) | Ratio vs. player walk |
| ------------- | --------- | ----------------------- | --------------------- |
| Player walk   | 3.5       | 56                      | 1.00×                 |
| Horse walk    | 4.0       | 64                      | 1.14×                 |
| Player sprint | 5.5       | 88                      | 1.57×                 |
| Horse sprint  | 7.0       | 112                     | 2.00×                 |

Horses have two gaits only (walk and sprint); no intermediate gallop tier.

## Rationale

### Reference points

**Pokémon (Gen 4 era):**

| Gait    | Tiles/sec |
| ------- | --------- |
| Walking | 3.5       |
| Running | 7.5       |
| Biking  | 9.5       |

**Skyrim (vanilla, game units/sec):**

| Gait                 | Units/sec | Ratio vs. jog |
| -------------------- | --------- | ------------- |
| Walk                 | 80        | 0.22×         |
| Run/jog (default)    | 370       | 1.00×         |
| Sprint               | 500       | 1.35×         |
| Horse sprint         | 600       | 1.62×         |

### How we landed on the numbers

- **Baseline mapping:** Pokémon's walk (3.5 tiles/sec) maps to Skyrim's default
  jog (370 u/s) — both are "the speed you actually travel at." Skyrim's literal
  walk (80 u/s) is notoriously broken/unused and was excluded from ratio math.
- **Ratios are a compromise between Skyrim and Pokémon.** Skyrim's realistic
  ratios (1.35× sprint, 1.62× horse) read as underwhelming on a tile grid,
  where perceived speed comes almost entirely from tiles-scrolled-per-second
  (no FOV kick / headbob / motion blur to help). Pokémon's ratios (2.1× run,
  2.7× bike) exaggerate for feel, but the bike (9.5 tiles/sec, ~0.1s per tile)
  is faster than comfortable reaction time and hard to control. Our top speed
  of 7.0 (~0.14s per tile) stays controllable.
- **Interleaved tiers are intentional:** walk < horse walk < player sprint <
  horse sprint. A walking horse being slower than a sprinting human is
  realistic, and each gait gets a distinct job: precision / ambient travel /
  urgency on foot / covering distance.
- **Clean pixel math:** all four speeds are whole pixels-per-second on the
  16px grid, avoiding sub-pixel jitter.

## Traversal-Time Implications

Per the island sizing rule (see `map-specs.md`), a major island's longest
axis-aligned span is a 45–60 minute unobstructed walk (9,450–12,600 tiles):

| Gait          | Unobstructed crossing time |
| ------------- | -------------------------- |
| Player walk   | 45–60 min                  |
| Horse walk    | ~39–53 min                 |
| Player sprint | ~29–38 min                 |
| Horse sprint  | ~22–30 min                 |

Real traversal is designed to run **75–100% longer** than these unobstructed
times due to terrain friction (mountains, lakes, rivers, encounters, POI
distractions) — the same effect that stretches Skyrim's ~30–45 min
straight-line walk into a 1.5–2 hour experienced crossing. This friction
target is part of the island sizing rule in `map-specs.md`.

## Open Tuning Notes (for the prototype)

- The walk → horse-walk gap (3.5 → 4.0, +14%) may be imperceptible in motion.
  If so, nudge horse walk to 4.5.
- Consider stamina-gating player sprint (Skyrim-style) so sustained
  cross-country speed stays near walk pace, preserving the sense of distance.
- Sell speed tiers with feel juice rather than higher numbers: sprint dust
  particles, slight camera zoom-out while mounted, gallop screen shake.
- Horse handling can be slightly floaty (acceleration, turn momentum) — it
  covers control slop and sells the riding feel.
- Since top speed only 2.0× base, the horse needs value beyond speed (e.g.
  stamina-free sustained travel, carry capacity, fewer/no encounters in tall
  grass). To be designed later.
