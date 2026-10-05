# v0.7 验收记录

本轮验收覆盖商店金额与可操作性、十二类生物和船员网格、第一人称手部、攻击反馈，以及这些修改涉及的战斗和购买流程。使用本机 Unity 2021.3.16f1c1 Windows x64 构建。子代理负责 UI、怪物、特效及代码复查，根代理集成、启动播放器、检查截图和决定返修。

## 复现

```powershell
./Tools/build.ps1
./Tools/test_v07.ps1 -Mode All
./Tools/smoke.ps1 -ArtReview
./Tools/test_v06.ps1 -Mode Systems -Speed 2
./Tools/test_v05.ps1 -Mode Campaign -Speed 2
./Tools/test_v05.ps1 -Mode Bosses -Speed 2
./Tools/test_v05.ps1 -Mode Negative -Speed 2
```

一次只启动一个播放器。`test_v07.ps1` 还支持 UI、Models、Effects 独立模式，始终使用正常时间。每轮证据保存在 `Builds/Artifacts/V07QA/<时间戳>-<模式>`，包括构建身份、原始断言、作用范围、按钮射线日志和截图。精选证据归档于 [TestResults/v07](TestResults/v07/)。构建身份同时记录 EXE 和 Assembly-CSharp 的 SHA256，避免只用不随 C# 修改变化的 Unity 启动器识别版本。

## 价格缺失原因及验证

SeaFont 的 PointSize 为 42、LineHeight 为 60.816。旧版按钮用 20 pt 字号，约 28.96 px 行高大于 26 px 文本区域，省略策略会使价格整行不出现；锁定说明还会占用价格行。新版单独安排解锁说明和固定高度购买条，价格由目录/实际购买函数产生。

覆盖六个工坊分类与三种分辨率：1600×900、1280×720、1280×1024。逐个价格检查数字字形实际网格、可见标记、文本区屏幕边界及溢出；校对商品目录金额；检查资金不足仍显示 55 金币、满级按钮不可继续购买。三个分辨率均执行 EventSystem 屏幕坐标射线，确认最上层命中目标为购买条，再调用其实际按钮回调，核对 85 金币扣款与升级。这里使用 Unity 事件系统和回调，没有模拟操作系统鼠标。

## 迭代中发现并解决的问题

第一轮 [359 PASS / 3 FAIL](TestResults/v07/iteration1-presentation/results.txt)：所有金额和购买扣款检查通过，三个分辨率的按钮射线检查失败。旧截图助手将 Overlay 临时转成 ScreenSpaceCamera，恢复后未刷新 Canvas；新页也未等到布局帧结束。恢复后强制更新 Canvas，并在射线前等待布局帧完成。保留失败记录，没有移除或放宽射线断言。

同轮模型截图发现白色高亮弱点球遮住新造型、鳍面重叠条纹，以及检查镜头被手持鱼竿和 HUD 遮挡。分别改成有眼睑与虹膜的低亮弱点、按鳍面法线增加厚度；模型检查镜头明确隐藏工具和 HUD。真实战斗镜头继续保留两者。

第二轮 [363 PASS / 0 FAIL](TestResults/v07/iteration2-presentation/results.txt)，同构建另通过 [135 项动画与表现检查](TestResults/v07/iteration2-art-review/results.txt)。但人工查看 1280×1024 的截图时发现整页 TMP 文字消失，说明字形网格断言不能替代像素验收。

第三轮将商店截图改为 `WaitForEndOfFrame` 后的 `ScreenCapture.CaptureScreenshotAsTexture`，直接读取实际 Overlay 游戏帧，不改变 UI 的渲染模式。三种尺寸的原生截图都显示完整文字与金额，证实第二轮窄屏缺字是离屏截图工具问题。[第三轮 363 PASS / 0 FAIL](TestResults/v07/iteration3-presentation/results.txt)，并完成 [Systems 362 项](TestResults/v07/iteration3-systems/results.txt) 与 [Campaign 49 项](TestResults/v07/iteration3-campaign/results.txt)。

最后的外观复查发现鳐鱼、鳗鱼与海马共用冠刺高度导致装饰悬空，按家族体表截面修正根部与比例，随后重新构建验收。没有改动命中体、伤害或解锁规则。

第四轮完整候选构建完成展示、动画、Systems、Campaign、十一首领和错误动作回归。查看实际码头的白鲸侧面后，又给扁平弱点补上无碰撞的组织连接；该修改重新通过展示、23 种解剖/首领弱点射线与完整白鲸战。

