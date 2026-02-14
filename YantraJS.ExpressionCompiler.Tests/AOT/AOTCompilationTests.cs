using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using YantraJS.Expressions;
using YantraJS.Runtime;

namespace YantraJS.AOT
{
    [TestClass]
    public class AOTCompilationTests
    {
        [TestMethod]
        public void SimpleAddition_AOT()
        {
            var a = YExpression.Parameter(typeof(int), "a");
            var b = YExpression.Parameter(typeof(int), "b");

            var exp = YExpression.Lambda<Func<int, int, int>>("add",
                YExpression.Binary(a, YOperator.Add, b),
                new YParameterExpression[] { a, b });

            var fx = exp.CompileAOT();

            Assert.AreEqual(3, fx(1, 2));
            Assert.AreEqual(10, fx(5, 5));
            Assert.AreEqual(0, fx(-5, 5));
        }

        [TestMethod]
        public void SimpleConstant_AOT()
        {
            var exp = YExpression.Lambda<Func<int>>("constant",
                YExpression.Constant(42),
                new YParameterExpression[] { });

            var fx = exp.CompileAOT();

            Assert.AreEqual(42, fx());
        }

        [TestMethod]
        public void Conditional_AOT()
        {
            var a = YExpression.Parameter(typeof(int), "a");
            var b = YExpression.Parameter(typeof(int), "b");

            var exp = YExpression.Lambda<Func<int, int, int>>("max",
                YExpression.Conditional(
                    a > b,
                    a,
                    b),
                new YParameterExpression[] { a, b });

            var fx = exp.CompileAOT();

            Assert.AreEqual(5, fx(5, 3));
            Assert.AreEqual(8, fx(2, 8));
            Assert.AreEqual(10, fx(10, 10));
        }

        [TestMethod]
        public void CheckAOTAvailability()
        {
            Assert.IsTrue(RuntimeAssemblyAOT.IsAOTSupported());
        }
    }
}
