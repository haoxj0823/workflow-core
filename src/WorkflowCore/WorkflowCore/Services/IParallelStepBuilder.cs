using WorkflowCore.Primitives;

namespace WorkflowCore.Services;

public interface IParallelStepBuilder<TData, TStepBody>
    where TStepBody : IStepBody
{
    IParallelStepBuilder<TData, TStepBody> Do(Action<IWorkflowBuilder<TData>> builder);

    IStepBuilder<TData, Sequence> Join();
}
