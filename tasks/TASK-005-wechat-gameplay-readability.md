# Task: 微信全局游玩区与颜色可读性

Task ID: TASK-005  
Task Version: 1  
Status: Ready for Human Check  
Harness Mode: Lean  
Type: Feature / Visual UX  
Risk: High  
Build Mode: Code Only  
Preview Mode: Requirement  
Technical Gate: Fresh Review  
Art Gate: Not Required  
Experience Gate: Human Check  
Created By: PM Orchestrator  
Created At: 2026-09-17

---

# Clarifications and Decisions

| ID | Question / Unclear Item | Confirmed Decision | Confirmed By | Task Version Impact | Affected Work |
|---|---|---|---|---|---|
| CL-001 | 改进范围 | 应用于冒险、每日挑战及全部玩法关卡，不仅限每日挑战 | User | None | HUD、相机、颜色与全局回归 |
| CL-002 | 是否与导航数据简化放在同一 Task | 拆分；导航优化为 TASK-004，本 Task 改为 TASK-005 | User | None | Task 边界、预览路径与构建顺序 |
| CL-003 | 固定机位何时决定 | 每关在生成/交付时确定自己的固定机位；玩家进入后不自动择优、不变化 | User | None | 关卡生成、旧关回填、相机构图验证 |
| CL-004 | 游玩区域纵向位置 | 整体游玩区域下移，在顶部系统/HUD 与底部道具安全区之间适配 | User | None | 共享可玩矩形、相机偏移、点击与教程 |
| CL-005 | 过宽关卡如何放大 | 在关卡生成/交付阶段扭转平台空间布局，压缩横向宽度；运行中不改变布局 | User | None | 平台 XZ 位置、楼梯重建、构图与移动回归 |
| CL-006 | 允许的布局扭转角度 | 只允许 90° 或 180°，不使用其他任意角度 | User | None | 候选生成、确定性搜索与回退 |
| CL-007 | r006 直角折转方向 | 确认；作为正式构图方向继续设计与实现 | User | None | 视觉方向、构图合同与验收 |
| CL-008 | HUD 是否显示步数 | 删除全部玩法关卡的步数显示；内部步数和既有玩法逻辑保留 | User | None | 顶部 HUD、教程与回归 |
| CL-009 | 六色是否允许重新校准 | 允许重新校准六种基础色；保留颜色 ID、玩法身份和人物染色区域 | User | TASK-003 已升级至 v3 | ColorCatalog、正式材质、背景与全状态辨色 |
| CL-010 | r007 六色候选对比度是否足够 | 不足；继续提高六色之间及其与背景/米白区域的视觉对比度 | User | None | 基础色候选、光照与状态预览 |
| CL-011 | r008 高对比六色方向 | 确认；冻结为首轮正式实现基础色，Unity/微信真机仍需 Human Check | User | None | ColorCatalog、正式材质、光照与状态验收 |

未解决的阻断问题：

- 若选择调整统一高度比例，本 Task 只消费 TASK-004 提供的受验证接口，不自行重构导航。
- r002 证实降低层高不能解除横向投影瓶颈；第 14 关候选固定方位收益约 1.00×，第 18 关平台包络约 1.16×。机位与高度都不能承诺所有关卡获得相同比例放大。

---

<!-- FROZEN_START -->

# Frozen Requirement

## Original Request

用户基于微信真机截图反馈三个问题：“1. UI 不匹配微信小游戏格式；2. 游玩区域占屏幕比重较小；3. 色彩分辨度较低，无法清晰辨认不同颜色。”后续确认应用于全部关卡。

## Problem

- 真机顶部 HUD 与微信胶囊及安全区竞争，信息层级与间距不符合小游戏使用场景。
- 棋盘、平台与人群在竖屏中占比不足，上下留白过多。
- 背景色覆盖感强，人物颜色面积小，玩法颜色的色相、明度和边缘对比不足。

## Goal and Player / User Outcome

- 所有关卡在微信竖屏中的 HUD 避让系统胶囊和安全区，信息与操作层级清晰。
- 棋盘和可操作人群在不裁切关键节点的前提下显著增大，更充分使用可用屏幕。
- 玩家能在手机尺寸下快速区分所有玩法颜色，且难度不再来自低对比或难以辨色。

