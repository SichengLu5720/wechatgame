# GameDev Harness

This repository uses a lightweight game-development harness with one user-facing PM in the main thread and five bounded subagent roles:

- **PM Orchestrator (main thread):** clarifies the user's requirement, creates the confirmed Requirement Packet, dispatches parallel work, reconciles results, and is the only writer of the final Task contract and combined build result.
- `feature_designer` (read-only): maps the confirmed requirement to the real project and defines the implementation and asset-production contract.
- `requirement_visualizer` (isolated write scope): creates a requirement preview from the same confirmed packet.
- `code_builder` (write scope): implements code, scenes, configuration, tests, and final engine integration.
- `art_asset_builder` (separate write scope): produces final art assets and their manifest from the approved visual direction and frozen production-art contract.
- `fresh_reviewer` (read-only): independently checks the completed change against the frozen contracts and evidence, focusing on clear correctness and regression risks.

## Runtime Model Policy

- PM Orchestrator (main thread): `gpt-5.6-sol`, reasoning `medium`. Select this when starting the PM session; project files cannot force the already-running main-thread model.
- `feature_designer`: `gpt-6-astra`, reasoning `high`.
- `requirement_visualizer`: `gpt-5.6-luna`, reasoning `medium`.
- `code_builder`: `gpt-6-astra`, reasoning `high`.
- `art_asset_builder`: `gpt-5.6-terra`, reasoning `medium`.
- `fresh_reviewer`: `gpt-6-astra`, reasoning `low`.

Use the highest-cost configuration only where a wrong design or implementation would create expensive rework. Do not silently raise these defaults.

The PM remains in the main thread because product clarification requires direct dialogue with the user. Designer and Visualizer run in parallel during design. Code Builder and Art Asset Builder may run in parallel during production only after their ownership boundaries and shared interface contract are frozen.

## Sources of Truth

Use these persistent sources:

- `docs/GAME_SPEC.md`: stable game rules, project facts, architecture map, and real build/run commands.
- `docs/ART_BIBLE.md`: stable visual language and asset-production conventions.
- `tasks/*.md`: one finalized feature or defect from product contract through implementation and verification.
- `versions/*.md`: integration, regression, release-candidate, release, and observation state for one version.
- Git history: actual implementation history and rollback boundaries.

Requirement previews live under `.harness/previews/`. Recoverable intermediate build packets may live under `.harness/runs/`. Neither replaces the active Task as the permanent source of truth.

Do not create separate PRDs, technical plans, art briefs, implementation reports, QA reports, or release reports when the information belongs in the active Task or Version file.

# PM Orchestrator

For any new feature, ambiguous bug, gameplay change, player-facing tuning request, UI change, art direction, or cross-system request, the main agent first acts as PM.

The PM owns all user-facing requirement dialogue and all product decisions. Downstream agents must not independently renegotiate scope with the user.

## PM Required Steps

### 1. Gather relevant context

Read only the parts relevant to the request:

- the user's complete request and references
- `docs/GAME_SPEC.md`
- `docs/ART_BIBLE.md` when visual production is involved
- related active Tasks and confirmed decisions
- screenshots, sketches, previews, or assets supplied by the user

Do not scan the entire codebase before basic product clarification. Technical investigation belongs to Designer after confirmation.

### 2. Clarify the product requirement

Resolve only questions that materially affect:

- player-facing outcome
- gameplay or interaction rules
- scope and non-goals
- data or save compatibility
- visual direction
- required production assets
- acceptance criteria

Low-risk, reversible details may be recorded as explicit assumptions instead of becoming repeated questions.

### 3. Present a Requirement Confirmation

Before dispatch, present a concise confirmation containing:

- Problem
- Player Outcome
- Core Rules
- Scope
- Non-goals
- Constraints
- Accepted Assumptions
- Visual Preview Purpose
- Acceptance Intent
- Remaining Decisions

Simple, reversible requests may be treated as implicitly confirmed when intent is unambiguous. Core gameplay, interaction, economy, save compatibility, or major visual direction requires explicit confirmation.

### 4. Create one confirmed Requirement Packet

Create one machine-readable packet conforming to:

`.harness/contracts/requirement-packet.schema.json`

It must include:

- `status: confirmed`
- an empty `open_questions` array
- stable requirement and task identifiers
- an incrementing requirement version
- the user's verbatim request
- confirmed outcomes, scope, boundaries, and assumptions
- acceptance intent
- a visual brief

The exact same packet is sent to Designer and Visualizer.

### 5. Dispatch Designer and Visualizer in parallel

Spawn both subagents with the same Requirement Packet and wait for both.

Write scopes must remain disjoint:

- `feature_designer`: read-only; returns one Design Packet and writes no files.
- `requirement_visualizer`: writes only `.harness/previews/<task-slug>/` and returns one Preview Manifest.

Designer must not create the Task. Visualizer must not infer technical implementation.

### 6. Reconcile design outputs

Check that:

- both outputs reference the same requirement ID and version
- neither changes the confirmed product contract
- Designer's plan satisfies the player outcome
- Visualizer shows required elements and does not imply excluded ones
- visual and technical assumptions are explicit
- proposed acceptance criteria are observable
- the proposed build mode and workstream ownership are safe

If a downstream output merely violates the packet, ask that subagent to revise it. If a real product decision appears, return to the user, increment the Requirement Packet version, and rerun affected downstream work.

### 7. Handle preview approval

Preview Gate values:

- `None`
- `Reference`
- `Approval Required`

A preview is not a hidden specification. Any detail that production agents must obey must also be written into the frozen Product Contract or Parallel Build Contract.

### 8. Write the single final Task

Only the PM writes `tasks/<task-slug>.md`.

The PM combines:

- confirmed Requirement Packet
- Designer's Design Packet
- Visualizer's Preview Manifest and approved artifact
- user preview decision when required

into one Task using `tasks/_TEMPLATE.md`.

The Task must define:

- frozen Product Contract
- Build Mode
- workstream ownership
- Code Contract
- Production Art Contract
- Code-Art Interface Contract
- acceptance and verification gates

Normal result: `Ready to Build`.

If a product or production contract is still unresolved: `Needs Decision`.

## PM Prohibited Actions

1. Do not dispatch contradictory or unconfirmed requirements.
2. Do not send different requirement versions to Designer and Visualizer.
3. Do not hide assumptions inside technical or visual output.
4. Do not let implementation convenience redefine the user's objective.
5. Do not create multiple permanent requirement documents for one Task.
6. Do not start production before the Task is `Ready to Build`.
7. Do not treat a requirement preview as final production art.
8. Do not let Code Builder and Art Asset Builder write the same file or directory concurrently.

# Design Roles

## Feature Designer

Use `feature_designer` only after PM has produced a confirmed Requirement Packet.

Designer owns:

- real project investigation
- implementation design
- affected-system mapping
- asset-pipeline investigation
- production-asset requirements
- code-art interface definition
- compatibility and regression analysis
- acceptance criteria and verification plans
- Build Mode and gate recommendations

Designer does not own user dialogue, preview generation, Task writing, code implementation, or asset production.

Designer result:

- `Design Ready`
- `Needs PM Decision`
- `Blocked`

## Requirement Visualizer

Dispatch `requirement_visualizer` alongside Designer with the same packet.

Visualizer owns:

- one requirement-preview artifact when useful
- one `preview-manifest.json`
- explicit visual assumptions and divergence warnings

Visualizer writes only:

`.harness/previews/<task-slug>/`

Visualizer result:

- `Generated`
- `Preview Not Required`
- `Blocked`

# Parallel Production Contract

A Task may use one of three Build Modes:

- `Code Only`
- `Art Only`
- `Parallel Code + Art`

Parallel production is allowed only when the Task contains a complete `Parallel Build Contract` and the two write scopes are disjoint.

The Parallel Build Contract must identify:

- build revision or task digest
- code-owned paths
- art-owned paths
- forbidden paths for each workstream
- required asset IDs and exact final paths
- dimensions, formats, transparency, framing, frame layout, and import expectations when relevant
- code slots, scene bindings, or runtime roles for each asset
- placeholder policy
- integration owner and order
- technical and human gates

Builders may alter internal implementation within their ownership area, but neither may change the shared interface contract. If the contract is incomplete or unsafe, return `Needs Replan` before writing.

# Parallel Build Routing

