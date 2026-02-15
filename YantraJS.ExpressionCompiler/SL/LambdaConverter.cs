using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using YantraJS.Core;
using YantraJS.Expressions;

namespace YantraJS.SL
{
    public class LambdaConverter : YExpressionVisitor<Expression>
    {
        private Dictionary<YParameterExpression, ParameterExpression> cache = new Dictionary<YParameterExpression, ParameterExpression>();
        private Dictionary<YLabelTarget, LabelTarget> labelCache = new Dictionary<YLabelTarget, LabelTarget>();

        private LabelTarget GetOrCreateLabel(YLabelTarget yLabel)
        {
            if (labelCache.TryGetValue(yLabel, out var label))
                return label;
            label = Expression.Label(yLabel.LabelType, yLabel.Name);
            labelCache[yLabel] = label;
            return label;
        }

        public (IFastEnumerable<ParameterExpression> pe, IDisposable disposable) Register(IFastEnumerable<YParameterExpression> plist)
        {
            if (plist == null)
            {
                return (null, null);
            }

            var pe = new Sequence<ParameterExpression>(plist.Count);
            var en = plist.GetFastEnumerator();
            while(en.MoveNext(out var e))
            {
                // var e = plist[i];
                var p = Expression.Parameter(e.Type, e.Name);
                // pe[i] = p;
                pe.Add(p);
                cache[e] = p;
            }

            var d = new DisposableAction(() => {
                var a = plist;
                var en = plist.GetFastEnumerator();
                while(en.MoveNext(out var item))
                {
                    cache.Remove(item);
                }
            });

            return (pe, d);
        }

        protected override Expression VisitAddressOf(YAddressOfExpression node)
        {
            // AddressOf is not directly supported in LINQ Expressions
            // This is typically used for ref parameters
            // For AOT compatibility, we'll just visit the target
            // The actual ref behavior should be handled at the parameter level
            return Visit(node.Target);
        }

        protected override Expression VisitArrayIndex(YArrayIndexExpression yArrayIndexExpression)
        {
            // Use ArrayAccess instead of ArrayIndex to ensure the expression is writable
            // This is important for assignments like: array[index] = value
            return Expression.ArrayAccess(Visit(yArrayIndexExpression.Target), Visit(yArrayIndexExpression.Index));
        }

        protected override Expression VisitArrayLength(YArrayLengthExpression arrayLengthExpression)
        {
            return Expression.ArrayLength(Visit(arrayLengthExpression.Target));
        }

        protected override Expression VisitAssign(YAssignExpression yAssignExpression)
        {
            return Expression.Assign(Visit(yAssignExpression.Left), Visit(yAssignExpression.Right));
        }

        protected override Expression VisitBinary(YBinaryExpression yBinaryExpression)
        {
            var left = Visit(yBinaryExpression.Left);
            var right = Visit(yBinaryExpression.Right);
            switch (yBinaryExpression.Operator)
            {
                case YOperator.Add:
                    return Expression.Add(left, right);
                case YOperator.Subtract:
                    return Expression.Subtract(left, right);
                case YOperator.Multipley:
                    return Expression.Multiply(left, right);
                case YOperator.Divide:
                    return Expression.Divide(left, right);
                case YOperator.Mod:
                    return Expression.Modulo(left, right);
                case YOperator.Power:
                    return Expression.Power(left, right);
                case YOperator.Xor:
                    return Expression.ExclusiveOr(left, right);
                case YOperator.BitwiseAnd:
                    return Expression.And(left, right);
                case YOperator.BitwiseOr:
                    return Expression.Or(left, right);
                case YOperator.BooleanAnd:
                    return Expression.AndAlso(left, right);
                case YOperator.BooleanOr:
                    return Expression.OrElse(left, right);
                case YOperator.Less:
                    return Expression.LessThan(left, right);
                case YOperator.LessOrEqual:
                    return Expression.LessThanOrEqual(left, right);
                case YOperator.Greater:
                    return Expression.GreaterThan(left, right);
                case YOperator.GreaterOrEqual:
                    return Expression.GreaterThanOrEqual(left, right);
                case YOperator.Equal:
                    return Expression.Equal(left, right);
                case YOperator.NotEqual:
                    return Expression.NotEqual(left, right);
                case YOperator.LeftShift:
                    return Expression.LeftShift(left, right);
                case YOperator.RightShift:
                    return Expression.RightShift(left, right);
                case YOperator.UnsignedRightShift:
                    return Expression.RightShift( Expression.Convert(left, typeof(uint)), right);
            }
            throw new NotImplementedException();
        }

