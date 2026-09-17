# Task: 实机开发调试看板——行走速度与平台/人物比例

Task ID: TASK-006  
Task Version: 2  
Status: Ready for Human Check  
Harness Mode: Strict  
Type: Tooling  
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
| CL-001 | 看板是否面向正式玩家 | 仅开发模式可见和可操作，正式发布模式不出现入口、不响应输入 | User | None | 编译/运行门控、UI、输入 |
| CL-002 | 速度调节只改变动画还是同时改变实际移动 | 实际移动与行走动画一起加速，保持视觉步频与位移同步 | User | None | 移动时序、动画相位、到达与收尾 |
| CL-003 | 平台与人物比例如何调节 | 默认联动缩放，同时允许切换为平台、人物分别调试 | User | None | 调试参数、布局、碰撞/点击保护 |
| CL-004 | 调试参数如何持久化 | 提供独立“保存当前参数”按钮；只有按下保存后才写入持久化存储 | User | None | UI、PlayerPrefs、启动加载 |
| CL-005 | 看板展开方向及是否改变游戏画面比例 | 从入口横向展开为悬浮层；不压缩或缩放游戏视口，不改变屏幕宽高比、镜头和游戏画面比例 | User | None | UI 布局、输入隔离、镜头保护 |
| CL-006 | 平台比例是否改变平台中心、楼梯、站位、碰撞或规则 | 全部保持不变；平台比例只改变可见平台模型，不改变平台中心、楼梯、人物站位、碰撞、点击、寻路、镜头与关卡规则 | User | None | 平台视觉层、回归保护 |
| CL-007 | 首次无保存参数时的默认速度倍率 | 确认采用建议值 `1.25×`；速度范围 `0.50–2.00×`、步进 `0.05`，平台/人物范围 `0.75–1.25×`、步进 `0.01` | User | None | 参数默认值、UI、验证范围 |
| CL-008 | 调参控件与速度上限 | 三个参数恢复为滑块；速度范围改为 `0.50–20.00×`，平台/人物仍为 `0.75–1.25×`；保存、联动/独立与开发模式门控不变 | User | v2 | UI、输入、参数校验、移动子步与性能验证 |
| CL-009 | 本次交付是否执行 QA | 不执行；取消本轮专项 QA、Fresh Review 与实机验收，交付结果明确标记为未验证，不以未运行检查宣称通过 | User | v2 | 构建收尾、验收状态、风险披露 |
| CL-010 | v2 源码交付后是否继续生成实际可运行版本 | 是；在 TASK-008 缺失类型恢复后继续实装并生成开发模式可运行包，仍不恢复已取消的专项 QA | User | v2 | Unity 集成、开发构建、交付产物 |
| CL-011 | 可运行包生成后是否完成必要 QA/回归再交付 | 是；恢复 TASK-006 v2 必要技术 QA、核心回归与 Fresh Review，覆盖滑块、20×、保存、正式门控及 TASK-007 第8关锁修复 | User | v2 | 验收、回归、Fresh Review、最终状态 |

未解决的阻断问题：

- None.

---

<!-- FROZEN_START -->

# Frozen Requirement

## Original Request

“行走动画加速，帮我建立一个看板，可以在实机游戏中调整行走速度，平台+人物比例。”后续确认：仅开发模式；实际移动和动画一起加速；平台与人物比例默认联动，也可单独调试；使用独立按钮保存当前参数。

## Problem

当前行走速度、人物显示比例和平台显示比例由项目代码/资源固定，实机体验调整需要改代码、重建，且难以快速比较参数组合。需要一个不会进入正式玩家界面的开发看板，让开发者直接在真实关卡中调节并保存参数。

## Goal and Player / User Outcome

- 开发者可在实机真实游戏画面中即时调节实际移动速度与同步行走动画速度。
- 平台和人物比例默认联动变化，也能分别微调，以快速找到合适的空间与阅读比例。
- 参数只有点击独立保存按钮后才跨启动保留；未保存试调不污染已保存配置。
- 正式玩家、关卡规则和正式构建界面不受调试看板影响。

