# Task: 失败判定与本地关卡进度

Task ID: TASK-001  
Requirement ID: REQ-001  
Requirement Version: 3  
Build Revision: 2  
Status: Verified  
Type: Feature  
Risk: High  
Build Mode: Code Only  
Preview Gate: Reference  
Technical Gate: Fresh Review  
Art Gate: Not Required  
Experience Gate: Human Check  
Created By: PM Orchestrator  
Created At: 2026-09-16

---

<!-- FROZEN_START -->

# Product Contract

## User Request

“加上失败条件；游戏启动进入首页后，载入当前进度关卡，做支持关卡保存的功能。”后续确认：不用求解器，仅当无合法操作或只剩重复操作时判负；不保存每一步；首页不新增选择关卡 icon，现有 icon 色调与当前风格一致；采用已确认的失败弹窗布局；v3 明确去除“当前局面无法继续推进”提示。

## Goal and Player Outcome

### Problem

游戏目前没有失败状态，并且每次启动或返回首页都固定载入第 1 关。玩家即使已经完成后续关卡，也无法从首页继续当前进度；进入没有合法操作或只能回到本次尝试已出现状态的局面后，也只能自行判断并手动重开。

### Expected Outcome

玩家走入没有合法操作，或所有合法操作都只能回到本次关卡尝试中已出现逻辑状态的局面时，会在当前移动完整结束后看到明确的失败结果并可重试或返回主页。游戏在本机保存顺序关卡进度；以后启动或返回首页时，首页展示并从“下一未通关关卡”开始，已完成的进度不会因退出、重玩旧关或失败而丢失。

---

## Scope and Boundaries

### Scope

本轮包含：

- 为本次关卡尝试记录已经出现过的逻辑棋盘状态。当前状态未胜利且不存在任何 `Rules.Preview` 合法操作时判定失败；如果存在合法操作，但每一个合法操作产生的下一逻辑状态都已在本次尝试中出现，也判定失败。
- 失败后立即停止新的棋盘操作，并在当前移动、到达和必要表现完整收尾后显示失败结算；结算提供“重试本关”和“返回主页”。
- 使用本地持久化保存内置顺序关卡的“下一未通关关卡”索引，不保存棋盘布局、逐步操作、撤销历史或道具余量。
- 完成当前进度关卡时持久化推进到下一关；失败、重开、撤销、返回主页、选择或重玩其他关卡不得回退或越级推进进度。
- 游戏启动及返回首页时载入已保存的当前进度关卡作为首页展示；点击“开始”进入该关的初始局面。
- 为失败结算和首页当前关卡状态生成效果预览图，并在任何实现代码修改前取得用户确认。

### Non-goals

本轮明确不处理：

- 不保存或恢复关卡中途的棋盘状态、移动历史、道具使用情况或动画状态。
- 不提供云存档、账号同步、跨设备同步、存档导入导出或服务器存储。
- 不新增进度重置入口；不新增首页选择关卡入口、按钮或 icon，也不修改现有关卡选择器的开放范围。
- 不为自定义关卡保存顺序进度；自定义关卡仍按现有编辑器和关卡库流程运行。
- 不增加步数上限、倒计时、生命值或其他失败条件。
- 不修改关卡内容、解法数据、关卡顺序或现有胜利规则。
- 不在运行时调用 `Solver`，不搜索或证明当前局面是否仍存在最终通关路径。

### Constraints

必须遵守的规则、体验目标或项目限制：

