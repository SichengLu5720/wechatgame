# GameDev Harness v0.1

这是一个可直接放进现有游戏项目根目录的轻量 Codex Harness。

它只包含当前已经设计完成、能够立即使用的核心部分：

- 项目级 `AGENTS.md`
- `feature_designer`：分析需求 + 调查项目 + 制定方案
- `feature_builder`：执行实现 + 自我验证
- 单一 Task 模板
- Version Manifest 模板
- 稳定 Game Spec 模板

本版本**暂不包含**：

- MCP
- Codex command rules
- 自动化 scripts
- Skills
- 平台专用 SDK 或发布流程

这些能力以后只在真实流程需要时增加。

---

## 1. 目录结构

```text
<your-game-project>/
├── AGENTS.md
├── HARNESS_README.md
├── .codex/
│   └── agents/
│       ├── feature-designer.toml
│       └── feature-builder.toml
├── docs/
│   └── GAME_SPEC.md
├── tasks/
│   └── _TEMPLATE.md
└── versions/
    └── _TEMPLATE.md
```

---

## 2. 接入方式

把本 Harness 文件夹中的内容复制到游戏项目根目录。

项目根目录最好同时是 Git 根目录。

### 项目已经存在 `AGENTS.md`

不要直接覆盖。

将本 Harness 中的规则与现有 `AGENTS.md` 合并，并优先保留：

- 项目真实的运行、构建与测试命令
- 现有架构与代码规范
- 团队已有安全约束
- 本 Harness 的 Task、冻结区、验证 Gate 与版本流程

### 项目还没有 `AGENTS.md`

可以直接使用本文件。

---

## 3. 第一次使用前

先填写 `docs/GAME_SPEC.md` 中能够确认的信息，至少包括：

- 引擎及版本
- 项目入口与主要场景
- 核心玩法循环
- 已确认的玩法约束
- 当前主要系统位置
- 真实可用的运行、构建与测试命令
- 不应手工修改的缓存或生成目录

无法确认的内容保持 `Not documented`，不要猜测。

---

## 4. 核心流程

```text
用户想法 / Bug / 体验反馈
        ↓
Feature Designer
        ↓
tasks/<task>.md
Status: Ready to Build
        ↓
Feature Builder
        ├─ 建立基线
        ├─ 实施修改
        ├─ Build / Test / Run
        ├─ 对照冻结验收标准自检
        └─ 回写同一 Task
        ↓
Technical Gate
        ├─ Self Check
        └─ Fresh Review
        ↓
Experience Gate
        ├─ None
        └─ Human Check
        ↓
Task Verified
        ↓
Version Manifest
        ↓
集成、版本回归、RC、发布与观察
```

---

## 5. 何时调用 Designer

适合调用：

- 新功能或新玩法
- 玩家行为、操作、状态或规则变化
- 原因不明确的体验调优
- 多文件或跨系统修改
- 需求已发生过返工
- Bug 的真实原因、预期或归属不清楚

可以跳过：

- 明确文案替换
- 已知配置项的简单数值修改
- 明确且低风险的颜色/资产替换
- 已确认原因的局部修复
- 纯注释、格式或文档修改

### 推荐调用 Prompt

```text
使用 feature_designer 处理以下需求：

<在这里写你的想法、反馈或 Bug>

请读取相关项目上下文与真实实现，
先明确玩家结果，再制定最小实施方案。
需求足够明确时，只创建一份 tasks/<task-slug>.md。
低风险细节可以自行采用合理假设；
只有会显著改变玩家体验、核心玩法、功能范围或数据兼容性的事项才询问我。
```

Designer 最终只应返回：

- `Ready to Build`
- `Needs Decision`

---

## 6. 调用 Builder

Designer 生成 Task 后，使用：

```text
使用 feature_builder 执行：

tasks/<task-file>.md

以该 Task 为本次工作的唯一需求来源。
不得修改 FROZEN 区域。
完成实现、自我验证与 Git Diff 检查后，
只更新原 Task 的 Builder Result 与 Status。
```