        protected override Expression VisitBlock(YBlockExpression yBlockExpression)
        {
            var (list, d) = Register(yBlockExpression.Variables);
            using (d)
            {
                return Expression.Block(list, yBlockExpression.Expressions.Select(Visit));
            }
        }

        protected override Expression VisitBooleanConstant(YBooleanConstantExpression node)
        {
            return Expression.Constant(node.Value, typeof(bool));
        }

        protected override Expression VisitBox(YBoxExpression node)
        {
            return Expression.Convert(Visit(node.Target), typeof(object));
        }

        protected override Expression VisitByteConstant(YByteConstantExpression node)
        {
            return Expression.Constant(node.Value, typeof(byte));
        }

        protected override Expression VisitCall(YCallExpression yCallExpression)
        {
            return Expression.Call(Visit(yCallExpression.Target), yCallExpression.Method, yCallExpression.Arguments.Select(Visit));
        }

        protected override Expression VisitCoalesce(YCoalesceExpression yCoalesceExpression)
        {
            return Expression.Coalesce(Visit(yCoalesceExpression.Left), Visit(yCoalesceExpression.Right));
        }

        protected override Expression VisitCoalesceCall(YCoalesceCallExpression node)
        {
            // CoalesceCall: target?.test() ? true() : false()
            // Expand to: (target != null && target.test()) ? target.true() : target.false()
            
            var target = Visit(node.Target);
            Expression condition;
            
            if (node.Test is PropertyInfo prop)
            {
                // Property test - just access the property value
                var propAccess = Expression.Property(target, prop);
                condition = propAccess;
            }
            else if (node.Test is MethodInfo method)
            {
                // Method test
                condition = Expression.Call(target, method, node.TestArguments?.Select(Visit) ?? Enumerable.Empty<Expression>());
            }
            else
            {
                throw new NotSupportedException($"Unsupported test member type: {node.Test?.GetType()}");
            }
            
            // Add null check for target if it's a reference type
            if (!target.Type.IsValueType)
            {
                condition = Expression.AndAlso(
                    Expression.NotEqual(target, Expression.Constant(null, target.Type)),
                    condition);
            }
            
            // Build true and false branches
            Expression trueExpr = node.True != null 
                ? Expression.Call(target, node.True, node.TrueArguments?.Select(Visit) ?? Enumerable.Empty<Expression>())
                : Expression.Constant(null, node.Type);
                
            Expression falseExpr = node.False != null
                ? Expression.Call(target, node.False, node.FalseArguments?.Select(Visit) ?? Enumerable.Empty<Expression>())
                : Expression.Constant(null, node.Type);
            
            return Expression.Condition(condition, trueExpr, falseExpr);
        }

        protected override Expression VisitConditional(YConditionalExpression yConditionalExpression)
        {
            var test = Visit(yConditionalExpression.test);
            var trueExpr = Visit(yConditionalExpression.@true);
            var falseExpr = yConditionalExpression.@false != null 
                ? Visit(yConditionalExpression.@false) 
                : Expression.Empty();
            
            // Ensure both branches have compatible types for Expression.Condition
            // If one branch is void, both must be void
            if (trueExpr.Type == typeof(void) && falseExpr.Type == typeof(void))
            {
                // Both are void, use IfThenElse pattern instead of Condition
                return Expression.IfThenElse(test, trueExpr, falseExpr);
            }
            
            // If types don't match, try to make them compatible
            if (trueExpr.Type != falseExpr.Type)
            {
                // If one is void, convert to block that returns default value
                if (trueExpr.Type == typeof(void))
                {
                    trueExpr = Expression.Block(trueExpr, Expression.Default(falseExpr.Type));
                }
                else if (falseExpr.Type == typeof(void))
                {
                    falseExpr = Expression.Block(falseExpr, Expression.Default(trueExpr.Type));
                }
                else
                {
                    // Try to find common type
                    var resultType = trueExpr.Type;
                    if (!trueExpr.Type.IsAssignableFrom(falseExpr.Type))
                    {
                        resultType = typeof(object);
                    }
                    
                    if (trueExpr.Type != resultType)
                    {
                        trueExpr = Expression.Convert(trueExpr, resultType);
                    }
                    if (falseExpr.Type != resultType)
                    {
                        falseExpr = Expression.Convert(falseExpr, resultType);
                    }
                }
            }
            
            return Expression.Condition(test, trueExpr, falseExpr);
        }

