namespace WorkflowCore.Models.v1;

public class DefinitionSourceV1 : DefinitionSource
{
    public string DataType { get; set; }

    public WorkflowErrorHandling DefaultErrorBehavior { get; set; }

    public TimeSpan? DefaultErrorRetryInterval { get; set; }

    public List<StepSourceV1> Steps { get; set; } = [];
}
