using System;
using System.Linq.Expressions;
using YantraJS.Expressions;
using YantraJS.SL;

namespace YantraJS.Runtime
{
    /// <summary>
    /// Compilation methods using Expression.Compile(preferInterpretation: true)
    /// Compatible with Native AOT deployment scenarios.
    /// </summary>
    public static class RuntimeAssembly
    {
        /// <summary>
        /// Compiles a YExpression to a delegate using LINQ Expression interpretation.
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
            
            // Compile with interpretation preference for AOT compatibility
            return ((Expression<T>)linqExpr).Compile(preferInterpretation: true);
        }

        /// <summary>
        /// Compiles a YLambdaExpression to a delegate using LINQ Expression interpretation.
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
            
            // Compile with interpretation preference for AOT compatibility
            return linqExpr.Compile(preferInterpretation: true);
        }

        /// <summary>
        /// Compiles a YExpression with nested lambdas using LINQ Expression interpretation.
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
            
            // Compile with interpretation for AOT compatibility
            return ((Expression<T>)linqExpr).Compile(preferInterpretation: true);
        }
    }
}
