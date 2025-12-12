using System.Collections.Concurrent;
using WorkflowCore.Models;
using WorkflowCore.Services.FluentBuilders;

namespace WorkflowCore.Services;

public class WorkflowRegistry : IWorkflowRegistry
{
    private readonly IWorkflowBuilder _workflowBuilder;
    private readonly ConcurrentDictionary<string, WorkflowDefinition> _workflowDefinitions = [];
    private readonly ConcurrentDictionary<string, WorkflowDefinition> _latestVersionDefinitions = [];

    public WorkflowRegistry(IWorkflowBuilder workflowBuilder)
    {
        _workflowBuilder = workflowBuilder;
    }

    public WorkflowDefinition GetDefinition(string workflowId, int? version = null)
    {
        if (version.HasValue)
        {
            if (_workflowDefinitions.TryGetValue($"{workflowId}-{version}", out var value))
            {
                return value;
            }
        }
        else
        {
            if (_latestVersionDefinitions.TryGetValue(workflowId, out var value))
            {
                return value;
            }
        }

        return default;
    }

    public void RegisterWorkflow(IWorkflow workflow)
    {
        var builder = _workflowBuilder.UseData<object>();
        workflow.Build(builder);
        var def = builder.Build(workflow.Id, workflow.Version);
        RegisterWorkflow(def);
    }

    public void RegisterWorkflow<TData>(IWorkflow<TData> workflow)
        where TData : new()
    {
        var builder = _workflowBuilder.UseData<TData>();
        workflow.Build(builder);
        var def = builder.Build(workflow.Id, workflow.Version);
        RegisterWorkflow(def);
    }

    public void RegisterWorkflow(WorkflowDefinition definition)
    {
        if (_workflowDefinitions.ContainsKey($"{definition.Id}-{definition.Version}"))
        {
            throw new InvalidOperationException($"Workflow {definition.Id} version {definition.Version} is already registered");
        }

        lock (_workflowDefinitions)
        {
            _workflowDefinitions[$"{definition.Id}-{definition.Version}"] = definition;

            if (_latestVersionDefinitions.TryGetValue(definition.Id, out var value))
            {
                if (value.Version <= definition.Version)
                {
                    _latestVersionDefinitions[definition.Id] = definition;
                }
            }
            else
            {
                _latestVersionDefinitions[definition.Id] = definition;
            }
        }
    }

    public void DeregisterWorkflow(string workflowId, int version)
    {
        if (!_workflowDefinitions.ContainsKey($"{workflowId}-{version}"))
        {
            return;
        }

        lock (_workflowDefinitions)
        {
            _workflowDefinitions.TryRemove($"{workflowId}-{version}", out var _);

            if (_latestVersionDefinitions[workflowId].Version == version)
            {
                _latestVersionDefinitions.TryRemove(workflowId, out var _);

                var latest = _workflowDefinitions.Values
                    .Where(x => x.Id == workflowId)
                    .OrderByDescending(x => x.Version)
                    .FirstOrDefault();

                if (latest != default)
                {
                    _latestVersionDefinitions[workflowId] = latest;
                }
            }
        }
    }

    public bool IsRegistered(string workflowId, int version)
    {
        return _workflowDefinitions.ContainsKey($"{workflowId}-{version}");
    }

    public IEnumerable<WorkflowDefinition> GetAllDefinitions()
    {
        return _workflowDefinitions.Values;
    }
}