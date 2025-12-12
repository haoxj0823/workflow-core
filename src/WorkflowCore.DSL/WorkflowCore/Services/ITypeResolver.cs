namespace WorkflowCore.Services;

public interface ITypeResolver
{
    Type FindType(string name);
}