- “无合法操作”定义为：遍历当前关卡所有来源/目标组合后，没有任何 `Rules.Preview(level, state, from, to)` 返回合法。
- “只剩重复操作”定义为：当前状态至少有一个 `Rules.Preview` 合法操作，但每个合法操作经 `Rules.Apply` 得到的 `Rules.Key` 都已存在于本次关卡尝试的已见状态集合，因此当前状态无法直接产生任何新逻辑状态。
- 初次载入或重试关卡时，清空已见状态集合并加入该关初始状态；之后每个实际到达的逻辑棋盘状态只记录一次。Undo 不作为失败判断中的候选操作；重试和重新载入必须开始新的状态集合。
- 完全禁止运行时 `Solver`、通关路径搜索、搜索上限或异步求解。该轻量规则不保证识别所有理论无解局面，也可能在继续通关需要先回到旧状态时判负；用户明确接受这一范围。
- 胜利优先于失败；已解决状态绝不能显示失败。
- 失败锁定后，普通移动、道具、撤销及视图触发的棋盘操作均不得改变局面；只有结算中的“重试本关”或“返回主页”可以继续流程。
- 失败判断只允许枚举当前状态的一步合法操作与结果键，不得阻塞主线程执行多步状态搜索。关卡切换、重开、返回主页或销毁对象时，旧关卡的已见状态不得污染新尝试。
- 首页只提供进入已保存当前进度的主操作，文案采用“继续第 N 关”语义；不得在首页增加选择关卡入口或 icon。
- 首页设置齿轮、旗帜及其他现有/必要图标的色调必须沿用当前游戏视觉语言，收敛为深青灰、米白和柔和珊瑚色，不使用脱离当前风格的高饱和或异色图标。
- 预览图继续作为中文、竖屏、柔和夜空与米色面板的布局和色调参考；失败弹窗保留“本关失败”、当前关卡和两个后续动作，但不得显示“当前局面无法继续推进”提示。v3 文案决定优先于旧预览中的该行文字。
- 存档使用带版本/命名空间的独立键；首次运行或没有旧进度存档时从第 1 关开始。存档缺失、损坏、负数或超出当前关卡目录时必须安全回落到有效索引并可继续游戏。
- 进度只能单调前进。完成最后一关后，当前进度保持为最后一关，首页和“再次挑战”不得循环覆盖为第 1 关。
- 自定义关卡的胜负状态不得读写内置关卡进度。

---

## Acceptance Criteria

- AC-01：未完成关卡的当前状态没有任何 `Rules.Preview` 合法操作时，系统在当前表现收尾后显示失败。
- AC-02：未完成关卡的当前状态仍有合法操作、但每个合法操作的下一 `Rules.Key` 都属于本次尝试的已见状态时，系统显示失败；只要至少一个合法操作可产生未见状态，就不因本规则失败。已解决局面只进入现有胜利流程。
- AC-03：触发失败的移动及其表现完整收尾后，失败面板明确显示“本关失败”和当前关卡，并提供“重试本关”和“返回主页”；不得显示“当前局面无法继续推进”。失败期间点击棋盘、撤销或道具不会改变状态。
- AC-04：选择“重试本关”后从该关初始状态开始，移动数和道具恢复为该关初始值；选择“返回主页”后首页展示已保存的当前进度关卡，且失败不推进进度。
- AC-05：首次运行或没有进度存档时，启动首页展示第 1 关，主操作显示“继续第 1 关”并进入第 1 关初始局面；已有进度时对应显示“继续第 N 关”。首页不出现新增的选择关卡入口或 icon。
- AC-06：完成已保存的当前进度关卡后，存档推进且仅推进一关；关闭并重新启动游戏，首页展示新的当前进度关卡，点击“开始”进入该关初始局面。
- AC-07：重玩旧关、通过关卡选择器载入其他关卡、失败、撤销、重开和返回主页均不会回退或越级推进已保存进度；完成最后一关后重启仍以最后一关为当前进度，不循环到第 1 关。
- AC-08：损坏或越界的本地进度数据不会导致启动异常；系统选择有效关卡并继续运行。自定义关卡的游玩和保存不会改变内置关卡进度。
- AC-09：失败判断只枚举一步合法操作，不运行 `Solver` 或多步通关搜索并保持界面响应；重试、重新载入、切关和返回主页后使用新的已见状态集合，旧尝试不会在新关卡或新尝试上触发失败。Undo 不作为候选操作。
- AC-10：实现前的失败结算与首页进度效果预览图已由用户明确确认；最终实机界面的信息层级、中文文案和主要操作与确认稿一致。首页图标采用当前项目的深青灰、米白、柔和珊瑚色调，不新增选择关卡入口或 icon；失败弹窗保持用户已正向反馈的设计方向。

<!-- FROZEN_END -->

---

<!-- VISUAL_PREVIEW_START -->

# Requirement Preview