## Core Rules and Confirmed Decisions

- 改进应用于冒险、每日挑战和全部玩法关卡。
- 参考图只用于信息密度、棋盘占屏和颜色对比的方向参考；不复制其品牌、角色、按钮、货币、锁或具体配色。
- 每关的固定立体斜俯视机位必须在生成/交付时确定并满足竖屏构图要求；游玩中不自动择优、不变化，也不新增玩家拖动旋转视角。
- 游玩区域整体下移，在顶部系统/HUD 与底部道具安全区之间使用共享可玩矩形完成构图。
- 顶部 HUD 删除步数显示；设置位于左侧，关卡/每日挑战计时居中，微信系统区域独立避让。内部步数统计与玩法逻辑不变。
- 允许重新校准六种基础色，但颜色 ID、玩法身份、分组判定和人物既有染色区域保持不变；正式人物材质必须与统一颜色目录一致。
- 六色首轮正式实现基础色按稳定 ID 冻结为：红 `#EE5147`、蓝 `#187FE8`、绿 `#43BF58`、黄 `#E9AC16`、紫 `#B44CDB`、黑 `#202024`。黑色与深背景的分离还必须通过人物专用阴影下限和背景协调验证，不得以继续压暗黑色代替。
- 当关卡横向包络限制放大时，允许在生成/交付阶段扭转平台的平面空间布局以压缩宽度；必须保留节点身份、边及其顺序、容量、队列、颜色、机制、目标与解法，并重新生成合法楼梯。
- 平台布局扭转角度仅允许 90° 或 180°；没有合法且有效的离散候选时回退原布局，不得采用其他角度凑出结果。
- 玩法颜色仍由 `ColorCatalog` 统一管理，不改变关卡中的颜色身份与规则。

## Scope

- 微信安全区与顶部/底部 HUD 排布。
- 全部玩法关卡移除步数显示，不删除内部步数数据。
- 所有关卡的动态相机适配、棋盘占屏和关键节点防裁切。
- 关卡生成/交付阶段的固定机位要求、现有关卡回填与构图验证。
- 过宽关卡的平台 XZ 布局扭转、楼梯重建、遮挡检查、移动时长对比与失败回退。
- 玩法颜色、人物服装颜色面积、边缘对比与背景分离。
- 冒险、每日挑战、教程、失败/完成和道具 HUD 的针对性回归。

## Non-goals

- 不复制参考游戏的美术风格、UI 或商业化元素。
- 不新增货币、奖励、锁、关卡编号牌或新玩法。
- 不改变关卡规则、节点连接拓扑、解法、容量、完成条件、难度数据或存档；允许已确认的平面空间布局与实际楼梯长度变化。
- 不启用视角旋转。

## Constraints

- 中文 UI，继续使用项目自有的深青灰夜空、暖米白、柔和珊瑚和薄荷语言，但玩法颜色必须优先清晰可辨。
- 必须适配微信右上角胶囊区域、竖屏安全区和现有 390×844 / 700×1000 验证尺寸。
- 不为放大棋盘而裁切可交互平台、人群、楼梯或移动反馈。
- 颜色调整不得破坏既有颜色 ID、关卡数据、材质引用或分组判定。
- 平台扭转不得产生平台重叠、缺失楼梯、楼梯穿越平台或不可点击遮挡；每日挑战的实际移动时间变化必须单独记录并进行体验验收。
- 候选生成必须确定性使用 90°/180° 离散扭转，禁止随机角度或因设备性能不同得到不同布局。

## Acceptance Criteria

