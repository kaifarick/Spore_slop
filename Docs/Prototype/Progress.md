# Prototype 0 — Progress

## Current Phase

Phase 1 — Player Foundation (**complete**)

Next: Phase 2 — Physical Interaction

## Current Experiment

Validate whether basic third-person movement and camera feel are good enough to continue the prototype.

**Playtest result:** acceptable. Walk, sprint, jump, fall and respawn work. Camera feel usable for further iteration. Free orbit (can look away from the character) accepted for now.

## Implemented

- Third-person **walk** and **sprint** (camera-relative, Input System)
- **Jump**, **gravity**, **falling**
- **Respawn** via Y threshold + spawn point
- **Cinemachine 3.1.7** third-person camera (Third Person Follow + Pan Tilt)
- **PlayerCameraInput** — Look input and sensitivity only
- Test scene **Prototype0** with ground, gap, spawn point
- **Player** prefab (CharacterController, Visual + CameraTarget children)

## Files Changed (Phase 1)

**Scripts**

- `Assets/_Project/Scripts/Player/PlayerMovement.cs`
- `Assets/_Project/Scripts/Player/PlayerRespawn.cs`
- `Assets/_Project/Scripts/Camera/PlayerCameraInput.cs`

**Assets**

- `Assets/_Project/Prefabs/Player.prefab`
- `Assets/_Project/Scenes/Prototype0.unity`

**Project**

- `Packages/manifest.json` — Cinemachine 3.1.7
- `ProjectSettings/EditorBuildSettings.asset` — Prototype0 in build
- `Assets/InputSystem_Actions.cs` — generated input class (user)

**Documentation**

- `AGENTS.md` — Debugging Unknown Bugs (no hack fixes)
- `Docs/Decisions.md` — ADR-001, ADR-002, ADR-003, ADR-004

## Important Decisions

- **Cinemachine** for third-person camera; rig tuning in Inspector, not in code (ADR-001)
- **CharacterController** for Phase 1 movement; revisit at ragdoll (Phase 4) (ADR-002)
- **Y-threshold respawn** with scene Transform spawn point (ADR-003)
- **No hack fixes** — debug first, fix root cause (ADR-004)
- **Player root does not rotate** — Visual child rotates toward movement; CameraTarget stays stable
- **Movement yaw** from Cinemachine Pan Tilt, not Main Camera transform
- Asset root: `Assets/_Project/`

## Known Problems

- Camera uses **free orbit** — player can rotate view away from the character. Not a blocker for Phase 1.
- **Third Person Aim** on the vcam must stay **disabled** (not used for sandbox movement).
- **Cinemachine rig** (distance, damping, obstacles) tuned manually in Inspector per scene.
- **CharacterController / mesh alignment** may need per-scene adjustment depending on spawn height.
- Formal **feel tuning pass** not done yet — current defaults accepted as starting point.

## Next Task

**Phase 2, step 1:** add a small physical test object (Rigidbody + Collider) and minimal **object detection** in front of the player (raycast or overlap). No full grab system yet.

Goal: confirm the player can reliably identify a grabbable object before implementing grab/hold/release.

## Last Updated

2026-09-23