        protected override Expression VisitConstant(YConstantExpression yConstantExpression)
        {
            return Expression.Constant(yConstantExpression.Value);
        }

        protected override Expression VisitConvert(YConvertExpression convertExpression)
        {
            return Expression.Convert(Visit(convertExpression.Target), convertExpression.Type);
        }

        protected override Expression VisitDebugInfo(YDebugInfoExpression node)
        {
            return Expression.Empty();
        }

        protected override Expression VisitDelegate(YDelegateExpression yDelegateExpression)
        {
            // Create a delegate from a method
            // This is typically Expression.Call for static methods or instance methods
            if (yDelegateExpression.Method.IsStatic)
            {
                return Expression.Constant(
                    System.Delegate.CreateDelegate(yDelegateExpression.Type, yDelegateExpression.Method));
            }
            else
            {
                // For instance methods, we need a target object
                // This is a limitation - we'll throw for now as we need context
                throw new NotSupportedException("Instance method delegates require a target object");
            }
        }

        protected override Expression VisitDoubleConstant(YDoubleConstantExpression node)
        {
            return Expression.Constant(node.Value, typeof(double));
        }

        protected override Expression VisitEmpty(YEmptyExpression exp)
        {
            return Expression.Empty();
        }

        protected override Expression VisitField(YFieldExpression yFieldExpression)
        {
            return Expression.Field(Visit(yFieldExpression.Target), yFieldExpression.FieldInfo);
        }

        protected override Expression VisitFloatConstant(YFloatConstantExpression node)
        {
            return Expression.Constant(node.Value, typeof(float));
        }

        protected override Expression VisitGoto(YGoToExpression yGoToExpression)
        {
            var label = GetOrCreateLabel(yGoToExpression.Target);
            if (yGoToExpression.Default != null)
            {
                return Expression.Goto(label, Visit(yGoToExpression.Default));
            }
            return Expression.Goto(label);
        }

        protected override Expression VisitILOffset(YILOffsetExpression node)
        {
            // IL Offset is debug-only information, not needed for AOT
            // Return an empty expression
            return Expression.Empty();
        }

        protected override Expression VisitIndex(YIndexExpression yIndexExpression)
        {
            var target = Visit(yIndexExpression.Target);
            var args = yIndexExpression.Arguments.Select(Visit).ToArray();
            
            // Check if the property is valid for the target type
            var property = yIndexExpression.Property;
            if (property.DeclaringType != null && !property.DeclaringType.IsAssignableFrom(target.Type))
            {
                // Find the property on the actual target type
                var indexParams = property.GetIndexParameters();
                var paramTypes = indexParams.Select(p => p.ParameterType).ToArray();
                
                // Try to find matching indexer on target type
                var targetProperty = target.Type.GetProperty("Item", paramTypes);
                if (targetProperty != null)
                {
                    property = targetProperty;
                }
            }
            
            return Expression.Property(target, property, args);
        }

        protected override Expression VisitInt32Constant(YInt32ConstantExpression node)
        {
            return Expression.Constant(node.Value, typeof(int));
        }

        protected override Expression VisitInt64Constant(YInt64ConstantExpression node)
        {
            return Expression.Constant(node.Value, typeof(long));
        }

        protected override Expression VisitInvoke(YInvokeExpression invokeExpression)
        {
            return Expression.Invoke(Visit(invokeExpression.Target), invokeExpression.Arguments.Select(Visit));
        }

