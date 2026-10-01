---
{"critical_rules":[{"id":"combat-contracts","kind":"invariant","scope":"Assets/_Project/Scripts/Combat/**","severity":"error","statement":"Preserve common damage/parry/deflect interfaces and physics query semantics across player, enemies and projectiles."},{"id":"data-driven-content","kind":"invariant","scope":"Assets/_Project/Scripts/ScriptableObjects_Data/**","severity":"error","statement":"Keep content variation in existing ScriptableObject contracts and preserve prefab/asset identities and references."},{"id":"run-persistent-state-separation","kind":"invariant","scope":"Assets/_Project/Scripts/Managers/**","severity":"error","statement":"Preserve run-local state ownership separately from persistent wallet/progression and save state."},{"id":"save-legacy-compatibility","kind":"invariant","scope":"Assets/_Project/Scripts/Managers/SaveManager.cs","severity":"error","statement":"Preserve supported existing save fields and progression compatibility; verify migration/round-trip behavior for persistence changes."},{"id":"unity-editor-safe-setup","kind":"invariant","scope":"Assets/_Project/Scripts/Editor/**","severity":"error","statement":"Use repeatable duplicate-safe Undo-supported setup tools; do not silently save scenes or change project packages/settings."}],"manifest_version":1,"project_id":"tempoblade","project_name":"TempoBlade","schema":"project-ai-manifest-v1"}
---
# Purpose

Unity 6 action roguelite prototype with tempo combat, parry/deflect, room encounters, hub economy and persistent progression.

# Repository Map

- player-combat: Assets/_Project/Scripts/Player, Assets/_Project/Scripts/Combat
- enemy-encounters: Assets/_Project/Scripts/Enemy, Assets/_Project/Scripts/Environment
- room-rewards: Assets/_Project/Scripts/Core
- hub-save-economy: Assets/_Project/Scripts/Hub, Assets/_Project/Scripts/Managers, Assets/_Project/Scripts/Progression
- skill-progression: Assets/_Project/Scripts/SkillTree, Assets/_Project/Data
- content-contracts: Assets/_Project/Scripts/ScriptableObjects_Data, Assets/_Project/ScriptableObjects
- presentation: Assets/_Project/Scripts/UI, Assets/_Project/Scripts/VFX, Assets/_Project/Resources
- editor-validation: Assets/_Project/Scripts/Editor

# Architecture

Assets/_Project owns game source and data. ScriptableObjects define weapons/enemies/rooms/rewards/skills; Player and Combat components implement runtime behavior. RunManager owns run state and room transition continuity; SaveManager owns persistent JSON/progression. Managers and events connect gameplay to UI/audio/VFX. MainMenu -> Hub -> Gameplay is the documented scene flow.

# Validation Notes

Use local syntax/reference checks for changed C# and mapped manual acceptance notes. No automated game test suite is established by this rollout. Save create/load/delete/legacy compatibility and build restart persistence remain separate runtime acceptance checks; do not run Unity/build without explicit authorization.

# Sensitive Areas

Preserve legacy save/progression compatibility and run-vs-persistent state separation. Assets, prefab references and Unity .meta identities are contracts. Editor tools should support repeatable safe setup.

# Non-goals

No automatic project restructuring/assembly split or new manager layer. Roadmap intentions in Docs/PLAN.md and Docs/TASKS.md are not implemented capability evidence.
