# GameDev Harness v0.4

这是一个可直接放进现有游戏项目根目录的轻量 Codex Harness。

v0.4 在 v0.3 的 PM 需求入口上增加了**双生产 Builder**：

- `code_builder`：高精度实现代码、场景、配置、测试与最终引擎集成。
- `art_asset_builder`：高精度生产最终美术资产、执行资产 QA，并输出机器可读 Asset Manifest。

两个 Builder 可以并行，但只在 Task 已经冻结了代码—美术接口、并且写入目录完全分离时启动。最终资源导入、Scene / Prefab / Node 绑定与完整运行验证由 Code Builder 顺序完成。

## 1. 核心架构

```text
用户
  ↕
PM Orchestrator（主线程）
  ├─ 与用户聊清需求
  ├─ Requirement Confirmation
  └─ confirmed Requirement Packet
            ↓
   ┌────────┴────────┐
   ↓                 ↓
Feature Designer   Requirement Visualizer
调查技术与资产管线  生成需求预览
设计代码—美术合同    帮用户确认方向
   └────────┬────────┘
            ↓
     PM 汇总并写唯一 Task
     冻结 Product Contract
     冻结 Parallel Build Contract
            ↓
   ┌────────┴────────┐
   ↓                 ↓
Code Builder       Art Asset Builder
代码/场景/测试       最终生产美术
占位与接口准备       Asset Manifest
   └────────┬────────┘
            ↓
       PM 合同校验
            ↓
 Code Builder Final Integration
 导入、绑定、构建、运行、截图
            ↓
 Art QA / Human Art Approval
            ↓
 Fresh Review / Human Playtest
            ↓
          Verified
```

## 2. 为什么不能让两个 Builder 完全无约束地同时写

代码和美术适合并行生产，但游戏项目中这些内容常相互耦合：

- 代码引用的资源路径和 asset ID
- Sprite 尺寸、图集布局和动画帧数
- Scene / Prefab / Node 绑定
- Unity `.meta`、Godot import 信息等引擎元数据
- UI 九宫格、Pivot、Pixels Per Unit、过滤和压缩设置

因此 v0.4 采用：

> **并行生产 + 顺序集成。**

并行阶段两边只写各自目录；Code Builder 在两边完成后成为唯一集成写入者。

## 3. 目录结构

```text
<your-game-project>/
├── AGENTS.md
├── HARNESS_README.md
├── .codex/
│   └── agents/
│       ├── feature-designer.toml
│       ├── requirement-visualizer.toml
│       ├── code-builder.toml
│       ├── art-asset-builder.toml
│       └── fresh-reviewer.toml
├── .harness/
│   ├── contracts/
│   │   ├── requirement-packet.schema.json
│   │   ├── build-dispatch-packet.schema.json
│   │   ├── asset-manifest.schema.json
│   │   ├── code-result.schema.json
│   │   └── integration-result.schema.json
│   ├── previews/
│   └── runs/
├── docs/
│   ├── GAME_SPEC.md
│   └── ART_BIBLE.md
├── tasks/
│   └── _TEMPLATE.md
└── versions/
    └── _TEMPLATE.md
```

## 4. 五个 Subagent 的职责

### Feature Designer

接收 PM 已确认的 Requirement Packet，调查：

- 真实代码和场景入口
- 当前资源目录与导入管线
- 现有资产尺寸、命名、绑定方式
- 哪些内容适合并行、哪些必须顺序集成

返回：

- Build Mode
- Code Contract
- Production Art Contract
- Code-Art Interface Contract
- 写入范围
- 验收与 Gate 建议

Designer 只读，不写 Task。

### Requirement Visualizer

根据同一 Requirement Packet 生成需求预览，用来确认：

- 玩家看到什么
- 空间和状态关系
- UI 或场景方向
- 美术基调

它不是最终资产生产者。

### Code Builder

分两个模式：

1. `parallel_build`
   - 实现逻辑、场景骨架、配置、测试
   - 使用合同规定的 asset ID、路径和占位策略
   - 不生成最终美术

2. `integration`
   - 读取 Art Manifest
   - 导入和绑定最终资产
   - 处理引擎元数据与资源设置
   - 完整 Build / Test / Run
   - 生成 in-game 截图或录屏供视觉 QA

### Art Asset Builder

分三个模式：

1. `production`
   - 读取批准的预览、Art Bible 与 Production Art Contract
   - 生成最终 2D 资产
   - 验证路径、尺寸、格式、Alpha、帧布局和风格一致性
   - 输出 `asset-manifest.json`

2. `visual_qa`
   - 对照集成后的游戏截图检查真实显示效果
   - 在自己的资产范围内做修订

3. `tuning`
   - 进行不改变冻结方向的局部颜色、裁切、边距和一致性调整

### Fresh Reviewer

在 Code Builder 完成后只读检查冻结合同、相关 Diff、直接调用链与验证证据。它不修改代码，只返回 `Review Passed`、`Changes Required` 或 `Inconclusive`，重点拦截明显的正确性、状态、存档、资源引用和回归问题。

## 5. “高精度”由什么保证

Harness 无法承诺任何模型输出绝对正确，但通过以下结构显著降低偏差：

1. **同一 Task、同一 Build Revision**：两边不接收不同版本的需求。
2. **批准预览 + Art Bible**：生产资产有固定视觉锚点。
3. **Production Art Contract**：每个资产都有 asset ID、路径、尺寸、格式、Alpha、视角和风格要求。
4. **Code-Art Interface Contract**：代码预先知道每个资产绑定到哪里。
5. **不重叠写入**：避免并行覆盖与 Scene 冲突。
6. **Asset Manifest**：美术交付不是“生成了图片”，而是逐资产记录规格和 QA。
7. **Final Integration**：最终效果必须在游戏里验证，不能只看单张图。
8. **Human Art Approval**：角色、UI、美术吸引力等主观结果由人最终确认。


