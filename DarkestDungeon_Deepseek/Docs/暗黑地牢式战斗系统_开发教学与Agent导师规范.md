# 《暗黑地牢1》风格战斗系统：开发教学与 Agent 导师规范

> Unity / UGUI / QFramework / UniTask / Luban ｜ 项目实施说明 + 分阶段学习计划 + Agent 教学协议  
> 文档版本：v1.0 ｜ 编写日期：2026-10-29 ｜ 文档性质：**学习型复刻项目规范**

## 0. 如何使用这份文档

**给开发者：** 目标不是照着代码拼出一个可运行的界面，而是亲自理解并实现一套“回合制 + 一维站位 + 技能位置限制”的战斗规则。每完成一个里程碑，必须能独立解释设计、运行测试并复现结果。

**给 Agent：** 请首先阅读第 10 章《Agent 导师协议》，再执行第 11 章《首次会话指令》。不要一次性生成全部源码，不要跳过开发者的思考与代码练习，也不要假设你已连接 Unity 编辑器或本地仓库。

**本文的约定：** “原作参考”表示从公开资料抽取的机制；“项目实现规则”表示本 Demo 明确约定的行为；“后续扩展”表示暂不属于 MVP。即使规则类似原作，也不要求复制角色、美术、文本和音效等受保护内容。

### 0.1 一页总览

| 项目维度 | 定义 |
| --- | --- |
| 项目目标 | 可独立运行的 4v4 一维站位回合制战斗 Demo，逐步接近《暗黑地牢1》战斗体验 |
| 主要技术 | Unity、UGUI、C#、QFramework Architecture / Model / System / Command |
| 迭代技术 | UniTask（表现异步）、Luban（配置表），YooAsset/HybridCLR 暂缓 |
| MVP 的终点 | 完整战斗循环、站位/目标判定、技能伤害/治疗/移位、基础 AI、胜负结算、最小 UI |
| 教学原则 | 先理解 → 自己实现 → 测试验证 → 复盘 → 再进入下一课 |
| 成功标准 | 不依赖场景也可运行并测试核心规则，能够解释、修改、扩展所有关键模块 |

## 1. 项目目标、范围与不做什么

### 1.1 最终学习目标

1. 能区分回合（Round）、单位行动（Turn）、战斗指令（Action）、技能效果（Effect）和战斗事件（Event）。
2. 能将战斗规则拆成可测试的 C# 领域模型，而不是写入 MonoBehaviour 或 Button.onClick 中。
3. 能用 QFramework 正确组织 View / Command / System / Model，做到表现与规则解耦。
4. 能处理站位交换、拉动、击退、死亡补位、非法技能、角色被眩晕等边界条件。
5. 能实现完整的“指令校验 → 逻辑结算 → 事件产生 → UI 演出 → 下一行动”流程。
6. 能通过测试、日志、固定随机种子和 Git 记录证明实现是正确的。
7. 能完成一次完整的可展示项目，并清楚解释架构取舍与自己的贡献。

### 1.2 MVP（必须先完成）

- 一场 4v4 战斗；角色和敌方分别最多占据 4 个站位。
- 三类原创测试英雄（前排、防御/控制；中排、输出；后排、治疗），三类原创测试敌人。
- 两组阵型：1 为最前排、4 为最后排；MVP 所有单位各占 1 格。
- 每轮混合行动顺序，每个存活普通单位默认行动一次；速度加入可复现随机判定。
- 技能使用站位、合法目标站位、目标阵营与目标存活状态校验。
- 伤害、治疗、闪避/命中、暴击（基础版可先固定数值后引入概率）。
- 向前/向后移动、击退、拉动；死亡后无尸体的阵型补位。
- 基础敌方 AI：从合法技能和目标中选择行动，不允许非法动作。
- UGUI：站位展示、当前行动单位、技能栏、可选目标高亮、血条、结果面板。
- 战斗结束：一方所有可战斗单位死亡，正确进入胜负结算状态。

### 1.3 不属于 MVP，按阶段加入

- 流血、中毒、眩晕、增减益、抵抗与标记。
- 敌方尸体占位、尸体清除、大型怪物（占 2 格以上）。
- 压力、濒死之门、死亡抗性、特殊死亡判定。
- 完整角色职业、关卡探索、营地、城镇、背包、存档、网络战斗。
- 商业级 UI、原作角色与资源复用、资源热更和程序集热更新。

### 1.4 完成定义（Definition of Done）

单项功能只有同时满足 **功能正确、异常/边界条件覆盖、自动化测试或明确的手工用例、运行日志可追踪、开发者能口述原理**，才算完成。仅“没有编译报错”不算通过。

## 2. 规则分层：原作参考与项目约定

### 2.1 回合与出手顺序

**原作参考：** 每轮开始，行动顺序受 SPD（速度）和 1～8 的随机值影响；普通单位每轮通常行动一次，少数特殊角色和 Boss 可以例外。速度相同存在额外平局规则。来源见第 13 章。

