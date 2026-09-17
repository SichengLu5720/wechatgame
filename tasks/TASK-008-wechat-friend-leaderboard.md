# Task: 微信好友排行榜

Task ID: TASK-008  
Task Version: 1  
Status: Ready for Human Check  
Harness Mode: Strict  
Type: Feature  
Risk: High  
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
| CL-001 | 本轮排行榜范围 | 只实现微信好友排行，不实现铃铛排行、全服排行或其他榜单。 | User | None | Scope, UI, data source |
| CL-002 | 排名成绩 | 按累计通关关数倒序，界面显示“已通关 N 关”。 | User | None | Score model, copy, upload |
| CL-003 | 同分排序 | 同一通关数下，先达到该成绩的玩家排在前面。 | User | None | Tie-break data and sort |
| CL-004 | 入口与榜单结构 | 首页提供排行榜入口；弹窗只展示好友排行；每行展示名次、微信头像、昵称、通关数；玩家本人名次固定在底部。 | User | None | Navigation and layout |
| CL-005 | 授权或接口失败 | 仍展示排行榜框架，头像昵称降级为默认头像与“微信玩家”，并提供重试。 | User | None | Error and privacy states |
| CL-006 | 平台与验收 | 接入真实微信好友排行榜及成绩上传；Windows/Editor 提供模拟数据用于本地验收。 | User | None | Platform adapter and tests |
| CL-007 | 旧存档没有首次达成时间，且不能区分最后一关已解锁/已完成 | 保守迁移：旧存档按能证明的通关数入榜，首次升级并同步时间作为达成时间；旧存档最多先记 19 关，更新后再次完成最后一关才记为 20 关。 | User | None | Migration, historical score and tie-break |
| CL-008 | 微信无原子条件写入与可信服务端时钟 | 接受普通单设备流程保证“先达到者优先”；设备时钟偏差或多设备同时写入时不承诺绝对真实先后，界面不夸大精度。 | User | None | Tie-break accuracy and multi-device consistency |
| CL-009 | Implementation Preview r001 | 批准首页设置下方次级入口、单页好友榜、列表内本人保留并重复显示固定摘要、现有色调与各状态方向。 | User | None | UI layout and state presentation |

未解决的阻断问题：

- None.

---

<!-- FROZEN_START -->

# Frozen Requirement

## Original Request

实现一个排行榜功能，目前只加好友排名，交互逻辑参考用户提供的两张界面截图。用户随后确认全部采用 PM 建议，并明确同分时先达到对应成绩的玩家排在前面。

## Problem

当前游戏只有本地关卡进度，没有让玩家查看微信好友通关进度并进行轻量比较的入口，也没有向微信好友排行榜同步成绩的链路。

## Goal and Player / User Outcome

- 玩家可以从首页进入好友排行榜，快速看到自己与微信好友的累计通关进度和相对名次。
- 排名结果稳定、可理解：通关数更高者靠前，同分时更早达到该成绩者靠前。
- 微信授权、数据或网络不可用时，界面明确降级且可重试，不阻断游戏主流程。
- 开发者可以在 Windows/Editor 使用模拟好友数据完整检查榜单交互与视觉状态。

## Core Rules and Confirmed Decisions

- 本轮仅有“好友排行”一个榜单，不显示或预留可操作的其他榜单页签。
- 排名主值为累计通关关数，按降序排列，显示文案为“已通关 N 关”。
- 同一通关数下，按首次达到该通关数的时间升序排列，先达到者靠前。
- 排行榜行至少包含名次、头像、昵称和通关数。
- 当前玩家的排行摘要固定显示在榜单底部；若本人已出现在当前可见列表中，具体去重表现由设计保证不造成名次误读。
- 首页提供排行榜入口，打开后为模态界面，玩家可明确关闭并回到原首页状态。
- 头像昵称不可用时使用默认头像和“微信玩家”；数据读取失败时提供可理解的失败反馈和重试操作。
- 玩家有效通关进度提高后向微信侧同步排行榜成绩；成绩不得因重玩旧关、失败、自定义关卡或异常读取而倒退或被重复累加。
- Windows/Editor 不访问真实微信好友数据，使用可控模拟数据覆盖常见名次、长昵称、空榜、本人榜外、失败与重试状态。
- 旧存档按能证明的通关数迁移，首次升级并同步时间作为该历史成绩的达成时间；由于旧存档无法区分末关已解锁/已完成，旧存档最多先记 19 关，更新后再次完成末关才记为 20 关。
- “先达到者优先”在普通单设备流程内保证；微信无原子条件写入和可信服务端时钟，设备时钟偏差或多设备同时写入时不承诺绝对真实先后。

