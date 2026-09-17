# Task: 移动中完成人群集合

Task ID: TASK-007  
Task Version: 2  
Status: Ready for Human Check  
Harness Mode: Strict  
Type: Feature  
Risk: Medium  
Build Mode: Code Only  
Preview Mode: Implementation  
Technical Gate: Fresh Review  
Art Gate: Not Required  
Experience Gate: Human Check  
Created By: PM Orchestrator  
Created At: 2026-09-17

---

# Clarifications and Decisions

| ID | Question / Unclear Item | Confirmed Decision | Confirmed By | Task Version Impact | Affected Work |
|---|---|---|---|---|---|
| CL-001 | 人群应先集合再整体移动，还是在前进中形成队形 | 改为从原位置直接出发，在沿路线前进的过程中逐渐集合；不得出现全员先到中间停顿、再整体出发的分段感 | User | None | 移动轨迹、启动时序、队形过渡 |
| CL-002 | 目标平台已有角色是否为到达角色重新让位 | 不让位；平台原有角色在本次移动中保持原位，到达角色直接匹配并补入现有空位，以减少平台内二次移动和视觉负担 | User | None | 最终槽位分配、目标平台入场、轨迹数量 |
| CL-003 | 本次是否把当前正式移动的拥挤容许度升级为严格角色间距 | 沿用当前正式版本的拥挤容许度；本次只要求不新增明显穿模，不扩大为严格间距路径规划重构 | User | None | 轨迹安全标准、性能与验证范围 |
| CL-004 | 中转平台人数变化时，居民不动与旧版最终座位无法同时保持，应优先哪一项 | 以人物稳定为优先：保留现有人物当前位置，新到人物补入最近的可用位置；允许中转平台最终视觉队形与旧版不同，但逻辑人数、顺序、路径和玩法结果不变 | User | None | 中转平台槽位语义、最终视觉队形、撤销 |
| CL-005 | 旧版 4/8/12/16 人中转布局互不嵌套，无法从所有旧初始布局稳定补满 16 人 | 中转平台统一使用固定 16 个安全位置；初始 4/8/12 人使用居中确定性子集，后续人物补剩余空位。允许初始中转视觉队形相对旧版轻微变化 | User | v2 | 中转初始队形、Reset、稳定槽位候选库 |

未解决的阻断问题：

- None.

---

<!-- FROZEN_START -->

# Frozen Requirement

## Original Request

“我想要调整移动视觉效果，现在是集合到中间再进行整体移动，我想达到在移动过程中集合的效果。”

## Problem

当前多人移动先在来源区域集合到中间，再以整体队形沿路线移动，形成明显的两段式停顿，削弱流动感。

## Goal and Player / User Outcome

- 人群点击移动后立即从各自位置开始向目标方向前进。
- 不停止主行进，在移动途中自然收束为可通过楼梯和平台的队形。
- 到达时准确落入合法空位，移动过程清楚、顺滑且不新增明显穿模。
- 目标平台原有人物不因新人物到达而重新排位；新到人物直接补入空位。

## Core Rules and Confirmed Decisions

- 汇聚与沿路线前进必须重叠发生，不得先完成原地/中心集合再开始整体位移。
- 逻辑移动、人数、路径连通、平台容量、撤销和胜负结果不变；中转平台的最终视觉槽位允许因居民不动而与旧版不同。
- 允许不同角色存在轻微错峰启动，以避免碰撞，但整体观感必须是同一波向前流动，不能重新形成等待全部集合的阶段。
- 本次移动开始前已在目标平台上的角色保持原位置和槽位，不生成让位轨迹；到达角色只能使用未占用的合法槽位。
- 中转平台使用固定 16 槽安全布局；4/8/12 人初始状态使用其居中确定性子集，人数变化时仅补空位或移出对应人物，其他居民不重排。

## Scope

本轮包含：

- 普通关卡、每日挑战和教程共用的多人移动轨迹与播放时序。
- 来源平台离场、楼梯/平台路线中的队形收束，以及目标平台入场落位。
- 目标平台“居民不动、来者补位”的槽位分配与入场轨迹。
- 与当前角色行走动画和移动速度调节的同步兼容。

## Non-goals

- 不修改关卡、导航拓扑、移动规则、容量或解法。
- 不修改角色模型、美术资源、镜头、UI、颜色或平台尺寸。
- 不新增玩家操作。

## Constraints

