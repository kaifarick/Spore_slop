# Prototype 0 — Progress

## Current Phase

Phase 2 complete (grab/hold/release/throw). Look-driven third-person camera polished.
Next: Phase 5 injured carry/drag.

## Current Experiment

Physical co-op sandbox: CharacterController locomotion, look-driven Cinemachine camera,
grab/hold/drop/throw, humanoid ragdoll + GettingUp, Mixamo locomotion/hold.

## Implemented (this session)

- Throw: Attack while holding; impulse along Visual facing (`throwSpeed` as VelocityChange)
- Look-driven camera: `CameraTargetLookYaw` sets CameraTarget yaw from Pan (strafe does not swing camera)
- Ragdoll camera: hips follow for position only; yaw stays look-driven
- Look arming: disarm until `sceneLoaded` + cursor lock + settled Look delta (`PlayerCameraInput`)
- Cleanup: removed unused `viewYawSource` from `PlayerMovement`

## Files Changed

- `Assets/_Project/Scripts/Player/PlayerGrab.cs`
- `Assets/_Project/Scripts/Player/PlayerMovement.cs`
- `Assets/_Project/Scripts/Player/PlayerRagdoll.cs`
- `Assets/_Project/Scripts/Camera/PlayerCameraInput.cs`
- `Assets/_Project/Scripts/Camera/CameraTargetLookYaw.cs`
- `Assets/_Project/Prefabs/Player.prefab`
- `Assets/_Project/Scenes/Prototype0.unity`
- `Docs/Decisions.md` (ADR-001), `Docs/Prototype/Progress.md`, `Docs/Prototype/Roadmap.md`

## Important Decisions

- Camera yaw = Look Pan only (World PanTilt); not locked to Visual
- Throw direction = character facing (Visual), not camera
- Look gated on load/focus settle + cursor lock (Play-mode mouse spike guard)
- CameraTarget: ragdoll owns position; LookYaw owns yaw

## Known Problems

- No throw animation
- Soft-recenter while running forward not implemented (optional)
- `HoldPointHandFollow` unused (off on prefab)
- Imported map meshes may lack colliders — CharacterController needs static colliders on world geometry
- Scene vs prefab Player overrides can drift after Setup re-runs

## Next Task

Phase 5 first step: grab a ragdolled / injured entity (or proxy) — detect + hold/drag limb without rewriting movement/camera.

## Last Updated

2026-09-30