## Core Rules and Confirmed Decisions

- 调试看板仅在开发模式存在；正式模式不绘制、不接收看板输入、不加载调试覆盖值。
- 行走速度参数同时驱动实际路径位移速度和动画相位速度，避免加速后滑步或动画与导航脱节。
- 平台与人物比例默认处于联动模式；必须能切换为分别调节。
- 调节即时预览，但持久化只能由独立“保存当前参数”操作触发。
- 看板横向悬浮展开，不通过缩小棋盘、改变 viewport 或重算镜头来腾出空间；面板区域拦截触摸，未覆盖区域保持实时游戏画面。
- 平台比例是纯视觉调试倍率，只作用于可见平台模型；平台中心、楼梯、人物站位、碰撞、点击、寻路、镜头包络和关卡规则必须保持基线不变。
- 保存的是调试覆盖参数，不改写关卡数据、模型资源或 `ColorCatalog`。
- 调节不得改变合法移动、路径、容量、队列顺序、撤销/重置结果或关卡解法。
- 首次无保存参数时速度默认为 `1.25×`；速度滑块范围 `0.50–20.00×`。平台与人物视觉比例默认均为 `1.00×`，滑块范围 `0.75–1.25×`。三个参数均使用触屏滑块即时预览。

## Scope

- 开发模式看板入口、展开/收起、三个参数滑块、当前数值显示、联动/单独切换和保存反馈。
- 实际移动速度与 `CharacterWalkDriver` 动画相位的同步调节。
- 平台视觉比例与人物视觉比例的联动和独立调节。
- 已保存参数的启动加载、未保存参数的会话行为及回归验证。
- Windows 开发构建验证；保留微信实机/开发环境可用性检查点。

## Non-goals

- 不向正式玩家开放调参功能。
- 不改变关卡规则、平台容量、碰撞/寻路语义、队列位置或关卡存档格式。
- 不修改人物模型、平台模型、材质和六色目录。
- 不上传、发布、Commit、Push 或远程 Tag。

## Constraints

- Unity 6000.0.26f1；主要目标为微信小游戏，开发入口不能依赖仅桌面可用的键盘操作。
- 看板必须支持触屏，不能遮挡或穿透影响棋盘操作；展开时需要明确输入隔离。
- 平台和人物缩放首先作为视觉调试参数；点击、碰撞、落脚、楼梯和镜头包络必须被明确保护或验证。
- 继续满足高角色数场景稳态 0 B/frame 托管分配目标。
- 使用独立命名空间的本地调试配置键，不覆盖正式设置或关卡存档。

## Accepted Assumptions

- None.

## Acceptance Criteria

| ID | Type | Criterion | Verification Owner | Required Evidence |
|---|---|---|---|---|
| AC-F-01 | Functional | 开发模式可打开/关闭看板，正式模式无入口且不应用调试覆盖 | Builder / Reviewer | 开发/正式双模式运行检查 |
| AC-F-02 | Functional | 速度滑块在 `0.50–20.00×` 全范围即时调节实际移动与同步步态；到达、转向、上下楼梯和停止仍正确 | Builder / Reviewer | 同路径多档速度连续采样与录像/截图，包含 20× |
| AC-F-03 | Functional | 速度、平台和人物均使用触屏滑块；平台和人物默认联动，也可分别调节；保存按钮可持久化当前参数，未保存值不覆盖已保存配置 | Builder / Reviewer | 滑块拖动/取消/边界、UI 状态、重启 roundtrip 与存储隔离测试 |
| AC-T-01 | Technical | 调试缩放不改变棋盘规则、路径、容量、碰撞/点击结果、撤销/重置或关卡解法 | Builder / Reviewer | 基线/候选回归与核心 smoke |
| AC-T-02 | Technical | 高角色数调参和行走保持 0 B/frame，参数读取无持续增长或逐帧持久化 | Builder / Reviewer | 性能/GC 与 PlayerPrefs 写入计数证据 |
| AC-E-01 | Experiential | 实机触屏看板易于操作、数值变化可理解，能有效找到更合适的速度和比例 | Human | 实机调试 Human Check |

