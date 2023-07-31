using System.Linq.Expressions;

namespace DimonSmart.Specification;

internal static class ExpressionOperations
{
    public static Expression<Func<T, bool>> CombineExpressions<T>(
        Expression<Func<T, bool>> expression1,
        Expression<Func<T, bool>> expression2,
        Func<Expression, Expression, BinaryExpression> logicalOperator)
    {
        var parameter = expression1.Parameters[0];
        var body2 = new ParameterReplacer(parameter).Visit(expression2.Body);

        var combinedBody = logicalOperator(expression1.Body, body2);

        var lambda = Expression.Lambda<Func<T, bool>>(combinedBody, parameter);
        return lambda;
    }
}