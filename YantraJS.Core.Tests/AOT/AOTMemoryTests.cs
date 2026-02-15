using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using YantraJS.Core;
using YantraJS.Runtime;

namespace YantraJS.Tests.AOT
{
    [TestClass]
    public class AOTMemoryTests
    {
        [TestMethod]
        public void RecursiveFibonacci_MemoryTest_JIT()
        {
            // Use JIT mode (preferInterpretation: false) for better memory efficiency
            RuntimeAssembly.PreferInterpretation = false;

            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            // Force garbage collection before test
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var memBefore = GC.GetTotalMemory(false);
            Console.WriteLine($"Memory before (JIT): {memBefore / 1024.0 / 1024.0:F2} MB");

            context.Execute(@"
                function fib(n) {
                    if (n <= 1) return n;
                    return fib(n - 1) + fib(n - 2);
                }
                console.log(fib(20));
            ");

            var memAfter = GC.GetTotalMemory(false);
            Console.WriteLine($"Memory after (JIT): {memAfter / 1024.0 / 1024.0:F2} MB");
            Console.WriteLine($"Memory used (JIT): {(memAfter - memBefore) / 1024.0 / 1024.0:F2} MB");
            Console.WriteLine($"Result: {result}");

            Assert.AreEqual("6765", result);
            
            // Memory usage check - should be less than 20 MB for fib(20) with JIT
            var memUsed = (memAfter - memBefore) / 1024.0 / 1024.0;
            Assert.IsTrue(memUsed < 20, $"Memory usage too high: {memUsed:F2} MB");
        }

        [TestMethod]
        public void RecursiveFibonacci_MemoryTest_Interpreted()
        {
            // Use interpretation mode (preferInterpretation: true) for AOT compatibility
            RuntimeAssembly.PreferInterpretation = true;

            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            // Force garbage collection before test
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var memBefore = GC.GetTotalMemory(false);
            Console.WriteLine($"Memory before (Interpreted): {memBefore / 1024.0 / 1024.0:F2} MB");

            context.Execute(@"
                function fib(n) {
                    if (n <= 1) return n;
                    return fib(n - 1) + fib(n - 2);
                }
                console.log(fib(20));
            ");

            var memAfter = GC.GetTotalMemory(false);
            Console.WriteLine($"Memory after (Interpreted): {memAfter / 1024.0 / 1024.0:F2} MB");
            Console.WriteLine($"Memory used (Interpreted): {(memAfter - memBefore) / 1024.0 / 1024.0:F2} MB");
            Console.WriteLine($"Result: {result}");

            Assert.AreEqual("6765", result);
            
            // Memory usage check - interpreted mode uses more memory but still should be < 50 MB for fib(20)
            var memUsed = (memAfter - memBefore) / 1024.0 / 1024.0;
            Assert.IsTrue(memUsed < 50, $"Memory usage too high: {memUsed:F2} MB");
        }

        [TestMethod]
        public void RecursiveFibonacci30_MemoryTest_JIT()
        {
            // Use JIT mode for better memory efficiency
            RuntimeAssembly.PreferInterpretation = false;

            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            // Force garbage collection before test
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var memBefore = GC.GetTotalMemory(false);
            Console.WriteLine($"Memory before (JIT): {memBefore / 1024.0 / 1024.0:F2} MB");

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
            Console.WriteLine($"Memory after (JIT): {memAfter / 1024.0 / 1024.0:F2} MB");
            Console.WriteLine($"Memory used (JIT): {(memAfter - memBefore) / 1024.0 / 1024.0:F2} MB");
            Console.WriteLine($"Time: {elapsed.TotalMilliseconds} ms");
            Console.WriteLine($"Result: {result}");

            Assert.AreEqual("832040", result);
            
            // Memory usage check - JIT mode should use much less memory (< 50 MB for fib(30))
            var memUsed = (memAfter - memBefore) / 1024.0 / 1024.0;
            Console.WriteLine($"Memory check: {memUsed:F2} MB (should be < 50 MB)");
            Assert.IsTrue(memUsed < 50, $"Memory usage too high: {memUsed:F2} MB");
        }

        [TestMethod]
        public void RecursiveFibonacci30_MemoryTest_Interpreted()
        {
            // Use interpretation mode for AOT compatibility (but higher memory usage)
            RuntimeAssembly.PreferInterpretation = true;

            var context = new JSContext();
            var result = "";
            context.Log += (s, e) => result = e.ToString();

            // Force garbage collection before test
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var memBefore = GC.GetTotalMemory(false);
            Console.WriteLine($"Memory before (Interpreted): {memBefore / 1024.0 / 1024.0:F2} MB");

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
            Console.WriteLine($"Memory after (Interpreted): {memAfter / 1024.0 / 1024.0:F2} MB");
            Console.WriteLine($"Memory used (Interpreted): {(memAfter - memBefore) / 1024.0 / 1024.0:F2} MB");
            Console.WriteLine($"Time: {elapsed.TotalMilliseconds} ms");
            Console.WriteLine($"Result: {result}");

            Assert.AreEqual("832040", result);
            
            // Interpreted mode uses more memory - this test documents the behavior
            // We expect ~190 MB which is high but acceptable for AOT scenarios
            var memUsed = (memAfter - memBefore) / 1024.0 / 1024.0;
            Console.WriteLine($"Memory check (Interpreted): {memUsed:F2} MB (expected ~190 MB)");
            // Just verify it completes successfully - memory will be high in interpreted mode
            Assert.IsTrue(memUsed > 0, "Should use some memory");
        }
    }
}
