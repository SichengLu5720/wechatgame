# Task: 局内人物建模与高精度行走动画

Task ID: TASK-003  
Task Version: 5  
Status: Ready for Human Check  
Harness Mode: Strict  
Type: Art  
Risk: High  
Build Mode: Code Only  
Preview Mode: Both  
Technical Gate: Fresh Review  
Art Gate: Human Art Approval  
Experience Gate: Human Check  
Created By: PM Orchestrator  
Created At: 2026-09-17

---

# Clarifications and Decisions

| ID | Question / Unclear Item | Confirmed Decision | Confirmed By | Task Version Impact | Affected Work |
|---|---|---|---|---|---|
| CL-001 | 更新范围是否覆盖局内全部六色人群角色 | 覆盖全部六色局内角色；六色共用完全一致的几何外形，仅按玩法颜色区分 | User | None | 模型变体、材质、运行时绑定、验收范围 |
| CL-002 | “高精度行走动画”的目标表现、可接受夸张度与移动节奏 | 选择 A：允许采用更克制自然的移动节奏；披风不抖动、不飘摆，只做低频、小幅、平滑的压缩—回弹，使材质略显 Q 弹；六色共享资源并控制内存稳定 | User | None | 骨骼/分件结构、动画循环、移动速度、披风与身体次级运动、性能验收 |
| CL-003 | 正式建模对参考图的还原级别 | 用户提供的新图为正式建模基准；精度与外形需完全一致，覆盖正面、侧面、背面与 3/4 视角 | User | None | 模型比例、轮廓、结构、细节、材质分区、视觉验收 |
| CL-004 | Astra r005 Implementation Preview 是否可作为正式实现基准 | 接受 r005，开始正式实现 | User | None | 正式建模、动画、集成与视觉验收 |
| CL-005 | Unity 首轮/返工模型是否可继续局部修补 | 不可；当前模型整体不对，必须按概念图 1:1、高精度重新建模，并明确使用 Astra high 生成 | User | None | Art Workstream、集成基准、Human Art Approval |
| CL-006 | 兜帽整体鼓起是否允许用光滑圆面替代原型切面 | 不允许；头部必须模仿原型形状并保留清晰棱角。目标是整体鼓起、前包且钝顶的低多边形硬切面结构，保留顶部脊线、左右斜面、前后折面与硬边法线，不得球化或平滑化 | User | None（解释原冻结参考图要求） | 兜帽拓扑、法线、四视角外形验收 |
| CL-007 | 是否继续 Astra 高精度模型迭代 | 用户要求暂停；停止当前美术代理与后续集成，保留全部安全基线和隔离候选，等待用户恢复 | User | None | Art Workstream、最终集成、Human Art Approval |
| CL-008 | 成熟参考图存在时应如何约束美术生成 | 参考图作为首要造型约束，减少可能干扰视觉复现的提示词；文字仅补充图片无法可靠表达的技术硬条件，不用文字重新设计可直接观察的形体 | User | None（工作方法澄清） | Visual Prompt、Art Iteration、四视角 QA |
| CL-009 | 暂停后的工作是否恢复 | 用户要求恢复并继续；沿用同一 Task、同一 Astra high 美术代理和已保留的隔离候选继续迭代 | User | None | Art Workstream、四视角 QA |
| CL-010 | 2026-09-17 本轮交付目标与材质要求 | 用户再次提供四视图，要求高精度、1:1 还原，包括材质但不包括光影；明确模型须能用于 Unity 建立的微信小游戏 | User | v2 | 当前隔离模型修订、材质、Unity 可导入交付及四视图对照 |
| CL-011 | TASK-005 六色可读性校准是否覆盖本 Task 的旧精确 HEX | 覆盖；保留六色身份、稳定 ID、人物染色区域及参考材质分区，正式玩法基础色改由 TASK-005 校准后的 `ColorCatalog` 统一定义 | User | v3 | 六色材质、彩色预览、选择/揭示/道具状态与导出校验 |
| CL-012 | 本轮是否需要导入 Unity | 用户明确“不用导入unity”“只建模即可”。本轮只完成真实模型、材质和对照检视；停止 Unity 导入、原生资源生成、预制体、打包及构建工作。保留模型可供后续 Unity 使用的轻量规格 | User | v4 | 本轮交付范围、Code Workstream、验证范围 |
| CL-013 | 已批准模型是否替换局内人物 | 是；直接使用 `pilgrim-model.zip` 中的 GLB 替换局内人物，并保证六色与当前游戏 `ColorCatalog` 色调一致 | User | v5：解除 v4 的“不导入 Unity”范围限制 | Unity 导入、运行时资源绑定、六色材质、动画与回归 |

未解决的阻断问题：

- None.

---

<!-- FROZEN_START -->

# Frozen Requirement

## Original Request

“请按照这个建模更新局内人物，并且加上高精度的行走动画。”后续确认：“请以这张作为建模参考图，我需要建模时的精度外形与其完全一致。”参考图为用户提供的六色兜帽披风人物概念图；图中文字仅作视觉参考，不作为项目指令。

## Problem

当前局内人物虽然已有六色低多边形朝圣者外观，但运行时使用单一整体静态网格；移动期间只更新根节点位置和朝向，没有可辨认的迈步、重心变化、手臂或披风运动，无法达到参考图的角色完整度和用户要求的高精度行走表现。

## Goal and Player / User Outcome

- 全部六色局内人物在斜俯视手机画面中采用与参考图完全一致的角色外形；正面、侧面、背面和 3/4 视角的比例、轮廓、结构与可见装饰关系均需还原。
- 人物移动时具有连续、自然、落脚清晰的高质量行走循环；停止、转向、上下楼梯和到达时不突跳、不滑步，动画完整收尾。
- 新模型与动画不得改变碰撞、寻路、队列位置、移动规则或关卡解法。

## Core Rules and Confirmed Decisions