## Scope

本轮包含：

- 首页排行榜入口及好友排行榜模态界面。
- 微信好友排行榜数据读取、成绩上传与必要的平台桥接。
- 好友头像、昵称、名次、通关数和本人固定排行摘要。
- 主排序和同分先到先得规则所需的数据记录与兼容处理。
- 加载、空榜、授权/资料不可用、接口失败、重试和关闭状态。
- Windows/Editor 模拟数据与自动/人工验证入口。
- 与现有本地内置关卡进度完成链路的接入。

## Non-goals

本轮明确不处理：

- 全服排行、陌生人排行、铃铛/货币排行、每日挑战排行、赛季榜或分组榜。
- 好友邀请、加好友、点赞、奖励领取、炫耀分享或排行榜作弊治理后台。
- 云存档、跨设备恢复完整游戏进度或通用账号系统。
- 复刻参考游戏的角色、图标、粗黑描边、色彩和品牌视觉。
- 正式上传、提审、发布或生产配置变更。

## Constraints

- 目标平台为微信小游戏；必须沿用项目现有 Unity WebGL/微信 SDK 集成，不把 AppID、密钥或用户敏感数据写入仓库、日志、Task 或截图。
- 微信好友数据的获取和绘制必须符合平台当前可用能力及隐私约束；若已确认产品规则与平台能力冲突，必须停止并澄清，不得伪造真实好友数据。
- UI 必须遵循 `docs/ART_BIBLE.md`：深青灰、暖米白、柔和珊瑚/薄荷点缀、圆角和克制层级；参考图仅用于信息结构和交互逻辑。
- 竖屏、安全区、微信胶囊区域和不同分辨率下入口、关闭按钮、滚动区域及本人固定栏均需可见可点。
- 排行榜不得阻断首页开始游戏、设置、关卡进度、失败流程、每日挑战、自定义关卡与既有分享功能。
- 对成绩和首次达成时间的写入必须单调、安全；读取异常不得降低本地进度或覆盖更高的已上传成绩。
- 多设备并发与设备时钟偏差属于已接受的平台限制；实现和界面不得声称具备服务端原子排序或绝对时间精度。
- 正式微信目标的能力与接口应以实现时官方文档和项目实际 SDK 为准。

## Accepted Assumptions

- “累计通关关数”指内置主线按进度首次完成的关卡数量，不通过重复完成旧关累加；每日挑战和自定义关卡不计入。
- 榜单以好友可见范围返回的数据为事实来源；无可见好友时仍显示玩家本人固定栏和空榜提示。
- 旧存档迁移的历史达成时间采用功能首次同步时间，而非尝试伪造真实历史时间。

## Acceptance Criteria

