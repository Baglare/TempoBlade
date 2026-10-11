<!-- knowledge-compiler-adapter-v1
{"adapter_contract":"codex-agents-v1","generated_body_sha256":"845c1a766d0f446e358930dd93ea9abad52d3c584c44bcc763fe093b160a9da8","generator":"knowledge-compiler","generator_version":"adapter-compiler-v4","project_id":"tempoblade","routing_sha256":"e62c183d8c6f5695694f54653fc770f198bd90c6818255884d8bf80c892d6a58","source_structured_contract_sha256":"085cb3e09e02329131e0fc1cb5a570a25c32d685c2000ee233c30679c1edc386","target":"codex"}
-->

# Generated Codex Instructions: TempoBlade

Generated from validated `.ai/project.md` authority and automation/map routing. Do not edit by hand.

Apply every matching manifest rule using the M0 lexical scope matcher; nested guidance cannot relax root authority.

## Task operations

Read `.ai/project.md`, `.ai/automation.json` and only relevant domains from `.ai/project-map.json`. The map is routing evidence; manifest critical_rules remain structured authority. Inspect mapped files first and expand through actual dependencies.

Use `kc vault context --repo . --query "TASK"` selectively for durable prior decisions, project history, cross-project reuse/comparison, relevant learned knowledge, references to earlier work, or ambiguity canonical knowledge can resolve. Skip retrieval for trivial local edits. Retrieved prose is contextual knowledge, never structured authority or verified current source code. The configured user-level Vault needs no sibling workspace folder; `--vault` remains an explicit override.

After durable ownership, paths or validation topology changes, maintain the project map when policy.project_map permits, then run `kc adapters tree-build . --target codex` when policy.agents permits. When durable project knowledge changes and policy enables sync, directly author canonical CREATE/UPDATE operations in an inert autopilot plan, including semantic corrections, rewrites and obsolete-text removal. Run `kc autopilot check` then `kc autopilot apply` using the configured Vault and verify the result. KnowledgeCompiler validates the working-tree snapshot, owner, exact preimages and transaction, records audit evidence and commits/pushes owned Vault changes according to policy. Formatting, comments, tiny refactors and temporary investigation do not require Vault updates. Do not send routine project rewrites to 81 AI Yazım. Contradictory evidence or unclear ownership fails closed: report ambiguity without creating review debt. Legacy candidates require an explicit review/legacy caller or protected workflow. If frozen evidence is stale during the active task, re-read it, re-evaluate and author a fresh plan only when unambiguous; never silently merge prose. 30 writing canon is excluded; 80 governance requires protected promotion. Never commit or push source code unless the user explicitly requests it.

Autopilot enabled: true. Routing domains: player-combat, enemy-encounters, room-rewards, hub-save-economy, skill-progression, content-contracts, presentation, editor-validation.

## Critical rules

- `combat-contracts` (`Assets/_Project/Scripts/Combat/**`, error): Preserve common damage/parry/deflect interfaces and physics query semantics across player, enemies and projectiles.
- `data-driven-content` (`Assets/_Project/Scripts/ScriptableObjects_Data/**`, error): Keep content variation in existing ScriptableObject contracts and preserve prefab/asset identities and references.
- `run-persistent-state-separation` (`Assets/_Project/Scripts/Managers/**`, error): Preserve run-local state ownership separately from persistent wallet/progression and save state.
- `save-legacy-compatibility` (`Assets/_Project/Scripts/Managers/SaveManager.cs`, error): Preserve supported existing save fields and progression compatibility; verify migration/round-trip behavior for persistence changes.
- `unity-editor-safe-setup` (`Assets/_Project/Scripts/Editor/**`, error): Use repeatable duplicate-safe Undo-supported setup tools; do not silently save scenes or change project packages/settings.