| ID | Type | Criterion | Verification Owner | Required Evidence |
|---|---|---|---|---|
| AC-F-01 | Functional | 所有关卡模式的顶部 HUD、设置和系统胶囊不重叠，底部道具不超出安全区 | Builder / Reviewer | 微信真机及 390×844 / 700×1000 截图 |
| AC-F-03 | Functional | 冒险、教程和每日挑战局内不再显示步数；撤销、解法验证及内部计数行为保持 | Builder / Reviewer | 全模式截图与行为回归 |
| AC-F-02 | Functional | 所有可交互平台、人群和楼梯在小、中、大关卡中完整可见、可点击 | Builder / Reviewer | 代表关卡构图与输入 Smoke Test |
| AC-T-01 | Technical | 相机适配在目标尺寸与代表关卡上稳定，不引入震荡、裁切或错误点选 | Builder / Reviewer | 自动构图边界检查与 Player 回归 |
| AC-T-03 | Technical | 每个关卡的固定机位在生成/交付时确定并通过构图验证；运行中不重新择优或跳变 | Builder / Reviewer | 关卡构图元数据/确定性规则检查与运行时稳定性回归 |
| AC-T-04 | Technical | 过宽关卡的扭转结果保留全部节点、边顺序与解法，并生成完整合法楼梯；验证失败时不得交付错误布局 | Builder / Reviewer | 修改前后结构对比、全边路径、楼梯与解法回放 |
| AC-C-01 | Comparative | 对触发扭转的关卡，横向受限程度下降，人物像素高度和棋盘占屏相对原布局提高；同时记录移动总时长变化 | Builder / Human | 同尺寸投影/实机前后对比与完整解法计时 |
| AC-T-02 | Technical | `ColorCatalog` 中全部玩法颜色在实际材质与背景上保持可区分，且颜色 ID 与关卡数据不变 | Builder / Reviewer | 颜色目录静态验证、对比截图与数据回归 |
| AC-T-05 | Technical | 正式人物材质、普通/选择/揭示/道具状态均从统一六色合同派生，不再出现目录色与运行时材质基础色不一致 | Builder / Reviewer | 材质一致性检查与渲染证据 |
| AC-E-01 | Experiential | 微信真机中的 HUD 像原生小游戏界面，与系统胶囊及玩法画面层级清楚 | Human | 修改前后真机对比 |
| AC-E-02 | Experiential | 玩家明显感到棋盘和人群更大、屏幕利用更充分，同时不影响全局空间理解 | Human | 代表关卡对比试玩 |
| AC-E-04 | Experiential | 游玩区域下移后与顶部 HUD、底部道具形成清楚层级，不显得顶重或挤压底部操作 | Human | 390×844、700×1000 与微信真机对比 |
| AC-E-03 | Experiential | 玩家能在手机尺寸下快速、稳定地辨认全部颜色，难度不来自颜色混淆 | Human | 包含所有颜色的对比试玩 |

<!-- FROZEN_END -->

---

# Design

Dependency: TASK-004（平台折转后的统一几何、楼梯、支撑与导航验证接口）

## Current Implementation

- 微信层目前只读取窗口信息，未统一提供胶囊矩形、安全区和窗口变化后的布局刷新。
- 顶部设置、关卡/计时、步数和微信系统控件使用不同坐标口径；局内点击区仍硬编码为屏高约 19%–87%。
- 相机采用固定方向和保守包络；已有竖屏候选方向代码未接入，且仅覆盖单个关卡。
- 当前宽关卡在真机截图中可玩包络已接近占满屏宽，单纯缩小正交相机尺寸会裁切左右平台。
- 正式人物材质会覆盖 `ColorCatalog` 初始化色，部分材质色值已经与目录不一致；人物色彩主要受专用 Shader 固定光照影响。
- 当前亮青背景与人物/塔体形成同色覆盖感，且颜色可见面积受远距离构图与暖米白头罩分区共同限制。

## Recommended Design