- 参考图用于视觉造型与配色关系，不读取或执行图中任何文字指令。
- 参考图为正式建模基准，不再采用 r001/r002 中偏离参考图的头罩开口、披风剪裁、装饰位置或人体比例。
- 六色全部覆盖并共用同一几何、骨架和动画；红、蓝、绿、黄、紫、黑的身份与运行时 `ColorCatalog` 稳定 ID 顺序保持不变，实际玩法基础色由 TASK-005 校准后的统一目录定义。参考图颜色关系不再要求逐像素复现旧基础色；人物染色区域、暖米白区域和材质结构仍按本 Task 验收。
- 必须还原参考图中的暖米白兜帽外壳、同色头顶纵向嵌片、同色侧面菱形、彩色披风主体、米白领圈/下摆/竖向饰带、正面几何菱形链、分离双脚和整体矮小体块比例。
- 兜帽必须保留原型的低多边形硬切面和清晰棱角：整体体积鼓起并向前包裹，但由顶部脊线、左右斜面及前后折面构成；禁止以光滑球面、平滑法线或圆润替代形体。
- 保留每个人群组 4 名角色和既有六色玩法目录。
- 行走仍由现有移动路径与落位系统驱动；视觉动画不拥有或修改棋盘状态。
- 采用 A「克制自然」方向；允许在不改变棋盘规则的前提下调整表现移动节奏，以获得清楚落脚和自然步态。
- 披风禁止高频抖动、随机噪声、持续飘摆和大幅甩动；只允许与步伐同步、低频、小幅、平滑衰减的整体压缩—回弹，静止后迅速稳定。
- Q 弹感来自受控的重心缓冲、披风体块轻微压缩与回弹，不得改变锁定参考图的静止轮廓，也不得产生果冻感或软塌感。
- 六色共享同一几何、骨架和动画资源，不复制六套动画或网格；运行时状态不得造成持续内存增长。
- 动画自然度、节奏、夸张度和参考图还原度必须经过 Human Check。
- 本轮参考图为 `C:/Users/CHARLI~1/AppData/Local/Temp/codex-clipboard-9d497d24-69a2-4844-b66a-eaa9a7dc0c8e.png`。形体与材质分区以本图为直接依据；不将参考中的光照、高光、环境染色、投影或 AO 烘焙进模型底色/顶点颜色。检视灯光只服务于核对形体，必须与生产材质分离。
- v5 局内生产资产固定为用户批准的 `pilgrim-reference.glb`（SHA-256 `8073C8428386AEEA9323D8AE48E8964C4C12EE5FA11A9F6F1446A5B72FED9B32`）。集成不得修改其可见几何、比例、硬边法线、14 节点骨架或内嵌 `walk_a_in_place_v1`；暖米白材质保持统一，彩色披风材质槽按当前 `ColorCatalog` 六色映射，并继续响应既有选择、揭示与道具明暗属性。

## Scope

- 局内人物模型结构、六色材质和必要的动画分件或骨骼。
- 参考图正面、侧面、背面和 3/4 视角的建模对照与偏差检查。
- 待机到行走、持续行走、转向/上下楼梯、停止到达的视觉过渡。
- 运行时人物创建、动画驱动、资源绑定和针对性验证。
- Requirement Preview、Implementation Preview 与集成后的视觉连续性检查。

## Non-goals

- v4 的“只建模、不导入 Unity”限制已由用户在 v5 明确解除；v5 仅执行已批准模型的必要导入、局内替换、材质适配、构建与回归，不再修改模型造型。

- 不改变任何关卡规则、人群数量、移动路径、平台容量或输入方式；移动节奏仅按已确认的 A 方向做必要表现调整。
- 不重做平台、楼梯、远景、UI、隐藏角色图形或音效。
- 不加入角色养成、换装、皮肤选择或新的角色颜色。
- 不执行外部上传、发布、Commit、Push 或远程 Tag。

## Constraints

- Unity 6000.0.26f1；Windows x64 验证，主要目标为微信小游戏 WebGL2。
- 维持 60 FPS 目标，遵守初始 128 MB / 最大 512 MB 的微信小游戏内存约束。
- 碰撞胶囊、点击区域与导航占位保持不变；动画模型包络按扣除既有选择抬升和桥面展示抬升后的视觉子节点口径验证。
- 颜色必须来自 `ColorCatalog`，材质保持低多边形、柔和、低噪声和手机尺寸可读性。
- 不覆盖用户现有未提交修改；正式资源与代码分工在设计完成后冻结。

## Accepted Assumptions

- None.

## Acceptance Criteria

| ID | Type | Criterion | Verification Owner | Required Evidence |
|---|---|---|---|---|
| AC-F-01 | Functional | 所有确认范围内的局内角色使用新模型，并在移动、转向、上下楼梯和到达时播放正确状态 | Builder / Reviewer | 运行时录像或连续截图、状态驱动检查 |
| AC-F-02 | Functional | 动画不改变棋盘状态、寻路、碰撞、移动数量、队列落位、撤销和重置结果 | Builder / Reviewer | 自动化回归与核心流程 Smoke Check |
| AC-T-01 | Technical | 新角色保持在现有玩法包络内，引用完整，在 Windows 构建及微信小游戏目标约束下无新增错误 | Builder / Reviewer | 包络检查、构建日志、引用检查 |
| AC-T-02 | Technical | 典型与高角色数场景仍满足项目 60 FPS 目标，且角色动画没有不可接受的内存或渲染开销 | Builder / Reviewer | 修改前后性能对比与高负载采样 |
| AC-T-03 | Technical | 未蒙皮基准、绑定姿势和 Unity 最终导入后的静止外表面一致；六色可验证为共享同一网格、骨架和动画 | Builder / Reviewer | 绑定前后表面对照、资源引用检查 |
| AC-E-01 | Experiential | 斜俯视实机画面中，玩家能清楚感受到连续迈步、克制的重心转移与披风体块低频小幅回弹；披风无抖动、飘摆或果冻感，停止时无明显滑步或生硬跳变 | Human | 对比试玩与录像判断 |
| AC-C-01 | Comparative | 与修改前静态滑移表现相比，新角色更接近参考图造型，行走更自然、更精致，同时六色辨识不下降 | Human | 修改前后同机位对比 |
| AC-C-02 | Comparative | 六色模型在正面、侧面、背面与 3/4 对照视图中的外轮廓、身体比例、兜帽/披风结构和装饰位置与参考图视觉一致，不出现可辨认的造型改写 | Human / Reviewer | 同尺度并排图与半透明叠加偏差检查 |

