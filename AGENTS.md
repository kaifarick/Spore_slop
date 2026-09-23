\# AGENTS.md



\## Project



This is a Unity 6 PC 3D cooperative game prototype.



Target:



\* 4–6 players

\* PC

\* Steam

\* Cooperative gameplay

\* Small development team

\* Rapid experimentation is more important than production completeness



The final game concept is not fully defined yet.



The current goal is to discover and validate the fundamental gameplay through a small physical co-op prototype.



\---



\# Current Development Phase



We are building Prototype 0.



Prototype 0 is a gameplay sandbox, not the final game.



Its purpose is to test whether the fundamental physical interactions are fun, readable, reliable and capable of creating emergent cooperative gameplay.



Potential prototype interactions include:



\* running

\* jumping

\* falling

\* grabbing

\* carrying

\* dragging

\* throwing

\* releasing objects

\* ragdoll

\* recovering from ragdoll

\* rescuing injured entities

\* eating food

\* basic attacks

\* calling / vocal communication

\* object interaction

\* basic NPC interaction

\* experimental mating interaction



Do not implement final game systems unless explicitly requested.



Do not assume the final game design.



\---



\# Source of Truth



Use the following sources in this order:



1\. The actual Unity project and existing code

2\. `AGENTS.md`

3\. Relevant files in `Docs/`

4\. Current task instructions

5\. Chat history



Chat history is temporary.



Important decisions and project state must exist in the repository, not only in the chat.



Do not rely on memory of previous conversations when the information can be verified from the project.



\---



\# Documentation



The repository may contain:



```text

Docs/

├── Architecture.md

├── Decisions.md

└── Prototype/

&#x20;   ├── Toy.md

&#x20;   ├── Roadmap.md

&#x20;   └── Progress.md

```



Use these files according to their purpose.



\### `Architecture.md`



Describes the intended technical architecture.



\### `Decisions.md`



Contains important architectural decisions and their reasons.



\### `Prototype/Toy.md`



Describes the current prototype goals and gameplay experiments.



\### `Prototype/Roadmap.md`



Describes Prototype 0 phases, milestones and goals. Use it to understand scope and priority order.



\### `Prototype/Progress.md`



Describes what has actually been implemented, known problems, decisions and the next step.



Do not turn documentation into a transcript of the chat.



Only record durable information.



\---



\# Architecture



Use:



\*\*Component-Based Architecture + Composition over Inheritance + Capability-Oriented Gameplay\*\*



Gameplay should generally be built from small focused components.



Prefer composition over deep inheritance hierarchies.



Avoid large classes that contain unrelated gameplay responsibilities.



Avoid speculative abstractions.



\---



\## Player Architecture



The player should be composed from focused capabilities/components when needed.



Possible components include:



```text

PlayerMovement

PlayerLook

PlayerInteraction

PlayerGrab

PlayerCarry

PlayerRagdoll

PlayerHealth

PlayerAttack

PlayerCall

```



Do not create all components automatically.



Implement only the components required by the current milestone.



Avoid putting all player functionality into one large `PlayerController`.



\---



\## Entity Capabilities



Objects and entities should expose capabilities rather than relying on hardcoded object types.



Possible capabilities include:



```text

Grabbable

Carryable

Draggable

Edible

Attackable

Interactable

```



For example, a food object may be:



```text

Rigidbody

Grabbable

Edible

```



A corpse may be:



```text

Rigidbody

Grabbable

Carryable

Draggable

```



Interaction systems should prefer checking capabilities over checking concrete object types.



Avoid systems such as:



```text

if target is Corpse

if target is Food

if target is NPC

```



when a capability-based solution is sufficient.



\---



\# Interaction Architecture



Interaction should generally follow:



```text

Player

&#x20; ↓

Interaction detection

&#x20; ↓

Target

&#x20; ↓

Available capabilities

&#x20; ↓

Action

```



The interaction system should remain generic.



Do not create object-specific interaction systems unless there is a clear gameplay reason.



Do not build the entire interaction framework before the current prototype needs it.



\---



\# Physics



Physics is an important part of the gameplay.



Use Unity physics where appropriate:



\* Rigidbody

\* Collider

\* joints

\* forces

\* controlled physics



However:



\*\*Gameplay reliability and fun are more important than physical purity.\*\*



Do not pursue physically perfect simulation if it makes controls unreliable or frustrating.



Physics interactions should be controllable and readable.



\---



\# Ragdoll



Ragdoll should be treated as a gameplay state.



The basic model is:



```text

Normal

&#x20; ↓

Ragdoll

&#x20; ↓

Recover

&#x20; ↓

Normal

```



Future states such as incapacitated, injured or being rescued may be added later.



Do not over-engineer the state system before those states are actually required.



\---



\# Carrying and Dragging



Carrying and dragging should be implemented as reusable physical interactions.



The system should eventually be able to support things such as:



\* food

\* resources

\* corpses

\* injured players

\* babies / young creatures

\* NPCs