1. 新增共享布局快照，统一输出屏幕安全区、微信胶囊/系统预留带、顶部 HUD、底部道具和可玩矩形。
2. 微信平台层在初始化、窗口变化和回到前台时刷新窗口与胶囊信息；无效数据回退到 `Screen.safeArea`。
3. 主相机继续全屏绘制，按共享可玩矩形解算关卡包络的最大等比缩放和偏移；以受限轴充分利用为目标，不承诺所有长宽比都达到相同高度占比。
4. 固定机位方向在生成/交付阶段确定并验证；运行时仅按设备安全区做等比缩放和平移，不再扫描或随机选择方向。待确认采用全关统一机位还是逐关表现合同。
5. 先最大等比 fit，再用剩余纵向空间进行偏下锚定；空间不足时钳制偏移，不能为达到固定下移像素而缩小棋盘。
6. 相机包络覆盖完整平台、楼梯、人物、选中抬升、步态和反馈；装饰长柱不参与构图。相机只在关卡、分辨率或布局变化时重算。
7. 输入有效区、HUD 触摸排除区和教程提示读取同一布局，替换屏高 19%–87% 及教程固定边界，避免“看得到但点不到”。
8. 预加载只准备不可变表现数据，不触碰当前共享相机；正式切关时才应用固定机位并 fit。
9. 先统一 `ColorCatalog` 与正式人物材质，再在批准的基础色合同内调整背景分离、固定光照和轻微边缘对比；保留揭示、隐藏、选择和道具聚焦状态。
10. 对宽度压力明显的关卡，按固定顺序搜索局部分支的 `+90° / -90° / 180°` 折转；保持节点 y 与分支内部距离，不允许自由角度、缩放、剪切或任意平移。候选不合法或无实际收益时回退原布局。
11. 折转在世界 XZ 平面计算，再转换回关卡坐标；结果必须确定性固化，运行时不搜索。逐边检查相对方向、平台/楼梯重叠、全边生成、站位、足底、解法、点击、遮挡和移动时长。

## Main Change Areas

- 共享安全区与布局：`WeChatPlatform.cs`、`CloudInterface.cs`、`SettingsInterface.cs`、`DailyChallenge.cs`。
- 相机、输入与教程：`CrowdScene.cs`、`StairsGame.cs`、`ViewOrbit.cs`、`TutorialFlow.cs`。
- 背景与运行时材质：`NightPresentation.cs`、`FixedTowerBackdrop.cs`、相关 Shader 与 `RefinedPresentation.cs`。
- TASK-003 正在修改 `CrowdScene.cs`、`RefinedPresentation.cs` 和人物资产；共享文件必须串行交接。

## Workstream Ownership

| Workstream | Required | Allowed Write Paths | Forbidden Paths / Shared Files |
|---|---:|---|---|
| Code Builder | Yes | 新增 TASK-005 专属布局/构图/验证文件；批准后的微信、HUD、相机、输入、教程、背景与材质代码 | Task、关卡 JSON、导航、存档、规则、TASK-003 模型/动画/导入器 |
| Art Asset Builder | No | None | 全部项目文件 |
| Final Integration | Code Builder，待 TASK-003 串行交接 | 同上 | Task、关卡数据 |

## Code–Art Interface

不启用独立 Art Builder。必须保留六色 ID 顺序、材质参数 `_ClothColor` / `_RevealTint` / `_PropDim`、TASK-003 网格/骨架/COL0 遮罩和固定外形。若最终要求改变染色分区或生产模型，需重新规划 Build Mode。

---

# Visual Direction

Preview Status: Approved Direction  
Latest Approved Preview: r006（直角折转构图）＋r008（高对比六色）；r007 的无步数 HUD 同步作为布局基准

## Accepted Visual Decisions

- 微信系统胶囊与游戏 HUD 必须清晰避让。
- 棋盘和人群显著增大，但不裁切关键交互内容。
- 玩法颜色的区分性优先于低对比氛围效果。
- 参考图不是可直接复制的美术或 UI 合同。
- 每关生成/交付时固定机位；过宽关卡只允许 90°/180° 局部分支折转，无有效候选则回退原布局。
- 接受 r006 展示的直角折转构图方向；示例节点操作和倍率不是全关固定参数。
- 接受 r007 的无步数 HUD 层级与 r008 的高对比六色方向；离线材质图不替代 Unity/微信真机验收。

## Open Visual Decisions

- 顶部 HUD 在微信胶囊下方的分组、尺寸与对齐。
- 不同关卡尺寸下棋盘占屏与留白的平衡。
- 颜色目录的色相、明度、饱和度、边缘与背景对比。

## Preview History