## Test Proxies

| Proxy ID | Proxy | Supports Criterion | Limitation |
|---|---|---|---|
| TP-001 | 行走循环包含稳定左右脚相位、垂直重心和停止混合，连续采样无位置突变 | AC-E-01 | 不能独立证明自然度或观感 |
| TP-002 | 高角色数场景记录帧率、批次、三角面和动画 CPU 时间的前后差异 | AC-T-02 | 不能替代目标微信设备实测 |

<!-- FROZEN_END -->

---

# Design

## Current Pass — v4 Delivered / User Accepted

- 本轮用户明确只建模：输出真实可编辑模型与材质，保留后续 Unity 微信使用的合理规格，但本轮不进行 Unity 导入、场景集成、包生成或构建。继续同一 TASK-003。
- 保留全部旧源文件、隔离候选及现有运行时；当前修订先在新目录制作，产出可导入资产与同模型四视角证据，技术验证后交付 Human Art Approval。
- 本轮优先验证所附珊瑚红参考的完整外形和材质；沿用既有六色共享、骨架与材质属性接口，不能因此改动玩法颜色目录或已确认动画规则。
- 未提供物理尺寸；沿用既有等比尺度接口（Y-up、足底原点、局部包络），不宣称从图片恢复了真实物理尺寸。
- 不存在用户对旧候选的美术批准；不得将旧模型或数值相似度替代 1:1 视觉验收。
- Designer 已确认独立 Unity 导入可行；本轮以新原始像素重新计算对照，不运行会覆盖现有角色的旧导入器。
- 当前跨任务版本说明：v3 的 TASK-005 调色决策由并行工作写入并保留；本轮只建模的范围变更单独记录为 v4，不覆盖该决策。本轮源模型材质仍以用户所附珊瑚红参考为检视对象，实际游戏六色映射留待后续集成。
- Current Implementation (v2): 候选步态、14 骨架与运行时驱动已经实现；正式资源仍 `runtimeEnabled: 0`，旧导入资产为 r003，旧美术交换路径保留 r004。上方当前方案与后续原始 v1 设计基线应区分阅读。
- Reference SHA-256: `717EB47945C9AF538FE745063A325F36DCE177A590049F0B14CC7070036CF600`。新图与旧图角色/姿态目视相同，但文件及裁切分辨率不同，本轮不复用旧分数作为通过证据。
- Design Handoff: Feature Designer 只读核对了旧导入器版本/哈希锁、AO 通道与 Runtime 加载路径；确认新增隔离导入可避免旧资源覆盖。Art 和 Code 已分别启动，无写入范围重叠。


### v4 Production Contract

- Source Reference: `.harness/previews/TASK-003/r014/reference-original.png` 与 `reference-*-raw.png`；当前图为几何、细节和材质分区的首要约束。r014 只保存原图裁切，不包含重新设计的预览。
- Art Owner: 本 Task 已明确要求的 Astra high 美术生产代理；独占 `art-source/task-003-v2-r007/`，从真实三维几何生产新候选，允许复用技术工具但不得把旧未批准模型直接当作成品。
- Code / Integration Owner: Cancelled for current pass by User。Code Builder 已中断，仅做自有进程安全收尾和新文件清点；不得继续导入或生成。
- Art Exchange: `art-source/task-003-v2-r007/exports/task-003-pilgrim-shared-v2.gltf`，自包含 glTF；保留原 14 节点骨架、TRIANGLES/POSITION/NORMAL/COLOR_0/JOINTS_0/WEIGHTS_0、逆绑定与原地 walk clip 接口。Asset Manifest 标记 taskVersion=4 与当前 SHA-256。保留隔离目录与交换文件名中的 v2 以免打断源生产；文件名不代表 Task 当前版本。
- Material: COLOR_0.r 保留染色语义；g=0、b=1、a=1。不得烘焙 AO、明暗、高光、投影或背景染色。Unity 独立材质保留 `_ClothColor` / `_RevealTint` / `_PropDim`；默认颜色依冻结目录，暖米白保持纯底色。使用真实表面法线表达切面，可提供独立检视光照；无光照视图须能核对纯材质分区。
- Delivery: 标准三维模型（优先 GLB，另附 OBJ/MTL 或自包含 glTF）、材质以及可编辑生成源；检视图必须来自实际模型。不交付 Unity 预制体或 unitypackage。标准模型应具有真实珊瑚红/暖米白材质，不应把项目专用染色权重误当作显示颜色。
- Budget: 保留当前接口 10,000 导入顶点 / 5,000 三角面、1 个蒙皮渲染器 / 1 材质槽、最多 4 权重、包络等比适配。预算不能代替造型质量；如无法兼顾须带证据返回，不擅自改轮廓。
- QA: 实际解码导出 glTF 检查面/法线/权重/骨架/动画/包络/颜色；相同模型四视图比较原像素轮廓与内部结构，包含 unlit 材质视图。只验证实际模型、标准文件可读取性与材质，不执行 Unity 导入或构建。
- Regression: 对照 `.harness/artifacts/task003-v2-r007/protected-baseline.json` 检查146个旧文件未变；检查 Git diff；不以桌面验证宣称微信真机 60 FPS。
- Standard Model Derivative: 用户本轮主交付 GLB 可将同一表面按红色/米白拆为两个标准 PBR 材质 primitive，并移除项目专用 COLOR_0 染色mask显示属性；单slot/14骨架接口仍用于保留的项目交换件，不能将mask误显示为蓝色/品红。几何、法线和可见材质分区不变。
- Cancelled Integration Preparation: Code Builder 未读取/导入 Art glTF，没有生成 Mesh/Prefab/Material/AnimationClip/unitypackage/WebGL 构建，其自有 Unity 进程已退出；只将本轮新增 Assets 工具/Shader移到 `.harness/artifacts/task003-v2-r007/unused-integration-preparation/` 保存。原有146个受保护文件在收尾检查中全部未变。
- Required Independent Review: 保留原 Fresh Review 要求，不将其降为 Self Check。本轮原 Designer 的只读结构意见是设计复核，不冒称独立 Fresh Review 完成。Human Art Approval 和 1:1 标准保持不变。
- Required Gates: 源模型技术自检与独立只读复核在实际可行范围内执行；Human Art Approval 核对 1:1 视觉。用户原始参考已锁定，无需再批准一份虚构需求图；正式输出尚未通过美术批准，状态只能 Ready for Human Check。