Preview Status: Reference  
Preview Artifact: `.harness/previews/failure-and-level-progress/requirement-preview.png`  
Preview Manifest: `.harness/previews/failure-and-level-progress/preview-manifest.json`  
Preview Type: UI Mockup

## Preview Purpose

确认首页当前进度、失败状态、当前关卡和两个后续动作的信息层级，并锁定深青灰、米白与柔和珊瑚色方向。v3 不再采用旧预览中的“当前局面无法继续推进”提示。

## What the Preview Illustrates

- 首页只保留“继续第 N 关”的主操作，不新增选择关卡入口或 icon。
- 失败弹窗的布局、当前关卡、“重试本关”和“返回主页”仍作为参考；旧图中的“当前局面无法继续推进”文字必须移除。
- 现有图标与新界面保持项目当前柔和夜空风格。

## Visual Assumptions

- 最终界面继续使用项目现有运行时 UI 体系。
- 字体、间距与安全区在实机构建中调整，不要求逐像素复制概念图。
- 已批准概念图的失败面板未逐字显示关卡编号；冻结 AC-03 的“显示当前关卡”仍然优先，最终运行时必须补齐。

## What the Preview Does Not Define

- 不定义生产美术资产、算法实现或中途存档。
- 不允许从画面偶然细节推导新玩法或新增选关入口。

## Human Preview Decision

Status: Accepted with v3 text override  
Decision Date: 2026-09-16  
Notes: 用户保留已确认的布局方向，并明确去除“当前局面无法继续推进”提示。

<!-- VISUAL_PREVIEW_END -->

---

## Current Implementation

- Entry point: `StairsGame.InitializeGame()` 初始化关卡目录后调用 `ReturnHome()`；`StairsGame.Awake()` 通过 `WeChatPlatform.Initialize(InitializeGame)` 启动。
- Owning system: `Assets/Scripts/Runtime/StairsGame.cs` 负责首页、载入、移动提交、重开和主要 IMGUI；`TutorialFlow.cs` 负责前几关胜利后的自动过渡；`CloudInterface.cs` 还包含当前运行路径使用的首页与结算界面。
- Related files / scenes / resources: `Assets/Scripts/Core/CrowdRules.cs`、`Assets/Scripts/Runtime/CampaignRepository.cs`、`GameSettings.cs`、`PropInterface.cs`、`TutorialFlow.cs`、`CloudInterface.cs`、相关 Runtime Verification，以及 `Assets/Scenes/StairsCrowd.unity`。
- Current state or data flow: `ReturnHome()` 固定调用 `LoadBoard(0,true)`，`StartGame()` 直接开始首页已载入的棋盘。`Board.TryMove()` 保存撤销快照并提交新状态；`Board.Solved` 由 `Rules.AllGathered()` 计算。教程前段胜利由 `AdvanceTutorialExit()` 自动 `LoadLevel(levelIndex+1)`，普通胜利面板载入下一关。
- Confirmed current behavior: `CampaignRepository.Load()` 返回 20 个内置关卡，前 10 个位置由教程目录覆盖。现有关卡选择器可直接载入目录内关卡；最后一关的现有结算逻辑会循环载入第 1 关。
- Confirmed defect or gap: 没有失败状态或进度所有者；`GameSettings` 和 `IslandEditor` 虽使用 `PlayerPrefs`，但没有内置关卡进度键。
- Available rule support: `Rules.Preview()` 可枚举当前状态的一步合法操作，`Rules.Apply()` 可得到该操作的下一状态，`Rules.Key()` 可稳定标识逻辑棋盘状态；这些纯规则能力足以实现轻量失败判定，无需运行时求解器。

---

## Design and Implementation Plan

推荐实施路径：