## Test Proxies

| Proxy ID | Proxy | Supports Criterion | Limitation |
|---|---|---|---|
| TP-001 | 位移/动画相位比在各速度档保持稳定，到达误差在既有容差内 | AC-F-02 | 不能替代对步态自然度的人工判断 |
| TP-002 | UI 自动化覆盖联动、独立、保存和重启 roundtrip | AC-F-03 | 不能证明实机触屏手感 |

<!-- FROZEN_END -->

---

# Design

## Current Implementation

- Entry point: 待 Designer 调查。
- Owning system: `StairsGame`、`CrowdMotion`、`CharacterWalkDriver`、平台与人物表现层、开发入口与设置存储。
- Related code: 待 Designer 核对真实调用链。
- Related scenes / prefabs / nodes: 运行时动态场景；待调查。
- Related art / resources: 当前平台资源与 TASK-003 v5 人物资源。
- Current state or data flow: 速度与显示比例分散在运行时代码常量/表现结构中，没有实机调试面板和显式保存流程。
- Confirmed current behavior: 已有开发/编辑入口和 PlayerPrefs 设置机制，但无本 Task 看板。
- Confirmed gap or defect: 需要改代码重建才能比较速度与比例。

## Recommended Design

1. 新增开发调参服务，分离 `working` 与 `saved` 快照；拖动只改内存，点击保存才执行独立 PlayerPrefs JSON 写入与 `Save()`。
2. 使用 `UNITY_EDITOR || DEVELOPMENT_BUILD` 门控入口、输入和保存值加载；正式构建完全忽略调试覆盖。
3. 速度倍率应用于 `StairsGame.Advance()` 的运动时钟；`CharacterWalkDriver` 继续按实际位移推进相位，不额外乘动画倍率，避免重复加速。
4. 看板使用现有 IMGUI/GameplayLayout，触屏入口与面板手势需完整隔离，避免穿透棋盘。
5. 人物比例需要独立调试层，位于导航根和选择/出场动画层之间；比例变化时重置足锁并重算支撑。
6. 平台比例只作用于 renderer-only 可见层；不得缩放同时承载碰撞面和楼梯端点的 NodeRoot，也不得改平台中心、楼梯、站位、点击、寻路、镜头或规则。
7. 平台只缩 renderer-only 可见层；非四边形平台拆出无 Collider 的视觉子节点。人物新增独立调试比例层，导航 root、选择层和碰撞保持不变；比例变化重置足锁并按视觉步幅校正周期距离。
8. v2 使用三个触屏滑块：速度范围 `0.50–20.00×`，平台/人物范围 `0.75–1.25×`。显示两位小数；滑块拖动即时预览并完整捕获手势，联动模式共同乘同一倍率并在任一边界共同停止，保留相对比例。具体内部量化精度不得影响端点可达与保存 roundtrip。

## Main Change Areas

- 开发模式门控与触屏看板。
- 调试参数状态、即时应用和显式持久化。
- 移动/动画同步倍率。
- 平台/人物视觉比例与保护性适配。

## Technical Options

- None yet.

## Proposed Product Decisions

- None.

## Workstream Ownership

| Workstream | Required | Allowed Write Paths | Forbidden Paths / Shared Files |
|---|---:|---|---|
| Code Builder | Yes | `Assets/Scripts/Runtime/RuntimeTuning*.cs`、必要的 `StairsGame.cs` / `CloudInterface.cs` / `SettingsInterface.cs` / `ViewOrbit.cs` / `CrowdScene.cs` / `WaterBirdVisual.cs` / `CharacterWalkDriver.cs` / `ScenePreparation.cs` / `RefinedPresentation.cs`，因新增视觉层而必须适配且不改变验收语义的 `LoadingVerification.cs` / `HiddenArtVerification.cs` / `RefinedRuntimeVerification.cs`，以及 Task 专属 Editor 构建/验证脚本 | Task、Core/关卡数据、`WalkSpace` 几何常量、镜头包络规则、生产美术、`ColorCatalog`、SDK/vendor |
| Art Asset Builder | No | None | 全部 |
| Final Integration | Yes | 由 Code Builder 唯一负责运行时绑定、构建与验证 | Task、生产美术源文件 |

