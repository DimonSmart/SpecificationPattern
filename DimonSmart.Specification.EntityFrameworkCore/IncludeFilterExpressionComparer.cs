using System.Linq.Expressions;

namespace DimonSmart.Specification.EntityFrameworkCore;

/// <summary>
/// Conservatively compares normalized Include-filter expressions without invoking or compiling them.
/// Unrecognized expression nodes are not deduplicated unless they are the same object.
/// Closure references must be identical; value constants are compared by value.
/// </summary>
internal static class IncludeFilterExpressionComparer
{
    internal static bool AreEquivalent(LambdaExpression left, LambdaExpression right) =>
        Equal(left, right, new Dictionary<ParameterExpression, ParameterExpression>(
            ReferenceEqualityComparer.Instance));

    private static bool Equal(Expression? a, Expression? b,
        Dictionary<ParameterExpression, ParameterExpression> map)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null || a.NodeType != b.NodeType || a.Type != b.Type)
            return false;

        switch (a, b)
        {
            case (LambdaExpression x, LambdaExpression y):
                if (x.Parameters.Count != y.Parameters.Count ||
                    x.Parameters.Where((parameter, i) => parameter.Type != y.Parameters[i].Type).Any())
                    return false;
                for (var i = 0; i < x.Parameters.Count; i++)
                    map[x.Parameters[i]] = y.Parameters[i];
                var equal = Equal(x.Body, y.Body, map);
                foreach (var parameter in x.Parameters) map.Remove(parameter);
                return equal;

            case (ParameterExpression x, ParameterExpression y):
                return map.TryGetValue(x, out var mapped)
                    ? ReferenceEquals(mapped, y) : ReferenceEquals(x, y);

            case (ConstantExpression x, ConstantExpression y):
                return ReferenceEquals(x.Value, y.Value) ||
                    (x.Value is not null && y.Value is not null &&
                     (x.Type.IsValueType || x.Value is string) && Equals(x.Value, y.Value));

            case (MemberExpression x, MemberExpression y):
                return Equals(x.Member, y.Member) && Equal(x.Expression, y.Expression, map);

            case (MethodCallExpression x, MethodCallExpression y):
                return Equals(x.Method, y.Method) && Equal(x.Object, y.Object, map) &&
                    EqualAll(x.Arguments, y.Arguments, map);

            case (UnaryExpression x, UnaryExpression y):
                return Equals(x.Method, y.Method) && Equal(x.Operand, y.Operand, map);

            case (BinaryExpression x, BinaryExpression y):
                return Equals(x.Method, y.Method) &&
                    x.IsLifted == y.IsLifted && x.IsLiftedToNull == y.IsLiftedToNull &&
                    Equal(x.Left, y.Left, map) && Equal(x.Right, y.Right, map) &&
                    Equal(x.Conversion, y.Conversion, map);

            case (ConditionalExpression x, ConditionalExpression y):
                return Equal(x.Test, y.Test, map) &&
                    Equal(x.IfTrue, y.IfTrue, map) && Equal(x.IfFalse, y.IfFalse, map);

            case (NewArrayExpression x, NewArrayExpression y):
                return EqualAll(x.Expressions, y.Expressions, map);

            case (NewExpression x, NewExpression y):
                return Equals(x.Constructor, y.Constructor) &&
                    EqualAll(x.Arguments, y.Arguments, map) &&
                    ((x.Members is null && y.Members is null) ||
                     (x.Members is not null && y.Members is not null &&
                      x.Members.SequenceEqual(y.Members)));

            case (TypeBinaryExpression x, TypeBinaryExpression y):
                return x.TypeOperand == y.TypeOperand && Equal(x.Expression, y.Expression, map);

            case (DefaultExpression, DefaultExpression):
                return true;

            default:
                return false;
        }
    }

    private static bool EqualAll(IReadOnlyList<Expression> a, IReadOnlyList<Expression> b,
        Dictionary<ParameterExpression, ParameterExpression> map)
    {
        if (a.Count != b.Count) return false;
        for (var i = 0; i < a.Count; i++)
            if (!Equal(a[i], b[i], map)) return false;
        return true;
    }
}
