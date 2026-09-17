# Task: 每日挑战

Task ID: TASK-002  
Task Version: 1  
Status: Verified  
Harness Mode: Lean  
Type: Feature  
Risk: High  
Build Mode: Code Only  
Preview Mode: Requirement  
Technical Gate: Fresh Review  
Art Gate: Not Required  
Experience Gate: Human Check  
Created By: PM Orchestrator  
Created At: 2026-09-16

---

# Clarifications and Decisions

| ID | Question / Unclear Item | Confirmed Decision | Confirmed By | Task Version Impact | Affected Work |
|---|---|---|---|---|---|
| CL-001 | 倒计时归零后的结果 | 直接按本关失败处理 | User | None | 计时、失败流程、验收 |
| CL-002 | 同日重试规则 | 同一天保持同一关；重试从 3:00 重新开始 | User | None | 每日选择、重试 |
| CL-003 | “更难”的比较基准 | 以已调试的前 14 关为基准；第 15 关及之后未经调试，不用于难度验收 | User | None | 关卡候选筛选、对比试玩、AC-E-01 |
| CL-004 | 每日挑战完成后的日历 | 增加日历功能，用户提供的参考图仅作为布局与状态表达参考 | User | None | 胜利流程、本地完成记录、日历 UI |
| CL-005 | 日历入口、完成状态与重玩 | 打开每日挑战时先弹出日历；完成当天挑战后在日历中显示薄荷色勾选；日历仅展示完成记录，不允许补玩或重玩 | User | None | 首页入口、日历状态、本地存储、日期选择 |
| CL-006 | 每日挑战奖励与胜利操作 | 不加入金币或其他奖励；完成后只显示“返回主页”，不提供“再次挑战” | User | None | 胜利面板、日历 UI、Non-goals |
| CL-007 | 日历月份范围 | 只显示当前月份，不能查看历史月份 | User | None | 日历 UI、历史记录展示 |
| CL-008 | 三分钟计时开始点 | 玩家第一次在关卡内做出交互操作时开始 | User | None | 计时状态机、入场流程、边界测试 |
| CL-009 | 首次交互触发范围与暂停 | 第一次点击人物或平台，或选择道具时启动计时；点击空白处、打开设置或拖动手势不启动；启动后在设置界面和应用后台期间暂停 | User | None | 输入入口、计时状态机、暂停与后台测试 |
| CL-010 | 视角拖动与计时 | 保持当前视角拖动禁用；从每日挑战的计时触发中删除视角拖动，单纯拖动手势不启动计时 | User | None | 输入触发、ViewOrbit 回归 |
| CL-011 | 每日挑战跨本地零点 | 已开始的当前局继续；成功时勾选开始挑战的日期；失败后旧日期不可重试，返回日历后只能进入新一天 | User | None | 会话日期、完成记录、失败重试 |

未解决的阻断问题：

- None.

---

<!-- FROZEN_START -->

# Frozen Requirement

## Original Request

“接下来加入每日挑战，在冒险模式的正下方。难度大于挑战关，上方加入3分钟的计时。”后续确认：倒计时归零时直接按本关失败处理；每日挑战完成后增加日历功能，参考用户提供的日历界面图。

## Problem

当前首页只有冒险进度入口，缺少独立的高难、限时每日游玩目标。

## Goal and Player / User Outcome

- 玩家可从首页冒险模式入口正下方进入每日挑战。
- 玩家在同一天重复进入时面对同一关，并拥有独立的 3 分钟挑战计时。
- 倒计时归零时明确失败；每日模式不改变冒险模式进度。

## Core Rules and Confirmed Decisions

- 每日挑战入口位于冒险模式入口正下方。
- 每日挑战的整体体验难度高于已调试的前 14 关；第 15 关及之后不作为比较基准。
- 每次尝试的计时为 3:00，显示在游戏画面上方。
- 3:00 在玩家第一次在关卡内做出交互操作时开始；入场和纯观察期不扣时。
- 第一次点击人物或平台，或选择道具时启动计时；空白点击、打开设置和拖动手势不启动。
- 计时启动后，设置界面和应用后台期间暂停。
- 跨本地零点时，当前局不中断；成功记录归属挑战开始日，失败后过期日期不得重试。
- 倒计时归零时按本关失败处理。
- 同一天固定同一关；重试重新开始 3:00。
- 从首页打开每日挑战时先进入日历，仅当天且尚未完成的挑战可开始。
- 完成当天挑战后，日历使用薄荷色勾选记录完成；已完成日期和历史日期不能补玩或重玩。
- 日历只显示当前月份，不提供历史月份查看或月份切换。
- 每日挑战完成面板只提供“返回主页”。