## Code–Art Interface

Not Required.

## Asset Contract

Art Bible: `docs/ART_BIBLE.md`  
Approved Preview: Pending  
Asset Manifest: Not Required

---

# Visual Direction

Preview Status: Reference Only — Requirements Matched  
Latest Approved Preview: `.harness/previews/TASK-006/r002/implementation-preview.png`

## Accepted Visual Decisions

- 仅开发模式，触屏可用，不进入正式玩家 UI。
- 看板应清楚区分即时调试、联动/独立模式和显式保存操作。

## Rejected Directions

- 自动保存每次拖动。
- 仅键盘可用的桌面调试入口。

## Open Visual Decisions

- 入口位置、面板占位、控件形式、数值范围与保存反馈。

## Preview History

| Revision | Type | Artifact | Decision | Feedback / Changes |
|---:|---|---|---|---|
| r001 | Requirement | `.harness/previews/TASK-006/r001/requirement-preview.png` | Needs Clarification | 四状态需求提案：右上入口、底部工具区、联动/独立、当前/已保存值与显式保存；展开态棋盘可用区域和输入策略待确认，数值仅示例 |
| r002 | Implementation | `.harness/previews/TASK-006/r002/implementation-preview.png` | Reference Only | 按用户确认改为右侧向左横向悬浮；原 viewport、镜头、HUD、背景和棋盘像素不变，面板外游戏继续运行；390/700 八状态检查通过，入口需按实机安全区锚定 |

---

# Regression Plan

## Protected Behaviors

- RB-01：正式模式 UI、输入与默认参数不变。
- RB-02：棋盘状态、合法移动、路径、容量、撤销、重置与关卡解法不变。
- RB-03：人物六色、模型、材质、隐藏/揭示和选择状态不变。

## Impacted Systems

- 移动时序、动画相位、人物/平台表现尺度、镜头包络、点击与碰撞、开发 UI、PlayerPrefs。

## Baseline Checks

- 当前移动时长、动画相位、平台/人物显示尺度、点击区域、楼梯落脚、高角色数性能。
- 正式模式无看板；现有设置与关卡 PlayerPrefs 内容。

## Targeted Regression Checks

- 多档速度、联动/独立缩放、保存/未保存/重启、开发/正式门控、触屏输入隔离。
- 上下楼梯、转向、到达、撤销、重置、隐藏/揭示、多人批量移动与关卡切换。

## Core Smoke Path

1. 开发模式进入关卡，打开看板并提高速度。
2. 联动调整比例，再切换独立模式分别调整平台和人物。
3. 完成包含转向和楼梯的移动，保存参数并重启确认；随后验证未保存变更不会覆盖。
4. 正式模式启动，确认无入口且使用正式默认值。

## Visual Regression Checks

- 调整范围内检查平台与人物遮挡、穿插、点击区域、脚底接触、镜头裁切和六色可读性。
- 检查看板在窄屏/宽屏、安全区和触屏下不遮挡关键操作。

## Known Pre-existing Issues

- TASK-003 v5 已记录的 Hidden 问号亮度断言和 VerifyAlternatives 历史失败继续单独归类。

---

# Build and Verification Results

> 本区域由 PM 根据 Subagent 返回结果更新。Builder 不直接修改 Task。

## Code Result

Status: Complete; Development and Release Builds Produced

