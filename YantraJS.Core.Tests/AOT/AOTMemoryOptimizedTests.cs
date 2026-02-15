using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using YantraJS.Core;
using YantraJS.Runtime;

namespace YantraJS.Tests.AOT
{
    [TestClass]
    public class AOTMemoryOptimizedTests
    {
        [TestMethod]
        public void IterativeFibonacci_LowMemory()
        {
            // Iterative solution uses minimal memory
            RuntimeAssembly.PreferInterpretation = false;

            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var memBefore = GC.GetTotalMemory(false);

            context.Execute(@"
                function fib(n) {
                    if (n <= 1) return n;
                    let a = 0, b = 1;
                    for (let i = 2; i <= n; i++) {
                        let temp = a + b;
                        a = b;
                        b = temp;
                    }
                    return b;
                }
                console.log(fib(30));
            ");

            var memAfter = GC.GetTotalMemory(false);
            var memUsed = (memAfter - memBefore) / 1024.0 / 1024.0;
            
            Console.WriteLine($"Memory used (Iterative fib(30)): {memUsed:F2} MB");
            Assert.AreEqual("832040", result);
            
            // Iterative solution should use < 5 MB
            Assert.IsTrue(memUsed < 5, $"Memory usage: {memUsed:F2} MB (should be < 5 MB)");
        }

        [TestMethod]
        public void MemoizedFibonacci_LowMemory()
        {
            // Memoization significantly reduces memory usage
            RuntimeAssembly.PreferInterpretation = false;

            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var memBefore = GC.GetTotalMemory(false);

            var startTime = DateTime.Now;
            context.Execute(@"
                function fib(n, memo) {
                    memo = memo || {};
                    if (n <= 1) return n;
                    if (memo[n]) return memo[n];
                    memo[n] = fib(n - 1, memo) + fib(n - 2, memo);
                    return memo[n];
                }
                console.log(fib(30));
            ");
            var elapsed = DateTime.Now - startTime;

            var memAfter = GC.GetTotalMemory(false);
            var memUsed = (memAfter - memBefore) / 1024.0 / 1024.0;
            
            Console.WriteLine($"Memory used (Memoized fib(30)): {memUsed:F2} MB");
            Console.WriteLine($"Time: {elapsed.TotalMilliseconds} ms");
            Assert.AreEqual("832040", result);
            
            // Memoization should use much less memory than naive recursion
            Assert.IsTrue(memUsed < 20, $"Memory usage: {memUsed:F2} MB (should be < 20 MB)");
        }

        [TestMethod]
        public void TailRecursiveFactorial_ModerateMemory()
        {
            // Tail recursion (though not optimized by .NET)
            RuntimeAssembly.PreferInterpretation = false;

            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var memBefore = GC.GetTotalMemory(false);

            context.Execute(@"
                function factorial(n, acc) {
                    acc = acc || 1;
                    if (n <= 1) return acc;
                    return factorial(n - 1, n * acc);
                }
                console.log(factorial(20));
            ");

            var memAfter = GC.GetTotalMemory(false);
            var memUsed = (memAfter - memBefore) / 1024.0 / 1024.0;
            
            Console.WriteLine($"Memory used (Tail recursive factorial(20)): {memUsed:F2} MB");
            Assert.AreEqual("2432902008176640000", result);
            
            // Tail recursion with moderate depth should be reasonable
            Assert.IsTrue(memUsed < 10, $"Memory usage: {memUsed:F2} MB (should be < 10 MB)");
        }

        [TestMethod]
        public void CompareRecursiveStrategies()
        {
            RuntimeAssembly.PreferInterpretation = false;

            Console.WriteLine("\n=== Comparing Recursive Strategies for fib(25) ===\n");

            // 1. Naive Recursion
            {
                var context = new JSContext();
                var result = "";
                context.Log += (s, e) => result = e.ToString();

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                var memBefore = GC.GetTotalMemory(false);
                var startTime = DateTime.Now;

                context.Execute(@"
                    function fib(n) {
                        if (n <= 1) return n;
                        return fib(n - 1) + fib(n - 2);
                    }
                    console.log(fib(25));
                ");

                var elapsed = DateTime.Now - startTime;
                var memAfter = GC.GetTotalMemory(false);
                var memUsed = (memAfter - memBefore) / 1024.0 / 1024.0;
                
                Console.WriteLine($"1. Naive Recursion:");
                Console.WriteLine($"   Memory: {memUsed:F2} MB");
                Console.WriteLine($"   Time: {elapsed.TotalMilliseconds:F0} ms");
                Console.WriteLine($"   Result: {result}");
            }

            // 2. Memoization
            {
                var context = new JSContext();
                var result = "";
                context.Log += (s, e) => result = e.ToString();

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                var memBefore = GC.GetTotalMemory(false);
                var startTime = DateTime.Now;

                context.Execute(@"
                    function fib(n, memo) {
                        memo = memo || {};
                        if (n <= 1) return n;
                        if (memo[n]) return memo[n];
                        memo[n] = fib(n - 1, memo) + fib(n - 2, memo);
                        return memo[n];
                    }
                    console.log(fib(25));
                ");

                var elapsed = DateTime.Now - startTime;
                var memAfter = GC.GetTotalMemory(false);
                var memUsed = (memAfter - memBefore) / 1024.0 / 1024.0;
                
                Console.WriteLine($"\n2. Memoization:");
                Console.WriteLine($"   Memory: {memUsed:F2} MB");
                Console.WriteLine($"   Time: {elapsed.TotalMilliseconds:F0} ms");
                Console.WriteLine($"   Result: {result}");
            }

            // 3. Iterative
            {
                var context = new JSContext();
                var result = "";
                context.Log += (s, e) => result = e.ToString();

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                var memBefore = GC.GetTotalMemory(false);
                var startTime = DateTime.Now;

                context.Execute(@"
                    function fib(n) {
                        if (n <= 1) return n;
                        let a = 0, b = 1;
                        for (let i = 2; i <= n; i++) {
                            let temp = a + b;
                            a = b;
                            b = temp;
                        }
                        return b;
                    }
                    console.log(fib(25));
                ");

                var elapsed = DateTime.Now - startTime;
                var memAfter = GC.GetTotalMemory(false);
                var memUsed = (memAfter - memBefore) / 1024.0 / 1024.0;
                
                Console.WriteLine($"\n3. Iterative:");
                Console.WriteLine($"   Memory: {memUsed:F2} MB");
                Console.WriteLine($"   Time: {elapsed.TotalMilliseconds:F0} ms");
                Console.WriteLine($"   Result: {result}");
            }

            Console.WriteLine($"\n=== Conclusion: Iterative and Memoization are much more efficient! ===\n");
        }
    }
}
