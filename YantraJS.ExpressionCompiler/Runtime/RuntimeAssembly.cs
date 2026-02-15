using System;
using System.Linq.Expressions;
using YantraJS.Expressions;
using YantraJS.SL;

namespace YantraJS.Runtime
{
    /// <summary>
    /// Compilation methods using Expression.Compile()
    /// Supports both JIT (for performance) and interpretation (for AOT).
    /// </summary>
    public static class RuntimeAssembly
    {
        /// <summary>
        /// When true, uses interpretation mode for AOT compatibility but higher memory usage.
        /// When false, uses JIT compilation for better performance and memory efficiency.
        /// Default is false (JIT) for better memory efficiency.
        /// </summary>
        public static bool PreferInterpretation { get; set; } = false;

        /// <summary>
        /// Compiles a YExpression to a delegate.
        /// </summary>
        /// <typeparam name="T">The delegate type</typeparam>
        /// <param name="exp">The YExpression to compile</param>
        /// <returns>Compiled delegate</returns>
        public static T Compile<T>(this YExpression<T> exp)
        {
            // Convert YExpression to LINQ Expression
            var converter = new LambdaConverter();
            var linqExpr = converter.Visit(exp) as LambdaExpression;
            
            if (linqExpr == null)
            {
                throw new InvalidOperationException("Failed to convert YExpression to LINQ Expression");
            }
            
            // Compile with user-specified preference
            // preferInterpretation: false = JIT compilation (better performance, less memory)
            // preferInterpretation: true = Interpretation (AOT compatible, more memory)
            return ((Expression<T>)linqExpr).Compile(preferInterpretation: PreferInterpretation);
        }

        /// <summary>
        /// Compiles a YLambdaExpression to a delegate.
        /// </summary>
        /// <param name="exp">The YLambdaExpression to compile</param>
        /// <returns>Compiled delegate</returns>
        public static object Compile(this YLambdaExpression exp)
        {
            // Convert YExpression to LINQ Expression
            var converter = new LambdaConverter();
            var linqExpr = converter.Visit(exp) as LambdaExpression;
            
            if (linqExpr == null)
            {
                throw new InvalidOperationException("Failed to convert YExpression to LINQ Expression");
            }
            
            // Compile with user-specified preference
            return linqExpr.Compile(preferInterpretation: PreferInterpretation);
        }

        /// <summary>
        /// Compiles a YExpression with nested lambdas.
        /// This method handles nested lambda expressions by recursively converting and compiling them.
        /// </summary>
        /// <typeparam name="T">The delegate type</typeparam>
        /// <param name="exp">The YExpression to compile</param>
        /// <returns>Compiled delegate result</returns>
        public static T CompileWithNestedLambdas<T>(this YExpression<T> exp)
        {
            // For nested lambdas, we need to ensure all inner lambdas are also converted
            // The LambdaConverter.VisitLambda method handles this recursively
            var converter = new LambdaConverter();
            var linqExpr = converter.Visit(exp) as LambdaExpression;
            
            if (linqExpr == null)
            {
                throw new InvalidOperationException("Failed to convert YExpression to LINQ Expression");
            }
            
            // Compile with user-specified preference
            return ((Expression<T>)linqExpr).Compile(preferInterpretation: PreferInterpretation);
        }
    }
}
