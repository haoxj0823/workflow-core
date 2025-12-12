using System.Dynamic;

namespace WorkflowCore.Models.v1;

public class StepSourceV1
{
    public string StepType { get; set; }

    public string Id { get; set; }

    public string Name { get; set; }

    public string CancelCondition { get; set; }

    public WorkflowErrorHandling? ErrorBehavior { get; set; }

    public TimeSpan? RetryInterval { get; set; }

    public List<List<StepSourceV1>> Do { get; set; } = [];

    public List<StepSourceV1> CompensateWith { get; set; } = [];

    public bool Saga { get; set; } = false;

    public string NextStepId { get; set; }

    public ExpandoObject Inputs { get; set; } = new();

    public Dictionary<string, string> Outputs { get; set; } = [];

    public Dictionary<string, string> SelectNextStep { get; set; } = [];

    public bool ProceedOnCancel { get; set; } = false;
}