- 角色脚底全程位于真实可行走表面，沿用当前正式版本的拥挤容许度，不新增明显穿模、不越过不可行走区域。
- 最终逻辑队列状态与现有实现完全一致；视觉 `actorSlots` 可按居民稳定规则变化；撤销、重置、连续追加移动、隐藏/揭示、失败和胜利流程正确。
- 目标平台不让位规则必须适用于普通平台与中转平台；若现有槽位布局无法在不移动居民的前提下安全补位，应由设计明确安全分配方案，不得静默恢复整个平台重排。
- 允许固定 16 槽方案导致中转平台 4/8/12 人初始视觉队形相对旧版轻微变化；Reset 恢复新固定布局的确定性初始子集。
- 不破坏 TASK-003 行走动画与 TASK-006 实际移动/步态同步倍率。
- 高人数关卡不得引入明显卡顿或每帧垃圾分配。

## Accepted Assumptions

- “移动过程中集合”指离开初始座位后，在朝路径出口及后续路线持续前进时逐渐收束；不是取消队形约束。

## Acceptance Criteria

| ID | Type | Criterion | Verification Owner | Required Evidence |
|---|---|---|---|---|
| AC-F-01 | Functional | 4/8/12/16 人移动均从原位直接进入前进行程，并在持续前进中完成队形收束，无“先到中心等待、再整体出发”阶段 | Builder / Reviewer | 轨迹采样、连续截图或录像 |
| AC-F-02 | Functional | 棋盘队列状态、移动人数、撤销、重置、连续移动和胜负结果与修改前一致；仅中转平台视觉槽位允许按居民稳定规则发生预期变化 | Builder / Reviewer | 自动化对比与核心流程回归 |
| AC-F-03 | Functional | 每次移动前已在目标平台上的角色全程保持原位；到达角色直接占用最近的合法空位，平台上不出现为其让位的二次移动；中转平台最终视觉队形允许相应变化 | Builder / Reviewer | 居民轨迹为零、槽位占用与连续帧验证 |
| AC-T-01 | Technical | 所有角色全程得到行走面支撑并沿用当前正式版本的拥挤容许度；上下楼梯、转向和平台边缘不新增明显穿模或悬空 | Builder / Reviewer | 修改前后连续轨迹安全对比 |
| AC-T-02 | Technical | 兼容角色步态和速度倍率，高人数场景无新增每帧分配或明显性能回退 | Builder / Reviewer | 构建、运行时与性能对比 |
| AC-E-01 | Experiential | 相比修改前，移动观感更连续、更像一波人流在前进中成队，且不会因过度错峰显得逐个排队 | Human | 同路径前后对比试玩 |

## Test Proxies

| Proxy ID | Proxy | Supports Criterion | Limitation |
|---|---|---|---|
| TP-001 | 角色开始沿目标方向产生位移的时间，与其进入收束队形的时间区间重叠 | AC-E-01 | 不能独立证明整体观感自然 |
| TP-002 | 全体启动时间跨度和路径进度分布受限 | AC-E-01 | 不能替代真人对节奏的判断 |

<!-- FROZEN_END -->

---

# Design

## Current Implementation

- 实际链路为 `StairsGame.TryMoveSync()` → `FastMovement.Build()` → `MotionComposer.Append()` → `ActorTrack.PlaybackPosition()`。
- `FastMovement` 当前让旅行者先接入来源平台中心附近的窄列，再沿楼梯路线前进，中心必经点造成明显的先集合后出发观感。
- 普通平台的现有排布通常能保持目标居民位置；中转平台 `Seat()` 随 4/8/12/16 人切换布局，人数变化会重排居民。
- `State.actorSlots` 已由 Clone、Board 历史和 Undo 深拷贝，可承载稳定表现位置。
- 当前正式入口允许紧密重叠；旧 `MotionPlanner` 的严格间距不是本任务运行时合同。

## Recommended Design

1. 保留 `FastMovement` 和现有 `MotionPlan / ActorTrack` 接口，把来源中心必经点改成沿出口方向推进的汇合段；前进与横向收束同步，并对离散折线验证平台、接驳面和楼梯覆盖。
2. 为中转平台引入固定 16 槽安全布局及与人数无关的稳定表现位置 ID；4/8/12 人使用居中确定性子集。复用 `actorSlots`、提交和 Undo；普通平台继续使用原有槽位语义。
3. 锁定目标平台全部居民的稳定位置，再从经验证的候选位置中为到达者执行确定性批量匹配；按末段距离优化，并以位置 ID、角色 ID 破同分。
4. 合法候选必须有地面支撑、未占用、可达且不新增明显身体穿模。持续补入、部分移出和再次补入必须可达容量 16；不得以移动居民或拒绝原本合法操作兜底。
5. `MotionPlan.finalSlots` 提交完整分配；Board 校验稳定 ID 的范围与唯一性，Undo 恢复精确位置，Reset 恢复初始布局；不新增 PlayerPrefs、存档或关卡字段。
6. 连续追加移动以已预约终点为占用依据；仍在上一笔行程中的角色继续原轨迹。保留 TASK-003 实际位移步态和 TASK-006 运动时钟倍率。