\* other physical entities



Do not create separate systems for each object type unless necessary.



Large cooperative objects moved by multiple players are a future feature.



Do not implement them unless explicitly requested.



\---



\# NPCs



NPC systems should remain minimal during Prototype 0.



The initial goal is to test basic interaction.



Do not create a complete AI ecosystem, reproduction simulation or complex social system unless explicitly requested.



The experimental mating interaction is a gameplay test, not a requirement for a complete reproduction system.



\---



\# Multiplayer



Networking is intentionally postponed during the earliest prototype.



Do not:



\* add networking packages

\* implement Steam networking

\* create dedicated-server infrastructure

\* create matchmaking

\* create network synchronization systems



unless explicitly requested.



The eventual target is approximately 4–6 players on Steam.



Even without networking, keep gameplay reasonably separated into:



```text

Input

&#x20; ↓

Gameplay State

&#x20; ↓

Physics / Visual Representation

```



This keeps future multiplayer implementation possible without prematurely implementing it.



\---



\# Dependencies



Do not add third-party packages or frameworks without explicit approval.



Prefer Unity's built-in systems and simple project-owned code.



Do not introduce architecture frameworks, dependency injection containers, ECS/DOTS or other major technologies unless explicitly requested.



\---



\# Code Style and Design



Prefer:



\* small focused classes

\* clear responsibilities

\* composition

\* serialized gameplay parameters

\* simple dependencies

\* readable code

\* explicit behavior

\* easy experimentation



Avoid:



\* giant manager classes

\* deep inheritance

\* unnecessary interfaces

\* speculative abstractions

\* global state

\* unnecessary singletons

\* premature optimization

\* framework-heavy solutions



Use `SerializeField` for gameplay values that need to be tuned during prototyping.



Examples include:



\* movement speed

\* acceleration

\* jump force

\* gravity

\* air control

\* rotation speed

\* interaction distance

\* grab strength

\* throw force



\---



\# AI Development Workflow



For every coding task:



1\. Inspect the relevant documentation.

2\. Inspect the relevant existing code.

3\. Explain the intended approach briefly when the task is architectural or ambiguous.

4\. Implement the smallest useful version.

5\. Avoid unrelated changes.

6\. Check for compilation or obvious syntax errors.

7\. Inspect the files changed by the task.

8\. Explain how to test the result.

9\. Update project progress when the task represents a meaningful milestone.



Do not refactor unrelated code while implementing a feature.



Do not silently change architectural decisions.



If an important architectural decision is required and the requirements are unclear, ask before implementing it.



\---



\# Debugging Unknown Bugs



Do **not** fix bugs with hacks or workarounds.



A fix must be:



\* **logical** — the cause and the solution are understandable

\* **architecturally consistent** — it fits the existing systems and responsibilities

\* **minimal** — changes only what the bug actually requires

\* **maintainable** — it does not create hidden behavior or duplicate sources of truth



If the root cause is unclear, **debug first** — do not patch symptoms.



\### Avoid hack fixes such as:



\* overwriting Inspector / serialized settings at runtime to mask misconfiguration

\* forcing hidden defaults every Play Mode session

\* disabling components from code instead of fixing scene / prefab setup

\* adding unrelated systems, managers, or "fix-all" blocks to silence a symptom

\* special-case logic that bypasses the intended architecture

\* speculative changes to multiple systems "just in case"



These may hide the bug temporarily but make the project harder to understand, tune and extend.



\### Preferred workflow when the bug is unclear:



1\. Reproduce the issue and identify which system is actually involved.

2\. Add **temporary diagnostic logs** (`Debug.Log`, clearly prefixed) at the relevant point.

3\. Ask the user to reproduce once and **share the Console output**.

4\. Identify the root cause.

5\. Apply the fix in the **correct layer** (scene, prefab, component settings, or focused code).

6\. Remove or reduce diagnostic logs after the fix is confirmed.



Inspector and prefab values remain the source of truth for tuning unless an ADR documents otherwise.



When choosing between a quick workaround and a proper fix, prefer the proper fix — especially during prototype work, where hidden hacks accumulate fast.



\---



\# One Task at a Time



Prefer small, testable tasks.



Good:



```text

Implement player jumping.

```



```text

Implement basic object grabbing.

```



```text

Fix grabbed-object rotation.

```



```text

Add ragdoll recovery.

```



Avoid giant tasks such as:



```text

Build the complete interaction system.

```



or:



```text

Build the whole game.

```



Break large features into milestones.



After each meaningful milestone, the game should be playable and testable.



\---



\# Context and Token Efficiency



Optimize for useful context, not maximum project inspection.



Do not read, search or modify files that are not relevant to the current task.



The goal is to minimize unnecessary context while maintaining enough information to make correct changes.



\---



\## Start Narrow



Before exploring the repository:



1\. Read `AGENTS.md`.

2\. Read only the documentation relevant to the task.

3\. Identify the specific folders and scripts involved.

4\. Inspect only those files.



