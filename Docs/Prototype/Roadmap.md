# Prototype 0 Roadmap

## Phase 1 — Player Foundation

- [x] Third-person movement
- [x] Camera (free-orbit Cinemachine — Phase 1)
- [x] Camera soft-lock / behind player (playtest follow-up → look-driven yaw)
- [x] Running
- [x] Jumping
- [x] Falling
- [x] Respawn

Goal: Validate basic movement and camera feel.

**Status:** Movement + look-driven third-person camera (2026-09-30).

**Next:** Phase 5 injured carry/drag (or feel tuning).

---

## Phase 2 — Physical Interaction

- [x] Detect physical objects
- [x] Grab
- [x] Hold
- [x] Release
- [x] Throw

Goal: Validate basic physical interaction.

**Status:** Grab/hold/release/throw playtested path complete (throw 2026-09-30).

---

## Phase 3 — Dragging

- [x] Grab heavy object
- [x] Pull object
- [x] Release object

Goal: Validate physical dragging.

**Status:** Playtested (2026-09-28), then **removed** (2026-09-29). Prop-drag vs CharacterController was a dead end; Hold remains. Player/injured drag stays Phase 5. See ADR-007.

---

## Phase 4 — Ragdoll

- [x] Enter ragdoll
- [x] Physics-controlled body (humanoid bones)
- [x] Recover (auto after delay + GettingUp anim)
- [x] GettingUp → Locomotion exit without sink/toss (baked root Y + floor stick — 2026-09-30)
- [x] Locomotion/hold polish (Speed from input, CycleSpeed feet, Holding layer weight)

Goal: Validate whether falling, impacts and ragdoll feel fun.

**Status:** Complete for Prototype 0 playtest (2026-09-30).

---

## Phase 5 — Living Entities

- [ ] Grab injured entity
- [ ] Carry injured entity
- [ ] Drag injured entity
- [ ] Rescue / recover

Goal: Validate cooperative physical interaction.

---

## Phase 6 — Simple Gameplay Interactions

- [ ] Eat food
- [ ] Basic attack
- [ ] Call / shout
- [ ] Generic object interaction

Goal: Test basic gameplay verbs.

---

## Phase 7 — NPC Experiment

- [ ] Basic NPC interaction
- [ ] Experimental mating interaction

Goal: Test whether NPC interactions create interesting emergent gameplay.

---

## Phase 8 — Multiplayer Experiment

- [ ] 2-player connection
- [ ] Player movement synchronization
- [ ] Basic physical interaction
- [ ] Ragdoll synchronization
- [ ] Carry / drag synchronization

Goal: Determine whether the current gameplay works in multiplayer.

---

## Later

- [ ] 4–6 player test
- [ ] Large cooperative objects
- [ ] Procedural map experiment
- [ ] Progression prototype
- [ ] Final game-specific mechanics

---

## Deferred (not blocking current order)

- Kill-zone trigger volumes (alternative to Y-threshold respawn)
- Feel tuning pass for movement defaults
- Optional soft-recenter camera while running forward
- Prop-drag system (removed; revisit only if needed outside Phase 5 player-drag)

## Current order (2026-09-30)

1. Phase 5 injured carry/drag
2. Feel tuning as needed from playtest