## Scope

- 首页每日挑战入口。
- 每日关卡的稳定选择或生成。
- 三分钟计时、超时失败、重试和返回主页流程。
- 每日挑战完成日历与本地完成状态。
- 冒险进度隔离与针对性验证。

## Non-goals

- 不增加关卡选择 icon。
- 不增加账号、云端排行榜、奖励或跨设备同步。
- 不保存每一步。
- 不制作新的生产美术资产。
- 不改变冒险模式已有进度、失败条件和关卡内容。

## Constraints

- 沿用现有 Unity IMGUI 和本地离线结构。
- 中文 UI，保持深青灰夜空、暖米白、柔和珊瑚和薄荷色的现有视觉语言。
- 不在运行时使用求解器。
- 主观难度必须保留 Human Check，不能由单一技术指标代替。

## Accepted Assumptions

- 每日切换按玩家本机当地日期计算；属于离线、可逆实现假设。

## Acceptance Criteria

| ID | Type | Criterion | Verification Owner | Required Evidence |
|---|---|---|---|---|
| AC-F-01 | Functional | 首页冒险入口正下方显示可用的每日挑战入口 | Builder | 运行截图与入口流程检查 |
| AC-F-02 | Functional | 同一本地日期重复进入获得同一关，重试恢复相同关卡并从 3:00 重新计时 | Builder | 自动化日期与重试验证 |
| AC-F-03 | Functional | 未完成时倒计时归零会锁定操作并进入现有失败流程 | Builder / Reviewer | 运行时边界测试 |
| AC-F-04 | Functional | 每日胜负、重试与返回主页均不改变冒险进度 | Builder / Reviewer | 存档隔离测试 |
| AC-F-05 | Functional | 从首页打开每日挑战时先显示日历，仅当天未完成状态可进入挑战 | Builder / Reviewer | 日期注入与入口流程测试 |
| AC-F-06 | Functional | 完成后在本机持久化当日薄荷色勾选；历史和已完成日期不可补玩或重玩 | Builder / Reviewer | 跨进程日历记录与重玩阻止测试 |
| AC-F-07 | Functional | 每日挑战完成面板仅显示“返回主页”，不显示奖励、下一关或再次挑战 | Builder / Reviewer | 运行时结算流程与截图 |
| AC-F-08 | Functional | 日历仅展示当前月份，不显示或提供历史月份导航 | Builder / Reviewer | 日期边界测试与运行截图 |
| AC-F-09 | Functional | 入场、观察、空白点击、设置和拖动保持未启动 3:00；人物/平台有效点击或可用道具选择启动计时 | Builder / Reviewer | 输入矩阵自动化验证 |
| AC-F-10 | Functional | 设置和应用后台分别暂停计时，重叠时只有全部暂停原因解除后恢复，不补扣后台时长 | Builder / Reviewer | 可控时钟与暂停组合测试 |
| AC-F-11 | Functional | 跨日保持当前局；成功勾选会话开始日；过期失败和设置重开不得重试旧日 | Builder / Reviewer | 注入日期的跨日/跨月验证 |
| AC-F-12 | Functional | 截止前的逻辑胜利优先；零秒后新棋盘操作不能提交，超时锁定保持到重试或离开 | Builder / Reviewer | 零秒竞态与动画收尾测试 |
| AC-T-01 | Technical | 每日关卡无需运行时求解器，且具有离线验证的合法完成路径 | Builder / Reviewer | 数据验证与见证回放 |
| AC-T-02 | Technical | 日历计算覆盖闰年、月末、年末与六行月份，开始前重新核对日期和完成记录 | Builder / Reviewer | 日期边界自动化验证 |
| AC-T-03 | Technical | 重启后完成记录保持且所有重玩入口受限；测试存储不改动真实冒险进度 | Builder / Reviewer | 隔离 PlayerPrefs 的持久化测试 |
| AC-E-01 | Experiential | 实际试玩中，包含三分钟限制的每日挑战整体难度高于已调试的前 14 关，且难度不来自可读性、性能或操作问题 | Human | 与前 14 关的首次对比试玩 |
| AC-E-02 | Experiential | 首页入口和顶部计时在竖屏安全区内清晰且不遮挡关键玩法 | Human | 目标设备截图与试玩 |

