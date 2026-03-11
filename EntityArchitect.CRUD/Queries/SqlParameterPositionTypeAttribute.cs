using System;

namespace EntityArchitect.CRUD.Queries;

[AttributeUsage(AttributeTargets.Property)]
public class SqlParameterPositionTypeAttribute(SqlParameterPosition position) : Attribute
{
    public SqlParameterPosition Position { get; } = position;
}