using WorkflowCore.Models;
using WorkflowCore.Services;

namespace WorkflowCore.Primitives;

public class WorkflowStepInline : WorkflowStep<InlineStepBody>
{
    public Func<IStepExecutionContext, ExecutionResult> Body { get; set; }

    public override IStepBody ConstructBody(IServiceProvider serviceProvider)
    {
        return new InlineStepBody(Body);
    }
}