**项目实现规则（MVP）：**

1. `RoundIndex` 从 1 开始。每轮进入 `RoundStart`，收集所有存活且可行动的单位。
2. 每个单位生成 `Initiative = SPD + Random.NextInclusive(1, 8)`；按照 Initiative 降序排列。
3. 平局先按我方优先，再按更靠前的站位（Rank 更小）优先，最后按稳定 `UnitId` 排序，确保日志可复现。
4. 排序在轮开始确定；本轮中途更改 SPD 不重新排序（该简化规则先写死，后期可配置）。
5. 每单位成功执行一次合法行动或因控制失去行动后标记为已行动。非法输入不消耗行动。
6. 本轮队列耗尽后进行 `RoundEnd`，检查胜负，再进入下一轮。
7. 单位如果在轮内死亡，跳过其尚未执行的行动；即使被复活也不自动插入本轮队列（暂不做复活）。

> 教学重点：回合推进必须由状态机驱动，而不是写成 `while(true)` 阻塞 Unity 主线程；表现动画完成不能直接更改战斗规则。

### 2.2 一维站位与目标选择

- 每方有 1～4 四个 Rank，**Rank 1 最靠近对手**。UI 可反向排列，但数据模型含义始终相同。
- MVP 所有单位体型 Size=1；一方角色按 Rank 顺序有序存储，位置是战斗数据而不是世界坐标。
- 施法位置 `CasterRanks` 与受击位置 `TargetRanks` 分别设置，均可用 4 位 bitmask 表达。
- 技能必须进一步声明 `TargetSide`（Enemy/Ally/Self）、`TargetMode`（Single/AllMatching），不能只靠“位置是否合法”判断目标。
- MVP 的前后移动采用“**抽出并插入**”算法：把目标从当前 Rank 移到新 Rank，中间单位相应顺移，超出 1～队伍人数的移动被截断；最终所有单格单位保持连续有序。
- 单位死亡且不生成尸体时，其后面的存活单位向前补位；保留相对顺序。后续引入尸体后，补位规则由占位对象决定。
- 移位或死亡后必须重新计算角色 Rank；所有目标/技能的合法性也必须重新计算。

**示例：** 队列 `[盾卫, 游侠, 医师, 术士]`（对应 Rank 1～4），术士被拉动到 Rank 2，结果为 `[盾卫, 术士, 游侠, 医师]`；这不是 Swap（两人换位），而是“插入式重排”。Swap 应另设效果类型。

### 2.3 技能与数值计算

**本项目的初始测试技能（原创占位数值，不代表原作）：**

| SkillId | 技能 | 可用 Rank | 目标 Rank | 目标 | 效果 |
| --- | --- | --- | --- | --- | --- |
| 1001 | 盾击 | 1、2 | 敌方 1、2 | 单体 | 基础伤害 6 |
| 1002 | 钩索 | 2、3 | 敌方 3、4 | 单体 | 伤害 3 + 向前拉 2 |
| 1003 | 包扎 | 3、4 | 我方 1～4 | 单体 | 治疗 5 |
| 1004 | 后撤射击 | 2、3、4 | 敌方 2～4 | 单体 | 伤害 4 + 自身后移 1 |

**先实现确定性伤害与治疗，再实现概率。** 推荐逐步引入 `HitChance = Clamp(BaseAccuracy + AttackerAccMod - TargetDodge, 5, 95)`、暴击、护甲等；这些是 **Demo 的教学公式**，不能误写成《暗黑地牢1》的完整原始公式。概率全部经由统一随机服务；未来若要求高还原，必须对照资料与版本逐项修订。

### 2.4 伤害/死亡/尸体的过渡规则

- MVP：HP 不低于 0，不高于 MaxHP；受到致命伤时立即死亡并从可行动队列剔除；无尸体则队伍自动补位。
- 扩展版：敌方可生成 `Corpse` 占位实体（不作为可行动单位）；其存在影响后排补位与目标选择。
- 扩展版：我方英雄 HP 降至 0 进入 `DeathsDoor`，不立刻按普通怪物规则死亡；新受伤会触发死亡抗性判定。
- 特殊效果（DoT、暴击清尸等）及大型怪物占位需有独立、显式的规则，不能混进通用 `OnDead()` 回调里。

## 3. 核心架构设计

### 3.1 总体数据流

```text
玩家输入 / 敌方 AI
       ↓
UseSkillCommand / MoveCommand（提出战斗意图）
       ↓
BattleFlowSystem（检查当前阶段与当前行动者）
       ↓
SkillSystem → TargetValidator → EffectResolver
       ↓
DamageSystem / FormationSystem / StatusEffectSystem
       ↓
BattleModel（保存战斗结果） + BattleEventLog（按序产出事件）
       ↓
BattlePresenter / UGUI（消费事件，播放演出）
       ↓
演出结束回执 → 战斗流程允许下一次操作
```

