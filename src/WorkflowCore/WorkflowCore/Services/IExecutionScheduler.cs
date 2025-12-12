using WorkflowCore.Models;

namespace WorkflowCore.Services;

public interface IExecutionScheduler
{
    Task DetermineNextExecutionTime(WorkflowInstance workflow, WorkflowDefinition def);
}