1. 先基于当前游戏画面生成两态效果预览：首页只保留“继续第 N 关”主操作，不增加选择关卡入口/icon，并将齿轮、旗帜等图标统一为深青灰、米白和柔和珊瑚色；失败面板显示原因、“重试本关”和“返回主页”。把预览路径及用户确认结果记录到本 Task 的 Builder Result；整体修订稿确认前停止实现。
2. 增加一个小型、版本化的本地进度所有者，集中完成读取、规范化和原子式 `PlayerPrefs.Save()`；以“下一未通关关卡索引”为唯一持久字段。启动和 `ReturnHome()` 均从它取得首页关卡。
3. 将内置关卡胜利推进汇聚到一个幂等入口，覆盖教程自动过渡与普通结算；只在胜利关卡恰好等于当前进度时加一，并在最后一关饱和。
4. 每次载入或重试关卡时创建新的已见状态集合并加入初始 `Rules.Key`；每次棋盘状态实际改变后记录当前键。Undo 可改变当前状态并继续记录/检查，但绝不作为候选操作；重新载入、重试、切关和返回主页不得复用旧集合。
5. 当前状态未胜利时，枚举所有来源/目标组合：用 `Rules.Preview()` 收集合法操作，用 `Rules.Apply()` 和 `Rules.Key()` 判断是否至少能产生一个未见状态。无合法操作，或有合法操作但所有结果均已见时，设置失败；禁止调用 `Solver` 或进行多步搜索。
6. 增加明确失败状态并纳入所有输入/道具/视图与生命周期守卫。失败结果只在对应状态的移动和表现收尾后生效；加入沿用现有视觉语言的失败面板操作。
7. 扩充纯规则和运行时验证，构造“完全无合法操作”“合法操作结果全部已见”“至少一个合法操作结果未见”“胜利优先”和重试/重载清空集合等用例；补测首次启动、胜利推进、重启恢复、重玩不回退、最后一关饱和、损坏存档和自定义关卡隔离。

### Main Change Areas

- `Assets/Scripts/Core/CrowdRules.cs`：复用 `Rules.Preview`、`Rules.Apply` 与 `Rules.Key`，必要时增加局部的一步状态分类辅助，保持纯 C# 规则层且不接入 `Solver`。
- `Assets/Scripts/Runtime/StairsGame.cs` 及相关 partial：失败检查生命周期、状态守卫、首页当前关卡和胜利推进调用。
- `Assets/Scripts/Runtime/CloudInterface.cs` / `TutorialFlow.cs` / `PropInterface.cs`：失败界面、教程推进和道具路径的一致状态处理。
- 新的局部 Runtime 进度所有者（或等价的现有设置扩展）及相关验证脚本。

### Data / State Changes

- 新增一个版本化、命名空间隔离的 `PlayerPrefs` 整数键，值为内置目录中的当前进度索引。
- 运行时新增本次关卡尝试的已见 `Rules.Key` 集合，以及“继续 / 无合法操作 / 只剩重复操作”的轻量结果；这些状态不写入存档。
- 不序列化 `Board.Current`、历史栈、移动数、道具余量或自定义关卡游玩状态。

### Compatibility Handling

- 无新键视为索引 0；读取后对负数、越界值和目录缩短做安全规范化。
- 目录增长时保留原索引；目录缩短或最后一关完成时夹到新的最后一关。
- 存档推进只由内置当前进度关卡胜利触发，避免现有任意关卡选择造成跳关或回退。

---

<!-- EXECUTION_CONTRACT_START -->

# Parallel Build Contract

Contract Status: Ready  
Build Mode: Code Only  
Contract Digest: `sha256:f6ccb6164a2c708b96ff9a70a7b5408752f3146c4e2cab5a50021f994faf3471` (the digest line is normalized before hashing)

## Workstream Ownership

### Code Workstream