| ID | Type | Criterion | Verification Owner | Required Evidence |
|---|---|---|---|---|
| AC-F-01 | Functional | 首页可打开并关闭好友排行榜；关闭后首页状态和当前关卡进度不变。 | Builder | Runtime verification and screenshots |
| AC-F-02 | Functional | 好友按通关数降序排列；同分按首次达到该成绩的时间升序排列，名次和本人固定栏一致。 | Builder / Reviewer | Deterministic sort tests covering ties |
| AC-F-03 | Functional | 每行显示名次、头像、昵称与“已通关 N 关”；列表可滚动且本人榜外时仍可见固定摘要。 | Builder | Runtime verification with simulated datasets |
| AC-F-04 | Functional | 内置主线进度首次提高时同步更高成绩和达成时间；重玩、失败、每日挑战、自定义关卡及较低值不会累加或回退。 | Builder / Reviewer | Progress/upload boundary tests |
| AC-F-05 | Functional | 加载、空榜、资料不可用、接口失败与重试均有明确状态；失败不阻断游戏主流程。 | Builder | Simulated runtime state coverage |
| AC-T-01 | Technical | 微信构建使用平台允许的好友数据路径，真实好友数据不越过平台隐私边界；凭据和敏感数据不进入仓库或日志。 | Builder / Reviewer | Source review, build evidence, platform test plan |
| AC-T-02 | Technical | Windows/Editor 使用隔离的模拟适配器，不调用微信接口，并能确定性覆盖排序和主要 UI 状态。 | Builder | Editor/Windows runtime evidence |
| AC-T-03 | Technical | 竖屏安全区与微信系统区域下入口、弹窗、滚动列表、关闭按钮和本人固定栏无关键遮挡。 | Builder / Reviewer | Multi-resolution screenshots and geometry checks |
| AC-C-01 | Comparative | 相比参考图，保留“首页入口 → 好友榜 → 列表 + 本人固定栏”的信息逻辑，同时视觉明确属于《群岛》。 | Human | Approved preview and playable comparison |
| AC-E-01 | Experiential | 玩家能快速理解比较指标、自己的位置和关闭/重试方式；榜单不会显得像第三方游戏界面的复制品。 | Human | Human judgement in playable build |

## Test Proxies

| Proxy ID | Proxy | Supports Criterion | Limitation |
|---|---|---|---|
| TP-001 | 在 700×1000、窄屏和带微信系统区域的布局中，所有关键控件矩形保持在安全区且最小触控范围达标。 | AC-T-03, AC-E-01 | 不能独立证明实机可读性和操作自然度 |
| TP-002 | 模拟数据覆盖至少 15 行、三组同分、本人榜内/榜外、长昵称及接口失败。 | AC-F-02, AC-F-03, AC-F-05 | 不能替代真实微信好友数据验证 |

<!-- FROZEN_END -->

---

# Design

## Current Implementation

- `CampaignProgress` 只保存 `next-built-in-level`，范围为 `0..levelCount-1`，没有独立完成数或达成时间；索引 19 同时表示“到达最后一关”和“已完成最后一关”。
- 有效主线完成写入点为 `FailureFlow.EvaluateCurrentBoard → RecordBuiltInVictory → CampaignProgress.RecordCompletion`；每日挑战和自定义关卡已在该链路排除。
- 首页由 `CloudInterface` 使用 IMGUI 绘制；`SettingsInterface.InterfaceBlocksInput` 尚无排行榜模态状态。
- `GameplayLayout`、`WeChatDisplay` 已提供安全区、微信胶囊和显示修订能力，可复用。
- 当前 SDK 已提供开放数据上下文、消息、显示和隐藏接口；但 `UseFriendRelation=0`，导出会删除开放数据域。
- SDK 自带榜单示例包含随机写分、好友对象日志、单值排序和头像识别本人，不可直接启用，必须替换为自有实现。

## Recommended Design

1. 新增独立版本化排行榜进度 `{completedCount, achievedAtMs}`，保留原 `CampaignProgress` 的继续游戏语义；仅从有效主线胜利入口单调更新，显式支持 20/20。
2. Unity 主域只发送自己的成绩、布局和生命周期命令；好友读取、解析、排序、头像、本人识别、列表及固定栏均在微信开放数据域绘制，好友明细不回传 C#。
3. 云同步先读取自己的云记录，再执行单调合并；低分不覆盖高分，同分不重写首次时间；读取失败不得当空记录覆盖。
4. 首页入口、遮罩、标题和关闭按钮沿用项目 IMGUI；共享画布负责榜单内容。排行榜加入统一输入守卫，关闭时防止点击穿透。
5. Windows/Editor 使用隔离模拟适配器和固定夹具，覆盖排序、同分、长昵称、空榜、本人榜外、错误和重试。
6. 微信构建脚本开启好友关系链并部署项目自有开放数据域；构建校验必须排除 SDK 示例随机写分、好友日志和无关群榜功能。

## Main Change Areas

