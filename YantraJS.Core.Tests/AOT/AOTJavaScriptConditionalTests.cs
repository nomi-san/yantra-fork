using Microsoft.VisualStudio.TestTools.UnitTesting;
using YantraJS.Core;

namespace YantraJS.Tests.AOT
{
    [TestClass]
    public class AOTJavaScriptConditionalTests
    {
        [TestMethod]
        public void FibonacciWithConditional_AOT()
        {
            // This is the exact test case from the bug report
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                function fib(n) {
                    if (n >= 1) return n;
                    return 0;
                }
                console.log(fib(10));
            ");

            Assert.AreEqual("10", result);
        }

        [TestMethod]
        public void SimpleIfStatement_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var x = 5;
                if (x > 3) {
                    console.log('greater');
                } else {
                    console.log('less');
                }
            ");

            Assert.AreEqual("greater", result);
        }

        [TestMethod]
        public void NestedConditionals_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                function classify(n) {
                    if (n > 0) {
                        if (n > 10) {
                            return 'large';
                        } else {
                            return 'small';
                        }
                    } else {
                        return 'negative';
                    }
                }
                console.log(classify(15));
            ");

            Assert.AreEqual("large", result);
        }
    }
}