- Required: Yes
- Owner: `code_builder`
- Allowed implementation writes: `Assets/Scripts/Core/CrowdRules.cs`, `Assets/Scripts/Core/AttemptFailure.cs`, `Assets/Scripts/Runtime/FailureFlow.cs`, `Assets/Scripts/Runtime/CampaignProgress.cs`, `Assets/Scripts/Runtime/StairsGame.cs`, `Assets/Scripts/Runtime/CloudInterface.cs`, `Assets/Scripts/Runtime/PropInterface.cs`, `Assets/Scripts/Runtime/SettingsInterface.cs`, `Assets/Scripts/Runtime/TutorialFlow.cs`, `Assets/Scripts/Runtime/ViewOrbit.cs`, `Assets/Scripts/Runtime/IslandEditor.cs`, `Assets/Scripts/Runtime/NightPresentation.cs`, `Assets/Scripts/Runtime/ScenePreparation.cs`, `Assets/Scripts/Runtime/FailureProgressRuntimeVerification.cs`, `Assets/Editor/FailureProgressVerification.cs`, `Assets/Editor/UnityBuild.cs`, required `.meta` files, `.harness/runs/TASK-001-r1/code-result.json`, `.harness/runs/TASK-001-r1/integration-result.json`, `Builds/Windows/`, and `artifacts/failure-and-level-progress/`.
- Verification-only generated writes: the existing `UnityBuild.Build()` pipeline may deterministically touch `Assets/Art/Materials/`, project Prefabs, `Assets/Scenes/StairsCrowd.unity`, and `ProjectSettings/`. These paths are not feature scope: record the before/after state and leave no unrelated generated diff in the final result.
- Existing partial implementation in these paths must be audited before reuse.
- Forbidden implementation writes: all `tasks/**`, `versions/**`, `docs/**`, `AGENTS.md`, `.codex/**`, `.harness/previews/**`, 关卡/解法/navigation 资源、SDK、`Packages/**`、凭据、发布配置、字体、hero 图片、shader、未列出的 production art、Unity caches 和无关系统。验证副产物例外不授权有意修改这些文件。

### Art Workstream

- Required: No
- Owner: None
- Allowed writes: None
- Reason: 本功能使用现有运行时 UI、材质与图标语言，不要求新增生产位图或模型资产；批准预览仅作需求参考。

### Integration Workstream

- Required: Yes
- Owner: `code_builder` after code implementation
- Allowed writes: 与 Code Workstream 相同，以及 `.harness/runs/TASK-001-r1/integration-result.json`。
- PM 是 Task 最终结果区的唯一写入者。

## Code Contract

### Required Code Outcomes

- 提供纯一步判定：无合法操作，或所有合法 `Rules.Apply` 结果键均已在本次尝试出现；不得调用 `Solver` 或多步搜索。
- 纯规则分类器先判胜利，再返回 `Continue / Solved / NoLegalMoves / OnlyRepeatedMoves`；预览操作不得污染已见集合，Undo 不删除历史键也不作为候选动作。
- 胜利优先，失败只在当前移动与表现收尾后锁定并展示；失败期间棋盘、Undo、道具和视图触发操作不得改变状态。
- 每次载入、重试、切关和返回主页建立隔离的已见状态生命周期。
- 失败规则也适用于有效的自定义关卡游玩，但自定义关卡的胜负、编辑与保存不得读写内置进度键。
- 用版本化 PlayerPrefs 键保存“下一未通关内置关卡”；值安全规范化、单调推进、末关饱和、自定义关卡隔离。
- 启动、返回主页、教程胜利、普通胜利、重试和结算入口保持一致。
- 首页显示“继续第 N 关”；失败面板保留“本关失败”、当前关卡及“重试本关 / 返回主页”，并移除“当前局面无法继续推进”。
- 为规则、进度、生命周期和输入锁定提供可执行验证证据。
- 验证不得污染开发者真实进度：使用隔离存储或精确保留并恢复本功能的单独键，禁止 `PlayerPrefs.DeleteAll()`。

### Allowed Placeholder Policy

- 不允许新增视觉占位资源。
- 可以复用现有运行时绘制、字体、颜色、面板和 icon 实现。

### Code Constraints

- 保持 `StairsCrowd.Core` 纯 C# 可验证；Unity 持久化和界面留在 Runtime 层。
- 不引入大型框架、网络依赖、云存档或平台专用存储。
- 不修改关卡内容、顺序、解法数据或自定义关卡保存格式。
- 不把预览图片导入 `Assets/`。
- 完整构建命令造成的确定性生成副产物必须与基线对比；未被本功能需要的变化不得留在最终 Diff 中。

## Production Art Contract

- No production art assets required.
- Approved preview remains outside `Assets/` at `.harness/previews/failure-and-level-progress/requirement-preview.png`.

## Code-Art Interface Contract

- None. Runtime code must reuse current project UI/material/icon language recorded in `docs/ART_BIBLE.md`.

## Shared Contract Rules