## Test Proxies

| Proxy ID | Proxy | Supports Criterion | Limitation |
|---|---|---|---|
| TP-001 | 初始混色、解法步数、搜索访问量等指标不低于前 14 关的高难候选基线 | AC-E-01 | 只能筛选候选，不能独立证明玩家主观感受更难 |

<!-- FROZEN_END -->

---

# Design

## Current Implementation

- 真实首页入口由 `CloudInterface.cs` 绘制，当前主按钮为“继续第 N 关”；`StairsGame.OnGUI()` 调用 `DrawCloudGUI()`。
- 难度对比仅使用已调试的前 14 关；第 15 关及之后虽有现有数据，但未经调试，不用于本 Task 的难度结论。
- 现有 `FailureFlow` 已支持无合法操作/只剩重复操作失败；`CampaignProgress` 是冒险进度唯一存储路径。
- 顶部 HUD 由 `SettingsInterface.DrawPropHUD()` 绘制，关卡名、步数、设置和道具提示已共享顶部空间。
- 旧 Harness 生成的设计文本因混入未确认产品规则，不作为本 Task 的 Design Handoff。

## Recommended Design

1. 增加独立的每日模式会话标识，不借用冒险索引或自定义关卡标识模拟。
2. 使用离线生成、验证并打包的每日候选关卡池；按本地日期键和固定算法选关，运行时不调用求解器。
3. 新建独立的 `DailyChallengeProgress`，按 `yyyy-MM` 使用版本化 PlayerPrefs 完成位集；只有逻辑胜利写入，写入幂等。
4. 首页每日入口先打开 IMGUI 日历模态层。日历在每次绘制和执行前重新核对本地日期，只显示当月；当日未完成才提供“挑战今日”。
5. 每日会话在进入关卡时固定挑战日期、关卡身份、初始局面与计时状态；重试同时恢复棋盘、步数、道具、失败历史和未启动的 3:00。
6. 计时与现有动画 `clock` 分离，使用可注入的单调时间源和“未启动/运行/暂停/胜利/失败”状态。人物/平台有效点击或已启用道具选择触发；拖动、空白点击和设置不触发。
7. 设置与应用后台使用独立暂停原因，任一存在即暂停；恢复时不补扣后台时长。
8. 超时通过单一入口锁定操作并复用失败流程，终态不被后续失败分类清除。截止前已逻辑完成的胜利不得在动画收尾期间被改判。
9. 跨零点保留已开始会话；胜利写入会话开始日，失败后所有重试/设置重开入口都必须拒绝过期日期并返回新一天日历。
10. 冒险进度记录、下一关预热、教程跳转和胜利结算显式排除每日模式；完成后只提供返回主页。

## Main Change Areas

- `StairsGame.cs`、`CloudInterface.cs`、`SettingsInterface.cs`、`FailureFlow.cs`。
- `PropInterface.cs`、`NightPresentation.cs`、`ScenePreparation.cs`、`TutorialFlow.cs`、`IslandEditor.cs`、`EditorEntrance.cs`、`ViewOrbit.cs`、`WeChatPlatform.cs` 中必要的模式生命周期、输入与暂停守卫。
- 独立每日关卡数据、导航数据、`Assets/Editor/` 中的离线验证入口与 Player 验证入口。

## Technical Options

- None recorded.

## Proposed Product Decisions

- None.

## Workstream Ownership

| Workstream | Required | Allowed Write Paths | Forbidden Paths / Shared Files |
|---|---:|---|---|
| Code Builder | Yes | `Assets/Scripts/`、`Assets/Editor/` 中本 Task 的验证入口、每日关卡所需的 `Assets/Resources/`、必要的 Unity `.meta` | Task、Harness 配置、生产美术路径、现有 campaign/tutorial 内容 |
| Art Asset Builder | No | None | 全部项目文件 |
| Final Integration | Code Builder | 与 Code Builder 相同 | Task、生产美术源文件 |

## Code–Art Interface

Not Required.

## Asset Contract

Not Required.

---

# Visual Direction

