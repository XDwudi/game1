# v0.8 验收记录

本轮覆盖六场后期首领的推进机制、所有生物的技能主题映射、攻击预告与释放，以及 Windows 三种显示模式。三位子代理分别负责首领、特效和显示设置；根代理集成代码、构建、运行播放器、查看截图并决定返修。使用本机 Unity 2021.3.16f1c1 和 Built-in 渲染。

## 复现

```powershell
python -X utf8 Tools/prepare_resources.py
./Tools/build.ps1
./Tools/test_v08.ps1 -Mode All
./Tools/test_v08.ps1 -Mode Display -DisplayStep SeedRestart
./Tools/test_v08.ps1 -Mode Display -DisplayStep ResumeRestart
./Tools/test_v05.ps1 -Mode Bosses -Speed 2
./Tools/test_v05.ps1 -Mode Negative -Speed 2
./Tools/test_v05.ps1 -Mode Bosses -Boss 113 -Speed 1
./Tools/test_v05.ps1 -Mode Campaign -Speed 2
./Tools/test_v06.ps1 -Mode Systems -Speed 2
./Tools/test_v07.ps1 -Mode UI
./Tools/smoke.ps1 -ArtReview
```

一次只启动一个播放器。V08 专项固定正常时间，使用独立 QA 存档；重启验证的两个步骤必须相邻运行。原始证据保存在 `Builds/Artifacts/V08QA/<时间戳>-<模式>-<步骤>`，包括断言、截图方式、UI 射线、物种主题表及构建身份。精选记录归档在 [TestResults/v08](TestResults/v08/)。识别二进制使用 Assembly-CSharp SHA256，避免用不随 C# 修改变化的 Unity 启动器代替实际代码身份。

## 发现问题后的迭代

1. 首轮特效自动检查 118 项通过，但人工查看截图后拒绝了过细闪电、平板火焰与矩形浪墙。改成亮芯分叉闪电、三面旋动火舌、羽化卷浪与白沫，降低边界线亮度；保留原危险范围和伤害时机。
2. 首轮显示 152 PASS / 4 FAIL。初始三种真实模式均成功；超时恢复后测试在 UI 的下一帧刷新前点击了仍禁用的按钮。修复测试等待实际可交互控件，同时让恢复操作立即进入 Busy 状态，未放宽实际屏幕模式断言。
3. 首轮十一首领 73 PASS / 3 FAIL，镜渊战卡在离开最后足迹的步骤。旧标记只有三米远，自动行走的到达容差会提前停在离开阈值内；新标记沿玩家真实来路设置在 4.2 米处，实际收集门槛保持 2.7 米。修复后常速完整三阶段 8 项通过。
4. 第二轮综合 328 PASS / 1 FAIL。冰锁刚生成时碰撞体 bounds 尚未同步，验收相机朝向了旧坐标；在读取前同步 Physics，并等待实际更新帧。保留真实枪击与视线检查，第三轮综合 330 项、完整十一首领 76 项均通过。
5. 人工检查第三轮冰锁截图，仍发现近处雪松树冠遮挡目标；树叶没有射击碰撞，所以射线通过不能证明肉眼可见。将实际护炉路线移到西侧开阔岸线，补上全路线地面及树冠检查，继续用实际玩家眼高验收。
6. 最终只读复查发现：显示预览可以用 Esc 或页签提前离开，随后在战斗中自动切回；初次加载时间会侵占显示验证期限；保存失败仍显示成功。统一导航取消入口、把验证计时放在异步等待后，并据实际写盘结果显示状态。新增暂停战斗中切换、页签取消、Esc 连按及恢复后继续战斗检查。
7. 同轮修正低温时仍建议继续冷却的文案、白鲸观测失败残留导线与旧站颜色；珊瑚错拍和熔炉立即过载补上同帧主题反馈。可破坏目标的双行提示改为先确定文字、再计算面板大小。
8. 第四轮综合 380 项通过，截图确认树冠遮挡与提示问题解决；随后仅对仍像方块的冰锁做最后造型调整，替换成蓝白分瓣冰壳与曲线冰齿，保留碰撞体。重新构建后执行最终整套回归。

## 最终结果

最终候选 Assembly-CSharp SHA256：`CEE4131D4DEAF24DD19B2B71381FC23A57390CBBF7B3C627B5877760A94974B5`。Unity 构建成功，0 错误、1 条未连接云项目的警告；游戏为离线单人。