- 新增 `FriendLeaderboard*` 运行时状态、记录、布局、模拟和验证代码。
- 新增 `WeChatLeaderboard` 条件编译平台桥接。
- 局部接入 `CampaignProgress`、`FailureFlow`、`CloudInterface`、`SettingsInterface`、`StairsGame`、`EditorEntrance`。
- 新增项目自有微信开放数据域 JS/Canvas2D 实现。
- 更新 `WeChatBuild`，确保导出保留自有开放数据域并启用好友关系链。

## Technical Options

- 推荐自有轻量 Canvas2D 开放数据域，不引入 SDK 示例 Layout 插件和示例逻辑。
- 云记录使用独立整数成绩与毫秒时间字段，不把时间压入浮点复合分数。
- 本人识别使用专属随机标记，禁止使用头像或昵称相等判断；无法可靠识别时不得伪造准确名次。

## Proposed Product Decisions

- None.

## Workstream Ownership

| Workstream | Required | Allowed Write Paths | Forbidden Paths / Shared Files |
|---|---:|---|---|
| Code Builder | Yes | `Assets/Scripts/Runtime/FriendLeaderboard*.cs`, `WeChatLeaderboard.cs`, 已列明的现有运行时接入文件, `Assets/WeChat/LeaderboardOpenData/`, `Assets/Editor/WeChatBuild.cs`, 排行榜验证文件 | Task、预览目录、SDK/vendor、无关生产资源 |
| Art Asset Builder | No | None | 全部项目文件 |
| Final Integration | No | None | 全部项目文件 |

## Code–Art Interface

Not Required. 本 Task 计划使用现有运行时 UI 图形语言和代码绘制，不生产独立美术资产。

## Asset Contract

Art Bible: `docs/ART_BIBLE.md`  
Approved Preview: Pending  
Asset Manifest: Not Required

Not Required.

---

# Visual Direction

Preview Status: Accepted  
Latest Approved Preview: `.harness/previews/TASK-008/r001/implementation-preview.png`

## Accepted Visual Decisions

- 只借鉴参考图的入口、好友列表和本人固定栏逻辑，不复刻其角色、品牌、色彩或粗黑描边风格。
- 排行榜遵循项目现有深青灰、暖米白、柔和珊瑚/薄荷和圆角留白语言。
- 信息优先级为：榜单身份与关闭 → 好友排名列表 → 本人固定排名 → 状态反馈/重试。
- 首页排行榜入口位于设置按钮下方，作为深青灰次级入口。
- 本人在可见列表中保留原排行行，同时在底部重复显示带“我”标识的固定摘要，两处名次与成绩一致。
- r001 的加载、空榜、资料降级、失败重试和安全区方向获准作为实现基准。

## Rejected Directions

- 不显示铃铛榜或其他榜单页签。
- 不复制参考图的高饱和彩虹、贴纸、厚黑线和专有图标。

## Open Visual Decisions

- 由 Implementation Preview 基于现有首页真实布局确定入口位置、弹窗占屏比例、滚动密度和本人固定栏去重表现。

## Preview History

| Revision | Type | Artifact | Decision | Feedback / Changes |
|---:|---|---|---|---|
| r001 | Implementation | `.harness/previews/TASK-008/r001/implementation-preview.png` | Accepted | 用户于 2026-09-17 批准；覆盖入口、列表、滚动、本人栏、资料降级、加载、空榜、失败重试和安全区压力状态 |
| r002 | Integrated Visual QA | `.harness/previews/TASK-008/r002/visual-comparison.png` | Pass (Windows screenshots) | 两种分辨率 18 张运行截图符合批准方向；真实微信字体、头像、共享画布、触控与前后台仍待真机 Human Check |

---

# Regression Plan

## Protected Behaviors

- RB-01：首页继续当前关卡、开始游戏、设置和既有入口行为不变。
- RB-02：内置主线进度保持单调，失败、重玩、每日挑战和自定义关卡不错误推进。
- RB-03：微信初始化、窗口尺寸/安全区刷新、后台前台切换和 60 FPS 设置不被破坏。
- RB-04：排行榜打开时阻止底层首页误触，关闭后恢复正常输入。

## Impacted Systems

- 首页 UI 与模态输入守卫。
- `CampaignProgress` 完成记录链路。
- `WeChatPlatform` 平台桥接及微信构建。
- Windows/Editor 验证与模拟数据。

## Baseline Checks

修改前应记录：

