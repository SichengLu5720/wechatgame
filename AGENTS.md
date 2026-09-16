# GameDev Harness

This repository uses a lightweight game-development harness built around two project-scoped Codex agents:

- `feature_designer`: turns an idea, feedback item, tuning request, or non-trivial bug into one executable Task.
- `feature_builder`: implements one approved Task and verifies it against frozen acceptance criteria.

The harness optimizes for agent autonomy while preserving product intent, traceability, rollback safety, and human control over subjective experience and external release actions.

## Sources of Truth

Use only these persistent sources:

- `docs/GAME_SPEC.md`: stable game rules, project facts, architecture map, and known build/run commands.
- `tasks/*.md`: one feature or defect from design through implementation and verification.
- `versions/*.md`: integration, regression, release-candidate, release, and observation state for one version.
- Git history: actual code history and rollback boundaries.

Do not create separate PRDs, technical plans, implementation reports, QA reports, or release reports when the same information belongs in the active Task or Version file.

Chat history may provide intent, but it is not the only source of truth for persistent project state.

## Working Principle

Use the minimum process needed for the risk of the change.

- Low-risk, explicit changes may be implemented directly.
- Ambiguous, cross-system, player-facing, or repeatedly reworked changes should first become a Task through `feature_designer`.
- Each Task should have one writer during implementation.
- Internal implementation details may change; frozen player-facing outcomes may not.

## Feature Design Routing

Use `feature_designer` before implementation when any of the following applies:

- a new feature or gameplay mechanic
- a change to player behavior, controls, rules, state flow, win/loss, progression, or economy
- a tuning request whose real cause or implementation location is unclear
- a change spanning multiple systems, scenes, resources, or files
- a bug whose reproduction, expected behavior, or ownership is unclear
- a request that has already caused rework or regression
- a feature requiring subjective experience goals or explicit acceptance criteria

The following may skip Designer when they are explicit, low-risk, and reversible:

- a text replacement
- a known configuration-value change
- a simple color or asset swap with no behavior change
- a localized bug with a confirmed cause and expected result
- comments, formatting, or documentation-only edits

Designer must create or reuse exactly one `tasks/<task-slug>.md` file and return either:

- `Ready to Build`
- `Needs Decision`

If Designer returns `Needs Decision`, do not begin implementation until the blocking product decision is resolved.

## Feature Build Routing

Use `feature_builder` only for a Task whose status is `Ready to Build`.

Provide one Task at a time. The Task is the primary requirement source.

Builder must:

1. inspect the actual implementation and establish a pre-change baseline
2. implement the smallest complete change that satisfies the Task
3. run the real checks available in the project
4. assess every frozen acceptance criterion with evidence
5. inspect the final Git diff for unrelated changes
6. update only the Task status and its Builder Result area

Builder may adjust the internal implementation plan when the real code differs from the Designer's map, but it must record material deviations.

Builder must not create duplicate implementation or QA documents.

## Frozen Requirements

The area between:

```text
<!-- FROZEN_START -->
...
<!-- FROZEN_END -->
```

is controlled by Designer and the user.

Builder must not delete, weaken, reinterpret, or modify this area.

If the frozen requirements are contradictory, unsafe, impossible with the real project, or require a material product change, Builder returns `Needs Replan` instead of editing them.

A claimed acceptance `Pass` must include concrete evidence. An unexecuted or uncertain check is `Not Verified`, not `Pass`.

## Task States

Normal flow:

```text
Needs Decision
→ Ready to Build
→ Ready for Review / Ready for Human Check / Verified
```

Exceptional states:

- `Verification Failed`: implementation exists but required acceptance failed.
- `Needs Replan`: product result, scope, or acceptance criteria require redesign.
- `Blocked`: an environment, permission, asset, dependency, or external condition prevents progress.

Do not use `Done`, `Closed`, or `Approved` as substitutes for these states.

## Verification Gates

Each Task defines two independent gates:

- `Technical Gate`: `Self Check` or `Fresh Review`
- `Experience Gate`: `None` or `Human Check`

### Self Check

Builder may return `Verified` only when all required technical criteria pass, the Task uses `Self Check`, no subjective experience decision remains, and there are no unexplained failures or unrelated changes.

### Fresh Review

When `Technical Gate` is `Fresh Review`, Builder stops at `Ready for Review`.

Use an independent read-only review, including Codex `/review` when available. Review only:

- the Task frozen requirements
- Builder Result
- the relevant Git diff
- changed files and directly affected call paths

Prioritize:

- correctness defects
- acceptance-criteria violations
- player-visible behavior outside scope
- state and lifecycle errors
- scene, prefab, node, serialized-field, resource, and configuration-reference risks
- data or save compatibility risks
- unsupported verification claims
- meaningful regressions

Do not block on style-only preferences or unrelated refactoring suggestions.

Review result must be one of:

- `Review Passed`
- `Changes Required`
- `Inconclusive`

### Human Check

When `Experience Gate` is `Human Check`, technical success is not final acceptance.

Builder must provide a short, reproducible playtest checklist for the user.

Human result:

- `Accepted`: Task may become `Verified` after all technical gates pass.
- `Needs Tuning`: return to Builder without changing frozen requirements.
- `Needs Redesign`: return to Designer because the desired result or interaction must change.

Subjective judgments such as feel, readability, fun, pacing, animation naturalness, vibration strength, audiovisual cohesion, and difficulty require human acceptance unless the user explicitly waives it.

## Single-Writer Rule

Only one write-capable agent should modify the working tree for the active Task at a time.

Designer may write only the Task file it creates or updates.

Builder may write project implementation files plus the active Task's Builder Result and status.

Reviewers should remain read-only.

Do not allow concurrent agents to edit the same code, scene, asset, configuration, Task, or Version file.

## Version Lifecycle

A Version is the integration and delivery unit above individual Tasks. Maintain one file:

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

Exceptional states:

- `Changes Required`
- `Blocked`
- `Rejected`
- `Rolled Back`

A Release Candidate may contain only Included Tasks whose final status is `Verified`.

At version integration:

1. record branch, base commit, working-tree state, and previous stable point
2. confirm the change boundary for each Included Task
3. verify cross-Task interactions rather than repeating every Task test
4. run a repeatable core-flow smoke test appropriate to the project's maturity
5. check production configuration, scenes/resources, and absence of unintended debug/test content
6. create an immutable Release Candidate tied to an exact commit and artifact
7. complete required technical and human release gates
8. record release, observation, hotfix, or rollback outcomes in the same Version file

Any code, scene, asset, dependency, configuration, or build-setting change invalidates the current Release Candidate and requires a new one.

Do not silently change frozen Version scope after Integration begins. Record the scope change and rerun affected checks.

## Platform Release Procedures

Platform-specific release steps may remain blank until a target platform is selected.

At first integration of a platform:

- research the current official platform documentation
- record the verification date and relevant SDK/toolchain versions
- route SDK, code, build, or configuration adaptation through a normal Task
- record the reusable platform release procedure only after it has been validated with a real test or release build

Do not guess current platform requirements from memory.

External account creation, legal acceptance, credential changes, production signing, upload, submission, rollout, publication, and production rollback require explicit user authorization.

Never store secrets, tokens, private keys, signing credentials, or certificates in source files, Task files, Version files, logs, screenshots, or Git history.

## Git and User Work

Before changing the project, inspect `git status` when Git is available.

Never overwrite, discard, or silently absorb unrelated user changes.

Do not use destructive operations such as:

- `git reset --hard`
- `git clean -fd`
- forced checkout over user changes
- forced push

Designer and Builder must not commit, tag, push, merge, publish, or release unless the user explicitly authorizes that action.

After a Task is `Verified`, the main agent or user may create an atomic commit containing only that Task's intended changes.

## Project Commands

Use only commands confirmed by the repository, `docs/GAME_SPEC.md`, package configuration, engine configuration, or direct inspection.

Do not invent build, test, export, or platform commands.

If no automated check exists, provide a reproducible manual check and mark automation as unavailable rather than fabricating it.

## Project-Wide Prohibitions

1. Do not change product intent to fit the current architecture.
2. Do not expand a Task into unrelated features, refactors, dependency upgrades, or migrations.
3. Do not hide failed builds, failed tests, warnings, runtime errors, missing evidence, or unverified behavior.
4. Do not make a test pass by weakening valid assertions, skipping failures, enlarging tolerances without justification, or redefining faulty behavior as expected.
5. Do not modify generated caches, import caches, temporary build artifacts, or unrelated engine-managed files unless the active Task explicitly requires it.
6. Do not claim a Task or Version is verified when a required gate has not passed.
7. Do not perform external side-effect actions without explicit user authorization.
8. Do not create duplicate documents for information already owned by the active Task, Version, GAME_SPEC, or Git.

## Maintaining This Harness

Keep this file focused on durable routing, boundaries, and completion rules.

Do not add a new rule after a single minor mistake. Add or revise rules when a failure pattern repeats or when a missing boundary creates material risk.