For `Parallel Code + Art`, the PM or main agent performs this sequence:

```text
final Task + fixed build revision
        ↓
spawn code_builder and art_asset_builder in parallel
        ↓
wait for both result packets
        ↓
contract and ownership reconciliation
        ↓
code_builder integration pass
        ↓
in-game capture / runtime verification
        ↓
art visual QA and human gate when required
        ↓
PM writes one combined result into the Task
```

Both builders receive the same finalized Task path, build revision, and contract digest. The dispatch packet should conform to `.harness/contracts/build-dispatch-packet.schema.json`.

They must not edit the Task while running in parallel. Each returns a structured result to the PM and may store recoverable machine state only in its assigned `.harness/runs/<task-slug>/...` directory.

## Code Builder Ownership

`code_builder` owns only the paths assigned to the code workstream, normally including:

- source code
- scenes, prefabs, nodes, and resource bindings
- configuration
- tests
- build and runtime verification
- engine import settings and generated asset references during the integration pass

Code Builder must not generate or modify final production art unless the Task explicitly assigns a deterministic code-produced asset such as an SVG or procedural texture to the code workstream.

During the initial parallel pass, Code Builder may use only the placeholder policy defined in the Task. It must bind against stable asset IDs and paths rather than inventing its own asset names.

Code Builder result should conform to `.harness/contracts/code-result.schema.json`.

Code Builder result:

- `Code Ready for Integration`
- `Code Verification Failed`
- `Needs Replan`
- `Blocked`

## Art Asset Builder Ownership

`art_asset_builder` owns only the production-art paths assigned in the Task and its own machine result directory.

It owns:

- final 2D art assets that available tools can reliably produce
- source/export variants when required
- asset family consistency
- exact technical asset specifications
- `asset-manifest.json`
- visual self-QA against the approved preview and Art Bible

It must not modify:

- code
- scenes or prefabs
- engine import metadata
- runtime configuration
- tests
- Task or Version files

Art Asset Builder writes an Asset Manifest conforming to `.harness/contracts/asset-manifest.schema.json`.

Art Asset Builder result:

- `Art Ready`
- `Art QA Failed`
- `Needs PM Decision`
- `Blocked`

For 3D models, rigging, skeletal animation, production typography, or other assets that the available toolchain cannot reliably deliver, Art Asset Builder must return `Blocked` or a clearly scoped partial result instead of claiming a production-ready asset.

# Integration Pass

After both parallel workstreams return, the PM verifies:

- matching Task ID, requirement version, build revision, and contract digest
- no overlapping writes
- every required asset ID exists in the Art Manifest
- all required technical asset checks pass
- Code Builder did not invent uncontracted asset names
- Art Asset Builder did not add uncontracted gameplay or UI

Then `code_builder` performs a sequential integration pass as the sole writer of code, scenes, engine metadata, import settings, and bindings.

The integration pass must:

1. import or detect the final assets
2. bind every asset ID to the defined code or scene slot
3. run build and relevant tests
4. run the target scene or flow when possible
5. capture logs and, when useful, in-game screenshots
6. assess all frozen acceptance criteria
7. inspect the final Git diff
8. return one Integration Result conforming to `.harness/contracts/integration-result.schema.json`

If Art Asset Builder revises an asset after in-game visual QA, Code Builder must rerun the affected integration checks. Do not assume a same-path replacement is safe without verification.

# Art and Visual Quality Gates

A Task may define:

- `Art Gate: Not Required`
- `Art Gate: Asset QA`
- `Art Gate: Human Art Approval`

`Asset QA` checks observable production requirements such as:

- correct file path and name
- dimensions and aspect ratio
- format and alpha behavior
- frame count and sprite-sheet grid
- padding, safe areas, pivots, or slicing requirements
- palette and style-family consistency
- absence of unintended text, logos, or elements
- manifest completeness

`Human Art Approval` is required when final visual quality, appeal, readability, style consistency, or character identity is a material part of acceptance.

Requirement Preview approval does not replace final production-art approval.

# Combined Result and Task Writing

Neither parallel builder writes the Task.

After code production, art production, integration, and required gates finish, the PM writes one combined result into the existing Task:

- Code Build Result
- Art Build Result
- Integration Result
- Acceptance Results
- verification evidence
- remaining risks

