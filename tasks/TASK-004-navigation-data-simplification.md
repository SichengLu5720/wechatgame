# Task: 导航数据与几何生成简化

Task ID: TASK-004  
Task Version: 1  
Status: Ready for Human Check  
Harness Mode: Lean  
Type: Architecture / Gameplay Infrastructure  
Risk: High  
Build Mode: Code Only  
Preview Mode: None  
Technical Gate: Fresh Review  
Art Gate: Not Required  
Experience Gate: None  
Created By: PM Orchestrator  
Created At: 2026-09-17

---

# Clarifications and Decisions

| ID | Question / Unclear Item | Confirmed Decision | Confirmed By | Task Version Impact | Affected Work |
|---|---|---|---|---|---|
| CL-001 | 是否把导航简化与微信视觉改进放在同一 Task | 拆分；导航优化保留为 TASK-004，微信 UI、占屏与辨色改为 TASK-005 | User | None | Task 边界与依赖 |
| CL-002 | 微信性能验收口径 | 使用同一微信真机做修改前后对比；关卡加载、峰值内存和帧率不得退化，并记录实际包体变化 | User | None | 性能基线、技术验收与发布证据 |

待 Designer 调查后收敛：

- 已确认当前角色不是物理避障物；本 Task 沿用现有地面支撑、断崖拒绝和玩法占位规则，不恢复逐人物避碰。

---

<!-- FROZEN_START -->

# Frozen Requirement

## Original Request

用户确认导航优化是独立的新需求，并要求拆分后将“优化导航”作为 TASK-004。

## Problem

当前平台高度、楼梯台阶、足底支撑、导航采样与烘焙校验紧密耦合。调整楼梯高度比例会使旧导航缓存失效，并要求重新验证；导航数据与可派生几何之间的职责需要简化。

## Goal and Player / User Outcome

- 关卡作者只维护解题与空间结构所需的最小事实，不手工维护可由几何生成的重复导航数据。
- 平台高度比例或楼梯表现参数发生受控变化时，系统能可靠识别旧缓存并重新生成，不产生无提示的错位导航。
- 玩家看到的连通关系、关卡解法、角色落脚和移动结果保持正确。

## Core Rules and Confirmed Decisions

- TASK-004 专门负责导航数据与几何生成简化。
- 关卡节点、连接关系、容量、队列、颜色、目标和解法仍是玩法事实，不得因导航重构改变。
- 导航缓存属于可派生数据，必须可校验、可失效并可重建；不得要求设计者手工同步重复高度表面数据。
- 角色仍必须沿真实平台和楼梯支撑面移动，不能仅凭拓扑连通跳过足底、碰撞和路径安全检查。

## Scope

- 调查并简化关卡源数据、世界高度转换、楼梯生成、支撑表面、路径搜索和导航缓存的职责边界。
- 为高度比例与楼梯几何提供单一、受验证的生成入口。
- 导航缓存版本、几何哈希、失效与重建流程。
- 现有关卡、编辑器、自定义关卡、冒险和每日挑战的兼容与回归。
- 为 TASK-005 提供可安全评估高度压缩的技术接口与验证证据，但不在本 Task 决定视觉参数。

## Non-goals

- 不修改关卡拓扑、解法、难度数据、容量、颜色或每日挑战规则。
- 不在本 Task 修改微信 HUD、相机、背景、人物配色或屏幕占比。
- 不取消真实几何支撑、脚底碰撞或路径安全验证。
- 不要求所有高度差变为相同数值；具体视觉高度比例由后续需求决定。

## Constraints

- 保持既有存档与关卡标识兼容。
- 旧缓存不得被静默当作新几何使用；失效必须可检测。
- 微信运行环境下的生成耗时、峰值内存与包体变化必须有实际证据。
- 微信性能采用同一真机修改前后对比：关卡加载、峰值内存和帧率不得退化，并记录实际包体变化；Windows 数据只能作为辅助证据。
- 不覆盖或回滚 TASK-003 正在进行的人物移动与楼梯表现修改；共享文件必须串行交接。

