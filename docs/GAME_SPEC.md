# Game Spec

本文件只记录长期稳定、会跨多个 Task 持续生效的项目事实与规则。单个功能的开发记录属于 `tasks/`，版本集成与发布记录属于 `versions/`。

Last Updated: 2026-09-16

---

## Project Identity

- Project Name: 群岛 · Stairs Sort（当前 PlayerSettings 中仍显示 `Stairs Crowd Sky v7 Preview`，构建脚本会更新正式显示名）
- Engine: Unity
- Engine Version: 6000.0.26f1
- Primary Language: C#
- Repository Root: `D:\群岛-harness`
- Source Control: Git；`main` 跟踪 `https://github.com/SichengLu5720/wechatgame.git`
- Current Development Stage: Prototype
- Primary Target Platform: 微信小游戏（Unity WebGL 转换）
- Other Confirmed Build Target: Windows x64，用于本地运行和验证
- Monetization Model: Not documented
- Typical Session Length: Not documented

---

## Product Vision

一款竖屏、单点操作的空间人群排序解谜游戏。玩家在悬浮平台与楼梯构成的立体关卡中搬运同色人群，把每种颜色完整集合到可完成的平台，同时保持路线、落脚点和平台容量合法。

---

## Core Loop

1. 观察平台连接、前排颜色、容量、封印与中转限制。
2. 选择一个可移动的人群，再选择合法目标平台；同色前排人群会沿可通行楼梯移动。
3. 将同色人群集合到完成平台，逐步解锁后续平台，直到所有非中转平台完成集合。
4. 需要时撤销或重置关卡；完成后继续下一关或返回关卡选择。

---

## Stable Gameplay Rules

### Player

- 每个人群组包含 4 名角色。
- 玩家移动的是来源平台前排连续、已揭示且同色的人群组；实际移动数量受目标剩余容量限制。
- 隐藏人群组只有在其到达来源队列前排后才会揭示。

### Input and Controls

- 主操作是点选来源人群/平台，再点选目标平台。
- 再次点击已选来源会取消选择；选择另一个可移动来源会切换选择。
- Windows 与 Unity Editor 首页可通过 `Ctrl+Shift+E` 打开关卡编辑器；首页也提供编辑器按钮。
- `Escape` 用于取消道具选择或关闭关卡选择；设置界面打开时用于关闭设置。


### Progression / Economy

- 关卡数据按固定顺序从 `tutorial-v1.json` 与 `campaign-v1.json` 载入。
- 平台可通过 `unlockAfter` 要求在完成指定数量的颜色集合后解锁。
- 当前没有已记录的货币或付费经济系统。

### Win / Loss

- 当所有仍有人群的平台均为合法完成平台时，关卡判定解决。
- 完成平台必须是非中转平台、达到 4 组容量，并且所有组同色；带颜色限制的平台还必须匹配目标颜色。
- 当前规则中未记录失败状态；玩家可撤销或重置继续尝试。

### Session and Meta Flow

- 游戏启动进入首页，并载入目录中的第 1 关作为展示场景。
- 首页开始游戏后进入关卡；运行时支持关卡选择、设置、关卡编辑与分享相关界面。
- 音效、音乐和振动设置保存在 Unity `PlayerPrefs`。
- 自定义关卡库保存在 Unity `PlayerPrefs`；当前未发现独立文件或服务器存档。

---

## Confirmed Product Constraints

- 竖屏布局；默认窗口尺寸为 700 × 1000。
- 目标帧率为 60 FPS。
- 微信小游戏使用 WebGL2、Gamma 色彩空间、IL2CPP，初始内存 128 MB、最大内存 512 MB。
- 微信 AppID 只能通过环境变量 `STAIRS_WECHAT_APPID` 注入，不得写入仓库。
- 自定义 Node 路径只能通过环境变量 `STAIRS_NODE_PATH` 注入。

---

## Current Project Map

### Startup

- Main entry: 场景中的 `StairsCrowd.Runtime.StairsGame` 组件
- Initial scene: `Assets/Scenes/StairsCrowd.unity`
- Boot / initialization owner: `StairsGame.Awake()` → `WeChatPlatform.Initialize(InitializeGame)`

### Gameplay