- v2 已将三个参数改为触屏滑块：速度 `0.50–20.00×`（对数映射、`.05` 量化、端点精确），平台/人物 `0.75–1.25×`（线性映射）。联动/独立、显式保存、开发模式门控和横向悬浮保持不变。
- 高倍率运动子步容量已扩展并完整推进 `motionDelta`；移动中的入步、转向和高度响应跟随运动虚拟时间，世界、揭示、UI、每日计时及其他生命周期保持真实时间。
- v2 触及：`RuntimeTuning.cs`、`RuntimeTuningBoard.cs`、`RuntimeTuningVisuals.cs`、`RuntimeTuningVerification.cs`、`StairsGame.cs`、`CrowdScene.cs`、`RefinedPresentation.cs`、`CharacterWalkDriver.cs`、`WaterBirdVisual.cs`、`ScenePreparation.cs`、`ViewOrbit.cs`、`RuntimeTuningBuild.cs`，以及最小验证路径适配。
- 多指或系统取消后，剩余触点不会重新激活滑块，直至全部触点释放；对应两项断言已进入最终开发 QA。
- 最终 Development 与 Release Windows 包均已生成；两包各记录 98 份源码，与当前源码及彼此比较均为 0 差异。

## Art Result

Status: Not Required

## Integration Result

Status: Integration Complete; Technical Gate Passed

- 可交付开发包：`Builds/RuntimeTuningV2Dev/StairsCrowd.exe`；正式门控包：`Builds/RuntimeTuningV2Release/StairsCrowd.exe`。
- 开发定向检查于 17:27:13 通过 382,462 项，包含新增 remaining-finger 与 system-cancel 两项断言；正式门控于 17:27:26 通过 4 项。
- 开发/正式包的 `source-manifest.json` 各覆盖 98 份源码，与当前源码、彼此以及收尾前快照比较均为 0 差异。
- 最终定向检查覆盖 127,048 个相位样本；运动、参数变化、持续滑块拖动、面板 120 次事件及 20× 检查均为 0 B 分配。
- Loading、Playback、第 8 关锁修复、持久化等证据来自本轮较早检查，最终重建后未重复扩大执行这些回归。

## Acceptance Results

| Criterion | Result | Evidence | Owner |
|---|---|---|---|
| AC-F-01 | Pass (Windows tested scope) | Development QA PASS 382,462 | Builder / Reviewer |
| AC-F-02 | Pass (Windows tested scope) | Slider/link/independent/save/restart and unsaved-isolation checks | Builder / Reviewer |
| AC-F-03 | Pass (Windows tested scope) | Release gate PASS 4; no tuning UI/save behavior in release | Builder / Reviewer |
| AC-T-01 | Pass (Windows tested scope) | Movement/animation virtual-time checks and 127,048 phase samples | Builder / Reviewer |
| AC-T-02 | Pass (Windows tested scope) | Renderer-only platform scaling, input capture/cancel checks, source manifests 0 diff | Builder / Reviewer |
| AC-E-01 | Pending Human Check | Real touch feel, safe area, device FPS, foot contact and gait naturalness | Human |

## Regression Result

Overall Result: Pass in Executed Scope

- Introduced and fixed: lifecycle substep coupling, linked-scale floating-point boundary, safe-area hitbox mismatch, and multi-touch cancellation reactivation.
- Expected Change: developer-only tuning overlay, virtual movement-time acceleration, renderer-only platform scale and independent character visual scale.
- Pre-existing: Hidden question-mark brightness assertion remains separately tracked.
- No Difference: final dev/release packages contain the same 98-source snapshot; no source drift was detected during final rebuild.

## Fresh Review

Status: Pass

- Independent read-only review confirmed development QA PASS 382,462, release gate PASS 4, both new cancellation assertions in the mandatory path, and all current/dev/release source comparisons at 0 differences.
- Technical Gate passed. Automated gesture-state tests do not replace real-device touch and experience review.

## Human Art Approval

Status: Not Required

## Human Experience Check

Status: Pending

- Required on target WeChat/device for touch feel, safe-area behavior, 20× full-frame performance, foot contact and animation naturalness.

---

# Final Decision

Status: Ready for Human Check

## Task Version History

| Version | Date | Change | Reason | Invalidated Outputs |
|---:|---|---|---|---|
| 1 | 2026-09-17 | Initial draft | 用户要求建立开发模式实机速度与比例调试看板 | None |
| 2 | 2026-09-17 | 三个参数恢复滑块；速度上限提高至 20× | 用户调整调参交互与速度范围 | v1 加减按钮 UI、2× 上限验证与预览 |