**规则层与演出层绝不可互相反向依赖。** 例如：扣血必须在规则层完成；动画/飘字只展示已结算结果。动画被跳过、加速或取消时不得导致两次扣血。

### 3.2 QFramework 职责边界

| 组件 | 应做 | 不应做 |
| --- | --- | --- |
| `BattleModel : AbstractModel` | 保存当前战斗快照、阶段、队伍、轮次、战斗日志等 | 访问 Animator / 直接操作 UI |
| `BattleFlowSystem : AbstractSystem` | 管理状态机、轮次、当前行动者、推进时机 | 直接更新 TMP_Text、创建 GameObject |
| `FormationSystem : AbstractSystem` | 合法位移、插入式重排、死亡补位 | 决定 UI 中角色应该位于多少像素 |
| `SkillSystem : AbstractSystem` | 技能可用性、目标校验、效果分发 | 直接播放技能动画 |
| `UseSkillCommand` | 传入明确意图并委派领域服务执行规则 | 持有长生命周期、缓存可变战斗状态 |
| `BattlePanel : IController/MonoBehaviour` | 收集点击、读取状态、监听事件、播放演出 | 自己计算最终伤害、随机命中或决定合法目标 |
| `IRandomProvider`、配置仓储 | 封装随机源、静态配置访问 | 混入业务 UI |

**落地建议：** 核心规则优先放在不依赖 UnityEngine 和 QFramework 的纯 C# 类中；QFramework System 是业务协调与容器接口，不要把所有战斗算法塞进巨大的 `BattleFlowSystem`。这样 EditMode 甚至独立 .NET 测试更容易执行。

### 3.3 最小纯 C# 领域对象（示例，不要求照抄）

```csharp
public enum TeamSide { Heroes, Enemies }
public enum BattlePhase {
    NotStarted, RoundStart, AwaitingAction,
    ResolvingAction, RoundEnd, Victory, Defeat
}

public sealed class BattleUnitState {
    public int UnitId;         // 单场战斗内唯一
    public int CharacterId;    // 静态配置 Id
    public TeamSide Team;
    public int Rank;           // 1 = 前排；MVP: 1..4
    public int Hp;
    public int MaxHp;
    public int Spd;
    public bool IsAlive => Hp > 0;
}

public readonly struct BattleActionRequest {
    public readonly int ActorId;
    public readonly int SkillId;
    public readonly int TargetId;
    public BattleActionRequest(int actorId, int skillId, int targetId) {
        ActorId = actorId; SkillId = skillId; TargetId = targetId;
    }
}
```

**扩展提醒：** 加入濒死之门后，`IsAlive => Hp > 0` 将不再成立。应将“活着”“可行动”“占据位置”拆为不同性质；从教学上先用最简单实现，再安排正式重构，不能默默忽略这项设计债务。

### 3.4 战斗状态机

```text
NotStarted
   ↓ StartBattle
RoundStart ─→ 生成本轮行动队列
   ↓
AwaitingAction ─→ 收集合法 ActionRequest（人/AI）
   ↓
ResolvingAction ─→ 修改 Model + 产出 Events
   ↓
检查胜负 ─→ Victory / Defeat（终态）
   ↓ 未结束
本轮还有可行动单位？── 是 → AwaitingAction
           │ 否
           ↓
       RoundEnd → RoundStart
```

**禁止的状态迁移：** 在 `Victory/Defeat` 之后继续接受技能；在 `ResolvingAction` 中重复点击技能；任何未经过合法性校验的 `Request` 修改 Model。

### 3.5 事件与演出契约

优先采用可序列化的 `BattleEvent` 记录：`RoundStarted`、`TurnStarted`、`ActionUsed`、`AttackMissed`、`DamageApplied`、`HealApplied`、`UnitMoved`、`UnitDied`、`BattleFinished`。每条事件至少包含 `SequenceId / EventType / SourceUnitId / TargetUnitId / Payload`。

`ActionResult` 可包含 `Success/FailReason/Events`；逻辑先一次性完成原子结算，再由 Presenter 逐条播放演出。UI 的动画等待使用 UniTask + CancellationToken；对取消、跳过、战斗退出必须有确定语义。日志不要依赖 UI 监听成功才能保存。

## 4. 数据驱动与工具接入策略

### 4.1 数据模型与配置模型严格分离

**配置表（静态）：** `Character`（角色基础属性、职业、技能列表）、`Skill`（施法位置、目标规则、效果 ID）、`Effect`（EffectType、数值、持续轮数、目标类型）、`EnemyAI`（候选技能、权重/条件）。

**运行态（动态）：** UnitId、当前血量、当前 Rank、已行动、状态叠加层数、剩余回合、临时增益、角色队伍。禁止在战斗过程中修改 Luban 生成的静态配置行来保存当前 HP。

### 4.2 推荐 Skill 表字段

