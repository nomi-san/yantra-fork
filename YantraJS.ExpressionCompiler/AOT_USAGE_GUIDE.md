# AOT Compilation Support for YantraJS.ExpressionCompiler

## Quick Start

YantraJS.ExpressionCompiler now supports **two compilation modes**:

### 1. Reflection.Emit (Existing - Fastest)
```csharp
using YantraJS.Expressions;
using YantraJS.Runtime;

var a = YExpression.Parameter(typeof(int), "a");
var b = YExpression.Parameter(typeof(int), "b");

var exp = YExpression.Lambda<Func<int, int, int>>("add",
    YExpression.Binary(a, YOperator.Add, b),
    new[] { a, b });

// Fast JIT compilation (existing behavior)
var func = exp.Compile();
Console.WriteLine(func(5, 3)); // 8
```

### 2. AOT-Compatible (New - Native AOT Ready)
```csharp
using YantraJS.Expressions;
using YantraJS.Runtime;

var a = YExpression.Parameter(typeof(int), "a");
var b = YExpression.Parameter(typeof(int), "b");

var exp = YExpression.Lambda<Func<int, int, int>>("add",
    YExpression.Binary(a, YOperator.Add, b),
    new[] { a, b });

// AOT-compatible compilation (new feature)
var func = exp.CompileAOT();
Console.WriteLine(func(5, 3)); // 8
```

## When to Use AOT Compilation

Use `CompileAOT()` when:
- ✅ Deploying to **iOS** (JIT not allowed)
- ✅ Using **.NET Native AOT** (.NET 7+)
- ✅ Running in **sandboxed environments**
- ✅ Targeting **embedded systems**
- ✅ Security policies **prohibit dynamic code generation**

Use `Compile()` when:
- ⚡ Maximum performance is critical
- ⚡ Running in standard .NET environments
- ⚡ JIT compilation is available

## API Reference

### CompileAOT<T>()
```csharp
public static T CompileAOT<T>(this YExpression<T> exp)
```
Compiles a YExpression to a delegate using LINQ Expression interpretation (AOT-compatible).

**Example:**
```csharp
var exp = YExpression.Lambda<Func<int>>("constant",
    YExpression.Constant(42),
    new YParameterExpression[] { });

var func = exp.CompileAOT();
Console.WriteLine(func()); // 42
```

### CompileAOTWithNestedLambdas<T>()
```csharp
public static T CompileAOTWithNestedLambdas<T>(this YExpression<T> exp)
```
Compiles a YExpression with nested lambda support (AOT-compatible).

**Example:**
```csharp
// Expression with nested lambdas
var outer = YExpression.Lambda<Func<Func<int>>>("outer",
    YExpression.Lambda<Func<int>>("inner",
        YExpression.Constant(42),
        new YParameterExpression[] { }),
    new YParameterExpression[] { });

var func = outer.CompileAOTWithNestedLambdas();
```

### Runtime Detection
```csharp
// Check if AOT compilation is supported (always true)
bool aotSupported = RuntimeAssemblyAOT.IsAOTSupported();

// Check if Reflection.Emit is available
bool emitAvailable = RuntimeAssemblyAOT.IsReflectionEmitAvailable();

// Choose compilation strategy at runtime
var func = emitAvailable 
    ? exp.Compile()      // Fast path
    : exp.CompileAOT();  // AOT path
```

## Performance Comparison

| Compilation Mode | Relative Speed | Native AOT | iOS |
|------------------|---------------|------------|-----|
| Reflection.Emit | 1x (baseline) | ❌ No | ❌ No |
| AOT Interpretation | 2-5x slower | ✅ Yes | ✅ Yes |

**Note:** The 2-5x performance difference is acceptable for AOT scenarios and often negligible compared to I/O or business logic.

## Supported Features

### ✅ Fully Supported (40+ expression types)

**Constants:**
- Boolean, Byte, Int32, Int64, UInt32, UInt64
- Float, Double, String, Type, MethodInfo

**Core Expressions:**
- Parameters, New, Property, Field, Call
- Binary operations, Unary operations
- Array access, Array length, Array creation
- Type checking (TypeIs, TypeAs)
- Boxing/Unboxing

**Control Flow:**
- If/else conditionals
- Loops (while, for)
- Labels and Goto
- Return statements
- Try/Catch/Finally
- Switch statements

**Complex Expressions:**
- Lambda expressions (nested supported)
- Member initialization
- List initialization
- Delegate creation

### ⚠️ Limited Support

- **Yield expressions** - Require state machine transformation (not yet implemented)
- **AddressOf** - Simplified (ref behavior at parameter level)
- **IL Offset** - Debug information not preserved