Preview Status: Integrated Visual QA Passed  
Latest Approved Preview: `.harness/previews/TASK-002/r002/qa-manifest.json`

## Accepted Visual Decisions

- 每日挑战位于冒险模式正下方。
- 计时显示在游戏画面上方。
- 不使用关卡选择 icon；保持现有项目色调。

## Rejected Directions

- 预览中的装饰标题“冒险之旅”不是需求内容，不进入实现。
- r001 的“星屿拼图”品牌、狐狸角色、重新设计的世界场景及额外装饰文案不属于本项目，不得沿用。
- 不再使用生成式图片直接绘制日历文字、日期网格和按钮；下一版使用真实游戏画面与确定性 UI 排版。
- r002/r003 的低保真截图拼版未达到交付质量，不作为实现或视觉批准依据。

## Open Visual Decisions

- 实施前无真实每日挑战 UI 可用于项目内截图预览；r001 仅作信息结构参考。实施后必须由同一 Visual Agent 执行 Integrated Visual QA，覆盖首页、日历、顶部 3:00 计时与完成状态。
- 日历参考图可借鉴月份标题、7 列日期网格、今日高亮、完成打勾和关闭操作；不默认引入金币、连续天数或里程碑奖励。

## Preview History

| Revision | Type | Artifact | Decision | Feedback / Changes |
|---:|---|---|---|---|
| legacy-r001 | Requirement | `.harness/previews/daily-challenge/requirement-preview.png` | Pending | v0.4 生成；迁移保留，不含正式生产资产规格 |
| r001 | Requirement | `.harness/previews/TASK-002/r001/requirement-preview.png` | Reference Only | 保留已确认的信息层级和状态参考；品牌、角色、重绘场景与装饰不进入实现。真实 UI 视觉验收延后至集成后。 |
| r002 | Integrated Visual QA | `.harness/previews/TASK-002/r002/qa-manifest.json` | Passed | 基于 r3 真实 390×844 与 700×1000 截图；首页、日历、计时、超时、完成、六行月份和闰日布局通过 |
| r002 | Visual Iteration | `.harness/previews/TASK-002/r002/requirement-preview.png` | Needs Revision | 真实截图方向正确，但仍是低保真线框，窗口边框、粗糙块面与说明文字混入界面，不合格。 |
| r003 | Visual Iteration | `.harness/previews/TASK-002/r003/requirement-preview.png` | Needs Revision | 高保真迭代失败，日历、HUD 和完成弹窗的层级、面板与排版均不足，不合格。 |

---

# Regression Plan

## Protected Behaviors

- RB-01：冒险模式本地进度、首页续关和已有失败流程保持不变。
- RB-02：撤销、重置、设置、编辑器及自定义关卡流程保持不变。

## Impacted Systems

- 首页、模式会话、关卡数据、HUD、失败/胜利结算、冒险进度、场景预热与缓存。

## Baseline Checks

- Unity 验证与 Windows 构建基线。
- 当前首页、冒险开始、失败、重试和返回主页流程。
- 当前冒险进度存取。

## Targeted Regression Checks

- 同日重新进入、重启进程和重试选择同一关。
- 倒计时起点、暂停/后台、零秒、移动中超时和截止前逻辑胜利。
- 每日胜负、重试、返回首页、切换冒险/编辑器均不改冒险进度。
- 每日关卡池的数据、见证、失败分类、实际移动规划与无道具完整回放。
- 每日 → 首页 → 冒险 → 编辑器/自定义 → 首页 → 每日的跨模式状态隔离。

## Core Smoke Path

1. 启动并进入首页。
2. 分别进入冒险模式和每日挑战。
3. 验证每日胜负、超时、重试、返回主页及冒险进度隔离。

## Visual Regression Checks

- 竖屏安全区、首页双入口层级、顶部计时与玩法 HUD 不重叠。

## Known Pre-existing Issues

- `ProjectSettings/ProjectSettings.asset` 当前已有未提交状态；不属于本 Task。
- `TextToolDatas/` 当前为未跟踪目录；不属于本 Task。

---

# Build and Verification Results

## Code Result

Status: Implemented

- 新增独立每日会话、当前月日历、稳定日期选关、完成记录、禁止重玩、3:00 计时、暂停原因、超时终态和跨日处理。
- 每日池包含 7 局离线验证关卡，共 192 步见证；运行时不调用求解器。
- Windows 可玩构建：`Builds/Task002/Windows/StairsCrowd.exe`。