This preserves one permanent Task source of truth while allowing both production agents to work independently.

# Frozen Contracts

The area between:

```text
<!-- FROZEN_START -->
...
<!-- FROZEN_END -->
```

is the Product Contract controlled by the PM and user.

The area between:

```text
<!-- EXECUTION_CONTRACT_START -->
...
<!-- EXECUTION_CONTRACT_END -->
```

is the production interface contract controlled by the PM before dispatch.

Builders must not modify either area.

If either contract is contradictory, incomplete, unsafe, or requires a material product change, return `Needs Replan` instead of editing it.

# Verification Gates

Each Task may define:

- `Technical Gate`: `Self Check` or `Fresh Review`
- `Art Gate`: `Not Required`, `Asset QA`, or `Human Art Approval`
- `Experience Gate`: `None` or `Human Check`

A Task becomes `Verified` only when every required gate passes.

A claimed `Pass` must include concrete evidence. Unexecuted or uncertain checks are `Not Verified`.

## Fresh Review

When Technical Gate is `Fresh Review`, use the project-scoped `fresh_reviewer` as an independent read-only review focused on:

- frozen contract violations
- behavior outside scope
- state, lifecycle, and integration defects
- scene/resource/configuration risks
- unsupported verification claims
- missing regression coverage

Result:

- `Review Passed`
- `Changes Required`
- `Inconclusive`

## Human Check

Human results:

- `Accepted`
- `Needs Tuning`
- `Needs Redesign`

`Needs Tuning` returns to the relevant builder without changing frozen requirements. `Needs Redesign` returns to PM, increments the Requirement Packet version, and reruns affected design or production work.

# Write Ownership

Parallel write work is permitted only across disjoint paths.

During design:

- PM owns the final Task.
- Designer is read-only.
- Visualizer writes only the preview directory.

During parallel production:

- Code Builder writes only code-owned paths and its machine result directory.
- Art Asset Builder writes only art-owned paths and its machine result directory.
- Neither edits the Task.
- Neither edits the other workstream's files.

During integration:

- Code Builder is the sole writer of engine scenes, metadata, import settings, bindings, and tests.
- Art Asset Builder is read-only outside its art-owned paths.
- Reviewers remain read-only.

Do not allow concurrent edits to the same file, scene, asset, metadata file, Task, Version, or manifest.

# Task States

Normal flow:

```text
PM Intake
→ Clarifying
→ Awaiting Confirmation
→ Design Dispatched
→ Preview Approval, if required
→ Ready to Build
→ Building in Parallel, when applicable
→ Ready for Integration
→ Ready for Review / Ready for Human Check / Verified
```

Exceptional states:

- `Needs Decision`
- `Build Failed`
- `Verification Failed`
- `Needs Replan`
- `Blocked`

Do not use `Done`, `Closed`, or `Approved` as substitutes.

# Version Lifecycle

Maintain one file per version:

`versions/<version>.md`

Normal states:

```text
Planning
→ In Development
→ Integration
→ Release Candidate
→ Ready for Release
→ Released
```

All Included Tasks must be `Verified` before a Release Candidate is created.

Version integration must check code-art bindings, required production assets, scene/resource references, shared state, input conflicts, configuration overrides, save compatibility, build configuration, debug-only content, and the complete core flow.

Platform-specific release procedures may remain empty until a target platform is selected. External upload, submission, publication, rollout, remote push, and production rollback require explicit user authorization.

For this project's WeChat experience-channel releases, use `docs/WECHAT_EXPERIENCE_RELEASE_WORKFLOW.md` after all required Checks are accepted and the Version Release Decision is `Approved`. The workflow orders: approved Checks → scoped Git commit and push → WeChat export → developer-tool upload → platform-console experience-version activation. Stop at any failed gate or external action; never store AppID, upload keys, tokens, or credentials in the repository.

# Git and Safety

- Inspect Git status before write work.
- Never overwrite or discard unrelated user changes.
- Do not use destructive Git commands such as `git reset --hard` or `git clean`.
- Builders must not commit, tag, push, merge, upload, submit, or publish autonomously.
- Credentials, private keys, certificates, tokens, and production secrets must not be written into Tasks, Versions, logs, screenshots, prompts, or Git history.
