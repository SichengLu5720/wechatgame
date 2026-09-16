# Task: <任务名称>

Task ID: <TASK-001>  
Status: Draft  
Type: Feature | Bug | Tuning | Tooling | Art | Platform  
Risk: Low | Medium | High  
Technical Gate: Self Check | Fresh Review  
Experience Gate: None | Human Check  
Created By: Feature Designer  
Created At: <YYYY-MM-DD>

---

<!-- FROZEN_START -->

## Goal and Player Outcome

### Problem

说明当前真正需要解决的问题。

### Expected Outcome

说明完成后，玩家或开发者实际能够看到、感受到或完成什么。

---

## Scope and Boundaries

### Scope

本轮包含：

- 
- 

### Non-goals

本轮明确不处理：

- 
- 

### Constraints

必须遵守的规则、体验目标或项目限制：

- 
- 

---

## Acceptance Criteria

验收标准应描述可观察结果，而不是只描述内部代码结构。

- AC-01：
- AC-02：
- AC-03：

<!-- FROZEN_END -->

---

## Current Implementation

Designer 对真实项目的调查结果。

- Entry point:
- Owning system:
- Related files / scenes / resources:
- Current state or data flow:
- Confirmed current behavior:
- Confirmed defect or gap:

不要复制大量源代码，只记录 Builder 能快速进入工作的事实。

---

## Design and Implementation Plan

推荐实施路径：

1. 
2. 
3. 

### Main Change Areas

- 
- 

### Data / State Changes

- 

### Compatibility Handling

- 

该部分是推荐路径，不是冻结区域。Builder 可以在不改变冻结结果的前提下调整内部实现，但必须记录重要偏差。

---

## Verification Plan

### Automated or Technical Checks

只填写项目中真实存在或能够明确执行的方式。

- Build / compile:
- Tests:
- Logs / runtime checks:
- Scene / resource checks:
- Other:

### Manual Playtest

只保留需要人工判断的内容。

- HC-01：
- HC-02：

若 `Experience Gate: None`，填写 `Not Required`。

---

## Assumptions and Risks

### Confirmed Facts

- 

### Assumptions

- 

### Risks

- 

### Open Questions

- None.

若存在会显著改变产品结果的阻断性问题，本 Task 不应进入 `Ready to Build`。

---

<!-- BUILDER_RESULT_START -->

# Builder Result

Status: Not Started

## Implementation Summary

尚未执行。

## Changed Areas

尚未执行。

## Plan Deviations

None.

## Baseline

| Check | Before Change | Notes |
|---|---|---|
| Git status | Not Run | |
| Build / compile | Not Run | |
| Relevant tests | Not Run | |
| Reproduction | Not Run | |

## Acceptance Results

| Criterion | Result | Evidence |
|---|---|---|
| AC-01 | Not Verified | |
| AC-02 | Not Verified | |
| AC-03 | Not Verified | |

允许结果：`Pass`、`Fail`、`Not Verified`、`Not Applicable`。

## Verification Evidence

### Commands Run

```text
Not Run
```

### Runtime Checks

Not Run.

### Scene / Resource / Configuration Checks

Not Run.

### Git Diff Review

Not Run.

## Failed or Not Run

- None recorded.

## Manual Check Required

- Not evaluated.

## Remaining Risks

- None recorded.

<!-- BUILDER_RESULT_END -->

---

<!-- REVIEW_RESULT_START -->

# Fresh Review Result

Status: Not Required | Pending | Review Passed | Changes Required | Inconclusive

## Scope Reviewed

- Task frozen requirements
- Builder Result
- Relevant Git diff
- Changed files and directly affected call paths

## Findings

None recorded.

每个 Finding 建议包含：

- Finding ID
- Blocking / Non-blocking
- Related Acceptance Criterion
- Evidence
- Impact
- Required Outcome

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

# Human Check

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

尚未填写。

## Accepted Values

- None.

## Decision

Pending.

<!-- HUMAN_CHECK_END -->