| 检查 | PASS / FAIL | 证据 |
| --- | --- | --- |
| 正常速度首领专项、全部主题、实际技能释放、预算、显示切换与暂停导航 | 380 / 0 | [final-all](TestResults/v08/final-all/results.txt) |
| 独立进程保存确认的无边框模式 | 42 / 0 | [final-display-seed](TestResults/v08/final-display-seed/results.txt) |
| 再次启动恢复模式、显示器原生尺寸与窗口尺寸 | 45 / 0 | [final-display-resume](TestResults/v08/final-display-resume/results.txt) |
| 十一首领完整三阶段、三个实际完成事件、有限补给 | 76 / 0 | [final-bosses](TestResults/v08/final-bosses/results.txt) |
| 十一首领错误动作、传说机制反例及失败恢复 | 89 / 0 | [final-negative](TestResults/v08/final-negative/results.txt) |
| 正常速度冰鲸完整战与新护炉路线 | 8 / 0 | [final-frost-normal](TestResults/v08/final-frost-normal/results.txt) |
| 正常起始钓获、售卖、购买、首领、第二岛与保存继续 | 49 / 0 | [final-campaign](TestResults/v08/final-campaign/results.txt) |
| 生态、专属标本、金币研究、真实精英反制 | 362 / 0 | [final-systems](TestResults/v08/final-systems/results.txt) |
| 商店三种尺寸、价格字形及像素、射线点击、实际扣款 | 435 / 0 | [final-shop-ui](TestResults/v08/final-shop-ui/results.txt) |
| 动画、23 类解剖/首领弱点射线、实际码头视线与表现 | 135 / 0 | [final-art-review](TestResults/v08/final-art-review/results.txt) |

实际显示器为 2560×1440；窗口往返覆盖 1280×720 和 1600×900。进入显示预览后，实际 `Screen` 模式及尺寸稳定才开始确认倒计时。重启证据来自两个独立游戏进程，不是只读回一次内存或 JSON。

以上 10 套共 1621 PASS / 0 FAIL，全部来自同一交付 Assembly；[汇总与逐套身份](TestResults/v08/summary.json) 可核对。冰鲸常速完整战为 66.94 秒，三个机制阶段均完成；此数值来自已知路线的自动操作者。

最终表现检查在 RTX 3080 / Ryzen 9 5900X 上，对单只克拉肯、1600×900、正常时间的实际码头视角采样三秒：平均 11.17 ms、P95 11.46 ms。采样不含截图编码，只是短时观察，不代表最低配置或长时帧率保证。设备与采样条件见 [原始记录](TestResults/v08/final-art-review/presentation-device-and-frametimes.txt)。

完整包为 37,595,246 字节，152 个文件通过 CRC 和逐文件 SHA256 比对，包含本版游玩说明。包内 Assembly 与上述验收一致，详见 [包校验](TestResults/v08-package.json)。

## 检查范围与边界

- 首领专项使用明确的装备、位置和阶段夹具；检查真实射击碰撞体、错误极性、假足迹、鱼竿不能直接断腕、重复原站不能替代跨站定位。完整战另外执行三个实际机制完成事件、真实弹药/装填、有限生命和补给，没有测试无敌。
- 完整战自动操作者知道弱点、机制和安全路线。零受伤说明该解法可执行，不代表初见玩家难度或真人平衡评测。没有把隔离首领战称为九岛自然通关。
- 完整十一首领、错误动作和成长套件以 `timeScale=2` 加速世界时间，角色移动参数不加速；原始断言中的 `normal-speed movement` 指原有移动参数，不表示正常墙钟速度。V08 专项和额外冰鲸完整战为 `timeScale=1`，具体速度在每轮构建身份中记录。
- 119 条记录映射到 11 类主题库，14 种攻击动作产生各自运动表现；不是 119 套独立粒子系统。检查实际攻击生成、释放前无伤害、危险清理、暂停与复用预算。119 个物种逐条映射检查不等于逐一录制全部战斗。
- 特效上限为 128 个网格装饰、40 条路径、20 个浪面和 1520 个粒子。压力检查覆盖装饰饱和和同帧音效节流；超量替换装饰，不替换权威伤害与边界。没有做跨硬件长时性能认证。
- 显示测试调用实际 `Screen.SetResolution`，核对 `Screen.fullScreenMode` 与实际渲染尺寸。控件经 EventSystem 射线与按钮事件执行；Esc 经生产输入处理函数派发，没有模拟系统按键。检查正常/暂停倒计时、显式恢复、跨进程持久化。显示器拒绝模式和磁盘写入失败路径经代码审查，没有注入硬件故障或破坏磁盘权限。
- 图像均来自运行中的 Unity 世界或实际帧缓冲。隐藏窗口黑帧时使用分开的世界/UI 相机渲染，方法记录在证据中；主题展示使用码头上的隔离检查镜头，不冒充自然战斗截图。
- 商店回归验证价格字形、实际像素、三种尺寸和购买扣款。成长回归检查正常起始钓鱼、出售、金币购买及第二岛续玩；生态回归检查专属样本、研究升级与实际精英反制。
- 本轮没有重跑完整九岛任务路径矩阵；变化涉及的护炉路线单独检查，首岛到第二岛仍执行真实步行。旧版全地图路径结果只作为历史记录。

构建警告、最终包 CRC 及文件身份记入本轮结果与 [分发包校验](TestResults/v08-package.json)。本项目仍是持续打磨的可玩开发版本，不把自动检查数量等同于商业品质认证。
