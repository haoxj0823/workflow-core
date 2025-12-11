using WorkflowCore.Exceptions;
using WorkflowCore.Models;
using WorkflowCore.Services;

namespace WorkflowCore.Primitives;

public class Sequence : ContainerStepBody
{
    public override ExecutionResult Run(IStepExecutionContext context)
    {
        if (context.PersistenceData == null)
        {
            return ExecutionResult.Branch([context.Item], new ControlPersistenceData { ChildrenActive = true });
        }

        if ((context.PersistenceData is ControlPersistenceData) && (context.PersistenceData as ControlPersistenceData).ChildrenActive)
        {
            if (context.Workflow.IsBranchComplete(context.ExecutionPointer.Id))
            {
                return ExecutionResult.Next();
            }

            return ExecutionResult.Persist(context.PersistenceData);
        }

        throw new CorruptPersistenceDataException();
    }
}
