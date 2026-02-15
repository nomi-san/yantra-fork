using Microsoft.VisualStudio.TestTools.UnitTesting;
using YantraJS.Core;

namespace YantraJS.Tests.AOT
{
    [TestClass]
    public class AOTLogicTests
    {
        [TestMethod]
        public void ComplexBooleanAnd_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var a = 5;
                var b = 10;
                var c = 15;
                if (a < b && b < c && a + b === c) {
                    console.log('true');
                } else {
                    console.log('false');
                }
            ");

            Assert.AreEqual("true", result);
        }

        [TestMethod]
        public void ComplexBooleanOr_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var x = 3;
                if (x < 0 || x > 10 || x === 3) {
                    console.log('passed');
                } else {
                    console.log('failed');
                }
            ");

            Assert.AreEqual("passed", result);
        }

        [TestMethod]
        public void ShortCircuitAndEvaluation_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var called = false;
                function sideEffect() {
                    called = true;
                    return true;
                }
                var result = false && sideEffect();
                console.log(called);
            ");

            Assert.AreEqual("false", result);
        }

        [TestMethod]
        public void ShortCircuitOrEvaluation_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var called = false;
                function sideEffect() {
                    called = true;
                    return false;
                }
                var result = true || sideEffect();
                console.log(called);
            ");

            Assert.AreEqual("false", result);
        }

        [TestMethod]
        public void TernaryOperator_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var age = 18;
                var status = age >= 18 ? 'adult' : 'minor';
                console.log(status);
            ");

            Assert.AreEqual("adult", result);
        }

        [TestMethod]
        public void NestedTernaryOperator_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var score = 75;
                var grade = score >= 90 ? 'A' : 
                           score >= 80 ? 'B' : 
                           score >= 70 ? 'C' : 'F';
                console.log(grade);
            ");

            Assert.AreEqual("C", result);
        }

        [TestMethod]
        public void SwitchStatement_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var day = 3;
                var name;
                switch(day) {
                    case 1: name = 'Monday'; break;
                    case 2: name = 'Tuesday'; break;
                    case 3: name = 'Wednesday'; break;
                    case 4: name = 'Thursday'; break;
                    case 5: name = 'Friday'; break;
                    default: name = 'Weekend';
                }
                console.log(name);
            ");

            Assert.AreEqual("Wednesday", result);
        }

        [TestMethod]
        public void SwitchWithFallthrough_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var value = 2;
                var output = '';
                switch(value) {
                    case 1:
                    case 2:
                    case 3:
                        output = 'low';
                        break;
                    case 4:
                    case 5:
                        output = 'high';
                        break;
                    default:
                        output = 'unknown';
                }
                console.log(output);
            ");

            Assert.AreEqual("low", result);
        }

        [TestMethod]
        public void ComplexLogicalExpression_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                function isValidRange(x, min, max) {
                    return x >= min && x <= max && x !== 0;
                }
                console.log(isValidRange(5, 1, 10));
            ");

            Assert.AreEqual("true", result);
        }

        [TestMethod]
        public void NotOperator_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var a = true;
                var b = false;
                console.log(!a && !b);
            ");

            Assert.AreEqual("false", result);
        }

        [TestMethod]
        public void DoubleNotOperator_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var x = 'hello';
                console.log(!!x);
            ");

            Assert.AreEqual("true", result);
        }

        [TestMethod]
        public void ComplexConditionalChain_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                function categorize(temp) {
                    if (temp < 0) {
                        return 'freezing';
                    } else if (temp >= 0 && temp < 10) {
                        return 'cold';
                    } else if (temp >= 10 && temp < 20) {
                        return 'cool';
                    } else if (temp >= 20 && temp < 30) {
                        return 'warm';
                    } else {
                        return 'hot';
                    }
                }
                console.log(categorize(25));
            ");

            Assert.AreEqual("warm", result);
        }

        [TestMethod]
        public void BitwiseOperators_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var a = 5;  // 101
                var b = 3;  // 011
                console.log(a & b); // 001 = 1
            ");

            Assert.AreEqual("1", result);
        }

        [TestMethod]
        public void NullishCoalescing_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var value = null;
                var defaultValue = 'default';
                console.log(value || defaultValue);
            ");

            Assert.AreEqual("default", result);
        }
    }
}