Do not perform broad repository exploration unless necessary.



Prefer:



```text

Assets/\_Project/Scripts/Player/

```



over searching the entire repository for player-related files.



\---



\## Unity YAML Is Expensive Context



Treat these as expensive files:



```text

.unity

.prefab

.asset

.meta

```



Do not read large Unity YAML files unless their contents are directly relevant.



Do not dump entire scenes or prefabs into the context when only a small part is needed.



Do not manually construct or rewrite large Unity YAML files unless explicitly required.



Prefer:



\* C# components

\* targeted Unity Editor operations

\* Editor scripts

\* small serialized changes



when appropriate.



Do not force everything into code. Normal Unity Editor workflows remain appropriate for production content such as art, animation, materials, level layout and complex authored prefabs.



\---



\# Generated and Third-Party Files



Do not inspect or search these directories unless explicitly required:



```text

Library/

Temp/

Logs/

obj/

Build/

Builds/

UserSettings/

```



Do not modify generated files.



Do not search package caches or third-party package contents unless the task specifically concerns them.



When searching the repository, prefer project-owned directories:



```text

Assets/\_Project/

Docs/

```



and only inspect `Packages/manifest.json` when package information is required.



\---



\# Search Discipline



Avoid repository-wide `grep`, `glob` or equivalent searches unless necessary.



Prefer:



\* exact paths

\* known directories

\* specific filenames

\* specific symbols

\* targeted text searches



Avoid searching common terms across the entire Unity project.



If a search may return a large amount of data, narrow the search before executing it.



\---



\# Read Before Edit



Before changing a file:



1\. Read the relevant portion.

2\. Understand its current responsibility.

3\. Make the smallest change that solves the task.



Do not read unrelated files simply to understand the entire project.



Do not rewrite files unnecessarily.



Do not refactor surrounding systems unless required by the current task.



\---



\# Tool Output Discipline



When using tools:



\* avoid dumping large files

\* avoid listing thousands of files

\* avoid printing entire Unity scenes

\* avoid printing large YAML structures

\* avoid printing package caches

\* avoid repeating already-known information



Prefer focused output.



If only one method is relevant, inspect that method rather than the entire project.



If only one directory is relevant, search that directory rather than the repository root.



\---



\# Prefer Incremental Implementation



Do not implement five connected systems in one step merely because they are related.



For example, prefer:



```text

Movement

↓

Test

↓

Grab

↓

Test

↓

Drag

↓

Test

↓

Ragdoll

↓

Test

↓

Carry

```



over implementing all systems at once.



This makes gameplay problems easier to identify and makes AI-generated changes easier to review and revert.



\---



\# Prototype Over Engineering



Prototype 0 is an experiment.



Prefer:



\*\*working and testable > theoretically perfect\*\*



Do not build production infrastructure prematurely.



Do not implement systems merely because the final game might eventually need them.



When two solutions are valid, prefer the simpler solution that is easy to change.



\---



\# Scene and Prefab Discipline



Do not recreate scenes or prefabs unnecessarily.



Do not make large serialized changes when a small component change is sufficient.



When a prototype object can be created efficiently through an Editor utility or bootstrap code, this is acceptable.



However, do not move normal authored game content into code simply to reduce token usage.



Use the appropriate Unity workflow for the content.



\---



\# Completion Checklist



After implementing a task:



\* Check compilation or obvious syntax errors.

\* Inspect only the files changed by the task.

\* Verify important references.

\* Explain how to test the feature in Unity.

\* Report known limitations.

\* Do not perform a full project audit unless requested.



Final summaries should be concise.



Include:



```text

Changed:

\- files



Implemented:

\- short summary



Test:

\- how to verify in Unity



Known issues:

\- if any



Next:

\- recommended next small task

```



\---



\# Progress and Handoff



After a meaningful development session, update:



```text

Docs/Prototype/Progress.md

```



with:



\* completed work

\* current state

\* known problems

\* important decisions

\* next logical task



Keep it concise.



Do not record the entire chat history.



For important architectural decisions, update:



```text

Docs/Decisions.md

```



when appropriate.



\---



\# Context Handoff Between Sessions



A new coding session should be able to continue from the repository without relying on the previous chat.



At the beginning of a new session, prefer:



```text

Read:

\- AGENTS.md

\- relevant documentation

\- relevant project files



Inspect the current implementation and continue from the current project state.



Do not inspect unrelated systems.

```



Do not ask the AI to rediscover the entire project unless necessary.



At the end of a significant session, ensure durable state is written to the appropriate documentation.



\---



\# Final Principle



The AI is an implementation partner, not the game designer.



The human team decides:



\* game design

\* gameplay feel

\* priorities

\* scope

\* architecture changes

\* important tradeoffs



The AI should:



\* inspect

\* propose when appropriate

\* implement

\* test

\* explain

\* document durable decisions



When requirements are unclear, do not invent major gameplay or architectural decisions.



Choose the smallest reasonable implementation and keep the project easy to change.



