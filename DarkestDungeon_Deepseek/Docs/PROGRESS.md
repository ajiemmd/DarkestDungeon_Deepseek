# Battle Learning Progress
- CurrentLesson: L02
- CurrentMilestone: M1（无画面战斗模拟）
- Status: L01 已通过（学员自测确认；Agent 侧验证到编译产物与 asmdef 配置，未直接观测 Test Runner 日志）；L02 进行中
- UnityVersion: 2022.3.62f3c1（Unity 中国版）
- QFrameworkVersion: 未引入（计划 L08，目标 QFramework v1.0）
- LastCommit: 09d5da8

## 本次学习记录
- 日期：2026-10-29
- 本次目标：L01 工程初始化与分层 —— 建立纯净 C# 战斗核心程序集 + 其 EditMode 测试程序集，打通 Git 基线。
- 我实际完成：用 Unity Hub（2022.3.62f3c1，2D Built-In 模板）建工程；建 Assets/Game/Battle/{Core/{Domain,Contracts},Tests/EditMode,Scenes}；Battle.Core.asmdef 置空依赖并勾 No Engine References + Auto Referenced；测试程序集 Battle.Core.Tests.EditMode 放在 Battle/Tests/EditMode 且引用 Battle.Core；跨程序集断言 Assert.AreEqual(4, BattleConstants.TeamSize)；git init -b main、写 Unity .gitignore、首个提交并 push 到 GitHub；按审查意见修正 Contract→Contracts、删除模板残留 using。
- 测试结果：1/1 passed（学员自查确认；Agent 侧可证：Battle.Core.dll 与 Battle.Core.Tests.EditMode.dll 编译产物存在、Battle.Core.csproj 中 UnityEngine 引用为 0）
- 我能解释的关键原理：asmdef 决定程序集边界；No Engine References 让 Core 物理上无法 using UnityEngine；EditMode 测试用 NUnit、Include Platforms=Editor；ordered list 让「一人一格」「rank 连续」成为免费不变量；Core 内输出用 TestContext 而非 Debug.Log。
- 尚未解决的问题：用例 B（Core 内写 using UnityEngine; 必须 CS0246）的报错原文尚未归档；工程仍位于子目录 DarkestDungeon_Deepseek/DarkestDungeon_Deepseek，待与仓库根合并（ADR-002）。
- 下一步唯一任务：L02 —— 实现 BattleUnit / Formation（GetUnitAtRank / GetRankOfUnit / MoveUnitToRank / RemoveUnit），并让 MoveUnitToRank("u4", 2) 之后 rank 序列变为 [u1, u4, u2, u3]。

## 课程状态
- [x] L01 工程初始化与分层
- [ ] L02 单位与阵型
- [ ] L03 回合状态机
- [ ] L04 技能合法性
- [ ] L05 伤害与事件
- [ ] L06 位移和补位
- [ ] L07 AI 与胜负
- [ ] L08 QFramework 集成
- [ ] L09 UGUI 可玩版
- [ ] L10 状态效果
- [ ] L11 尸体/濒死/压力
- [ ] L12 Luban 与交付

> 本文件由 Agent 起草；「我能解释的关键原理」一栏请按你自己的真实理解改写后提交。
