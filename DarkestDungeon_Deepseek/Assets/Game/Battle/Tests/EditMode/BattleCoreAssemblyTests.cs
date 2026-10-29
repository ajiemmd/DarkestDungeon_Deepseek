using Battle.Core.Domain;
using NUnit.Framework;

namespace Battle.Tests.EditMode
{

    public class BattleCoreAssemblyTests
    {
        // A Test behaves as an ordinary method
        [Test]
        public void TestAssembly_CanResolveBattleCoreTypes()
        {
            // Use the Assert class to test conditions
            // 断言“测试程序集能解析到 Battle.Core 里的类型”——这才是本课要证明的东西
            Assert.AreEqual(4, BattleConstants.TeamSize);
        }

    }

}