## Main Change Areas

- `FastMovement.cs` 与新增队形辅助：途中收束、末段入场、稳定分配接入。
- `WalkSpace.cs`：稳定位置解析与统一 `SeatFor()`。
- `TransitSlots.cs` 或新分配器：居民锁定、候选空位和确定性批量匹配。
- `CrowdRules.cs`：仅表现槽位兼容与校验，不改变规则队列语义。
- 必要时最小修改 `CrowdMotion.cs` / `MotionComposer.cs`，并新增 TASK-007 专属验证。

## Technical Options

- None.

## Proposed Product Decisions

- None.

## Workstream Ownership

| Workstream | Required | Allowed Write Paths | Forbidden Paths / Shared Files |
|---|---:|---|---|
| Code Builder | Yes | `Assets/Scripts/Runtime/FastMovement.cs`、`WalkSpace.cs`、`TransitSlots.cs`、必要的 `CrowdRules.cs` / `CrowdMotion.cs` / `MotionComposer.cs`、TASK-007 专属验证与构建配置 | Task、正式美术资源、关卡数据、UI、镜头、导航几何常量、SDK；尽量不修改 TASK-006 所有的 `StairsGame.cs` / `CharacterWalkDriver.cs` |
| Art Asset Builder | No | None | 全部项目文件 |
| Final Integration | No | None | 全部项目文件 |

## Code–Art Interface

Not Required.

---

# Visual Direction

Preview Status: Pending Approval  
Latest Approved Preview: Not Required

## Accepted Visual Decisions

- 汇聚和主路径前进重叠发生。
- 不出现全员先到中心等待的分段。

## Rejected Directions

- 先集合到平台中间，再整体移动。

## Open Visual Decisions

- None.

---

# Regression Plan

## Protected Behaviors

- RB-01：移动规则、最终落位、路径、容量、撤销/重置和胜负不变。
- RB-02：角色行走动画、速度调节、每日挑战计时与输入行为不变。
- RB-03：目标平台已有角色保持可读的稳定位置，隐藏/揭示和完成状态仍正确。

## Impacted Systems

- 移动规划、轨迹播放、队形槽位、角色动画时序。

## Baseline Checks

- 记录 4/8/12/16 人典型移动的启动、汇聚、进入楼梯和到达轨迹。
- 记录相同动作的最终状态、安全间距和运行性能。

## Targeted Regression Checks

- 来源平台离场、跨楼梯、转向、目标平台落位、连续追加、撤销和重置。
- 目标平台已有 4/8/12 人等不同占用量时，居民零位移、来者安全补位。
- 普通关卡、教程、每日挑战及高人数关卡。

## Core Smoke Path

1. 执行一次多人跨楼梯移动并观察途中成队。
2. 连续追加移动，随后撤销和重置。
3. 完成一关并验证结算时序。

## Visual Regression Checks

- 同机位、同动作前后连续帧对比。

## Known Pre-existing Issues

- None known.

---

# Build and Verification Results

## Code Result

Status: Integration Ready

- Implementation Summary: 删除来源中心必经集合阶段，改为从原位向出口前进时持续收束；中转平台使用固定 16 槽及居中初始子集，居民不让位，到达者确定性匹配最近空位；修正台阶缓存高度切换边界误差。
- Changed Areas: `FastMovement.cs`、`TransitSlots.cs`、`WalkSpace.cs`、`CrowdRules.cs`、`CrowdMotion.cs` 及 TASK-007 专属验证。
- Baseline Result: 69 例、966 步；旧版记录到 1,300 条居民让位轨迹，并存在 7 个台阶边界高度差异采样点。
- Commands / Tests Run: TASK-007 修改前后矩阵、Continuations、Windows Build、Player Probe、UnityBuild.Verify、TASK-006 tuning、TASK-003 性能代理与 scoped diff check。
- Plan Deviations: 台阶边界精确高度校正为范围内最小修复；不改变导航几何与玩法。
- Waiting for Art / Integration: None.
- Remaining Risks: 微信真机表现与性能、途中成队自然度和拥挤观感需人工验收。
- Bugfix: 修复第 8 关开发版视觉自检异常后 `HiddenArtVerification.Active` 未释放、导致点击可提交但动画时钟不再推进的问题；Disable/Destroy 同时停止自检协程并幂等释放锁，异常日志和测试失败退出码继续保留。

