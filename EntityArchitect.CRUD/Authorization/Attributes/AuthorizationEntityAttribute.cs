namespace EntityArchitect.CRUD.Authorization.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class AuthorizationEntityAttribute : Attribute
{
    public string? Name { get; }

    public AuthorizationEntityAttribute(string? name = null)
    {
        Name = name ?? GetDefaultName();
    }

    private static string GetDefaultName()
    {
        var stackTrace = new System.Diagnostics.StackTrace();
        var frame = stackTrace.GetFrame(1);
        var method = frame?.GetMethod();
        return method?.DeclaringType?.Name ?? "UnknownClass";
    }
}