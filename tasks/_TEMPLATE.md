# Task: <任务名称>

Task ID: <TASK-001>  
Requirement ID: <REQ-001>  
Requirement Version: <1>  
Build Revision: <1>  
Contract Digest: <optional hash or Not Recorded>  
Status: Ready to Build  
Type: Feature | Bug | Tuning | Tooling | Art | Platform  
Risk: Low | Medium | High  
Build Mode: Code Only | Art Only | Parallel Code + Art  
Preview Gate: None | Reference | Approval Required  
Technical Gate: Self Check | Fresh Review  
Art Gate: Not Required | Asset QA | Human Art Approval  
Experience Gate: None | Human Check  
Created By: PM Orchestrator  
Created At: <YYYY-MM-DD>

---

<!-- FROZEN_START -->

# Product Contract

## User Request

保留用户原始请求或忠实摘要。

## Problem

说明真正需要解决的问题。

## Player / User Outcome

完成后，玩家或开发者实际能够看到、感受到或完成什么：

- 
- 

## Core Rules and Confirmed Decisions

- 
- 

## Scope

本轮包含：

- 
- 

## Non-goals

本轮明确不处理：

- 
- 

## Constraints

必须遵守的产品、体验、平台或项目约束：

- 
- 

## Accepted Assumptions

仅记录 PM 已向用户明确展示、且不会改变核心结果的低风险假设：

- None.

## Acceptance Criteria

验收标准描述可观察结果，不只描述内部代码结构。

- AC-01：
- AC-02：
- AC-03：

<!-- FROZEN_END -->

---

<!-- VISUAL_PREVIEW_START -->

# Requirement Preview

Preview Status: Not Required | Generated | Accepted | Needs Revision | Blocked  
Preview Artifact: <path or Not Generated>  
Preview Manifest: <path or Not Generated>  
Preview Type: UI Mockup | Gameplay Storyboard | Before/After | Spatial Diagram | Scene Concept | Art Direction | Not Required

## Preview Purpose

说明预览帮助用户确认什么。

## What the Preview Illustrates

- 
- 

## Visual Assumptions

- None recorded.

## What the Preview Does Not Define

除非同时写入冻结合同，预览图不单独定义：

- 精确尺寸、数值和计时
- 状态规则和边界条件
- 输入逻辑
- 技术实现
- 最终生产资产质量

## Human Preview Decision

Status: Not Required | Accepted | Needs Revision  
Feedback: <none>

<!-- VISUAL_PREVIEW_END -->

---

# Current Implementation

Feature Designer 对真实项目的调查结果：

- Entry point:
- Owning system:
- Related code:
- Related scenes / prefabs / nodes:
- Related art and resource paths:
- Current state or data flow:
- Current asset import / binding flow:
- Confirmed current behavior:
- Confirmed gap or defect:

不要复制大量源码，只记录 Builder 能快速进入工作的事实。

---

# Design and Implementation Plan

## Recommended Design

1. 
2. 
3. 

## Main Change Areas

- 
- 

## Data / State Changes

- None.

## Compatibility Handling

- None.

## Integration Order

1. Code and art workstreams run in parallel when Build Mode permits.
2. PM reconciles both result packets.
3. Code Builder performs the final integration pass.
4. Required art and human gates run after the integrated build exists.

该部分是推荐路径，不是产品冻结区；内部路径可以调整，但共享接口不能被 Builder 私自更改。

---

<!-- EXECUTION_CONTRACT_START -->

# Parallel Build Contract

> 本区在 Builder 启动前由 PM 冻结。Code Builder 与 Art Asset Builder 均不得修改。

## Workstream Ownership

| Workstream | Required | Allowed Write Paths | Forbidden Write Paths | Result Packet |
|---|---:|---|---|---|
| Code Builder | Yes / No | | Production art paths; preview paths; Task | `.harness/runs/<task-slug>/code/code-result.json` |
| Art Asset Builder | Yes / No | | Code, scenes, engine metadata, Task | `.harness/runs/<task-slug>/art/asset-manifest.json` |
| Integration Pass | Yes / No | | Production art source files; Task | `.harness/runs/<task-slug>/integration/integration-result.json` |

Rules:

- 两个并行工作流的 Allowed Write Paths 不得重叠。
- Scene、Prefab、Node、engine import metadata 与最终绑定由 Integration Pass 单独拥有。
- 若无法划分清楚，Build Mode 不得使用 `Parallel Code + Art`。

## Code Contract

### Required Code Outcomes

- 
- 

### Allowed Placeholder Policy

- Placeholder allowed: Yes | No
- Placeholder path:
- Placeholder must be removed before final integration: Yes | No
- Placeholder behavior:

### Code Constraints

- 
- 

## Production Art Contract

Art Bible: `docs/ART_BIBLE.md`  
Approved Preview: <path or Not Required>  
Reference Assets: <paths or None>

| Asset ID | Purpose | Final Path | Source Path | Type | Dimensions / Aspect | Format | Alpha / Background | View / Composition | Style and Must-Preserve Rules | States / Frames | Import Expectations |
|---|---|---|---|---|---|---|---|---|---|---|---|
| ART-001 | | | | | | | | | | | |

For each production asset, explicitly define when relevant:

- crop, padding, safe area, pivot, or nine-slice margins
- palette, light direction, material, outline, and texture language
- frame count, sprite-sheet grid, order, and loop behavior
- exact text allowed, or `No text`
- elements that must not appear
- whether editable source is required

If Build Mode is `Code Only`, write `Not Required`.

## Code-Art Interface Contract

| Asset ID | Runtime Role | Consuming Scene / System | Binding Slot / Resource Key | Expected Final Path | Fallback | Import / Binding Owner | Blocks Integration |
|---|---|---|---|---|---|---|---:|
| ART-001 | | | | | | Code Builder | Yes |