        protected override Expression VisitJumpSwitch(YJumpSwitchExpression node)
        {
            // JumpSwitch is a computed goto with jump table
            // Convert to a switch expression with goto statements
            var target = Visit(node.Target);
            
            // Create labels for each case
            var caseLabels = new List<LabelTarget>();
            var en = node.Cases.GetFastEnumerator();
            while (en.MoveNext(out var caseLabel))
            {
                caseLabels.Add(GetOrCreateLabel(caseLabel));
            }
            
            // Build switch cases - each case just jumps to its label
            var switchCases = new List<SwitchCase>();
            for (int i = 0; i < caseLabels.Count; i++)
            {
                var gotoExpr = Expression.Goto(caseLabels[i]);
                switchCases.Add(Expression.SwitchCase(gotoExpr, Expression.Constant(i)));
            }
            
            // Add default case that throws for out-of-bounds values
            var defaultCase = Expression.Throw(
                Expression.New(
                    typeof(ArgumentOutOfRangeException).GetConstructor(new[] { typeof(string) }),
                    Expression.Constant("JumpSwitch target index out of bounds")));
            
            // Create the switch expression with default case
            return Expression.Switch(target, defaultCase, switchCases.ToArray());
        }

        protected override Expression VisitLabel(YLabelExpression yLabelExpression)
        {
            var label = GetOrCreateLabel(yLabelExpression.Target);
            if (yLabelExpression.Default != null)
            {
                return Expression.Label(label, Visit(yLabelExpression.Default));
            }
            return Expression.Label(label);
        }

        protected override Expression VisitLambda(YLambdaExpression yLambdaExpression)
        {
            // Register parameters for this lambda scope
            var paramList = new YParameterExpression[yLambdaExpression.Parameters.Length];
            Array.Copy(yLambdaExpression.Parameters, paramList, yLambdaExpression.Parameters.Length);
            var (linqParams, disposable) = Register(paramList.AsSequence());
            using (disposable)
            {
                var body = Visit(yLambdaExpression.Body);
                
                // Create the lambda expression
                return Expression.Lambda(yLambdaExpression.Type, body, linqParams);
            }
        }

        protected override Expression VisitListInit(YListInitExpression node)
        {
            var newExpr = Visit(node.NewExpression) as NewExpression;
            
            // Check if the type implements IEnumerable
            // Expression.ListInit requires IEnumerable but some types like JSArray don't implement it
            var type = newExpr.Type;
            var isEnumerable = typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
            
            if (isEnumerable)
            {
                // Use built-in ListInit for IEnumerable types
                var initializers = new List<ElementInit>();
                var en = node.Members.GetFastEnumerator();
                while (en.MoveNext(out var member))
                {
                    initializers.Add(Expression.ElementInit(member.AddMethod, member.Arguments.Select(Visit)));
                }
                return Expression.ListInit(newExpr, initializers);
            }
            else
            {
                // For non-IEnumerable types, manually create a block with Add calls
                var variable = Expression.Variable(type, "listInit");
                var expressions = new List<Expression>();
                
                // Assign the new instance to the variable
                expressions.Add(Expression.Assign(variable, newExpr));
                
                // Add each initializer as a method call
                var en = node.Members.GetFastEnumerator();
                while (en.MoveNext(out var member))
                {
                    // Visit arguments and convert types if necessary
                    var visitedArgs = new List<Expression>();
                    var parameters = member.AddMethod.GetParameters();
                    int argIndex = 0;
                    
                    var argEn = member.Arguments.GetFastEnumerator();
                    while (argEn.MoveNext(out var arg))
                    {
                        var visitedArg = Visit(arg);
                        
                        // Convert type if necessary
                        if (argIndex < parameters.Length)
                        {
                            var paramType = parameters[argIndex].ParameterType;
                            if (visitedArg.Type != paramType && !paramType.IsAssignableFrom(visitedArg.Type))
                            {
                                visitedArg = Expression.Convert(visitedArg, paramType);
                            }
                        }
                        visitedArgs.Add(visitedArg);
                        argIndex++;
                    }
                    
                    expressions.Add(Expression.Call(variable, member.AddMethod, visitedArgs));
                }
                
                // Return the variable
                expressions.Add(variable);
                
                return Expression.Block(new[] { variable }, expressions);
            }
        }