## Acceptance Criteria

| ID | Type | Criterion | Verification Owner | Required Evidence |
|---|---|---|---|---|
| AC-F-01 | Functional | 全部内置关卡、每日挑战及自定义关卡仍保持原节点连通、合法路径和解题结果 | Builder / Reviewer | 修改前后关卡与解法回归 |
| AC-F-02 | Functional | 修改受控高度生成参数后，无需手工修改重复导航数据即可生成有效楼梯、支撑面与路径 | Builder / Reviewer | 参数变化与自动重建测试 |
| AC-T-01 | Technical | 导航缓存与几何不一致时能稳定失效并重建，不读取陈旧缓存 | Builder / Reviewer | 哈希/版本失效测试与日志证据 |
| AC-T-02 | Technical | 角色移动全过程保持足底受支撑，不穿透、悬空、跨越断崖或停在错误高度 | Builder / Reviewer | 上下楼运动、足底和断崖回归 |
| AC-T-03 | Technical | 同一微信真机上，修改后关卡加载、峰值内存和帧率不得相对修改前退化，并记录实际包体变化 | Builder / Reviewer | 同设备修改前后微信构建与运行测量 |
| AC-C-01 | Comparative | 与修改前相比，关卡源数据和高度/楼梯生成职责更少重复，调整高度不再要求多处手工同步 | Reviewer | 数据流与修改点前后对比 |

<!-- FROZEN_END -->

---

# Design

## Current Implementation

- `NodeSpec.y` 经 `WalkSpace.LayerHeightScale` 转换为平台世界高度。
- 同一世界高度同时参与楼梯台阶数、支撑面采样、足底高度、路径搜索和导航几何哈希。
- 正式游玩主要通过 `Rules.Route → FastMovement → PlaybackFloor` 使用固定楼梯走廊，不依赖每步运行密集网格寻路。
- 教程及 reference/delivery 布局已使用 `geometryOnly` 跳过密集网格；其他入口的策略仍分散。
- 现有 17 份派生 bake 共 10,116,487 字节；缺失时会同步生成，但存在且失配时会直接抛错，主要加载入口没有统一恢复。
- 台阶高度公式在支撑面、场景台阶和 `PlaybackFloor` 中重复；现有几何 hash 未覆盖全部生成参数，也没有完整损坏校验。

## Recommended Design

采用“统一几何生成 + 正式运行时轻量派生 + 离线验证/可选 bake”的混合方案：

1. 保持 `LevelSpec`、节点、边、关卡 ID、存档和解法格式不变，不做数据迁移。
2. 引入不可变几何配置快照，集中世界高度比例、楼梯宽度和目标踏步高度；默认参数保持现状。
3. 生成单一不可变几何描述，供场景台阶、支撑面和 `PlaybackFloor` 共同消费，移除三处重复踏步公式。
4. 正式玩法统一使用轻量几何和按需稀疏采样，不在加载时同步重烘整张密集网格。
5. 密集 bake 仅保留为离线验证或可选缓存；缺失、旧版、签名不符、尺寸错误或损坏时安全失效并从当前几何重建。
6. 使用确定性几何签名覆盖生成参数、算法版本、表面输出、网格信息和足迹参数；配置变化同时失效旧空间、走廊和高度曲线。
7. 全部正式入口迁移后，停止把正式玩法不用的密集 bake 放入 `Resources`；离线产物进入 artifacts。
8. 构建预检统一覆盖实际 campaign、tutorial 和 daily，但不得修改布局或重新求解关卡。

## Workstream Ownership

单一 Code Builder 负责导航与最终集成：

