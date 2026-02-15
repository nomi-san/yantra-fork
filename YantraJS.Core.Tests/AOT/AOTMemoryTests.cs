using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using YantraJS.Core;

namespace YantraJS.Tests.AOT
{
    [TestClass]
    public class AOTMemoryTests
    {
        [TestMethod]
        public void RecursiveFibonacci_MemoryTest()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            // Force garbage collection before test
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var memBefore = GC.GetTotalMemory(false);
            Console.WriteLine($"Memory before: {memBefore / 1024.0 / 1024.0:F2} MB");

            context.Execute(@"
                function fib(n) {
                    if (n <= 1) return n;
                    return fib(n - 1) + fib(n - 2);
                }
                console.log(fib(20));
            ");

            var memAfter = GC.GetTotalMemory(false);
            Console.WriteLine($"Memory after: {memAfter / 1024.0 / 1024.0:F2} MB");
            Console.WriteLine($"Memory used: {(memAfter - memBefore) / 1024.0 / 1024.0:F2} MB");
            Console.WriteLine($"Result: {result}");

            Assert.AreEqual("6765", result);
            
            // Memory usage check - should be less than 50 MB for fib(20)
            var memUsed = (memAfter - memBefore) / 1024.0 / 1024.0;
            Assert.IsTrue(memUsed < 50, $"Memory usage too high: {memUsed:F2} MB");
        }

        [TestMethod]
        public void RecursiveFibonacci30_MemoryTest()
        {
            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            // Force garbage collection before test
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var memBefore = GC.GetTotalMemory(false);
            Console.WriteLine($"Memory before: {memBefore / 1024.0 / 1024.0:F2} MB");

            var startTime = DateTime.Now;
            context.Execute(@"
                function fib(n) {
                    if (n <= 1) return n;
                    return fib(n - 1) + fib(n - 2);
                }
                console.log(fib(30));
            ");
            var elapsed = DateTime.Now - startTime;

            var memAfter = GC.GetTotalMemory(false);
            Console.WriteLine($"Memory after: {memAfter / 1024.0 / 1024.0:F2} MB");
            Console.WriteLine($"Memory used: {(memAfter - memBefore) / 1024.0 / 1024.0:F2} MB");
            Console.WriteLine($"Time: {elapsed.TotalMilliseconds} ms");
            Console.WriteLine($"Result: {result}");

            Assert.AreEqual("832040", result);
            
            // Memory usage check - should be less than 100 MB for fib(30)
            var memUsed = (memAfter - memBefore) / 1024.0 / 1024.0;
            Console.WriteLine($"Memory check: {memUsed:F2} MB (should be < 100 MB)");
            Assert.IsTrue(memUsed < 100, $"Memory usage too high: {memUsed:F2} MB");
        }
    }
}
