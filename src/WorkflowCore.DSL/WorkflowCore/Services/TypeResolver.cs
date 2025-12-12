namespace WorkflowCore.Services;

public class TypeResolver : ITypeResolver
{
    public Type FindType(string name)
    {
        return Type.GetType(name, true, true);
    }
}