## Migration Guide

### Updating Existing Code

No changes required! Your existing code using `Compile()` continues to work:

```csharp
// Existing code - no changes needed
var func = exp.Compile();
```

### Opting into AOT

Simply replace `Compile()` with `CompileAOT()`:

```csharp
// Before
var func = exp.Compile();

// After
var func = exp.CompileAOT();
```

### Conditional Compilation

Choose at runtime based on capabilities:

```csharp
#if RELEASE
    var func = exp.CompileAOT(); // AOT for production
#else
    var func = exp.Compile();    // Fast for development
#endif
```

## Examples

### Example 1: Simple Calculation
```csharp
var x = YExpression.Parameter(typeof(int), "x");
var y = YExpression.Parameter(typeof(int), "y");

var multiply = YExpression.Lambda<Func<int, int, int>>("multiply",
    YExpression.Binary(x, YOperator.Multipley, y),
    new[] { x, y });

var func = multiply.CompileAOT();
Console.WriteLine(func(6, 7)); // 42
```

### Example 2: Conditional Logic
```csharp
var age = YExpression.Parameter(typeof(int), "age");

var isAdult = YExpression.Lambda<Func<int, bool>>("isAdult",
    age >= YExpression.Constant(18),
    new[] { age });

var func = isAdult.CompileAOT();
Console.WriteLine(func(21)); // true
Console.WriteLine(func(16)); // false
```

### Example 3: Object Creation
```csharp
var ctor = typeof(Person).GetConstructor(new[] { typeof(string), typeof(int) });
var name = YExpression.Parameter(typeof(string), "name");
var age = YExpression.Parameter(typeof(int), "age");

var createPerson = YExpression.Lambda<Func<string, int, Person>>("createPerson",
    YExpression.New(ctor, new[] { name, age }.AsSequence()),
    new[] { name, age });

var func = createPerson.CompileAOT();
var person = func("Alice", 30);
```

### Example 4: Try/Catch Error Handling
```csharp
var divisor = YExpression.Parameter(typeof(int), "divisor");
var ex = YExpression.Parameter(typeof(DivideByZeroException), "ex");

var safeDivide = YExpression.Lambda<Func<int, int>>("safeDivide",
    YExpression.TryCatchFinally(
        YExpression.Binary(YExpression.Constant(100), YOperator.Divide, divisor),
        new YCatchBody(ex, YExpression.Constant(-1)),
        null),
    new[] { divisor });

var func = safeDivide.CompileAOT();
Console.WriteLine(func(2));  // 50
Console.WriteLine(func(0));  // -1 (caught exception)
```

## Troubleshooting

### "Expression tree may not contain a method call to a method that returns by reference"

**Problem:** Your expression uses ref returns which aren't fully supported in interpreted mode.

**Solution:** Simplify the expression or use `Compile()` for development.

### Performance is slower than expected

**Explanation:** AOT interpretation is 2-5x slower than JIT compilation. This is expected and acceptable for AOT scenarios.

**Solution:** 
- Use `Compile()` during development
- Use `CompileAOT()` only for production AOT deployments

### NotSupportedException for Yield

**Problem:** Yield expressions require state machine transformation.

**Solution:** Refactor to use regular return values, callbacks, or IEnumerable patterns.

## Technical Details

### How It Works

1. **YExpression AST** → Your custom expression tree
2. **LambdaConverter** → Visitor pattern converts to LINQ Expressions
3. **Expression.Compile()** → Compiles with `preferInterpretation: true`
4. **Interpreted Execution** → AOT-compatible, no JIT required

### Architecture

```
YExpression (Custom AST)
    ↓
LambdaConverter.Visit()
    ↓
System.Linq.Expressions.Expression
    ↓
Expression.Compile(preferInterpretation: true)
    ↓
Interpreted Delegate (AOT-Compatible)
```

## Contributing

Found a bug or want to add support for more expression types? Contributions welcome!

See:
- `YantraJS.ExpressionCompiler/SL/LambdaConverter.cs` - Expression converters
- `YantraJS.ExpressionCompiler/Runtime/RuntimeAssembly.AOT.cs` - AOT entry points
- `YantraJS.ExpressionCompiler.Tests/AOT/` - Test suite

## References

- [AOT_REFACTORING_ASSESSMENT.md](../AOT_REFACTORING_ASSESSMENT.md) - Technical assessment
- [AOT_IMPLEMENTATION_SUMMARY.md](../AOT_IMPLEMENTATION_SUMMARY.md) - Implementation details
- [FINAL_SUMMARY.md](../FINAL_SUMMARY.md) - Executive summary

## License

Apache-2.0 (same as YantraJS)