## Original Implementation — v1 Design Baseline

- Entry point: `StairsGame.Advance()` 调用 `PersonView.Pose(...)`。
- Owning system: `CrowdScene` / `PersonView` / `WaterBirdVisual`。
- Related code: `Assets/Scripts/Runtime/CrowdScene.cs`, `Assets/Scripts/Runtime/StairsGame.cs`, `Assets/Scripts/Runtime/RefinedPresentation.cs`。
- Related art / resources: `Assets/Resources/RefinedArtLibrary.asset`, `Assets/Art/RefinedV2/`, `Assets/Editor/RefinedArt/`。
- Current state or data flow: 移动轨迹计算世界位置和方向，`Pose` 只设置根节点位置、朝向和选择高度。
- Confirmed current behavior: 正式精修角色资源为共享整体网格和六色材质；运行时无四肢或披风动画。
- Confirmed gap or defect: 当前效果为静态模型沿路径滑移，无法满足高精度行走要求。

## Recommended Design

1. 使用共享模型、轻量 Generic 骨架、单 `SkinnedMeshRenderer` 与单材质槽；禁用 Root Motion，导航根节点仍由现有路径系统控制。
2. 将导航根、既有选择/桥面展示节点、动画节点分层；动画只修改视觉子节点，并以角色实际累计位移驱动连续相位，避免拼接移动或全局时钟重置导致步态重启。
3. 运行时动画接收与 `StairsGame.Advance(delta)` 相同的确定性时间，覆盖起步、持续移动、转向、楼梯接地、短距离调整、停止与到达朝向。
4. 对现有计划做统一表现时间缩放，使所有角色保持相对运动关系；保留路径几何、队列、状态提交和最终位置，不保留会把自然步态重新压回高速的总时长上限。
5. 起步保持即时响应；短距离使用受控短步；到达前缩短末步并平滑释放足锁，根节点准确到位后允许视觉层完成短暂收尾。连续追加移动从当前姿势继续，不重置步态。
6. 保留隐藏/揭示、选择高亮、道具变暗和跨关对象复用；新增骨骼、足点和动画状态必须在 Populate、撤销、重置与对象迁移时完整复位。
7. 正式生产前冻结 FBX/骨架/材质通道/接触相位/资源预算；最终资源导入、Prefab/资源绑定和场景验证只由 Code Builder 完成。

## Main Change Areas

- 角色生产资源、轻量骨架、原地动画与六色材质。
- 角色运行时装配、连续相位、接地与动画状态驱动。
- 模型包络、性能、移动与视觉回归验证。

## Technical Options

- 首选共享蒙皮网格与轻量骨架；分件网格仅在正式预览和性能证据证明不降低披风连续性、也不造成过多渲染器时作为备选。
- “高精度”以轮廓、接地、重心、过渡和披风跟随为准，不以单纯增加面数或关键帧数量代替体验质量。
- 保持既有导航根、路径几何和棋盘规则；按已确认的 A 方向统一调整表现计时，移动时间变化属于 Expected Change。
- 首轮联合调参搜索范围：平地约 1.0–1.8 单位/秒、完整左右循环约 0.8–1.3 Hz、起止混合约 0.12–0.25 秒；披风竖向压缩约 1%–2.5%，停止后约 0.2–0.35 秒内稳定。上述仅为 Builder 搜索范围，不是体验通过标准。

## Proposed Product Decisions

- None.

## Workstream Ownership

| Workstream | Required | Allowed Write Paths | Forbidden Paths / Shared Files |
|---|---:|---|---|
| Code Builder | Yes | `Assets/Scripts/Runtime/` 相关动画与装配代码、专属导入/验证脚本、最终角色资源导入和绑定 | Task、`art-source/task-003/`、无关环境资产 |
| Art Asset Builder | Yes | `art-source/task-003/` 可编辑源模型、骨架、原地动画、导出文件与 Asset Manifest | Task、C#、Unity Scene/Prefab/.asset/.meta |
| Final Integration | Yes | 由 Code Builder 唯一负责 `Assets/Art/CharacterWalk/`、Unity 导入元数据、Prefab/资源绑定与构建 | Task、Art 源文件、无关场景与资源 |

## Code–Art Interface

模型接口已确认：Y 向上/Z 向前/双脚落地面原点/Unity 1 单位尺度；六色共享一份固定三角化几何、同一 Generic 骨架和动画；外轮廓、兜帽切面、头顶嵌片、菱形凸起、领圈、披风折线、饰带、徽记和双脚必须由真实结构支持，不能使用只在单一机位成立的贴片；新增动画支撑拓扑、内侧面或隐藏接缝不得改变参考姿势可见外表面；固定法线与硬边，避免导入自动平滑改变切面；禁止烘焙导航根位移；保留 `_ClothColor`、`_RevealTint`、`_PropDim`；六色按 `ColorCatalog` ID 而非参考图排列顺序绑定。动作采用原地动画、位移驱动连续相位与明确左右足接触；披风只使用低频、小幅、平滑衰减的压缩—回弹通道，禁止随机或高频噪声，停止后回到锁定静止轮廓。

