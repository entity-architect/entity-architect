
namespace EntityArchitect.Entities.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class RelationManyToOneAttribute<T>(string PropertyName) : Attribute;