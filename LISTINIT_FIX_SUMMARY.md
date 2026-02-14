# VisitListInit Fix Summary

## Problem

Runtime tests in YantraJS.Core were failing with errors related to `VisitListInit` in LambdaConverter.cs:

```
System.InvalidOperationException: Type 'YantraJS.Core.JSArray' is not IEnumerable
```

The issue occurred when JavaScript code created arrays or collections, specifically in Array constructor tests.

## Root Cause

The `VisitListInit` method in LambdaConverter.cs was attempting to use `Expression.ListInit()` for all collection initializations. However, `Expression.ListInit()` has a requirement that the type must implement `IEnumerable`.

JSArray and similar types in YantraJS.Core don't implement `IEnumerable`, causing the compilation to fail at runtime.

## Solution

Modified `VisitListInit` to intelligently handle both IEnumerable and non-IEnumerable types:

### For IEnumerable Types
Uses the built-in `Expression.ListInit()` for optimal performance:
```csharp
return Expression.ListInit(newExpr, initializers);
```

### For Non-IEnumerable Types
Creates a manual block with explicit Add method calls:
```csharp
var listInit = new JSArray();
listInit.Add(item1);
listInit.Add(item2);
return listInit;
```

### Type Conversion
Added automatic type conversion for Add method arguments to match expected parameter types:
- Checks each argument type against the method parameter type
- Inserts `Expression.Convert()` when types don't match
- Fixes "Expression of type 'System.Object' cannot be used for parameter of type 'YantraJS.Core.JSValue'" errors

## Implementation Details

```csharp
protected override Expression VisitListInit(YListInitExpression node)
{
    var newExpr = Visit(node.NewExpression) as NewExpression;
    var type = newExpr.Type;
    var isEnumerable = typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
    
    if (isEnumerable)
    {
        // Use Expression.ListInit for IEnumerable types
        var initializers = new List<ElementInit>();
        // ... build initializers
        return Expression.ListInit(newExpr, initializers);
    }
    else
    {
        // Create block with manual Add calls for non-IEnumerable types
        var variable = Expression.Variable(type, "listInit");
        var expressions = new List<Expression>();
        
        expressions.Add(Expression.Assign(variable, newExpr));
        
        // Add each initializer with proper type conversion
        // ... build Add method calls with converted arguments
        
        expressions.Add(variable);
        return Expression.Block(new[] { variable }, expressions);
    }
}
```

## Test Results

### Before Fix
- ❌ Failed: 112 tests
- ✅ Passed: 163 tests
- Total: 275 tests
- Pass Rate: 59%

### After Fix
- ❌ Failed: 39 tests
- ✅ Passed: 236 tests
- Total: 277 tests
- Pass Rate: 85%

### Improvement
- **73 tests fixed!**
- **65% reduction in failures**
- **26% improvement in pass rate**

## Specific Tests Fixed

- `ArrayTests.Constructor` - Now passes
- Various Array manipulation tests
- Collection initialization tests
- JSArray creation and manipulation

## Files Modified

- `YantraJS.ExpressionCompiler/SL/LambdaConverter.cs` - VisitListInit method (~60 lines)

## Impact

This fix is critical for AOT (Ahead-of-Time) compilation compatibility:
- Enables JavaScript array and collection operations in AOT mode
- Makes YantraJS.Core functional with Expression.Compile instead of Reflection.Emit
- Essential for iOS, Native AOT, and other JIT-restricted environments

## Remaining Work

39 tests still failing (out of 277). These appear to be related to other expression conversion issues, not ListInit. Investigation needed for:
- Void expression handling
- Other custom expression types
- Edge cases in type conversions

## Related Issues

- Part of the larger effort to replace Reflection.Emit with Expression.Compile
- Enables netstandard2.0/2.1 compatibility without Reflection.Emit
- Critical for AOT deployment scenarios