```text
SkillId            int
Name               string
CasterRankMask     int      # Rank 1..4 四个位
TargetRankMask     int
TargetSide         enum     # Self/Ally/Enemy
TargetMode         enum     # Single/AllMatching
BaseAccuracy       int
EffectIds          list<int>
AnimationKey       string   # 表现层使用，可为空
```

配置校验需要覆盖：技能 ID 不重复、Mask 不能非法或全 0、效果引用存在、单体技能必须有选择目标规则。先用手写常量/JSON 跑通 MVP，**到第 12 课再换成 Luban**，避免学习过程同时卡在编辑器、表导出、运行时加载三条线上。

### 4.3 UniTask 的使用范围

- 用于：角色进场、技能动作、受击演出、飘字、回合 UI 动画、等待场景或资源加载。
- 不用于：决定规则结果、靠等待动画持续时间判断命中、替代纯 C# 规则模拟。
- 所有异步演出接受 `CancellationToken`，在关闭战斗界面/销毁对象/战斗重开时取消；异常进入可观察日志。
- 避免在 Editor 脚本或纯测试环境中无条件依赖 Unity PlayerLoop。

### 4.4 暂缓采用的技术

YooAsset、HybridCLR、VContainer/R3 不作为这个战斗 Demo 的初期强制依赖。只有当 MVP 与自动化测试稳定后，才评估资源分包、热更新、依赖注入或响应式 UI 的实际收益。不要为了“技术栈完整”破坏学习主线。

## 5. 建议目录与依赖方向

```text
Assets/
└─ Game/Battle/
   ├─ Core/                 # 纯 C#；不可依赖 UnityEngine
   │  ├─ Domain/            # BattleUnitState / BattleState / Formation
   │  ├─ Rules/             # TargetValidator / EffectResolver / DamageCalculator
   │  ├─ Contracts/         # IRandomProvider / IConfigProvider / Result
   │  └─ Events/            # 结构化战斗事件
   ├─ Framework/            # QFramework 适配层
   │  ├─ BattleArchitecture.cs
   │  ├─ Models/
   │  ├─ Systems/
   │  └─ Commands/
   ├─ Config/               # 前期测试数据，后期 Luban 适配
   ├─ Presentation/         # UGUI/MonoBehaviour/演出控制器
   │  ├─ Views/
   │  └─ Presenters/
   ├─ Scenes/               # BattleDemo.unity
   └─ Tests/
      ├─ EditMode/
      └─ PlayMode/
Docs/
├─ PROGRESS.md              # Agent 每课维护
├─ DESIGN_DECISIONS.md      # 架构决策记录 ADR
└─ BATTLE_TEST_CASES.md     # 测试用例清单
```

**强约束：** `Core` 不引用 `UnityEngine`、`UnityEditor`、`QFramework`；`Presentation` 可以依赖 Framework 暴露的只读模型/事件；`Core` 不回头引用 Presentation。最终以 asmdef 或编译依赖检查强化边界。

## 6. 课程路线图：12 课，推荐 8～10 周

时间是建议，不是硬性要求；每课都以“验收通过”为唯一晋级条件。若每周投入约 6～8 小时，可以按下述安排。允许中途重学或加练，不允许把未通过验收的内容标记为完成。

| 课次 | 核心主题 | 主要交付物 | 至少应会回答的问题 |
| --- | --- | --- | --- |
| L01 | 工程初始化与分层 | Git 仓库、测试程序集、空 BattleDemo 场景 | Core 为什么不引用 UnityEngine？ |
| L02 | Unit / Formation 数据结构 | 4v4 状态、站位查询/重排 + 测试 | Rank 与 Transform.position 有何区别？ |
| L03 | 回合状态机与速度 | Round、行动队列、固定种子日志 | Round、Turn、Action 分别是什么？ |
| L04 | 技能合法性校验 | SkillConfig、TargetValidator | 非法请求为什么不能消耗行动？ |
| L05 | 伤害/治疗/死亡 | EffectResolver、DamageCalculator | 为什么逻辑扣血不能放动画里？ |
| L06 | 位置变化与边界 | 拉动/击退/移动/死亡补位 | Pull 与 Swap 有什么差异？ |
| L07 | 敌方 AI 与胜负流程 | 规则型 AI、结果判定 | AI 怎样保证永远不发非法指令？ |
| L08 | QFramework 集成 | Architecture / Model / System / Command | 规则为什么不全塞入 System？ |
| L09 | UGUI 可玩版本 | 角色站位/技能栏/目标高亮/结算页 | 动画播放失败时如何继续战斗？ |
| L10 | 状态效果系统 | Stun、Bleed、Blight、Buff 测试 | DoT 的结算点与持续期如何定义？ |
| L11 | 特色机制扩展 | 尸体、濒死之门、压力（分小任务） | 尸体是不是单位？为何要独立对象？ |
| L12 | Luban 与交付质量 | 数据生成、回归测试、说明文档、Demo | 新技能能否只配置不改核心代码？ |

### 6.1 里程碑与晋级门槛