| Revision | Type | Artifact | Decision | Feedback / Changes |
|---:|---|---|---|---|
| r001 | Requirement | `.harness/previews/TASK-005/r001/requirement-preview.png`、`hud-phone-390.png` | Needs Clarification | 从拆分前 TASK-004 迁移；可评审 HUD 层级与六色调色方向；宽关卡显著放大受当前横向包络限制，未用拉伸或裁边伪造结果 |
| r002 | Requirement | `.harness/previews/TASK-005/r002/composition-comparison.png`、`level18-composition-comparison.png`、`hud-color-comparison.png` | Needs Clarification | 使用真实第 14/18 关节点比较原机位、加载时固定方位与 15% 高度压缩概念；高度压缩未伪装成放大收益，正式机位、六色与 HUD 层级待确认 |
| r003 | Requirement | `.harness/previews/TASK-005/r003/level14-downshift-comparison.png`、`level18-downshift-comparison.png`、手机尺寸预览 | Preview Ready / Pending Human Review | 按最新反馈把机位选择移至生成阶段，并演示共享可玩矩形偏下；40px 仅为比较值。下移不放大棋盘，且会压缩底部余量，正式实现必须按安全区钳制 |
| r004 | Requirement | `.harness/previews/TASK-005/r004/overview.png`、`level18-stress-comparison.png`、六张手机单案图 | Preview Ready / Pending Human Review | 基于真实第 14/18 关结构提出 A 固定机位精确适配、B 生成阶段竖向重排、C 平台/人物协同放大；B/C 涉及真实几何变化，尚未批准进入生产 |
| r005 | Requirement | `.harness/previews/TASK-005/r005/` | Needs Revision / Superseded | 使用了后来被禁止的非直角候选；保留历史但不得作为实现依据，由 r006 取代 |
| r006 | Requirement | `.harness/previews/TASK-005/r006/level14-fold-comparison.png`、`level18-fold-comparison.png`、手机前后图 | Approved Direction | 用户确认只使用 90°/180° 局部分支折转；第14关示例减宽8.9%、人物代理约+10%，第18关减宽23.3%、人物代理约+30%。真实楼梯、导航、遮挡与解法仍须实现验证 |
| r007 | Implementation | `.harness/previews/TASK-005/r007/hud-overview.png`、`material-state-comparison.png`、390px 手机图 | Preview Ready / Pending Human Review | 删除步数后的 HUD 与六色候选；使用当前正式源网格及真实染色遮罩展示普通、选择、揭示中段。离线近似渲染，不代表 Unity/微信集成已通过 |
| r008 | Visual Iteration | `.harness/previews/TASK-005/r008/contrast-iteration-overview.png`、`packed-phone-comparison.png`、390px 满员图 | Approved Direction | 用户确认高对比六色方向；冻结首轮基础色，黑/深背景仍需结合人物专用阴影下限和背景分离在 Unity/真机复核 |

---

# Regression Plan

## Protected Behaviors

- RB-01：所有关卡规则、路径、存档、失败、每日计时与日历保持不变。
- RB-02：平台点选、人群选择、道具、教程、编辑器和自定义关卡保持可用。

## Baseline Checks

- 微信真机当前截图作为 UI、占屏与辨色基线。
- 390×844 / 700×1000 首页、代表关卡、道具、失败、每日计时和日历截图。
- Unity Verify、Windows Player 与微信导出基线。

## Targeted Regression Checks

- 尺寸覆盖 390×844、700×1000、实际真机纵横比；模拟无安全区、刘海、底部 inset、异常胶囊返回和 DPR 限制。
- 代表关卡覆盖教程第 1 关、第 7 关、第 10 关、冒险第 13/14 关、第 18 关横向压力场景及每日挑战；第 15 关以后仅验证技术适配，不作为已调试难度基准。
- 自动记录关卡投影宽/高利用率、人物像素高度、受限轴和关键包络到可玩矩形的最小边距。
- 通过真实按下/抬起输入路径检查平台点选；同步验证拖动取消、禁止视角旋转、教程气泡、机制介绍、设置、日历、失败和完成面板。
- 对普通、选中、揭示、隐藏和道具聚焦状态检查六色身份；目录、正式材质和渲染后色彩证据分别记录。
- 修改前后运行同一 Unity Verify、Windows Player、微信导出与核心流程；先记录既有失败，避免把旧断言误归因于本 Task。