冻结交换格式为单一、自包含 buffer 的 glTF 2.0：`art-source/task-003-astra/exports/task-003-pilgrim-shared-v1.gltf`。此前 `art-source/task-003/` 内的低精度候选已被用户拒绝，不得再次导入。新交换件使用标准 glTF 右手坐标、Y-up、模型前向 +Z；Task 专属 Editor 导入器统一镜像 X、反转三角形绕序并转换到 Unity 左手坐标，Art 不预先输出 Unity 镜像版本。只允许 TRIANGLES，以及 `POSITION`、`NORMAL`、`COLOR_0`、`JOINTS_0`、`WEIGHTS_0`、skin inverse bind matrices 和 TRS 动画的 LINEAR/STEP 子集；不引入第三方 glTF 包或 Animator 依赖。

骨架固定为 14 个节点：`visual_root > pelvis > spine > cloak_core > neck > hood_shell > crown_inset`；`pelvis > leg_l > shin_l > foot_l`；`pelvis > leg_r > shin_r > foot_r`；`pelvis > trim_front`。`foot_l` / `foot_r` 本地原点为足底接触面，Bind Pose 世界 Y=0。头罩、嵌片和徽记使用刚性权重；饰带/下摆随所属披风表面变形。每顶点最多 4 个有效权重。

唯一 clip key 为 `walk_a_in_place_v1`：30 fps、1.0 秒、首尾姿势一致、导航根零位移。左足接触区间 `[0.00,0.10]` 与 `[0.50,0.60]`；右足接触区间 `[0.25,0.35]` 与 `[0.75,0.85]`；通过/最高重心参考相位约 `0.125/0.625`。`cloak_core` 仅在 `0.00/0.50` 附近做 Y 轴 `0.985–0.990` 的整体压缩，在 `0.10/0.60` 回到 `1.0`；不得含横向摆动、随机噪声或 Root Motion。参考循环位移为 1.25 m，仅用于相位标定，实际导航位移由代码驱动。

材质为单一 `SkinnedMeshRenderer` / 单材质槽；`COLOR_0.r` 为玩法颜色染色权重（彩色披风、头顶嵌片、侧面菱形及彩色标记为 1，暖米白结构为 0），`COLOR_0.g` 为保留通道并固定 0，`COLOR_0.b` 为局部遮蔽/细节权重，`COLOR_0.a` 固定 1。最终材质继续暴露 `_ClothColor`、`_RevealTint`、`_PropDim`。绑定姿势模型局部包络为 `minY=0`、`maxY<=2.0`、XZ 最大径向 `<=0.49`；只能整体等比缩放适配，不得局部改比例。

正式对照协议：四视角必须来自同一生产模型修订；从锁定参考图裁取原始像素，不补画、不透视拉正、不做 AI 增强；模型机位记录投影、方位、俯仰和统一缩放，只允许平移与等比缩放对齐；每个视角输出并排图、50% 半透明叠加和双色轮廓，同时检查内部结构与装饰位置。量化轮廓指标仅作 Test Proxy，不能替代 Human Art Approval。

成熟参考图存在时，生成提示保持最小化：参考图直接决定可见造型、比例、切面与装饰关系；Prompt 只补充共享真实三维拓扑、固定硬边法线、四视角一致、禁止贴图/单机位作弊、骨架与材质接口等不可由图片充分表达的约束。不得用冗长风格词或额外形体描述覆盖参考图证据。

轮廓 QA 除 silhouette IoU 外，还应按视角输出可直接指导返工的尺寸残差：角色最大宽度及其高度位置、头宽/身高比、肩宽、披风中下段宽度、下摆宽度、侧视最大纵深、头套前后包络、脚宽/间距与关键轮廓点偏移。所有指标基于锁定原像素、平移与统一等比缩放后的结果，不允许横向拉伸或局部 warp；它们只负责定位形体问题，不单独构成视觉通过。

动作运行时职责必须覆盖：参考姿势完整复位；提交位置、实际位移、方向、剩余路径、目标朝向与地面信息；使用与 `StairsGame.Advance(delta)` 相同的确定性时间推进；保留隐藏/揭示/选择与既有材质属性；提供双脚、姿态、朝向和披风均稳定的 `PresentationSettled` 门槛；跨场景复用后清除旧地面引用与足点状态。动画收尾不得阻止连续指令、撤销或重置。

首轮资源预算：单角色 1 个 `SkinnedMeshRenderer`、1 个材质槽；共享网格不超过 10,000 导入顶点 / 5,000 三角面并优先接近现有 7,340 / 3,478；不超过 20 个蒙皮骨骼、每顶点最多 4 个有效权重；共享模型与动画资源目标不超过 4 MiB；96 人相对基线内存增量目标不超过 32 MiB；稳态动画更新 0 B/frame 托管分配；禁止逐帧 `BakeMesh`、逐角色复制 Clip 或逐帧实例化材质。超预算时先提交测量证据，不得擅自削弱锁定外形。