**M0（L01）工程可运行。** Unity 版本、包版本、C# 脚本编译正常；EditMode 测试至少一条；提交首次 Git commit。

**M1（L02～L03）无画面战斗模拟。** 能用日志模拟两个四人队伍的轮次；同 Seed 相同初始状态得到同一序列；行动结束后不重复行动。

**M2（L04～L06）可计算的战斗。** 技能/目标合法性、伤害治疗、位移、死亡与补位；至少 15 个自动测试通过。

**M3（L07～L09）可玩的 MVP。** 玩家可鼠标点击完成 4v4；AI 自动行动；战斗结果正确；快速连点、重开、关闭界面不造成重复行动或死锁。

**M4（L10～L11）特色机制扩展。** 状态机能处理控制、持续伤害、尸体、濒死之门；补全边界测试。

**M5（L12）数据驱动交付。** 修改角色/技能配置即可生成新内容；单场战斗日志可复现；README、架构图、测试证据齐全。

### 6.2 每课统一的教学节奏

1. **开场诊断（5 分钟）：** Agent 询问上次任务完成情况，让开发者复述当前核心概念。
2. **讲清原理（10～15 分钟）：** 用 1 个具体战斗例子说明目标、数据流与替代方案。
3. **最小练习（20～40 分钟）：** 只布置一个能独立完成的代码增量；先给接口/伪代码/提示，不直接给全解。
4. **运行验证（10～20 分钟）：** 写测试、运行、阅读日志，分析失败边界。
5. **复盘（5～10 分钟）：** 开发者总结；Agent 按评分表审核、记录进度与下一课前置条件。

## 7. 前六课的精细任务卡

### L01：工程初始化与依赖边界

- 讲解：Assembly Definition、EditMode/PlayMode 测试、Git 基线、纯 C# 代码和 Unity 组件区别。
- 开发者任务：建立建议目录；创建 `Battle.Core` 和测试程序集；运行一个 `Assert.AreEqual(1, 1)` 测试。
- Agent 应核查：Unity 实际版本、QFramework 是否已安装、目录是否为新项目/现有项目、程序集之间的引用、能否执行测试；不能自称运行成功而没有日志。
- 验收：编辑器零编译错误；测试为绿色；首个 Git 提交可定位；解释为什么要做分层。

### L02：阵型与单位

- 讲解：静态配置 vs 运行时数据、有序列表 vs 字典、Rank 的索引基准、数据不变量。
- 任务：定义单位、队伍、站位读写；实现 `GetUnitAtRank`、`GetRankOfUnit`、`MoveUnitToRank`。
- 练习：把 Rank 4 单位插入 Rank 2，并打印四个 Rank 的最终单位 ID。
- 验收：正常移动、无效 Rank、重复 UnitId、死亡剔除与顺序保持均有测试；不会产生两个单位同占一格。

### L03：回合队列

- 讲解：状态机、稳定排序、1～8 速度骰、随机接口、固定 Seed 的意义。
- 任务：`StartBattle`、`StartRound`、`GetCurrentActor`、`FinishActorTurn`，记录行动日志。
- 验收：普通单位每轮恰好行动一次；速度平局可复现；死亡单位跳过；两次相同 Seed 得到相同顺序。

### L04：技能合法性

- 讲解：Command vs Request、前置条件校验、TargetSide/TargetMode、4 位 RankMask。
- 任务：`CanUseSkill(actorId, skillId)` 和 `GetValidTargets(actorId, skillId)`；返回错误码而不是仅 `false`。
- 验收：后排无法释放前排技能；不能打队友或已死亡目标；空合法目标时不能消耗行动；失效 UI 点击不会修改状态。

### L05：结算与战斗事件

- 讲解：命中/伤害结算、生命边界、原子操作、事件作为事实而非命令。
- 任务：执行攻击和治疗；输出 `ActionUsed / DamageApplied / UnitDied` 的有序事件。
- 验收：HP 始终落在合法范围；非法请求既不扣血也不推进回合；行动结果和日志一致；重复消费同一事件不重复结算。

### L06：击退、拉动与阵型变动

- 讲解：插入式移动、稳定顺序、目标移动对技能合法性的影响；移动方向以“面向敌人”为参照。
- 任务：实现 `Pull`、`Push`、`MoveSelf`；对死亡后补位与位移上限进行一致处理。
- 验收：被拉到 2 号位后的其它单位正确顺移；边界 Rank 不越界；技能后再次查询的 Rank 正确；后排无法错误穿越阵型。

## 8. 后六课的精细任务卡

### L07：敌人 AI 与战斗结束

- AI 分三步：收集合法技能 → 收集合法目标 → 在候选中按权重或简单优先级选择；没有合法攻击时可选择跳过或允许的移动。
- 不要一开始引入行为树；先使“规则型 AI 可测试”。
- 验收：AI 绝不提交非法指令；没有合法动作时不会死循环；最后一名敌人死亡后立即结算。

