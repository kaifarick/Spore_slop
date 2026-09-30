# Architectural Decisions

Record important decisions here with context and rationale. Keep entries concise.

---

## ADR-001: Third-Person Camera — Cinemachine

**Status:** Accepted

**Context:** Phase 1 requires third-person movement and camera. Goal is to validate movement and camera feel (`Roadmap.md` Phase 1). Soft-lock behind the player was added after 2026-09-30 playtest (free-orbit felt wrong for continued sandbox play).

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

**Setup (look-driven yaw — current):**

- `CinemachineCamera` with **Third Person Follow** + **Pan Tilt**
- Follow target: `CameraTarget` (~1.6m). **Yaw = Look Pan** via `CameraTargetLookYaw` so TPF sits behind the camera heading (player stays framed). Ragdoll only moves `CameraTarget` position (hips); yaw stays look-driven.
- `CinemachinePanTilt` **ReferenceFrame = World**. Pan recentering **off** — camera yaw only from Look (strafe turns Visual, not the camera).
- `PlayerMovement` uses **live camera world yaw** for camera-relative move.
- `PlayerCameraInput`: Look → Pan/Tilt; **disarmed** until `sceneLoaded` + cursor lock + settled Look delta (Play/load spike guard). Cursor lock on by default.
- Rig distance/shoulder: Inspector. Do not use **Third Person Aim** for basic sandbox movement (keep disabled).

**Do not:**
- Drive `CameraTarget` from Visual while Pan is World — Aim and TPF disagree → player leaves frame
- Clear `CameraTarget` rotation during ragdoll while Look still writes Pan

**Optional later:** soft recenter Pan toward Visual only while moving mostly forward (not on strafe).

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

**When to revisit:** Start of Phase 4 (Ragdoll). → Revisited in **ADR-008**.

---

## ADR-003: Respawn — Y Threshold

**Status:** Accepted

**Context:** Phase 1 needs fall + respawn without extra systems.

**Decision:** `PlayerRespawn` teleports player when `position.y < fallThresholdY`. Spawn point is a scene Transform.

**Why:** Simplest working solution. Kill-zone triggers can be added later if level design requires them.

**When to revisit:** When building complex vertical levels (post Phase 1).

---

## ADR-005: Object Detection — Facing + Body-Height Box

**Status:** Accepted

**Context:** Phase 2 needs reliable "object in front of player" detection. Player root does not rotate; Visual child faces movement. Camera can free-orbit away from the character.

**Decision:** `PlayerInteraction` detects along the **Visual** (facing) forward, not camera look direction. Uses an `OverlapBox` in front of the player, vertically sized to span the player's own `CharacterController` height (+ a small margin), and selects the nearest `Grabbable`.

**Why:**

- Matches readable body facing for co-op physical play
- Avoids grabbing things behind the character while looking around
- Capability-based (`Grabbable`), not object-type checks
- **Vertical span matches the player's own height, not one fixed detection height** — a single narrow band (e.g. capsule at chest height) geometrically cannot reach short ground objects like food/resources. Debugged during Phase 2 step 1: a `0.35`-radius capsule at chest height missed a `0.6m`-tall test box no matter where it was placed, because the box's top never entered the capsule's narrow reachable band. Root cause was detection geometry, not object placement.

**When to revisit:** If camera-aim grab feels better after throw / ranged interactions exist, or if the box needs independent per-entity tuning (e.g. reaching tall NPCs vs. small items) beyond player-height + margin.

---

## ADR-006: Hold — Kinematic + Reparent (not a Joint)

**Status:** Accepted (Phase 2 step 2, playtested)

**Context:** Phase 2 needs a reliable "hold a grabbed object in front of the player" behavior. Options considered: (a) physics joint (`FixedJoint`/`ConfigurableJoint`) connecting the object to a hold anchor, or (b) directly parenting the object to a `HoldPoint` transform and making its `Rigidbody` kinematic while held.

**Decision:** `PlayerGrab` sets the `Grabbable`'s `Rigidbody.isKinematic = true`, disables its `Collider`s, and reparents it under `HoldPoint` (a child of `Visual`, so it follows body facing). Reversed on release.

**Why:**

- Fully predictable position/rotation while held — no jitter, no joint overshoot, no fighting the `CharacterController`'s non-physical movement
- Disabling colliders while held avoids the held object pushing the player (or being pushed) through unrelated physics contacts
- Matches AGENTS.md: "gameplay reliability and fun are more important than physical purity"
- Simplest version that still lets Phase 3 (dragging) or a later physics-driven carry replace this per-capability, without redesigning `Grabbable`/`PlayerInteraction`

**Trade-off:** No physical resistance/weight feedback while held, and the object can currently be "held" through walls/other geometry (no collision while carried). Acceptable for a Phase 2 test; revisit if carrying interacts badly with level geometry or other players in later phases.