随后在隐藏窗口验收中发现 `ScreenCapture` 偶发返回整张黑帧，保留了 [原始黑帧](TestResults/v07/iteration5-presentation/hidden-window-black-frame.png)。最终只修改 QA 截图助手：优先读原生 Overlay 帧；检测到黑帧时，分别渲染世界和 UI 相机，避免近裁剪面及隐藏窗口问题，最后恢复原来的层和 Canvas 状态。新增逐价格区域的亮色字形像素检查，不再仅检查 TMP 网格。[逐图截图方式](TestResults/v07/final-presentation/capture-methods.txt) 区分原生帧与相机渲染，两者都是运行中的游戏内容。

## 最终结果与构建身份

| 检查 | PASS / FAIL | 证据 |
| --- | --- | --- |
| 最终展示、商店字形与像素、三种尺寸、模型、特效复用 | 531 / 0 | [final-presentation](TestResults/v07/final-presentation/results.txt) |
| 最终动画、23 种命中射线、首领实际码头视线与演出 | 135 / 0 | [final-art-review](TestResults/v07/final-art-review/results.txt) |
| 生态、研究购买保存、自然精英战 | 362 / 0 | [iteration4-systems](TestResults/v07/iteration4-systems/results.txt) |
| 从首岛钓获到第二岛购买及续玩 | 49 / 0 | [iteration4-campaign](TestResults/v07/iteration4-campaign/results.txt) |
| 十一首领完整三阶段战斗 | 76 / 0 | [iteration4-bosses](TestResults/v07/iteration4-bosses/results.txt) |
| 十一首领错误动作及传说机制反例 | 88 / 0 | [iteration4-negative](TestResults/v07/iteration4-negative/results.txt) |
| 组织连接微调后的完整白鲸战 | 8 / 0 | [after-sculpture-whale](TestResults/v07/after-sculpture-whale/results.txt) |

这些记录不是全部来自同一个 Assembly：第四轮完整玩法回归为 `595F424B72C5BC4649C66E58B9BD4C04DE31B38037F753018DE1CE4AEC94A1AD`；增加纯外观组织连接后的白鲸回归为 `DD576E8701A1DD2AA01B5F0874FE6132E3DBB98E3D477C1822493F53DF3648BF`。最后仅调整 QA 截图工具与像素检查，交付 Assembly 为 `CBBADDD1B98A149778322058DC7181BCDDAB75B3865DFA9FEA64396289127A52`，执行上表最终两套表现检查。没有把旧构建结果冒充交付二进制重新跑过的完整玩法套件。

最终编译成功，0 错误、1 条未连接 Unity 云项目的构建警告；本游戏离线运行。测试使用独立临时 QA 存档目录，不覆盖正常玩家存档。压缩包 CRC、必要运行文件及 Assembly/EXE 身份见 [分发包校验](TestResults/v07-package.json)。

## 检查边界

- 展示套件使用预设金币、装备与岛屿访问权限；模型在隔离检查镜头中生成；特效压力场景主动发射 180 组命中和弹道。它们用于查模型、文本和对象复用，不能当成自然赚取装备或真人操作难度。
- Models 检查三座岛的向导和商人、十二个精英体型、部分岛屿首领及两位传说首领。检查有效网格和弱点保留。ArtReview 另验证十二类和十一首领的正面弱点射线、动态网格弯曲、实际码头视线及首领动作；白鲸先完成实际声呐机制再检查露出。
- Effects 检查六枪真实 `Fire` 发射、弱点/击杀/水花分层画面、连续效果不会增加发射器或 Renderer，离岛清理粒子、网格装饰和弹道。固定池控制分配，不代表所有硬件性能认证。
- Systems 覆盖地域鱼池、专属样本来源拒绝、金币购买与封顶、存档、研究效果，以及实际抛岸的精英水母和海马装置可见、可用枪击破坏并用有限生命击败。
- Campaign 从正常起始经济出发，步行、接委托、实际钓鱼射击、售卖、点击工坊购买 85 金币改装、灯塔守护、三阶段巨蟹、交付、第二岛 150 金币霰弹枪、保存与继续。自动操作者知道规则和弱点，不代表初见玩家体验。
- Bosses 和 Negative 使用隔离装备配置、已知机制解法、自动瞄准和有限补给，验证正确/错误动作的实际结果，没有把它们称为九岛自然通关。

没有重跑前版完整九岛路径矩阵：本轮地图路径、任务位置和建筑碰撞没有修改；人物新增网格没有碰撞体。首岛到第二岛自然流程仍执行真实 CharacterController 步行。全地图路径证据仍属于 v0.6 历史记录。

本轮没有真人盲玩或跨硬件长时性能测试。采用真实游戏截图与程序化网格建模，不称为 119 套独立高精度模型，也不把自动断言数量视为商业品质认证。
