namespace EntityArchitect.CRUD.Authorization.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class AuthorizationEntityAttribute() : Attribute
{
    public string? Name { get; }
    
    public AuthorizationEntityAttribute(string name) : this()
    {
        Name = name;
    }
}