- Unity Verify 和 Windows 构建/运行基线。
- 首页、安全区、设置及继续进度的现有运行截图和行为。
- 微信导出能否在当前工作区完成；不能完成时记录真实原因，不将其视为本 Task 引入。

## Targeted Regression Checks

修改后必须重测：

- 排序、同分首次达成、成绩单调写入和进度来源边界。
- 首页打开/关闭、列表滚动、本人固定栏、加载/空榜/失败/重试。
- 多分辨率安全区、系统胶囊和底层输入拦截。
- 主线完成、返回首页、失败重试、每日挑战、自定义关卡和设置流程。
- 微信目标编译/导出与真实设备验证计划。

## Core Smoke Path

1. 启动进入首页，打开排行榜并检查模拟/真实好友数据。
2. 滚动列表、确认本人固定栏，关闭后继续当前关卡。
3. 完成一个新的主线关卡，返回榜单确认成绩只提高一次且同分时间规则正确。
4. 模拟失败后重试，确认主流程不受阻断。

## Visual Regression Checks

- 对比批准的 Implementation Preview 检查信息层级、色调、入口位置、列表密度和本人固定栏。
- 700×1000、窄屏、带顶部/底部安全区场景下检查无关键遮挡和正文截断。

## Known Pre-existing Issues

- 工作区已有大量来自其他 Task 的未提交修改；Builder 必须只触碰批准路径并分类 diff，不得回滚或覆盖。

---

# Build and Verification Results

> 本区域由 PM 根据 Subagent 返回结果更新。Builder 不直接修改 Task。

## Code Result

Status: Code Ready

- Implementation Summary: 已实现独立版本化好友榜成绩、旧存档保守迁移、19→20 末关记录、主线胜利单调更新、首页入口与完整模态状态、Windows/Editor mock、微信主域桥接、自有开放数据域 Canvas、先读后合并、请求超时恢复、迟到回调隔离、未决写入保护、本人固定栏、输入守卫及导出部署。
- Changed Areas: 新增 `FriendLeaderboard*`、`WeChatLeaderboard`、`Assets/WeChat/LeaderboardOpenData/` 和 `FriendLeaderboardVerification`；局部接入 `CampaignProgress`、`FailureFlow`、`CloudInterface`、`SettingsInterface`、`EditorEntrance`、`StairsGame`、`WeChatBuild`。
- Baseline Result: Unity Verify、失败进度 runtime 与每日挑战 runtime 通过；原 RuntimeTuning 构建后哈希检查为既有不确定项，本 Task 使用独立构建入口完成最终验证。
- Commands / Tests Run: Unity Verify、排行榜定向验证、Windows 构建、700×1000/390×844 runtime、失败进度、每日挑战、JS model/runtime/语法、开放域部署 fixture、diff/checksum。
- Plan Deviations: `STAIRS_WECHAT_APPID` 缺失，因此未执行真实微信导出、上传或真机验证；fixture 不替代真实平台。
- Waiting for Art / Integration: None.
- Remaining Risks: 真实微信好友读写、头像、共享画布触摸、前后台和设备安全区待真机；已接受设备时钟与多设备并发边界。

## Art Result

Status: Not Required

- 无新增生产美术；使用现有运行时 UI 图形语言。

## Integration Result

Status: Integrated; Local Verification Passed

- Integration Summary: 排行榜已接入首页、主线胜利进度和微信平台桥接；开放数据域保持好友隐私边界。
- Build / Runtime Result: `Builds/FriendLeaderboard/StairsCrowd.exe` 构建通过；两种竖屏 runtime 各 42 checks 通过。
- Scene / Resource Binding Result: 无场景或生产美术绑定；开放数据域部署 fixture 通过。
- Core Smoke Result: 首页→榜单→滚动→关闭→继续、失败进度及每日挑战回归通过。
- Git Diff Review: 构建前后 15 个生产/接入文件 SHA-256 一致；TASK-006 的运动相关改动保留并排除在本 Task 成果之外。
- Remaining Risks: 未进行真实微信导出/真机链路和最终 Human Check。

## Acceptance Results

