
namespace EntityArchitect.Entities.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class RelationOneToManyAttribute<T>(string PropertyName) : Attribute;