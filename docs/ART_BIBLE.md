# Art Bible

本文件记录群岛项目稳定的视觉规则。Task 特例必须写入对应冻结合同，不能静默修改本文件。

## 1. Visual Identity

- Style: 低多边形、柔和材质、立体斜俯视的漂浮阶梯与小岛。
- Tone: 安静、轻盈、带夜空氛围；失败反馈清楚但不压迫。
- Player impression: 规则易读、空间层级清晰、界面克制。
- Excluded: 高饱和霓虹、写实厚重材质、与当前项目无关的卡通贴纸或异色 icon。
- Originality: 使用项目自有造型与图形语言，不仿制第三方角色、商标或专有 UI。

## 2. Camera and Perspective

- 固定立体斜俯视视角，相机根据关卡空间自动适配。
- 竖屏为主要构图，必须兼容安全区与窗口尺寸变化。
- 前景 UI 不遮挡关键平台、角色目标或移动反馈。

## 3. Shape Language and Proportions

- 平台、面板与按钮以圆角和简洁大形为主。
- 每个人群组固定展示 4 名角色，轮廓在手机尺寸下必须可辨。
- 装饰细节服从玩法信息，不与可交互对象争夺注意力。

## 4. Line, Material, and Surface

- 材质保持柔和、低噪声、低高光密度；沿用 `Assets/Shaders/Pastel.shader` 及现有运行时材质体系。
- 边缘可用轻微明暗或描边强化层级，不使用厚重黑色漫画线。
- 纹理和表面细节保持克制，避免在小屏产生摩尔纹或视觉噪声。

## 5. Color and Lighting

- Primary: 深青灰、暗蓝绿与夜空色。
- Neutral: 暖米白、低饱和灰白。
- Accent: 柔和珊瑚色、浅薄荷色；玩法颜色必须来自 `Assets/Scripts/Core/ColorCatalog.cs`。
- Restricted: 与当前界面无关联的高饱和纯色、荧光色和强烈彩虹渐变。
- Lighting: 柔和方向光与环境光，阴影用于表达台阶和层次，不遮蔽操作信息。

## 6. UI Visual Language

- 中文为默认界面语言。
- 面板使用暖米白或深青灰半透明底，圆角清晰，留白充足。
- 主按钮优先柔和珊瑚色；次级动作采用深青灰或低对比中性色。
- 设置齿轮、旗帜和状态 icon 使用深青灰、米白、柔和珊瑚色体系。
- 不把正文文字烘焙进生产图片；运行时文字需适配安全区和不同分辨率。
- 失败面板按“原因 → 当前关卡 → 主次动作”的顺序组织。

## 7. Character and Animation Rules

- 角色比例、颜色分组与现有资源一致；参考 `Assets/Resources/start_hero.png` 和当前运行时生成方式。
- 移动、到达与集合表现应完整收尾后再出现结算遮罩。
- 动画自然度、节奏和夸张度属于 Human Check 项目。

## 8. Environment and Prop Rules

- 环境以漂浮平台、阶梯、塔体和夜空背景建立纵深。
- 道具与可交互物必须在玩法尺寸下可辨，装饰不应伪装成可操作目标。
- 重复构件通过高度、色阶和局部组合降低机械感。

## 9. VFX Rules

- VFX 使用柔和几何形和短时反馈，优先保证选择、行走、到达与结算的可读性。
- 粒子密度与屏幕空间强度保持低至中等，不长期遮挡棋盘。
- 颜色沿用对应玩法色和整体低饱和基调。

## 10. Asset Production Standards

- Asset ID: `<task-slug>.<purpose>.<variant>`。
- 文件名使用小写 kebab-case；变体追加语义后缀。
- 默认位图格式为 PNG；需要透明时保留 straight alpha；颜色空间遵循现有 Unity 导入设置。
- UI 九宫格、像素密度、过滤方式和压缩设置必须在 Production Art Contract 中逐资产指定。
- 可编辑源文件放在 Task 指定的源目录，导出文件只放入合同允许的项目路径。
- 生产资产必须提供 Asset Manifest；预览图仅保存在 `.harness/previews/`，不得当作正式游戏资产。

## 11. Approved References

| Reference | Purpose | Preserve | Do Not Treat As |
|---|---|---|---|
| `Assets/Resources/start_hero.png` | 当前角色与色调参考 | 柔和、简洁、小屏轮廓 | 新角色合同 |
| `Assets/Shaders/Pastel.shader` | 材质语言 | 柔和明暗与低噪声表面 | 固定参数值 |
| `.harness/previews/failure-and-level-progress/requirement-preview.png` | 失败弹窗与首页进度需求参考 | 信息层级、中文、色调、两个动作 | 可直接导入的最终 UI 资产 |

## 12. Known Visual Risks

- 当前主要 UI 由运行时代码绘制，字体、字号、触控范围和不同安全区下的实际效果必须在构建中复核。
- 概念预览中的间距与细节可能无法逐像素复现，冻结合同优先于偶然画面细节。

## 13. Change Policy

稳定视觉规则变更需由 PM 与用户确认。Task 专属视觉要求保留在对应冻结合同中，不自动升级为全局规则。