### L08：QFramework 架构集成

- 将纯 C# Core 经由 QFramework `Model/System/Command` 组织；UI 只能发送 Command 或读取状态。
- 规则计算不要在 Command/Panel 中重复实现；System 不应仅是巨大静态方法集合。
- 验收：Command 能执行且产生结果事件；同一套规则无需打开场景就能被测试；Model 可通过查询被读取但不可从 UI 任意修改。

### L09：UGUI + UniTask 表现

- UI 首版只做：两侧四人站位、角色状态、行动指示、技能按钮、合法目标高亮、技能结果文本、结束弹窗。
- PlayerLoop 异步的取消绑定到 Battle View 生命周期；禁用双击重复提交；跳过动画仅影响表现，不影响逻辑。
- 验收：单场完整战斗；中途关闭/重新开战没有陈旧动画修改新一场战斗 UI；无异常持续堆积。

### L10：状态与效果流水线

- 定义效果执行时机：`OnTurnStart`、`BeforeAction`、`OnTurnEnd`、`OnRoundEnd`；**先定时点，再写代码**。
- Stun：消耗目标的下一次行动；DoT：在明确时点扣血并检查濒死/死亡；Buff：叠加与到期规则可配置。
- 验收：眩晕不导致队列卡死；DoT 致死能正常补位与结算；叠层与持续时间有精确测试。

### L11：尸体、濒死之门、压力

- 先将“单位生命状态”重构为显式状态，避免 `Hp > 0` 与濒死规则冲突。
- 再加入独立 `CorpseSlotOccupant`，验证多占位实体、尸体清除与后排补位。
- 最后添加压力、阈值效果、心脏病发作等机制；先用测试值，再决定是否逐步贴近原作数值。
- 验收：0 HP 英雄进入濒死但仍占位；尸体不行动却会占位；相同 Seed 下死亡抗性与压力判定可复现。

### L12：Luban、质量、展示

- 以当前项目实际 Luban 版本为准建立 Character/Skill/Effect/AI 表，检查引用完整性。
- 自动生成配置、加载并执行测试；新增一个敌人及两种技能，确保不修改核心规则代码。
- 输出 README：环境、启动方式、规则、架构图、截图/演示视频地址、已知缺陷、测试结论。
- 验收：新机器可按说明运行；固定种子回归通过；能够口头讲清一次 Action 从 UI 到结果的全流程。

## 9. 测试清单与完成证据

**统一规则：** Core 规则优先写 EditMode 测试；UGUI、演出取消与交互再用 PlayMode/手工测试。固定随机种子和输入、输出日志是必需的调试工具。

| 用例 ID | 前提 / 行为 | 必须成立的结果 |
| --- | --- | --- |
| T01 | 初始 4 英雄 + 4 敌人 | 两队 Rank 均为 1～4，无冲突 |
| T02 | 把 Rank 4 插入 Rank 2 | 顺序变为 [1,4,2,3] |
| T03 | 对 Rank 0/5 进行移动 | 返回失败或截断（按接口定义），不破坏阵型 |
| T04 | 使用未配置的 SkillId | 拒绝并带错误码 |
| T05 | CasterRank 不合法 | 不扣血、不消耗行动 |
| T06 | 目标已死亡 | 不允许指定为普通单体目标 |
| T07 | 我方技能被设为 Enemy | 不能选中友方 |
| T08 | 输出确定性伤害 6 | HP 恰好减少 6，最低为 0 |
| T09 | 治疗目标剩 2/10，治疗 5 | HP 为 7/10 |
| T10 | 治疗目标剩 9/10，治疗 5 | HP 为 10/10 |
| T11 | 同 Seed + 同输入重复跑 | 完全相同的行动/伤害日志 |
| T12 | 完整一轮有 8 个普通存活单位 | 每人恰好行动一次 |
| T13 | 单位死于轮内且尚未行动 | 从剩余队列跳过 |
| T14 | 非法 ActionRequest | 不推进当前行动者 |
| T15 | 拉动 Rank 4 → Rank 2 | 位置合法且其它单位顺移 |
| T16 | 击退已经在后排边界的目标 | 不越界，返回明确位移结果 |
| T17 | 一方最后单位死亡 | 进入 Victory/Defeat，拒绝后续行动 |
| T18 | AI 无合法攻击 | 选择合法备选或跳过，不死循环 |
| T19 | 单击之后快速连点技能 | 只有一次结算和一次扣血 |
| T20 | 演出期间关闭 BattlePanel | 异步取消、无悬空回调修改新界面 |
| T21 | DoT 使当前单位死亡 | 日志、阵型、轮次、胜负一致 |
| T22 | Stun 跳过行动 | 单位只失去规定的一次行动 |
| T23 | 前排敌人生成尸体 | 尸体占位，后排不能提前补位 |
| T24 | 英雄 HP 降至 0 | 扩展版进入濒死，不立即移出站位 |
| T25 | 配置引用不存在的 EffectId | 加载校验失败并指出具体配置项 |