## Art Result

Status: Not Required

## Integration Result

Status: Verified

- 最终 `UnityBuild.Verify` 通过。
- Windows 构建通过，无 C# 编译 warning/error。
- 同一 r5 Player 二进制连续两次各 181 项断言通过；独立进程持久化 3 项通过。
- Fresh Review：桌面 Technical Gate Pass。
- Integrated Visual QA：Pass。

## Acceptance Results

| Criterion | Result | Evidence | Owner |
|---|---|---|---|
| AC-F-01 | Pass | `artifacts/task002/player-r5-repeat/home-390x844.png`; Player r5 | Builder / Reviewer |
| AC-F-02 | Pass | `artifacts/task002/player-r5-repeat/runtime.json` | Builder / Reviewer |
| AC-F-03 | Pass | Player r5 超时、零秒与输入锁定断言 | Builder / Reviewer |
| AC-F-04 | Pass | Player r5 冒险进度隔离；旧 FailureProgress 回归 | Builder / Reviewer |
| AC-F-05 | Pass | `artifacts/task002/player-r5-repeat/calendar-390x844.png`; Player r5 | Builder / Reviewer |
| AC-F-06 | Pass | `artifacts/task002/persistence-final/persistence.json`; Player r5 | Builder / Reviewer |
| AC-F-07 | Pass | `artifacts/task002/player-r5-repeat/complete-390x844.png` | Builder / Reviewer |
| AC-F-08 | Pass | Player r5 当前月日历断言；Integrated Visual QA | Builder / Reviewer |
| AC-F-09 | Pass | Player r5 首次输入矩阵 | Builder / Reviewer |
| AC-F-10 | Pass | Player r5 设置/后台/重叠暂停断言 | Builder / Reviewer |
| AC-F-11 | Pass | Player r5 跨日、跨月、年末与过期重试断言 | Builder / Reviewer |
| AC-F-12 | Pass | Player r5 规划跨截止点、胜利优先与超时终态断言 | Builder / Reviewer |
| AC-T-01 | Pass | `artifacts/task002/offline-verification.json`：7 局/192 步回放 | Builder / Reviewer |
| AC-T-02 | Pass | Player r5 闰年、月末、年末和六行月份断言 | Builder / Reviewer |
| AC-T-03 | Pass | 独立进程持久化 3 项与重玩守卫 | Builder / Reviewer |
| AC-E-01 | Pass | User confirmed 2026-09-17: 每日挑战整体体验难度高于前 14 关 | Human |
| AC-E-02 | Pass | User confirmed 2026-09-17: 首页、日历和顶部计时清晰且操作自然 | Human |

## Regression Result

Overall Result: Desktop technical regression passed; one retained Uncertain event

- 修改前后 Unity Verify、Windows 构建和原失败/重试/冒险进度 Player 检查均通过。
- Introduced and fixed：日历 alpha 圆角重复叠绘纹理；非微信平台 unused 编译警告；早期截图设施黑图。
- Uncertain：r4 在新增截止点测试后出现一次“今日见证移动”被拒，原日志证据不足；r5 增加诊断后同一二进制连续两次未复现，不声称已修复或已归因。

## Fresh Review

Status: Pass (Desktop Technical Gate)

- 未发现可确认的 P1/P2 实现问题。
- 规划跨 180 秒的提交拒绝已通过实际 `FastMovement` + `MotionComposer` 路径验证。
- 2026-09-17 用户确认微信真机后台行为、安全区与核心流程验证通过。

## Human Art Approval

Status: Not Required

## Human Experience Check

Status: Pass

- 2026-09-17 用户确认每日挑战相对前 14 关的整体难度，以及首页、日历和顶部计时的清晰度与操作自然度。
- 2026-09-17 用户确认微信真机前/后台暂停、安全区和核心流程验证通过。

---

# Final Decision

Status: Verified

Windows Technical Gate、Fresh Review、Integrated Visual QA、Human Check 与微信真机 Gate 已全部通过。

## Task Version History

| Version | Date | Change | Reason | Invalidated Outputs |
|---:|---|---|---|---|
| 1 | 2026-09-16 | Migrated confirmed daily-challenge requirements into the v0.6 single-Task format | Harness v0.6 reinstall | Old free-form Designer output |
