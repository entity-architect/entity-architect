using System;

namespace EntityArchitect.CRUD.Authorization.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SecuredAttribute : Attribute
{
    public Type[] EntityTypes { get; }

    public SecuredAttribute(params Type[] entityTypes)
    {
        EntityTypes = entityTypes;
    }
}