### 9.1 每课提交证据

- Git commit 哈希及简短改动摘要。
- 测试名称、测试运行结果（如 18/18 passed），或明确说明未运行。
- 必要的控制台日志、失败截图；UI 课附一次完整战斗的截图/录像。
- 开发者用自己的话回答“为什么这样设计”“换一种实现有什么代价”。

### 9.2 评分标准（每课 100 分）

| 维度 | 分值 | 核心标准 |
| --- | --- | --- |
| 规则正确性 | 35 | 正常路径与边界行为符合本课规则 |
| 架构与可维护性 | 25 | 依赖方向正确、职责清晰、没有重复逻辑 |
| 测试与可复现 | 25 | 自动化测试、固定种子、日志/证据充足 |
| 理解与表达 | 15 | 能独立解释核心代码、问题原因与取舍 |

**建议通过线：≥ 80 分，且规则正确性不得低于 25/35；低于通过线，Agent 应安排补课，不进入下一课。**

## 10. Agent 导师协议（最重要的执行规则）

### 10.1 你的身份

你是“Unity 回合制战斗系统技术导师 + Code Reviewer + 结对开发教练”，**不是一次性代写项目的代码生成器**。目标是帮助学习者独立建立战斗系统，理解每个设计决定，并形成工程能力。解释和交流一律使用中文，代码标识符使用清晰的英文命名。

### 10.2 会话开始前必须做的事

1. 先阅读本规范、`Docs/PROGRESS.md`、`Docs/DESIGN_DECISIONS.md`（如果存在）。
2. 如果有本地工程访问能力，检查 Unity 版本、Package/asmdef、现有目录、测试运行方式和 Git 状态；若无访问能力，**明确说明未连接工程**，让学习者提供必要信息，不可编造已读文件或已运行测试。
3. 判断正在学习哪一课，优先完成当前课验收，不得擅自跨越多个课次。
4. 每次仅提出 1 个小任务；不要因开发者提问而自动重构全工程。
5. 变更任何文件前，先告知“将改什么、为什么、怎样验证”；若具备写入权限，按学习者同意的操作方式执行。

### 10.3 教学回答的固定格式

每次技术教学优先使用如下结构：

**① 本课目标：** 用一两句话说清要实现的可见结果。

**② 原理与反例：** 解释规则、职责分层和常见错误；最多先讲当前任务需要的知识。

**③ 你来实现：** 给类/方法建议、输入输出、伪代码或局部示例，优先让学习者自己写主体代码。

**④ 如何验收：** 给一个正常用例、一个边界用例、一个需要观察的运行结果。

**⑤ 复盘问题：** 询问学习者对关键概念的理解，在确认后更新学习进度。

### 10.4 提示阶梯：不得直接把学习环节跳过

- 第 1 级：只给目标、约束、输入输出、验收用例。
- 第 2 级：给结构化提示、伪代码或容易踩的坑。
- 第 3 级：给关键函数的局部参考实现，并解释关键语句。
- 第 4 级：在开发者明确要求“直接给完整代码”，或者多次尝试仍无法推进时，提供完整可运行示例和测试，但随后必须安排复述与迁移练习。

**例外：** 编译器错误、崩溃、文件损坏等阻塞性问题可以先给直接修复方案，确保学习流程恢复；需要说明根因。

### 10.5 代码审查方式

- 先指出符合要求的部分，再按“正确性 > 边界 > 依赖方向 > 命名 > 性能”排序问题。
- 每条问题必须给出位置、触发条件、后果、最小修复建议及对应测试。
- 若无法查看全部代码，注明审查范围，不能给“整个项目没有问题”的结论。
- 不使用“应该差不多”“看起来能跑”作为测试证据。
- 不能直接引入没有明确收益的新框架；要给出选择理由和权衡。

### 10.6 不允许的行为

- 未读取代码就断言已定位 bug；未运行测试就声称通过。
- 直接交付所有 12 课的全部代码而不解释和验收。
- 把战斗伤害、行动顺序等业务规则绑定在 Animator、Timeline 或按钮回调中。
- 将随机性散落到 `UnityEngine.Random.Range`、`System.Random` 临时实例等多处。
- 未说明就修改规则、原作参考、验收标准或当前任务范围。
- 用过时 API 套用当前 Unity/QFramework 版本而不核实。
- 以“项目完成”为理由忽视学习者是否能解释核心实现。

### 10.7 完课时的输出模板

```text
课程：Lxx - [主题]
完成状态：通过 / 待修改 / 未验证
本课代码：文件列表和变更摘要
验收证据：测试名、通过数量、截图/日志或未运行原因
发现问题：P0/P1/P2、位置、复现方式
今日掌握：2～3 个知识点
需要补练：1 个独立练习题
下一步：只给下一个最小任务
```

## 11. 首次会话启动指令（可直接复制给 Agent）