Builder 最终可能返回：

- `Verified`
- `Ready for Review`
- `Ready for Human Check`
- `Verification Failed`
- `Needs Replan`
- `Blocked`

---

## 7. Fresh Review

当 Task 的 `Technical Gate` 为 `Fresh Review` 时，Builder 应停在：

```text
Ready for Review
```

可使用 Codex `/review` 检查当前未提交变更，并给出类似指令：

```text
Review the current changes against:

tasks/<task-file>.md

Read the frozen requirements, Builder Result,
relevant Git diff, changed files, and directly affected call paths.

Focus on correctness, acceptance-criteria violations,
behavior outside scope, state/lifecycle bugs,
scene/resource/configuration reference risks,
compatibility risks, regressions, and unsupported verification claims.

Do not modify the working tree, redesign the feature,
or report style-only issues.

Return:
- Review Passed
- Changes Required
- Inconclusive
```

Reviewer 只审查，不直接修改项目。

---

## 8. Human Check

以下内容通常需要人工试玩：

- 手感与爽感
- 移动、攻击和镜头节奏
- 震动强度
- 动画自然度
- UI 直观性与可读性
- 难度与数值节奏
- 美术一致性

结果：

- `Accepted`：技术 Gate 也通过后，Task 可变为 `Verified`
- `Needs Tuning`：回到 Builder，只调整参数或局部表现
- `Needs Redesign`：回到 Designer，重新定义目标或交互

---

## 9. Version 流程

当一个版本包含多个 Task 时，从 `versions/_TEMPLATE.md` 创建：

```text
versions/v0.1.0.md
```

Version 负责：

- 本版本目标与范围
- Included Tasks
- Task 之间的依赖与集成顺序
- 多功能组合后的回归测试
- Release Candidate 与构建产物
- 人工发布检查
- 回滚点
- 发布结果与发布后观察

Task 证明单个功能正确；Version 证明这些功能组合成一个可交付版本后仍然正确。

平台具体发布步骤目前可以保持空白。第一次确定目标平台时，再根据当时最新官方文档完成平台接入 Task，并把验证后的流程写入对应 Version 或后续平台 Playbook。

---

## 10. 核心设计原则

### 两个长期角色

```text
Feature Designer
= 需求分析 + 技术方案

Feature Builder
= 代码执行 + 自我 QA
```

### 一个 Task，一份事实来源

同一个功能不再拆成：

- PRD
- Technical Plan
- Implementation Report
- QA Report

这些内容全部放进同一个 `tasks/<task>.md`。

### 冻结玩家结果，不冻结内部实现

Designer 冻结：

- Goal and Player Outcome
- Scope
- Non-goals
- Constraints
- Acceptance Criteria

Builder 可以改变内部实现，但不能改变以上结果。

### 按风险增加 Gate

简单改动：

```text
Designer → Builder → Verified
```

中高风险改动：

```text
Designer → Builder → Fresh Review → Verified
```

主观体验改动：

```text
Designer → Builder → Human Check → Verified
```

需要两者时：

```text
Designer → Builder → Fresh Review → Human Check → Verified
```

### 不自动执行外部高风险动作

Harness 默认禁止 Agent 擅自：

- Commit / Tag / Push / Merge
- 上传构建包
- 提交平台审核
- 开始正式 rollout
- 发布正式版本
- 修改生产配置
- 执行生产回滚

这些动作必须得到用户明确授权。

---

## 11. 建议的第一次真实测试

不要用大型玩法测试 Harness。

先选一个小而真实的任务，例如：

```text
把角色移动速度从硬编码提取为统一可调参数，
默认表现保持不变，暂时不制作完整调试看板。
```

依次执行：

1. Designer 创建 Task
2. 检查 Task 是否准确
3. Builder 实施与验证
4. 必要时 Fresh Review
5. 人工试玩
6. 将 Task 标记为 Verified

连续稳定完成 2—3 个真实 Task 后，再决定是否增加 Scripts、Skills、Rules 或 MCP。
