using Microsoft.VisualStudio.TestTools.UnitTesting;
using YantraJS.Core;

namespace YantraJS.Tests.AOT
{
    [TestClass]
    public class AOTLoopTests
    {
        [TestMethod]
        public void SimpleForLoop_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var sum = 0;
                for (var i = 1; i <= 5; i++) {
                    sum += i;
                }
                console.log(sum);
            ");

            Assert.AreEqual("15", result);
        }

        [TestMethod]
        public void ForLoopWithBreak_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var i;
                for (i = 0; i < 10; i++) {
                    if (i === 5) break;
                }
                console.log(i);
            ");

            Assert.AreEqual("5", result);
        }

        [TestMethod]
        public void ForLoopWithContinue_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var sum = 0;
                for (var i = 1; i <= 10; i++) {
                    if (i % 2 === 0) continue;
                    sum += i;
                }
                console.log(sum);
            ");

            // Sum of odd numbers 1-10: 1+3+5+7+9 = 25
            Assert.AreEqual("25", result);
        }

        [TestMethod]
        public void WhileLoop_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var count = 0;
                var i = 1;
                while (i <= 5) {
                    count += i;
                    i++;
                }
                console.log(count);
            ");

            Assert.AreEqual("15", result);
        }

        [TestMethod]
        public void DoWhileLoop_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var count = 0;
                var i = 1;
                do {
                    count += i;
                    i++;
                } while (i <= 5);
                console.log(count);
            ");

            Assert.AreEqual("15", result);
        }

        [TestMethod]
        public void NestedForLoops_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var product = 1;
                for (var i = 1; i <= 3; i++) {
                    for (var j = 1; j <= 3; j++) {
                        product *= 2;
                    }
                }
                console.log(product);
            ");

            // 2^9 = 512
            Assert.AreEqual("512", result);
        }

        [TestMethod]
        public void ForLoopArrayIteration_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var arr = [1, 2, 3, 4, 5];
                var sum = 0;
                for (var i = 0; i < arr.length; i++) {
                    sum += arr[i];
                }
                console.log(sum);
            ");

            Assert.AreEqual("15", result);
        }

        [TestMethod]
        public void ForLoopWithComplexCondition_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var count = 0;
                for (var i = 0; i < 100 && count < 10; i++) {
                    if (i % 2 === 0) {
                        count++;
                    }
                }
                console.log(count);
            ");

            Assert.AreEqual("10", result);
        }

        [TestMethod]
        public void NestedLoopsWithBreak_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var found = false;
                var target = 15;
                outer: for (var i = 1; i <= 10; i++) {
                    for (var j = 1; j <= 10; j++) {
                        if (i * j === target) {
                            console.log(i + ',' + j);
                            found = true;
                            break outer;
                        }
                    }
                }
            ");

            Assert.AreEqual("3,5", result);
        }

        [TestMethod]
        public void WhileLoopWithComplexLogic_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var i = 1;
                var sum = 0;
                while (i <= 100) {
                    if (i % 3 === 0 && i % 5 === 0) {
                        sum += i;
                    }
                    i++;
                }
                console.log(sum);
            ");

            // Sum of numbers divisible by both 3 and 5 (i.e., 15, 30, 45, 60, 75, 90)
            Assert.AreEqual("315", result);
        }
    }
}
