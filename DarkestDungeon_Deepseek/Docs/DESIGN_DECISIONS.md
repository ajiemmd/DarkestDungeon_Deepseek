# Architecture Decision Records

## ADR-001：Battle.Core 使用纯 C#
- 状态：Accepted（2026-10-29）
- 背景：需要脱离 Unity 场景快速测试回合/站位规则；后续要求「同 Seed + 同初始状态 → 完全相同战斗日志」（T11），这也要求核心规则不依赖引擎的随机数与时间。
- 决定：Battle.Core（Assets/Game/Battle/Core/**）只依赖 .NET 基础类库；不引用 UnityEngine、UnityEditor、QFramework、UGUI。asmdef 依赖列表为空且勾选 No Engine References。
- 替代方案：把规则全部写进 MonoBehaviour / BattleSystem，用场景测试验证。
- 代价：必须额外维护 Framework 适配层（L08）与 Presentation 层（L09）；随机数必须来自注入的 IRandomSource；日志必须走 Console / TestContext，不能用 Debug.Log。
- 验证：Battle.Core.csproj 中 UnityEngine 引用数 = 0；在 Core 内写 `using UnityEngine;` 会得到 CS0246；EditMode 测试可脱离场景独立运行。

## ADR-002：仓库根 = Unity 工程根
- 状态：Accepted（2026-10-29；执行待关闭 Unity）
- 背景：Unity Hub 以「Location=仓库工作区、Name=同名」建工程，导致工程落在 <repo>/DarkestDungeon_Deepseek/，仓库根与工程根错开一层；而规范 §5 要求 Docs/ 与 Assets/ 同级。
- 决定：把 <repo>/DarkestDungeon_Deepseek/ 下的全部内容上移一层到 <repo>/，使仓库根即 Unity 工程根，Assets/ 与 Docs/ 同级。
- 替代方案：保持嵌套（每次命令都要多一层路径）；或对子目录建 junction/符号链接（Windows 下跨工具易出问题）。
- 代价：移动前必须完全退出 Unity（Library 文件句柄被占用）；路径变化后需在 Unity Hub 用 Add project from disk 重新添加工程。
- 验证：git ls-files 中路径不以 DarkestDungeon_Deepseek/ 开头；重新打开工程后 EditMode 测试仍全绿。

## ADR-003：Formation 用有序列表 + 插入式位移
- 状态：Proposed（待 L02 实现与复盘后确认）
- 背景：规范 T02 要求「把 Rank 4 单位插入 Rank 2」后顺序变为 [1,4,2,3]（其余单位顺移，而非交换）；同时要求「不会产生两个单位同占一格」。
- 决定（候选）：Formation 内部用按 rank 排序的 List<BattleUnit>，rank = index + 1（1 基，基准来自 BattleConstants.FirstRank）；对外只暴露 IReadOnlyList 防止外部打乱顺序；MoveUnitToRank 采用「取出—插入」语义；RemoveUnit 后自动前移补齐，rank 恒为连续 1..N。
- 替代方案：Dictionary<int, BattleUnit>（键值双真相，易出现两单位同格）；swap 语义（结果为 [1,4,3,2]，与 T02 不符）。
- 代价：插入/删除是 O(n)（4 人规模可忽略）；L11 尸体占位会打破「rank 连续」假设，届时需改为显式空位模型。
- 验证：T01（初始阵型无冲突）、T02（[1,4,2,3]）、T03（无效 rank 不破坏阵型）三条用例。
