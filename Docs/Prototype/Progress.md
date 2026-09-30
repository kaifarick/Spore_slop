# Prototype 0 — Progress

## Current Phase

Phase 2 complete. Look-driven camera + load-safe Look arming.
Next: Phase 5 injured carry/drag.

## Current Experiment

Physical co-op sandbox: movement, look-driven camera, grab/hold/drop/throw, ragdoll + get-up.

## Implemented (this session)

- Throw along character facing (`PlayerGrab`)
- Look-driven `CameraTarget` yaw; ragdoll follows hips position only
- Look arming: `sceneLoaded` + cursor lock + settled delta (`PlayerCameraInput`)
- Cleanup: drop unused `viewYawSource` from `PlayerMovement`; tighten camera input helpers

## Files Changed

- `Assets/_Project/Scripts/Camera/PlayerCameraInput.cs`
- `Assets/_Project/Scripts/Camera/CameraTargetLookYaw.cs`
- `Assets/_Project/Scripts/Player/PlayerMovement.cs`
- `Assets/_Project/Scripts/Player/PlayerGrab.cs`
- `Assets/_Project/Scripts/Player/PlayerRagdoll.cs`
- `Assets/_Project/Prefabs/Player.prefab`
- `Assets/_Project/Scenes/Prototype0.unity`
- `Docs/Decisions.md` (ADR-001), `Docs/Prototype/Progress.md`, `Docs/Prototype/Roadmap.md`

## Important Decisions

- Camera yaw = Look only (not Visual); throw = Visual forward
- Look gated on scene load + cursor lock + mouse settle

## Known Problems

- No throw animation
- Soft-recenter while running forward optional
- `HoldPointHandFollow` unused

## Next Task

Phase 5: grab / carry / drag injured entity + rescue.

## Last Updated

2026-09-30
