using System;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;

namespace YantraJS.Expressions
{
    public class YNewArrayBoundsExpression: YExpression
    {
        public readonly Type ElementType;
        public readonly YExpression Size;

        [RequiresDynamicCode("Creating arrays at runtime requires dynamic code generation.")]
        public YNewArrayBoundsExpression(Type type, YExpression size)
            : base(YExpressionType.NewArrayBounds, type.MakeArrayType())
        {
            this.ElementType = type;
            this.Size = size;
        }

        public override void Print(IndentedTextWriter writer)
        {
            writer.Write($"new {ElementType.GetFriendlyName()} [");
            Size.Print(writer);
            writer.Write("]");
        }
    }
}