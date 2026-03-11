namespace EntityArchitect.CRUD.Authorization;

public class SecuredAttribute(params Type[] securedByTypes) : Attribute
{
    public Type[] SecuredByTypes { get; } = securedByTypes;
}