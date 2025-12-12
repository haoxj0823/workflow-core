using Newtonsoft.Json;
using SharpYaml.Serialization;
using WorkflowCore.Models.v1;

namespace WorkflowCore.Services;

public static class Deserializers
{
    private readonly static Serializer yamlSerializer = new();

    public readonly static Func<string, DefinitionSourceV1> Json = JsonConvert.DeserializeObject<DefinitionSourceV1>;

    public readonly static Func<string, DefinitionSourceV1> Yaml = (source) => yamlSerializer.DeserializeInto(source, new DefinitionSourceV1());
}