- Main gameplay scene: `Assets/Scenes/StairsCrowd.unity`
- Core rules and board state: `Assets/Scripts/Core/CrowdRules.cs`
- Main runtime coordinator and UI: `Assets/Scripts/Runtime/StairsGame.cs` 及其同名 partial 文件
- World presentation: `Assets/Scripts/Runtime/CrowdScene.cs`
- Movement planning and playback: `Assets/Scripts/Runtime/FastMovement.cs`, `MotionComposer.cs`, `CrowdMotion.cs`, `WalkSpace.cs`
- Input owner: `StairsGame` 的指针、键盘与 IMGUI 处理
- Camera owner: `StairsGame` 与 `ViewOrbit.cs`

### Data and Configuration

- Main campaign source: `Assets/Resources/tutorial-v1.json`, `Assets/Resources/campaign-v1.json`
- Legacy/fallback level source: `Assets/Resources/levels.json`
- Campaign loading owner: `Assets/Scripts/Runtime/CampaignRepository.cs`
- Navigation data: `Assets/Resources/campaign-navigation/`, `Assets/Resources/navigation/`
- Save-data owner: `GameSettings.cs` 和 `IslandEditor.cs` 使用 Unity `PlayerPrefs`
- Runtime settings owner: `Assets/Scripts/Runtime/GameSettings.cs`

### UI

- UI root: `StairsGame` 的 IMGUI 绘制逻辑
- Navigation owner: `StairsGame` partial 文件与 `TutorialFlow.cs`
- Settings owner: `SettingsInterface.cs`
- Level editor: `IslandEditor.cs`, `EditorEntrance.cs`, `EditorViewport.cs`
- Debug / verification UI: `Assets/Scripts/Runtime/*Verification.cs`

### Assets

- Art source directories: `Assets/Art/`, `Assets/Shaders/`
- Runtime resource directories: `Assets/Resources/`
- Main generated scene: `Assets/Scenes/StairsCrowd.unity`（`UnityBuild.Build` 会重建并保存该场景）
- Generated outputs that should not be hand-edited: `Builds/`, `artifacts/`, `Library/`, `Logs/`, `Temp/`, `obj/`
- SDK/vendor areas: `Assets/WX-WASM-SDK-V2/`, `Packages/com.qq.weixin.minigame/`

---

## Project Commands

以下命令入口均来自现有 Editor 脚本。当前开发机的 Unity 6000.0.26f1 位于 `D:\GameDev\Tools\Unity\6000.0.26f1\Editor\Unity.exe`。

### Run

```text
在 Unity 6000.0.26f1 中打开项目，载入 Assets/Scenes/StairsCrowd.unity 并进入 Play Mode。
```

### Build / Compile

```text
"D:\GameDev\Tools\Unity\6000.0.26f1\Editor\Unity.exe" -batchmode -nographics -projectPath "D:\群岛-harness" -executeMethod UnityBuild.Build -logFile -
```

输出：`Builds/Windows/StairsCrowd.exe`。

### Tests

```text
"D:\GameDev\Tools\Unity\6000.0.26f1\Editor\Unity.exe" -batchmode -nographics -projectPath "D:\群岛-harness" -executeMethod UnityBuild.Verify -logFile -
"D:\GameDev\Tools\Unity\6000.0.26f1\Editor\Unity.exe" -batchmode -nographics -projectPath "D:\群岛-harness" -executeMethod UnityBuild.VerifyAlternatives -logFile -
```

关卡交付检查另有 `LevelDeliveryCheck.Run`；它要求通过 `-deliveryFolder <目录>` 提供包含 `candidate.json` 的交付目录。

### Lint / Static Checks

```text
Not documented.
```

### Export / Package

```text
"D:\GameDev\Tools\Unity\6000.0.26f1\Editor\Unity.exe" -batchmode -nographics -projectPath "D:\群岛-harness" -executeMethod WeChatBuild.Export -logFile -
```

输出：`Builds/WeChat/minigame`。导出前可在 Unity 菜单中使用 `群岛/微信小游戏/配置并打开转换面板`。Check 已确认后的 Git 推送、微信开发者工具上传和后台设为体验版遵循 `docs/WECHAT_EXPERIENCE_RELEASE_WORKFLOW.md`；AppID、设备验证与上传权限仍只保留在本机和平台账号中。

---

## Technical Constraints

- Supported development OS: Windows（现有本地构建脚本仅明确 Windows x64）
- Runtime target: 微信小游戏 / WebGL2；Windows x64 验证构建
- Performance target: 60 FPS；更细指标由现有性能验证脚本及具体 Task 定义
- WebGL memory: initial 128 MB, maximum 512 MB
- Package-size target: Not documented
- Network assumptions: 核心关卡与资源从包内 `Resources` 载入；联网要求 Not documented
- Offline requirements: Not documented
- Minimum device / browser: Not documented
- Save compatibility policy: Not documented

