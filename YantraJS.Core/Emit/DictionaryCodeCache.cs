#if !WEBATOMS
// using FastExpressionCompiler;
#endif
using YantraJS.Core;
using YantraJS.Core.Core.Storage;
using YantraJS.Runtime;

namespace YantraJS.Emit
{
    public class DictionaryCodeCache : ICodeCache
    {
        private static ConcurrentStringMap<JSFunctionDelegate> cache
            = ConcurrentStringMap<JSFunctionDelegate>.Create();

        public static ICodeCache Current = new DictionaryCodeCache();

        // Configuration option: use AOT compilation if true, Reflection.Emit if false
        public static bool UseAOTCompilation { get; set; } = false;

        public JSFunctionDelegate GetOrCreate(in JSCode code)
        {
            var compiler = code.Compiler;
            return cache.GetOrCreate(code.Key, (k) => {
                var  exp = compiler();
                // Use AOT compilation if enabled and available, otherwise fall back to Reflection.Emit
                if (UseAOTCompilation && RuntimeAssemblyAOT.IsAOTSupported())
                {
                    try
                    {
                        return exp.CompileAOTWithNestedLambdas();
                    }
                    catch
                    {
                        // Fall back to Reflection.Emit if AOT compilation fails
                        return exp.CompileWithNestedLambdas();
                    }
                }
                return exp.CompileWithNestedLambdas();
                // return exp.CompileDynamic();
            });
        }

    }
}
