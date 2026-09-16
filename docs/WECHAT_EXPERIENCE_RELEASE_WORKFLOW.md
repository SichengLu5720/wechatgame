# 微信体验版发布工作流

本工作流用于将已完成 Check 的项目变更推送到 Git，并上传为微信小游戏体验版。

它是受控发布流程，不是每次构建后的自动动作。每次发布均在对应 `versions/<version>.md` 中记录实际 Commit、导出产物、体验版版本号与后台记录。

## 触发条件

以下条件必须全部成立，才允许开始：

1. 本次版本的所有 Included Tasks 状态均为 `Verified`。
2. 所有必需的 Technical / Art / Experience Check 均为 `Accepted` 或 `Not Required`。
3. Version 的 Release Candidate 已建立并通过验证。
4. Version 的 Human Release Check 为 `Accepted`。
5. Version 的 Release Decision 为 `Approved`，且明确目标为 `微信体验版`。
6. 当前工作区只包含该版本已批准的变更；不得把 `Builds/`、`Library/`、`Logs/`、`Temp/`、临时预览或凭据加入提交。

若任一条件不满足，流程停止，不执行 Git 推送或微信上传。

## 环境与凭据前置条件

- Git remote `origin` 指向项目主仓库，推送者具有目标分支的推送权限。
- `STAIRS_WECHAT_APPID` 仅在本机环境中设置，且必须是正式小游戏 AppID；不得写入源文件、Task、Version、日志或 Git。
- 已安装并登录微信开发者工具，且该账号拥有小游戏上传权限。
- 项目可从 `Builds/WeChat/minigame` 作为“小游戏”工程导入。
- 若以后改用 `miniprogram-ci`，上传私钥与 IP 白名单必须先作为独立平台接入事项验证；本工作流默认不依赖它。

## 执行顺序

### 1. 发布前核验

1. 打开对应 Version，逐项确认上述触发条件。
2. 记录当前分支、`git status`、待提交 Diff 和目标版本号。
3. 运行该 Version 要求的 Build、测试与目标设备 Smoke Test。
4. 若构建脚本产生编辑器副产物，先恢复不属于版本范围的材质、Prefab、Scene、ProjectSettings 和缓存差异。
5. 任一检查失败或发现未批准文件，回到 Integration；不得继续。

### 2. 提交并推送 Git

1. 仅暂存本次版本批准的源代码、资源、配置、Task、Version 与 Harness 文件；不要使用不加筛选的批量暂存。
2. 提交信息使用：`release: <版本号> — <简短说明>`。
3. 记录提交 SHA、分支和远端 URL 到 Version 的 Release Candidate / Release Result。
4. 推送：`git push origin <branch>`。
5. 推送失败、远端拒绝或本地 SHA 与预期不一致时停止；不得继续上传微信体验版。

### 3. 导出微信小游戏

在已推送的同一 Commit 上运行：

```text
"D:\GameDev\Tools\Unity\6000.0.26f1\Editor\Unity.exe" -batchmode -nographics -projectPath "D:\群岛-harness" -executeMethod WeChatBuild.Export -logFile -
```

必须确认：

- 日志含 `WECHAT EXPORT PASSED`。
- `Builds/WeChat/minigame/game.js`、`game.json`、`project.config.json` 均存在。
- `artifacts/wechat/integration.txt` 不泄露 AppID，仅记录是否已配置。
- 导出失败时停止；不上传旧产物。

### 4. 微信开发者工具与体验版

1. 在微信开发者工具导入 `Builds/WeChat/minigame`，工程类型选择“小游戏”。
2. 先在模拟器和真机预览完成目标设备 Smoke Test；若需要其他体验者，先在后台配置测试人员。
3. 点击“上传”，填写与 Version 一致的版本号和说明。
4. 到微信小游戏后台的“版本管理 / 开发版本”定位刚上传的版本，设置为“体验版”。
5. 记录后台版本号、上传人、上传时间和后台页面引用到 Version 的 Release Result。

上传或后台设为体验版失败时，Version 标记 `Failed`，保留已推送 Commit 和失败证据；不得将其标记为 Released。

## 完成与回退

- 成功条件：Git push 成功、微信导出成功、开发者工具上传成功、后台已设为体验版，且记录齐全。
- 体验版不是正式发布；不得将其视为审核通过或 Production rollout。
- 若体验版有问题，创建 Hotfix Task；必要时在后台切回上一体验版，并在 Version 的 Rollback Plan 记录对应 Commit。

## 参考

- Unity 微信小游戏部署说明：<https://docs.unity.cn/cn/tuanjiemanual/1.9/Manual/UploadWeixinMiniGame.html>
- `miniprogram-ci` 上传能力与上传密钥前置条件：<https://www.npmjs.com/package/miniprogram-ci>