        protected override Expression VisitLoop(YLoopExpression yLoopExpression)
        {
            var breakLabel = GetOrCreateLabel(yLoopExpression.Break);
            var continueLabel = GetOrCreateLabel(yLoopExpression.Continue);
            return Expression.Loop(Visit(yLoopExpression.Body), breakLabel, continueLabel);
        }

        protected override Expression VisitMemberInit(YMemberInitExpression memberInitExpression)
        {
            var newExpr = Visit(memberInitExpression.Target) as NewExpression;
            var bindings = new List<MemberBinding>();
            
            var en = memberInitExpression.Bindings.GetFastEnumerator();
            while (en.MoveNext(out var binding))
            {
                switch (binding.BindingType)
                {
                    case BindingType.MemberAssignment:
                        var assignment = binding as YMemberAssignment;
                        bindings.Add(Expression.Bind(binding.Member, Visit(assignment.Value)));
                        break;
                    case BindingType.MemberListInit:
                        var listInit = binding as YMemberElementInit;
                        var elementInits = listInit.Elements.Select(e => 
                            Expression.ElementInit(e.AddMethod, e.Arguments.Select(Visit))).ToArray();
                        bindings.Add(Expression.ListBind(binding.Member, elementInits));
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported binding type: {binding.BindingType}");
                }
            }
            
            return Expression.MemberInit(newExpr, bindings);
        }

        protected override Expression VisitMethodConstant(YMethodConstantExpression node)
        {
            return Expression.Constant(node.Value, typeof(System.Reflection.MethodInfo));
        }

        protected override Expression VisitNew(YNewExpression yNewExpression)
        {
            return Expression.New(yNewExpression.constructor, yNewExpression.args.Select(Visit));
        }

        protected override Expression VisitNewArray(YNewArrayExpression yNewArrayExpression)
        {
            if (yNewArrayExpression.Elements == null || yNewArrayExpression.Elements.Count == 0)
            {
                return Expression.NewArrayInit(yNewArrayExpression.ElementType);
            }
            return Expression.NewArrayInit(yNewArrayExpression.ElementType, yNewArrayExpression.Elements.Select(Visit));
        }

        protected override Expression VisitNewArrayBounds(YNewArrayBoundsExpression yNewArrayBoundsExpression)
        {
            return Expression.NewArrayBounds(yNewArrayBoundsExpression.ElementType, Visit(yNewArrayBoundsExpression.Size));
        }

        protected override Expression VisitParameter(YParameterExpression yParameterExpression)
        {
            if (cache.TryGetValue(yParameterExpression, out var param))
            {
                return param;
            }
            // Create a new parameter if not in cache (shouldn't normally happen)
            var newParam = Expression.Parameter(yParameterExpression.Type, yParameterExpression.Name);
            cache[yParameterExpression] = newParam;
            return newParam;
        }

        protected override Expression VisitProperty(YPropertyExpression yPropertyExpression)
        {
            var target = yPropertyExpression.Target == null ? null : Visit(yPropertyExpression.Target);
            return Expression.Property(target, yPropertyExpression.PropertyInfo);
        }

        //protected override Expression VisitRelay(YRelayExpression yRelayExpression)
        //{
        //    throw new NotImplementedException();
        //}

        protected override Expression VisitReturn(YReturnExpression yReturnExpression)
        {
            var label = GetOrCreateLabel(yReturnExpression.Target);
            if (yReturnExpression.Default != null)
            {
                var valueExpr = Visit(yReturnExpression.Default);
                
                // Ensure the value matches the label's type
                if (label.Type != typeof(void) && valueExpr.Type != label.Type)
                {
                    // Try to convert the expression to the expected type
                    if (valueExpr.Type.IsAssignableFrom(label.Type) || label.Type.IsAssignableFrom(valueExpr.Type))
                    {
                        valueExpr = Expression.Convert(valueExpr, label.Type);
                    }
                    else if (valueExpr.Type == typeof(object) && label.Type != typeof(object))
                    {
                        // Convert from object to the target type
                        valueExpr = Expression.Convert(valueExpr, label.Type);
                    }
                }
                
                return Expression.Return(label, valueExpr);
            }
            return Expression.Return(label);
        }

        protected override Expression VisitStringConstant(YStringConstantExpression node)
        {
            return Expression.Constant(node.Value, typeof(string));
        }

