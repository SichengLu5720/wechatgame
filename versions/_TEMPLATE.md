# Version: <version>

Status: Planning  
Release Channel: Local | Internal | Test | Production  
Target Platform: Not Selected  
Platform Release Required: No | Yes | TBD  
Created At: <YYYY-MM-DD>  
Base Branch:  
Base Commit:  
Previous Stable Version:  
Previous Stable Commit or Tag:

---

<!-- VERSION_SCOPE_START -->

## Version Goal

说明该版本希望达到的整体产品或开发阶段结果，不要只罗列功能。

---

## Included Tasks

| Task | Current Status | Change Boundary | Integration Dependency | Notes |
|---|---|---|---|---|
| `tasks/<task>.md` | Planned | Not Recorded | None | |

规则：

- Planning 阶段可以包含未完成 Task。
- 创建 Release Candidate 前，所有 Included Tasks 必须为 `Verified`。
- 不复制 Task 的完整需求与验收标准。
- Change Boundary 填写 commit、commit range，或明确的工作区变更边界。

---

## Excluded Scope

本版本明确不包含：

- 
- 

---

## Dependencies and Integration Order

| Order | Task or System | Depends On | Reason |
|---:|---|---|---|
| 1 | | | |

没有依赖时填写：`No known ordering dependency.`

---

## Version Acceptance Criteria

只记录版本集成与交付结果，不重复所有 Task 的局部验收标准。

- VA-01：所有 Included Tasks 达到 `Verified`。
- VA-02：
- VA-03：
- VA-04：

常见内容：完整核心流程、跨 Task 兼容、目标构建、正式配置、无意外 Debug 内容、目标平台可运行、回滚点可用。

<!-- VERSION_SCOPE_END -->

---

## Scope Change Log

版本进入 `Integration` 后，任何范围变化都必须记录。

| Date | Change | Reason | Impacted Checks | Approved By |
|---|---|---|---|---|
| | | | | |

没有变化时填写：`No scope changes.`

---

# Development Progress

| Task | Status | Verification Result | Commit or Change Boundary |
|---|---|---|---|
| | | | |

## Open Development Blockers

- None.

---

# Integration Plan

## Integration Baseline

- Branch:
- Base Commit:
- Git Status:
- Existing Uncommitted Changes:
- Previous Build Result:
- Previous Test Result:
- Previous Stable Point:

## Integration Focus

本版本需要重点检查的跨功能关系：

- 
- 

## Expected Integration Risks

- 
- 

---

# Version Regression Plan

版本回归覆盖完整流程与跨功能风险，不重复每个 Task 的全部验收。

## Startup

- [ ] 应用能够正常启动
- [ ] 正确初始场景能够加载
- [ ] 启动过程没有阻断错误
- [ ] 正式配置被正确加载

## Core Flow

- [ ] 能够开始一局或进入主要功能
- [ ] 核心玩家输入正常
- [ ] 核心系统能够共同运行
- [ ] 成功路径可以达到
- [ ] 失败路径可以达到
- [ ] 重开、返回或退出流程正常

## Integration

- [ ] Included Tasks 之间没有阻断性冲突
- [ ] 共享状态与生命周期没有明显错误
- [ ] 场景、Prefab、Node、资源与序列化引用完整
- [ ] 所有生产资产均来自已验证 Task，且 asset ID、路径与绑定正确
- [ ] 纹理、图集、字体、动画或其他资源的导入设置符合 Task 合同
- [ ] 正式构建中不存在需求预览图、临时占位图或未批准资产
- [ ] 配置没有互相覆盖
- [ ] 存档或数据兼容性符合预期
- [ ] 平台条件代码没有明显异常（如适用）

## Release Configuration

- [ ] 版本号正确
- [ ] 目标构建配置正确
- [ ] 不包含非预期 Debug UI 或开发工具
- [ ] 不包含测试账号、测试凭证或开发环境地址
- [ ] 不包含非预期测试资源
- [ ] 正式依赖与 SDK 配置正确（如适用）

## Project-Specific Checks

- [ ] 
- [ ] 

---

# Integration Result

Status: Not Started

## Included Task Check

| Task | Verified | Change Boundary Confirmed | Integration Notes |
|---|---|---|---|
| | | | |

## Regression Results

| Check | Result | Evidence |
|---|---|---|
| Startup | Not Run | |
| Core Flow | Not Run | |
| Integration | Not Run | |
| Release Configuration | Not Run | |
| Project-Specific | Not Run | |

允许结果：`Pass`、`Fail`、`Not Verified`、`Not Applicable`。

## Commands and Procedures Run

```text
Not Run
```

## Runtime Evidence