| Criterion | Result | Evidence | Owner |
|---|---|---|---|
| AC-F-01 | Pass | 700×1000 / 390×844 runtime 打开、关闭、输入守卫与首页恢复检查通过。 | Builder / Reviewer |
| AC-F-02 | Pass | C# 与 JS 定向测试覆盖成绩降序、同分时间升序、首次时间不重写和本人名次一致。 | Builder / Reviewer |
| AC-F-03 | Pass | 两种分辨率各 42 checks；正常、滚动、榜内/榜外本人固定栏和长昵称截图通过。 | Builder / Visual QA |
| AC-F-04 | Pass | 覆盖保守迁移、0→1、19→20、重玩、失败、每日挑战、自定义关卡、云高分保留与未决写入。 | Builder / Reviewer |
| AC-F-05 | Pass | 加载、空榜、资料降级、失败、重试、无回调超时恢复及迟到回调隔离测试通过。 | Builder / Reviewer |
| AC-T-01 | Not Verified | 源码/fixture 证明架构遵循开放数据域隐私边界，但未完成真实微信导出和真机好友读写。 | Builder / Reviewer |
| AC-T-02 | Pass | Windows/Editor 隔离 mock 与确定性夹具通过。 | Builder |
| AC-T-03 | Pass | 700×1000、390×844 和模拟安全区截图/几何检查通过；真实设备仍列 Human Check。 | Builder / Visual QA |
| AC-C-01 | Pass | 用户于 2026-09-17 确认本地实现与批准视觉方向。 | Human |
| AC-E-01 | Pass | 用户于 2026-09-17 确认本地可玩体验；真实微信共享画布体验仍由 AC-T-01 平台 Gate 覆盖。 | Human |

## Regression Result

Overall Result: No New Regression Found

在实际执行的基线、定向回归和核心流程范围内，没有发现由本次修改引入的新回归。

| Finding | Classification | Evidence | Resolution |
|---|---|---|---|
| 无回调请求会永久卡住重试 | Introduced | Fresh Review 初审与 Node VM 复现 | 加入请求级超时、代次失效、迟到回调隔离和测试，复审通过 |
| 空榜资料降级缺少重试 | Introduced | Fresh Review 初审 | 空榜纳入资料重试并覆盖真实触摸事件，复审通过 |
| Standalone SDK 编译检查符号失败 | Introduced | 保留的失败构建日志 | 移除无效宏，改用 Editor SDK 签名检查；当前源码重建通过 |
| `RuntimeTuning.cs` 基线哈希异常 | Pre-existing | 修改前独立构建基线 | 未触碰该文件；最终使用本 Task 独立构建入口 |
| TASK-006 运动代码并行变化 | Uncertain | StairsGame 定向前后对比 | 原样保留，不计入本 Task，当前源码重新终验通过 |

## Fresh Review

Status: Review Passed

- Scope Reviewed: 迁移与末关记录、单调合并、超时/迟到回调、开放数据域隐私、本人识别、输入守卫、部署和证据完整性。
- Findings: 初审两项 P2 与一次验证路径失败均已修复；复审未发现修复引入的新回归。
- Required Action: 真实微信导出和真机好友链路仍必须由 Human Check 完成，不能由本地 fixture 替代。

## Human Art Approval

Status: Not Required

## Human Experience Check

Status: Accepted

- Test Setup: 使用 `Builds/FriendLeaderboard/StairsCrowd.exe` 做本地体验；使用微信开发者工具/真机做真实开放数据域、好友读写、头像、触摸、安全区和前后台验证。
- Feedback: 用户于 2026-09-17 回复“确认 上传微信”，接受本地实现并授权进入微信上传准备。
- Accepted Values: r001 Implementation Preview、本地排行榜入口与交互、r002 Integrated Visual QA 结果。
- Remaining Platform Gate: 微信开发者工具/真机的真实好友数据、头像、共享画布触摸、安全区和前后台行为仍属 AC-T-01，上传前后不得伪称已验证。

---

# Final Decision

Status: Ready for Human Check

## Task Version History

| Version | Date | Change | Reason | Invalidated Outputs |
|---:|---|---|---|---|
| 1 | 2026-09-17 | Initial draft | 用户确认好友榜范围、排序、界面、降级和本地验收方案 | None |
