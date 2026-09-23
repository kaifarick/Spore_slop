\# Prototype Architecture



\## Main gameplay concepts



Player

Entity

Interaction

Physics

Ragdoll

NPC

Food

Resource



\---



\## Player



The player is composed of independent capabilities.



Potential components:



PlayerMovement

PlayerLook

PlayerInteraction

PlayerGrab

PlayerCarry

PlayerRagdoll

PlayerHealth

PlayerAttack

PlayerCall



Not all components need to exist immediately.



\---



\## Entity



Entities may expose capabilities.



Examples:



Grabbable

Carryable

Draggable

Edible

Attackable

Interactable



An entity should only implement capabilities that it actually supports.



\---



\## Interaction



The player detects an interactable target.



The interaction system determines what actions are available.



The player should not contain hardcoded logic for every possible object type.



\---



\## Physics



Physics objects use Rigidbody when physical simulation is useful.



Gameplay-critical interactions may use controlled physics or constraints when necessary.



\---



\## Ragdoll



Ragdoll is a state of an entity.



Possible state flow:



Normal

&#x20;   ↓

Ragdoll

&#x20;   ↓

Recover



Potential future state:



Normal

&#x20;   ↓

Incapacitated

&#x20;   ↓

Ragdoll

&#x20;   ↓

Rescue

&#x20;   ↓

Recover



The exact state model is not finalized.



\---



\## Future Networking



Networking will eventually be introduced.



Current architecture should not prevent synchronization of:



\- player movement

\- entity state

\- interaction state

\- carried objects

\- ragdoll state

\- health/injury state



However, no networking implementation should be created during Prototype 0 unless explicitly requested.

