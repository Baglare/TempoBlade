<!-- knowledge-compiler-adapter-v1
{"adapter_contract":"codex-agents-v1","generated_body_sha256":"04674c6c0ae48c488c6af8deca381d5502c26728cc7422ae6ec1db778868395f","generator":"knowledge-compiler","generator_version":"adapter-compiler-v2","project_id":"tempoblade","routing_sha256":"e62c183d8c6f5695694f54653fc770f198bd90c6818255884d8bf80c892d6a58","source_structured_contract_sha256":"085cb3e09e02329131e0fc1cb5a570a25c32d685c2000ee233c30679c1edc386","target":"codex"}
-->

# Generated Codex Instructions: TempoBlade

Generated from validated `.ai/project.md` authority and automation/map routing. Do not edit by hand.

Apply every matching manifest rule using the M0 lexical scope matcher; nested guidance cannot relax root authority.

## Task operations

Read `.ai/project.md`, `.ai/automation.json` and only relevant domains from `.ai/project-map.json`. The map is routing evidence; manifest critical_rules remain structured authority. Inspect mapped files first and expand through actual dependencies.

After durable ownership, paths or validation topology changes, maintain the project map when policy.project_map permits, then run `kc adapters tree-build . --target codex` when policy.agents permits. When durable project knowledge changes and policy enables sync, author an inert autopilot plan, run `kc autopilot check` then `kc autopilot apply`. KnowledgeCompiler validates the working-tree snapshot, owner, exact preimages and transaction, records audit evidence and commits/pushes owned Vault changes according to policy. Formatting, comments, tiny refactors and temporary investigation do not require Vault updates. Ambiguity fails closed; 81 is exceptional manual fallback. 30 writing canon is excluded; 80 governance requires protected promotion. Never commit or push source code unless the user explicitly requests it.

Autopilot enabled: true. Routing domains: player-combat, enemy-encounters, room-rewards, hub-save-economy, skill-progression, content-contracts, presentation, editor-validation.

## Critical rules

- `combat-contracts` (`Assets/_Project/Scripts/Combat/**`, error): Preserve common damage/parry/deflect interfaces and physics query semantics across player, enemies and projectiles.
- `data-driven-content` (`Assets/_Project/Scripts/ScriptableObjects_Data/**`, error): Keep content variation in existing ScriptableObject contracts and preserve prefab/asset identities and references.
- `run-persistent-state-separation` (`Assets/_Project/Scripts/Managers/**`, error): Preserve run-local state ownership separately from persistent wallet/progression and save state.
- `save-legacy-compatibility` (`Assets/_Project/Scripts/Managers/SaveManager.cs`, error): Preserve supported existing save fields and progression compatibility; verify migration/round-trip behavior for persistence changes.
- `unity-editor-safe-setup` (`Assets/_Project/Scripts/Editor/**`, error): Use repeatable duplicate-safe Undo-supported setup tools; do not silently save scenes or change project packages/settings.
