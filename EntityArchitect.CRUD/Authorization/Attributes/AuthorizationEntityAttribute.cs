namespace EntityArchitect.CRUD.Authorization.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class AuthorizationEntityAttribute : Attribute
{
    public Type[] EntityTypes { get; }

    public AuthorizationEntityAttribute(params Type[] entityTypes)
    {
        EntityTypes = entityTypes;
    }
}