using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using YantraJS.Expressions;
using YantraJS.Runtime;

namespace YantraJS.AOT
{
    [TestClass]
    public class AOTConditionalBugTests
    {
        [TestMethod]
        public void SimpleConditional_AOT()
        {
            // Test simple conditional: if (a > b) then a else b
            // This is the key test case for the bug
            var a = YExpression.Parameter(typeof(int), "a");
            var b = YExpression.Parameter(typeof(int), "b");

            var exp = YExpression.Lambda<Func<int, int, int>>("max",
                YExpression.Conditional(
                    a > b,
                    a,
                    b),
                new YParameterExpression[] { a, b });

            var fx = exp.Compile();

            Assert.AreEqual(5, fx(5, 3));
            Assert.AreEqual(8, fx(2, 8));
        }
        
        [TestMethod]
        public void ConditionalWithNullFalse_AOT()
        {
            // Test conditional with null false branch
            var a = YExpression.Parameter(typeof(int), "a");
            var b = YExpression.Parameter(typeof(int), "b");

            // Create a conditional where false might be null
            var test = a > b;
            var trueExpr = a;
            var falseExpr = b;
            
            var conditional = YExpression.Conditional(test, trueExpr, falseExpr);

            var exp = YExpression.Lambda<Func<int, int, int>>("test",
                conditional,
                new YParameterExpression[] { a, b });

            var fx = exp.Compile();

            Assert.AreEqual(10, fx(10, 5));
            Assert.AreEqual(3, fx(2, 3));
        }
    }
}