**When to revisit:** Phase 3 (Dragging) or Phase 5 (Living Entities / carrying) if a physics-driven hold (joint-based) is needed for weight/momentum feel.

---

## ADR-007: Drag — Runtime ConfigurableJoint (Spring Drive) to a Kinematic Anchor

**Status:** Superseded / removed from Prototype 0 (2026-09-29)

**Context:** Phase 3 validated physical dragging of heavy props (`Draggable` + `PlayerDrag` + spring `ConfigurableJoint` to `DragAnchor`). Playtested; FixedJoint jitter led to spring drive.

**Why removed:** Prop-drag fought CharacterController (climb-on / clip-into box). Real drag target for the prototype is Phase 5 — drag an injured/ragdolled player by a limb, where that CC↔box conflict does not apply. Hold (`Grabbable`) stays.

**Decision now:** `PlayerDrag` / `Draggable` deleted. `HeavyProp` remains as inert scenery (no drag capability). Spring-joint lessons may inform Phase 5 player-drag later.

**When to revisit:** Phase 5 — drag ragdolled / injured entity.

---

## ADR-008: Ragdoll — CharacterController ↔ Humanoid Bone Ragdoll

**Status:** Accepted (Phase 4; revised 2026-09-29 — bone ragdoll)

**Context:** Phase 4 needs enter ragdoll → physics tumble → recover. After Mixamo X Bot, a single root capsule Rigidbody looked like a stiff prop (“fell like a rock”), not a ragdoll.

**Decision:** `PlayerRagdoll` + `PlayerRagdollBody`:

1. **Normal:** only `CharacterController` on the player root; bone RBs kinematic, bone colliders off; Animator on
2. **Ragdoll:** CC off; Animator off; bone rigidbodies / capsule-box colliders / `CharacterJoint`s active (built once from humanoid bones); tip linear + angular impulse on hips
3. **Recover:** bone physics off; root snapped to hips ground position; Animator plays **GettingUp**; input returns when the clip finishes (or `RecoverImmediate` for respawn)

Enter: debug **R**, or hard landing. Auto get-up after `autoRecoverDelay` (default 2.5 s); **R** during ragdoll starts GettingUp early; **R** during GettingUp skips to control. `CameraTarget` follows hips while tumbling (world-upright). Grab force-release on enter.

**Why:** Bone joints give readable limb tumble; matches the skinned placeholder and future Phase 5 “drag by a limb”.

**Trade-off:** Recover blends ragdoll pose into Mixamo GettingUp (~0.45s) and lifts the mesh to the floor while the clip plays (lying frames sit lower than the standing root). Visual yaw is kept from before the fall — deriving yaw from hips while lying caused a hard spin. A dedicated face-down clip can come later.

**GettingUp height (2026-09-30):** Mixamo Getting Up stores stand-up rise in Root Transform Position (Y). Red **Loop Match** dots on that channel are normal for a one-shot clip (start on floor ≠ end standing) — not an import error. Do **not** apply root motion onto `Visual/Model` and transfer Y at exit (caused upward jerk). Instead bake Root Y/XZ/Rotation into pose via `SporeSlop → Setup Player Animation`, keep `applyRootMotion = false`, and exit with `IsGettingUp = false` only.

**When to revisit:** Separate face-up / face-down get-up clips; joint limit tuning; incapacitated/rescue (Phase 5).

---

## ADR-009: Character Animation — Model under Visual, Animator Driven One-Way

**Status:** Accepted (Phase 4 step 3, pending playtest)

**Context:** Phase 4 needs a get-up animation, which requires a skinned character. Placeholder: Mixamo X Bot + Mixamo clips (Humanoid, one Avatar shared by all clips). Movement stays CharacterController-based (ADR-002); ragdoll is bone physics (ADR-008).

**Decision:** The model is a child (`Visual/Model`) of the existing `Visual`; the capsule mesh renderer is disabled. `Animator` has `applyRootMotion = false` — movement stays owned by `PlayerMovement` / ragdoll physics. `PlayerAnimation` only **reads** gameplay state and writes Animator parameters (`Speed`, `IsGrounded`, `IsHolding`); it never drives movement or ragdoll. While ragdolled, Animator is disabled so clips cannot fight bone physics. The controller and prefab wiring are produced by an idempotent Editor tool (`SporeSlop → Setup Player Animation`).

**Why:**

- Visual/physics separation (AGENTS.md: Input → Gameplay State → Physics / Visual Representation) keeps future networking possible
- No changes to movement logic just to show animation; the model can be swapped without touching gameplay code
- Humanoid + shared Avatar lets any Mixamo/other humanoid clip be reused

**Trade-off:** Root motion is ignored, so foot sliding is possible until Speed thresholds are tuned. Recover still snaps from ragdoll pose to Idle until GettingUp is wired.

**When to revisit:** GettingUp blend; if final creatures are not humanoid.