- Logs:
- Screenshots:
- Recordings:
- Test scenes:
- Device or environment:

## Integration Findings

- None.

## Required Fixes

- None.

## Remaining Risks

- None.

---

# Release Candidate

Status: Not Created

- RC Identifier:
- Version Number:
- Release Channel:
- Target Platform:
- Branch:
- Commit:
- Tag:
- Build Configuration:
- Build Command:
- Engine Version:
- SDK / Toolchain Version:
- Build Identifier:
- Artifact Path:
- Artifact Checksum:
- Created At:
- Created By:

## Release Candidate Build Result

Result: Not Run

```text
Build output or concise result.
```

## Release Candidate Validation

| Item | Result | Evidence |
|---|---|---|
| Artifact exists | Not Verified | |
| Artifact matches recorded commit | Not Verified | |
| Version number is correct | Not Verified | |
| Build configuration is correct | Not Verified | |
| No blocking build errors | Not Verified | |
| Required smoke test passes | Not Verified | |

任何代码、场景、资源、配置、依赖或构建设置变化都会使当前 RC 失效，必须返回 Integration 并生成新 RC。

---

# Human Release Check

Status: Pending | Accepted | Rejected | Not Required

## Build Context

- RC Identifier:
- Artifact:
- Target Environment:
- Device / Browser:
- Configuration:

## Check Items

- [ ] 版本内容与 Version Goal 一致
- [ ] 核心流程能够完整运行
- [ ] 操作与主要体验没有阻断问题
- [ ] 画面、动画与声音没有明显异常
- [ ] 最终生产美术在目标设备与真实尺寸下清晰、一致且无错误裁切
- [ ] 已知问题处于可接受范围
- [ ] RC 可以进入目标发布渠道

## Human Feedback

尚未填写。

## Decision

Pending.

---

# Platform Release Procedure

Status: Not Required | Not Integrated | Draft | Verified

- Target Platform:
- Reusable Playbook:
- Official Documentation Verified At:
- Relevant SDK / Toolchain Version:

## Account and Access Prerequisites

首次接入目标平台时，根据当时最新官方文档填写。微信体验版发布填写 `docs/WECHAT_EXPERIENCE_RELEASE_WORKFLOW.md`，并且只能在全部 Check 已确认、Human Release Check 已接受、Release Decision 已批准后执行 Git 推送与平台上传。

## Project Integration Requirements

平台 SDK、代码、配置与构建适配应通过单独的 Feature Task 完成。

## Build and Packaging

暂时留空。

## Signing and Credentials

暂时留空。不得在本文件中写入密钥、Token、证书内容或其他凭证。

## Test or Sandbox Distribution

暂时留空。

## Submission and Review

暂时留空。

## Rollout and Publication

暂时留空。

## Platform Rollback or Hotfix

暂时留空。

## Platform Verification Result

Not Verified.

---

# Known Release Issues

| Issue | Severity | Player Impact | Accepted | Mitigation |
|---|---|---|---|---|
| | | | | |

没有已知问题时填写：`No known release issues.`

---

# Rollback Plan

- Previous Stable Version:
- Previous Stable Commit or Tag:
- Rollback Artifact:
- Save or Data Compatibility:
- Irreversible Migration: No | Yes | Unknown

## Rollback Procedure

1. 
2. 
3. 

## Rollback Limitations

- None.

## Rollback Approval Required

- 

---

# Release Decision

Status: Pending | Approved | Rejected

- Approved Version:
- Approved RC:
- Approved Commit:
- Approved Artifact:
- Approved Target:
- Approved By:
- Approved At:

## Decision Notes

尚未填写。

外部上传、提交、发布、正式 rollout 与生产回滚必须获得明确人工授权。

---

# Release Result

Status: Not Released | Released | Failed | Rolled Back

- Actual Version:
- Release Channel:
- Target Platform:
- Released RC:
- Released Commit:
- Released Tag:
- Released Artifact:
- Released At:
- Released By:

## Release Actions

- 

## Release Evidence

- Platform build ID:
- Submission ID:
- Review result:
- Release console reference:
- Other evidence:

未接入平台时可留空。

---

# Post-Release Observation

Status: Not Started | Observing | Stable | Issue Detected | Closed

- Observation Window:
- Observed Environment:
- Responsible Person or Agent:

## Checks

- [ ] 启动与加载正常
- [ ] 没有新增阻断错误
- [ ] 核心流程可用
- [ ] 平台接口正常（如适用）
- [ ] 广告、支付或账号系统正常（如适用）
- [ ] 关键行为数据无明显异常（如适用）

## Findings

- None.

## Decision

Continue | Hotfix Required | Rollback Required | Observation Complete

## Follow-up Tasks

- None.
