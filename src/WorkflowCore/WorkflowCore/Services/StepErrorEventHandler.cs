using WorkflowCore.Models;

namespace WorkflowCore.Services;

public delegate void StepErrorEventHandler(WorkflowInstance workflow, WorkflowStep step, Exception exception);