| Asset ID | Purpose | Art Exchange Path | Final / Generated Path | Runtime Role | Fallback | Integration Owner |
|---|---|---|---|---|---|---|
| `task003.pilgrim.shared-v1` | 共享角色几何、14 节点骨架与 A 行走 clip | `art-source/task-003-astra/exports/task-003-pilgrim-shared-v1.gltf` | `Assets/Art/CharacterWalk/Source/task-003-pilgrim-shared-v1.gltf` → `Assets/Resources/CharacterWalk/Task003PilgrimCharacter.asset` | 六色共用模型、骨架与显式采样曲线 | 保留当前 `RefinedArtLibrary.character`，不启用新动画 | Code Builder |
| `task003.pilgrim.manifest-v1` | 哈希、统计、骨架/clip/材质合同与 QA 证据索引 | `art-source/task-003-astra/asset-manifest.json` | `Assets/Art/CharacterWalk/Source/asset-manifest.json` | 导入与验收证据 | 导入失败即停止启用新角色 | Code Builder |

## Asset Contract

Art Bible: `docs/ART_BIBLE.md`  
Approved Preview: `.harness/previews/TASK-003/r005/task-003-a-implementation-preview.png`  
Asset Manifest: Required

---

# Visual Direction

Preview Status: Needs Revision  
Latest Approved Preview: `.harness/previews/TASK-003/r005/task-003-a-implementation-preview.png`

Locked Reference SHA-256: `CC63460C2D1B209A7D1290F7A1E9A1BB081BE917D47CA48C449642DFE77F1173`

## Accepted Visual Decisions

- 参考图的低多边形兜帽披风轮廓、米白结构与颜色识别关系作为方向输入。
- 必须保持当前项目安静、轻盈、低噪声的视觉语言。
- r002 已修正为参考图的“彩色披风主体 + 暖米白兜帽/镶边 + 同色识别标记”。
- 用户已明确要求正式模型外形与最新参考图完全一致；r002 只保留为已否定方向记录，不作为生产造型依据。

## Rejected Directions

- r001/r002 的开放式黑色脸部、外翻斗篷形披风、偏高挑人体比例与简化装饰均不符合最新确认，不得进入生产模型。
- Astra r004 的平顶五边形头罩、偏直筒披风、过窄肩部以及简化的领圈、胸链、下摆和脚部比例，与参考图的圆鼓前包兜帽、矮胖厚实披风体积明显不符；不得通过局部冠带修补继续沿用该主体造型。

## Open Visual Decisions

- None.

## Preview History

| Revision | Type | Artifact | Decision | Feedback / Changes |
|---:|---|---|---|---|
| r001 | Requirement | `.harness/previews/TASK-003/r001/task-003-requirement-preview.png` | Superseded | 彩色主体关系反转，要求新 Revision 修正 |
| r002 | Requirement | `.harness/previews/TASK-003/r002/task-003-requirement-preview.png` | Rejected | 用户要求以最新参考图完全一致建模；r002 造型仍存在明显改写 |
| r003 | Requirement | `.harness/previews/TASK-003/r003/task-003-modeling-baseline.png` | Accepted | 原样保存用户指定参考图作为锁定建模基准；哈希与临时源图一致 |
| r004 | Implementation | `.harness/previews/TASK-003/r004/task-003-a-motion-board.svg` | Superseded | 抽象动作边界板；按用户要求改由 Astra 使用锁定角色造型重新生成 |
| r005 | Implementation | `.harness/previews/TASK-003/r005/task-003-a-implementation-preview.png` | Accepted | 用户批准作为正式实现基准；Astra 生成，直接展示锁定角色的接触、经过、抬脚、落地复位、楼梯步态和六色一致性 |
| r006 | Integrated Visual QA | `.harness/previews/TASK-003/r006/integrated-visual-qa-board.png` | Needs Revision | Unity 首轮集成模型与锁定参考明显不符；步态采样帧无像素变化，证据不足 |
| r007 | Integrated Visual QA | `.harness/previews/TASK-003/r007/integrated-visual-qa-r007-board.png` | Needs Revision | 第二轮模型仍有悬浮嵌片/菱形、饰带脱离、兜帽/披风/双脚比例偏差；用户明确拒绝并要求 Astra high 从零重建 |
| r008 | Integrated Visual QA | `.harness/previews/TASK-003/r008/integrated-visual-qa-r008-board.png` | Needs Revision | Astra high 首轮候选明显改善但仍未达 1:1：兜帽过圆、冠带不连续、侧菱形遮挡错误、领圈/披风/链条/双脚比例偏差 |
| r009 | Integrated Visual QA | `.harness/previews/TASK-003/r009/integrated-visual-qa-r009-board.png` | Needs Revision | Astra r002 静态外形仍有兜帽、冠带、领圈、披风、链条与脚部偏差；真实步态和无抖动披风初步符合 A 方向 |
| r010 | Integrated Visual QA | `.harness/previews/TASK-003/r010/static-reference-comparison-board.png` | Needs Revision | Astra r003 原像素叠加显示轮廓已接近但 3/4 与内部结构仍不满足严格 1:1；动画真实、停止复位和披风稳定通过初检，楼梯接地待 Human Check |
| r011 | User Comparison Feedback | `.harness/previews/TASK-003/r011/user-direct-comparison.png` | Rejected | 用户指出参考与当前模型观感差距过大；确认 r004 主体造型不合格，下一轮需重做兜帽体积、肩—披风过渡、整体宽高比例及内部结构，不能继续局部修补 |
| r012 | Art Source Comparison | `.harness/previews/TASK-003/r012/astra-r005-volume-comparison.png` | Needs Revision | 已重做硬边分面兜帽、连续冠带、实体侧菱形、胸链、下摆与短宽脚；主体明显接近，但背部冠带偏左、侧面冠带位置和背面轮廓略退，侧视顶部仍偏平，未批准且未集成 |
| r013 | Art Source Comparison + Quantitative QA | `.harness/previews/TASK-003/r013/astra-r006-comparison.png`; `.harness/previews/TASK-003/r013/astra-r006-contour-measurements.png` | Needs Revision | 原像素、统一等比缩放、无 warp 的尺寸 QA 已覆盖四视角宽度、纵深、比例、脚部和关键轮廓点。冠带内部指标全面改善，但侧/背/3/4 的头部投影显示角向体积分布仍不一致，背面轮廓略低于 r004，继续隔离修订 |