- Requirement ID `REQ-001`, Requirement Version `3`, Build Revision `2` must match all machine results.
- Builders must not change frozen product outcomes or reinterpret the approved preview.
- Any contradiction requiring a player-facing decision returns `Needs Replan` to PM.

<!-- EXECUTION_CONTRACT_END -->

---

## Verification Plan

### Automated or Technical Checks

- Build / compile: 运行 `UnityBuild.Build`，确认 Windows x64 编译与场景生成成功。
- Tests: 运行 `UnityBuild.Verify` 与 `UnityBuild.VerifyAlternatives`；扩充项目现有纯规则/Runtime verification 入口覆盖 AC-01、AC-02、AC-05 至 AC-09。
- Logs / runtime checks: 验证失败原因可区分“无合法操作”和“只剩重复操作”，并确认重试、重载和切关后没有旧的已见状态泄漏。
- Scene / resource checks: 使用 `Assets/Scenes/StairsCrowd.unity` 在竖屏安全区检查首页与失败面板，无新增丢失引用或生成缓存修改。
- Other: 最终检查 Git diff，只包含本 Task 所需实现、验证及 Task Builder Result；Fresh Review 重点复核一步枚举边界、已见状态生命周期、胜利/失败优先级和存档单调性，并确认运行时不存在 `Solver` 调用。

### Manual Playtest

- HC-01：对照用户确认的预览图，检查首页能一眼看出并继续当前关卡、不出现新增选关入口/icon，图标色调与项目一致；失败原因和两个动作清晰、可点且没有遮挡核心信息。
- HC-02：实际走入“无合法操作”和“合法操作只会产生已见状态”的局面，确认失败出现时点自然，不截断行走/到达表现，也不会让玩家误以为按钮失效。
- HC-03：完成关卡、返回首页并重启游戏，确认继续进度符合预期；失败后重试和回主页流程没有突兀跳转。

---

## Assumptions and Risks

### Confirmed Facts

- 用户最终将失败规则简化为“不用求解器，仅当无合法操作，或只剩重复操作时判负”；“只剩重复操作”按所有合法操作结果均属于本次尝试已见逻辑状态落实。
- 用户确认不保存每一步；失败后立即锁定棋盘，并采用“重试本关 / 返回主页”的默认流程。
- 用户反馈首页不需要选择关卡 icon/入口，首页图标色调需与当前项目风格一致；用户对失败弹窗方向给出正向反馈。
- 当前纯规则层已有 `Rules.Preview`、`Rules.Apply` 和 `Rules.Key`，可完成一步合法操作及重复状态判断。
- 当前首页固定载入索引 0，现有 PlayerPrefs 没有内置关卡进度。

### Assumptions

- “当前进度”定义为内置顺序目录中的下一未通关关卡，而不是最近打开或最近游玩的关卡。
- 关卡选择器保持现有开放范围；它只改变本次游玩关卡，不直接改变持久进度。
- 本轮仅本地保存；微信小游戏与 Windows/Editor 均通过 Unity `PlayerPrefs` 使用相同语义。

### Risks

- 轻量重复规则有意不判断理论可解性：它会漏掉仍可进行新操作但实际上无解的局面，也可能在必须先回到旧状态才能走新分支时提前判负；这是用户明确接受的取舍，界面文案不得暗示系统执行了完整求解。
- 教程自动过渡、普通胜利面板、关卡选择、自定义关卡和道具均有独立入口，遗漏任何入口都可能导致进度错写、过期失败或输入未锁定。
- 现有最后一关按钮会循环到第 1 关，需要在不改变关卡目录的情况下与“进度饱和在最后一关”保持一致。

### Open Questions

- None. 用户已于 2026-09-16 确认修订效果预览；项目内归档为 `.harness/previews/failure-and-level-progress/requirement-preview.png`。

---

<!-- CODE_RESULT_START -->

# Code Build Result

Status: Code Ready for Integration

## Implementation Summary

已移除中断版本中的运行时 Solver/不可解证明方案，保留原有离线 Solver。新增纯一步失败分类器、本次尝试已见状态生命周期、失败输入锁定与表现收尾、版本化本地进度、首页当前关卡加载，以及真实 Windows Player 运行时验证。v3/r2 删除“当前局面无法继续推进”提示，保留“本关失败”、当前关卡和两个动作，并收紧弹窗高度。

