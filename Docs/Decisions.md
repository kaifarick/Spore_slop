# Architectural Decisions

Record important decisions here with context and rationale. Keep entries concise.

---

## ADR-001: Third-Person Camera — Cinemachine

**Status:** Recommended (pending install)

**Context:** Phase 1 requires third-person movement and camera. Goal is to validate movement and camera feel (`Roadmap.md` Phase 1).

**Decision:** Use **Cinemachine** (`com.unity.cinemachine`) instead of a custom orbit camera script.

**Why install it:**

- Official Unity package, not a third-party dependency
- Phase 1 goal explicitly includes **camera feel** — Cinemachine is faster to tune in the Inspector (distance, damping, FOV, collision avoidance)
- Built-in **camera collision** (avoid clipping through walls) — painful to implement reliably in a custom script
- Ragdoll (Phase 4) and carry (Phase 5) will change player transform unpredictably — Cinemachine handles follow targets better than a minimal custom script
- Less project-owned camera code to maintain while movement/interaction systems are still changing

**Alternative rejected for now:** Custom `ThirdPersonCamera.cs` — viable for a bare-minimum prototype, but shifts time from movement tuning to camera bugs (smoothing, collision, edge cases).

**Install:**

```
Window → Package Manager → Unity Registry → Cinemachine → Install
```

Or add to `Packages/manifest.json` (use latest **3.1.x**, not 2.x):

```json
"com.unity.cinemachine": "3.1.7"
```

On Unity 6000.3, verify in Package Manager that version 3.1.x is selected — older Unity versions may default to Cinemachine 2.x.

**Phase 1 setup (minimal):**

- `CinemachineCamera` with **Third Person Follow** + **Pan Tilt**
- Follow target: empty child on player (`CameraTarget`, ~1.6m height)
- Rig tuning (distance, damping, shoulder, obstacles): **Cinemachine Inspector only**
- `PlayerCameraInput`: **Look input + sensitivity only** — does not override Cinemachine settings
- Do not use **Third Person Aim** for basic sandbox movement (disable or remove on the vcam)

**When to revisit:** If Cinemachine feels wrong for cooperative physics gameplay after Phase 4 (ragdoll), evaluate custom follow or hybrid approach.

---

## ADR-004: No Hack Fixes — Debug First

**Status:** Accepted

**Context:** During Phase 1 camera debugging, a script overwrote Cinemachine Inspector settings every Play to mask misconfiguration. This was a symptom-level workaround, not a root-cause fix.

**Decision:** Do not fix bugs with hacks or workarounds. Fixes must be logical, minimal, and fit the project architecture. If the cause is unclear, add diagnostic logs, ask for Console output, find the root cause, then fix it in the correct layer (see `AGENTS.md` → Debugging Unknown Bugs).

**Examples of hacks to avoid:** runtime Inspector overrides, hidden forced defaults, disabling components from code instead of fixing prefabs/scenes, unrelated "fix-all" logic.

**Exception:** Small bridge scripts with a clear single responsibility (e.g. feeding Look input into Pan/Tilt) are fine. They must not silently re-configure unrelated systems.

---

## ADR-002: Player Movement — CharacterController (Phase 1)

**Status:** Accepted

**Context:** Phase 1 needs reliable walk, run, jump, fall, respawn. Ragdoll is Phase 4.

**Decision:** Use `CharacterController` for Phase 1 movement.

**Why:**

- Predictable control for feel iteration
- Simple ground check and jump
- Ragdoll not required yet

**Trade-off:** Will likely require refactor or hybrid (CC ↔ Rigidbody) when ragdoll is added in Phase 4. Keep movement logic isolated in `PlayerMovement` so camera and respawn are unaffected.

**When to revisit:** Start of Phase 4 (Ragdoll).

---

## ADR-003: Respawn — Y Threshold

**Status:** Accepted

**Context:** Phase 1 needs fall + respawn without extra systems.

**Decision:** `PlayerRespawn` teleports player when `position.y < fallThresholdY`. Spawn point is a scene Transform.

**Why:** Simplest working solution. Kill-zone triggers can be added later if level design requires them.

**When to revisit:** When building complex vertical levels (post Phase 1).