---

# Regression Plan

## Protected Behaviors

- RB-01：人群选择、合法移动、批量移动和落位结果不变。
- RB-02：撤销、重置、隐藏角色揭示、完成集合与关卡胜利流程不变。
- RB-03：镜头适配、角色点击区域和多人避碰不变。

## Impacted Systems

- 人物渲染、运行时姿态、资源加载、移动播放、场景准备与对象复用。

## Baseline Checks

- 记录修改前模型、同机位行走录像、角色包络、典型与高角色数性能。
- 执行现有 Verify / VerifyAlternatives 与角色相关运行时验证。

## Targeted Regression Checks

- 六色材质、隐藏/揭示、选择高亮、转向、上下楼梯、到达、撤销、重置、关卡切换与对象复用。
- 角色碰撞包络、最小间距、多人批量移动和完成动画时序。

## Core Smoke Path

1. 从首页进入关卡并选择一组四人角色。
2. 完成一次包含转向和楼梯的合法移动。
3. 撤销、重置，再完成一次多人集合并触发胜利。

## Visual Regression Checks

- 同机位比较修改前后六色识别、静止轮廓、行走连续性、落脚与停止。
- 使用参考图的正面、侧面、背面和 3/4 视角做同尺度并排与半透明叠加，检查轮廓、比例、结构和装饰偏差。
- 检查接触、经过、抬脚、落脚缓冲与停止复位；披风不得出现边缘抖动、横向甩动、随机噪声或持续摆动，停止后恢复锁定静止轮廓。
- 普通关卡与 96 人场景分别比较静止、移动、揭示和切关的真实帧时间、动画 CPU、绘制、GC、内存与资源计数。
- 在竖屏默认分辨率和高角色数画面检查遮挡、穿插、抖动与披风异常。

## Known Pre-existing Issues

- 当前人物移动只有根节点位移与朝向，没有实际步态。

---

# Build and Verification Results

> 本区域由 PM 根据 Subagent 返回结果更新。Builder 不直接修改 Task。

## Current v5 In-game Integration — Technical Pass

- 用户批准的 `pilgrim-reference.glb` 已作为局内生产角色启用，`runtimeEnabled=1`；源副本 SHA-256 仍为 `8073C8428386AEEA9323D8AE48E8964C4C12EE5FA11A9F6F1446A5B72FED9B32`。
- 独立只读复核直接解码 GLB 与 Unity 序列化资源，确认 4,946 顶点、2,842 三角面、位置、硬边法线、关节、权重、14 节点骨架及 7 动画通道/868 个关键帧分量一致，仅有必要的坐标系镜像与绕序转换；绑定表面最大误差约 `5.96e-8`。
- GLB 两个材质 primitive 在导入阶段无损派生为现有单槽染色遮罩：3,056 个暖米白顶点、1,890 个彩色顶点，无漏面、重复、跨区或错分。暖米白 Gamma 为 `[0.949020, 0.901961, 0.756863]`；六色使用当前 `ColorCatalog`：`EE5147 / 187FE8 / 43BF58 / E9AC16 / B44CDB / 202024`。
- 六色共享网格、选择提亮/取消恢复、`_PropDim`、隐藏 0.1 秒遮罩、揭示中段/完成及材质属性互不覆盖检查通过；生产/fallback Verify、Playback、Loading、真实步态、双向楼梯与 390/700 宽 3,434 项 gameplay smoke 通过。
- Windows 构建：`Builds/CharacterWalkV5/StairsCrowd.exe`。96 人移动旧→新：Advance `0.135→0.347 ms`、render submit `0.612→0.623 ms`、均 `0 B/frame`；内存约 `+0.87 MiB`，共享角色约 `0.865 MiB`，三角数 `1,104,024→648,024`，draw `108→281`。
- 修改前后均存在的 `Hidden art: question is still breathing` 和 `VerifyAlternatives` 雾后色彩无安全路径失败归类为 Pre-existing；本轮未修改对应运行时代码。微信真机 60 FPS、最终颜色观感、步态自然度和楼梯接地仍需 Human Check。
- 证据：`artifacts/task003-v5-integration/`、`artifacts/task003-v5-gameplay/`、`Builds/CharacterWalkV5/character-probe/`、`Builds/CharacterWalkV5/playback-check/`、`Builds/CharacterWalkV5/loading-check/` 与 `Logs/task003-v5-*.log`。

## Current v4 Model Delivery — Complete

- 用户最新确认“现在这版本即可”；当前模型冻结，Human Art Approval 通过，不再修改外形或材质区域。此确认不等同于严格 1:1 测量认证。
- 静态模型 Fresh Review: Pass。4,946 顶点、2,842 三角面；标准 GLB 与 OBJ/MTL 已导出，材质不含烘焙光影。没有执行 Unity 导入。
- 冻结源 SHA256: `488558D9760A462DD4F4868C78653B5BCD80BDB9229D91230B508A99C0BAF452`。
- 交付 GLB SHA256: `8073C8428386AEEA9323D8AE48E8964C4C12EE5FA11A9F6F1446A5B72FED9B32`。
- 交付目录：`artifacts/pilgrim-model/`，含 GLB、OBJ、MTL 和 ZIP；对照与技术证据位于 `.harness/previews/TASK-003/r014/`。
- 技术检查通过容器、属性、索引、法线、退化面、导出表面一致性和材质检查。没有执行通用自相交求解；未宣称整套衣物全局封闭。Unity、微信设备、历史步态体验验收不在本轮交付内，仍未验证。
- 最后保护基线复查：146 项中 21 项与早期基线不同；存在并行任务，未归因于本模型交付，未覆盖或回滚这些文件。本轮静态资产检查结论不延伸到这些项目变化。
- 以下 Code / Art / Integration 与验收表为历史记录；其中“未批准”仅指旧候选，不覆盖本次用户接受的 v4 模型。全 Task 保留历史待验收状态，不把静态模型交付冒充游戏集成完成。

