namespace WorkflowCore.Models;

public abstract class DefinitionSource
{
    public string Id { get; set; }

    public int Version { get; set; }

    public string Description { get; set; }
}