---

## Architecture Principles

- `StairsCrowd.Core` 保持可验证的规则、状态、求解与关卡模型；`StairsCrowd.Runtime` 负责 Unity 表现、输入和平台集成。
- 玩家规则变更必须同步检查关卡数据、解法见证、移动规划、撤销/重置与运行时表现。
- 关卡及导航数据应通过现有生成/验证入口更新，避免手工修改生成产物。
- 平台凭据与本机路径通过环境变量注入，不进入源文件、Task、Version 或日志。
- 不为局部功能引入大型框架；优先沿用现有纯 C# 规则与 Unity 运行时结构。

---

## Art and UX Rules

### Camera and Perspective

- 固定的立体斜俯视关卡表现；相机由关卡空间自动适配。
- 竖屏安全区和窗口尺寸变化必须保持 UI 与关卡可见。

### Character / Object Proportions

- 每个人群组固定展示 4 名角色；平台规则容量与角色展示必须保持一致。

### Palette and Materials

- 颜色必须来自 `Assets/Scripts/Core/ColorCatalog.cs` 的已注册目录。
- 主要材质与着色器位于 `Assets/Art/Materials/`, `Assets/Shaders/Pastel.shader` 和 `Assets/Resources/` 中的运行时着色器。

### Outline / Lighting / Shadow

- 运行时使用柔和方向光、环境光和夜空/塔体背景；具体美术调整需要 Human Check。

### UI and Feedback

- 中文 UI 文案为当前默认。
- 非法移动应给出原因，不得静默改变棋盘状态。
- 到达、完成集合、选择与行走反馈由 `InteractionFeedback` 和运行时音频协同处理。

### Requirement Preview Context

- 需求预览统一存放在 `.harness/previews/<task-slug>/`，用于确认玩家可见结果、信息层级和状态关系。
- 预览是需求参考，不是可直接导入游戏的生产资产，也不能单独新增玩法规则。
- 任何需要 Builder 遵守的预览细节都必须同时写入对应 Task 的冻结 Product Contract 或 Execution Contract。
- 已批准预览仍需在实机中通过 Human Check；实际安全区、字体和运行时布局以可玩构建为准。

### Audio / Haptics

- 音效默认开启，音乐默认关闭，振动默认开启；三者均可在设置中切换并持久化。
- 强度、节奏与自然度属于主观体验，修改时使用 Human Check。

---

## Current External Integrations

| Integration | Status | Version | Source of Truth | Notes |
|---|---|---|---|---|
| 微信小游戏 Unity SDK | Integrated | package `0.1.1`; build script records SDK changelog `0.1.34` / commit `d288776…` | `Packages/com.qq.weixin.minigame/`, `Assets/WX-WASM-SDK-V2/`, `Assets/Editor/WeChatBuild.cs` | 版本标识存在两套口径，发布前需按当时官方文档重新核验 |
| Unity PlayerPrefs | Integrated | Unity 6000.0.26f1 | `GameSettings.cs`, `IslandEditor.cs` | 保存本地设置与自定义关卡库 |

---

## Stable Decisions

### DEC-001 — 使用 Task 作为单功能事实来源

- Date: 2026-09-16
- Status: Accepted
- Context: 项目接入 GameDev Harness。
- Decision: 每个非简单功能或缺陷只维护一个 `tasks/<task-slug>.md`，冻结玩家结果和验收标准，Builder 只回写同一 Task。
- Consequences: 不再为同一功能创建重复的 PRD、技术方案、实现报告或 QA 报告。

### DEC-002 — 外部发布动作保持人工授权

- Date: 2026-09-16
- Status: Accepted
- Context: 微信小游戏导出涉及 AppID、设备验证、上传与平台发布。
- Decision: Harness 可准备和验证本地产物，但上传、提交审核、发布、正式配置与回滚必须得到用户明确授权。
- Consequences: 任何 Task 或 Version 不得将本地导出等同于正式发布。

---

## Out of Scope for Current Stage

- 未经单独 Task 批准的付费、广告、账号或云存档系统。
- Harness 自动 Commit、Tag、Push、Merge、上传、审核提交或生产发布。
- 未经验证的平台专用发布流程与凭据管理。
