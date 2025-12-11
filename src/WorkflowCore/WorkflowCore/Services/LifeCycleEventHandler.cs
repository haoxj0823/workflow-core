using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Services;

public delegate void LifeCycleEventHandler(LifeCycleEvent evt);