## Changed Areas

- Core: 新增 `AttemptFailure`，仅使用 `Rules.Preview`、`Rules.Apply`、`Rules.Key`。
- Runtime: 新增/更新失败流程、进度所有者、首页/结算、道具、设置、教程与视图守卫。
- Verification: 新增 Editor 规则/存档验证和 Windows Player `-failureprogresstest` 真实运行链路。

## Plan Deviations

- 未新增生产美术；运行时 UI 复用现有程序绘制、字体和配色。
- 首轮 Review 发现运行时证据不足及自定义关卡失败按钮偏差；修正后复审通过。

## Baseline

| Check | Before Change | Notes |
|---|---|---|
| Git status | Existing dirty Task implementation | 保留并定向修正中断实现，没有清空用户工作区。 |
| Build / compile | Pass | Windows 64 位构建成功。 |
| Relevant tests | Pass | Unity Verify、失败/进度验证、Windows Player 运行时验证通过。 |

## Acceptance Evidence

| Criterion | Result | Evidence |
|---|---|---|
| AC-01 | Pass | 纯分类器覆盖无合法操作；Player 验证确认失败立即锁定且 Busy 结束后才显示。 |
| AC-02 | Pass | 覆盖全重复、存在未见结果和胜利优先三种互斥结果。 |
| AC-03 | Pass | Player 验证覆盖移动、Undo、道具、设置、视图锁定；断言“本关失败”、当前关卡和两个按钮存在，且被删除提示不存在。 |
| AC-04 | Pass | Player 验证覆盖 Retry/Load/Home 新尝试隔离与初始状态恢复。 |
| AC-05 | Pass | 空存档为第 1 关，首页显示“继续第 N 关”，未新增选关入口/icon。 |
| AC-06 | Pass | 自动验证覆盖单调推进与重新初始化读取；用户于 2026-09-16 完成人工确认，包括关闭重开后的当前进度体验。 |
| AC-07 | Pass | 覆盖旧关/未来关/重复完成不推进及末关饱和。 |
| AC-08 | Pass | 覆盖缺失、负数、超界与自定义关卡隔离；验证使用独立 key。 |
| AC-09 | Pass | 静态与运行时确认无 Solver/多步搜索；覆盖尝试集合隔离和 Undo 历史语义。 |
| AC-10 | Pass | 批准预览已归档；v3 精简弹窗的最终视觉、信息层级与实际操作已由用户于 2026-09-16 确认。 |

## Verification Results

- `UnityBuild.Verify` r2: Pass；20 个关卡、49 个既有解法动作及新增失败/进度验证通过。
- `UnityBuild.Build` r2: Pass；`Builds/Windows/StairsCrowd.exe` 已更新。
- Windows Player `-failureprogresstest` r2: Pass，退出码 0；`failureCopy=required-only`，`removedHintAbsent=true`。
- Code/Integration result JSON 均通过 v0.4 Schema。
- 构建重写的既有材质、Prefab、Scene 与 ProjectSettings 已恢复基线；额外生成的 `Pastel-16` 至 `Pastel-40` 已删除。

## Human Check

Status: Preview Approved; Implementation Playtest Pending

Checklist:

- 确认首页只保留“继续第 N 关”，没有新增选择关卡入口/icon，且图标色调与当前项目一致。
- 最终确认已获正向反馈的失败文案、失败面板层级以及“重试本关 / 返回主页”按钮布局。
- 实现完成后按 HC-01 至 HC-03 进行实机检查。

## Remaining Risks

- 已见状态集合在长时间单局中的增长需要在验证中观察，但不得通过运行时多步搜索替代本 Task 的一步规则。
- 微信小游戏目标平台的 PlayerPrefs 持久化需要在正式发布准备时再按目标平台验证。

<!-- CODE_RESULT_END -->

---

<!-- ART_RESULT_START -->

# Art Build Result

Status: Not Required  
Manifest: Not Required

## Assets Produced

None. 本 Task 复用现有运行时 UI 与项目视觉语言。

## Production Method and References

Approved requirement preview only; it remains outside `Assets/`.

## Technical Asset QA