        protected override Expression VisitSwitch(YSwitchExpression node)
        {
            var target = Visit(node.Target);
            var defaultBody = node.Default != null ? Visit(node.Default) : null;
            var cases = node.Cases.Select(c => 
                Expression.SwitchCase(
                    Visit(c.Body), 
                    c.TestValues.Select(Visit)
                )).ToArray();
            
            if (node.CompareMethod != null)
            {
                return Expression.Switch(target, defaultBody, node.CompareMethod, cases);
            }
            return Expression.Switch(target, defaultBody, cases);
        }

        protected override Expression VisitThrow(YThrowExpression throwExpression)
        {
            if (throwExpression.Expression != null)
            {
                var valueExpr = Visit(throwExpression.Expression);
                
                // If the expression is already an Exception type, throw it directly
                if (typeof(Exception).IsAssignableFrom(valueExpr.Type))
                {
                    return Expression.Throw(valueExpr);
                }
                
                // Otherwise, wrap it in JSException.FromValue
                // Find JSException type by name (to avoid hard reference to YantraJS.Core)
                var jsExceptionType = valueExpr.Type.Assembly.GetType("YantraJS.Core.JSException");
                if (jsExceptionType == null)
                {
                    // Fallback: if JSException not found, try to convert to string and throw InvalidOperationException
                    var message = Expression.Call(valueExpr, valueExpr.Type.GetMethod("ToString", Type.EmptyTypes));
                    var invalidOpEx = Expression.New(
                        typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) }),
                        message);
                    return Expression.Throw(invalidOpEx);
                }
                
                // Find JSValue type
                var jsValueType = valueExpr.Type.Assembly.GetType("YantraJS.Core.JSValue");
                if (jsValueType == null)
                {
                    jsValueType = valueExpr.Type; // If JSValue not found, use the expression type
                }
                
                // Find JSException.FromValue(JSValue) method
                var fromValueMethod = jsExceptionType.GetMethod(
                    "FromValue", 
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { jsValueType },
                    null);
                
                if (fromValueMethod == null)
                {
                    // Fallback to constructor
                    var ctor = jsExceptionType.GetConstructor(new[] { jsValueType });
                    if (ctor != null)
                    {
                        if (valueExpr.Type != jsValueType)
                        {
                            valueExpr = Expression.Convert(valueExpr, jsValueType);
                        }
                        var exceptionExpr = Expression.New(ctor, valueExpr);
                        return Expression.Throw(exceptionExpr);
                    }
                }
                else
                {
                    // Convert the expression to JSValue if needed
                    if (valueExpr.Type != jsValueType)
                    {
                        valueExpr = Expression.Convert(valueExpr, jsValueType);
                    }
                    
                    var exceptionExpr = Expression.Call(fromValueMethod, valueExpr);
                    return Expression.Throw(exceptionExpr);
                }
                
