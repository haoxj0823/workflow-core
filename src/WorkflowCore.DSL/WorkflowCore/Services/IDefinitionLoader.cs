using WorkflowCore.Models;
using WorkflowCore.Models.v1;

namespace WorkflowCore.Services;

public interface IDefinitionLoader
{
    WorkflowDefinition LoadDefinition(string source, Func<string, DefinitionSourceV1> deserializer);
}