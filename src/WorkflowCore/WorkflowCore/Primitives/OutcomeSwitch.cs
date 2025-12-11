using WorkflowCore.Exceptions;
using WorkflowCore.Models;
using WorkflowCore.Services;

namespace WorkflowCore.Primitives;

public class OutcomeSwitch : ContainerStepBody
{
    public override ExecutionResult Run(IStepExecutionContext context)
    {
        if (context.PersistenceData == null)
        {
            var result = ExecutionResult.Branch([context.Item], new ControlPersistenceData { ChildrenActive = true });
            result.OutcomeValue = GetPreviousOutcome(context);
            return result;
        }

        if ((context.PersistenceData is ControlPersistenceData) && (context.PersistenceData as ControlPersistenceData).ChildrenActive)
        {
            if (context.Workflow.IsBranchComplete(context.ExecutionPointer.Id))
            {
                return ExecutionResult.Next();
            }
            else
            {
                var result = ExecutionResult.Persist(context.PersistenceData);
                result.OutcomeValue = GetPreviousOutcome(context);
                return result;
            }
        }

        throw new CorruptPersistenceDataException();
    }

    private static object GetPreviousOutcome(IStepExecutionContext context)
    {
        var prevPointer = context.Workflow.ExecutionPointers.FindById(context.ExecutionPointer.PredecessorId);
        return prevPointer.Outcome;
    }
}
