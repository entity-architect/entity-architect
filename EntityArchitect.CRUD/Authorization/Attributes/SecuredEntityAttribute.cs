using System;

namespace EntityArchitect.CRUD.Authorization.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class SecuredEntityAttribute : Attribute
{
    public Type[] EntityTypes { get; }

    public SecuredEntityAttribute(params Type[] entityTypes)
    {
        EntityTypes = entityTypes;
    }
}