## Code Result

Status: Implemented; Final Enablement Held

- 已完成共享骨架步态、按实际位移连续推进相位、左右足接触/台阶地面采样、停止约 0.24 秒平滑复位、隐藏/揭示/撤销/重置/对象复用清理，以及 `PresentationSettled` 门槛。
- A 方向约束已落地：披风无横向摆动、无随机/高频噪声，只保留低频 Y 轴轻微压缩—回弹；稳态更新测得 0 B/frame 托管分配。
- 候选资源开关默认关闭；最终模型未通过 Human Art Approval 前，局内继续使用现有人物回退，不会误启用未批准模型。
- Astra r003 集成候选已通过 Windows 构建、候选/回退 Verify、Playback、Hidden 与 Loading 检查；96 人候选测得动画推进约 0.262 ms、渲染提交约 0.410 ms，共享资源约 1.52 MiB、保守内存增量约 2.66 MiB。
- 微信设备 60 FPS 尚未实机验证；`VerifyAlternatives` 的“雾后色彩 step=5”失败在修改前后均存在，归类为 Pre-existing。

## Art Result

Status: Revision Required

- Astra high 已从零生成 r001-r004；r008-r010 集成对照均未达到用户要求的 1:1，因此没有任何模型被批准。
- 当前正式交换路径已恢复为最后一份完整且可独立校验的 `astra-r004` 安全基线，SHA-256 `8D4EC5CE118914FF3A64A08EACB5A0AF57BF5805DF77F7AB5F9842009798FEFC`，8746 顶点 / 3194 三角面；glTF、骨架、材质、闭合兜帽、循环 clip 与零 Root Motion 技术检查通过。
- r004 四视角轮廓 Test Proxy 为正面 0.8880、侧面 0.8802、背面 0.9185、3/4 0.8531；正面冠带内部色面仍明显回归，且领圈、披风、链条与双脚仍有可见差异，不能据此宣称外形一致。
- 未完成的 r005 冠带试验没有候选同时满足 `frontRecovered` 与 `otherViewsProtected`；该中间结果已拒绝，交换路径未保留其输出。其试验记录保留在 `art-source/task-003-astra/qa/r005-crown-fit-trials.json` 供后续 Astra high 继续，不得作为集成输入。
- `art-source/task-003/` 的旧低精度模型仍永久排除。下一轮必须继续修正四视角共享拓扑，并重新执行 Unity 集成对照与 Human Art Approval。

## Integration Result

Status: Candidate Verified; Production Enablement Held

- 已完成 Astra r003 候选的导入、绑定、构建与运行回归；证据位于 `artifacts/task003-astra-integration-r003/` 与 `Builds/CharacterWalkAstraR003/character-probe/`。
- r003 随后因 r010 外形对照未通过而被否决；当前恢复的 r004 未进入生产启用流程，避免把未批准模型带入局内。

## Acceptance Results

| Criterion | Result | Evidence | Owner |
|---|---|---|---|
| AC-F-01 | Pass in executed scope | 真实步态、双向楼梯、到达复位与 49 次生产移动验证 | Builder / Reviewer |
| AC-F-02 | Pass in executed scope | Verify、Playback、Loading、撤销/重置与 gameplay smoke；两项历史失败前后相同 | Builder / Reviewer |
| AC-T-01 | Partial Pass | Windows 构建、包络、引用和 fallback 通过；微信目标设备未验证 | Builder / Reviewer |
| AC-T-02 | Pending Target-device Check | Windows 96 人性能与 0 B/frame 通过；draw 上升，不能替代微信 60 FPS 实测 | Builder / Reviewer |
| AC-T-03 | Pass | Fresh Review 独立逐顶点/逐三角/骨架/动画对照 | Builder / Reviewer |
| AC-E-01 | Pending Human Check | | Human |
| AC-C-01 | Pending Human Check | | Human |
| AC-C-02 | Pending Human Check | | Human / Reviewer |

## Regression Result

Overall Result: Partial Pass — code/runtime checks pass in executed scope; final art and WeChat device gates remain open.

## Fresh Review

Status: Pass for v5 Windows integration technical scope; no blocking introduced issue found

## Human Art Approval

Status: Source model approved by user; integrated six-color appearance pending Human Check

## Human Experience Check

Status: Pending

---

# Final Decision

Status: Building

## Task Version History

| Version | Date | Change | Reason | Invalidated Outputs |
|---:|---|---|---|---|
| 1 | 2026-09-17 | Initial draft | 用户要求按参考图更新局内人物并加入高精度行走动画 | None |
| 2 | 2026-09-17 | 恢复四视图高精度模型制作，明确材质不包含烘焙光影，目标为 Unity 微信小游戏 | 用户本轮重新指定参考并确认使用平台 | 旧候选保留为未批准基线；旧光影/遮蔽通道不能视为本轮材质验收证据 |
| 3 | 2026-09-17 | 将六色精确基础色交由 TASK-005 的统一 `ColorCatalog` 校准，保留身份、ID、染色区域、模型与动画合同 | 用户要求重新校准颜色 | 旧六色材质色值验证、彩色预览、选择/揭示/道具状态截图及含旧色值的导出清单需重验；网格、骨架、动画、染色分区和纯轮廓证据继续有效 |
| 4 | 2026-09-17 | 本轮收窄为只建模与材质 | 用户明确不导入 Unity，只建模即可 | 取消本轮原生资源/预制体/包/构建交付；已做的导入准备不作为模型产物 |
| 5 | 2026-09-17 | 将用户批准并打包的标准 GLB 直接替换局内人物，恢复必要 Unity 集成 | 用户明确“替换局内人物，保证色调一致” | v4 的只建模范围被解除；旧 r003/r004 集成候选与单材质顶点色输入不再是生产资产基线 |