- 主要写入：`WalkSpace.cs`，新增几何配置/描述、缓存编解码和统一加载工厂。
- 接入：`PlaybackFloor.cs`、`SurfaceCoverage.cs`、必要的 `SurfaceSet.cs`、`StairsGame.cs`、`ScenePreparation.cs`、`CampaignRepository.cs`、每日挑战与编辑器入口。
- 构建与验证：`NavigationMenu.cs`、统一构建预检、定向验证脚本及派生缓存资源。
- `CrowdScene.cs` 只改为消费统一台阶描述；`FastMovement.cs` / `CrowdMotion.cs` 仅做必要接入，不改变路径和动画计时。
- TASK-003 正在修改多个共享文件，必须串行交接；人物模型、动画与 `CharacterWalkDriver` 不属于本 Task。

---

# Regression Plan

## Protected Behaviors

- RB-01：全部关卡规则、拓扑、解法、存档和每日挑战保持不变。
- RB-02：角色选择、移动、上下楼、到达、撤销、洗混、失败与完成保持可用。
- RB-03：编辑器与自定义关卡继续生成合法导航。

## Baseline Checks

- 记录当前全部关卡的几何哈希、路径成功率、导航生成耗时、缓存尺寸和足底验证结果。
- 运行现有 Unity Verify、导航、高度、移动、关卡与每日挑战回归。

## Targeted Regression Checks

- 覆盖实际 campaign 20 关、原始 campaign 20 关、legacy 6 关和 daily 7 关，分别记录几何签名、边双向通过、解法回放与足底结果。
- 参数覆盖默认、压缩/放大高度、不同目标踏步高度与楼梯宽度；检查同高桥、负高度、斜向连接、高节点度、超过 64 个表面和稀疏模式。
- 缓存覆盖无缓存、有效缓存、旧版、错误签名、截断、错误尺寸与损坏负载；失效后结果必须与无缓存生成一致。
- 用独立表面与实际台阶顶面交叉验证，避免仅比较同一实现生成的两份 hash。
- 保留 Verify、VerifyAlternatives、HeightNavigationCheck，并补充全部正式关卡的 FastMovement 回放、加载、每日和 TASK-003 接地回归。
- 编辑器覆盖载入旧自定义关卡、修改高度、保存、分享导入、试玩、撤销和重置。
- 核心流程覆盖冒险/每日进入、移动、连续指令、撤销、洗混、完成、返回主页和跨关预加载。

---

# Build and Verification Results

## Code Result

Status: Integration Ready

- 已引入不可变几何配置/描述，台阶实体、支撑高度与 `PlaybackFloor` 共享同一踏步来源。
- 正式玩法使用轻量按需导航；密集缓存具备版本、SHA-256 几何签名、尺寸、负载和损坏校验，失效时重建。
- 17 份旧密集缓存及 metadata 共 10,116,487 字节从正式 `Resources` 移至 `artifacts/task004/legacy-resource-caches/`，原样可恢复。
- 53 关矩阵、1,056 条双向路径、918 步完整解法和 80,321 次足底检查通过；逐关几何观测、终局状态和移动总时长与基线一致。
- Windows 构建、正式27关预检、自定义保存/分享/改高/撤销、每日181项、loading/playback/navigation Player 检查通过。
- `VerifyAlternatives` 的“雾后色彩 step=5 3→5”失败在修改前后均复现，分类为 Pre-existing。

## Fresh Review

Status: Inconclusive

- AC-F-01、AC-F-02、AC-T-01、AC-T-02、AC-C-01 在已执行范围内通过。
- AC-T-03 缺少同一微信真机修改前后的加载、峰值内存、帧率和实际包体对比，不能通过。
- Reviewer 未发现 P0/P1 实现缺陷；GAME_SPEC 的旧缓存路径残留已由 PM 更新。

## Final Decision

Status: Ready for Human Check — 等待微信同设备性能 Gate

## Task Version History

| Version | Date | Change | Reason | Invalidated Outputs |
|---:|---|---|---|---|
| 1 | 2026-09-17 | 创建导航数据与几何生成简化 Task | 用户要求从微信视觉改进中拆分，并保留为 TASK-004 | None |