## Art Result

Status: Not Required

## Integration Result

Status: Ready for Human Check

- Integration Summary: TASK-003 步态与 TASK-006 速度倍率接口保持兼容；连续追加、撤销、重置和居民稳定补位已集成。
- Build / Runtime Result: Windows Build 成功；69 例、966 步、3,500,722 次脚底采样通过；752 次精确边界采样 0 误差、0 分配。
- Scene / Resource Binding Result: 无新资产或场景绑定。
- Core Smoke Result: 终局 Key 与基线一致；连续追加 10,425 项、TASK-006 46,845 项及 Player 77 张连续画面通过。
- Git Diff Review: 本轮 scoped `git diff --check` 通过；未 Commit、Push 或上传。
- Remaining Risks: Human Check 与微信真机尚未执行。
- Bugfix Build: `Builds/Task007Bugfix/Windows/StairsCrowd.exe`；第 8 关 41 项真实指针/故障注入/Disable/Destroy 验证通过。

## Acceptance Results

| Criterion | Result | Evidence | Owner |
|---|---|---|---|
| AC-F-01 | Pass | 最终矩阵 7,120 次前向出发，启动跨度最大 0.1614 秒；Player 连续画面 | Builder / Reviewer |
| AC-F-02 | Pass | 69 例终局 Key 与步数一致；连续追加、Undo、Reset 通过 | Builder / Reviewer |
| AC-F-03 | Pass | 目标居民轨迹 1,300 → 0；固定 16 槽补位与 Player 连续画面通过 | Builder / Reviewer |
| AC-T-01 | Pass | 3,500,722 次脚底采样与 752 次边界采样 0 高度误差；未采用严格 1.09 标准 | Builder / Reviewer |
| AC-T-02 | Pass | TASK-003/006 兼容、0 播放分配；桌面性能代理无明显回退 | Builder / Reviewer |
| AC-E-01 | Pending Human Check | | Human |

## Regression Result

Overall Result: No New Regression Found

| Finding | Classification | Evidence | Resolution |
|---|---|---|---|
| 途中收束、居民零让位、固定中转初始布局 | Expected Change | `artifacts/task007/final/formation.json`、Player 连续画面 | 按冻结需求保留 |
| 首轮直连楼梯外端切过接驳边角 | Introduced | 首轮 after 失败记录 | 改为出口内侧衔接点，完整复测通过 |
| 台阶浮点边界高度误差 | Pre-existing | 修改前 7 个差异点 | 边界精确采样修正，752 次专项与构建通过 |
| 已覆盖的逻辑队列、终局、容量、Undo/Reset | No Difference | 前后 69 例对比、Continuations | 无需处理 |
| HiddenArt 自检异常后遗留全局 Active 锁，游戏可选中但动画停止 | Introduced | `level8-before/pointer.json`：故障后 clock 增量 0、Active 泄漏 | 生命周期释放与协程取消；`level8-after/pointer.json` 41 项通过，故障后 clock 正常推进 |
| HiddenArt `question is still breathing` 像素断言失败 | Pre-existing | 修改前后日志均复现 | 未改美术或放宽阈值；修复后仍显式退出 1，但不再冻结游戏 |

## Fresh Review

Status: Review Passed

- Scope Reviewed: TASK-007 v2 核心实现、稳定槽位、边界修复、矩阵/Player/兼容/性能证据与 scoped diff。
- Findings: 未发现可确认的 P0–P3 缺陷；Technical Gate Pass。
- Required Action: 保留 Human Check；最小角色距离基线接近 0，不能替代人眼对明显穿模和整体流动感的判断。
- Bugfix Review: 第 8 关锁泄漏修复 Review Pass；已关闭 Disable 后协程继续运行的 P2，错误未被吞掉。

## Human Art Approval

Status: Not Required

## Human Experience Check

Status: Pending

- Test Setup: 使用 `Builds/Task007/StairsCrowd.exe` 或后续微信测试版，对比多人出发、途中成队、12 人居民补位、上下楼梯和连续移动。
- Feedback: Pending.
- Accepted Values: Pending.

---

# Final Decision

Status: Ready for Human Check

## Task Version History

| Version | Date | Change | Reason | Invalidated Outputs |
|---:|---|---|---|---|
| 1 | 2026-09-17 | Initial draft | 调整多人移动为途中成队 | None |
| 2 | 2026-09-17 | 中转平台改用固定 16 槽安全布局及居中子集 | 旧人数布局互不嵌套，无法同时满足居民不动与补满容量 | v1 中转候选库与初始轮廓方案 |