## Shared Contract Rules

- Code Builder must reference contracted asset IDs and paths.
- Art Asset Builder must deliver exactly those asset IDs and paths.
- Neither Builder may silently rename, substitute, resize, or remove a contracted asset.
- A contract change requires PM reconciliation and an incremented Build Revision.

<!-- EXECUTION_CONTRACT_END -->

---

# Verification Plan

## Code and Technical Checks

- Build / compile:
- Tests:
- Logs / runtime checks:
- Scene / prefab / resource checks:
- Platform or performance checks:

## Asset QA Checks

- Required file and path checks:
- Dimensions and format:
- Alpha / background:
- Sprite-sheet / state checks:
- Style-family consistency:
- In-game visual checks:

If `Art Gate: Not Required`, write `Not Required`.

## Manual Playtest

- HC-01：
- HC-02：

If `Experience Gate: None`, write `Not Required`.

---

# Design Risks and Open Technical Issues

## Risks

- 

## Designer Assumptions

这些是技术或生产假设，不是新的产品需求：

- None.

## Open Technical Issues

- None.

若存在会改变 Product Contract 或 Parallel Build Contract 的问题，Task 不应进入 `Ready to Build`。

---

<!-- CODE_RESULT_START -->

# Code Build Result

> PM 在收到 Code Builder 的结构化结果后填写。Code Builder 不直接修改本区。

Status: Not Started

## Implementation Summary

Not started.

## Changed Areas

Not started.

## Baseline

| Check | Before Change | Notes |
|---|---|---|
| Git status | Not Run | |
| Build / compile | Not Run | |
| Relevant tests | Not Run | |
| Reproduction | Not Run | |

## Code Verification Evidence

### Commands Run

```text
Not Run
```

### Results

- Not Run.

## Assets Referenced

- None.

## Plan Deviations

- None.

## Failed or Not Run

- None recorded.

## Remaining Code Risks

- None recorded.

<!-- CODE_RESULT_END -->

---

<!-- ART_RESULT_START -->

# Art Build Result

> PM 在收到 Art Asset Builder 的 manifest 和结构化结果后填写。Art Asset Builder 不直接修改本区。

Status: Not Started  
Asset Manifest: Not Generated

## Assets Produced

| Asset ID | Final Path | Technical QA | Visual QA | Notes |
|---|---|---|---|---|
| | | Not Run | Not Run | |

## Production Method and References

- Not recorded.

## Technical Asset QA

- Not Run.

## Visual Assumptions or Deviations

- None recorded.

## Failed or Not Verified

- None recorded.

## Remaining Art Risks

- None recorded.

<!-- ART_RESULT_END -->

---

<!-- INTEGRATION_RESULT_START -->

# Integration Result

> PM 在收到 Code Builder 的 integration result 后填写。

Status: Not Started

## Integrated Assets and Bindings

| Asset ID | Bound To | Import / Binding Result | Evidence |
|---|---|---|---|
| | | Not Run | |

## Final Changed Areas

- Not recorded.

## Acceptance Results

| Criterion | Result | Evidence |
|---|---|---|
| AC-01 | Not Verified | |
| AC-02 | Not Verified | |
| AC-03 | Not Verified | |

Allowed results: `Pass`, `Fail`, `Not Verified`, `Not Applicable`.

## Build and Runtime Evidence

### Commands Run

```text
Not Run
```

### Runtime Checks

- Not Run.

### In-Game Captures

- None.

### Git Diff Review

- Not Run.

## Failed or Not Run

- None recorded.

## Remaining Integration Risks

- None recorded.

<!-- INTEGRATION_RESULT_END -->

---

<!-- ART_APPROVAL_START -->

# Final Art Check

Status: Not Required | Pending | Asset QA Passed | Accepted | Needs Revision | Needs Redesign

## Build Context

- Integrated build / scene:
- Screenshots / recording:
- Device / viewport:
- Relevant import settings:

## Check Items

- [ ] 资产在游戏真实尺寸下清晰可读
- [ ] 比例、裁切、边距和透明区域正确
- [ ] 颜色、材质、描边、光照与 Art Bible 一致
- [ ] 同族资产风格一致
- [ ] 没有非预期文字、logo 或多余元素
- [ ] 预览方向与最终生产结果一致

## Human Art Feedback

Not recorded.

## Decision

Pending.

<!-- ART_APPROVAL_END -->

---

<!-- REVIEW_RESULT_START -->

# Fresh Review Result

Status: Not Required | Pending | Review Passed | Changes Required | Inconclusive

## Scope Reviewed

- frozen Product Contract
- frozen Parallel Build Contract
- Code / Art / Integration Results
- relevant Git diff
- changed files and directly affected call paths

## Findings

None recorded.

## Acceptance Assessment

| Criterion | Review Result | Notes |
|---|---|---|
| AC-01 | Not Reviewed | |
| AC-02 | Not Reviewed | |
| AC-03 | Not Reviewed | |

## Final Decision

Not Reviewed.

<!-- REVIEW_RESULT_END -->

---

<!-- HUMAN_CHECK_START -->

# Human Experience Check

Status: Not Required | Pending | Accepted | Needs Tuning | Needs Redesign

## Build Context

- Build / artifact:
- Scene / level:
- Device / browser:
- Configuration:
- Relevant parameter values:

## Setup

1. 
2. 
3. 

## Check Items

### HC-01 — <检查项>

操作：

观察：

### HC-02 — <检查项>

操作：

观察：

## Human Feedback

Not recorded.

## Accepted Values

- None.

## Decision

Pending.

<!-- HUMAN_CHECK_END -->