## Core Smoke Path

1. 从首页进入冒险与每日挑战。
2. 分别检查小、中、大关卡的构图、点选、移动、道具、失败和完成。
3. 在真机上检查系统胶囊、安全区、棋盘占屏与全色彩辨识。

---

# Build and Verification Results

## Code Result

Status: Integration Ready

- 已实现共享微信安全区/胶囊/HUD/道具/可玩矩形；窗口变化与回前台刷新，异常数据回退 `Screen.safeArea`。
- 全玩法局内删除步数显示，内部 `Moves`、撤销、验证与每日规则保持。
- 生成 43 份固定机位/布局元数据，22 份使用合法 `±90°/180°` 局部分支折转；不合格候选回退原布局，源关卡 JSON 未改。
- 构图按完整平台、楼梯、满员人物与选择/移动包络最大等比适配，再利用剩余空间偏下。
- 六色目录和现役正式材质统一为批准 HEX；人物专用阴影下限不影响平台，选择/隐藏/揭示/道具状态保留。
- 未导入或启用 TASK-003 v4 新模型。

## Art Result

Status: Not Required

## Integration Result

Status: Ready for Human Check

- Unity Verify、12 种布局/106 组投影、Windows Player、微信本地导出与27关导航/43份表现预检通过。
- 最终 Player coverage 995,822 项通过：210 次真实 pointer 搬运、1,150 次来源选择、2,472 次拖动取消、985,088 个人物包络角点、1,664 人次满员场景、48 色彩状态与74张截图。
- 43份元数据仍有22份合法折转；新增人物/楼梯屏幕可见性检查和故意遮挡负例均通过。

## Acceptance Results

| Criterion | Result | Evidence | Owner |
|---|---|---|---|
| AC-F-01 | Pending Human Check | 桌面12种布局通过；真实微信胶囊/安全区/DPR待真机 | Human |
| AC-F-02 | Pass（已执行范围） | 全部交付离线检查＋代表关卡真实pointer、满员人物/楼梯可见性；`artifacts/task005/final-coverage/coverage.json` | Builder / Reviewer |
| AC-F-03 | Pass（桌面范围） | 冒险、教程、每日无步数；内部Moves、移动与撤销保持 | Builder / Reviewer |
| AC-T-01 | Pass | 390×844 / 700×1000 包络、相机稳定、真实点击与拖动取消 | Builder / Reviewer |
| AC-T-02 | Pending Human Check | 六色静态合同与48态Player技术检查通过；真机辨色待验收 | Human |
| AC-T-03 | Pass | 固定机位由交付元数据决定，运行时不搜索；预加载不改变当前镜头 | Builder / Reviewer |
| AC-T-04 | Pass | 仅90°/180°确定性折转、全边/解法/几何/遮挡验证及失败回退 | Builder / Reviewer |
| AC-T-05 | Pass | 目录、现役材质、普通/选择/隐藏/揭示/道具与恢复矩阵通过 | Builder / Reviewer |
| AC-C-01 | Pending Human Check | 技术记录减宽与移动时长；同尺寸真机体验待确认 | Human |
| AC-E-01 | Pending Human Check | | Human |
| AC-E-02 | Pending Human Check | | Human |
| AC-E-03 | Pending Human Check | | Human |
| AC-E-04 | Pending Human Check | | Human |

## Regression Result

Overall Result: Technical Pass in executed Unity / Windows Player / local WeChat export scope. No P0–P3 blocking findings after supplementary review. Real-device and experiential checks remain.

## Fresh Review

Status: Technical Gate Pass（限定已执行的 Unity、Windows Player 与微信本地导出范围）

## Human Experience Check

Status: Pending

---

# Final Decision

Status: Ready for Human Check

## Task Version History

| Version | Date | Change | Reason | Invalidated Outputs |
|---:|---|---|---|---|
| 1 | 2026-09-17 | 创建全局微信 UI、棋盘占屏与颜色可读性改进 Task；从原 TASK-004 重编号为 TASK-005，预览同步迁移 | 真机反馈；用户要求将导航优化保留为 TASK-004 | None；r001 仍为未批准需求预览 |
