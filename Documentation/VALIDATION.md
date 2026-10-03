# v0.1.0 验证记录

验证日期：2026-10-03。编辑器：Unity 2021.3.16f1c1。目标：Windows x64 / Mono / Direct3D 11。

## 结果

- Windows 构建成功，0 个错误。唯一构建警告为工程未绑定 Unity Cloud；本游戏为离线单机，不依赖该服务。
- 1,715 项逻辑断言通过：钓鱼策略、强化三选一去重、属性封顶、精英奖励、存档往返、损坏备份恢复与失败清档。
- 93 项集成检查通过：主菜单、图鉴、设置、钓鱼、断线重试、真实摄像机射线射击、伤害、范围重击、冲刺保护、购买、余额不足、读档、全部九个主线钓点、克拉肯和幽海白鲸两条分支、死亡结算。
- 设置滑条布局修复后重新构建，通过 6 项菜单动作检查，并重新导出、检查主菜单、图鉴、设置和教程画面。
- Unity 工程验证扫描 173 个文件：缺失/孤立 `.meta`、GUID 重复、冲突标记、清单格式检查全部通过，0 错误、0 警告。
- 在实际 Unity Player 中导出并目视检查主菜单、钓鱼、普通战斗、商店、奖励、三种守关 BOSS、两种稀有 BOSS 及结算画面。

结果文件在 [TestResults](TestResults)；部分实际渲染截图在 [Images](Images)。

## 测试范围说明

集成驾驶使用独立临时存档，自动瞄准、开火，并在通关覆盖阶段启用无敌以稳定检查状态流程。伤害、范围攻击与冲刺无敌另行在关闭该保护的步骤中验证。自动驾驶不等价于真人难度测试。

Windows 原生窗口抓图连续超时，故采用 Unity Camera.Render + RenderTexture 导出实际场景与 Canvas；图片不是概念图。按钮通过游戏内实际 Button 回调执行，未宣称已完成完整的人工键鼠游玩测试。

数值模型见 [BALANCE.md](BALANCE.md)。已验证首版数值与流程一致性，最终难度仍需不同玩家的命中率、反应时间和多轮反馈来调整。

## 复现

```powershell
./Tools/build.ps1
./Tools/smoke.ps1
python -X utf8 Tools/balance_audit.py
./Tools/package.ps1
```

仅复查菜单画面：运行 `Builds/Release/Tidebreak.exe -tidebreakSmoke -tidebreakVisual`。此模式仍使用隔离的测试存档，完成后自动退出。