## 6. 模型与精度设置

本项目按试错成本分配模型：

| 角色 | 模型 | Reasoning |
|---|---|---|
| PM 主线程 | `gpt-5.6-sol` | `medium` |
| Feature Designer | `gpt-6-astra` | `high` |
| Requirement Visualizer | `gpt-5.6-luna` | `medium` |
| Code Builder | `gpt-6-astra` | `high` |
| Art Asset Builder | `gpt-5.6-terra` | `medium` |
| Fresh Reviewer | `gpt-6-astra` | `low` |

PM 主线程模型需要在启动任务时选择；项目内的 TOML 会固定各 Subagent 的模型与推理档位。高成本配置只用于设计和代码实施，预览、美术生产与独立复核使用较低档位。

## 7. 接入方式

把 v0.4 内容复制到游戏项目根目录。

项目已有 `AGENTS.md` 时不要直接覆盖；合并本 Harness 的角色、Task、写入边界和 Gate 规则。

先填写：

- `docs/GAME_SPEC.md`
- `docs/ART_BIBLE.md`

未知内容写 `Not documented`，不要猜测。

## 8. PM 到 Task 的流程

### 第一步：PM 与用户确认需求

```markdown
## Requirement Confirmation

### Problem
### Player Outcome
### Core Rules
### Scope
### Non-goals
### Constraints
### Accepted Assumptions
### Visual Preview Purpose
### Acceptance Intent
### Remaining Decisions
```

### 第二步：PM 并行派发设计

```text
以下是已经由用户确认的 Requirement Packet。

请使用完全相同的 Packet 并行启动：

1. feature_designer
   - 只读项目
   - 调查代码与资产管线
   - 返回 Build Mode、Production Art Contract、Code-Art Interface Contract

2. requirement_visualizer
   - 只写 .harness/previews/<task-slug>/
   - 生成一张需求预览和 preview-manifest.json

等待两者完成后再继续。

<Requirement Packet JSON>
```

### 第三步：PM 写唯一 Task

PM 将 Product Contract 与 Parallel Build Contract 一次性写入：

```text
tasks/<task-slug>.md
```

`FROZEN` 区域控制玩家结果；`EXECUTION_CONTRACT` 区域控制并行生产接口。

## 9. 并行 Builder 调用

当 Task 的 Build Mode 为 `Parallel Code + Art`：

```text
读取：tasks/<task-slug>.md

从该 Task 生成同一份 Build Dispatch Packet，确保：

- 相同 Task ID
- 相同 Requirement Version
- 相同 Build Revision
- 相同 Contract Digest
- 明确且不重叠的 write paths

并行启动：

A. code_builder，mode = parallel_build
- 只写 Code Workstream 路径
- 不修改生产美术
- 返回 code-result.json

B. art_asset_builder，mode = production
- 只写 Art Workstream 路径
- 不修改代码、场景或引擎 metadata
- 返回 asset-manifest.json

等待两者完成。
两个 Builder 均不得修改 Task。
```

## 10. 最终集成调用

```text
使用 code_builder，mode = integration。

输入：
- tasks/<task-slug>.md
- code-result.json
- asset-manifest.json
- 当前 Build Revision

要求：
- 校验所有 asset ID、路径和规格
- 导入并绑定最终资产
- 清理不应保留的占位内容
- 运行 Build / Test / target scene
- 输出 integration-result.json
- 输出可供 Art QA 与人工确认的游戏截图或录屏路径

不得修改 Task。
```

随后按 Task 的 Gate 执行：

```text
Art Gate: Not Required | Asset QA | Human Art Approval
Technical Gate: Self Check | Fresh Review
Experience Gate: None | Human Check
```

最后由 PM 一次性把 Code Result、Art Result 和 Integration Result 汇总回同一 Task。

## 11. 适合并行和不适合并行的情况

### 适合 `Parallel Code + Art`

- 新角色逻辑与角色贴图可以通过固定 asset ID 分离
- UI 逻辑与按钮、面板、图标可以通过固定尺寸和路径分离
- 新道具系统与道具图标可以独立生产
- 场景逻辑与背景图可以通过固定分辨率分离

### 不适合直接并行

- 两边都要持续修改同一 Scene / Prefab
- 美术尺寸尚未决定，而代码布局完全依赖它
- 动画帧数尚未决定，而状态机依赖具体帧事件
- Atlas 由两边同时编辑
- 3D 模型、骨骼、动作和代码挂点尚未建立统一合同

这些任务应先补齐接口，或改为顺序执行。

## 12. 生产美术能力边界

当前图片生成适合 2D UI 资产、背景、插画、角色与道具图、Sprite Sheet 和占位图。

复杂 3D 模型、拓扑、UV、Rig、Spine 工程、精确长文本排版等，需要相应工具链。缺少工具时，Art Asset Builder 应返回 `Blocked` 或 partial delivery，而不是声称 production-ready。

## 13. 完整流程

```text
用户需求
  ↓
PM 聊清并确认
  ↓
Requirement Packet
  ↓
Designer + Visualizer 并行
  ↓
PM 写单一 Task 与并行生产合同
  ↓
Code Builder + Art Asset Builder 并行
  ↓
Code Builder Final Integration
  ↓
Art QA / Human Art Approval
  ↓
Fresh Review / Human Playtest
  ↓
Task Verified
  ↓
Version 集成、RC、发布与观察
```

v0.4 仍未安装 MCP、command rules、scripts 或自定义 Skills。