                // Last resort: throw as-is (will likely fail)
                return Expression.Throw(valueExpr);
            }
            return Expression.Rethrow();
        }

        protected override Expression VisitTryCatchFinally(YTryCatchFinallyExpression tryCatchFinallyExpression)
        {
            var tryBody = Visit(tryCatchFinallyExpression.Try);
            Expression result = tryBody;

            if (tryCatchFinallyExpression.Catch != null)
            {
                var yParam = tryCatchFinallyExpression.Catch.Parameter;
                ParameterExpression exceptionParam = Expression.Parameter(typeof(Exception), "ex");
                
                Expression catchBody;
                
                if (yParam != null)
                {
                    // Check if we need to convert Exception to JSVariable or other type
                    var jsVariableType = yParam.Type.Assembly?.GetType("YantraJS.Core.JSVariable");
                    
                    if (jsVariableType != null && yParam.Type == jsVariableType)
                    {
                        // Create a variable for JSVariable and initialize it from the exception
                        var jsVarParam = Expression.Parameter(yParam.Type, yParam.Name);
                        cache[yParam] = jsVarParam;
                        
                        // Find JSVariable constructor: JSVariable(Exception e, string name)
                        var ctor = jsVariableType.GetConstructor(new[] { typeof(Exception), typeof(string) });
                        if (ctor != null)
                        {
                            // Create: var varName = new JSVariable(ex, "varName");
                            var initExpr = Expression.Assign(
                                jsVarParam,
                                Expression.New(ctor, exceptionParam, Expression.Constant(yParam.Name)));
                            
                            // Wrap catch body in a block with the variable initialization
                            var bodyExpr = Visit(tryCatchFinallyExpression.Catch.Body);
                            catchBody = Expression.Block(
                                new[] { jsVarParam },
                                initExpr,
                                bodyExpr);
                        }
                        else
                        {
                            // Fallback if constructor not found
                            catchBody = Visit(tryCatchFinallyExpression.Catch.Body);
                        }
                        
                        cache.Remove(yParam);
                    }
                    else if (typeof(Exception).IsAssignableFrom(yParam.Type))
                    {
                        // Parameter is already an Exception type
                        var catchParam = Expression.Parameter(yParam.Type, yParam.Name);
                        cache[yParam] = catchParam;
                        catchBody = Visit(tryCatchFinallyExpression.Catch.Body);
                        cache.Remove(yParam);
                        exceptionParam = catchParam; // Use the typed exception parameter
                    }
                    else
                    {
                        // Other types - try direct mapping
                        var catchParam = Expression.Parameter(yParam.Type, yParam.Name);
                        cache[yParam] = catchParam;
                        catchBody = Visit(tryCatchFinallyExpression.Catch.Body);
                        cache.Remove(yParam);
                    }
                }
                else
                {
                    catchBody = Visit(tryCatchFinallyExpression.Catch.Body);
                }
                
                var catchBlock = Expression.Catch(exceptionParam, catchBody);
                result = Expression.TryCatch(tryBody, catchBlock);
            }

            if (tryCatchFinallyExpression.Finally != null)
            {
                var finallyBody = Visit(tryCatchFinallyExpression.Finally);
                if (tryCatchFinallyExpression.Catch != null)
                {
                    // TryCatchFinally
                    result = Expression.TryFinally(result, finallyBody);
                }
                else
                {
                    // TryFinally
                    result = Expression.TryFinally(tryBody, finallyBody);
                }
            }

            return result;
        }

        protected override Expression VisitTypeAs(YTypeAsExpression yTypeAsExpression)
        {
            return Expression.TypeAs(Visit(yTypeAsExpression.Target), yTypeAsExpression.Type);
        }

        protected override Expression VisitTypeConstant(YTypeConstantExpression node)
        {
            return Expression.Constant(node.Value, typeof(Type));
        }

        protected override Expression VisitTypeIs(YTypeIsExpression yTypeIsExpression)
        {
            return Expression.TypeIs(Visit(yTypeIsExpression.Target), yTypeIsExpression.TypeOperand);
        }

        protected override Expression VisitUInt32Constant(YUInt32ConstantExpression node)
        {
            return Expression.Constant(node.Value, typeof(uint));
        }

        protected override Expression VisitUInt64Constant(YUInt64ConstantExpression node)
        {
            return Expression.Constant(node.Value, typeof(ulong));
        }

        protected override Expression VisitUnary(YUnaryExpression yUnaryExpression)
        {
            var target = Visit(yUnaryExpression.Target);
            switch (yUnaryExpression.Operator)
            {
                case YUnaryOperator.Not:
                    return Expression.Not(target);
                case YUnaryOperator.Negative:
                    return Expression.Negate(target);
                case YUnaryOperator.OnesComplement:
                    return Expression.OnesComplement(target);
            }
            throw new NotSupportedException($"Unsupported unary operator: {yUnaryExpression.Operator}");
        }

        protected override Expression VisitUnbox(YUnboxExpression node)
        {
            return Expression.Convert(Visit(node.Target), node.Type);
        }

        protected override Expression VisitYield(YYieldExpression node)
        {
            // Yield is complex - it requires state machine transformation
            // For now, throw NotSupportedException as this requires significant work
            // TODO: Implement state machine transformation for generators
            throw new NotSupportedException("Yield expressions require state machine transformation and are not yet supported in AOT compilation mode. " +
                "Consider refactoring to use regular return values or callbacks instead of generators.");
        }
    }
}
