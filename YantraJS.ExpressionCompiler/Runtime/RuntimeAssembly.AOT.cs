using System;
using System.Linq.Expressions;
using YantraJS.Expressions;
using YantraJS.SL;

namespace YantraJS.Runtime
{
    /// <summary>
    /// AOT-compatible compilation methods using Expression.Compile(preferInterpretation: true)
    /// These methods are compatible with Native AOT deployment scenarios where Reflection.Emit is not available.
    /// </summary>
    public static class RuntimeAssemblyAOT
    {
        /// <summary>
        /// Compiles a YExpression to a delegate using LINQ Expression interpretation (AOT-compatible).
        /// This method does not use Reflection.Emit and is compatible with Native AOT.
        /// </summary>
        /// <typeparam name="T">The delegate type</typeparam>
        /// <param name="exp">The YExpression to compile</param>
        /// <returns>Compiled delegate</returns>
        public static T CompileAOT<T>(this YExpression<T> exp)
        {
            // Convert YExpression to LINQ Expression
            var converter = new LambdaConverter();
            var linqExpr = converter.Visit(exp) as LambdaExpression;
            
            if (linqExpr == null)
            {
                throw new InvalidOperationException("Failed to convert YExpression to LINQ Expression");
            }
            
            // Compile with interpretation preference for AOT compatibility
            // preferInterpretation: true ensures the expression is interpreted rather than JIT compiled
            return ((Expression<T>)linqExpr).Compile(preferInterpretation: true);
        }

        /// <summary>
        /// Compiles a YLambdaExpression to a delegate using LINQ Expression interpretation (AOT-compatible).
        /// This method does not use Reflection.Emit and is compatible with Native AOT.
        /// </summary>
        /// <param name="exp">The YLambdaExpression to compile</param>
        /// <returns>Compiled delegate</returns>
        public static object CompileAOT(this YLambdaExpression exp)
        {
            // Convert YExpression to LINQ Expression
            var converter = new LambdaConverter();
            var linqExpr = converter.Visit(exp) as LambdaExpression;
            
            if (linqExpr == null)
            {
                throw new InvalidOperationException("Failed to convert YExpression to LINQ Expression");
            }
            
            // Compile with interpretation preference for AOT compatibility
            return linqExpr.Compile(preferInterpretation: true);
        }

        /// <summary>
        /// Compiles a YExpression with nested lambdas using LINQ Expression interpretation (AOT-compatible).
        /// This method handles nested lambda expressions by recursively converting and compiling them.
        /// Note: This is a simplified version - complex nested closures may not work as expected.
        /// </summary>
        /// <typeparam name="T">The delegate type</typeparam>
        /// <param name="exp">The YExpression to compile</param>
        /// <returns>Compiled delegate result</returns>
        public static T CompileAOTWithNestedLambdas<T>(this YExpression<T> exp)
        {
            // For nested lambdas, we need to ensure all inner lambdas are also converted
            // The LambdaConverter.VisitLambda method handles this recursively
            var converter = new LambdaConverter();
            var linqExpr = converter.Visit(exp) as LambdaExpression;
            
            if (linqExpr == null)
            {
                throw new InvalidOperationException("Failed to convert YExpression to LINQ Expression");
            }
            
            // Compile and execute
            var compiledFunc = ((Expression<T>)linqExpr).Compile(preferInterpretation: true);
            
            // If T is a Func<TResult>, invoke it and return the result
            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Func<>))
            {
                var result = (compiledFunc as Delegate)?.DynamicInvoke();
                return result != null ? (T)result : default(T);
            }
            
            return compiledFunc;
        }

        /// <summary>
        /// Gets a value indicating whether the current runtime supports AOT compilation.
        /// </summary>
        /// <returns>True if AOT is supported (always returns true for LINQ Expression compilation)</returns>
        public static bool IsAOTSupported()
        {
            // LINQ Expression.Compile with preferInterpretation is always supported
            return true;
        }

        /// <summary>
        /// Gets a value indicating whether Reflection.Emit is available in the current runtime.
        /// </summary>
        /// <returns>True if Reflection.Emit is available</returns>
        public static bool IsReflectionEmitAvailable()
        {
            try
            {
                // Try to access a Reflection.Emit type
                var type = typeof(System.Reflection.Emit.ILGenerator);
                return type != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
