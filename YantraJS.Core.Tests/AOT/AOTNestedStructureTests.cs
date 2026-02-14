using Microsoft.VisualStudio.TestTools.UnitTesting;
using YantraJS.Core;

namespace YantraJS.Tests.AOT
{
    [TestClass]
    public class AOTNestedStructureTests
    {
        [TestMethod]
        public void NestedFunction_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                function outer(x) {
                    function inner(y) {
                        return x + y;
                    }
                    return inner(5);
                }
                console.log(outer(10));
            ");

            Assert.AreEqual("15", result);
        }

        [TestMethod]
        public void Closure_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                function makeCounter() {
                    var count = 0;
                    return function() {
                        count++;
                        return count;
                    };
                }
                var counter = makeCounter();
                counter();
                counter();
                console.log(counter());
            ");

            Assert.AreEqual("3", result);
        }

        [TestMethod]
        public void MultiLevelClosure_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                function level1(a) {
                    return function level2(b) {
                        return function level3(c) {
                            return a + b + c;
                        };
                    };
                }
                console.log(level1(1)(2)(3));
            ");

            Assert.AreEqual("6", result);
        }

        [TestMethod]
        public void NestedObjectsAndArrays_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var data = {
                    users: [
                        { name: 'Alice', age: 30 },
                        { name: 'Bob', age: 25 }
                    ]
                };
                console.log(data.users[0].name);
            ");

            Assert.AreEqual("Alice", result);
        }

        [TestMethod]
        public void RecursiveFibonacci_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                function fib(n) {
                    if (n <= 1) return n;
                    return fib(n - 1) + fib(n - 2);
                }
                console.log(fib(10));
            ");

            Assert.AreEqual("55", result);
        }

        [TestMethod]
        public void RecursiveFactorial_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                function factorial(n) {
                    if (n <= 1) return 1;
                    return n * factorial(n - 1);
                }
                console.log(factorial(5));
            ");

            Assert.AreEqual("120", result);
        }

        [TestMethod]
        public void ArrayMap_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var arr = [1, 2, 3, 4, 5];
                var doubled = arr.map(function(x) { return x * 2; });
                console.log(doubled.join(','));
            ");

            Assert.AreEqual("2,4,6,8,10", result);
        }

        [TestMethod]
        public void ArrayFilter_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var arr = [1, 2, 3, 4, 5, 6];
                var evens = arr.filter(function(x) { return x % 2 === 0; });
                console.log(evens.join(','));
            ");

            Assert.AreEqual("2,4,6", result);
        }

        [TestMethod]
        public void ArrayReduce_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var arr = [1, 2, 3, 4, 5];
                var sum = arr.reduce(function(acc, val) { return acc + val; }, 0);
                console.log(sum);
            ");

            Assert.AreEqual("15", result);
        }

        [TestMethod]
        public void NestedArrayOperations_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var arr = [1, 2, 3, 4, 5];
                var result = arr
                    .filter(function(x) { return x > 2; })
                    .map(function(x) { return x * 2; })
                    .reduce(function(acc, val) { return acc + val; }, 0);
                console.log(result);
            ");

            // [3, 4, 5] -> [6, 8, 10] -> 24
            Assert.AreEqual("24", result);
        }

        [TestMethod]
        public void ImmediatelyInvokedFunctionExpression_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var result = (function(x, y) {
                    return x * y;
                })(5, 7);
                console.log(result);
            ");

            Assert.AreEqual("35", result);
        }

        [TestMethod]
        public void ClosureWithMultipleVariables_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                function createCalculator(initial) {
                    var value = initial;
                    return {
                        add: function(x) { value += x; return value; },
                        multiply: function(x) { value *= x; return value; },
                        getValue: function() { return value; }
                    };
                }
                var calc = createCalculator(10);
                calc.add(5);
                calc.multiply(2);
                console.log(calc.getValue());
            ");

            Assert.AreEqual("30", result);
        }

        [TestMethod]
        public void NestedRecursion_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                function ackermann(m, n) {
                    if (m === 0) return n + 1;
                    if (n === 0) return ackermann(m - 1, 1);
                    return ackermann(m - 1, ackermann(m, n - 1));
                }
                console.log(ackermann(2, 2));
            ");

            Assert.AreEqual("7", result);
        }

        [TestMethod]
        public void ComplexObjectNesting_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                var company = {
                    name: 'TechCorp',
                    departments: {
                        engineering: {
                            teams: {
                                backend: {
                                    members: 10,
                                    lead: 'Alice'
                                }
                            }
                        }
                    }
                };
                console.log(company.departments.engineering.teams.backend.lead);
            ");

            Assert.AreEqual("Alice", result);
        }

        [TestMethod]
        public void FunctionReturningFunction_AOT()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            context.Execute(@"
                function multiplier(factor) {
                    return function(x) {
                        return x * factor;
                    };
                }
                var double = multiplier(2);
                var triple = multiplier(3);
                console.log(double(5) + triple(5));
            ");

            Assert.AreEqual("25", result);
        }
    }
}