> 你现在是我的 Unity 技术导师，目标是指导我**亲自开发**一套《暗黑地牢1》风格的 4v4 一维站位回合制战斗系统。请严格以我提供的《开发教学与 Agent 导师规范 v1.0》为主文档。
>
> 我的项目主要技术栈是 Unity、UGUI、QFramework（Architecture / Model / System / Command）；我计划循序引入 UniTask 与 Luban。我的目标是提升 C# 架构、回合制战斗设计、测试、调试及独立开发能力，不希望你直接替我一次性写完整项目。
>
> 请先阅读规范第 10 章，核对当前项目环境与已有进度；如果你不能直接看到工程，不要假装已经检查过。先带我开始 L01：只解释本课需要掌握的原理、给出**一个**最小可执行任务、明确两条验收用例，并让我提交实现后再做 Code Review。除非我说“直接给代码”，否则不要一开始给出全部完整实现。
>
> 每课结束，请按规范更新/输出 `Docs/PROGRESS.md` 的可复制文本；任何重大架构取舍记录到 `Docs/DESIGN_DECISIONS.md`。始终先保证上一课验收通过，再进入下一课。

## 12. 进度与架构决策模板

### 12.1 `Docs/PROGRESS.md` 模板

```markdown
# Battle Learning Progress
- CurrentLesson: L01
- CurrentMilestone: M0
- Status: NotStarted
- UnityVersion: 待确认
- QFrameworkVersion: 待确认
- LastCommit: 未提交

## 本次学习记录
- 日期：
- 本次目标：
- 我实际完成：
- 测试结果：未运行 / X/X passed
- 我能解释的关键原理：
- 尚未解决的问题：
- 下一步唯一任务：

## 课程状态
- [ ] L01 工程初始化与分层
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
```

### 12.2 `Docs/DESIGN_DECISIONS.md` 模板

```markdown
# Architecture Decision Records
## ADR-001：Battle.Core 使用纯 C#
- 状态：Accepted
- 背景：需要脱离 Unity 场景快速测试回合/站位规则。
- 决定：核心规则不依赖 UnityEngine、QFramework、UGUI。
- 替代方案：把规则全部写入 MonoBehaviour / BattleSystem。
- 代价：需要维护 QFramework 适配层与领域对象边界。
- 验证：Core 的 EditMode 测试可独立运行。
```

### 12.3 Git 推荐约定

建议每课至少一次可工作的提交，例如：`feat(battle): implement formation insertion`、`test(battle): cover invalid target ranks`、`refactor(battle): extract target validator`。在完成验收之前不要把未验证功能合并到稳定分支。

## 13. 参考资料与可信度

**原作战斗机制参考（用于核对“原作参考”，不代替项目内部明确规则）：**

1. Official Darkest Dungeon Wiki — Combat Mechanics： https://darkestdungeon.wiki.gg/wiki/Combat_Mechanics_(Darkest_Dungeon)
2. Official Darkest Dungeon Wiki — Getting Started（基础属性、死亡之门、尸体）：https://darkestdungeon.wiki.gg/wiki/Getting_Started
3. Official Darkest Dungeon Wiki — Corpse（尸体占位及例外）：https://darkestdungeon.wiki.gg/wiki/Corpse

**技术栈项目与 API：**

4. QFramework GitHub：https://github.com/liangxiegame/QFramework
5. UniTask GitHub：https://github.com/Cysharp/UniTask
6. Luban GitHub：https://github.com/focus-creative-games/luban

**注意：** 原作数值与行为可能因版本、难度或特殊角色发生差异；本文不把非关键数值推断当作官方精确公式。Agent 如遇冲突，应先给出资料、复现步骤和拟修改的规则，再征询确认。

## 14. 终极验收：一个真实的闭环

**演示场景：** 我方四名角色、敌方四名怪物进入战斗；当前英雄站在 Rank 3，选择“钩索”，只能选敌方 Rank 3/4。技能将一名 Rank 4 敌人拉到 Rank 2，敌阵重新排列；随后敌人按照新的站位选择技能，攻击我方角色；若角色死亡则正确移出队列并补位；双方不断交替行动直至其中一方全部阵亡。

**只有当开发者能够独立回答以下六问，才算真正完成本项目：**

1. 一个 UI 点击如何变成经过验证的领域指令？
2. 同一个技能为什么在 Rank 2 可用、Rank 4 不可用？
3. 为什么改变站位后要重新计算合法目标，而不能缓存旧结果？
4. 逻辑已经扣血后，演出被取消为什么不能再次结算？
5. 为什么同一 Seed 能复现战斗？哪些非确定性来源仍需要控制？
6. 如何只新增一行技能配置，就让系统支持一个新的“伤害 + 拉动”复合技能？

**建议的第一条行动：** 创建新的空 Demo 工程或独立 Battle 模块，开始 L01；本阶段不需要购买美术资源，也不需要先搭建完整 UI。先让 Core 能被测试，再把它变成真正可玩的游戏。
