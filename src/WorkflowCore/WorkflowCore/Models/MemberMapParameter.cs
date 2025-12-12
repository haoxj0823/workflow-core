using System.Linq.Expressions;

namespace WorkflowCore.Models;

public class MemberMapParameter : IStepParameter
{
    private readonly LambdaExpression _source;
    private readonly LambdaExpression _target;

    public MemberMapParameter(LambdaExpression source, LambdaExpression target)
    {
        if (target.Body.NodeType != ExpressionType.MemberAccess)
        {
            throw new NotSupportedException();
        }

        _source = source;
        _target = target;
    }

    private void Assign(object sourceObject, LambdaExpression sourceExpr, object targetObject, LambdaExpression targetExpr, IStepExecutionContext context)
    {
        var resolvedValue = sourceExpr.Parameters.Count switch
        {
            1 => sourceExpr.Compile().DynamicInvoke(sourceObject),
            2 => sourceExpr.Compile().DynamicInvoke(sourceObject, context),
            _ => throw new ArgumentException(),
        };

        if (resolvedValue == null)
        {
            var defaultAssign = Expression.Lambda(Expression.Assign(targetExpr.Body, Expression.Default(targetExpr.ReturnType)), targetExpr.Parameters.Single());
            defaultAssign.Compile().DynamicInvoke(targetObject);
            return;
        }

        var valueExpr = Expression.Convert(Expression.Constant(resolvedValue), targetExpr.ReturnType);
        var assign = Expression.Lambda(Expression.Assign(targetExpr.Body, valueExpr), targetExpr.Parameters.Single());
        assign.Compile().DynamicInvoke(targetObject);
    }

    public void AssignInput(object data, IStepBody body, IStepExecutionContext context)
    {
        Assign(data, _source, body, _target, context);
    }

    public void AssignOutput(object data, IStepBody body, IStepExecutionContext context)
    {
        Assign(body, _source, data, _target, context);
    }
}