Not Required.

## Visual Assumptions or Deviations

None recorded.

## Failed or Not Verified

None.

## Remaining Art Risks

最终运行时布局与色调仍需 Human Check。

<!-- ART_RESULT_END -->

---

<!-- INTEGRATION_RESULT_START -->

# Integration Result

Status: Integrated

## Integrated Assets and Bindings

No new production assets. Approved preview remained outside `Assets/`; runtime UI reuses current drawing, font, material and icon language.

## Final Changed Areas

- `Assets/Scripts/Core/AttemptFailure.cs`
- `Assets/Scripts/Runtime/CampaignProgress.cs`
- `Assets/Scripts/Runtime/FailureFlow.cs`
- Existing runtime input, tutorial, home and result paths
- Editor and Windows Player verification entry points

## Acceptance Results

AC-01～05、AC-07～09 技术通过；v3 删除提示的反向文案断言通过。AC-06 的跨进程重启体验和 AC-10 的最终视觉/操作已通过 Human Check。

## Build and Runtime Evidence

- Unity Verify r2: Pass.
- Windows 64-bit build r2: Pass.
- Windows Player runtime verification r2: Pass, exit code 0.
- Fresh Review r2: Review Passed.

## Failed or Not Run

- 微信小游戏目标平台的持久化仍留待正式发布准备阶段验证；本 Task 的 Windows 人工体验 Gate 已通过。

## Remaining Integration Risks

- 轻量重复状态规则按合同可能在必须回到旧状态时提前判负。
- 关闭并重新启动后的完整进度体验仍需 HC-03。

<!-- INTEGRATION_RESULT_END -->

---

<!-- ART_APPROVAL_START -->

# Final Art Check

Status: Not Required

## Build Context

No new production art assets.

## Check Items

Covered by Human Experience Check.

## Human Art Feedback

Not Required.

## Decision

Not Required.

<!-- ART_APPROVAL_END -->

---

<!-- REVIEW_RESULT_START -->

# Fresh Review Result

Status: Review Passed

## Findings

- 首轮发现运行时链路证据不足，以及自定义关卡失败按钮错误显示“返回编辑”。
- Builder 新增实际 Windows Player 验证并统一失败按钮为“返回主页”。
- 第二轮确认两项 Finding 已关闭；运行时不存在 Solver/多步搜索，未发现新增明显功能问题或无关游戏资源差异。
- Review 边界：进程内首页重载已验证，真正关闭并重启与最终视觉仍属于 Human Check。
- v3/r2 复审确认删除提示、保留必要文案且未改变失败算法、进度或输入锁定；结论 `Review Passed`。

## Gate Decision

Review Passed. Technical Gate complete.

<!-- REVIEW_RESULT_END -->

---

<!-- HUMAN_CHECK_START -->

# Human Experience Check

Status: Accepted

## Build Context

Use `Builds/Windows/StairsCrowd.exe`. Technical verification and Fresh Review have passed; do not use `-failureprogresstest` for normal play.

## Setup

Use the verified playable build in portrait layout and compare with the approved requirement preview.

## Check Items

### HC-01 — 首页进度与视觉

确认首页能一眼看出并继续当前关卡，不出现新增选关入口/icon，图标与界面保持深青灰、米白、柔和珊瑚色。

### HC-02 — 失败反馈

分别走入“无合法操作”和“只剩重复操作”的局面；确认表现收尾后才出现“本关失败”，不显示“当前局面无法继续推进”，当前关卡与两个动作清晰，失败期间其他操作不改变棋盘。

### HC-03 — 进度闭环

完成关卡、返回主页并重启；确认继续进度正确。失败后重试与返回主页不推进或回退进度。

## Human Feedback

用户于 2026-09-16 确认最新 v3/r2 构建体验完毕，没有提出进一步调整。

## Accepted Values

- 首页继续当前关卡的信息层级与操作。
- 失败弹窗仅显示“本关失败”、当前关卡、“重试本关 / 返回主页”。
- 删除“当前局面无法继续推进”后的紧凑布局。
- 关闭重开后的本地进度体验。

## Decision

Accepted. Experience Gate complete; Task is Verified.

<!-- HUMAN_CHECK_END -->
